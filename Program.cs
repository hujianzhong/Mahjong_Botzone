
#define SUPPORT_CONCEALED_KONG_AND_MELDED_KONG
using System;
using Newtonsoft.Json;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Reflection.Emit;

namespace HJZ
{

    struct InputData
    {
        public dynamic[]
            requests, // 从平台获取的信息集合   
            responses;// 自己曾经输出的信息集合
        public string
            data, // 该对局中，上回合该Bot运行时存储的信息 
            globaldata; // 来自上次对局的、Bot全局保存的信息 
    }
    public class RecordResult
    {
        [JsonProperty("response")]
        public String response { get; set; }
        public string
            debug, // 调试信息，将被写入log
            data, // 此回合的保存信息，将在下回合输入
            globaldata; // Bot的全局保存信息，将会在下回合输入，对局结束后也会保留，下次对局可以继续利用
    }

    class Program
    {
        static int[] timeOut25 = new int[] { 181, 2759, 2809, 5176, 9674, 10577, 13092, 14003, 17510, 23345, 23958, 23965, 27397, 29185, 31390, 32550, 33816, 33823, 34207, 36941, 37831, 37840, 39451, 43160, 43405, 44149, 46628, 47261, 50673, 52446, 52811, 54835, 55170, 55557, 56042, 57650, 58131, 58286,
            58901,63766,64823,65989,65997,66631,66761,66859,66867,67330,67579,69114,72688,74531,75301,75571,79601,81988,85706,87190,87664,87672,87680,87839,88508,88669,90163,94040,94095,94486,95305,95821,97147,97155,97163,97171,97604,98079,99725,102791,103099 ,
            103099,103169,104894,104926,107873,108576,110285,110837,114659,115224,116777,119698,119766,119774,121355,121721,122955,122963,124869,126431,127170,127993,130876,133451,134038,135173,137766,138750,141631,143981,144118,
            144120,148066,148074,148131,148138,148724,148732,149122,149128,149136,150399,154620,154631,154810,155965,156873,157564,157891,159495,159643,162195,162202,162294,162302,166067,166717,167322,170016,171417,172606,173631,
            174714,175706,179657,181069,182974,186177,187180,187410,187514,188771,188974,188978,189180,
            191205,191206,191926,191933,197753,199070,199072,199581,200164,200167,201768,202395,202403,203167,203507,204491,205184,205948,206771,210818,210820,211670,212742,213397,214146,217095,218100,218259,218267,219061,219269,
            219270,220617,221168,221324,221332,221356,223055,223063,224861,225698,225980,230418,230813,230903,231994,232002,232009,232359,232403,232417,233010,234476,235138,236160,236167,236705,237143,237299,237858,239828,240871 };
        static int[] timeOut30 = {14003,23958,54835,72688,88508,88669,94040,102791,107873,115224,124869,135173,148732,155965,172606,182974,191205,191206,191926,200164,200167,202395,202403,203507,206771,210818,219061,219270,221168,
            230813,232403,232417,237299,240871,260426,289528,289588,295768,295776,298047,301709,301887,302061};
        static int[] deffOut = new int[] { 12, 92, 134, 158, 240, 268, 268, 270, 276, 289, 309, 323, 343, 368, 372, 376, 384, 392, 396, 404, 408, 412, 412, 428, 442, 487, 513, 517, 563, 585, 587, 639, 647, 653, 657, 661, 667, 669, 681, 697, 709, 717, 719, 723,
            785, 787, 849, 859, 893, 964, 976, 980, 988, 992, 996, 1060, 1067, 1069, 1081, 1083, 1128, 1162, 1170, 1176, 1180, 1284, 1292, 1314, 1369, 1379, 1385, 1393, 1413, 1439, 1451, 1475, 1483, 1499, 1596, 1618, 1640, 1686, 1692, 1700, 1702, 1706, 1708, 1710, 1716, 1728, 1730 };
        static int[] deffCPG = new int[] { 43, 55, 59, 61, 99, 139, 165, 165, 197, 227, 237, 251, 271, 304, 330, 344, 369, 369, 466, 484, 492, 496, 512, 520, 552, 570, 572, 582, 590, 594, 596, 596, 604, 616, 630, 636, 644, 644, 668, 670, 710, 736, 742, 776,
            776, 792, 842, 848, 858, 858, 862, 880, 884, 888, 890, 894, 937, 949, 957, 973, 1003, 1003, 1013, 1031, 1057, 1057, 1074, 1080, 1090, 1094, 1165, 1177, 1191, 1235, 1235, 1259, 1259, 1275, 1301, 1360, 1360, 1412, 1414, 1440, 1450, 1466, 1482, 1537, 1547, 1583, 1619, 1619, 1699, 1717 };
        static char[] ch = new char[] { 'D', 'N', 'X', 'B' };
        const int ABC_len = 144 - 8;
        static int[] pKRC = new int[ABC_len / 4 + 10 + 1];
        private static Mahjong[] mj = new Mahjong[4];
        static string[] Lines;
        static InputData input = new InputData();
        static String OCPG_String = "";
        static bool bCompitition = true;//-------------------比赛与调试，需要修改 false true

        static void Main(string[] args) // 请保证文件中只有一个类定义了Main方法
        {
            for (int i = 0; i < 4; i++)
            {
                mj[i] = new Mahjong(ch[i]);
                mj[i].InitData();
            }
            String InStr1 = "{\"requests\":[\"0 3 0\",\"1 0 0 0 0 W7 T9 T3 B1 W9 B4 F1 T3 J1 J1 T7 T8 B9\",\"3 0 DRAW\",\"3 0 PLAY F3\",\"3 1 DRAW\",\"3 1 PLAY F2\",\"3 2 DRAW\",\"3 2 PLAY J3\",\"2 B7\",\"3 3 PLAY F1\",\"3 0 DRAW\",\"3 0 PLAY F2\",\"3 1 DRAW\",\"3 1 PLAY T9\",\"3 2 DRAW\",\"3 2 PLAY J3\",\"2 W9\",\"3 3 PLAY B1\",\"3 0 DRAW\",\"3 0 PLAY J1\",\"3 3 PENG B4\",\"3 0 CHI B4 B8\",\"3 1 CHI B7 T5\",\"3 2 DRAW\",\"3 2 PLAY F3\",\"2 B9\",\"3 3 PLAY W9\",\"3 0 DRAW\",\"3 0 PLAY B1\",\"3 1 DRAW\",\"3 1 PLAY W1\",\"3 2 DRAW\",\"3 2 PLAY J1\",\"2 B8\",\"3 3 PLAY B9\",\"3 0 DRAW\",\"3 0 PLAY T1\",\"3 1 DRAW\",\"3 1 PLAY T5\"],\"responses\":[\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY F1\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY B1\",\"PASS\",\"PASS\",\"PENG B4\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY W9\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY B9\",\"PASS\",\"PASS\",\"PASS\",\"PASS\"]}";
            InStr1 = "{\"requests\":[\"0 3 3\",\"1 0 0 0 0 T6 B9 T8 W1 T9 T9 J1 W2 W4 T3 B5 W5 T8\",\"3 0 DRAW\",\"3 0 PLAY B1\",\"3 1 DRAW\",\"3 1 PLAY W4\",\"3 2 DRAW\",\"3 2 PLAY J3\",\"2 T5\",\"3 3 PLAY T9\",\"3 0 DRAW\",\"3 0 PLAY B4\",\"3 1 DRAW\",\"3 1 PLAY B9\",\"3 2 DRAW\",\"3 2 PLAY F4\",\"2 T2\",\"3 3 PLAY T5\",\"3 0 DRAW\",\"3 0 PLAY T4\",\"3 1 DRAW\",\"3 1 PLAY F4\",\"3 2 DRAW\",\"3 2 PLAY J1\",\"2 J1\",\"3 3 PLAY B9\",\"3 0 DRAW\",\"3 0 PLAY J3\",\"3 1 DRAW\",\"3 1 PLAY B8\",\"3 2 DRAW\",\"3 2 PLAY T9\",\"2 B4\",\"3 3 PLAY B4\",\"3 0 DRAW\",\"3 0 PLAY T9\",\"3 1 DRAW\",\"3 1 PLAY T6\",\"3 2 DRAW\",\"3 2 PLAY W1\",\"2 W2\",\"3 3 PLAY T9\",\"3 0 DRAW\",\"3 0 PLAY W3\",\"3 1 DRAW\",\"3 1 PLAY T4\",\"3 2 DRAW\",\"3 2 PLAY W7\",\"2 B2\",\"3 3 PLAY B5\",\"3 0 DRAW\",\"3 0 PLAY W2\",\"3 3 PENG T8\",\"3 0 DRAW\",\"3 0 PLAY W7\",\"3 1 DRAW\",\"3 1 PLAY W3\",\"3 2 CHI W4 B1\",\"2 B1\",\"3 3 PLAY W1\",\"3 0 DRAW\",\"3 0 PLAY W6\",\"3 1 DRAW\",\"3 1 PLAY W1\",\"3 2 DRAW\",\"3 2 PLAY B9\",\"2 T1\",\"3 3 PLAY B1\",\"3 0 DRAW\",\"3 0 PLAY B7\",\"3 1 DRAW\",\"3 1 PLAY T8\",\"3 2 DRAW\",\"3 2 PLAY W3\",\"3 3 CHI W4 T8\",\"3 0 DRAW\",\"3 0 PLAY B5\",\"3 1 DRAW\",\"3 1 PLAY T2\",\"3 2 DRAW\",\"3 2 PLAY B3\",\"2 J3\",\"3 3 PLAY J3\",\"3 0 DRAW\",\"3 0 PLAY B2\",\"3 1 DRAW\",\"3 1 PLAY F3\",\"3 2 DRAW\",\"3 2 PLAY W8\",\"3 1 PENG T4\",\"3 2 CHI T5 B8\",\"2 W1\",\"3 3 PLAY B2\"],\"responses\":[\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY T9\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY T5\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY B9\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY B4\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY T9\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY B5\",\"PASS\",\"PASS\",\"PENG T8\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY W1\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY B1\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"CHI W4 T8\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY J3\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PASS\",\"PLAY B2\"]}";
            
            if (bCompitition)
            {
                InStr1 = Console.ReadLine();
                Mahjong.TimeSpan = 3.8;
            }
            else Mahjong.TimeSpan = 33333.8;
            input = JsonConvert.DeserializeObject<InputData>(InStr1);
            // 分析自己收到的输入和自己过往的输出，并恢复状态2023-11-12 23:56:29
            int turnID = input.requests.Length - 1;
            if (input.responses.Length < input.requests.Length)
            {
                dynamic resp = new dynamic[turnID + 1];
                input.responses.CopyTo(resp, 0);
                resp[turnID] = "PASS";
                input.responses = resp;
            }
            int itmp = 0, ha = 0;
            String str1 = input.requests[0];
            string[] SplitStr = str1.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            int.TryParse(SplitStr[1], out ha);
            int.TryParse(SplitStr[2], out itmp);
            String reStr = "", outFanDebugInfo = "", outStr = "";

            //Console.Clear();
            //CheckJsonComp(ha, input, turnID);//---------------------------------------- 检查复盘JSON、比赛数据  sample.txt  data.txt
            //CheckJCAIdataGap("sample.txt", playerID, 115, 1125);//------------------------从data、sample数据中计算8种常用牌型到胡牌时的关键张差距
            //CheckJCAIdata("sample.txt", 16, 111800);//------------------------复盘data、sample数据
            //CheckJCAIdataForTime("sample.txt", playerID, 136757, 11128030);//------------------------计算时间
            //CheckJCAIdataFan("sample.txt", 2, 10000000);//-----------------------计算番数    AnalyseJCAIdataHu    
            //AnalyseJCAIdataHu("sample.txt", playerID, 2, 100000000);//-----------------------分析点炮胡牌时的番数、轮数、叫数情况，看是否必须胡      
            //AnalyseTin2HuStep("sample.txt", playerID, 20, 15000);//-----------------------分析听牌到胡牌有几步
            //StatisticsJCAIdataFan("sample.txt");//---------------------------------------统计胡牌时的番数情况             
            //StatisticsJCAI_HuTime("data.txt");

            mj[ha].InitData();
            CollectInfoJson(input, turnID);
            string str2 = MjCompInfo(ha, input, turnID, true);
            reStr = decision(ha, turnID, out outStr, out outFanDebugInfo, 2);
            RecordResult result = new RecordResult();
            result.response = reStr;
            if (!OCPG_String.Contains("结论"))
                str2 = str2.Substring(0, str2.IndexOf("底牌剩"));
            str2 = FormatHTMLOut(str2 + "\n" + OCPG_String, 100);
            if (reStr == "PASS") result.debug = "";
            else result.debug = str2;
            string jsonStr = JsonConvert.SerializeObject(result);

            Console.Clear();
            Console.WriteLine(jsonStr);
        }
        //对JCAI格式数据文件进行复盘
        static void CheckJCAIdata(String DataFile, int startLine, int endLine)
        {
            String outStr = "", outFanStr = "", response, reStr = "", defferOut = "{", defferCPG = "{";
            int[] yb = new int[14]; int[] handin = new int[13];
            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);
            //startLine = 1;//6436,19484, 50673 54835
            for (int i = startLine; i <= endLine; i++)
            {
                //if (!(Lines[i + 1] + Lines[i + 2]).Contains("Hu"))
                //    continue; deffOut
                //if (!timeOut25.Contains(i))
                //    continue;
                //if (!timeOut30.Contains(i))
                //    continue;
                //if (!deffOut.Contains(i))
                //    continue;
                //if (!deffCPG.Contains(i))
                //    continue; 
                string[] Requests = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (Requests.Length == 0 || Requests[0] == "Match" || Requests[0] == "Huang" || Requests[0] == "Score"
                    || Requests[0] == "Wind" || Requests[2] == "Deal" || Requests[0] == "Fan" || Lines[i + 1] == "Huang")
                {
                    //Console.WriteLine(Lines[i]);
                    continue;
                }

                response = Lines[i + 1];
                string[] Respones = response.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                int playerID = int.Parse(Requests[1]);

                CollectInfoFromData(DataFile, startLine, i); reStr = "PASS";
                if (Requests[2].Contains("Draw"))
                {
                    //if ((mj[playerID].Can_AnGang_Cards() > 0 || mj[playerID].Can_HouBaGang() > 0 || mj[playerID].Can_BaGang()))
                    //continue;
                    Array.Clear(Mahjong.RetOutCard, 0, Mahjong.RetOutCard.Length);
                    //writeMjInfoDATA(playerID, Lines, i, response);
                    mj[playerID].HandInCard.CopyTo(handin, 0);
                    reStr = decision(playerID, i, out outStr, out outFanStr, 0);
                    
                    handin.CopyTo(mj[playerID].HandInCard, 0);
                    int level = OCPG_String.IndexOf("L");//取出搜索深度+ Mahjong.RetCPG(Str2Card(reStr.Substring(14)))
                    level = OCPG_String[level + 1] - '0';
                    {
                        int[] tmp14 = new int[14];
                        mj[playerID].HandInCard.CopyTo(tmp14, 0);
                        tmp14[13] = Mahjong.in_card;
                        Array.Sort(tmp14);
                        int ww = getWeightFromCard(tmp14, Mahjong.RetOutCard, response.Substring(14));
                        //Console.WriteLine(reStr.Substring(7) + " W=" + ww);
                        {
                            if ((reStr + response).Contains("Play "))
                                if (level < 4 && reStr != response && OCPG_String.Contains("最大权重牌"))
                                {
                                    defferOut += i + ",";
                                    Debug.WriteLine("\n" + OCPG_String + i + "\n" + Mahjong.OCPG_str);//打的牌不一样
                                }
                            //if (Can_Gang_FetchedCOM(playerID))
                            //    if (reStr != response && Mahjong.SecondMaxOfArray(Mahjong.RetCPG) < 90)
                            //    {
                            //        defferOut += i + ",";
                            //        Debug.WriteLine("\n" + OCPG_String + i);//能杠
                            //    }
                            //if ((reStr + response).Contains("Hu"))
                            //    if (reStr.Substring(0, 12) != response.Substring(0, 12))
                            //    {
                            //        defferOut += i + ",";
                            //        Debug.WriteLine("\n" + outFanStr + i);//能胡
                            //    }
                        }
                    }
                }
                else if (Requests[2].Contains("Play"))
                {
                    //continue;
                    //if (!(mj[0].Can_Bump_Cards(Mahjong.out_card) || mj[1].Can_Bump_Cards(Mahjong.out_card) ||
                    //    mj[2].Can_Bump_Cards(Mahjong.out_card) || mj[3].Can_Bump_Cards(Mahjong.out_card)))
                    //    continue;
                    //if (!(mj[0].Can_MingGang(Mahjong.out_card) || mj[1].Can_MingGang(Mahjong.out_card) ||
                    //    mj[2].Can_MingGang(Mahjong.out_card) || mj[3].Can_MingGang(Mahjong.out_card)))
                    //    continue;
                    //if (!(mj[(playerID + 1) % 4].Can_Chi_Cards(Mahjong.out_card) && mj[(playerID + 1) % 4].Can_MingGang(Mahjong.out_card)))
                    //    continue;
                    string[] outStrs = new string[3]; string retMapStr = "PASS";
                    //Console.WriteLine("   -" + i.ToString().PadRight(3) + ":" + Lines[i]);//
                    for (int j = playerID + 1; j < playerID + 4; j++)
                    {
                        bool canCPGH = false;
                        int otherID = j % 4;
                        if (Requests[2].Contains("Play"))
                        {
                            canCPGH |= mj[otherID].Can_Hu_OutCard(Mahjong.out_card) > 0;
                            canCPGH |= (otherID - playerID + 4) % 4 == 1 && mj[otherID].Can_Chi_Cards(Mahjong.out_card);
                            canCPGH |= mj[otherID].Can_Bump_Cards(Mahjong.out_card);
                            canCPGH |= mj[otherID].Can_MingGang(Mahjong.out_card);
                        }

                        if (canCPGH)
                        {
                            response = getSelfResponse(otherID, Lines[i + 1]);
                            //Console.Write("    ");//
                            //writeMjInfoDATA(otherID, Lines, i, response);//
                            mj[otherID].HandInCard.CopyTo(handin, 0);
                            string Str = decision(otherID, i, out outStr, out outFanStr, 0);
                            reStr = priorityHGPC(reStr, Str);
                            if(reStr != response)
                            { }
                            handin.CopyTo(mj[otherID].HandInCard, 0);
                            {
                                //Console.WriteLine(reStr);
                                //if (Mahjong.out_card > 0 && (otherID - playerID + 4) % 4 == 1 && mj[otherID].Can_Chi_Cards(Mahjong.out_card))
                                //    if (reStr != response && Mahjong.SecondMaxOfArray(Mahjong.RetCPG) < 90)
                                //    {
                                //        defferCPG += i + ",";
                                //        Debug.WriteLine("\n" + OCPG_String + i);//能吃
                                //    }
                                //if (Mahjong.out_card > 0 && mj[otherID].Can_Bump_Cards(Mahjong.out_card))
                                //    if (reStr != response && Mahjong.SecondMaxOfArray(Mahjong.RetCPG) < 90)
                                //    {
                                //        defferCPG += i + ",";
                                //        Debug.WriteLine("\n" + OCPG_String + i);//能碰
                                //    }
                                //if (Mahjong.out_card > 0 && mj[otherID].Can_MingGang(Mahjong.out_card))
                                //    if (reStr != response && Mahjong.SecondMaxOfArray(Mahjong.RetCPG) < 90)
                                //    {
                                //        defferCPG += i + ",";
                                //        Debug.WriteLine("\n" + OCPG_String + i);//能杠
                                //    }
                                //if ((reStr + response).Contains("Hu") && Respones[1] == otherID.ToString())
                                //    if (reStr.Substring(0, 12) != response.Substring(0, 12))
                                //    {
                                //        defferCPG += i + ",";
                                //        Debug.WriteLine("\n" + outFanStr + i);//能胡
                                //    }
                            }
                        }
                    }
                    //if (retMapStr != reStr)
                    //{ }
                }
                else if (Requests[2].Contains("BuGang"))
                {
                    string retMapStr = "PASS";
                    for (int j = playerID + 1; j < playerID + 4; j++)
                    {
                        retMapStr = "PASS";
                        mj[Mahjong.old_azimuth].ganged = true;
                        Mahjong.out_card = Str2Card(Requests[3]);
                        if (mj[j % 4].Can_Hu_OutCard(Mahjong.out_card) > 0)
                            retMapStr = "Player " + (j % 4) + " Hu " + Card2Str(Mahjong.out_card);
                        Mahjong.out_card = 0;
                        reStr = decision(j % 4, i, out outStr, out outFanStr, 0);
                        if (response != reStr && int.Parse(Respones[1]) == j % 4)
                        { }
                    }
                }
                //else if (Requests[2].Contains("Peng") || Requests[2].Contains("Gang") || Requests[2].Contains("Chi"))
                //    Console.WriteLine("   *" + i.ToString().PadRight(3) + ":" + Lines[i]);
                response = response;
            }
            defferOut += "}"; defferCPG += "}";
        }
        //对比赛用的Json格式数据文件进行复盘
        static void CheckJsonComp(int ha, dynamic input, int turnID)
        {
            Console.WriteLine("0".PadRight(3) + ":  " + input.requests[0]);
            Console.WriteLine("1".PadRight(3) + ":  " + input.requests[1]);
            String outStr = "", outFanStr = "";
            for (int i = 2; i <= turnID; i++)
            {
                if (i == 5)
                { }
                String Str = input.requests[i];
                DateTime DateTime1 = DateTime.Now;
                CollectInfoJson(input, i);
                Console.Write(MjCompInfo(ha, input, i));
                String reStr = Str = decision(ha, i, out outStr, out outFanStr, 1);
                double span = (DateTime.Now - DateTime1).TotalSeconds;

                string respone = input.responses[i];
                if (!String.IsNullOrEmpty(outStr) && reStr != input.responses[i])
                    reStr += "\n--" + outStr;
                else if (String.IsNullOrEmpty(outStr) && reStr != input.responses[i])
                    reStr += "*";
                if (span >= 0.1) reStr += " " + span.ToString("f4") + "秒";
                Console.WriteLine(reStr);
                if (!respone.Contains(Str))
                { }
            }
        }
        //用JCAI格式数据文件进行复盘，研究11种6分以上的常胡牌型，它们的关键张到完成的距离
        static void CheckJCAIdataGap(String DataFile, int playerID, int startLine, int endLine)
        {
            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);
            //startLine = 559;
            for (int i = startLine; i <= endLine; i++)
            {
                string[] SplitStr = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (SplitStr.Length == 0 || SplitStr[0] == "Match" || SplitStr[0] == "Huang" || SplitStr[0] == "Score"
                    || SplitStr[0] == "Wind" || SplitStr[2] == "Deal" || SplitStr[0] == "Fan" || Lines[i + 1] == "Huang")
                    continue;
                if (!(playerID != int.Parse(SplitStr[1]) && (SplitStr[2] == "Play" || SplitStr[2] == "BuGang") ||
                    playerID == int.Parse(SplitStr[1]) && SplitStr[2] == "Draw"))//需要关注的是自己摸牌,别家打牌与巴杠
                    continue;

                int[] gap = new int[8], TWT;
                int[][] outCards = new int[8][], inCards = new int[8][];

                CollectInfoFromData(DataFile, startLine, i);
                if (Mahjong.in_card > 0)
                {
                    int[] yb = new int[14];
                    mj[playerID].HandInCard.CopyTo(yb, 0);
                    yb[13] = Mahjong.in_card;
                    Array.Sort(yb);
                    int[] oCards, iCards;
                    //mj[playerID].ucnKeyCard(yb, out gap, out oCards, out iCards);
                }
            }
        }

        //对JCAI格式数据文件进行复盘，检查运行时间
        static void CheckJCAIdataForTime(String DataFile, int playerID, int startLine, int endLine)
        {
            String outStr = "", outFanStr = "", response;
            int[] yb13 = new int[13];
            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);
            //startLine = 6877;
            for (int i = startLine; i <= endLine; i++)
            {
                string[] SplitStr = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (SplitStr.Length > 0 && SplitStr[0] == "Match")
                    Console.Write(i + " ");
                if (SplitStr.Length == 0 || SplitStr[0] == "Match" || SplitStr[0] == "Huang" || SplitStr[0] == "Score"
                    || SplitStr[0] == "Wind" || SplitStr[2] == "Deal" || SplitStr[0] == "Fan" || Lines[i + 1] == "Huang")
                    continue;
                if (!(playerID != int.Parse(SplitStr[1]) && (SplitStr[2] == "Play" || SplitStr[2] == "BuGang") ||
                    playerID == int.Parse(SplitStr[1]) && SplitStr[2] == "Draw"))//需要关注的是自己摸牌,别家打牌与巴杠
                    continue;

                response = Lines[i + 1];
                if (response.Length > 17 && response[5] == 'r')
                    response = response.Substring(0, 17).Trim();
                string[] SplitResponse = response.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (SplitResponse[2] == "Draw" || SplitResponse[1] != playerID.ToString() && (SplitResponse[2] == "Peng" || SplitResponse[2] == "Chi" || SplitResponse[2].IndexOf("Gang") >= 0))
                    response = "PASS";

                DateTime DateTime1 = DateTime.Now;
                CollectInfoFromData(DataFile, startLine, i);
                mj[playerID].HandInCard.CopyTo(yb13, 0);
                String reStr = decision(playerID, i, out outStr, out outFanStr, -1);
                int span = (int)((DateTime.Now - DateTime1).TotalSeconds * 1000);
                if (span >= 2000)
                {
                    Console.WriteLine(); string str = "";
                    yb13.CopyTo(mj[playerID].HandInCard, 0);
                    writeMjInfoDATA(playerID, Lines, i, response);
                    if (reStr.Length > 7)
                        str += reStr.Substring(7, reStr.Length - 7).PadRight(10);
                    else
                        str += reStr.PadRight(10);
                    str += " T:" + span.ToString().PadRight(4) + "ms";
                    Console.WriteLine(str);
                }
            }
        }
        //对JCAI格式数据文件进行复盘，检查胡牌时番数
        static void CheckJCAIdataFan(String DataFile, int startLine, int endLine)
        {
            String outStr = "", outFanStr = "";
            int[] whenHuZimo = new int[21], whenHuDianPao = new int[21];

            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);
            for (int i = startLine; i < Lines.Length - 1 && i <= endLine; i++)//endLine
            {
                string[] SplitStr = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                string[] NextStrs = Lines[i + 1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                //if (Lines[i] == "Huang")
                //{ Console.WriteLine((i + ": ABC=0  Huang").PadLeft(10)); continue; }
                //else 
                    if (NextStrs.Length <= 2 || NextStrs[2] != "Hu")
                        continue;
                int playerID = int.Parse(NextStrs[1]);
                CollectInfoFromData(DataFile, startLine, i);
                String reStr = decision(playerID, i, out outStr, out outFanStr, 0);
                string[] FanStrs = Lines[i + 2].Split(new Char[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] outFanStrs = outFanStr.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);

                bool same = true;
                //for (int j = 2; j < outFanStrs.Length; j++)
                //    if (!outFanStrs[j].Contains(FanStrs[j].Substring(0, 2)))
                //        same = false;
                if (FanStrs[1] != outFanStrs[1])
                    same = false;
                if (!same)
                {
                    Console.WriteLine(Lines[i] + " --> " + Lines[i + 1]);
                    writeMjInfoDATA(playerID, Lines, i, "PASS");
                    Console.WriteLine(reStr);
                    Console.WriteLine("".PadLeft(3 - outFanStrs[0].Length) + outFanStr);
                    Console.WriteLine(Lines[i + 2]);
                }
            }
        }
        //研究JCAI格式数据文件，检查胡牌时的牌型，看能否不胡追求大牌
        static void AnalyseJCAIdataHu(String DataFile, int playerID, int startLine, int endLine)
        {
            String outStr = "", outFanStr = "";
            int[] whenHuZimo = new int[(27 + 7) * 4 - 13 * 4], whenHuDianPao = new int[(27 + 7) * 4 - 13 * 4];
            int huangZhuang = 0, ziMu = 0, dianPao = 0, panNum = 0;

            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);
            for (int i = startLine; i < Lines.Length - 1 && i <= endLine; i++)//endLine
            {
                string[] SplitStr = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                string[] NextStrs = Lines[i + 1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (Lines[i] == "Huang")
                { Console.WriteLine((i + ": ABC=0  Huang").PadLeft(120)); huangZhuang++; continue; }
                else if (NextStrs.Length <= 2 || NextStrs[2] != "Hu")
                    continue;
                playerID = int.Parse(NextStrs[1]);
                CollectInfoFromData(DataFile, startLine, i);
                String reStr = decision(playerID, i, out outStr, out outFanStr, -1);
                string[] FanStrs = Lines[i + 2].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                string[] outFanStrs = outFanStr.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);

                int HuiHe = Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);
                if (Mahjong.out_card > 0)
                {
                    //int origRow = Console.CursorTop;
                    //Console.SetCursorPosition(177, origRow);
                    //Console.Write("||");
                    //printMjInfoDATA(playerID, lines, i, "|");
                    //Console.WriteLine();
                    dianPao++;
                    whenHuDianPao[HuiHe]++;
                }
                else if (Mahjong.out_card == 0)
                {
                    //Console.Write(("ABC=" + HuiHe).PadLeft(114)); Console.Write("        |||");
                    //printMjInfoDATA(playerID, lines, i, "|");
                    //Console.WriteLine();
                    ziMu++;
                    whenHuZimo[HuiHe]++;
                }
                panNum = huangZhuang + ziMu + dianPao;
            }

            for (int i = 0; i < whenHuDianPao.Length; i++)
                Console.Write(i.ToString().PadLeft(6));
            Console.WriteLine();
            for (int i = 0; i < whenHuDianPao.Length; i++)
                Console.Write(whenHuDianPao[i].ToString().PadLeft(6));
            Console.WriteLine();
            for (int i = 0; i < whenHuDianPao.Length; i++)
                Console.Write(whenHuZimo[i].ToString().PadLeft(6));
            Console.WriteLine();
            Console.WriteLine("huangZhuang = " + huangZhuang + "  ziMu = " + ziMu + "  dianPao = " + dianPao + "  panNum = " + panNum);
        }
        //用JCAI格式数据文件进行复盘，分析下叫到胡牌时的距离
        static void AnalyseTin2HuStep(String DataFile, int playerID, int startLine, int endLine)
        {
            double[] jiaoKRCstep = new double[13], jiaoKRCount = new double[jiaoKRCstep.Length];
            double[] Tin2HuSteps = new double[jiaoKRCstep.Length];
            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);
            for (int i = startLine; i < Lines.Length - 1 && i <= endLine; i++)//endLine
            {
                if (Lines[i].IndexOf("Hu ") == -1)
                    continue;
                string[] SplitStr = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                CollectInfoFromData(DataFile, startLine, i);
                int huLine = Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);
                int tap = 0, Line = i - 1;
                string strID = SplitStr[1];
                String Str001 = "";
                playerID = int.Parse(SplitStr[1]);
                writeMjInfoDATA(playerID, Lines, i, "|");
                for (int j = i - 1; j > 0 && j > i - 200; j--)//endLine 
                    if (Lines[j].IndexOf("Deal") >= 0)
                    { Line = j + 1; break; }
                int NoHuTap = 0;
                for (int j = Line; j < i; j++)//endLine
                {
                    SplitStr = Lines[j].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                    if (SplitStr.Length < 1 || SplitStr[1] != strID)
                        continue;
                    if (SplitStr[1] == strID && (SplitStr[2] == "Draw" || SplitStr[2] == "Chi" || SplitStr[2] == "Peng"))
                        continue;

                    CollectInfoFromData(DataFile, startLine, j);
                    int WallNum = Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);

                    if (j >= 2347)
                    { }
                    int[] jiao = new int[14]; int[] fan = new int[14];
                    if (!mj[playerID].what_jiao(mj[playerID].HandInCard, jiao))
                        continue;
                    int jiaoKRC = 0, maxFan = 0, tinLine = 0;
                    for (int k = 0; k < jiao.Length; k++)
                        if (jiao[k] > 0)
                        {
                            fan[k] = mj[playerID].Computer_Score_Hu_OneCardOut(jiao[k]);
                            if (fan[k] >= Mahjong.JiBenHuFen - 1)
                            {
                                tinLine = Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);
                                tap = tinLine - huLine;
                                Str001 = mj[playerID].RetFanT();
                                jiaoKRC += mj[playerID].KnownRemainCard[jiao[k]];
                                if (fan[k] > maxFan)
                                    maxFan = fan[k];
                            }
                        }
                    if (Mahjong.LongOfCardNZ(jiao) > 0 && maxFan == 0 && NoHuTap == 0)
                        NoHuTap = Mahjong.LongOfCardNZ(Mahjong.AllBottomCard) - huLine;
                    if (tap > 0)
                    {
                        int origRow = Console.CursorTop;
                        Console.SetCursorPosition(90, origRow);
                        Console.Write("|");
                        writeMjInfoDATA(playerID, Lines, j, "|");
                        Console.SetCursorPosition(180, origRow);
                        Console.Write("|NoHuTap=" + NoHuTap.ToString().PadLeft(2));
                        Console.Write(" Tin2HuStep=" + tap.ToString().PadLeft(2));
                        Console.Write(" tinABC=" + tinLine.ToString().PadLeft(3));
                        Console.Write(" jiaoKRC=" + jiaoKRC.ToString().PadLeft(2));
                        Console.Write(" maxFan=" + maxFan.ToString().PadLeft(2));
                        for (int k = 0; k < jiao.Length; k++)
                            if (jiao[k] > 0)
                                Console.Write("  JiaoFen:" + jiao[k] + "->" + fan[k]);
                        Console.SetCursorPosition(278, origRow);
                        Console.WriteLine(Str001);
                        //Console.Write(i + "-> ");
                        int krcNum = jiaoKRC < 12 ? jiaoKRC : 12;
                        jiaoKRCstep[krcNum] += tap;
                        jiaoKRCount[krcNum]++;
                        break;
                    }
                }
            }
            double count = 0;
            for (int i = 0; i < Tin2HuSteps.Length; i++)//endLine
            {
                if (jiaoKRCount[i] > 0)
                    Tin2HuSteps[i] = jiaoKRCstep[i] / jiaoKRCount[i];// 15.23  12.55  11.50  11.38  12.17   9.08   8.83   9.18   9.72   7.15   6.67   6.85   6.97
                count += jiaoKRCount[i];                         //     0     1     2       3     4      5      6      7      8       9     10     11     12
                Console.Write(Tin2HuSteps[i].ToString("F2").PadLeft(7));
            }
        }
        // 用JCAI格式数据文件，统计胡牌时番种类型
        static void StatisticsJCAIdataFan(String DataFile)
        {
            int[] fanStatistics = new int[(int)FanT.FAN_TABLE_SIZE];
            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);
            int panNum = 0, FanNum = 0;
            for (int i = 0; i < Lines.Length; i++)//endLine
            {
                string[] SplitStr = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (String.IsNullOrEmpty(Lines[i]) || SplitStr[0] != "Fan")
                    continue;
                panNum++;
                FanNum += int.Parse(SplitStr[1]);
                string[] FanStrs = SplitStr[2].Split(new Char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < FanStrs.Length; j++)
                {
                    int found = FanStrs[j].IndexOf('*');
                    int repeat = int.Parse(FanStrs[j].Substring(found + 1));
                    FanStrs[j] = FanStrs[j].Substring(0, found);
                    for (int k = 0; k < fanStatistics.Length; k++)
                        if (FanStrs[j] == Mahjong.fan_name[k])
                            for (int r = 0; r < repeat; r++)
                                fanStatistics[k]++;
                }
            }

            double sum = 0;
            Console.Clear();
            String Str1 = "盘数：" + panNum + "\n";
            for (int i = 17; i < fanStatistics.Length; i++)
            {
                Str1 += (i - 16).ToString().PadLeft(2) + ":  ";
                for (int j = 0; j < 5 - Mahjong.fan_name[i].Length; j++) Str1 += "  ";

                Str1 += Mahjong.fan_name[i] + "  ";//一色三同顺
                Str1 += fanStatistics[i].ToString().PadLeft(7) + "*";//次数
                double thisFan = fanStatistics[i] * Mahjong.fan_value_table[i];
                Str1 += Mahjong.fan_value_table[i].ToString().PadRight(2) + "= " + thisFan.ToString().PadRight(7);
                Str1 += (100 * thisFan / FanNum).ToString("F3").PadLeft(9) + "%  \n";
                sum += (100 * thisFan / FanNum);
            }
            Console.WriteLine(Str1);
        }
        // 用JCAI格式数据文件，统计胡牌时番种类型
        static void StatisticsJCAI_HuTime(String DataFile)
        {
            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);
            int HuPanNum = 0, HuangPanNum = 0, ZiMoPanNum = 0, DianPaoPanNum = 0, numZiMo = 0, numDianPao = 0;
            double numRChuang = 0;
            double lenAllResidualCard = 0, scoreZiMo = 0, scoreDianPao = 0;
            for (int i = 0; i < Lines.Length; i++)//endLine
            {
                string[] SplitStr = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (String.IsNullOrEmpty(Lines[i]))
                    continue;
                else if (SplitStr.Length > 0 && SplitStr[0] == "Huang")
                {
                    CollectInfoFromData(DataFile, i, i);
                    HuangPanNum++;
                    numRChuang += Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);
                    int numResidualCard = Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);
                }
                else if (SplitStr.Length > 3 && SplitStr[2] == "Hu")
                {
                    CollectInfoFromData(DataFile, i, i);
                    HuPanNum++;
                    int[] abc = Mahjong.AllBottomCard;
                    int numResidualCard = Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);
                    lenAllResidualCard += numResidualCard;
                    int[] ss = new int[4];

                    string[] NextStrs = Lines[i + 2].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                    if (NextStrs.Length > 0 && NextStrs[0] == "Score")
                    {
                        for (int j = 0; j < 4; j++)
                            ss[j] = int.Parse(NextStrs[j + 1]);
                        Array.Sort(ss);
                        if (ss[1] < -8)
                        { scoreZiMo += ss[3]; numZiMo++; }
                        else if (ss[1] == -8)
                        { scoreDianPao += ss[3]; numDianPao++; }
                    }
                    else
                    { }
                }

            }
            Console.WriteLine("胡牌盘数:" + HuPanNum + "  黄牌盘数:" + HuangPanNum + "  余牌总数:"
                + lenAllResidualCard + "  总盘数:" + (HuPanNum + HuangPanNum));
            Console.WriteLine("胡牌时平均剩余长度:" + (lenAllResidualCard / HuPanNum).ToString("F2"));
            Console.WriteLine("自摸次数:" + numZiMo + "  点炮次数:" + numDianPao +
                " 自摸平均得分:" + (scoreZiMo / numZiMo).ToString("F2") + "  点炮平均得分:" + (scoreDianPao / numDianPao).ToString("F2"));
            Console.WriteLine("黄牌盘数:" + HuangPanNum + "  黄牌余牌总数:"
                + numRChuang + "  平均余数:" + (numRChuang / HuangPanNum).ToString("F2"));
        }

        //决策函数
        //Class 0:data里拿数据, 1:json里拿数据, 2:比赛
        static String decision(int playerID, int Step, out String outStr, out String outFanDebugInfo, int Class) // 请保证文件中只有一个类定义了Main方法
        {
            Mahjong.TimeStart = DateTime.Now;
            outStr = outFanDebugInfo = "";
            int inCard = Mahjong.in_card;
            int outCard = Mahjong.out_card;
            string[] requestsStrs, responsesStrs; String responStr;
            Mahjong.in_card = Mahjong.out_card = 0;
            Array.Clear(Mahjong.RetCPG, 0, Mahjong.RetCPG.Length);
            for (int i = 0; i < Mahjong.EvalCPGHs.Length; i++)   Mahjong.EvalCPGHs[i] = null;

            if (Class >= 1)// 1:json里拿数据, 2:比赛
            {
                requestsStrs = input.requests[Step].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                responStr = input.responses[Step];
                responsesStrs = input.responses[Step].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            }
            else// 0:data里拿数据， -1:data里拿数据,但不作判别
            {
                requestsStrs = Lines[Step].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                int loc = Lines[Step + 1].IndexOf("Ignore");
                if (loc > 0) responStr = Lines[Step + 1].Substring(0, loc).Trim();
                else responStr = Lines[Step + 1];
                if (requestsStrs[2] == "Play" && responStr.Contains("Draw"))
                    responStr = "PASS";
                responsesStrs = responStr.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            }

            int[] tmp14 = new int[14];
            Array.Sort(mj[playerID].HandInCard);
            mj[playerID].HandInCard.CopyTo(tmp14, 0);
            String returnStr = "PASS"; OCPG_String = "";
            //自己摸的牌
            if (Step == 102)
            { }
            if (Class <= 0 && requestsStrs[2] == "Draw" && playerID == int.Parse(requestsStrs[1]) || requestsStrs[0] == "2")
            {
                if (Class >= 1)
                    Mahjong.in_card = tmp14[13] = Str2Card(requestsStrs[1]);
                else
                    Mahjong.in_card = tmp14[13] = Str2Card(requestsStrs[3]);
                Array.Sort(tmp14);
                mj[playerID].AdjusHandCard(tmp14);
                mj[playerID].AdjustHandShunKe(tmp14);
                mj[playerID].AdjustWinFlag(Mahjong.in_card, playerID);
                int Score = 0;
                if (Mahjong.If_NormHu_Cards(tmp14) > 0 || mj[playerID].If_Hu_SpecialCards())
                {
                    Score = mj[playerID].AdjustHandShunKe_ComputerScore(tmp14);
                    outStr += "###zmHu   ";
                    outFanDebugInfo = mj[playerID].RetFanT();
                    if (Score >= Mahjong.JiBenHuFen)
                        returnStr = (Class >= 1 ? "HU" : "Player " + requestsStrs[1] + " Hu " + requestsStrs[3]);
                    else
                        outStr = "自摸番数" + Score + ",不能胡!  ";
                }

                if (returnStr == "PASS" && Can_Gang_FetchedCOM(playerID) && Mahjong.myBCP[playerID] < 21)
                {
                    if (mj[playerID].If_Must_Gang(out OCPG_String)[0] > 0)
                    {
                        outStr = OCPG_String;
                        int GangCard = 0;
                        int[] hand13 = new int[mj[playerID].HandInCard.Length];
                        mj[playerID].HandInCard.CopyTo(hand13, 0);//备份HandInCard
                        if (mj[playerID].can_bagang || mj[playerID].can_houbagang)
                            returnStr = (Class >= 1 ? "BUGANG " : "Player " + requestsStrs[1] + " BuGang ") + Card2Str(GangCard = mj[playerID].BaGang_Cards(Mahjong.in_card));
                        else if (mj[playerID].can_angang)
                            returnStr = (Class >= 1 ? "GANG " : "Player " + requestsStrs[1] + " AnGang ") + Card2Str(GangCard = mj[playerID].AnGang_Cards());
                        hand13.CopyTo(mj[playerID].HandInCard, 0);//恢复HandInCard
                        if (responStr != returnStr)
                            outStr += "不杠?";
                    }
                    else
                    {
                        outStr += "可杠,不杠";
                        if (responsesStrs[0].IndexOf("GANG") == -1)
                            outStr += ",但他杠了";
                    }
                    outFanDebugInfo = outStr;
                    outStr += "###abGang ";
                }

                if (returnStr == "PASS")
                {
                    int outC = 0;
                    double[] Weight = new double[14];
                    outC = Choice_OneCard_Out(playerID, out Weight, out OCPG_String);
                    returnStr = (Class >= 1 ? "PLAY " : "Player " + requestsStrs[1] + " Play ") + Card2Str(outC);
                    outFanDebugInfo = OCPG_String;
                    if (Class <= 1)
                    {
                        if (Class >= 1 && responsesStrs.Length > 1)//220205 
                            outC = Str2Card(responsesStrs[1]);
                        if (Class == 0 && responsesStrs.Length > 3)
                            outC = Str2Card(responsesStrs[3]);
                        if (responStr == returnStr || Class == -1)
                        { }
                        else
                            outStr += JudgeOutCard(playerID, outC, Step, Weight);//都吃，但打牌不一样 + "  ###Out    "
                        OCPG_String = outStr + "\n" + OCPG_String;
                    }
                }
            }
            else if (playerID != int.Parse(requestsStrs[1]) && (Class <= 0 && requestsStrs[2] == "Play" || requestsStrs[0] == "3" && (requestsStrs[2] == "PLAY" || requestsStrs[2] == "PENG" || requestsStrs[2] == "CHI"))) //打出的牌
            {
                int OldHa = int.Parse(requestsStrs[1]);
                mj[playerID].can_bump = mj[playerID].can_chi = false;
                Mahjong.out_card = Str2Card(requestsStrs[requestsStrs.Length - 1]);
                outStr = ""; string retMapStr = "";
                returnStr = retMapStr = HowHGPC(playerID, OldHa, out OCPG_String, Class);
                outFanDebugInfo = mj[playerID].RetFanT();

            }
            else if (Class == 0 && requestsStrs[2] == "BuGang" || requestsStrs[0] == "3" && requestsStrs[2] == "BUGANG")
            {
                mj[Mahjong.old_azimuth].ganged = true;
                Mahjong.out_card = Str2Card(requestsStrs[3]);
                tmp14[13] = Mahjong.out_card;
                Array.Sort(tmp14);
                mj[playerID].AdjusHandCard(tmp14);
                mj[playerID].AdjustHandShunKe(tmp14);
                mj[playerID].AdjustWinFlag(Mahjong.out_card, playerID);
                mj[playerID].FanCardData.wfABOUT_KONG = 1;
                if (mj[playerID].If_Hu_Cards(Mahjong.out_card) > 0 || mj[playerID].If_Hu_SpecialCards())
                {
                    int Score = mj[playerID].AdjustHandShunKe_ComputerScore(tmp14);
                    outStr += "###qGang    ";
                    if (Score >= Mahjong.JiBenHuFen)
                        returnStr = Class >= 1 ? "HU" : "Player " + playerID + " Hu " + requestsStrs[3];
                    outFanDebugInfo = mj[playerID].RetFanT();
                }
            }


            if (outStr.Length > 1)//等于1表示返回*号,以示不同,0为相同
            {
                String str1 = "";
                //查看剩余牌
                str1 = "KRC=";
                for (int j = 1; j < 31; j++)
                {
                    if (j % 10 == 0)
                        str1 = str1.Substring(0, str1.Length - 1) + "#";
                    else
                        str1 += mj[playerID].KnownRemainCard[j].ToString().PadRight(2);
                }
                for (int j = 31; j < 44; j++)
                {
                    if (j == 38)
                        str1 = str1.Substring(0, str1.Length - 1) + "#";
                    else if (j % 2 == 0)
                        continue;
                    else
                        str1 += mj[playerID].KnownRemainCard[j].ToString().PadRight(2);
                }
                str1 += " 底牌剩" + (21 - Mahjong.myBCP[playerID]) + "张";
                if (outStr.Contains("权重") && outStr.Length < 10)//29权重99
                { }
                else
                {
                    outStr = str1 + "\n" + outStr;
                    str1 = "";
                    //查看做牌方向
                    mj[playerID].HandInCard.CopyTo(tmp14, 1);
                    tmp14[0] = Mahjong.in_card;
                    //if (outStr.IndexOf("###") < 0) str1 = mj[playerID].CreateAllFanTabInfo(tmp14);
                    //outStr += "\n" + str1;
                }
            }
            Mahjong.in_card = inCard;
            Mahjong.out_card = outCard;
            double tt = (DateTime.Now - Mahjong.TimeStart).TotalSeconds;
            OCPG_String += "Time=" + tt.ToString("f2") + "s    Line=";//
            if (tt > 3 && !bCompitition)
            {
                int level = OCPG_String.IndexOf("L");//取出搜索深度+ Mahjong.RetCPG(Str2Card(reStr.Substring(14)))
                level = OCPG_String[level + 1] - '0';
                writeMjInfoDATA(playerID, Lines, Step, Lines[Step + 1]);
                Console.WriteLine(returnStr.Replace("Player ", "").PadRight(10) + "|T=" + tt.ToString("f2") + "s L=" + level);
                Debug.WriteLine(OCPG_String + "\n");
            }
            outStr = OCPG_String;
            return returnStr.Trim();
        }

        static void CollectInfoJson(dynamic input, int Step = -1)
        {
            if (Step == -1)
                Step = input.requests.Length - 1;
            if (Step < 2) return;
            Mahjong.out_card = Mahjong.in_card = 0;
            string[] lastStr;
            string[] SplitStr = input.requests[0].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            String PlayerID = SplitStr[1];
            int ha = int.Parse(PlayerID);
            for (int i = 0; i < 4; i++)
            {
                mj[i].InitData();
                mj[i].FanCardData.quan_wind = (wind_t)(31 + 2 * int.Parse(SplitStr[2]));
            }

            int[] pKRC = mj[ha].KnownRemainCard;
            SplitStr = input.requests[1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < 4; i++)
                if (i == ha)
                    for (int j = 0; j < 13; j++)
                    {
                        mj[i].HandInCard[j] = Str2Card(SplitStr[j + 5]);
                        pKRC[Str2Card(SplitStr[j + 5])]--;
                    }
                else
                    for (int j = 0; j < 13; j++)
                        mj[i].HandInCard[j] = 44;
            Array.Sort(mj[ha].HandInCard);

            Array.Clear(Mahjong.Aband_Card, 0, Mahjong.Aband_Card.Length);
            Array.Clear(Mahjong.AllPutOutCard, 0, Mahjong.AllPutOutCard.Length);
            Mahjong.myBCP = new int[4];//2023.10.16
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 136 / 4 - 13; j++)
                    Mahjong.AllBottomCard[i * 34 + j] = 44;

            int card = 0, lastHa = 0, lastOutCard = 0;
            for (int i = 2; i <= Step; i++)
            {
                card = lastHa = lastOutCard = -1;
                if (i == Step - 1)
                { }
                SplitStr = input.requests[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                lastStr = input.requests[i - 1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);

                if (SplitStr[0] == "2")
                    ha = int.Parse(PlayerID);
                if (SplitStr[0] == "3")
                    ha = int.Parse(SplitStr[1]);

                if (lastStr[0] == "3")
                {
                    lastHa = int.Parse(lastStr[1]);
                    if (lastStr[2] == "PLAY" || lastStr[2] == "PENG" || lastStr[2] == "CHI")
                        lastOutCard = Str2Card(lastStr[lastStr.Length - 1]);
                }

                //处理打出的牌Aband_Card
                if (SplitStr[0] == "3" && (SplitStr[2] == "PLAY" || SplitStr[2] == "CHI" || SplitStr[2] == "PENG"))
                {
                    Mahjong.out_card = Str2Card(SplitStr[SplitStr.Length - 1]);
                    InsertNum2Array(Mahjong.Aband_Card, Mahjong.out_card);
                    InsertNum2Array(Mahjong.AllPutOutCard, Mahjong.out_card);
                    if (SplitStr[2] == "PENG" || SplitStr[2] == "CHI")
                        AarryMinusNum(Mahjong.Aband_Card, lastOutCard);
                }
                else if (SplitStr[0] == "3" && SplitStr[2] == "GANG" &&
                    lastStr.Length > 2 && (lastStr[2] == "PLAY" || lastStr[2] == "CHI" || lastStr[2] == "PENG"))
                    AarryMinusNum(Mahjong.Aband_Card, lastOutCard);

                //处理山牌AllBottomCard, 摸牌后Mahjong.out_card = 0
                if (SplitStr[0] == "2" || SplitStr[0] == "3" && SplitStr[2] == "DRAW")
                    Mahjong.out_card = Mahjong.AllBottomCard[ha * 34 + Mahjong.myBCP[ha]++] = 0;

                if (SplitStr[0] == "2")
                {
                    Mahjong.in_card = Str2Card(SplitStr[1]);
                    pKRC[Str2Card(SplitStr[1])]--;
                }
                else if (SplitStr[0] == "3" && SplitStr[1] == PlayerID)
                {
                    if (SplitStr[2] == "PLAY" || SplitStr[2] == "CHI" || SplitStr[2] == "PENG")
                        Mahjong.out_card = Str2Card(SplitStr[SplitStr.Length - 1]);

                    mj[Mahjong.old_azimuth].ganged = false;
                    Mahjong.old_azimuth = ha;

                    if (SplitStr[2] == "PLAY")
                    {
                        if (Mahjong.out_card != Mahjong.in_card)
                        {
                            AarryMinusNum(mj[ha].HandInCard, Mahjong.out_card);
                            InsertNum2Array(mj[ha].HandInCard, Mahjong.in_card);
                            Array.Sort(mj[ha].HandInCard);
                        } 
                        Mahjong.in_card = 0;
                    }
                    else if (SplitStr[2] == "CHI")
                    {
                        AarryMinusNum(mj[ha].HandInCard, Mahjong.out_card);//处理手牌
                        int midCard = 0, inC = 0;
                        midCard = Str2Card(SplitStr[3]);
                        SplitStr = input.requests[i - 1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                        if (SplitStr[2] == "PLAY" || SplitStr[2] == "CHI" || SplitStr[2] == "PENG")
                            inC = Str2Card(SplitStr[SplitStr.Length - 1]);
                        for (int j = midCard - 1; j <= midCard + 1; j++)
                            if (j != inC)
                                AarryMinusNum(mj[ha].HandInCard, j);
                        Array.Sort(mj[ha].HandInCard);
                        InsertNum2Array(mj[ha].FanCardData.ArrMshun, midCard);//处理吃牌  
                    }
                    else if (SplitStr[2] == "PENG")
                    {
                        AarryMinusNum(mj[ha].HandInCard, Mahjong.out_card);//处理手牌 
                        AarryMinusNum(mj[ha].HandInCard, lastOutCard);
                        AarryMinusNum(mj[ha].HandInCard, lastOutCard);
                        InsertNum2Array(mj[ha].FanCardData.ArrMke, lastOutCard);//处理碰牌 
                        Array.Sort(mj[ha].HandInCard); 
                    }
                    else if (SplitStr[2] == "BUGANG")
                    {
                        card = Str2Card(SplitStr[3]);//处理手牌
                        if (card != Mahjong.in_card)
                        {
                            AarryMinusNum(mj[ha].HandInCard, card);
                            InsertNum2Array(mj[ha].HandInCard, Mahjong.in_card);
                            Array.Sort(mj[ha].HandInCard);
                        }
                        AarryMinusNum(mj[ha].FanCardData.ArrMke, card);//处理碰、补杠
                        InsertNum2Array(mj[ha].FanCardData.ArrMgang, card);
                        Mahjong.out_card = Mahjong.in_card = 0;
                    }
                    else if (SplitStr[2] == "GANG")//暗杠  明杠
                    {
                        mj[ha].ganged = true;
                        //处理手牌 
                        if (lastStr[0] == "2")
                        {
                            int retCard = mj[ha].AnGang_Cards();//处理暗杠牌HandInCard
                            InsertNum2Array(mj[ha].FanCardData.ArrAgang, retCard);
                        }
                        else if (lastStr.Length > 2 && (lastStr[2] == "PLAY" || lastStr[2] == "CHI" || lastStr[2] == "PENG"))//处理明杠牌
                        {
                            int retCard = mj[ha].MingGang_Cards(lastOutCard);
                            InsertNum2Array(mj[ha].FanCardData.ArrMgang, lastOutCard);
                        }
                        Array.Sort(mj[ha].HandInCard);
                        Mahjong.out_card = Mahjong.in_card = 0;
                    }
                }
                else if (SplitStr[0] == "3" && SplitStr[1] != PlayerID)
                {
                    //其他家的吃、碰及打的牌 
                    if (SplitStr[2] == "PLAY" || SplitStr[2] == "CHI" || SplitStr[2] == "PENG")
                    {
                        mj[Mahjong.old_azimuth].ganged = false;
                        Mahjong.old_azimuth = ha;

                        Mahjong.out_card = Str2Card(SplitStr[SplitStr.Length - 1]);
                        pKRC[Mahjong.out_card]--;
                        Mahjong.in_card = 0;
                    }

                    if (SplitStr[2] == "DRAW")
                        Mahjong.in_card = 44;
                    else if (SplitStr[2] == "CHI")
                    {
                        int midCard = Str2Card(SplitStr[3]);
                        InsertNum2Array(mj[ha].FanCardData.ArrMshun, midCard);
                        for (int j = midCard - 1; j <= midCard + 1; j++)
                            if (j != lastOutCard)
                                pKRC[j]--;
                        for (int j = 0; j < 3; j++)
                            AarryMinusNum(mj[ha].HandInCard, 44);
                    }
                    else if (SplitStr[2] == "PENG")
                    {
                        InsertNum2Array(mj[ha].FanCardData.ArrMke, lastOutCard);
                        pKRC[lastOutCard] -= 2;
                        for (int j = 0; j < 3; j++)
                            AarryMinusNum(mj[ha].HandInCard, 44);
                    }
                    else if (SplitStr[2] == "BUGANG")
                    {
                        mj[ha].ganged = true;
                        card = Str2Card(SplitStr[3]);
                        pKRC[card]--;
                        InsertNum2Array(mj[ha].FanCardData.ArrMgang, card);
                        AarryMinusNum(mj[ha].FanCardData.ArrMke, card);
                        Mahjong.in_card = 0;
                    }
                    else if (SplitStr[2] == "GANG")
                    {
                        mj[ha].ganged = true;
                        if (lastStr[2] == "DRAW")
                            InsertNum2Array(mj[ha].FanCardData.ArrMgang, 44); //不知道他家暗杠的牌，set:44
                        else if (lastStr.Length > 2 && (lastStr[2] == "PLAY" || lastStr[2] == "CHI" || lastStr[2] == "PENG"))
                        {
                            pKRC[lastOutCard] -= 3;
                            InsertNum2Array(mj[ha].FanCardData.ArrMgang, lastOutCard);
                        }
                        for (int j = 0; j < 3; j++)
                            AarryMinusNum(mj[ha].HandInCard, 44);
                        Mahjong.in_card = 0;
                    }
                }
                ChecKrcHandin(int.Parse(PlayerID), input.requests[i]);
                if (Mahjong.in_card * Mahjong.out_card > 0)
                { }
            }

        }

        static void CollectInfoFromData(String DataFile, int startLine, int endLine)
        {
            string[] SplitStr;
            if (Lines == null || Lines.Length == 0)//dataJCAI  sample  is null
                Lines = File.ReadAllLines(DataFile);

            for (int i = endLine; i >= 0; i--)
            {
                SplitStr = Lines[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (SplitStr.Length > 0 && SplitStr[0] == "Match")
                { startLine = i; break; }
            }


            int ha = 0, card = 0;
            int[] yb14 = new int[14];
            for (int k = startLine; k <= endLine; k++)
            {
                SplitStr = Lines[k].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (SplitStr == null || SplitStr.Length == 0) continue;
                else if (SplitStr[0] == "Match")
                {
                    Array.Clear(Mahjong.Aband_Card, 0, Mahjong.Aband_Card.Length);
                    Mahjong.out_card = Mahjong.in_card = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        mj[i] = new Mahjong(ch[i]);
                        mj[i].InitData();
                    }
                    for (int h = 0; h < 4; h++)
                        for (int j = 1; j < mj[h].KnownRemainCard.Length - 1; j++)
                            if (j % 10 == 0 || j > 30 && j < mj[h].KnownRemainCard.Length && j % 2 == 0)
                                mj[h].KnownRemainCard[j] = 0;
                            else
                                mj[h].KnownRemainCard[j] = 4;
                    Mahjong.myBCP = new int[4];
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 136 / 4 - 13; j++)
                            Mahjong.AllBottomCard[i * 34 + j] = 44;
                    continue;
                }
                if (SplitStr.Length >= 2 && SplitStr[0] == "Wind")
                {
                    for (int j = 0; j < 4; j++)
                        mj[j].FanCardData.quan_wind = (wind_t)(31 + 2 * int.Parse(SplitStr[1]));
                    continue;
                }

                if (SplitStr.Length >= 4 && SplitStr[0] == "Player")
                { ha = int.Parse(SplitStr[1]); card = Str2Card(SplitStr[3]); }

                //处理打出的牌Aband_Card
                if (SplitStr.Length >= 4 && SplitStr[2] == "Play")
                {

                    InsertNum2Array(Mahjong.AbandCard4P[ha], card);
                    InsertNum2Array(Mahjong.Aband_Card, card);
                    InsertNum2Array(Mahjong.AllPutOutCard, card);
                }
                else if (SplitStr.Length >= 4 && (SplitStr[2] == "Gang" || SplitStr[2] == "Chi" || SplitStr[2] == "Peng"))
                    AarryMinusNum(Mahjong.Aband_Card, Mahjong.out_card);


                if (SplitStr.Length >= 4 && SplitStr[2] == "DRAW")
                {
                    Mahjong.out_card = Mahjong.AllBottomCard[ha * 34 + Mahjong.myBCP[ha]++] = 0;
                    Mahjong.in_card = Str2Card(SplitStr[3]);
                    mj[ha].KnownRemainCard[Mahjong.in_card]--;
                }
                else if (SplitStr[0] == "Huang" || SplitStr[0] == "Score" || SplitStr[0] == "Match") continue;
                else if (SplitStr.Length < 3) continue;
                else if (SplitStr[2] == "Deal")
                {
                    ha = int.Parse(SplitStr[1]);
                    for (int j = 0; j < mj[ha].HandInCard.Length; j++)
                    {
                        card = mj[ha].HandInCard[j] = Str2Card(SplitStr[3 + j]);
                        mj[ha].KnownRemainCard[card]--;
                    }
                    Array.Sort(mj[ha].HandInCard); continue;
                }
                else if (SplitStr[2] == "Draw")
                {
                    Mahjong.in_card = card;
                    Mahjong.out_card = 0;
                    Mahjong.AllBottomCard[ha * 34 + Mahjong.myBCP[ha]++] = 0;
                    mj[ha].KnownRemainCard[card]--;
                }
                else if (SplitStr[2] == "Play")
                {
                    mj[Mahjong.old_azimuth].ganged = false;
                    Mahjong.old_azimuth = ha;

                    Mahjong.out_card = card;
                    if (card != Mahjong.in_card)
                    {
                        AarryMinusNum(mj[ha].HandInCard, card);//处理手牌 
                        InsertNum2Array(mj[ha].HandInCard, Mahjong.in_card);
                    }
                    Mahjong.in_card = 0;
                    for (int i = 0; i < 4; i++)
                        if (i != ha)
                            mj[i].KnownRemainCard[card]--;
                }
                else if (SplitStr[2] == "Peng")
                {
                    AarryMinusNum(mj[ha].HandInCard, card);//处理手牌 
                    AarryMinusNum(mj[ha].HandInCard, card);
                    InsertNum2Array(mj[ha].FanCardData.ArrMke, card);//处理碰牌  
                    Mahjong.out_card = Mahjong.in_card = 0;
                    for (int i = 0; i < 4; i++)
                        if (i != ha)
                            for (int j = 0; j < 2; j++)
                                mj[i].KnownRemainCard[card]--;
                }
                else if (SplitStr[2] == "Chi")
                {
                    int[] ChiCards = new int[3] { card - 1, card, card + 1 };
                    for (int j = 0; j < 3; j++)
                        if (ChiCards[j] == Mahjong.out_card)
                            ChiCards[j] = 0;

                    for (int i = 0; i < 3; i++)//处理手牌
                        if (ChiCards[i] > 0)
                            AarryMinusNum(mj[ha].HandInCard, ChiCards[i]);
                    InsertNum2Array(mj[ha].FanCardData.ArrMshun, card);
                    Mahjong.out_card = Mahjong.in_card = 0;
                    for (int i = 0; i < 4; i++)
                        if (i != ha)
                            for (int j = 0; j < 3; j++)
                                if (ChiCards[j] > 0)
                                    mj[i].KnownRemainCard[ChiCards[j]]--;
                }
                else if (SplitStr[2] == "AnGang")
                {
                    mj[ha].ganged = true;
                    int retCard = mj[ha].AnGang_Cards(card);//处理暗杠牌HandInCard
                    InsertNum2Array(mj[ha].FanCardData.ArrAgang, retCard);
                    for (int i = 0; i < 4; i++)
                        if (i != ha)
                            for (int j = 0; j < 4; j++)
                                mj[i].KnownRemainCard[card]--;
                    //暗杠之后别家知道吗?
                }
                else if (SplitStr[2] == "Gang")//明杠
                {
                    mj[ha].ganged = true;
                    int retCard = mj[ha].MingGang_Cards(card);
                    InsertNum2Array(mj[ha].FanCardData.ArrMgang, card);
                    Mahjong.out_card = Mahjong.in_card = 0;
                    for (int i = 0; i < 4; i++)
                        if (i != ha)
                            for (int j = 0; j < 3; j++)
                                mj[i].KnownRemainCard[card]--;
                }
                else if (SplitStr[2] == "BuGang")
                {
                    if (k == 64270)//点炮 
                    { }
                    mj[ha].ganged = true;
                    if (card != Mahjong.in_card)
                    {
                        AarryMinusNum(mj[ha].HandInCard, card);
                        InsertNum2Array(mj[ha].HandInCard, Mahjong.in_card);
                    }
                    AarryMinusNum(mj[ha].FanCardData.ArrMke, card);//处理碰、补杠
                    InsertNum2Array(mj[ha].FanCardData.ArrMgang, card);
                    Mahjong.out_card = Mahjong.in_card = 0;
                    for (int i = 0; i < 4; i++)
                        if (i != ha)
                            mj[i].KnownRemainCard[card]--;
                }
                else if (SplitStr[2] == "Hu")
                {
                    if (Mahjong.out_card > 0)//点炮 
                    { }
                    else if (Mahjong.in_card > 0)//自摸
                    { }
                }
                Array.Sort(mj[ha].HandInCard);
                ChecKrcHandin(ha);
            }
            for (int i = 0; i < 4; i++)
            {
                int[] BC4P = Mahjong.BumpCard4P[i];
                Array.Clear(BC4P, 0, BC4P.Length);
                for (int j = 0; j < 4; j++)
                    if (mj[i].FanCardData.ArrMgang[j] > 0)
                        for (int k = 0; k < 4; k++)
                            InsertNum2Array(BC4P, mj[i].FanCardData.ArrMgang[j]);
                for (int j = 0; j < 4; j++)
                    if (mj[i].FanCardData.ArrAgang[j] > 0)
                        for (int k = 0; k < 4; k++)
                            InsertNum2Array(BC4P, mj[i].FanCardData.ArrAgang[j]);
                for (int j = 0; j < 4; j++)
                    if (mj[i].FanCardData.ArrMke[j] > 0)
                        for (int k = 0; k < 3; k++)
                            InsertNum2Array(BC4P, mj[i].FanCardData.ArrMke[j]);
                for (int j = 0; j < 4; j++)
                    if (mj[i].FanCardData.ArrMshun[j] > 0)
                    {
                        InsertNum2Array(BC4P, mj[i].FanCardData.ArrMshun[j] - 1);
                        InsertNum2Array(BC4P, mj[i].FanCardData.ArrMshun[j]);
                        InsertNum2Array(BC4P, mj[i].FanCardData.ArrMshun[j] + 1);
                    }
            }
        }

        static string MjCompInfo(int ha, dynamic input, int Step, bool bComp = false)
        {
            string[] SplitStr = input.requests[Step].Split(null as char[],
                StringSplitOptions.RemoveEmptyEntries);
            String printStr = "";

            printStr = Step.ToString().PadRight(3) + ":ha=";
            if (input.requests[Step].Length <= 4)
                printStr += input.requests[0][2];
            else
                printStr += input.requests[Step][2];

            if (SplitStr.Length <= 2 && SplitStr[0] == "2")
                printStr += "|I=" + Mahjong.in_card.ToString().PadRight(3) + "|";
            else if (SplitStr[2] == "PLAY" || SplitStr[2] == "PENG" || SplitStr[2] == "CHI")
                printStr += "|O=" + Mahjong.out_card.ToString().PadRight(3) + "|";
            else
                printStr += "|".PadRight(6) + "|";

            //打印手牌
            for (int i = 0; i < mj[ha].HandInCard.Length; i++)
                if (mj[ha].HandInCard[i] > 0)
                    printStr += mj[ha].HandInCard[i].ToString().PadRight(3);
                else
                    printStr += "   ";
            printStr += "|";

            //打印吃、碰、杠牌
            string str9 = "";
            for (int i = 0; i < mj[ha].FanCardData.ArrMke.Length; i++)
            {
                if (mj[ha].FanCardData.ArrMke[i] > 0)
                    str9 += "P" + mj[ha].FanCardData.ArrMke[i].ToString().PadRight(3);
                if (mj[ha].FanCardData.ArrMshun[i] > 0)
                    str9 += "C" + mj[ha].FanCardData.ArrMshun[i].ToString().PadRight(3);
                if (mj[ha].FanCardData.ArrAgang[i] > 0)
                    str9 += "A" + mj[ha].FanCardData.ArrAgang[i].ToString().PadRight(3);
                if (mj[ha].FanCardData.ArrMgang[i] > 0)
                    str9 += "M" + mj[ha].FanCardData.ArrMgang[i].ToString().PadRight(3);
            }
            printStr += str9.PadRight(8) + "|";
            if (bComp)
            {
                String str1 = "KRC= ";//查看剩余牌 
                for (int j = 1; j < 31; j++)
                {
                    if (j % 10 == 0)
                        str1 = str1.Substring(0, str1.Length - 1) + "#";
                    else
                        str1 += mj[ha].KnownRemainCard[j].ToString().PadRight(2);
                }
                for (int j = 31; j < 44; j++)
                {
                    if (j == 38)
                        str1 += "#".PadRight(2);
                    else if (j % 2 == 0)
                        continue;
                    else
                        str1 += mj[ha].KnownRemainCard[j].ToString().PadRight(2);
                }
                printStr += "底牌剩" + (21 - Mahjong.myBCP[ha]) + "张";
                printStr += "\n" + str1;
            }
            else
                printStr += input.requests[Step].PadRight(13) + "→" + input.responses[Step].PadRight(10) + "|";

            return printStr;
        }

        static void writeMjInfoDATA(int ha, string[] input, int Step, string response)
        {
            string[] SplitStr = input[Step].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            String printStr = "";

            printStr = Step.ToString().PadRight(5) + ":ha=";
            if (SplitStr[0] == "Player")
                printStr += ha;

            if (SplitStr.Length == 4 && SplitStr[2] == "Draw")
                printStr += "|I=" + Mahjong.in_card.ToString().PadRight(3) + "|";
            else if (SplitStr.Length == 4 && SplitStr[2] == "Play")
                printStr += "|O=" + Mahjong.out_card.ToString().PadRight(3) + "|";
            else
                printStr += "|".PadRight(6) + "|";

            //打印手牌
            for (int i = 0; i < mj[ha].HandInCard.Length; i++)
                if (mj[ha].HandInCard[i] > 0)
                    printStr += mj[ha].HandInCard[i].ToString().PadRight(3);
                else
                    printStr += "   ";
            printStr += "|";

            //打印吃、碰、杠牌
            string str9 = "";
            for (int i = 0; i < mj[ha].FanCardData.ArrMke.Length; i++)
            {
                if (mj[ha].FanCardData.ArrMke[i] > 0)
                    str9 += "P" + mj[ha].FanCardData.ArrMke[i].ToString().PadRight(3);
                if (mj[ha].FanCardData.ArrMshun[i] > 0)
                    str9 += "C" + mj[ha].FanCardData.ArrMshun[i].ToString().PadRight(3);
                if (mj[ha].FanCardData.ArrAgang[i] > 0)
                    str9 += "G" + mj[ha].FanCardData.ArrAgang[i].ToString().PadRight(3);
                if (mj[ha].FanCardData.ArrMgang[i] > 0)
                    str9 += "G" + mj[ha].FanCardData.ArrMgang[i].ToString().PadRight(3);
            }

            printStr += str9.PadRight(12) + "|";
            printStr += input[Step].PadRight(17) + "→";
            if (response.IndexOf("Ignore") > 0)
                response = response.Substring(0, response.IndexOf("Ignore") + 7);
            printStr += response.PadRight(17) + "|";
            printStr = printStr.Replace("Player ", "");
            Console.Write(printStr);
        }

        static String FormatHTMLOut(String inStr, int width)
        {
            String outStr = inStr.Replace("\n", "~");
            string[] SplitStr = outStr.Split(new Char[] { '~' }, StringSplitOptions.RemoveEmptyEntries);
            SplitStr[0] = SplitStr[0].PadRight(width - 15);
            for (int i = 1; i < SplitStr.Length; i++)
                if (SplitStr[i].Length < width)
                {
                    int len = GetHanNumFromString(SplitStr[i]);
                    SplitStr[i] = SplitStr[i].PadRight(width - (int)(len * 0.9));
                }
            outStr = "";
            for (int i = 0; i < SplitStr.Length; i++)
                outStr += SplitStr[i];
            return outStr;
        }
        public static int GetHanNumFromString(string str)
        {
            int count = 0;
            Regex regex = new Regex(@"^[\u4E00-\u9FA5]{0,}$");
            for (int i = 0; i < str.Length; i++)
                if (regex.IsMatch(str[i].ToString()))
                    count++;
            return count;
        }
        static string getSelfResponse(int ha, string input)
        {
            string ret = "";
            if (input.Contains("Draw")) ret = "PASS";
            else
            {
                int loc = input.IndexOf("Player " + ha);
                if (loc >= 0)
                {
                    ret = input.Substring(loc);
                    loc = ret.IndexOf("Ignore");
                    if (loc >= 0)
                        ret = ret.Substring(0, loc).Trim();
                }
            }
            if (ret.Length == 0) ret = "PASS";
            return ret;
        }
        static void ChecKrcHandin(int ha, string str = "")
        {
            if (Mahjong.in_card * Mahjong.out_card > 0)
            { }
            for (int h = 0; h < 4; h++)
            {
                Array.Clear(mj[h].BumpCard, 0, mj[h].BumpCard.Length);
                for (int i = 0; i < mj[h].FanCardData.ArrMke.Length; i++)
                {
                    if (mj[h].FanCardData.ArrMke[i] > 0)
                        for (int j = 0; j < 3; j++)
                            InsertNum2Array(mj[h].BumpCard, mj[h].FanCardData.ArrMke[i]);
                    if (mj[h].FanCardData.ArrMgang[i] > 0)
                        for (int j = 0; j < 4; j++)
                            InsertNum2Array(mj[h].BumpCard, mj[h].FanCardData.ArrMgang[i]);
                    if (mj[h].FanCardData.ArrAgang[i] > 0)
                        for (int j = 0; j < 4; j++)
                            InsertNum2Array(mj[h].BumpCard, mj[h].FanCardData.ArrAgang[i]);
                    if (mj[h].FanCardData.ArrMshun[i] > 0)
                    {
                        InsertNum2Array(mj[h].BumpCard, mj[h].FanCardData.ArrMshun[i] - 1);
                        InsertNum2Array(mj[h].BumpCard, mj[h].FanCardData.ArrMshun[i]);
                        InsertNum2Array(mj[h].BumpCard, mj[h].FanCardData.ArrMshun[i] + 1);
                    }
                }
            }

            int[] krc = new int[mj[ha].KnownRemainCard.Length];
            mj[ha].KnownRemainCard.CopyTo(krc, 0);
            for (int h = 0; h < 4; h++)
                for (int j = 0; j < mj[h].BumpCard.Length; j++)
                    if (mj[h].BumpCard[j] > 0)
                        krc[mj[h].BumpCard[j]]++;
            for (int j = 0; j < mj[ha].HandInCard.Length; j++)
                if (mj[ha].HandInCard[j] > 0)
                    krc[mj[ha].HandInCard[j]]++;
            for (int j = 0; j < Mahjong.Aband_Card.Length; j++)
                if (Mahjong.Aband_Card[j] > 0)
                    krc[Mahjong.Aband_Card[j]]++;
            //if (Mahjong.out_card > 0)
            //    krc[Mahjong.out_card]++;
            if (Mahjong.in_card > 0)
                krc[Mahjong.in_card]++;
            for (int j = 0; j < krc.Length; j++)
                if (!(krc[j] == 0 || krc[j] == 4 || j == 44))
                { break; }
            for (int j = 0; j < krc.Length; j++)
                if (j % 10 == 0 || j > 30 && j < krc.Length && j % 2 == 0)
                    if (mj[ha].KnownRemainCard[j] != 0)
                    { break; }


            int cc = 0;
            for (int h = 0; h < 4; h++)
            {
                cc = 0;
                for (int i = 0; i < mj[h].FanCardData.ArrMke.Length; i++)
                {
                    if (mj[h].FanCardData.ArrMke[i] > 0) cc++;
                    if (mj[h].FanCardData.ArrMshun[i] > 0) cc++;
                    if (mj[h].FanCardData.ArrAgang[i] > 0) cc++;
                    if (mj[h].FanCardData.ArrMgang[i] > 0) cc++;
                }
                int lenBump = Mahjong.LongOfCardNZ(mj[h].BumpCard);
                for (int i = 0; i < mj[h].BumpCard.Length - 4; i++)
                    if (mj[h].BumpCard[i] > 0 && mj[h].BumpCard[i] == mj[h].BumpCard[i + 1] && mj[h].BumpCard[i] == mj[h].BumpCard[i + 3])
                        lenBump--;
                if (cc * 3 + Mahjong.LongOfCardNZ(mj[h].HandInCard) < 13
                    && cc * 3 + Mahjong.LongOfCardNZ(mj[h].HandInCard) > 14
                    )//|| lenBump / 3 != cc  
                { }
            }

            cc = Mahjong.LongOfCardNZ(Mahjong.Aband_Card) + Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);
            for (int h = 0; h < 4; h++)
                cc += Mahjong.LongOfCardNZ(mj[h].BumpCard) + Mahjong.LongOfCardNZ(mj[h].HandInCard);
            if (Mahjong.in_card > 0)
                cc++;
            if (cc != 136)
            { }

        }

        private static string HowHGPC(int ha, int OldHa, out string OutStrs, int Class)
        {
            string OutS = ""; OutStrs = "";
            double[] mHGPC = new double[8];

            mHGPC[7] = mj[ha].Can_Hu_OutCard(Mahjong.out_card);
            if (mHGPC[7] > 0)
                OutStrs = mj[ha].RetFanT();

            if (mHGPC[7] == 0)//没有胡牌
            {
                if (mj[ha].Can_MingGang(Mahjong.out_card))
                    mHGPC = mj[ha].If_Must_Gang(out OutS);
                if (mHGPC[3] == 0 //之前未算过碰牌时
                    && mj[ha].Can_Bump_Cards(Mahjong.out_card))
                    mHGPC = mj[ha].If_Must_Bump(out OutS);
                if (mHGPC[4] + mHGPC[5] + mHGPC[6] == 0//之前未算过吃牌时
                    && (ha - OldHa + 4) % 4 == 1
                    && mj[ha].Can_Chi_Cards(Mahjong.out_card))
                    mHGPC = mj[ha].If_Must_Chi(out OutS);
                OutStrs += OutS;
                for (int j = 0; j < 3; j++)
                    mj[ha].ChiFa[j] = mHGPC[j + 4];
            }

            string returnStr = "PASS";
            if (mHGPC[7] > 0)
                return Class >= 1 ? "HU" : "Player " + ha + " Hu " + Card2Str(Mahjong.out_card);
            mHGPC[0] = 0;
            if (Mahjong.IndexOfMaxInArray(mHGPC) == 2)
                return Class >= 1 ? "GANG" : "Player " + ha + " Gang " + Card2Str(Mahjong.out_card);

            if (Mahjong.IndexOfMaxInArray(mHGPC) == 3)
                returnStr = Class >= 1 ? "PENG " : "Player " + ha + " Peng " + Card2Str(Mahjong.out_card);
            if (Mahjong.IndexOfMaxInArray(mHGPC) == 1)
                return returnStr;
            int loc = Mahjong.IndexOfMaxInArray(mHGPC);
            if (loc >= 4)
                returnStr = (Class >= 1 ? "CHI " : "Player " + ha + " Chi ") + Card2Str(Mahjong.out_card + 5 - loc) + " ";

            if (Class >= 1 && !returnStr.Contains("PASS"))//比赛时，还要直接输出要打的牌
            {
                double[] Weight = new double[14];
                if (returnStr.Contains("PENG"))
                    mj[ha].FanCardData.ArrMke[3] = mj[ha].Bump_Cards(Mahjong.out_card);
                if (returnStr.Contains("CHI"))
                    mj[ha].FanCardData.ArrMshun[3] = mj[ha].Chi_Cards(Mahjong.out_card);
                int outC = Choice_OneCard_Out(ha, out Weight, out OutS);
                mj[ha].FanCardData.ArrMshun[3] = 0;
                OutStrs += "  打牌\n" + OutS;
                returnStr += Card2Str(outC);
            }
            return returnStr;
        }
        private static string priorityHGPC(string act1, string act2)
        {
            int p1 = 0, p2 = 0;

            if (act1.Contains("Hu")) p1 = 4;
            else if (act1.Contains("Gang")) p1 = 3;
            else if (act1.Contains("Peng")) p1 = 2;
            else if (act1.Contains("Chi")) p1 = 1;
            else p1 = 0;

            if (act2.Contains("Hu")) p2 = 4;
            else if (act2.Contains("Gang")) p2 = 3;
            else if (act2.Contains("Peng")) p2 = 2;
            else if (act2.Contains("Chi")) p2 = 1;
            else p2 = 0;

            if (p1 >= p2) return act1;
            else return act2;
        }
        static private String JudgeOutCard(int ha, int outCard, int step, double[] dHuNum)
        {
            int[] yb14 = new int[14];
            mj[ha].HandInCard.CopyTo(yb14, 0);
            Array.Sort(mj[ha].HandInCard);
            yb14[13] = Mahjong.in_card;
            Array.Sort(yb14);
            int[] HuNum = Mahjong.NormalizeArray(dHuNum);
            int maxW = Mahjong.MaxOfArray(HuNum);
            if (maxW == 0) return "";

            //找出最大权重的牌组
            int outW = 0, cc = 0;
            if (HuNum[0] == maxW)
                cc++;
            for (int i = 1; i < HuNum.Length; i++)
                if (HuNum[i] >= 98 && yb14[i - 1] != yb14[i]) //98分以上都可以
                    cc++;
            int[] maxCard = new int[cc];
            cc = 0;
            if (HuNum[0] == maxW)
                maxCard[cc++] = yb14[0];
            for (int i = 1; i < HuNum.Length; i++)
                if (HuNum[i] >= 98 && yb14[i - 1] != yb14[i])
                    maxCard[cc++] = yb14[i];
            if (cc == 0)
            { }
            for (int i = 0; i < yb14.Length; i++)
                if (yb14[i] > 0 && yb14[i] == outCard && outW < HuNum[i])
                    outW = HuNum[i];

            //误差控制 
            String str = "";
            if (outW < 50)
            {
                //printMjInfoData(ha, step);
                str += "手牌最大权重牌[";
                for (int i = 0; i < maxCard.Length; i++)
                    str += maxCard[i] + " ";
                str += "]  实际出牌" + outCard + "  权重" + outW;
            }
            else
                str = outCard + "权重" + outW;
            return str;
        }


        static void printMjInfoData(int ha, int Step)
        {
            String Line = Lines[Step];
            String[] SplitStr = Line.Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            if (SplitStr.Length <= 2 || SplitStr[2] == "Deal")
                return;

            String printStr = Step.ToString().PadRight(2) + ": ha=" + ha + "|";
            if (SplitStr[2] == "Play" || SplitStr[2] == "Draw" || SplitStr[2] == "BuGang" || SplitStr[2] == "AnGang")
                printStr += "|I=" + Mahjong.in_card.ToString().PadRight(3) + "|";
            else if (SplitStr[2] == "Peng" || SplitStr[2] == "Chi" || SplitStr[2] == "Gang")//明杠
                printStr += "|O=" + Mahjong.out_card.ToString().PadRight(3) + "|";
            else
                printStr += "|".PadRight(6) + "|";
            for (int i = 0; i < mj[ha].HandInCard.Length; i++)
                printStr += mj[ha].HandInCard[i].ToString().PadRight(3);
            printStr += "|";
            printStr += Line.PadRight(10) + "->";
            Console.Write(printStr);
        }

        static double[] If_Must_Hu_OneCardOutedCOM(int ha)
        {
            int[] HIC = mj[ha].HandInCard;
            double[] RetArray = new double[3] { 1, 0, 0 };
            int[] KRC = mj[ha].KnownRemainCard;
            int FanZimo = 0;
            int JiaoNum = 0;
            int count = 0;
            double WallNum = Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);

            int[] jiao = new int[14];
            mj[ha].what_jiao(HIC, jiao);
            //计算胡牌的可能张数及收益
            int fan0 = mj[ha].Computer_Score_Hu_OneCardOut(Mahjong.out_card);
            string str = "";
            str += "OutCard:" + Mahjong.out_card.ToString().PadLeft(2) + " Fan:" + fan0.ToString().PadLeft(2) + "||";
            for (int i = 0; i < jiao.Length; i++)
                if (jiao[i] > 0)
                {
                    int oldOC = Mahjong.out_card;
                    Mahjong.out_card = 0;
                    int fan1 = mj[ha].Computer_Score_Hu_OneCardOut(jiao[i]);
                    String Str001 = mj[ha].RetFanT();
                    Mahjong.out_card = oldOC;
                    if (fan1 >= Mahjong.JiBenHuFen)
                    {
                        count = mj[ha].KnownRemainCard[jiao[i]];
                        FanZimo += fan1 * count;
                        JiaoNum += count;
                        str += "|Jiao:" + jiao[i].ToString().PadLeft(2) + " Fan:" +
                            fan1.ToString().PadLeft(2) + " KNC:" + count.ToString().PadLeft(2);
                    }
                }
            str = str.PadRight(88);
            if (JiaoNum > 0)
                str += " +Fan=" + (FanZimo / JiaoNum - fan0).ToString().PadLeft(2) +
                    " JiaoNum:" + JiaoNum.ToString().PadLeft(2);
            else
                str += "".PadLeft(19);
            str += " ABC=" + Mahjong.LongOfCardNZ(Mahjong.AllBottomCard);
            str = str.PadRight(122) + "|||";
            //Console.Write(str);
            if (Mahjong.LongOfCardNZ(jiao) > 1)
            { }
            return RetArray;
        }
        static bool Can_Gang_FetchedCOM(int ha)
        {
            if (Mahjong.myBCP[ha] >= 21)
                return false;

            int[] yb14 = new int[14];
            mj[ha].HandInCard.CopyTo(yb14, 0);
            yb14[13] = Mahjong.in_card;
            Array.Sort(yb14);
            Mahjong.WantGangCard = mj[ha].What_AnBaHouGang_Cards();

            mj[ha].can_angang = mj[ha].can_bagang = mj[ha].can_houbagang = false;
            if (Mahjong.CountCardfArray(yb14, Mahjong.WantGangCard) == 4)
                mj[ha].can_angang = true;
            else if (Mahjong.WantGangCard == Mahjong.in_card)
                mj[ha].can_bagang = true;
            else if (Mahjong.CountCardfArray(yb14, Mahjong.WantGangCard) == 1)
                mj[ha].can_houbagang = true;

            bool angang = false, bagang = false, houbagang = false;
            if (mj[ha].Can_BaGang())
                bagang = true;
            if (mj[ha].Can_HouBaGang() > 0)
                houbagang = true;
            if (mj[ha].Can_AnGang_Cards() > 0)
                angang = true;
            if(mj[ha].can_angang != angang || mj[ha].can_houbagang != houbagang || mj[ha].can_bagang != bagang)
            { }
            if (Mahjong.WantGangCard > 0)
            { }
            return mj[ha].can_angang || mj[ha].can_bagang || mj[ha].can_houbagang;
        }

        ///修改于2016.2.1
        ///看控牌方能否自摸胡。
        static bool Can_Hu(int ha)
        {
            int[] tmp = new int[14];
            mj[ha].HandInCard.CopyTo(tmp, 0);
            tmp[13] = Mahjong.in_card + Mahjong.out_card;
            Array.Sort(tmp);

            mj[ha].AdjusHandCard(tmp);
            mj[ha].AdjustHandShunKe(tmp);
            mj[ha].AdjustWinFlag(Mahjong.in_card, ha);
            for (int j = 0; j < mj[ha].AllFanTab.Length; j++)
                Array.Clear(mj[ha].AllFanTab[j], 0, mj[ha].AllFanTab[j].Length);

            if (mj[ha].If_Hu_Cards(Mahjong.in_card) > 0 || mj[ha].If_Hu_SpecialCards())
            {
                int Score = mj[ha].AdjustHandShunKe_ComputerScore(tmp);
                if (Score >= Mahjong.JiBenHuFen)
                    mj[ha].can_hu = true;
            }
            return mj[ha].can_hu;
        }


        static int Choice_OneCard_Out(int ha, out double[] out_card_weight, out string debugInfo)
        {
            int[] yb = new int[14];
            mj[ha].HandInCard.CopyTo(yb, 0);
            Array.Sort(yb);
            if (Mahjong.in_card > 0 && Mahjong.in_card < 46)
                yb[0] = Mahjong.in_card;
            Array.Sort(yb);

            out_card_weight = mj[ha].PriorityFastestHuCard(yb);
            int ind = Mahjong.IndexOfMaxInArray(out_card_weight);
            int card = yb[ind];
            debugInfo = mj[ha].CreateAllFanTabInfo(yb);
            Mahjong.RetOutCard = Mahjong.NormalizeArray(out_card_weight);

            string printStr = "";
            for (int j = 1; j < 31; j++)
            {
                if (j % 10 == 0)
                    printStr = printStr.Substring(0, printStr.Length - 1) + "#";
                else
                    printStr += mj[ha].KnownRemainCard[j].ToString().PadRight(2);
            }
            for (int j = 31; j < 44; j++)
            {
                if (j == 38)
                    printStr = printStr.Substring(0, printStr.Length - 1) + "#";
                else if (j % 2 == 0)
                    continue;
                else
                    printStr += mj[ha].KnownRemainCard[j].ToString().PadRight(2);
            }
            printStr += "<-KNC\n";
            for (int i = 0; i < yb.Length; i++)
                printStr += yb[i].ToString().PadRight(3);
            printStr += "<-YB\n";
            int[] HuNum = Mahjong.NormalizeArray(mj[ha].SafetyRatio14);
            for (int i = 0; i < HuNum.Length; i++)
                printStr += HuNum[i].ToString().PadRight(3);
            printStr += "<-SafetyRatio\n";
            HuNum = Mahjong.NormalizeArray(out_card_weight);
            for (int i = 0; i < HuNum.Length; i++)
                printStr += HuNum[i].ToString().PadRight(3);
            printStr += "<-权重\n";
            debugInfo = printStr + debugInfo;

            return card;
        }
        static bool Can_Chi_OneCardOuted(int ha)
        {
            int ChiHa = ha;
            if (mj[ChiHa].game_playing && mj[ChiHa].Can_Chi_Cards(Mahjong.out_card))
            {
                mj[ChiHa].can_chi = true;
                string OutStr;
                double[] ChiRe = mj[ChiHa].If_Must_Chi(out OutStr);
                bool Must_Chi = ChiRe[0] > 0;
                if (mj[ChiHa].auto_play && !Must_Chi)
                    mj[ChiHa].can_chi = false;
                ChiRe[0] = -ChiRe[0];
                int index = Mahjong.IndexOfMaxInArray(ChiRe) - 1;
                ChiRe[0] = -ChiRe[0];
                for (int i = 0; i < 3; i++)
                    mj[ChiHa].ChiFa[i] = ChiRe[i + 4];
                return mj[ChiHa].can_chi;
            }
            else
                mj[ChiHa].can_chi = false;
            return false;
        }

        static void Computer_RemainCard_JSON(int ha, dynamic input, int turnID = 0)
        {
            if (turnID == 0)
                turnID = input.requests.Length;
            if (turnID < 2) return;
            //初始化
            for (int j = 1; j < pKRC.Length - 1; j++)
                if (j % 10 == 0 || j > 30 && j < pKRC.Length && j % 2 == 0)
                    pKRC[j] = 0;
                else
                    pKRC[j] = 4;

            string[] SplitStr = input.requests[0].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            String myPlayerID = SplitStr[1];
            //减去本家手上的牌.
            String[] sHandC = new String[14];
            SplitStr = input.requests[1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < 13; j++)
                pKRC[Str2Card(SplitStr[j + 5])]--;

            for (int i = 2; i < turnID; i++)
            {
                if (i == 22)
                { }
                SplitStr = input.requests[i].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                if (SplitStr[0] == "2")
                    pKRC[Str2Card(SplitStr[1])]--;
                else if (SplitStr[0] == "3" && SplitStr[1] != myPlayerID)
                {
                    if (SplitStr[2] == "PLAY")
                        pKRC[Str2Card(SplitStr[3])]--;
                    else if (SplitStr[2] == "BUGANG")
                        pKRC[Str2Card(SplitStr[3])]--;
                    else if (SplitStr[2] == "CHI")
                    {
                        int midCard = 0, inC = 0;
                        midCard = Str2Card(SplitStr[3]);
                        pKRC[Str2Card(SplitStr[4])]--;
                        SplitStr = input.requests[i - 1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                        if (SplitStr[2] == "PLAY" || SplitStr[2] == "CHI" || SplitStr[2] == "PENG")
                            inC = Str2Card(SplitStr[SplitStr.Length - 1]);
                        for (int j = midCard - 1; j <= midCard + 1; j++)
                            if (j != inC)
                                pKRC[j]--;
                    }
                    else if (SplitStr[2] == "PENG")
                    {
                        pKRC[Str2Card(SplitStr[3])]--;
                        SplitStr = input.requests[i - 1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                        if (SplitStr[2] == "PLAY" || SplitStr[2] == "CHI" || SplitStr[2] == "PENG")
                            pKRC[Str2Card(SplitStr[3])] -= 2;
                    }
                    else if (SplitStr[2] == "GANG")
                    {
                        SplitStr = input.requests[i - 1].Split(null as char[], StringSplitOptions.RemoveEmptyEntries);
                        if (SplitStr[2] == "PLAY" || SplitStr[2] == "CHI" || SplitStr[2] == "PENG")
                            pKRC[Str2Card(SplitStr[SplitStr.Length - 1])] -= 3;
                    }
                }
            }
            Array.Clear(mj[ha].KnownRemainCard, 0, mj[ha].KnownRemainCard.Length);
            pKRC.CopyTo(mj[ha].KnownRemainCard, 0);
            int cc = 0;
            for (int i = 0; i < pKRC.Length; i++)
            {
                cc += pKRC[i];
                if (pKRC[i] < 0)
                { }
            }
            cc = 136 - cc;

            return;
        }
        static int Str2Card(String str)
        {
            str = str.Trim();
            if (String.IsNullOrEmpty(str))
                return -1;
            int ret = 0;
            char c = str[0];
            int num = str[1] - '0';
            if ((c == 'W' || c == 'B' || c == 'T' || c == 'F' || c == 'J') && num <= 9 && num >= 1)
                switch (c)
                {
                    case 'T':
                        ret = num; break;
                    case 'W':
                        ret = num + 10; break;
                    case 'B':
                        ret = num + 20; break;
                    case 'F':
                        ret = num * 2 + 29; break;
                    case 'J':
                        ret = num * 2 + 37; break;
                    default: break;
                }
            return ret;
        }
        static String Card2Str(int card)
        {
            if (card < 0 || card > 43 || card % 10 == 0 || card > 30 && card % 2 == 0)
            { }
            String ret = "";
            if (card >= 1 && card <= 9)
                ret = "T" + (card).ToString();
            else if (card <= 19)
                ret = "W" + (card - 10).ToString();
            else if (card <= 29)
                ret = "B" + (card - 20).ToString();
            else if (card <= 37)
                ret = "F" + ((card - 29) / 2).ToString();
            else if (card <= 43)
                ret = "J" + ((card - 37) / 2).ToString();
            return ret;
        }
        static int getWeightFromCard(int[] yb, int[] weight, string sCard)
        {
            int iCard = Str2Card(sCard);
            for (int j = 0; j < yb.Length; j++)
                if (yb[j] == iCard)
                    return (int)weight[j];
            return 0;
        }
        public static void AarryMinusNum(int[] arr, int card)
        {
            if (card == 0)
                return;
            for (int j = 0; j < arr.Length; j++)
                if (arr[j] == card)
                {
                    arr[j] = 0;
                    break;
                }
        }
        public static void InsertNum2Array(int[] arr, int card)
        {
            if (card == 0)
                return;
            for (int j = 0; j < arr.Length; j++)
                if (arr[j] == 0)
                {
                    arr[j] = card;
                    break;
                }
        }

        static void CntlDealAnGang(int ha, int card)
        {
            int len = Mahjong.LongOfCardNZ(mj[ha].BumpCard);
            mj[ha].BumpCard[len] = mj[ha].BumpCard[len + 1] = mj[ha].BumpCard[len + 2]
                = mj[ha].BumpCard[len + 3] = card;

            int[] yb = new int[14];
            mj[ha].HandInCard.CopyTo(yb, 0);
            Array.Sort(yb);
            yb[0] = Mahjong.in_card;
            Array.Sort(yb);

            for (int i = 0; i < yb.Length - 3; i++)
            {
                if (yb[i] > 0 && yb[i] == yb[i + 3] && yb[i] == card)
                {
                    yb[i] = yb[i + 1] = yb[i + 2] = yb[i + 3] = 0;
                    break;
                }
            }

            Array.Sort(yb);
            for (int j = 1; j < yb.Length; j++)
                mj[ha].HandInCard[j - 1] = yb[j];

            InsertNum2Array(mj[ha].FanCardData.ArrAgang, card);
        }
        static void CntlDealMinGang(int ha, int card)
        {
            int len = Mahjong.LongOfCardNZ(mj[ha].BumpCard);
            mj[ha].BumpCard[len] = mj[ha].BumpCard[len + 1] = mj[ha].BumpCard[len + 2]
                = mj[ha].BumpCard[len + 3] = card;

            Array.Sort(mj[ha].HandInCard);
            for (int i = 0; i < mj[ha].HandInCard.Length - 2; i++)
                if (mj[ha].HandInCard[i] == card)
                {
                    mj[ha].HandInCard[i] = mj[ha].HandInCard[i + 1] = mj[ha].HandInCard[i + 2] = 0;
                    break;
                }
            Array.Sort(mj[ha].HandInCard);

            InsertNum2Array(mj[ha].FanCardData.ArrMgang, card);

        }
        static void CntlDealBaGang(int ha, int card)
        {
            //扒杠 。
            int[] yb = mj[ha].BumpCard;
            for (int i = yb.Length - 1; i > 0; i--)
            {
                if (yb[i] > 0 && card != yb[i])
                {
                    yb[i + 1] = yb[i];
                    yb[i] = 0;
                }
                else if (yb[i] > 0 && card == yb[i])
                {
                    yb[i + 1] = yb[i];
                    break;
                }
            }
            //去除碰，记录巴杠
            for (int i = 0; i < mj[ha].FanCardData.ArrMke.Length; i++)
                if (mj[ha].FanCardData.ArrMke[i] == card)
                    mj[ha].FanCardData.ArrMke[i] = 0;
            InsertNum2Array(mj[ha].FanCardData.ArrMgang, card);
        }
        static void CntlDealHouGang(int ha, int lastCard)
        {
            int[] bumps = mj[ha].BumpCard;
            int gang_c = 0;
            for (int i = 0; i < bumps.Length; i++)
                for (int j = 0; j < mj[ha].HandInCard.Length; j++)
                    if (bumps[i] > 0 && bumps[i] == mj[ha].HandInCard[j])
                    {
                        for (int k = bumps.Length - 1; k > 0; k--)
                        {
                            if (bumps[k] > 0 && mj[ha].HandInCard[j] != bumps[k])
                            {
                                bumps[k + 1] = bumps[k];
                                bumps[k] = 0;
                            }
                            else if (bumps[k] > 0 && mj[ha].HandInCard[j] == bumps[k])
                            {
                                gang_c = mj[ha].HandInCard[j];
                                bumps[k + 1] = bumps[k];
                                mj[ha].HandInCard[j] = lastCard;
                                break;
                            }
                        }
                    }
            Array.Sort(mj[ha].HandInCard);

            //去除碰，记录巴杠
            for (int i = 0; i < mj[ha].FanCardData.ArrMke.Length; i++)
                if (mj[ha].FanCardData.ArrMke[i] == gang_c)
                    mj[ha].FanCardData.ArrMke[i] = 0;
            InsertNum2Array(mj[ha].FanCardData.ArrMgang, gang_c);
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////
    public struct WanFa
    {
        public int DiFen;//底分
        public bool ZiMoJaDi;//自摸加底
        public bool JiangDui;
        public bool HuanShanZhang;
        public int ZuiDaFanShu;//最大番数
        public int JuShuXuanZe;//局数选择
        public bool DianGangHua;//点杠花
        public bool MenQin;//门清
        public bool CaGua; //擦挂 
        public bool ZhongZhang;//中张
        public bool TianDiHu;
    };
    [Flags]
    public enum PlaySoundFlags : int
    {
        SND_SYNC = 0x0000,          /* play synchronously (default) */
        SND_ASYNC = 0x0001,         /* play asynchronously */
        SND_NODEFAULT = 0x0002,     /* silence (!default) if sound not found */
        SND_MEMORY = 0x0004,        /* pszSound points to a memory file */
        SND_LOOP = 0x0008,          /* loop the sound until next sndPlaySound */
        SND_NOSTOP = 0x0010,        /* don't stop any currently playing sound */
        SND_NOWAIT = 0x00002000,    /* don't wait if the driver is busy */
        SND_ALIAS = 0x00010000,     /* name is a registry alias */
        SND_ALIAS_ID = 0x00110000,  /* alias is a predefined ID */
        SND_FILENAME = 0x00020000,  /* name is file name */
        SND_RESOURCE = 0x00040004   /* name is resource name or atom */
    }

    public enum PlayerStatus
    {
        Playing = 1,
        Hu_ZiMo,       //自摸
        Hu_DianPao,    //点炮
    }

    enum MjAction
    {
        AffirmAzimuth = 1,
        EnterNet,
        ExitReadThread,
        WantSitDown,
        WantStandUp,
        SendManOrComputerStatus,
        BeginPlay,
        ExitApplication,
        SendAllName,
        SendMyName,
        SendOneName,
        FetchOneCard,
        PutOutOneCard,
        AbolishOutOneCard,
        GetOneOutCard,

        BaoTin,
        QiangGang,
        BumpCard,
        ChiCard,


        MingGangCard,
        AnGangCard,
        BaGangCard,
        HouBaGangCard,//用于后杠

        HuCard,
        LeaveNet,

        CanBaoTin,
        CanQiangGang,
        CanBumpCard,
        CanMingGangCard,
        CanAnGangCard,
        CanBaGangCard,
        CanHouBaGangCard,//用于后杠
        CanHuCard,


        WantBaoTin,
        WantQiangGang,
        WantBumpCard,
        WantMingGangCard,
        WantAnGangCard,
        WantBaGangCard,
        WantHuCard,
        WantPutOutOneCard,
        WantLeaveNet,
        SendAllData,
        SendMessage,
        GameOver,
        AnGangConcel,
        BaGangConcel,
        ChiceOneCardOut,
        BumpConcel,
        HuConcel,

        BaoTinConcel,
        QiangGangConcel,
    }

    /// <summary>
    /// mahjong 的摘要说明,麻将类。System.Runtime.CompilerServices
    /// </summary>
    public partial class Mahjong
    {
        public static int[] myBCP = new int[4];
        public const int ABC_len = 144 - 8; // 没有花牌
        public const int KrcLen = ABC_len / 4 + 11;
        public const int CardClassLen = 34;
        public static int[] AllBottomCard = new int[ABC_len];          //108张公用牌    
        public static int[] AllPutOutCard = new int[101];    //打出的牌 
        public static int[] Aband_Card = new int[15 * 6];        //不要后打出的牌 
        public static int[][] AbandCard4P = new int[4][];        //不要后打出的牌
        public static int[][] BumpCard4P = new int[4][];

        public static int fetch_azimuth, old_azimuth,  //fetched_bottom_azimuth,
                            bottom_point = -1;      //应该摸牌的方位、位置、底牌指针 
        public static int out_card, in_card, net_cmd_int_rev = 0, net_cmd_int_snd = 0;

        public int[] HandInCard = new int[13];          //每人抓的确14张牌
        public int[] BumpCard = new int[16];          //碰、杠后倒下的牌 。大四喜：4杠后单钓 16张 
        public int[] PutOutCard = new int[30];           //打出的牌

        public int[] KnownRemainCard = new int[KrcLen];        //每家能知道仍可能在底牌中的牌.
        public int max_jiao, score;
        public bool game_playing, can_hu, can_bump, can_chi,
                        can_angang, can_bagang, can_houbagang, can_minggang, ganged, bumped, chied, hued,
                        IfCancel, auto_play;

        public bool can_putoutcard;     //能否打牌。摸后、碰后可打。刚打过则不能打 
        public char azimuth_char;
        public int azimuth;
        public int HuCardNum = 9;  //胡牌数，越大越是跑得快。如5，如果还有5张以上未出现，则不胡. 

        public static int PlayingNum;
        public PlayerStatus player_status;
        public static int[] DianPaoCard = new int[5];     //点炮牌。DianPaoCard[4]为表示一炮双响。 DianPaoCard[i] = m,表示第i家胡的是m
        public double[,] TwtTable = new double[4, 86];
        public int AiLevel = 2;

        public static int NumOne = 0, NumTwo = 0, NumThree = 0, NumFour = 0, NumFive = 0, NumSix = 0;
        public static int sNum1 = 0, sNum2 = 0, sNum3 = 0, sNum4 = 0, sNum5 = 0;
        static Random rdm1 = new Random(2);

        public ArrayList KrcToWall = new ArrayList();
        public ErmjFanCardData FanCardData = new ErmjFanCardData();
        public static int[,] PossibleCS = new int[4, 20];   //可以摸的的牌的序列，5107：第51张牌为7条   TimeOut = 4500;
        public int FlowerCard = 0;
        static bool ShowConsole = true;
        //----------------------------------------------
        //public static int JiBenHuFen = 8, DepthHu = 9, DepthChi = 10, DepthBump = 10,
        //  DepthGang = 11, TimeOut = 4500;
        //key 参数 关系到速度
        public static int JiBenHuFen = 8, DepthHu = 8, DepthChi = 9, DepthBump = 9,
            DepthGang = 9, TimeOut = 4500;
        public double[] ChiFa = new double[] { -1, -1, -1 };
        public ushort[] fan_table = new ushort[(int)FanT.FAN_TABLE_SIZE];
        public ushort[][] AllFanTab = new ushort[160][];
        public String StudyInfo = "";
        public bool AiPlay = false, AiDecision = false;  //人工神经网络通信 
        public static DateTime TimeStart;
        public static double TimeSpan = 55555.000000;
        public static double[] RetCPG = new double[8];  //判断是否吃、碰、杠的全局数据
        public static double[][] EvalCPGHs = new double[8][];  //判断是否吃、碰、杠的全局数据
        public static double[][][] GlobeHuNums5Class = new double[5][][];  //判断是否吃、碰、杠的全局数据
        public static double[][][][] GlobeCPGHs = new double[8][][][];
        public static double[][][] GlobeHuNumsNormOr7Pair = new double[5][][];
        public static int[] RetOutCard = new int[14];   //判断打牌的全局数据
        public double[] SafetyRatio14 = new double[14];          //安全系数
        public static int WantGangCard = 0;
        public static String OCPG_str = "";
        public static int[,] ZuHeCard = new int[,] {
            { 1, 4, 7, 12, 15, 18, 23, 26, 29},
            { 1, 4, 7, 13, 16, 19, 22, 25, 28},
            { 2, 5, 8, 11, 14, 17, 23, 26, 29},
            { 2, 5, 8, 13, 16, 19, 21, 24, 27},
            { 3, 6, 9, 12, 15, 18, 21, 24, 27},
            { 3, 6, 9, 11, 14, 17, 22, 25, 28}, };
        public static int[] Card34 = new int[] {
            1, 2, 3, 4, 5, 6, 7, 8, 9,
            11, 12, 13, 14, 15, 16, 17, 18, 19,
            21, 22, 23, 24, 25, 26, 27, 28, 29,
            31, 33, 35, 37, 39, 41, 43};
        public Mahjong()
        {
        }
        public Mahjong(char ch)
        {
            azimuth_char = ch;
            if (ch == 'D')
                azimuth = 0;
            else if (ch == 'N')
                azimuth = 1;
            else if (ch == 'X')
                azimuth = 2;
            else if (ch == 'B')
                azimuth = 3;
        }

        public void InitData()
        {
            game_playing = true;
            IfCancel = false;
            can_hu = false;
            can_bump = false;

            can_angang = false;
            can_minggang = false;
            can_bagang = false;
            can_houbagang = false;
            can_chi = false;

            AiPlay = false;
            ganged = false;
            hued = bumped = chied = false;
            can_putoutcard = true;
            auto_play = true;

            Mahjong.PlayingNum = 4;
            Mahjong.bottom_point = -1;
            player_status = PlayerStatus.Playing;
            FlowerCard = 0;
            ChiFa = new double[] { -1, -1, -1 };

            Array.Clear(AllBottomCard, 0, AllBottomCard.Length);
            Array.Clear(HandInCard, 0, HandInCard.Length);
            Array.Clear(BumpCard, 0, BumpCard.Length);
            Array.Clear(PutOutCard, 0, PutOutCard.Length);
            Array.Clear(AllPutOutCard, 0, AllPutOutCard.Length);
            Array.Clear(DianPaoCard, 0, DianPaoCard.Length);
            Array.Clear(TwtTable, 0, TwtTable.Length);
            for (int j = 1; j < KnownRemainCard.Length - 1; j++)
                if (j % 10 == 0 || j > 30 && j < KnownRemainCard.Length && j % 2 == 0)
                    KnownRemainCard[j] = 0;
                else
                    KnownRemainCard[j] = 4;
            for (int j = 0; j < AllFanTab.Length; j++)
                AllFanTab[j] = new ushort[(int)FanT.FAN_TABLE_SIZE];
            for (int j = 0; j < 4; j++)
            {
                Mahjong.AbandCard4P[j] = new int[26];
                Mahjong.BumpCard4P[j] = new int[BumpCard.Length];
            }
            StudyInfo = "";

            FanCardData = new ErmjFanCardData();
            this.FanCardData.seat_wind = (wind_t)(31 + 2 * azimuth);
            KrcToWall.Clear();
        }

        public static void WashCard(int Seed)
        {
            Random rdm1 = new Random(Seed);

            //初始化108张牌
            for (int i = 0; i < 108; i++)
            {
                if (i < 4 * 9) { AllBottomCard.SetValue(i / 4 + 1, i); }
                else if (i < 4 * 9 * 2) { AllBottomCard.SetValue((i - 36) / 4 + 10 + 1, i); }
                else { AllBottomCard.SetValue((i - 72) / 4 + 20 + 1, i); }
            }
            for (int i = 108; i < ABC_len; i++)
            {
                AllBottomCard[i] = ((i - 108) / 4) * 2 + 31;
            }
            //从最后一张开始，最后一张与前面随机一张进行交换
            for (int i = AllBottomCard.GetUpperBound(0); i >= AllBottomCard.GetLowerBound(0); i--)
            {
                int r = rdm1.Next(AllBottomCard.GetLowerBound(0), i);
                int ex = (int)AllBottomCard.GetValue(i);
                AllBottomCard.SetValue(AllBottomCard.GetValue(r), i);
                AllBottomCard.SetValue(ex, r);
            }
        }

        /// <summary>
        /// 胡牌后求得分子程序。
        /// </summary>
        /// <param name="yb"></param>完整的能胡的牌组
        /// <param name="in_card"></param>最后摸的牌
        /// <returns></returns>分数.   平胡1，大对2，夹心五2，七对4，清一色4，幺九牌4，"报叫4，天胡8，"
        ///								清大对8 ，清夹心五8 ，将对8 ，龙七对8，清七对16，双龙16,
        public int Computer_Score_Hu_OneCardOut(int out_c)
        {
            int[] yb14 = new int[14];
            this.HandInCard.CopyTo(yb14, 0);
            yb14[13] = out_c;
            AdjusHandCard(yb14);
            AdjustHandShunKe(yb14);
            AdjustWinFlag(out_c, this.azimuth);
            int score = AdjustHandShunKe_ComputerScore(yb14);
            return score;
        }

        //能胡牌时，求得分
        public int Computer_Score_HuCard()
        {
            Array.Clear(fan_table, 0, fan_table.Length);

            if (If_ZuHeLong() > 0)//202205
            {
                int[] yb = new int[14];
                HandInCard.CopyTo(yb, 0);
                yb[13] = Mahjong.in_card + Mahjong.out_card;
                Array.Sort(yb);
                int[] JinKey; int maxJin;
                MaxZHL147_258_369Key(yb, out JinKey, out maxJin);
                //去筋  

                for (int k = 0; k < yb.Length; k++)
                {
                    if (yb[k] == 0 || yb[k] > 30 || k < yb.Length - 1 && yb[k] == yb[k + 1])
                        continue;
                    if (yb[k] % 10 % 3 == JinKey[yb[k] / 10] % 3)
                        yb[k] = 0;
                }
                Array.Sort(yb);
                int[][] JangShunKes = DivideToJiangShunKe(yb);
                int[] JSK = JangShunKes[0];

                int cc = 0;// 手牌获得的顺子
                Array.Clear(FanCardData.ArrAshun, 0, FanCardData.ArrAshun.Length);
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 200)
                        FanCardData.ArrAshun[cc++] = JSK[j] % 200;
                cc = 0;// 暗刻子数组
                Array.Clear(FanCardData.ArrAke, 0, FanCardData.ArrAke.Length);
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 100 && JSK[j] < 200)
                        FanCardData.ArrAke[cc++] = JSK[j] % 100;
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 0 && JSK[j] < 100)
                        FanCardData.Jiang = JSK[j];
            }
            int score = 1;
            //7--------------------------------------------88
            score += If_Da4Xi();
            score += If_Da3Yuan();
            score += If_ThirteenOne();
            score += If_LvYiSe();
            score += If_9LianBD();
            score += If_Lian7Dui();
            score += If_34Gang();

            //6--------------------------------------------64      
            score += If_QinYaoJiu();
            score += If_PingHu_XiaoSiXi_XiaoSanYuan(); //小三元
            //小四喜
            score += If_ZiYiSe();
            score += If_1Se2LongHui();
            score += If_234AnKe();

            //2--------------------------------------------48            
            score += If_1Se234TongShun();//一般高 一色四同顺
            score += If_1Se34JieGao();

            //3--------------------------------------------32
            score += If_1Se34BuGao();
            //三杠 If_34Gang 
            score += If_HunYaoJiu();

            //9--------------------------------------------24
            score += If_7Dui();
            score += If_7XinBuKao();
            score += If_Quan_DaiYao_DaiWu_ShuangKe(); // 全双刻 全带幺、全带五          
            score += If_WuZi_QiuYiMen_HunYiSe_QIYiSe_WuMenQi();// 无字、缺一门、混一色、清一色、五门齐
            //一色三节高   If_1Se34JieGao
            //一色三同顺   If_1Se234TongShun      
            score += If_DaYuWu_XiaoYuWu_QuanDa_QuanZhong_QuanXiao(); // 大于五、小于五、全大、全中、全小
            //全大 If_DaYuWu_XiaoYuWu_QuanDa_QuanZhong_QuanXiao
            //全中 If_DaYuWu_XiaoYuWu_QuanDa_QuanZhong_QuanXiao

            //6--------------------------------------------16
            score += If_QingLong(); ;
            score += If_3Se2LongHui();
            //一色三步高 If_1Se34BuGao
            //全带五 If_Quan_DaiYao_DaiWu_ShuangKe
            score += If_23TongKe_19Ke();//幺九、 双、三同刻 
                                        //三暗刻   If_234AnKe

            //5--------------------------------------------12  
            score += If_QuanBuKao();
            score += If_ZuHeLong();
            //大于五 If_DaYuWu_XiaoYuWu_QuanDa_QuanZhong_QuanXiao
            //小于五 If_DaYuWu_XiaoYuWu_QuanDa_QuanZhong_QuanXiao
            score += If_3FenKe();

            //9--------------------------------------------8 
            score += If_HuaLong();
            score += If_TuiBuDao();//推不倒
            score += If_23Se23TongShun();
            score += If_3Se3JiGao();
            //无番和
            score += If_HuJeuZhang_MiaoShouHC_HaiDeLY_ZiMu(); //妙手回春            
            //海底捞月 If_HuJeuZhang_MiaoShouHC_HaiDeLY_ZiMu
            //杠上开花???
            //抢杠和???

            //7--------------------------------------------6 
            score += If_PengPengHu();
            score += If_3Se3BuGao();
            //混一色 If_WuZi_QiuYiMen_HunYiSe_QIYiSe_WuMenQi
            //五门齐 If_WuZi_QiuYiMen_HunYiSe_QIYiSe_WuMenQi
            score += If_BuQiuRen_QuanQiuRen_MenQianQin();//不求人  全求人 门前清
            score += If_12AnGang();//双暗杠 
            score += If_12JianKe();//双箭刻 

            //4--------------------------------------------4                               
            //全带幺 If_Quan_DaiYao_DaiWu_ShuangKe
            //不求人 If_BuQiuRen_QuanQiuRen
            score += If_12MingGang();//双明杠
            score += If_HuJueZhang();

            //10--------------------------------------------2
            // 箭刻If_12JianKe
            score += If_MenFengKe();
            score += If_QuanFengKe();
            //score += If_MenQianQing();
            //平和 If_PingHu_XiaoSiXi_XiaoSanYuan
            score += If_SiGuiYi();
            score += If_23TongKe_19Ke();//双、三同刻 
                                        //双暗刻 If_234AnKe  
                                        //暗杠 If_12AnGang
            score += If_DuanYao(); //断幺 

            //13--------------------------------------------1
            //一般高 If_1Se234TongShun
            //喜相逢 If_23Se23TongShun
            score += If_1Se3BuGao_6Lian();//连六 

            //明杠 If_12MingGang
            //缺一门 If_HunYiSe_QinYiSe_Que1Men_WuZi 
            //无字   If_HunYiSe_QinYiSe_Que1Men_WuZi
            score += If_LaoShaoFu();
            score += If_BanZhang();  //边张???
            score += If_KanZhang(); //嵌张???
            score += If_DanDiao();  // 单钓将???
                                    //边张???
                                    //嵌张???
                                    //单钓将???
                                    //自摸 If_HuJeuZhang_MiaoShouHC_HaiDeLY_ZiMu
                                    //花牌??? 

            adjust_fan_table();

            String Str1 = RetFanT();
            int sco = 0; //1028 
            //for (int i = 0; i < Mahjong.fan_table.Length; i++)
            //    if (Mahjong.fan_table[i] > 0)
            //    {
            //        Str1 += Mahjong.fan_name[i];
            //        if (Mahjong.fan_table[i] > 1)
            //            Str1 += Mahjong.fan_table[i].ToString() + "*";
            //        Str1 += Mahjong.fan_value_table[i] + " ";
            //    }
            for (int tt = 0; tt < fan_table.Length; tt++)
                if (fan_table[tt] > 0)
                    sco += fan_table[tt] * fan_value_table[tt];

            return sco;
        }

        public string RetFanT()
        {
            String Str1 = "番 ";
            int sco = 0;
            for (int i = 0; i < fan_table.Length; i++)
                if (fan_table[i] > 0)
                    sco += fan_table[i] * fan_value_table[i];
            Str1 += sco + " ";

            if (sco > 0)
            {
                for (int i = 3 + 14; i < fan_table.Length; i++)
                    if (fan_table[i] > 0)
                    {
                        Str1 += Mahjong.fan_name[i];
                        if (fan_table[i] > 1)
                            Str1 += fan_table[i].ToString() + "*";
                        Str1 += Mahjong.fan_value_table[i] + " ";
                    }
            }
            else
                Str1 = "";
            return Str1;
        }

        public string RetAllFanT(ushort[] FanTabs)
        {
            String Str1 = "";
            int sco = FanTabs[(int)FanT.Score];
            int lever = FanTabs[(int)FanT.InOutLever];

            Str1 += lever + ": " + sco + "番 ";
            for (int i = 0; i < (int)FanT.FLOWER_TILES; i++)
                if (FanTabs[i] > 0)
                    Str1 += Mahjong.fan_name[i] + FanTabs[i] * Mahjong.fan_value_table[i] + " ";
            return Str1 + "\n";
        }

        public string RetAllFanT1(ushort[] FanTabs)
        {
            String Str1 = "";
            int sco = 0;
            for (int tt = 0; tt < FanTabs.Length; tt++)
                if (FanTabs[tt] > 0)
                    sco += FanTabs[tt] * fan_value_table[tt];
            if (sco == 0)
                return "";

            Str1 += "\t\t" + sco + " 番\n";
            int cc = 0;
            for (int i = 0; i < FanTabs.Length; i++)
                if (FanTabs[i] > 0)
                {
                    if (cc % 2 == 0)
                        Str1 += "\n";
                    else
                        Str1 += "\t";
                    cc++;
                    for (int j = 0; j < 5 - Mahjong.fan_name[i].Length; j++)
                        Str1 += "   ";
                    Str1 += Mahjong.fan_name[i];
                    Str1 += FanTabs[i].ToString().PadLeft(2) + " * ";
                    Str1 += Mahjong.fan_value_table[i].ToString().PadLeft(2) + "番";
                }
            return Str1;
        }


        /// YB 是一个十三张的牌组
        /// 返回值：无叫0，平胡1，大对2，夹心五3，七对5，清一色6，幺九牌7，"报叫8，天胡9，"
        ///			清大对10，清夹心五11，将对12，龙七对13，清七对20，双龙21,
        ///			
        public bool what_jiao(int[] yb, int[] jiao)
        {
            if (LongOfCardNZ(yb) % 3 != 1)
                return false;

            int[] HandIn31 = new int[FanCardData.HandIn31.Length];
            int[] HandIn14 = new int[14];
            FanCardData.HandIn31.CopyTo(HandIn31, 0);
            FanCardData.HandIn14.CopyTo(HandIn14, 0);

            int[] tmp = new int[14];    //应为14
            int cc = 0;

            Array.Clear(jiao, 0, jiao.Length);
            Array.Sort(yb);
            for (int i = 1; i < KnownRemainCard.Length; i++)
            {
                if (i % 10 == 0 || i > 30 && i % 2 == 0) continue;
                Array.Clear(tmp, 0, tmp.Length);
                yb.CopyTo(tmp, 0);
                Array.Sort(tmp);
                tmp[0] = i;
                Array.Sort(tmp);
                Array.Clear(FanCardData.HandIn31, 0, FanCardData.HandIn31.Length);
                AdjusHandCard(tmp);
                if (If_NormHu_Cards(tmp) > 0 || If_ZuHeLong(tmp) > 0 || If_QuanBuKao(tmp) > 0 || If_ThirteenOne(tmp) > 0)
                    jiao[cc++] = i;
            }

            HandIn31.CopyTo(FanCardData.HandIn31, 0);
            HandIn14.CopyTo(FanCardData.HandIn14, 0);

            int amount = 0;
            for (int i = 0; i < 10; i++)
                amount += jiao[i];
            if (amount > 0) return true;
            else return false;
        }




        int maxLen = 0;
        ArrayList JangKeShunSet = new ArrayList();
        ///第一个为将，后为顺子、刻子。小于50为刻子，大于50为顺子。64表示13、14、15。
        public int[][] DivideToJiangShunKe(int[] yb)
        {
            int[] tmp_card31 = new int[KnownRemainCard.Length];
            int[] tmp31 = new int[tmp_card31.Length];
            for (int i = 0; i < yb.Length; i++)
                tmp_card31[yb[i]]++;
            tmp_card31[0] = 0;

            if (LongOfCardNZ(yb) >= 10)//得到筋数
                for (int i = 0; i < 6; i++)
                {
                    int KnitNum = 0;
                    for (int j = 0; j < 9; j++)
                        if (tmp_card31[ZuHeCard[i, j]] > 0)
                            KnitNum++;
                    if (KnitNum == 9)
                    {
                        for (int j = 0; j < 9; j++)
                            tmp_card31[ZuHeCard[i, j]]--;
                        break;
                    }
                    
                }

            maxLen = 0;
            int[] JaKeSh = new int[5];
            JangKeShunSet.Clear();
            JangKeShunSet.Add(JaKeSh);
            DivideToShunKe(tmp_card31, JaKeSh, false);

            if (JangKeShunSet.Count > 1)//去重
            {
                foreach (int[] JKS in JangKeShunSet)
                    Array.Sort(JKS);
                for (int i = 0; i < JangKeShunSet.Count; i++)
                    for (int j = i + 1; j < JangKeShunSet.Count; j++)
                    {
                        bool bSame = true;
                        int[] JKS1 = (int[])JangKeShunSet[i];
                        int[] JKS2 = (int[])JangKeShunSet[j];
                        for (int k = 0; k < JKS1.Length; k++)
                            if (JKS1[k] != JKS2[k])
                                bSame = false;
                        if (bSame)//相同
                        {
                            JangKeShunSet.Remove(JKS2);
                            j = i;
                        }
                    }
            }

            int[][] div_Ret = new int[JangKeShunSet.Count][];
            for (int i = 0; i < div_Ret.Length; i++)
                div_Ret[i] = new int[5];
            for (int i = 0; i < JangKeShunSet.Count; i++)
                for (int k = 0; k < 5; k++)
                    div_Ret[i][k] = ((int[])JangKeShunSet[i])[k];
            if (JangKeShunSet.Count > 6)
            { }
            return div_Ret;
        }

        public void DivideToShunKe(int[] yb31, int[] JaKeSh, bool hasPair)
        {
            if (LongOfCardNZ(yb31) == 0)
                return;
            for (int i = 1; i < yb31.Length - 1; i++)
            {
                if (yb31[i] == 0) continue;
                if (yb31[i] >= 2 && !hasPair)//将
                {
                    hasPair = true;
                    yb31[i] -= 2;
                    AppendNum2Array(JaKeSh, i);
                    EnterShunKe(yb31, JaKeSh, hasPair);
                    AarryMinusNum(JaKeSh, i);
                    yb31[i] += 2;
                    hasPair = false;
                }
                if (yb31[i] >= 3)//刻子
                {
                    yb31[i] -= 3;
                    AppendNum2Array(JaKeSh, i + 100);
                    EnterShunKe(yb31, JaKeSh, hasPair);
                    AarryMinusNum(JaKeSh, i + 100);
                    yb31[i] += 3;
                }
                if (yb31[i - 1] * yb31[i] * yb31[i + 1] > 0)//顺子
                {
                    yb31[i - 1]--; yb31[i]--; yb31[i + 1]--;
                    AppendNum2Array(JaKeSh, i + 200);
                    EnterShunKe(yb31, JaKeSh, hasPair);
                    AarryMinusNum(JaKeSh, i + 200);
                    yb31[i - 1]++; yb31[i]++; yb31[i + 1]++;
                }
            }
        }

        public void EnterShunKe(int[] yb31, int[] JaKeSh, bool hasPair)
        {
            if (LongOfCardNZ(JaKeSh) > maxLen)
            {
                maxLen = LongOfCardNZ(JaKeSh);
                JangKeShunSet.Clear();
            }
            if (LongOfCardNZ(JaKeSh) == maxLen)
                JangKeShunSet.Add(JaKeSh.Clone());
            DivideToShunKe(yb31, JaKeSh, hasPair);
        }




        /// 把副露牌与Handin牌组合并一起，返回
        /// 把杠过的牌4张变成3张 
        public int[] AddFuLuToHandin(int[] myHandin)
        {
            int[] tmp14 = new int[14];
            int[] Handin = new int[14];
            myHandin.CopyTo(Handin, 0);
            int cc = 0;
            for (int i = 0; i < 4; i++)
            {
                if (FanCardData.ArrAgang[i] > 0)
                    tmp14[cc++] = tmp14[cc++] = tmp14[cc++] = FanCardData.ArrAgang[i];
                if (FanCardData.ArrMgang[i] > 0 && CountCardfArray(tmp14, FanCardData.ArrMgang[i]) == 0)
                    tmp14[cc++] = tmp14[cc++] = tmp14[cc++] = FanCardData.ArrMgang[i];
                if (FanCardData.ArrMke[i] > 0)
                    tmp14[cc++] = tmp14[cc++] = tmp14[cc++] = FanCardData.ArrMke[i];
            }

            for (int i = 0; i < 4; i++)
                if (FanCardData.ArrMshun[i] > 0)
                {
                    tmp14[cc++] = FanCardData.ArrMshun[i] - 1;
                    tmp14[cc++] = FanCardData.ArrMshun[i];
                    tmp14[cc++] = FanCardData.ArrMshun[i] + 1;
                }
            cc = 0;
            for (int i = 0; i < Handin.Length; i++)
                if (Handin[i] == 0)
                    Handin[i] = tmp14[cc++];

            if (LongOfCardNZ(Handin) != 14)
            { }
            Array.Sort(Handin);
            return Handin;
        }


        public int[] AddBumpToHandin(int[] myHandin)
        {
            if (LongOfCardNZ(myHandin) == 14)
                return myHandin;

            int[] tmp = new int[14];
            int[] yb = myHandin;
            yb.CopyTo(tmp, 0);
            Array.Sort(tmp);

            int[] pung_packs = new int[12];
            int pung_cnt = 0;
            for (int i = 0; i < 4; ++i)
                if (FanCardData.ArrAgang[i] > 0)
                    for (int j = 0; j < 3; j++)
                        pung_packs[pung_cnt++] = FanCardData.ArrAgang[i];
            for (int i = 0; i < 4; ++i)
                if (FanCardData.ArrMgang[i] > 0)
                    for (int j = 0; j < 3; j++)
                        pung_packs[pung_cnt++] = FanCardData.ArrMgang[i];
            for (int i = 0; i < 4; ++i)
                if (FanCardData.ArrMke[i] > 0)
                    for (int j = 0; j < 3; j++)
                        pung_packs[pung_cnt++] = FanCardData.ArrMke[i];
            for (int i = 0; i < 4; ++i)
                if (FanCardData.ArrMshun[i] > 0)
                {
                    pung_packs[pung_cnt++] = FanCardData.ArrMshun[i] - 1;
                    pung_packs[pung_cnt++] = FanCardData.ArrMshun[i];
                    pung_packs[pung_cnt++] = FanCardData.ArrMshun[i] + 1;
                }

            if (pung_cnt + LongOfCardNZ(FanCardData.HandIn14) != 14)
            { }
            for (int i = 0; i < pung_cnt; i++)
                tmp[i] = pung_packs[i];
            return tmp;
        }



        /// <summary>
        /// 判定 HandInCard 加 in_card 能否胡牌.
        /// </summary>
        /// <param name="in_card"></param>
        /// <returns></returns>
        public int If_Hu_Cards(int in_card)
        {
            int[] tmp = new int[14];
            int[] yb = this.HandInCard;

            yb.CopyTo(tmp, 0);
            Array.Sort(tmp);
            tmp[0] = in_card;
            Array.Sort(tmp);

            return If_NormHu_Cards(tmp);
        }

        /// <summary>
        /// 判断牌组是否能胡
        /// </summary>
        /// <param name="yb"></param>YB是一个牌组.
        /// <param name="in_card"></param>刚摸的牌,以判断是否是夹心五.
        /// <returns></returns> 返回值：无叫0，平胡1，大对2，夹心五3，七对5，清一色6，幺九牌7，报叫8，天胡9，
        ///			清大对10，清夹心五11，将对12，龙七对13，清七对20，双龙21,
        public static int If_NormHu_Cards(int[] yb)
        {
            int[] tmp_card = new int[KrcLen];
            for (int i = 0; i < yb.Length; i++)
                tmp_card[yb[i]]++;
            tmp_card[0] = 0;


            int[] tmp = new int[tmp_card.Length];
            for (int i = 1; i < tmp_card.Length - 1; i++)
            {
                if (tmp_card[i] < 2) continue;  //先找到将牌 
                tmp_card.CopyTo(tmp, 0);
                tmp[i] = tmp[i] - 2;            //去掉将牌   
                for (int k = 1; k < tmp_card.Length - 1; k++)
                {
                    //这里可改为直接跳出
                    if (tmp[k] == 0)
                        continue;
                    //前有三张相同牌时, 成一组, 清除掉。
                    if (tmp[k] >= 3)
                    {
                        tmp[k] = tmp[k] - 3; k--;
                    }
                    //有连续的三张牌时, 组成一组, 清除掉。
                    if (tmp[k] > 0 && tmp[k + 1] > 0 && tmp[k + 2] > 0)
                    {
                        tmp[k]--; tmp[k + 1]--; tmp[k + 2]--; k--;
                    }
                }
                //判断是否能胡牌 , 是否清空。               
                if (LongOfCardNZ(tmp) == 0)
                    return 1;
            }
            return 0;
        }

        public int If_Hu_Cards(int[] yb, int numHuCard, int winCard, bool bZHL = false)
        {
            int[] huCards = new int[14], yb31 = new int[KnownRemainCard.Length],
                med31 = new int[yb31.Length], tmp31 = new int[yb31.Length];
            int[] id = new int[5]; id[4] = yb31.Length - 1;
            for (int i = 0; i < yb.Length; i++)
                yb31[yb[i]]++;
            yb31[0] = 0;
            int Count = 0;
            int[] oldHuCards = new int[14];
            for (int i = 1; i < yb31.Length - 1; i++) // 清除掉010型
                if (yb31[i - 1] + yb31[i + 1] == 0 && yb31[i] == 1)
                    yb31[i] = 0;
            for (int i = 1; i < yb31.Length - 2; i++) // 清除掉01n0、0n10型
                if (yb31[i - 1] + yb31[i + 2] == 0 && yb31[i] * yb31[i + 1] > 0)
                {
                    if (yb31[i] == 1) yb31[i] = 0;
                    if (yb31[i + 1] == 1) yb31[i + 1] = 0;
                }

            for (int i = 0; i < yb31.Length; i++) Count += yb31[i];
            if (Count < numHuCard)//可组合牌太少
                return 0;

            if (numHuCard == 14)//七对 
            {
                Count = 0;
                for (int i = 0; i < yb31.Length; i++)
                    Count += yb31[i] / 2;
                if (Count >= 7)
                    return 24; //龙七对,小七对
            }

            String fanStr = ""; int maxCount = 0, sss = 0;
            for (int i = 0; i < yb31.Length - 1; i++)
            {
                if (yb31[i] < 2) continue;  //先找到将牌 
                yb31.CopyTo(med31, 0);
                Array.Clear(huCards, 0, huCards.Length);
                med31[i] = med31[i] - 2;            //去掉将牌  
                huCards[0] = huCards[1] = i;//2张将牌   

                //    把牌分成4段，分别遍历
                //   ├────┼────┼────┼──────┤
                // id[0]      id[1]     id[2]     id[3]          id[4]
                for (id[0] = 1; id[0] < yb31.Length; id[0]++)
                {
                    if (med31[id[0]] == 0) continue;
                    for (id[1] = id[0]; id[1] < yb31.Length; id[1]++)
                    {
                        if (med31[id[1]] == 0) continue;
                        for (id[2] = id[1]; id[2] < yb31.Length; id[2]++)
                        {
                            if (med31[id[2]] == 0) continue;
                            for (id[3] = id[2]; id[3] < yb31.Length; id[3]++)
                            {
                                if (med31[id[3]] == 0) continue;
                                med31.CopyTo(tmp31, 0);
                                Count = 2;
                                Array.Clear(huCards, 2, huCards.Length - 2);
                                if (numHuCard < 14) id[3] = id[4];
                                if (numHuCard < 11) id[2] = id[4];
                                if (numHuCard < 8) id[1] = id[4];

                                for (int j = 0; j < 4; j++)//分成4段 
                                {
                                    for (int k = id[j]; k <= id[j + 1]; k++)//每段分别遍历 
                                    {
                                        sss++;
                                        if (tmp31[k] == 0)
                                            continue;
                                        else if (tmp31[k] >= 3)
                                        {   //前有三张相同牌时, 成一组, 清除掉。
                                            huCards[j * 3 + 2 + 0] = huCards[j * 3 + 2 + 1] = huCards[j * 3 + 2 + 2] = k;
                                            tmp31[k] = tmp31[k] - 3; Count += 3;
                                            break;
                                        }
                                        else if (tmp31[k - 1] * tmp31[k] * tmp31[k + 1] > 0)
                                        {   //有连续的三张牌时, 组成一组, 清除掉。
                                            huCards[j * 3 + 2 + 0] = k - 1; huCards[j * 3 + 2 + 1] = k; huCards[j * 3 + 2 + 2] = k + 1;
                                            tmp31[k - 1]--; tmp31[k]--; tmp31[k + 1]--; Count += 3;
                                            break;
                                        }
                                    }
                                }
                                if (Count >= numHuCard)//判断是否能胡牌 
                                {
                                    for (int v = 0; v < huCards.Length; v++)
                                        if (oldHuCards[v] != huCards[v])
                                            break;
                                    if (bZHL)
                                    {
                                        maxCount = 12;
                                        goto END;
                                    }
                                    huCards.CopyTo(oldHuCards, 0);
                                    AdjusHandCard(huCards);
                                    AdjustHandShunKe(huCards);
                                    int sco2 = Computer_Score_HuCard();
                                    fanStr = RetFanT();
                                    if (sco2 > maxCount)
                                        maxCount = sco2;
                                }
                            }
                        }
                    }
                }
            }
        END:
            return maxCount;
        }

        //[MethodImpl(MethodImplOptions.InternalCall)]
        public bool If_Hu_SpecialCards()
        {
            if (If_ZuHeLong() > 0)
                return true;
            if (If_QuanBuKao() > 0)
                return true;
            if (If_ThirteenOne() > 0)
                return true;
            if (If_7Dui() > 0)
                return true;
            return false;
        }
        /// <summary>
        /// 用于暗杠.是否能杠牌? 是, 返回 true; 否,返回 false  
        /// </summary>这是指本已有的杠。
        public int Can_AnGang_Cards()
        {
            int[] yb = this.HandInCard;
            int[] tmp_card = new int[KnownRemainCard.Length];

            for (int i = 0; i < yb.Length; i++)
                tmp_card[yb[i]]++;
            tmp_card[Mahjong.in_card]++;
            tmp_card[0] = 0;

            for (int i = 1; i < tmp_card.Length; i++)
                if (tmp_card[i] == 4)
                    return i;
            return 0;
        }
        public int What_AnBaHouGang_Cards()//20240512
        {
            int[] yb = new int[14];
            this.HandInCard.CopyTo(yb, 0);
            yb[13] = Mahjong.in_card;
            Array.Sort(yb);

            int[] tmp_card = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                tmp_card[yb[i]]++;
            tmp_card[0] = 0;

            int[] gangCard = new int[4];
            double[] score = new double[4];
            int cc = 0;
            for (int i = 1; i < tmp_card.Length; i++)
                if (tmp_card[i] == 4)
                    gangCard[cc++] = i;
            for (int i = 1; i < tmp_card.Length; i++)
                if (tmp_card[i] == 1 && FanCardData.ArrMke.Contains(i))
                    gangCard[cc++] = i;

            for (int i = 0; i < cc && cc > 1; i++)
            {
                int[] tmp14 = new int[14];
                yb.CopyTo(tmp14, 0);
                for (int j = 0; j < tmp14.Length; j++)
                    if (tmp14[j] == gangCard[i])
                        for (int k = 0; k < tmp14.Length; k++)
                            if (tmp14[k] == gangCard[i])
                                tmp14[k] = 0;
                Array.Sort(tmp14);
                score[i] = evaluateCards13(tmp14, out Mahjong.EvalCPGHs[i]);
            }
            int ind = IndexOfMaxInArray(score);
            int card = 0;
            if (cc == 1)
                card = gangCard[0];
            else if (cc > 1)
                card = gangCard[ind];

            return card;
        }

        /// <summary>
        /// 用于扒杠.是否能杠牌? 是, 返回 true; 否,返回 false .
        /// </summary>
        public bool Can_BaGang()
        {
            int[] yb = this.BumpCard;
            int[] tmp_card = new int[KnownRemainCard.Length];
            if (FanCardData.ArrMke.Contains(Mahjong.in_card))
                return true;
            else
                return false;
        }
        /// <summary>
		/// 用于后扒杠.是否能杠牌? 是, 返回 true; 否,返回 false .
		/// </summary>
		public int Can_HouBaGang()//????不能算分哟
        {
            for (int i = 0; i < HandInCard.Length; i++)
                if (HandInCard[i] > 0 && FanCardData.ArrMke.Contains(HandInCard[i]))
                    return HandInCard[i];
            return 0;
        }
        /// <summary>
        /// 用于明杠 out_c 为要杠的牌.是否能杠牌? 
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="out_c"></param>
        /// <returns></returns>是, 返回 true; 否,返回 false .
        public bool Can_MingGang(int out_c)
        {

            // 用于明杠
            int[] yb = this.HandInCard;
            int[] tmp_card = new int[KnownRemainCard.Length];

            for (int i = 0; i < yb.Length; i++)
                tmp_card[yb[i]]++;

            tmp_card[out_c]++;
            tmp_card[0] = 0;

            for (int i = 1; i < tmp_card.Length; i++)
                if (tmp_card[i] == 4 && i == out_c)
                    return true;
            return false;
        }


        /// <summary>
        /// 扒杠 。
        /// </summary>
        /// <param name="yb"></param> 牌组。
        /// <param name="in_c"></param>摸的、别家打出的牌。
        /// <returns></returns>返回值：被杠的值，0 则没杠。
        public int BaGang_Cards(int in_c)
        {
            //扒杠 。
            int[] bc = this.BumpCard;

            for (int i = bc.Length - 1; i > 0; i--)
            {
                if (bc[i] > 0 && in_c != bc[i])
                {
                    bc[i + 1] = bc[i];
                    bc[i] = 0;
                }
                else if (bc[i] > 0 && in_c == bc[i])
                {
                    bc[i + 1] = bc[i];
                    int in_ccc = Mahjong.in_card;
                    Mahjong.in_card = 0;
                    return in_ccc;
                }
                bc.CopyTo(Mahjong.BumpCard4P[azimuth], 0);
            }
            return in_c;
        }
        /// <summary>
		/// 后杠 。
		/// </summary>
		/// <param name="yb"></param> 牌组。
		/// <returns></returns>返回值：被杠的值，0 则没杠。
		public void HouBaGang_Cards(int gandCard)
        {
            for (int i = 0; i < BumpCard.Length - 2; i++)
                if (BumpCard[i] > 0 && BumpCard[i] == BumpCard[i + 1] && BumpCard[i] == BumpCard[i + 2] && BumpCard[i] == gandCard)
                {
                    for (int j = BumpCard.Length - 1; j > 0; j--)
                    {
                        if (BumpCard[j] > 0 && gandCard != BumpCard[j])
                        {
                            BumpCard[j + 1] = BumpCard[j];
                            BumpCard[j] = 0;
                        }
                        else if (BumpCard[j] > 0 && gandCard == BumpCard[j])
                        {
                            BumpCard[j + 1] = BumpCard[j];
                            Mahjong.AarryMinusNum(this.HandInCard, gandCard);
                            Mahjong.AppendNum2Array(this.HandInCard, Mahjong.in_card);
                            Mahjong.in_card = 0;
                            Array.Sort(HandInCard);
                            BumpCard.CopyTo(Mahjong.BumpCard4P[azimuth], 0);
                            return;
                        }
                    }
                }
            BumpCard.CopyTo(Mahjong.BumpCard4P[azimuth], 0);
            return;
        }
        /// <summary>
        /// 点杠、扒杠 。
        /// </summary>
        /// <param name="yb"></param> 牌组。
        /// <param name="in_card"></param>摸的、别家打出的牌。
        /// <returns></returns>返回值：被杠的值，0 则没杠。
        public int MingGang_Cards(int in_card)
        {
            int[] yb = this.HandInCard;
            int len = LongOfCardNZ(this.BumpCard);
            Array.Sort(yb);
            for (int i = 0; i < yb.Length - 2; i++)
            {
                if (yb[i] > 0 && yb[i] == in_card)
                {
                    this.BumpCard[len] = BumpCard[len + 1] =
                        BumpCard[len + 2] = BumpCard[len + 3] = in_card;
                    BumpCard.CopyTo(Mahjong.BumpCard4P[azimuth], 0);
                    yb[i] = yb[i + 1] = yb[i + 2] = 0;
                    return in_card;
                }
            }
            return 0;
        }

        /// <summary>
        /// 暗杠 ，返回值：被杠的值，0 则没杠。
        /// </summary>
        /// <param name="yb"></param>
        /// <returns></returns>返回值：被杠的值，0 则没杠。注意可能改变Mahjong.in_card
        public int AnGang_Cards()
        {
            int[] yb = new int[14];
            this.HandInCard.CopyTo(yb, 0);
            Array.Sort(yb);
            yb[0] = Mahjong.in_card;
            Array.Sort(yb);

            int len = LongOfCardNZ(this.BumpCard);
            int ex = 0;
            for (int i = 0; i < yb.Length - 3; i++)
            {
                if (yb[i] > 0 && yb[i] == yb[i + 3] && yb[i] == yb[i + 1])
                {
                    ex = yb[i];
                    this.BumpCard[len] = BumpCard[len + 1] = BumpCard[len + 2] = BumpCard[len + 3] = ex;
                    BumpCard.CopyTo(Mahjong.BumpCard4P[azimuth], 0);
                    yb[i] = yb[i + 1] = yb[i + 2] = yb[i + 3] = 0;
                    Mahjong.in_card = 0;
                    Array.Sort(yb);
                    for (int j = 1; j < yb.Length; j++)
                        this.HandInCard[j - 1] = yb[j];
                    break;
                }
            }

            return ex;
        }
        public int AnGang_Cards(int card)
        {
            int[] yb = new int[14];
            this.HandInCard.CopyTo(yb, 0);
            Array.Sort(yb);
            yb[0] = Mahjong.in_card;
            Array.Sort(yb);

            int len = LongOfCardNZ(this.BumpCard);
            int ex = 0;
            for (int i = 0; i < yb.Length - 3; i++)
            {
                if (yb[i] > 0 && yb[i] == yb[i + 2] && yb[i] == yb[i + 3] && yb[i] == card)
                {
                    ex = yb[i];
                    this.BumpCard[len] = BumpCard[len + 1] = BumpCard[len + 2] = BumpCard[len + 3] = ex;
                    BumpCard.CopyTo(Mahjong.BumpCard4P[azimuth], 0);
                    yb[i] = yb[i + 1] = yb[i + 2] = yb[i + 3] = 0;
                    Mahjong.in_card = 0;
                    Array.Sort(yb);
                    for (int j = 1; j < yb.Length; j++)
                        this.HandInCard[j - 1] = yb[j];
                    break;
                }
            }

            return ex;
        }
        public int HowMany_Four(int in_c)
        {
            int[] yb = new int[14 + 4];
            int[] tmp_card = new int[KnownRemainCard.Length];
            this.HandInCard.CopyTo(yb, 0);
            Array.Sort(yb);
            yb[0] = in_c;
            AddArrayToOther(this.BumpCard, yb);
            Array.Sort(yb);

            for (int i = 0; i < yb.Length; i++)
                tmp_card[yb[i]]++;
            tmp_card[0] = 0;

            int count = 0;
            for (int i = 0; i < tmp_card.Length; i++)
                if (tmp_card[i] >= 4)
                    count++;

            return count;
        }
        /// <summary>
        /// 是否能碰牌? 是, 返回 true; 否,返回 false .
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="out_c"></param>
        /// <returns></returns>
        public bool Can_Bump_Cards(int out_c)
        {
            int[] yb = this.HandInCard;
            int[] tmp_card = new int[KnownRemainCard.Length];

            for (int i = 0; i < yb.Length; i++)
                tmp_card[yb[i]]++;
            tmp_card[0] = 0;

            if (tmp_card[out_c] > 1)
                return true;
            return false;
        }


        /// <summary>
        /// 是否能碰吃牌? 是, 返回 true; 否,返回 false .
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="out_c"></param>
        /// <returns></returns>
        public int Can_Hu_OutCard(int out_c)
        {
            int[] tmp14 = new int[14];
            HandInCard.CopyTo(tmp14, 0);
            tmp14[13] = out_c;
            Array.Sort(tmp14);
            AdjusHandCard(tmp14);
            //AdjustHandShunKe(tmp14);
            AdjustWinFlag(out_c, this.azimuth);

            if (If_Hu_Cards(out_c) > 0 || If_Hu_SpecialCards())
            {
                int Score = AdjustHandShunKe_ComputerScore(tmp14);
                string str = RetFanT();
                if (Score >= Mahjong.JiBenHuFen)
                    return Score;
            }
            return 0;
        }

        /// <summary>
        /// 是否能碰吃牌? 是, 返回 true; 否,返回 false .
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="out_c"></param>
        /// <returns></returns>
        public bool Can_Chi_Cards(int out_c)
        {
            if (out_c > 30) return false;
            int[] yb = this.HandInCard;
            int[] tmp31 = new int[KnownRemainCard.Length + 1];

            for (int i = 0; i < yb.Length; i++)
                tmp31[yb[i]]++;
            tmp31[0] = 0;

            if (out_c == 1)
            {
                if (tmp31[2] > 0 && tmp31[3] > 0)
                    return true;
            }
            else if (tmp31[out_c + 1] > 0 && tmp31[out_c + 2] > 0 ||
                tmp31[out_c - 1] > 0 && tmp31[out_c + 1] > 0 ||
                tmp31[out_c - 2] > 0 && tmp31[out_c - 1] > 0)
                return true;
            return false;
        }


        /// <summary>
        /// 碰牌 .
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="in_c"></param>
        /// <returns></returns>返回值：被碰的值，0 则没杠。
        public int Bump_Cards(int in_c)
        {
            int[] yb = this.HandInCard;
            int len = LongOfCardNZ(this.BumpCard);
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] == in_c)
                {
                    this.BumpCard[len] = this.BumpCard[len + 1] = this.BumpCard[len + 2] = in_c;
                    BumpCard.CopyTo(Mahjong.BumpCard4P[azimuth], 0);
                    yb[i] = yb[i + 1] = 0;
                    return in_c;
                }
            return 0;
        }

        /// <summary>
        /// 吃牌 .自动最优“吃”法
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="in_c"></param>
        /// <returns></returns>返回值：被碰的值，0 则没吃。
        public int Chi_Cards(int in_c)
        {
            int ret_c = Chi_Cards(in_c, IndexOfMaxInArray(ChiFa) + 1);
            return ret_c;
        }

        /// <summary>
        /// 吃牌 .指定“吃”法
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="in_c"></param>
        /// <param name="ChiPos">1:A45, 2:3A5, 3:45A</param>
        /// <returns></returns>返回值：被碰的值，0 则没吃。
        public int Chi_Cards(int in_c, int ChiPos)
        {
            if (in_c == 0)
                return 0;
            int[] yb = this.HandInCard;
            int[] tmp31 = new int[KnownRemainCard.Length + 1];
            int ret_c = -1;

            for (int j = 0; j < yb.Length; j++)
                tmp31[yb[j]]++;
            tmp31[0] = 0;
            tmp31[in_c]++;

            int len = LongOfCardNZ(this.BumpCard);
            if (ChiPos == 1)
            {
                tmp31[in_c]--; tmp31[in_c + 1]--; tmp31[in_c + 2]--;
                BumpCard[len] = in_c;
                BumpCard[len + 1] = in_c + 1;
                BumpCard[len + 2] = in_c + 2;
                ret_c = in_c + 1;
            }
            else if (ChiPos == 2)
            {
                tmp31[in_c - 1]--; tmp31[in_c]--; tmp31[in_c + 1]--;
                BumpCard[len] = in_c;
                BumpCard[len + 1] = in_c - 1;
                BumpCard[len + 2] = in_c + 1;
                ret_c = in_c;
            }
            else if (ChiPos == 3)
            {
                tmp31[in_c - 2]--; tmp31[in_c - 1]--; tmp31[in_c]--;
                BumpCard[len] = in_c;
                BumpCard[len + 1] = in_c - 2;
                BumpCard[len + 2] = in_c - 1;
                ret_c = in_c - 1;
            }

            int[] yb2 = new int[HandInCard.Length];
            int loc = 0;
            for (int j = 0; j < tmp31.Length; j++)
                for (int k = 0; k < tmp31[j]; k++)
                    yb2[loc++] = j;
            Array.Clear(HandInCard, 0, HandInCard.Length);
            yb2.CopyTo(HandInCard, 0);
            BumpCard.CopyTo(Mahjong.BumpCard4P[azimuth], 0);
            return ret_c;
        }

        /// <summary>
        /// 吃牌 .指定“吃”法
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="in_c"></param>
        /// <param name="ChiPos">1:A45, 2:3A5, 3:45A</param>
        /// <returns></returns>返回值：被碰的值，0 则没吃。
        public int Chi_Cards(int[] yb, int in_c, int ChiPos)
        {
            if (in_c == 0)
                return 0;
            int[] tmp31 = new int[KnownRemainCard.Length + 1];
            int ret_c = -1;

            for (int j = 0; j < yb.Length; j++)
                tmp31[yb[j]]++;
            tmp31[0] = 0;
            tmp31[in_c]++;

            if (ChiPos == 1)
            {
                tmp31[in_c]--; tmp31[in_c + 1]--; tmp31[in_c + 2]--;
                ret_c = in_c + 1;
            }
            else if (ChiPos == 2)
            {
                tmp31[in_c - 1]--; tmp31[in_c]--; tmp31[in_c + 1]--;
                ret_c = in_c;
            }
            else if (ChiPos == 3)
            {
                tmp31[in_c - 2]--; tmp31[in_c - 1]--; tmp31[in_c]--;
                ret_c = in_c - 1;
            }

            int[] yb2 = new int[yb.Length];
            int loc = 0;
            for (int j = 0; j < tmp31.Length; j++)
                for (int k = 0; k < tmp31[j]; k++)
                    yb2[loc++] = j;

            Array.Sort(yb2);
            yb2.CopyTo(yb, 0);
            return ret_c;
        }

        /// <summary>
        /// 随机产生一个13张的清一色牌组。
        /// </summary>
        /// <param name="yb"></param>num 牌的张数
        public static void random_pure_card(int[] yb, int num)
        {
            //	Random rdm1 = new Random(unchecked((int)DateTime.Now.Ticks)); 
            bool cont = false;
            while (!cont)
            {
                for (int i = 0; i < num; i++) yb[i] = rdm1.Next(1, 10);
                Array.Sort(yb);
                cont = true;
                for (int i = 0; i < num - 4; i++)
                {
                    if (yb[i] == yb[i + 4])
                    {
                        Array.Clear(yb, 0, yb.Length);
                        cont = false;
                        break;
                    }
                }
            }
            Array.Sort(yb);

        }
        static void random_one_nine_card(int[] yb)
        {
            Random rdm1 = new Random(unchecked((int)DateTime.Now.Ticks));
            bool cont = false;
            while (!cont)
            {
                for (int i = 0; i < 6; i++)
                {
                    int ex = rdm1.Next(0, 30);
                    while (ex % 10 == 4 || ex % 10 == 5 || ex % 10 == 6 || ex % 10 == 0)
                        ex = rdm1.Next(1, 30);
                    yb[i] = ex;
                }
                for (int i = 0; i < 7; i++)
                {
                    int ex = rdm1.Next(0, 30);
                    while (!(ex % 10 == 1 || ex % 10 == 9))
                        ex = rdm1.Next(1, 30);
                    yb[i + 6] = ex;
                }
                Array.Sort(yb);
                cont = true;
                for (int i = 0; i <= 10; i++)
                {
                    if (yb[i] == yb[i + 1] && yb[i] == yb[i + 2] && yb[i] == yb[i + 3])
                    {
                        for (int j = 0; j < yb.Length; j++) yb[j] = 0;
                        cont = false;
                        break;
                    }
                }
            }
            Array.Sort(yb);
        }

        /// <summary>
        /// 下一张摸到指定牌的概率
        /// </summary> 
        public double ProbFetchCard(double PRC, double Num)
        {
            double prob = 0;
            if (Num > 0)
                prob = PRC / Num;
            else
                prob = 0;
            return prob;
        }

        public static ushort CardToTile(int card)
        {
            ushort tt = 0;
            if (card <= 0) tt = (ushort)card;
            else if (card < 10) tt = (ushort)(card + 0x10);
            else if (card < 20) tt = (ushort)(card % 10 + 0x20);
            else if (card < 30) tt = (ushort)(card % 10 + 0x30);
            else if (card < 38) tt = (ushort)((card - 29) / 2 + 0x40);
            else if (card < 44) tt = (ushort)((card - 37) / 2 + 0x50);
            return tt;
        }
        //public ushort[] fixed_packs()
        //{
        //    ushort[] pung_packs = new ushort[5];
        //    int pung_cnt = 0;
        //    for (int i = 0; i < 4; ++i)
        //        if (FanCardData.ArrAgang[i] > 0)
        //            pung_packs[pung_cnt++] = (ushort)(
        //                 CardToTile(FanCardData.ArrAgang[i]) | 0B0100001100000000);
        //    for (int i = 0; i < 4; ++i)
        //        if (FanCardData.ArrMgang[i] > 0)
        //            pung_packs[pung_cnt++] = (ushort)(
        //                CardToTile(FanCardData.ArrMgang[i]) | 0B0000001100000000);
        //    for (int i = 0; i < 4; ++i)
        //        if (FanCardData.ArrMke[i] > 0)
        //            pung_packs[pung_cnt++] = (ushort)(
        //                 CardToTile(FanCardData.ArrMke[i]) | 0B0000001000000000);
        //    for (int i = 0; i < 4; ++i)
        //        if (FanCardData.ArrMshun[i] > 0)
        //            pung_packs[pung_cnt++] = (ushort)(
        //                 CardToTile(FanCardData.ArrMshun[i]) | 0B0000000100000000);
        //    return pung_packs;
        //}


        //yb 为14张能胡牌的牌组
        public void AdjusHandCard(int[] yb)
        {
            FanCardData.HandIn31 = new int[FanCardData.HandIn31.Length];
            for (int i = 0; i < yb.Length; i++)
                FanCardData.HandIn31[yb[i]]++;
            FanCardData.HandIn31[0] = 0;

            yb.CopyTo(FanCardData.HandIn14, 0);
            //Array.Sort(FanCardData.HandIn14);
        }

        //yb 为14张能胡牌的牌组
        public void AdjustHandShunKe(int[] yb)
        {
            FanCardData.FullCard = AddFuLuToHandin(yb);
            if (fan_table[(int)FanT.SEVEN_PAIRS] > 0)
                return;

            int[][] JangShunKes = DivideToJiangShunKe(yb);
            int[] JSK = JangShunKes[0];

            int cc = 0;// 手牌获得的顺子
            Array.Clear(FanCardData.ArrAshun, 0, FanCardData.ArrAshun.Length);
            for (int j = 0; j < JSK.Length; j++)
                if (JSK[j] > 200)
                    FanCardData.ArrAshun[cc++] = JSK[j] % 100;
            cc = 0;// 暗刻子数组
            Array.Clear(FanCardData.ArrAke, 0, FanCardData.ArrAke.Length);
            for (int j = 0; j < JSK.Length; j++)
                if (JSK[j] > 100 && JSK[j] < 200)
                    FanCardData.ArrAke[cc++] = JSK[j] % 100;
            for (int j = 0; j < JSK.Length; j++)
                if (JSK[j] > 0 && JSK[j] < 100)
                    FanCardData.Jiang = JSK[j];
        }

        //yb 为14张能胡牌的牌组
        public int AdjustHandShunKe_ComputerScore(int[] yb)
        {
            FanCardData.FullCard = AddFuLuToHandin(yb);
            FanCardData.Jiang = 0;
            Array.Clear(FanCardData.ArrAke, 0, FanCardData.ArrAke.Length);
            Array.Clear(FanCardData.ArrAshun, 0, FanCardData.ArrAshun.Length);
            Array.Clear(fan_table, 0, fan_table.Length);

            if (fan_table[(int)FanT.SEVEN_PAIRS] + fan_table[(int)FanT.GREATER_HONORS_AND_KNITTED_TILES] + fan_table[(int)FanT.LESSER_HONORS_AND_KNITTED_TILES] > 0)
                return Computer_Score_HuCard();

            int retSco = 0;
            int[][] JangShunKes = DivideToJiangShunKe(yb);
            ushort[] max_fan_table = new ushort[(int)FanT.FAN_TABLE_SIZE];

            for (int i = 0; i < JangShunKes.Length; i++)
            {
                int[] JSK = JangShunKes[i];
                int cc = 0;// 手牌获得的顺子
                FanCardData.Jiang = 0;
                Array.Clear(FanCardData.ArrAshun, 0, FanCardData.ArrAshun.Length);
                Array.Clear(FanCardData.ArrAke, 0, FanCardData.ArrAke.Length);
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 200)
                        FanCardData.ArrAshun[cc++] = JSK[j] % 100;
                cc = 0;// 暗刻子数组
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 100 && JSK[j] < 200)
                        FanCardData.ArrAke[cc++] = JSK[j] % 100;
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 0 && JSK[j] < 100)
                        FanCardData.Jiang = JSK[j];
                int sco = Computer_Score_HuCard();
                if (sco > retSco)
                {
                    retSco = sco;
                    fan_table.CopyTo(max_fan_table, 0);
                }
            }
            max_fan_table.CopyTo(fan_table, 0);
            return retSco;
        }

        public void AdjustWinFlag(int winT, int ha)
        {
            FanCardData.winCard = winT;

            int[] tmp31 = new int[KnownRemainCard.Length];
            KnownRemainCard.CopyTo(tmp31, 0);
            for (int j = 0; j < HandInCard.Length; j++)
                tmp31[HandInCard[j]]++;
            tmp31[0] = 0;

            FanCardData.wf4TH_TILE = tmp31[winT] == 0 ? 1 : 0;
            FanCardData.wfDISCARD = Mahjong.out_card > 0 ? 1 : 0;
            FanCardData.wfSELF_DRAWN = Mahjong.out_card > 0 ? 0 : 1;
            if (ganged)
                FanCardData.wfABOUT_KONG = 1;
            else
                FanCardData.wfABOUT_KONG = 0;

            //妙手回春、海底捞月
            //int next__azimuth = Mahjong.in_card > 0 ? (this.azimuth + 1) % 4 : (Mahjong.old_azimuth + 1) % 4;
            //FanCardData.wfWALL_LAST = myBCP[next__azimuth] >= 21 ? 1 : 0;    //botzone比赛时、JCAI复盘
            int bcp = myBCP[(ha + 1) % 4];
            FanCardData.wfWALL_LAST = Mahjong.AllBottomCard[bcp] == 0 ? 1 : 0;        //本机运行时
        }
        public void AdjustWinFlag(int[] inCards)
        {
            FanCardData.wf4TH_TILE = 0;
            if (inCards.Length > 3)
                return;
            int[] tmp31 = new int[KnownRemainCard.Length];
            KnownRemainCard.CopyTo(tmp31, 0);
            for (int j = 0; j < HandInCard.Length; j++)
                tmp31[HandInCard[j]]++;
            tmp31[in_card]++;
            tmp31[0] = 0;
            for (int i = 0; i < inCards.Length; i++)
                if (tmp31[inCards[i]] == 1)
                    FanCardData.wf4TH_TILE = 1;
        }

        public void FanCardDataClear()//1201
        {
            Array.Clear(FanCardData.HandIn31, 0, FanCardData.HandIn31.Length);
            Array.Clear(FanCardData.HandIn14, 0, FanCardData.HandIn14.Length);
            Array.Clear(FanCardData.FullCard, 0, FanCardData.FullCard.Length);
            Array.Clear(FanCardData.ArrAke, 0, FanCardData.ArrAke.Length);
            Array.Clear(FanCardData.ArrAshun, 0, FanCardData.ArrAshun.Length);

            FanCardData.winCard = FanCardData.Jiang = -1;//202205
            FanCardData.wf4TH_TILE = FanCardData.wfDISCARD = FanCardData.wfSELF_DRAWN =
                FanCardData.wfWALL_LAST = FanCardData.wfABOUT_KONG = -1;
        }

        public double ConvertScore(int[] yb, double sco, int[] inlist = null, int[] ic = null, int Level = 0)
        {
            int[] tmp = new int[ic.Length];
            for (int i = 0; i < ic.Length; i++) tmp[i] = inlist[ic[i]];
            int lenABC = LongOfCardNZ(Mahjong.AllBottomCard);
            int lenYB = LongOfCardNZ(yb);
            bool thistQQR = false, thistBQR = false, thistMQQ = false, thistQZ = false, thistDD = false, thistBZ = false, thisZM = false;
            //全求人
            if (sco < 8 && Level < 3 && lenYB < 3 * 3 && fan_table[(int)FanT.MELDED_HAND] == 0
                && lenABC > Level * 4)
            {
                fan_table[(int)FanT.MELDED_HAND] = 1;
                sco += 6; thistQQR = true;
            }
            // 不求人 
            if (sco < 8 && Level < 3 && lenYB >= 13 && fan_table[(int)FanT.FULLY_CONCEALED_HAND] == 0
                && lenABC < 28)
            {
                fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 1;
                sco += 4; thistBQR = true;
            }
            //门前清
            if (sco < 8 && Level < 3 && lenYB >= 13 && fan_table[(int)FanT.CONCEALED_HAND] == 0
                && fan_table[(int)FanT.FULLY_CONCEALED_HAND] == 0 && lenABC < 28)
            {
                fan_table[(int)FanT.CONCEALED_HAND] = 1;
                sco += 2; thistMQQ = true;
            }
            // 嵌张
            if (sco < 8 && fan_table[(int)FanT.CLOSED_WAIT] == 0 && lenABC > Level * 4)
                if (IfExitKanZhang(tmp) > 0)
                { sco++; thistQZ = true; }
            //边张
            if (sco < 8 && fan_table[(int)FanT.CLOSED_WAIT] == 0 && lenABC > Level * 4)
                if (IfExitBanZhang(tmp) > 0)
                { sco++; thistBZ = true; }
            //单钓
            if (sco < 8 && fan_table[(int)FanT.CLOSED_WAIT] == 0 && lenABC > Level * 4)
            { sco += IfExitDanDiao(tmp); thistDD = true; }
            // 全求人、边张、嵌张时不计单钓将
            if (fan_table[(int)FanT.CLOSED_WAIT] + fan_table[(int)FanT.EDGE_WAIT] + fan_table[(int)FanT.MELDED_HAND] > 0)
                if (fan_table[(int)FanT.SINGLE_WAIT] > 0)
                { fan_table[(int)FanT.SINGLE_WAIT] = 0; sco -= 1; thistDD = false; }
            //自摸 
            if (sco < 8 && fan_table[(int)FanT.SELF_DRAWN] == 0 && fan_table[(int)FanT.FULLY_CONCEALED_HAND] == 0 && lenABC > Level * 4)
            { fan_table[(int)FanT.SELF_DRAWN] = 1; sco += 1; thisZM = true; }

            double retSco = sco;
            if (sco < Mahjong.JiBenHuFen)
                return retSco = 0;

            if (Level < 1) Level = 1;
            if (thistQQR)
                retSco /= 4;
            if (thistBQR)
                retSco /= 3;
            if (thistMQQ)
                retSco /= 2;
            if (thistQZ)
                retSco /= Level;
            if (thistDD)
                retSco /= Level;
            if (thistBZ)
                retSco /= Level;
            if (thisZM)
                retSco /= Level;
            if (FanCardData.wf4TH_TILE > 0)
                retSco /= Level;
            if (sco < 8 && (fan_table[(int)FanT.TWO_CONCEALED_PUNGS] > 0 || fan_table[(int)FanT.THREE_CONCEALED_PUNGS] > 0 || fan_table[(int)FanT.FOUR_CONCEALED_PUNGS] > 0))
                retSco /= Gap2AnKe(yb);

            //考虑吃、碰会增加胡牌机会
            int[] Ashun = new int[4];
            int[] Ake = new int[4];
            int Jiang = 0;
            double coff = 1;
            int[] inCard = new int[ic.Length];
            for (int j = 0; j < ic.Length; j++)
                inCard[j] = inlist[ic[j]];
                         
            int[][] JangShunKes = DivideToJiangShunKe(yb);
            for (int i = 0; i < JangShunKes.Length; i++)
            {
                int[] JSK = JangShunKes[i];
                int cc = 0;// 手牌获得的顺子
                FanCardData.Jiang = 0;
                Array.Clear(Ashun, 0, Ashun.Length);
                Array.Clear(Ake, 0, Ake.Length);
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 200)
                        Ashun[cc++] = JSK[j] % 100;
                cc = 0;// 暗刻子数组
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 100 && JSK[j] < 200)
                        Ake[cc++] = JSK[j] % 100;
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 0 && JSK[j] < 100)
                        Jiang = JSK[j];

                //碰会增加胡牌机会
                for (int j = 0; j < inCard.Length; j++)
                    if (Ake.Contains(inCard[j]) && CountCardfArray(inCard, inCard[j])== 1)
                        coff *= 3;
                //吃会增加胡牌机会
                for (int j = 0; j < Ashun.Length; j++)
                    if (Ashun[j] > 0)
                        for (int k = 0; k < inCard.Length; k++)
                            if (Ashun[j] == inCard[k] || Ashun[j] == inCard[k] - 1 || Ashun[j] == inCard[k] + 1)
                            {
                                if (k > 0 && inCard[k] == inCard[k - 1])//两个进张一样，只考虑一个
                                    continue;
                                else
                                {
                                    int cccc = 0;
                                    if (inCard.Contains(Ashun[j])) cccc++;
                                    if (inCard.Contains(Ashun[j] - 1)) cccc++;
                                    if (inCard.Contains(Ashun[j] + 1)) cccc++;
                                    if (cccc == 1)//一坎顺子时，相关进张只有一个时才能增加胡的机会
                                         coff *= 2;
                                    if (cccc > 1)
                                    { }
                                }
                            }
               
            }
            string str = RetFanT();
            if (coff > 1)
            { }
            return retSco * coff;
        }
        //0：保留， 1：打出 ,  

        public double Gap2AnKe(int[] yb)
        {
            int gap = 0, cc = 0;
            for (int i = 0; i < yb.Length - 2; i++)
                if (yb[i] > 0 && yb[i] == yb[i + 2])
                {
                    cc++;
                    i += 2;
                }
            if (cc <= 1)
                return 1;
            for (int i = 0; i < yb.Length - 2; i++)
                if (yb[i] > 0 && yb[i] == yb[i + 2])
                {
                    gap += Math.Max(0, CountCardfArray(yb, yb[i]) - CountCardfArray(this.HandInCard, yb[i]));
                    i += 2;
                }

            double coff = Math.Pow(2, gap);
            if (gap > 1)
            { }
            return coff;
        }

        public int[] GenInListNorm(int[] yb, int level = 3, string Father = "")
        {
            if (level < 3)
                return Mahjong.Card34;

            int[] tmp14 = new int[yb.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            int[] List = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    yb31[yb[i]]++;

            for (int i = 0; i < List.Length - 1; i++)
            {
                List[i] = 1;
                if (i % 10 == 0 || i > 30 && i % 2 == 0)
                    List[i] = 0;
            }

            int ct = 0;
            for (int i = 31; i < yb31.Length; i += 2) //没有的字牌不进张
                if (yb31[i] == 0)
                { List[i] = 0; ct++; }
            if (ct == 7)//手上没有字牌
                for (int i = 31; i < KnownRemainCard.Length; i++)
                    if (KnownRemainCard[i] > 2)
                    { List[i] = 1; break; }
            for (int i = 1; i < KnownRemainCard.Length - 1; i++) //断张不进张
                if (KnownRemainCard[i] == 0 && List[i] > KnownRemainCard[i])
                    List[i] = 0;
            int[] single = new int[20];
            ct = 0;
            if (level >= 6 || Father.Length > 0 && level >= 5)//分别留下一张最优的风、箭牌
            {
                int maxInd = 0, max = 0;
                for (int i = 31; i < 38; i++)
                    if (yb31[i] == 1 && KnownRemainCard[i] > max)
                    { maxInd = i; max = KnownRemainCard[i]; }
                for (int i = 31; i < 38; i++)
                    if (yb31[i] == 1 && i != maxInd)
                        List[i] = 0;
                maxInd = max = 0;
                for (int i = 39; i < 44; i++)
                    if (yb31[i] == 1 && KnownRemainCard[i] > max)
                    { maxInd = i; max = KnownRemainCard[i]; }
                for (int i = 39; i < 44; i++)
                    if (yb31[i] == 1 && i != maxInd)
                        List[i] = 0;
            }

            if (Father == "ZHL")
            {
                for (int i = 0; i < 3; i++)//前后没有，不进张 
                    for (int j = 1; j <= 9; j++)
                        if (yb31[j - 1 + i * 10] + yb31[j + i * 10] + yb31[j + 1 + i * 10] == 0)
                            List[j + i * 10] = 0;
                for (int i = 0; i < 3; i++)//00100型，只进本张 
                {
                    if (yb31[1 + i * 10] == 1 && yb31[2 + i * 10] + yb31[3 + i * 10] == 0)//100
                    { List[2 + i * 10] = 0; single[ct++] = 1 + i * 10; }
                    if (yb31[9 + i * 10] == 1 && yb31[7 + i * 10] + yb31[8 + i * 10] == 0)//009
                    { List[8 + i * 10] = 0; single[ct++] = 9 + i * 10; }
                    for (int j = 2; j <= 8; j++)
                        if (yb31[j + i * 10] == 1 &&
                            yb31[j - 2 + i * 10] + yb31[j - 1 + i * 10] + yb31[j + 1 + i * 10] + yb31[j + 2 + i * 10] == 0)
                        {
                            List[j - 1 + i * 10] = List[j + 1 + i * 10] = 0;
                            single[ct++] = j + i * 10;
                        }
                }
                for (int i = 31; i < 44; i++)//统计孤张00100
                    if (yb31[i] == 1)
                        single[ct++] = i;
                Array.Sort(single);
                Array.Reverse(single);
                int num = LongOfCardNZ(single);
                for (int i = 0; i < single.Length; i++)//孤张去一半
                    if (single[i] > 0)
                    {
                        if (i >= num / 3)
                            break;
                        List[single[i]] = 0;
                    }

                for (int j = 2; j <= 28; j++)//01010型，只进中间张，两边张不进                     
                    if (yb31[j - 1] * yb31[j + 1] == 1 && yb31[j - 2] + yb31[j] + yb31[j + 2] == 0)
                        List[j - 1] = List[j + 1] = 0;

                for (int j = 1; j < 30; j++)
                {
                    if (yb31[j - 1] + yb31[j + 2] == 0 && yb31[j] * yb31[j + 1] == 1)
                        List[j] = List[j + 1] = 0; //0110型，中间2张不进
                    else if (yb31[j - 1] + yb31[j + 3] == 0 && yb31[j] * yb31[j + 1] * yb31[j + 2] == 1)//01110型，不进
                        List[j] = List[j + 1] = List[j + 2] = 0;
                    else if (yb31[j - 1] * yb31[j] * yb31[j + 1] * yb31[j + 2] == 1)
                        List[j] = List[j + 1] = 0; //1111型，中间2张不进
                }
            }

            if (Father == "Eval" && level == 6)
                for (int i = 0; i < 3; i++)//前后三张没有，不进张
                {
                    if (yb31[1 + i * 10] + yb31[2 + i * 10] + yb31[3 + i * 10] + yb31[4 + i * 10] == 0)//1
                        List[1 + i * 10] = 0;
                    if (yb31[1 + i * 10] + yb31[2 + i * 10] + yb31[3 + i * 10] + yb31[4 + i * 10] + yb31[5 + i * 10] == 0)//2
                        List[2 + i * 10] = 0;
                    if (yb31[6 + i * 10] + yb31[7 + i * 10] + yb31[8 + i * 10] + yb31[9 + i * 10] == 0)//9
                        List[9 + i * 10] = 0;
                    if (yb31[5 + i * 10] + yb31[6 + i * 10] + yb31[7 + i * 10] + yb31[8 + i * 10] + yb31[9 + i * 10] == 0)//8
                        List[8 + i * 10] = 0;

                    for (int j = 3; j <= 7; j++)//3 4 5 6 7 
                        if (yb31[j - 3 + i * 10] + yb31[j - 2 + i * 10] + yb31[j - 1 + i * 10] + yb31[j + i * 10]
                            + yb31[j + 1 + i * 10] + yb31[j + 2 + i * 10] + yb31[j + 3 + i * 10] == 0)
                            List[j + i * 10] = 0;
                }
            else if (Father == "Eval" && level >= 7)
                for (int i = 0; i < 3; i++)//前后2张没有，不进张
                {
                    if (yb31[1 + i * 10] + yb31[2 + i * 10] + yb31[3 + i * 10] == 0)//1
                        List[1 + i * 10] = 0;
                    if (yb31[7 + i * 10] + yb31[8 + i * 10] + yb31[9 + i * 10] == 0)//9
                        List[9 + i * 10] = 0;

                    for (int j = 2; j <= 8; j++)//2 3 4 5 6 7 8
                        if (yb31[j - 2 + i * 10] + yb31[j - 1 + i * 10] + yb31[j + i * 10] + yb31[j + 1 + i * 10]
                            + yb31[j + 2 + i * 10] == 0)
                            List[j + i * 10] = 0;
                }


            int cc = LongOfCardNZ(List);
            int[] inList = new int[cc];
            cc = 0;
            for (int i = 0; i < List.Length; i++)
                if (List[i] > 0)
                    inList[cc++] = i;
            return inList;
        }

        public int[][] GenOutListZuHeLongs(int[] yb)
        {
            int[] tmp1 = new int[yb.Length];
            int[] tmp2 = new int[yb.Length];
            int[] lap = new int[yb.Length];
            int[][] JinKeys;
            if (LongOfCardNZ(yb) < 10) return null;
            //去筋
            int minGap = 0;
            yb.CopyTo(tmp1, 0);
            MaxZHL147_258_369Keys(tmp1, out JinKeys, out minGap);
            int[][] OutList = new int[JinKeys.Length][];
            for (int v = 0; v < JinKeys.Length; v++)
            {
                OutList[v] = new int[14];
                yb.CopyTo(tmp1, 0);
                for (int k = 0; k < tmp1.Length; k++)
                {
                    if (tmp1[k] == 0 || tmp1[k] > 30 || k < tmp1.Length - 1 && tmp1[k] == tmp1[k + 1]) continue;
                    if (tmp1[k] % 10 % 3 == JinKeys[v][tmp1[k] / 10] % 3)
                        tmp1[k] = 0;
                }
                tmp1.CopyTo(OutList[v], 0);
            }
            return OutList;
        }


        public int[] GenOutListZuHeLong(int[] yb, int level = 3, string Father = "")
        {
            int[] tmp1 = new int[yb.Length];
            int[] tmp2 = new int[yb.Length];
            int[] OutList = new int[yb.Length];
            int[] lap = new int[yb.Length];
            int[] JinKey;
            int Num = 7;

            yb.CopyTo(tmp1, 0);
            Array.Sort(tmp1);

            if (LongOfCardNZ(yb) < 10) return OutList;
            if (level < 3) return yb;
            else if (level == 3) Num = 7;
            else if (level > 3) Num = level + 3;
            if (Father != "Eval")
                Num += 1;

            //去筋
            int maxJin = 0;
            MaxZHL147_258_369Key(tmp1, out JinKey, out maxJin);
            for (int k = 0; k < tmp1.Length; k++)
            {
                if (tmp1[k] == 0 || tmp1[k] > 30 || k < tmp1.Length - 1 && tmp1[k] == tmp1[k + 1]) continue;
                if (tmp1[k] % 10 % 3 == JinKey[tmp1[k] / 10] % 3)
                    tmp1[k] = 0;
            }
            tmp1.CopyTo(tmp2, 0);
            Array.Sort(tmp2);

            //对应回去
            int[] out31 = new int[KnownRemainCard.Length + 1];
            for (int i = 0; i < tmp2.Length; i++)
                if (tmp2[i] > 0)
                    out31[tmp2[i]]++;
            int[] lap1 = CalculateLapFormMinResidual(out31);
            lap1.CopyTo(lap, lap.Length - lap1.Length);

            for (int i = 0; i < yb.Length; i++)
                for (int j = 0; j < tmp2.Length; j++)
                    if (tmp2[j] > 0 && tmp1[i] == tmp2[j])
                    {
                        OutList[i] = lap[j];
                        break;
                    }

            while (LongOfCardNZ(OutList) > Num)
            {
                int min_ii = IndexOfMinNZInArray(OutList);
                OutList[min_ii] = 0;
            }
            for (int i = 0; i < OutList.Length; i++)
                if (OutList[i] > 0)
                    OutList[i] = yb[i];
            return OutList;
        }

        public int[][] GenInListZuHeLongs(int[] yb, int level = 3, string Father = "")
        {
            int[] tmp = new int[yb.Length];
            int[][] JinKeys;
            yb.CopyTo(tmp, 0);
            Array.Sort(tmp);

            int maxJin = 0;
            MaxZHL147_258_369Keys(tmp, out JinKeys, out maxJin);
            int[][] InLists = new int[JinKeys.Length][];
            for (int k = 0; k < JinKeys.Length; k++)
            {
                int[] tmp1 = new int[yb.Length];
                int[] tmp2 = new int[yb.Length];
                int[] yb31 = new int[KnownRemainCard.Length];
                int[] jin31 = new int[KnownRemainCard.Length];
                InLists[k] = new int[KnownRemainCard.Length];
                for (int i = 0; i < yb.Length; i++)
                    if (yb[i] > 0)
                        yb31[yb[i]]++;

                //需要的筋 
                for (int i = 0; i < 3; i++)
                    for (int j = 1; j <= 9; j++)
                        if (yb31[i * 10 + j] == 0 && JinKeys[k][i] % 3 == j % 3)
                            jin31[i * 10 + j] = 1;
                //把手牌的筋去除
                for (int i = 0; i < 3; i++)
                    for (int j = 1; j <= 9; j++)
                        if (yb31[i * 10 + j] > 0 && JinKeys[k][i] % 3 == j % 3)
                            yb31[i * 10 + j]--;

                //生成牌组
                int cc = 0;
                for (int i = 0; i < yb31.Length; i++)
                    for (int j = 0; j < yb31[i]; j++)
                        tmp1[cc++] = i;
                cc = 0;
                for (int i = 0; i < jin31.Length; i++)
                    if (jin31[i] > 0)
                        tmp2[cc++] = i;

                InLists[k] = GenInListNorm(tmp1, level, Father = "ZHL");
                InLists[k] = JoinArray(InLists[k], tmp2);
            }
            return InLists;
        }

        public int[] GenInListZuHeLong(int[] yb, int level = 3, string Father = "")
        {
            int[] tmp = new int[yb.Length];
            int[] tmp1 = new int[yb.Length];
            int[] tmp2 = new int[yb.Length];
            int[] InList = new int[KnownRemainCard.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            int[] jin31 = new int[KnownRemainCard.Length];
            int[] JinKey;
            yb.CopyTo(tmp, 0);
            Array.Sort(tmp);

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    yb31[yb[i]]++;

            int maxJin = 0;
            MaxZHL147_258_369Key(tmp, out JinKey, out maxJin);
            //需要的筋 
            for (int i = 0; i < 3; i++)
                for (int j = 1; j <= 9; j++)
                    if (yb31[i * 10 + j] == 0 && JinKey[i] % 3 == j % 3)
                        jin31[i * 10 + j] = 1;
            //把手牌的筋去除
            for (int i = 0; i < 3; i++)
                for (int j = 1; j <= 9; j++)
                    if (yb31[i * 10 + j] > 0 && JinKey[i] % 3 == j % 3)
                        yb31[i * 10 + j]--;

            //生成牌组
            int cc = 0;
            for (int i = 0; i < yb31.Length; i++)
                for (int j = 0; j < yb31[i]; j++)
                    tmp1[cc++] = i;
            cc = 0;
            for (int i = 0; i < jin31.Length; i++)
                if (jin31[i] > 0)
                    tmp2[cc++] = i;

            InList = GenInListNorm(tmp1, level, Father = "ZHL");
            InList = JoinArray(InList, tmp2);
            return InList;
        }


        public int[] GenOutListBuKao(int[] yb, int level = 3)
        {
            int Num = 0;
            if (level < 3 || LongOfCardNZ(yb) <= 6)
                return yb;
            else if (level == 3) Num = 7;
            else if (level == 4 || level == 5) Num = 6;
            else if (level > 5) Num = level;

            int[] tmp = new int[yb.Length];
            int[,] jinFB = new int[3, 3];//筋 
            int[,] twt = new int[6, 3];
            int[] OutList = new int[yb.Length];
            yb.CopyTo(tmp, 0);
            Array.Sort(tmp);

            int[] JinKey;
            int maxJin = 0;
            int[,] jinMax = MaxQBK147_258_369Key(tmp, out JinKey, out maxJin);
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0 && yb[i] < 30)
                {
                    OutList[i] = 10;
                    OutList[i] -= jinMax[yb[i] / 10, yb[i] % 10 % 3];
                }
            //最大筋 
            for (int i = 0; i < JinKey.Length; i++)
                for (int j = 0; j < yb.Length; j++)
                    if (yb[j] < 30 && i % 3 == yb[j] / 10 && yb[j] % 10 % 3 == JinKey[i] % 3)
                        OutList[j] = 0;

            for (int i = 0; i < yb.Length - 1; i++)
                for (int j = i + 1; j < yb.Length; j++)
                    if (yb[i] == yb[j])
                        OutList[j] = 10;

            while (LongOfCardNZ(OutList) > Num)
            {
                int min_ii = IndexOfMinNZInArray(OutList);
                OutList[min_ii] = 0;
            }
            for (int i = 0; i < OutList.Length; i++)
                if (OutList[i] > 0)
                    OutList[i] = yb[i];
            return OutList;
        }

        public int[][] GenOutListBuKaos(int[] yb)
        {
            int[] tmp1 = new int[yb.Length];
            int[,] jinFB = new int[3, 3];//筋  

            yb.CopyTo(tmp1, 0);
            Array.Sort(tmp1);

            int[][] JinKeys;
            int maxJin = 0;
            MaxQBK147_258_369Keys(tmp1, out JinKeys, out maxJin);
            int[][] OutList = new int[JinKeys.Length][];
            for (int v = 0; v < JinKeys.Length; v++)
            {
                OutList[v] = new int[14];
                yb.CopyTo(tmp1, 0);
                for (int k = 0; k < tmp1.Length; k++)
                {
                    if (tmp1[k] == 0 || tmp1[k] > 30 || k < tmp1.Length - 1 && tmp1[k] == tmp1[k + 1]) continue;
                    if (tmp1[k] % 10 % 3 == JinKeys[v][tmp1[k] / 10] % 3)
                        tmp1[k] = 0;
                }
                for (int k = 1; k < tmp1.Length; k++)
                    if (tmp1[k] > 30 && yb[k] != yb[k - 1])
                        tmp1[k] = 0;
                tmp1.CopyTo(OutList[v], 0);
            }
            return OutList;
        }
        public int[] GenInListBuKao(int[] yb, int Num = 24)
        {
            int[] tmp = new int[yb.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            int[] jin31 = new int[KnownRemainCard.Length];
            int[] JinKey;
            int[][] JinKeys;
            yb.CopyTo(tmp, 0);
            Array.Sort(tmp);

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    yb31[yb[i]]++;

            int maxJin = 0;
            MaxQBK147_258_369Key(tmp, out JinKey, out maxJin);
            MaxQBK147_258_369Keys(tmp, out JinKeys, out maxJin);
            //需要的筋 
            for (int i = 0; i < 3; i++)
                for (int j = 1; j <= 9; j++)
                    if (yb31[i * 10 + j] == 0 && JinKey[i] % 3 == j % 3)
                        jin31[i * 10 + j] = 1;
            //需要的字牌
            for (int i = 30; i < jin31.Length; i++)
                if (yb31[i] == 0 && i % 2 == 1)
                    jin31[i] = 1;

            //生成牌组
            int cc = 0;
            for (int i = 0; i < jin31.Length; i++)
                if (jin31[i] > 0)
                    cc++;
            int[] tmp1 = new int[cc];
            cc = 0;
            for (int i = 0; i < jin31.Length; i++)
                if (jin31[i] > 0)
                    tmp1[cc++] = i;

            return tmp1;
        }
        public int[][] GenInListBuKaos(int[] yb)
        {
            int[] tmp = new int[yb.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            int[][] JinKeys;
            yb.CopyTo(tmp, 0);
            Array.Sort(tmp);

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    yb31[yb[i]]++;

            int maxJin = 0;
            MaxQBK147_258_369Keys(tmp, out JinKeys, out maxJin);
            int[][] InLists = new int[JinKeys.Length][];
            for (int k = 0; k < JinKeys.Length; k++)
            {
                int[] jin31 = new int[KnownRemainCard.Length];
                //需要的筋 
                for (int i = 0; i < 3; i++)
                    for (int j = 1; j <= 9; j++)
                        if (yb31[i * 10 + j] == 0 && JinKeys[k][i] % 3 == j % 3)
                            jin31[i * 10 + j] = 1;
                //需要的字牌
                for (int i = 30; i < jin31.Length; i++)
                    if (yb31[i] == 0 && i % 2 == 1)
                        jin31[i] = 1;

                //生成牌组
                int cc = 0;
                for (int i = 0; i < jin31.Length; i++)
                    if (jin31[i] > 0)
                        cc++;
                InLists[k] = new int[cc];
                cc = 0;
                for (int i = 0; i < jin31.Length; i++)
                    if (jin31[i] > 0)
                        InLists[k][cc++] = i;
            }
            return InLists;
        }

        public int[] GenOutList13Yao(int[] yb, int Num = 7, int level = 3)
        {
            int[] OutList = new int[yb.Length];
            int[] lap = new int[yb.Length];
            int[] lap1 = new int[yb.Length];

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] < 30 && yb[i] % 10 % 8 != 1)
                    lap[i]++;

            for (int i = 0; i < yb.Length - 1; i++)
                for (int j = i + 1; j < yb.Length; j++)
                    if (yb[i] == yb[j])
                        lap[j]++;

            lap.CopyTo(lap1, 0);
            for (int i = 0; i < lap1.Length; i++)
                if (lap1[i] == 0)
                    OutList[i] = 0;
                else
                    OutList[i] = yb[i];
            return OutList;
        }
        public int[] GenInList13Yao(int[] yb, int Num = 24)
        {
            int[] InList = new int[KnownRemainCard.Length];
            for (int i = 0; i < InList.Length; i++)
                if (i < 30 && i % 10 % 8 == 1 || i > 30 && i % 2 != 0)
                    InList[i]++;

            int cc = 0;
            int[] tmp = new int[LongOfCardNZ(InList)];

            for (int i = 0; i < InList.Length; i++)
                if (InList[i] > 0)
                    tmp[cc++] = i;
            return tmp;
        }
        public delegate double[] HuInOutMethod(int[] yb, int[] OutList, int[] InList);

        //用前n-1行记录各类胡牌信息，第1列为分数，第2列为频数，第3列为深度。之后14列为手牌。
        //把不够番数的胡牌记录在特定位置, 最后一行的相应位置 
        public string CreateAllFanTabInfo(int[] yb)//int ha,
        {
            int[] tmp14 = new int[yb.Length];
            int lever = 0, showLines = 20;
            string str = "";
            yb.CopyTo(tmp14, 0);

            int[] gap = new int[5];
            int[] JinZHLKey, JinQBKKey; int jinNum, jinNum1;
            gap[0] = MaxNormHuGap(yb, (14 - Mahjong.LongOfCardNZ(yb)) / 3);
            gap[1] = 14 - MaxZuHeLongKey(yb, out JinZHLKey, out jinNum);
            gap[2] = 14 - MaxQuanBuKaoKey(yb, out JinQBKKey, out jinNum1);
            gap[3] = 14 - Max13YaoKey(yb);
            gap[4] = Max7PairKey(yb);

            str += "gap基组不幺七" + gap[0] + "-" + gap[1] + "-" + gap[2] + "-" + gap[3] + "-" + gap[4];
            if (Mahjong.MinOfArray(gap) == gap[2] || Mahjong.MinOfArray(gap) == gap[3])
                str += "   组合龙 经：" + JinQBKKey[0] + "-" + JinQBKKey[1] + "-" + JinQBKKey[2] + " ";
            str += " 山牌剩" + Mahjong.LongOfCardNZ(Mahjong.AllBottomCard) + " ";
            str += (Mahjong.out_card > 0 ? "out=" + Mahjong.out_card : "in=" + Mahjong.in_card) + "\nyb=";
            for (int i = 0; i < yb.Length; i++) str += yb[i] + " ";
            str += "\n";

            //冒泡排序算法
            ushort[] tmp = new ushort[(int)FanT.FAN_TABLE_SIZE];//0219
            for (int i = 1; i < AllFanTab.Length - 1; i++)
            {
                if (AllFanTab[i][(int)FanT.Score] + AllFanTab[i][(int)FanT.InOutLever] == 0
                     || AllFanTab[i][0] == 99)
                    break;
                for (int j = 1; j <= AllFanTab.Length - i; j++)
                {
                    if (AllFanTab[j - 1][(int)FanT.HuClass] == AllFanTab[j][(int)FanT.HuClass] &&
                        AllFanTab[j - 1][(int)FanT.InOutLever] == AllFanTab[j][(int)FanT.InOutLever] &&
                        AllFanTab[j - 1][(int)FanT.Frequency] < AllFanTab[j][(int)FanT.Frequency])
                    {
                        AllFanTab[j - 1].CopyTo(tmp, 0);
                        AllFanTab[j].CopyTo(AllFanTab[j - 1], 0);
                        tmp.CopyTo(AllFanTab[j], 0);
                    }
                }
            }

            int cc = 0; int itemNum = 0, lineNum = 0; ushort oldClass = 9;
            for (int k = 0; k < AllFanTab.Length - 1; k++)
            {
                if (lineNum > showLines) break;         //只显示5行
                ushort[] FanTab = AllFanTab[k];
                if (FanTab[(int)FanT.Score] > 0 && FanTab[(int)FanT.Score] < 99)
                {
                    if (oldClass != FanTab[(int)FanT.HuClass])
                    {
                        oldClass = FanTab[(int)FanT.HuClass];
                        itemNum = 0;
                    }
                    if (++itemNum > 4)
                        continue;
                    lineNum++;
                    ushort sco = FanTab[(int)FanT.Score];
                    lever = FanTab[(int)FanT.InOutLever];
                    if (itemNum == 1)
                        str += "L" + lever;
                    else
                        str += " " + lever;
                    str += " 频率" + FanTab[(int)FanT.Frequency].ToString().PadRight(2);
                    str += " 番" + sco.ToString().PadRight(2);
                    str += " ";

                    for (int i = (int)FanT.BIG_FOUR_WINDS; i < (int)FanT.FLOWER_TILES; i++)
                        if (FanTab[i] * Mahjong.fan_value_table[i] > 0)
                            str += Mahjong.fan_name[i] + FanTab[i] * Mahjong.fan_value_table[i] + " ";
                    //只显示3个胡牌信息，七对、清一色。没必要把所有信息都显示 

                    for (int i = (int)FanT.YB0; i < (int)FanT.YB0 + tmp14.Length; i++)
                        tmp14[i - (int)FanT.YB0] = FanTab[i];
                    Array.Sort(tmp14);
                    str += "{" + Mahjong.Cards2Str(tmp14);
                    str += ":" + LongOfCardNZ(tmp14) + "}";
                    //打印吃、碰、杠牌
                    string str9 = "";
                    for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                    {
                        if (FanCardData.ArrMke[i] > 0)
                            str9 += "P" + FanCardData.ArrMke[i].ToString().PadRight(2);
                        if (FanCardData.ArrMshun[i] > 0)
                            str9 += "C" + FanCardData.ArrMshun[i].ToString().PadRight(2);
                        if (FanCardData.ArrAgang[i] > 0)
                            str9 += "G" + FanCardData.ArrAgang[i].ToString().PadRight(2);
                        if (FanCardData.ArrMgang[i] > 0)
                            str9 += "G" + FanCardData.ArrMgang[i].ToString().PadRight(2);
                    }
                    if (str9.Length > 0) str += "<" + str9 + ">";
                    str += "\n";
                }
                else if (FanTab[(int)FanT.Score] == 99)
                    str += "FastHu:   ucn基本型" + FanTab[1] + " 组合龙" + FanTab[2] +
                        " 全不靠" + FanTab[3] + " 十三幺" + FanTab[4] + "\n";
            }

            cc = 0;
            for (int k = 0; k < AllFanTab.Length - 1; k++)
                if (AllFanTab[k][(int)FanT.Score] > 0)
                    cc++;
            if (cc > showLines)
                str += "共有" + cc + "可胡数!\n";
            //把不够番数的胡牌记录在了特定位置, 最后一行的相应位置，现在显示 
            int pos = AllFanTab.Length - 1;
            for (int i = 1; i < 8; i++)
                if (AllFanTab[pos][i] > 0)
                {
                    int L_lable = i - 1;
                    if (LongOfCardNZ(yb) % 3 == 1)//13张时
                        L_lable++;
                    if (str.Length < 6)
                        str += "深度" + L_lable + "时，未够番数胡牌次数" + AllFanTab[pos][i] + "\n";
                    else
                        str += "深度" + L_lable + ", 未胡数" + AllFanTab[pos][i] + "    \n";
                }
            return str;
        }

        //用前n-1行记录各类胡牌信息，第1列为分数，第2列为频数，第3列为深度。之后14列为手牌。
        //把不够番数的胡牌记录在特定位置, 最后一行的相应位置 

        public void RecordHuFanInfo(int[] yb, int level, int HuClass)
        {
            int sco = 0;
            for (int tt = 0; tt < fan_table.Length; tt++)
                sco += fan_table[tt] * fan_value_table[tt];
            if (sco < Mahjong.JiBenHuFen - 1)
            {
                //把不够番数的胡牌记录在特定位置, 最后一行的相应位置  
                int pos = AllFanTab.Length - 1;
                AllFanTab[pos][level + 1]++;
                return;
            }
            fan_table[(int)FanT.HuClass] = (ushort)(level * 10 + HuClass);
            fan_table[(int)FanT.Score] = (ushort)sco;
            fan_table[(int)FanT.Frequency] = 1;
            fan_table[(int)FanT.InOutLever] = (ushort)level;

            //FanT.YB0为yb[0]的起始位
            for (int i = 0; i < yb.Length; i++)
                fan_table[i + (int)FanT.YB0] = (ushort)yb[i];

            for (int i = 0; i < AllFanTab.Length - 1; i++)
            {
                if (AllFanTab[i][(int)FanT.Score] == fan_table[(int)FanT.Score] && // 相同得分、深度
                     AllFanTab[i][(int)FanT.InOutLever] == fan_table[(int)FanT.InOutLever])
                {
                    int j = 0;
                    for (j = (int)FanT.BIG_FOUR_WINDS; j < fan_table.Length; j++)
                        if (AllFanTab[i][j] != fan_table[j])
                            break;
                    if (j == fan_table.Length)//相同胡牌方式
                    {
                        AllFanTab[i][(int)FanT.Frequency]++;
                        break;
                    }
                }
                else if (AllFanTab[i][(int)FanT.Score] == 0)//空行
                {
                    for (int j = 0; j < fan_table.Length; j++)
                        AllFanTab[i][j] = fan_table[j];
                    break;
                }
            }
            //冒泡排序算法
            ushort[] tmp = new ushort[(int)FanT.FAN_TABLE_SIZE];//0219
            for (int i = 1; i < AllFanTab.Length - 1; i++)
            {
                if (AllFanTab[i][(int)FanT.Score] + AllFanTab[i][(int)FanT.InOutLever] == 0)
                    break;
                for (int j = 1; j <= AllFanTab.Length - i; j++)
                {
                    if (AllFanTab[j - 1][(int)FanT.HuClass] == AllFanTab[j][(int)FanT.HuClass] &&
                        AllFanTab[j - 1][(int)FanT.InOutLever] == AllFanTab[j][(int)FanT.InOutLever] &&
                        AllFanTab[j - 1][(int)FanT.Frequency] < AllFanTab[j][(int)FanT.Frequency])
                    {
                        AllFanTab[j - 1].CopyTo(tmp, 0);
                        AllFanTab[j].CopyTo(AllFanTab[j - 1], 0);
                        tmp.CopyTo(AllFanTab[j], 0);
                    }
                }
            }
        }

        public void Add1HuFanInfoFrequency(int[] huedArr)
        { 
            for (int i = 0; i < AllFanTab.Length - 1; i++)
            {
                int j = 0;
                for (j = huedArr.Length - 1; j >= 4 ; j--)
                    if (AllFanTab[i][j] != huedArr[j]) 
                        break; 
                if (j == 4)//相同胡牌方式
                {
                    AllFanTab[i][(int)FanT.Frequency]++;
                    break;
                } 
            } 
        }

        
        public static int Factorial(int n)
        {
            int res = 1;
            while (n > 1)
            {
                res *= n;
                n = n - 1;
            }
            return res;
        }

        //排列数
        public double Permutation(int N, int M)
        {
            if (M > N) return 0;
            double cc = 1;
            for (int i = 0; i < M; i++)
                cc *= N - i;
            return cc;
        }
        public double Permutation(double N, int M)
        {
            if (M > N) return 0;
            double cc = 1;
            for (int i = 0; i < M; i++)
                cc *= N - i;
            return cc;
        }
        // 自定义组合函数 
        public double CombNum(int n, int k)
        {
            if (k > n) return 0;
            return Permutation(n, k) / Factorial(k);
        }
        public double CombNum(int n, int k, double n_d)
        {
            if (k > n) return 0;
            if (2 * k > n) k = n - k;
            double cc = 1;
            for (int i = 0; i < k; i++)
                cc *= n_d * (n - i) / n;
            cc = cc / Factorial(k);
            return cc;
        }
        // 自定义组合函数,可重复 
        public double comb_repl(int n, int k)
        {
            if (k > n) return 1;
            return Permutation(n + k - 1, k) / Factorial(k);
        }

        public static String Cards2Str(int[] yb)//1224
        {
            String str1 = "123456789";
            String str2 = "一二三四五六七八九";
            //String str3 = "⑴⑵⑶⑷⑸⑹⑺⑻⑼";
            //String str4 = "⒈⒉⒊⒋⒌⒍⒎⒏⒐";
            //String str5 = "❶❷❸❹❺❻❼❽❾";
            //String str6 = "㈠㈡㈢㈣㈤㈥㈦㈧㈨";
            //String str7 = "①②③④⑤⑥⑦⑧⑨";
            //String str8 = "壹贰叁肆伍陆柒捌玖";
            String str9 = "东南西北中发白花";

            String str = "";
            int[] twt = new int[3];
            //int Fen = 0, Jian = 0;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0 && yb[i] < 30)
                    twt[yb[i] / 10]++;
            String str11 = str1, str12 = str2, str13 = str1;
            if (twt[1] > twt[0] + twt[2])
            {
                str11 = str13 = str2;
                str12 = str1;
            }
            else if (twt[1] == 0)
            {
                if (twt[2] > twt[0])
                    str11 = str2;
                else
                    str13 = str2;
            }
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0 && yb[i] < 10)
                    str += str11[yb[i] - 1];
                else if (yb[i] > 10 && yb[i] < 20)
                    str += str12[yb[i] - 11];
                else if (yb[i] > 20 && yb[i] < 30)
                    str += str13[yb[i] - 21];
                else if (yb[i] > 30 && yb[i] < 46)
                    str += str9[(yb[i] - 31) / 2];

            return str;
        }







        ArrayList CombResultList = new ArrayList();
        private void dfsCombination(int cur, int iLen, ArrayList cur_list, int[] original_list)
        {
            if (iLen == 0)
            {
                int[] tmp = new int[cur_list.Count];
                cur_list.CopyTo(tmp);
                CombResultList.Add(tmp);
                return;
            }
            // 已经超了或者将之后的全部加上也不够
            for (int i = cur; i < original_list.Length; i++)
            {
                if (original_list[i] == 0) continue;
                // 把当前元素添加到当前列表中 
                cur_list.Add(i);
                dfsCombination(i + 1, iLen - 1, cur_list, original_list);
                cur_list.RemoveAt(cur_list.Count - 1);
            }
        }
        public ArrayList combinations(int[] original_list, int inLen)
        {
            CombResultList = new ArrayList();
            if (inLen == 0)
            {
                CombResultList.Add(new int[1]);
                return CombResultList;
            }
            // 调用dfs函数，传入初始下标为0，空的当前列表和结果列表              
            dfsCombination(0, inLen, new ArrayList(), original_list);
            return CombResultList;
        }
        public static IEnumerable<int[]> GetCombinatons(int n, int m)
        {
            var result = new List<int[]>();

            var combination = new int[m];
            for (var i = 0; i < m; i++)
            {
                combination[i] = i;
            }

            while (combination[0] < n - m + 1)
            {
                result.Add(combination.ToArray());

                var t = m - 1;
                while (t != 0 && combination[t] == n - m + t)
                {
                    t--;
                }

                combination[t]++;
                for (var i = t + 1; i < m; i++)
                {
                    combination[i] = combination[i - 1] + 1;
                }
            }

            return result;
        }
        void dfs(int cur, int iLen, ArrayList cur_list, int[] original_list, int replace)
        {

            // 如果已经选出了m个元素，就把当前列表添加到结果列表中，并返回泌
            if (iLen == 0)
            {
                int[] tmp = new int[cur_list.Count];
                for (int i = 0; i < tmp.Length; i++) tmp[i] = (int)cur_list[i];
                CombResultList.Add(tmp);
                return;
            }
            for (int i = cur; i < original_list.Length; i++)
            {
                if (original_list[i] == 0) continue;
                // 把当前元素添加到当前列表中 
                cur_list.Add(i);
                dfs(i + replace, iLen - 1, cur_list, original_list, replace);
                cur_list.RemoveAt(cur_list.Count - 1);
            }
        }
        public ArrayList combinations_replacement(int[] original_list, int inLen, bool with_replacement = false)
        {
            CombResultList = new ArrayList();
            int[] tmp = new int[original_list.Length];
            int replace = with_replacement ? 0 : 1;
            if (inLen == 0)
            {
                CombResultList.Add(new int[1]);
                return CombResultList;
            }
            // 调用dfs函数，传入初始下标为0，空的当前列表和结果列表
            if (with_replacement)
                dfs(0, inLen, new ArrayList(), original_list, replace);
            else//第一个位置全排列，之后是组合关系。12，13，14，21，23，24，31，32，34，41，42，43
            {
                ArrayList thisCombList = new ArrayList();
                for (int i = 0; i < original_list.Length; i++)
                    if (original_list[i] > 0)
                    {
                        CombResultList = new ArrayList();
                        original_list.CopyTo(tmp, 0);
                        tmp[i] = 0;
                        dfs(0, inLen - 1, new ArrayList(), tmp, replace);
                        foreach (int[] CombItem in CombResultList)
                        {
                            int[] item = new int[inLen];
                            item[0] = i;
                            Array.Copy(CombItem, 0, item, 1, CombItem.Length);
                            thisCombList.Add(item);
                        }
                    }
                CombResultList = new ArrayList();
                foreach (int[] CombItem in thisCombList)
                    CombResultList.Add(CombItem);
            }
            return CombResultList;
        }


        public double[] computerKRC()
        {
            double[] dKRC = new double[KnownRemainCard.Length];
            for (int i = 0; i < KnownRemainCard.Length; i++)
                dKRC[i] = KnownRemainCard[i];
            return dKRC;

            return computerFromAbcKrc();
            return computerFromOwnKRC();

            int[] numFengJian = new int[4];
            int[] HandCardLen4P = new int[4];
            bool[] bBuKao13Yao = new bool[4];
            bool[] b7Pair = new bool[4];
            for (int i = 0; i < 4; i++)
            {
                int cc = 0;
                int[] AC4P = Mahjong.AbandCard4P[i];
                int[] BC4P = Mahjong.BumpCard4P[i];
                for (int j = 0; j < BC4P.Length - 3; j++)
                    if (BC4P[j] > 0 && BC4P[j] == BC4P[j + 3] && BC4P[j] == BC4P[j + 2] &&
                         LongOfCardNZ(BC4P) % 3 > 0)//333345
                        cc++;
                HandCardLen4P[i] = 13 - LongOfCardNZ(BC4P) + cc;
                b7Pair[i] = LongOfCardNZ(BC4P) == 0;

                if (i == this.azimuth) continue;
                bBuKao13Yao[i] = true;
                for (int j = 0; j < AC4P.Length && AC4P[j] > 0; j++)
                {
                    if (AC4P[j] > 30) numFengJian[i]++;
                    if (j < 3 && AC4P[j] > 30) bBuKao13Yao[i] = false;
                    if (j < 6 && numFengJian[i] > 1) bBuKao13Yao[i] = false;
                }
                if (HandCardLen4P[i] < 13) bBuKao13Yao[i] = false;
            }
            double krcLen = 0;
            for (int i = 0; i < KnownRemainCard.Length; i++)
            {
                dKRC[i] = KnownRemainCard[i];
                krcLen += KnownRemainCard[i];
            }
            for (int i = 0; i < 4; i++)
            {
                double changeLen = 6;
                int[] AC4P = Mahjong.AbandCard4P[i];
                if (i == this.azimuth || bBuKao13Yao[i]) continue;
                for (int j = 0; j < AC4P.Length && AC4P[j] > 0; j++)
                {
                    int card = AC4P[j];
                    if (j == changeLen || card == 0)
                        break;
                    if (IfFirstNumInArray(AC4P, j))
                    {
                        double coff = ((changeLen - j) / changeLen) * HandCardLen4P[i] / krcLen;
                        if (b7Pair[i])
                            coff /= 2;

                        dKRC[card] *= Math.Round(1 + coff, 2);
                        if (card < 30 && card % 10 == 9)
                        {
                            dKRC[card - 1] *= Math.Round(1 + coff, 3);
                            dKRC[card - 2] *= Math.Round(1 + coff / 2, 4);
                        }
                        if (card < 30 && card % 10 == 1)
                        {
                            dKRC[card + 1] *= Math.Round(1 + coff, 3);
                            dKRC[card + 2] *= Math.Round(1 + coff / 2, 4);
                        }
                        if (card < 30 && card % 10 > 1 && card % 10 < 9)
                        {
                            dKRC[card - 1] *= Math.Round(1 + coff, 3);
                            dKRC[card - 2] *= Math.Round(1 + coff / 2, 4);
                            dKRC[card + 1] *= Math.Round(1 + coff, 3);
                            dKRC[card + 2] *= Math.Round(1 + coff / 2, 4);
                        }
                        dKRC[0] = dKRC[10] = dKRC[20] = dKRC[30] = 0;
                    }
                }
            }
            for (int i = 0; i < KnownRemainCard.Length; i++)
                if (dKRC[i] / KnownRemainCard[i] > 1.3)
                { }
            return dKRC;
        }

        public double[] computerFromOwnKRC()
        {
            double[] dKRC = new double[KnownRemainCard.Length];
            double[] dOBC = new double[KnownRemainCard.Length];
            int[] usedBC = new int[21];

            int cc = 0;
            double OwnButtonCardCount = 0;
            for (int i = myBCP[this.azimuth]; i < Mahjong.AllBottomCard.Length; i++)
                if (Mahjong.AllBottomCard[i] > 0)
                {
                    dOBC[Mahjong.AllBottomCard[i]]++;
                    OwnButtonCardCount++;
                }
                else break;
            for (int i = myBCP[this.azimuth]; i < myBCP[azimuth] + OwnButtonCardCount; i++)
                if (OwnButtonCardCount > 0 && Mahjong.AllBottomCard[i] > 0 && CountCardfArray(usedBC, Mahjong.AllBottomCard[i]) == 0)
                {
                    usedBC[cc] = Mahjong.AllBottomCard[i];
                    dOBC[Mahjong.AllBottomCard[i]] *= Math.Round(1 + (OwnButtonCardCount - cc) / OwnButtonCardCount, 1);
                    cc++;
                }
            for (int i = 0; i < KnownRemainCard.Length; i++)
                cc += KnownRemainCard[i];
            for (int i = 0; i < KnownRemainCard.Length; i++)
                dKRC[i] = Math.Round(KnownRemainCard[i] * OwnButtonCardCount / cc, 3);
            for (int i = 0; i < KnownRemainCard.Length; i++)
                if (dOBC[i] > 0) dKRC[i] = dOBC[i];
            return dKRC;
        }

        public double[] computerFromAbcKrc()
        {
            int cc = 0;
            double ButtonCardCount = 0;
            double[] dKRC = new double[KnownRemainCard.Length];
            double[] dOBC = new double[KnownRemainCard.Length];

            for (int i = 0; i < KnownRemainCard.Length; i++)
                cc += KnownRemainCard[i];

            for (int i = 0; i < Mahjong.AllBottomCard.Length; i++)
                if (Mahjong.AllBottomCard[i] > 0)
                {
                    dOBC[Mahjong.AllBottomCard[i]]++;
                    ButtonCardCount++;
                }

            for (int i = 0; i < KnownRemainCard.Length; i++)
                dKRC[i] = KnownRemainCard[i] * ButtonCardCount / cc;
            for (int i = 0; i < KnownRemainCard.Length; i++)
                if (dOBC[i] > 0) dKRC[i] = dOBC[i];
                else dKRC[i] /= 4;
            return dKRC;
        }

        public double[] HuCard(int[] yb, int Level, int[] gap, out double[][] Hu_Nums, string Father = "")
        {
            FanCardDataClear();
            double[] HuNum = new double[14];
            double[] maxNums = new double[8];
            int[] maxInd = new int[maxNums.Length];
            Hu_Nums = new double[maxNums.Length][];

            if (LongOfCardNZ(Mahjong.AllBottomCard) < Level * 2)//山牌明显不足时
            {
                for (int i = 0; i < yb.Length; i++)
                    if (yb[i] > 0) HuNum[i] = 0.000001;
                return HuNum;
            }
            //for (int i = 0; i < KnownRemainCard.Length; i++)
            //    if (KnownRemainCard[i] > 0) KnownRemainCard[i] = 1;

            double[] HuNumNorm = new double[14];
            double[] HuNumZuHeL = new double[14];
            double[] HuNumBuKao = new double[14];
            double[] HuNum13Yao = new double[14];
            double[] HuNum7Pair = new double[14];
            GlobeHuNums5Class = new double[5][][];
            GlobeHuNumsNormOr7Pair = new double[5][][];

            string StudyInfo = "";
            for (int j = 0; j < maxInd.Length; j++) maxInd[j] = -1;
            int HuTimes = 0; int retnum = -1;

            for (int i = Level; i < maxNums.Length; i++)
            {
                if (i == 0 && Father.Length > 0)   continue;
                if (Father.Length == 0 && i > 4 && i > gap[0] + 1 && HuTimes > 0)   //已经到第4级，之前有可胡结果，已经计算过HuCardNorm
                { retnum = 0; break; }
                if (Father.Length == 0 && i > 5 && HuTimes > 0)                 //已经到第5级，之前有可胡结果
                { retnum = 1; break; }
                if (Father.Length == 0 && i > 3 &&                              //上两次有很好表现了，提前退出
                    (LongOfCardNZ(Hu_Nums[i - 1]) + LongOfCardNZ(Hu_Nums[i - 2]) > 18 || LongOfCardNZ(Hu_Nums[i - 1]) >= 13))
                { retnum = 2; break; }
                if (Father.Contains("Eval") && i > 3 && i > gap[0] && HuTimes > 0)// 已经到第4级，已经计算过HuCardNorm
                    if (LongOfCardNZ(yb) > 5 && LongOfCardNZ(Hu_Nums[i - 1]) * 3 / 2 > LongOfCardNZ(yb))   //上次有较好表现
                    { retnum = 3; break; }
                if (Father.Contains("Eval") && i > 5 && HuTimes > 0)            //已经到第4级，已有可胡结果
                { retnum = 4; break; }
                if (MinOfArray(gap) < 2 && HuTimes > 2)                         //gap为0、1时，已有两次可胡结果 
                { retnum = 5; break; }
                if (LongOfCardNZ(Mahjong.AllBottomCard) < Level * 3)            //山牌明显不足时
                { retnum = 6; break; }
                if (i >= 6 && HuTimes > 0)                       
                { retnum = 7; break; }

                if (i >= gap[0]) HuNumNorm = HuCardNorm(yb, i, Father);
                if (i >= gap[1]) HuNumZuHeL = HuCardZuHeLong(yb, i);
                if (i >= gap[2]) HuNumBuKao = HuCardBuKao(yb, i);
                if (i >= gap[3]) HuNum13Yao = HuCard13Yao(yb, i);
                if (i >= gap[4]) HuNum7Pair = HuCard7Pair(yb, i);
                Hu_Nums[i] = AddArray(HuNumNorm, HuNumZuHeL, HuNumBuKao, HuNum13Yao, HuNum7Pair);

                maxInd[i] = IndexOfMaxInArray(Hu_Nums[i]);
                maxNums[i] = MaxOfArray(Hu_Nums[i]) > 1 ? Math.Round(MaxOfArray(Hu_Nums[i]), 0): Math.Round(MaxOfArray(Hu_Nums[i]), 3);
                for (int j = 0; j < Hu_Nums[i].Length; j++)
                    Hu_Nums[i][j] = Math.Round(Hu_Nums[i][j], 1);
                StudyInfo = CreateAllFanTabInfo(yb);
                if (maxNums[i] > 0) HuTimes++;
                else Hu_Nums[i] = null;

                //if (GlobeHuNums5Class[0] == null && LongOfCardNZ(HuNumNorm) > 0)     GlobeHuNums5Class[0] = new double[8][];
                //if (GlobeHuNums5Class[1] == null && LongOfCardNZ(HuNumZuHeL) > 0)    GlobeHuNums5Class[1] = new double[8][];
                //if (GlobeHuNums5Class[2] == null && LongOfCardNZ(HuNumBuKao) > 0)    GlobeHuNums5Class[2] = new double[8][];
                //if (GlobeHuNums5Class[3] == null && LongOfCardNZ(HuNum13Yao) > 0)    GlobeHuNums5Class[3] = new double[8][];
                //if (GlobeHuNums5Class[4] == null && LongOfCardNZ(HuNum7Pair) > 0)    GlobeHuNums5Class[4] = new double[8][];
                //if (LongOfCardNZ(HuNumNorm) > 0)  { GlobeHuNums5Class[0][i] = new double[14]; HuNumNorm.CopyTo(GlobeHuNums5Class[0][i], 0); }
                //if (LongOfCardNZ(HuNumZuHeL) > 0) { GlobeHuNums5Class[1][i] = new double[14]; HuNumZuHeL.CopyTo(GlobeHuNums5Class[1][i], 0); }
                //if (LongOfCardNZ(HuNumBuKao) > 0) { GlobeHuNums5Class[2][i] = new double[14]; HuNumBuKao.CopyTo(GlobeHuNums5Class[2][i], 0); }
                //if (LongOfCardNZ(HuNum13Yao) > 0) { GlobeHuNums5Class[3][i] = new double[14]; HuNum13Yao.CopyTo(GlobeHuNums5Class[3][i], 0); }
                //if (LongOfCardNZ(HuNum7Pair) > 0) { GlobeHuNums5Class[4][i] = new double[14]; HuNum7Pair.CopyTo(GlobeHuNums5Class[4][i], 0); }
                if (DecisionTimeOut())
                    break;
                
            }

            FanCardDataClear();
            for (int i = 0; i < maxNums.Length; i++)
                if (Hu_Nums[i] != null)
                    for (int j = 0; j < HuNum.Length; j++)
                        HuNum[j] += Hu_Nums[i][j];

            if (LongOfCardNZ(HuNum) == 0)
                HuNum[13] = 1;

            int MHuNum = IndexOfMaxInArray(HuNum);
            if (Father.Length == 0)
                Mahjong.EvalCPGHs = Hu_Nums;

            //double[] Hu = Hu5C(yb, Level, gap, out double[][] Nums, Father);
            //if (IndexOfMaxInArray(HuNum) != IndexOfMaxInArray(Hu))                    
            //{
            //    Console.WriteLine("-----------------");
            //    string str1 = GlobeHuNums2String(0, GlobeHuNums5Class);
            //    string str2 = GlobeHuNums2String(0, GlobeHuNumsNormOr7Pair);
            //    //if (str2.Length > 8 && !str1.Contains(str2.Substring(8)))
            //    {
            //        Console.WriteLine(GlobeHuNums2String(0, GlobeHuNums5Class));
            //        Console.WriteLine(GlobeHuNums2String(0, GlobeHuNumsNormOr7Pair));
            //    }
            //}
            return HuNum;
        }

        public double[] Hu5C(int[] yb, int Level, int[] gap, out double[][] Hu_Nums, string Father = "")
        {
            FanCardDataClear();
            double[] HuNum = new double[14];
            double[] maxNums = new double[8];
            int[] maxInd = new int[maxNums.Length];
            Hu_Nums = new double[maxNums.Length][];

            if (LongOfCardNZ(Mahjong.AllBottomCard) < Level * 2)//山牌明显不足时
            {
                for (int i = 0; i < yb.Length; i++)
                    if (yb[i] > 0) HuNum[i] = 0.000001;
                return HuNum;
            }               
            GlobeHuNumsNormOr7Pair = new double[5][][];

            string StudyInfo = "";
            for (int j = 0; j < maxInd.Length; j++) maxInd[j] = -1;
            int HuTimes = 0; int retnum = -1;

            for (int i = Level; i < maxNums.Length; i++)
            {
                if (Father.Length == 0 && i > 4 && i > gap[0] + 1 && HuTimes > 0)   //已经到第4级，之前有可胡结果，已经计算过HuCardNorm
                { retnum = 0; break; }
                if (Father.Length == 0 && i > 5 && HuTimes > 0)                 //已经到第5级，之前有可胡结果
                { retnum = 1; break; }
                if (Father.Length == 0 && i > 3 &&                              //上两次有很好表现了，提前退出
                    (LongOfCardNZ(Hu_Nums[i - 1]) + LongOfCardNZ(Hu_Nums[i - 2]) > 18 || LongOfCardNZ(Hu_Nums[i - 1]) >= 13))
                { retnum = 2; break; }
                if (Father.Contains("Eval") && i > 3 && i > gap[0] && HuTimes > 0)// 已经到第4级，已经计算过HuCardNorm
                    if (LongOfCardNZ(yb) > 5 && LongOfCardNZ(Hu_Nums[i - 1]) * 3 / 2 > LongOfCardNZ(yb))   //上次有较好表现
                    { retnum = 3; break; }
                if (Father.Contains("Eval") && i > 5 && HuTimes > 0)            //已经到第4级，已有可胡结果
                { retnum = 4; break; }
                if (MinOfArray(gap) < 2 && HuTimes > 2)                         //gap为0、1时，已有两次可胡结果 
                { retnum = 5; break; }
                if (LongOfCardNZ(Mahjong.AllBottomCard) < Level * 3)            //山牌明显不足时
                { retnum = 6; break; }

                Hu_Nums[i] = HuCard5Class(yb, i);

                maxInd[i] = IndexOfMaxInArray(Hu_Nums[i]);
                maxNums[i] = Math.Round(MaxOfArray(Hu_Nums[i]), 0);
                for (int j = 0; j < Hu_Nums[i].Length; j++)
                    Hu_Nums[i][j] = Math.Round(Hu_Nums[i][j], 0);
                StudyInfo = CreateAllFanTabInfo(yb);
                if (maxNums[i] > 0) HuTimes++;
                else Hu_Nums[i] = null;
                if (DecisionTimeOut())
                    break; 
            }
             
            for (int i = 0; i < maxNums.Length; i++)
                if (Hu_Nums[i] != null)
                    for (int j = 0; j < HuNum.Length; j++)
                        HuNum[j] += Hu_Nums[i][j];
            if (LongOfCardNZ(HuNum) == 0)
                HuNum[13] = 1;             
            if (Father.Length == 0)
                Mahjong.EvalCPGHs = Hu_Nums;
            return HuNum;
        }


        public double[] HuCardNorm(int[] yb, int Level, string Father = "")
        {
            FanCardDataClear();
            double count = 0; int lenHu = 0, outLevel = 0, inLevel = 0;
            outLevel = inLevel = Level;
            int lenYb = lenHu = LongOfCardNZ(yb);
            if (lenYb % 3 == 1) { outLevel--; lenHu = lenYb + 1; }

            int[] InList = GenInListNorm(yb, inLevel, Father);
            int[] OutList = yb;
            InList = Mahjong.Card34;

            int[] tmp = new int[yb.Length];
            int[] HandIn31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                HandIn31[yb[i]] += 1;
            HandIn31[0] = 0;

            int[] inCards = new int[inLevel];
            int[] outCards = new int[outLevel];
            int[] tmp31 = new int[KnownRemainCard.Length];

            double[] dKRC = computerKRC();
            double[] outone_hu_num = new double[14];
            NumOne = NumTwo = NumThree = NumFour = NumFive = NumSix = 0;
            ArrayList comb_In = combinations_replacement(InList, inLevel, true); // C(InList.Length + inLevel - 1, inLevel)
            ArrayList comb_Out = combinations(OutList, outLevel);// C(InList.Length, inLevel)

            foreach (int[] ic in comb_In)
            {
                HandIn31.CopyTo(tmp31, 0);
                for (int i = 0; i < inLevel; i++) tmp31[InList[ic[i]]] += 1;//进牌
                if (If_NotNormHu_Cards(tmp31, lenHu))
                    continue;
                if (DecisionTimeOut())
                    break;
                NumOne += 1;
                for (int i = 0; i < inLevel; i++) inCards[i] = InList[ic[i]];
                double comb = 1; double comb1 = 1; //计算进张的组合数
                for (int i = 0; i < inLevel; i++)
                    for (int j = i + 1; j < inLevel + 1; j++)
                        if (j == inLevel || inCards[i] != inCards[j])
                        {
                            //if (j - i > 1.4 && KnownRemainCard[inCards[i]] == 4)
                            //{ }
                            //comb1 *= CombNum(KnownRemainCard[inCards[i]], j - i);
                            comb *= CombNum(KnownRemainCard[inCards[i]], j - i, dKRC[inCards[i]]);
                            //double d1 = CombNum(KnownRemainCard[inCards[i]], j - i, dKRC[inCards[i]]);
                            //double d2 = CombNum(KnownRemainCard[inCards[i]], j - i);
                            i = j - 1;
                            break;
                        }

                if (comb == 0 && Level > 0) continue;
                NumTwo += 1;
                foreach (int[] oc in comb_Out)
                {
                    if(oc.Contains(1) && oc.Contains(3) && oc.Contains(5))
                    { }
                    NumThree += 1;
                    for (int i = 0; i < outLevel; i++) outCards[i] = OutList[oc[i]];
                    if (inLevel == 4 && outCards.Contains(8) && outCards.Contains(15) && outCards.Contains(29) && outCards.Contains(43))
                    { }
                    if (inLevel > 1 && IfExitSameItemInTwoArray(inCards, outCards))
                        continue;
                    NumFour++;
                    HandIn31.CopyTo(tmp31, 0);
                    for (int i = 0; i < outLevel; i++)//打牌
                    { tmp31[outCards[i]]--; tmp31[inCards[i]]++; }
                    if (lenYb % 3 == 1)
                        tmp31[inCards[ic.Length - 1]]++;
                    if (FastDetermineNoHu31(tmp31))
                        continue;

                    NumFive++;
                    yb.CopyTo(tmp, 0);
                    for (int i = 0; i < outLevel; i++)
                        tmp[oc[i]] = InList[ic[i]];//打牌FanCardData.wf4TH_TILE =
                    if (lenYb % 3 == 1)
                        tmp[0] = InList[ic[ic.Length - 1]];
                    Array.Sort(tmp);

                    if (If_NormHu_Cards(tmp) > 0)
                    {
                        Array.Sort(tmp);
                        AdjusHandCard(tmp);
                        AdjustWinFlag(inCards);
                        int sco2 = AdjustHandShunKe_ComputerScore(tmp);

                        if (fan_table[(int)FanT.SEVEN_PAIRS] > 0)
                        { fan_table[(int)FanT.SEVEN_PAIRS] = 0; sco2 -= 24; }
                        count = comb * ConvertScore(tmp, sco2, InList, ic, inLevel);
                        String Str1 = RetFanT();
                        if (count == 0) continue;
                        NumSix++;
                        int[] scoMap = MapIncreaseScore(OutList, oc);
                        if (outCards.Contains(25))
                        { }
                        if (outLevel + inLevel == 1)
                            outone_hu_num[13] += count;
                        else
                        {
                            if (inLevel == outLevel)
                                for (int i = 0; i < outLevel; i++)
                                    outone_hu_num[oc[i]] += count * scoMap[i];
                            else if (inLevel > outLevel)
                            {   //计算一副牌好坏时，只对相同的第一张牌加数
                                if (!IfSecondSameItemInTwoArray(OutList, oc))
                                    for (int i = 0; i < outLevel; i++)
                                        outone_hu_num[oc[i]] += count * scoMap[i];
                                else
                                { }
                            }
                        }
                        RecordHuFanInfo(tmp, inLevel, 0);
                        if (outCards.Contains(16))
                        { }
                    }
                    else
                    { }
                }
            }
            string StudyInfo = CreateAllFanTabInfo(yb);
            for (int k = 0; k < AllFanTab.Length - 1; k++)
            {
                int sco = 0;
                for (int i = 0; i < AllFanTab[i].Length; i++)
                    sco += AllFanTab[k][i] * fan_value_table[i];

                if (sco < 8 + 6 && AllFanTab[k][(int)FanT.MELDED_HAND] > 0)            //全求人
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 3 + 1);
                else if (sco < 8 + 4 && AllFanTab[k][(int)FanT.FULLY_CONCEALED_HAND] > 0)//不求人
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 3 + 1);
                else if (sco < 8 + 2 && AllFanTab[k][(int)FanT.CONCEALED_HAND] > 0)      //门前清
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 3 + 1);
                else if (sco < 8 + 1 && AllFanTab[k][(int)FanT.CLOSED_WAIT] > 0)       // 嵌张 
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 3 + 1);
                else if (sco < 8 + 1 && AllFanTab[k][(int)FanT.EDGE_WAIT] > 0)         // 边张 
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 3 + 1);
                else if (sco < 8 + 1 && AllFanTab[k][(int)FanT.SINGLE_WAIT] > 0)       // 单钓
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 3 + 1);
                else if (sco < 8 + 1 && AllFanTab[k][(int)FanT.SELF_DRAWN] > 0)        //7分时，考虑有自摸，但可能性是1/3
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 3 + 1);
                else if (sco < 8 + 1 && AllFanTab[k][(int)FanT.LAST_TILE] > 0)        //7分时，考虑有自摸，但可能性是1/3
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 2 + 1);
                else if (AllFanTab[k][(int)FanT.TWO_CONCEALED_PUNGS] > 0)
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 2 + 1);
                else if (AllFanTab[k][(int)FanT.THREE_CONCEALED_PUNGS] > 0)
                    AllFanTab[k][(int)FanT.Frequency] = (ushort)(AllFanTab[k][(int)FanT.Frequency] / 3 + 1);
            }            

            if (GlobeHuNums5Class[0] == null) GlobeHuNums5Class[0] = new double[8][];
            GlobeHuNums5Class[0][inLevel] = new double[14];
            outone_hu_num.CopyTo(GlobeHuNums5Class[0][inLevel], 0);
            return Counts2OutNum(outone_hu_num, inLevel);
        }

        public double[] HuCardZuHeLong(int[] yb, int Level)
        {
            FanCardDataClear();
            int outLevel = Level, inLevel = Level, lenHu = 0;
            int lenYb = lenHu = LongOfCardNZ(yb);
            if (lenYb % 3 == 1) { outLevel--; lenHu = lenYb + 1; }
             
            double[] dKRC = computerKRC();
            double[] outone_hu_num = new double[yb.Length]; 
            if (inLevel == 0 || lenYb < 10) return outone_hu_num;
            int[] inCards = new int[inLevel];
            int[] outCards = new int[outLevel];
            int[] tmp31 = new int[KnownRemainCard.Length];
            int[] tmp = new int[yb.Length];
            int[] HandIn31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                HandIn31[yb[i]] += 1;
            HandIn31[0] = 0;
 
            ArrayList comb_Out = combinations(yb, outLevel);// C(InList.Length, inLevel)
            ArrayList comb_In = combinations_replacement(Mahjong.Card34, inLevel, true);
            double count = 0;
            NumOne = NumTwo = NumThree = NumFour = NumFive = 0;
            foreach (int[] ic in comb_In)
            {
                HandIn31.CopyTo(tmp31, 0);
                for (int i = 0; i < inLevel; i++) tmp31[Mahjong.Card34[ic[i]]] += 1;//进牌
                if (If_NotZuHeLong_Cards(tmp31, lenHu))
                    continue;

                NumOne += 1;
                for (int i = 0; i < inLevel; i++) inCards[i] = Mahjong.Card34[ic[i]];
                double comb = 1;     //计算进张的组合数
                for (int i = 0; i < inLevel; i++)
                    for (int j = i + 1; j < inLevel + 1; j++)
                        if (j == inLevel || inCards[i] != inCards[j])
                        {
                            //comb *= (int)CombNum(KnownRemainCard[inCards[i]], j - i);
                            comb *= CombNum(KnownRemainCard[inCards[i]], j - i, dKRC[inCards[i]]);
                            i = j - 1;
                            break;
                        }
                if (comb == 0 && Level > 0)
                    continue;
                NumTwo += 1;
                foreach (int[] oc in comb_Out)
                {
                    NumThree += 1;
                    for (int i = 0; i < outLevel; i++) outCards[i] = yb[oc[i]];
                    if (outCards.Contains(1) && outCards.Contains(6) && outCards.Contains(9) && outCards.Contains(22) && outCards.Contains(43))
                    { }
                    if (inLevel > 1 && IfExitSameItemInTwoArray(inCards, outCards))
                        continue;

                    NumFour++;
                    yb.CopyTo(tmp, 0);
                    for (int i = 0; i < outLevel; i++)
                        tmp[oc[i]] = Mahjong.Card34[ic[i]];//打牌
                    if (lenYb % 3 == 1)
                        tmp[0] = Mahjong.Card34[ic[ic.Length - 1]];

                    Array.Sort(tmp);
                    AdjusHandCard(tmp);
                    if (If_ZuHeLong(tmp) > 0)
                    {
                        FanCardData.FullCard = AddFuLuToHandin(tmp);
                        AdjustWinFlag(inCards);
                        //int sco2 = Computer_Score_HuCard();
                        int sco2 = AdjustHandShunKe_ComputerScore(tmp);
                        count = comb * ConvertScore(tmp, sco2, Mahjong.Card34, ic, inLevel);
                        if (count == 0) continue;

                        NumFive++;
                        int[] scoMap = MapIncreaseScore(yb, oc);

                        if (outLevel + inLevel == 1)
                            outone_hu_num[0] += count;
                        else
                        {
                            if (inLevel == outLevel)
                                for (int i = 0; i < outLevel; i++)
                                    outone_hu_num[oc[i]] += count * scoMap[i];
                            else if (inLevel > outLevel)
                            {
                                if (!IfSecondSameItemInTwoArray(yb, oc))
                                    for (int i = 0; i < outLevel; i++)
                                        outone_hu_num[oc[i]] += count * scoMap[i];
                                else
                                { }
                            }
                        }
                        RecordHuFanInfo(tmp, inLevel, 1);
                        String Str1 = RetFanT();
                    }
                }
            }
           
            double[] out_hu = new double[14];
            outone_hu_num.CopyTo(out_hu, 0);
            for (int j = 1; j < yb.Length; j++)
                if (yb[j] == yb[j - 1] && inLevel == outLevel)
                    out_hu[j] = out_hu[j - 1] = Math.Max(out_hu[j], out_hu[j - 1]);

            if (GlobeHuNums5Class[1] == null) GlobeHuNums5Class[1] = new double[8][];
            GlobeHuNums5Class[1][inLevel] = new double[14];
            out_hu.CopyTo(GlobeHuNums5Class[1][inLevel], 0);

            out_hu = Counts2OutNum(out_hu, inLevel);            
            string StudyInfo = CreateAllFanTabInfo(yb);
            return out_hu;
        }

        public double[] HuCardBuKao(int[] yb, int Level)
        {
            FanCardDataClear();
            double[] out_hu = new double[14];
            int lenYb = LongOfCardNZ(yb);
            if (lenYb < 13) return out_hu;
            int outLevel = Level, inLevel = Level;
            if (lenYb % 3 == 1) outLevel--;
             
            double[] outone_hu_num = new double[yb.Length]; 
            if (inLevel == 0 || lenYb < 13) return outone_hu_num;
            int[] inCards = new int[inLevel];
            int[] outCards = new int[outLevel];
            double[] dKRC = computerKRC();

            int[] tmp31 = new int[KnownRemainCard.Length];
            int[] tmp = new int[yb.Length];
            int[] HandIn31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                HandIn31[yb[i]] += 1;
            HandIn31[0] = 0;
             
            double count = 0;
            NumOne = NumTwo = NumThree = NumFour = NumFive = 0;
            ArrayList comb_In = combinations_replacement(Mahjong.Card34, inLevel, true);
            ArrayList comb_Out = combinations(yb, outLevel);
            foreach (int[] ic in comb_In)
            {
                HandIn31.CopyTo(tmp31, 0);
                for (int i = 0; i < inLevel; i++) tmp31[Mahjong.Card34[ic[i]]] += 1;//进牌
                if (If_NotBuKao_Cards(tmp31))
                    continue;

                for (int i = 0; i < inLevel; i++) inCards[i] = Mahjong.Card34[ic[i]];
                double comb = 1;     //计算进张的组合数
                for (int i = 0; i < inLevel; i++)
                    for (int j = i + 1; j < inLevel + 1; j++)
                        if (j == inLevel || inCards[i] != inCards[j])
                        {
                            //comb *= (int)CombNum(KnownRemainCard[inCards[i]], j - i);
                            comb *= CombNum(KnownRemainCard[inCards[i]], j - i, dKRC[inCards[i]]);
                            i = j - 1;
                            break;
                        }
                if (comb == 0 && Level > 0) continue;
                NumTwo += 1;
                foreach (int[] oc in comb_Out)
                {
                    NumTwo += 1;
                    for (int i = 0; i < outLevel; i++) outCards[i] = yb[oc[i]];
                    if (inLevel > 1 && IfExitSameItemInTwoArray(inCards, outCards))
                        continue;
                    yb.CopyTo(tmp, 0);
                    for (int i = 0; i < outLevel; i++)
                        tmp[oc[i]] = Mahjong.Card34[ic[i]];//打牌
                    if (lenYb % 3 == 1)
                        tmp[0] = Mahjong.Card34[ic[ic.Length - 1]];
                    NumFour++;
                    AdjusHandCard(tmp);
                    if (If_QuanBuKao(tmp) > 0)
                    {
                        FanCardData.FullCard = AddFuLuToHandin(tmp);
                        AdjustWinFlag(inCards);
                        NumFive++;
                        int sco2 = Computer_Score_HuCard();
                        count = comb * ConvertScore(tmp, sco2, Mahjong.Card34, ic, inLevel);
                        if (count == 0) continue;
                        Array.Sort(tmp);
                        int[] scoMap = MapIncreaseScore(yb, oc);
                        if (outLevel + inLevel == 1)
                            outone_hu_num[0] += count;
                        else
                            for (int i = 0; i < outLevel; i++)
                                outone_hu_num[oc[i]] += count * scoMap[i];
                        RecordHuFanInfo(tmp, inLevel, 2);
                        String Str1 = RetFanT();
                    }
                }
            }
            
            string StudyInfo = CreateAllFanTabInfo(yb);
            outone_hu_num.CopyTo(out_hu, 0);
            for (int j = 1; j < yb.Length; j++)
                if (yb[j] == yb[j - 1])
                    out_hu[j] = out_hu[j - 1] = Math.Max(out_hu[j], out_hu[j - 1]);

            if (GlobeHuNums5Class[2] == null) GlobeHuNums5Class[2] = new double[8][];
            GlobeHuNums5Class[2][inLevel] = new double[14];
            out_hu.CopyTo(GlobeHuNums5Class[2][inLevel], 0);

            out_hu = Counts2OutNum(out_hu, inLevel);
            return out_hu;
        }

        public double[] HuCard13Yao(int[] yb, int Level)
        {
            FanCardDataClear();
            int lenYb = LongOfCardNZ(yb);
            double[] outone_hu_num = new double[14];
            int outLevel = Level, inLevel = Level;
            if (lenYb % 3 == 1) outLevel--;
            if (inLevel == 0 || lenYb < 13) return outone_hu_num;

            int[] OutList = GenOutList13Yao(yb, outLevel);
            int[] InList = GenInList13Yao(yb, outLevel);
            double[] dKRC = computerKRC();

            int[] inCards = new int[inLevel];
            int[] outCards = new int[outLevel];
            int[] tmp31 = new int[KnownRemainCard.Length];
            int[] tmp = new int[yb.Length];
            int[] HandIn31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                HandIn31[yb[i]] += 1;
            HandIn31[0] = 0;

            double count = 0; NumOne = NumTwo = NumThree = NumFour = NumFive = 0;
            ArrayList comb_In = combinations_replacement(InList, inLevel, true);
            ArrayList comb_Out = combinations(OutList, outLevel);// C(InList.Length, inLevel)
            foreach (int[] ic in comb_In)
            {
                HandIn31.CopyTo(tmp31, 0);
                for (int i = 0; i < inLevel; i++) tmp31[InList[ic[i]]] += 1;//进牌
                if (If_Not13Yao_Cards(tmp31))
                    continue;

                NumOne += 1;
                for (int i = 0; i < inLevel; i++) inCards[i] = InList[ic[i]];
                double comb = 1;     //计算进张的组合数
                for (int i = 0; i < inLevel; i++)
                    for (int j = i + 1; j < inLevel + 1; j++)
                        if (j == inLevel || inCards[i] != inCards[j])
                        {
                            //comb *= (int)CombNum(KnownRemainCard[inCards[i]], j - i);
                            comb *= CombNum(KnownRemainCard[inCards[i]], j - i, dKRC[inCards[i]]);
                            i = j - 1;
                            break;
                        }
                if (comb == 0 && Level > 0) continue;
                NumTwo += 1;
                foreach (int[] oc in comb_Out)
                {
                    NumFive += 1;
                    yb.CopyTo(tmp, 0);
                    for (int i = 0; i < outLevel; i++)
                        tmp[oc[i]] = InList[ic[i]];//打牌
                    if (lenYb % 3 == 1)
                        tmp[0] = InList[ic[ic.Length - 1]];

                    NumFour++;
                    AdjusHandCard(tmp);
                    if (If_ThirteenOne(tmp) > 0)
                    {
                        FanCardData.FullCard = AddFuLuToHandin(tmp);
                        AdjustWinFlag(inCards);
                        NumFive++;
                        int sco2 = Computer_Score_HuCard();
                        count = comb * ConvertScore(tmp, sco2, InList, ic, inLevel);
                        if (count == 0) continue;
                        Array.Sort(tmp);
                        int[] scoMap = MapIncreaseScore(OutList, oc);
                        if (outLevel + inLevel == 1)
                            outone_hu_num[0] += count;
                        else
                            for (int i = 0; i < outLevel; i++)
                                outone_hu_num[oc[i]] += count * scoMap[i];
                        RecordHuFanInfo(tmp, inLevel, 3);
                        String Str1 = RetFanT();
                    }
                }
            }
            string StudyInfo = CreateAllFanTabInfo(yb);
            double[] out_hu = Counts2OutNum(outone_hu_num, inLevel);
            for (int k = 0; k < out_hu.Length; k++)
                out_hu[k] /= Math.Pow(inLevel, inLevel * 0.3);

            return out_hu;
        }


        public double[] HuCard7Pair(int[] yb, int Level)
        {
            FanCardDataClear();
            int lenYb = LongOfCardNZ(yb);
            double[] outone_hu_num = new double[14];
            if (lenYb < 13) return outone_hu_num;
            int outLevel = Level, inLevel = Level;
            if (lenYb % 3 == 1) outLevel--;
            if (inLevel == 0 || lenYb < 13 || Level >= 7) return outone_hu_num;

            int[] tmp31 = new int[KnownRemainCard.Length];
            int[] tmp = new int[yb.Length];

            int[] OutList = new int[14];
            yb.CopyTo(OutList, 0);
            for (int i = OutList.Length - 2; i >= 0; i--)
                if (OutList[i] == OutList[i + 1] & OutList[i] > 0)
                    OutList[i] = OutList[i + 1] = 0;
             
            int cc = LongOfCardNZ(OutList);
            int[] InList = new int[cc];
            cc = 0;
            for (int i = 0; i < OutList.Length; i++)
                if (OutList[i] > 0 && cc < InList.Length)
                    InList[cc++] = OutList[i];

            int[] inCards = new int[inLevel];
            int[] outCards = new int[outLevel];
            int[] HandIn31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                HandIn31[yb[i]] += 1;
            HandIn31[0] = 0;
             
            double[] dKRC = computerKRC();
            double count = 0; NumOne = NumTwo = NumThree = NumFour = NumFive = 0;
            ArrayList comb_In = combinations(InList, inLevel);// C(InList.Length, inLevel)
            ArrayList comb_Out = combinations(OutList, outLevel);// C(InList.Length, inLevel)
            foreach (int[] ic in comb_In)
            {
                HandIn31.CopyTo(tmp31, 0);
                for (int i = 0; i < inLevel; i++) tmp31[InList[ic[i]]] += 1;
                if (If_Not7Pair_Cards(tmp31))
                    continue;

                NumOne += 1;
                for (int i = 0; i < inLevel; i++) inCards[i] = InList[ic[i]];
                double comb = 1;     //计算进张的组合数
                for (int i = 0; i < inLevel; i++)
                    for (int j = i + 1; j < inLevel + 1; j++)
                        if (j == inLevel || inCards[i] != inCards[j])
                        {
                            //comb *= (int)CombNum(KnownRemainCard[inCards[i]], j - i);
                            comb *= CombNum(KnownRemainCard[inCards[i]], j - i, dKRC[inCards[i]]);
                            i = j - 1;
                            break;
                        }
                if (comb == 0 && Level > 0)
                    continue;
                NumTwo += 1;
                foreach (int[] oc in comb_Out)
                {
                    NumTwo += 1;
                    for (int i = 0; i < outLevel; i++) outCards[i] = OutList[oc[i]];
                    if (ExitSameCard(InList, ic, OutList, oc))
                        continue;
                    yb.CopyTo(tmp, 0);
                    for (int i = 0; i < outLevel; i++)
                        tmp[oc[i]] = InList[ic[i]];
                    if (lenYb % 3 == 1)
                        tmp[0] = InList[ic[ic.Length - 1]];

                    NumThree += 1;
                    Array.Sort(tmp);
                    if (FastDetermineNo7Pair(tmp))
                        continue;

                    NumFour += 1;
                    AdjusHandCard(tmp);
                    FanCardData.FullCard = AddFuLuToHandin(tmp);
                    AdjustWinFlag(inCards);
                    int sco2 = AdjustHandShunKe_ComputerScore(tmp);
                    count = comb * ConvertScore(tmp, sco2, InList, ic, inLevel);
                    if (count == 0)
                        continue;

                    int[] scoMap = MapIncreaseScore(OutList, oc);
                    if (outLevel + inLevel == 1)
                        outone_hu_num[0] += count;
                    else
                        for (int i = 0; i < outLevel; i++)
                            outone_hu_num[oc[i]] += count * scoMap[i];
                    RecordHuFanInfo(tmp, inLevel, 4);
                    String Str1 = RetFanT();
                    NumFive++;
                    if (outCards.Contains(25) && outCards.Contains(26))
                    { }
                }
            }
            string StudyInfo = CreateAllFanTabInfo(yb);
            if (outLevel == inLevel)
                for (int k = 0; k < yb.Length - 1; k++)
                    if (yb[k] == yb[k + 1])
                        outone_hu_num[k] = outone_hu_num[k + 1] = Math.Max(outone_hu_num[k], outone_hu_num[k + 1]);
            double[] out_hu = Counts2OutNum(outone_hu_num, inLevel);
             
            if (GlobeHuNums5Class[4] == null) GlobeHuNums5Class[4] = new double[8][];
            GlobeHuNums5Class[4][inLevel] = new double[14];
            outone_hu_num.CopyTo(GlobeHuNums5Class[4][inLevel], 0);
            return out_hu;
        }

 
        public double[] HuCard5Class(int[] yb, int Level, string Father = "", int preCard = 0)
        {
            for (int i = 0; i < AllFanTab.Length; i++) Array.Clear(AllFanTab[i], 0, AllFanTab[i].Length);

            for (int i = 0; i < GlobeHuNumsNormOr7Pair.Length; i++)
            {
                //if(GlobeHuNumsNormOr7Pair[i] == null)
                    GlobeHuNumsNormOr7Pair[i] = new double[8][];
                for (int j = 0; j < GlobeHuNumsNormOr7Pair[i].Length; j++) 
                    GlobeHuNumsNormOr7Pair[i][Level] = new double[14];
            }

            double count = 0;
            int lenHu = 0, outLevel = 0, inLevel = 0;
            outLevel = inLevel = Level;
            int lenYb = lenHu = LongOfCardNZ(yb);
            if (lenYb % 3 == 1) { outLevel--; lenHu = lenYb + 1; }

            int[] In7pairList = new int[14];
            yb.CopyTo(In7pairList, 0);
            for (int i = 0; i < In7pairList.Length - 1; i++)
                if (In7pairList[i] == In7pairList[i + 1])
                    In7pairList[i] = In7pairList[i + 1] = 0;

            int[] OutList = yb;
            int[] InList = Card34;
            InList = JoinArray(InList, In7pairList);
            InList = Mahjong.Card34;

            int[] inCards = new int[inLevel];
            int[] outCards = new int[outLevel];
            int[] KRC = new int[KnownRemainCard.Length];
            int[] tmp31 = new int[KnownRemainCard.Length];
            int[] tmp = new int[yb.Length];
            int[] OldTmp = new int[yb.Length];

            ArrayList HuedList = new ArrayList(); 
            List<double> CountMapList = new List<double>();
            List<int> huClassList = new List<int>();
            int[] HandIn31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                HandIn31[yb[i]] += 1;
            HandIn31[0] = 0;

            double[] outone_hu_num = new double[14];
            NumOne = NumTwo = NumThree = NumFour = NumFive = 0; 
            ArrayList comb_In = combinations_replacement(InList, inLevel, true); // C(InList.Length + inLevel - 1, inLevel)
            ArrayList comb_Out = combinations(OutList, outLevel);// C(InList.Length, inLevel)
            foreach (int[] ic in comb_In)
            {
                if (ic.Contains(0) && ic.Contains(18) && ic.Contains(19))
                { }
                NumOne += 1;
                HandIn31.CopyTo(tmp31, 0);
                for (int i = 0; i < inLevel; i++) tmp31[InList[ic[i]]] += 1;//进牌 
                if (If_Not_5Class(tmp31, lenHu)) 
                    continue;
                NumTwo += 1;
                for (int i = 0; i < inLevel; i++) inCards[i] = InList[ic[i]];
                double comb = 1;     //计算进张的组合数
                for (int i = 0; i < inLevel; i++)
                    for (int j = i + 1; j < inLevel + 1; j++)
                        if (j == inLevel || inCards[i] != inCards[j])
                        {
                            comb *= CombNum(KnownRemainCard[inCards[i]], j - i, KnownRemainCard[inCards[i]]);
                            i = j - 1;
                            break;
                        }
                if (comb == 0 && Level > 0) continue;
                NumTwo += 1;
                foreach (int[] oc in comb_Out)
                {
                       
                    NumThree += 1;
                    for (int i = 0; i < outLevel; i++) outCards[i] = OutList[oc[i]];
                    if (ExitSameCard(InList, ic, OutList, oc))
                        continue;
                    yb.CopyTo(tmp, 0);
                    for (int i = 0; i < outLevel; i++)
                        tmp[oc[i]] = InList[ic[i]];//打牌
                    if (lenYb % 3 == 1)
                        tmp[0] = InList[ic[ic.Length - 1]];
                    Array.Sort(tmp);
                    if(If_7DuiExitSameInOutCard(tmp, OutList, oc, ic))
                         continue;
                    int ind = IndexArrayInList(HuedList, tmp);

                    if (ind != -1)
                    {
                        //NumFive++;
                        //int[] HuedArray = (int[])HuedList[ind];
                        //int[] scoMap = MapIncreaseScore(OutList, oc);
                        //for (int i = 0; i < oc.Length; i++)
                        //{
                        //    double ss = CountMapList[ind] * scoMap[i];
                        //    if (outLevel + inLevel == 1)
                        //        oc[0] = 13;
                        //    if (outLevel + inLevel == 1 || inLevel == outLevel ||
                        //        inLevel > outLevel && !IfSecondSameItemInTwoArray(OutList, oc))
                        //    {
                        //        outone_hu_num[oc[i]] += ss;
                        //        GlobeHuNumsNormOr7Pair[huClassList[ind]][inLevel][oc[i]] += ss;
                        //    }
                        //}
                        //Add1HuFanInfoFrequency((int[])HuedList[ind]);
                        //continue;
                    }
                    if (FastDetermineNoHu5Class(tmp)) 
                            continue;

                    NumFour++;
                    AdjusHandCard(tmp);
                    AdjustWinFlag(inCards);
                    //if (If_ZuHeLong() > 0)
                    if (If_NormHu_Cards(tmp) > 0 || If_7Dui(tmp) || If_ZuHeLong() > 0 || If_QuanBuKao() > 0 || If_ThirteenOne() > 0)
                    {
                        
                        int sco2 = AdjustHandShunKe_ComputerScore(tmp);
                        count = comb * ConvertScore(tmp, sco2, InList, ic, inLevel);
                        if (count == 0) continue;

                        String Str1 = RetFanT(); int HuClass = 0;
                        if (fan_table[(int)FanT.LESSER_HONORS_AND_KNITTED_TILES] + fan_table[(int)FanT.GREATER_HONORS_AND_KNITTED_TILES] > 0)
                            HuClass = 2;
                        else if (fan_table[(int)FanT.KNITTED_STRAIGHT] > 0)
                            HuClass = 1;
                        else if (fan_table[(int)FanT.THIRTEEN_ORPHANS] > 0)
                            HuClass = 3;
                        else if (fan_table[(int)FanT.SEVEN_PAIRS] > 0)
                            HuClass = 4;
                        else
                        {
                            HuClass = 0;
                            int[] Ashun = new int[4];
                            int[] Ake = new int[4];
                            int Jiang = 0;
                            int[][] JangShunKes = DivideToJiangShunKe(tmp);
                            for (int i = 0; i < JangShunKes.Length; i++)
                            {
                                int[] JSK = JangShunKes[i];
                                int cc = 0;// 手牌获得的顺子
                                FanCardData.Jiang = 0;
                                Array.Clear(Ashun, 0, Ashun.Length);
                                Array.Clear(Ake, 0, Ake.Length);
                                for (int j = 0; j < JSK.Length; j++)
                                    if (JSK[j] > 200)
                                        Ashun[cc++] = JSK[j] % 100;
                                cc = 0;// 暗刻子数组
                                for (int j = 0; j < JSK.Length; j++)
                                    if (JSK[j] > 100 && JSK[j] < 200)
                                        Ake[cc++] = JSK[j] % 100;
                                for (int j = 0; j < JSK.Length; j++)
                                    if (JSK[j] > 0 && JSK[j] < 100)
                                        Jiang = JSK[j]; 
                            }
                        }

                        if (HuClass == 0 && oc.Contains(0) && oc.Contains(1) && oc.Contains(1))
                        { }
                        int[] scoMap = MapIncreaseScore(OutList, oc);
                        if (outLevel + inLevel == 1)
                            oc[0] = 13;
                        if (outLevel + inLevel == 1 || inLevel == outLevel ||
                            inLevel > outLevel && !IfSecondSameItemInTwoArray(OutList, oc))
                            for (int i = 0; i < oc.Length; i++)
                            {
                                outone_hu_num[oc[i]] += count * scoMap[i];
                                GlobeHuNumsNormOr7Pair[HuClass][inLevel][oc[i]] += count * scoMap[i];
                            }
                        RecordHuFanInfo(tmp, inLevel, HuClass);
                        Array.Sort(tmp);

                        NumFive++;
                        HuedList.Add((int[])tmp.Clone()); 
                        CountMapList.Add(count);
                        huClassList.Add(HuClass);
                        if (fan_table[(int)FanT.SEVEN_PAIRS] > 0 && outCards.Contains(3) && outCards.Contains(25) && outCards.Contains(26))
                        { HuClass = 4; }
                    }
                    else { }

                }
            }
            //PrintArrayList(HuedList, 3);
            //for (int k = 0; k < yb.Length - 1; k++)
            //    if (yb[k] == yb[k + 1])
            //        outone_hu_num[k] = outone_hu_num[k + 1] = Math.Max(outone_hu_num[k], outone_hu_num[k + 1]);
            StudyInfo = CreateAllFanTabInfo(yb);
            return Counts2OutNum(outone_hu_num, inLevel);
        }


        public void HuCardCombTest(int Level, int[] InList)
        {
            //int[] InList = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            //int[] krc = new int[] { 0, 1, 2, 3, 4, 1, 2, 3, 4 };

            int[] krc = KnownRemainCard;

            int[] inCards = new int[Level];
            ArrayList comb_In = combinations_replacement(InList, Level, true);
            int sum = 0;

            foreach (int[] ic in comb_In)
            {
                for (int i = 0; i < Level; i++)
                    inCards[i] = InList[ic[i]];
                int comb = 1;
                for (int i = 0; i < Level; i++)
                    for (int j = i + 1; j < Level + 1; j++)
                        if (j == Level || inCards[i] != inCards[j])
                        {
                            comb *= (int)CombNum(krc[inCards[i]], j - i);
                            i = j - 1;
                            break;
                        }
                if (comb == 0 && Level > 0) continue;
                sum += comb;
            }
            int krcCount = 0;
            for (int i = 0; i < krc.Length; i++)
                if (InList.Contains(i))
                    krcCount += krc[i];
            double p3 = CombNum(krcCount, Level);
            if (sum != p3)
            { }
        }

        public bool ExitSameCard(int[] InList, int[] ic, int[] OutList, int[] oc)
        {
            for (int i = 0; i < ic.Length; i++)
                for (int j = 0; j < oc.Length; j++)
                    if (InList[ic[i]] > 0 && InList[ic[i]] == OutList[oc[j]])
                        return true;
            return false;
        }
        public bool If_7DuiExitSameInOutCard(int[] yb, int[] OutList, int[] oc, int[] ic)//七对中有相同两张进片，不计分。表示之前计过
        {
            int pair = 0;
            Array.Sort(yb);
            for (int i = 0; i < yb.Length - 1; i += 2)
                if (yb[i] > 0 && yb[i] == yb[i + 1])
                    pair++;
            if (pair < 7)
                return false;
            for (int i = 1; i < ic.Length; i ++)
                if (ic[i] == ic[i - 1])
                    return true;
            for (int i = 1; i < oc.Length; i++)
                if (OutList[oc[i]] == OutList[oc[i - 1]])
                    return true;
            return false;
        }
        //对重复牌时，进行计分处理
        public int[] MapIncreaseScore(int[] yb, int[] combOut)
        {
            int[] map = new int[combOut.Length];
            int[] map1 = new int[combOut.Length];//手牌中相同牌中，非第一打出的
            int[] map2 = new int[combOut.Length];//打出的牌中有相同的

            for (int i = 0; i < combOut.Length; i++)//手牌中相同牌中，非第一打出的
                if (combOut[i] > 0 && yb[combOut[i]] == yb[combOut[i] - 1])
                    map1[i] = yb[combOut[i]];

            //打出的牌中有相同的
            for (int i = 0; i < combOut.Length - 1; i++)
                if (yb[combOut[i]] == yb[combOut[i + 1]])
                    map2[i] = yb[combOut[i]];
            for (int i = 1; i < combOut.Length; i++)
                if (yb[combOut[i]] == yb[combOut[i - 1]])
                    map2[i] = yb[combOut[i]];
            int len1 = LongOfCardNZ(map1);
            int len2 = LongOfCardNZ(map2);

            if (map1.Length <= 1 || len1 + len2 == 0)//通常情况
                for (int i = 0; i < combOut.Length; i++)
                    map[i] = 1;
            else if (len1 == 1 && len2 == 0)//一个后张，只计后张得分
            {
                for (int i = 0; i < combOut.Length; i++)
                    if (map1[i] > 0)
                        map[i] = 1;
            }
            else if (len1 > 1 && len2 == 0)//两个及以上都是后张时，不计分 
                for (int i = 0; i < combOut.Length; i++)
                    map[i] = 0;
            else if (len2 >= 2)//打出的牌中有相同的 
                for (int i = 0; i < combOut.Length; i++)
                    map[i] = 1;
            else if (LongOfCardNZ(map1) > 1)
            {
                //int max = MaxOfArray(map1);
                //int min = MinOfArrayNZ(map1);
                //if (max == min)
                //    for (int i = 0; i < combOut.Length; i++) 
                //        map1[i] = 1;
                //else if (max > min)
                //    for (int i = 0; i < combOut.Length; i++) 
                //        map1[i] = 0;                 
            }
            return map;
        }

        //对重复牌时，进行计分处理
        public bool MapIncreaseScoreOld(int[] yb, int[] combOut)
        {
            int[] map = new int[combOut.Length];
            int[] map1 = new int[combOut.Length];//手牌中相同牌中，非第一打出的
            int[] map2 = new int[combOut.Length];//打出的牌中有相同的

            for (int i = 0; i < combOut.Length; i++)//手牌中相同牌中，非第一打出的
                if (combOut[i] > 0 && yb[combOut[i]] == yb[combOut[i] - 1])
                    map1[i] = yb[combOut[i]];

            //打出的牌中有相同的
            for (int i = 0; i < combOut.Length - 1; i++)
                if (yb[combOut[i]] == yb[combOut[i + 1]])
                    map2[i] = yb[combOut[i]];
            for (int i = 1; i < combOut.Length; i++)
                if (yb[combOut[i]] == yb[combOut[i - 1]])
                    map2[i] = yb[combOut[i]];
            int len1 = LongOfCardNZ(map1);
            int len2 = LongOfCardNZ(map2);

            if (map1.Length <= 1 || len1 + len2 == 0)//通常情况
                for (int i = 0; i < combOut.Length; i++)
                    map[i] = 1;
            else if (len1 == 1 && len2 == 0)//一个后张，只计后张得分
            {
                for (int i = 0; i < combOut.Length; i++)
                    if (map1[i] > 0)
                        map[i] = 1;
            }
            else if (len1 > 1 && len2 == 0)//两个及以上都是后张时，不计分 
                for (int i = 0; i < combOut.Length; i++)
                    map[i] = 0;
            else if (len2 >= 2)//打出的牌中有相同的 
                for (int i = 0; i < combOut.Length; i++)
                    map[i] = 1;
            else if (LongOfCardNZ(map1) > 1)
            {
                //int max = MaxOfArray(map1);
                //int min = MinOfArrayNZ(map1);
                //if (max == min)
                //    for (int i = 0; i < combOut.Length; i++) 
                //        map1[i] = 1;
                //else if (max > min)
                //    for (int i = 0; i < combOut.Length; i++) 
                //        map1[i] = 0;                 
            }
            return false;
        }

        /// <summary>
        /// 把胡牌的点数转换成权重数
        /// </summary>
        /// <param name="outone_hu_num"></param>
        /// <param name="inLevel"></param>
        /// <param name="OutLength"></param>
        /// <param name="InLength"></param>
        /// <returns></returns>
        public double[] Counts2OutNum(double[] outone_hu_num, int inLevel)//20240413
        {
            double[] out_num = new double[outone_hu_num.Length];
            int ALLcount = 0;
            for (int i = 0; i < KnownRemainCard.Length; i++)
                ALLcount += KnownRemainCard[i];
            double p3 = CombNum(ALLcount, inLevel);
            for (int i = 0; i < outone_hu_num.Length; i++)
                out_num[i] = 1000000 * outone_hu_num[i] / p3;
            //double p4 = Permutation(ALLcount, inLevel);
            //for (int i = 0; i < outone_hu_num.Length; i++)
            //    out_num[i] = 1000000 * outone_hu_num[i] / p4;

            double abc = LongOfCardNZ(Mahjong.AllBottomCard);//后头可能被截胡 
            for (int i = 0; i < out_num.Length; i++)
                out_num[i] *= Math.Pow(abc / (abc + 4 + inLevel), inLevel);
             
            return out_num;
        }
 

        public int Max7PairKey(int[] yb, int card = 0)
        {
            if (LongOfCardNZ(yb) < 13)
                return 14;
            int cc = 0;
            int[] HandIn31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                HandIn31[yb[i]] += 1;
            HandIn31[0] = 0;
            for (int i = 0; i < HandIn31.Length; i++)
                cc += HandIn31[i] / 2;
            return 7 - cc;

        }







        int HuMinGap = 9;                               // 胡牌差距步数
        int HuMinGapCount = 0;
        int HuCount = 0;
        ArrayList HuSet = new ArrayList();
        ArrayList HuDiscard = new ArrayList();
        ArrayList[] HuJangKeShun = new ArrayList[9];    // 将牌、刻子、顺子、刻搭子、顺搭子、顺嵌张、将牌 

        int[][] HuPattern = {
                    new int[] {2},
                    new int[] {3},
                    new int[] {1, 1, 1},
                    new int[] {2},
                    new int[] {1, 1},
                    new int[] {1, 0, 1},
                }; // 0将牌、1刻子、2顺子、3刻搭子、4顺搭子、5嵌张搭子
        ArrayList PatternMatching(int[] cnt_table, int iTile, bool bJang)
        {
            ArrayList Patter = new ArrayList();
            if ((!bJang) && cnt_table[iTile] > 1)                           // 将、雀头 
                Patter.Add(0);
            // return Patter                                                // 如果已经通过削减雀头/面子降低了上听数，再按搭子计算的上听数肯定不会更少

            for (int cls = 1; cls < HuPattern.Length; cls++)
            {
                bool ret = true;
                if (iTile >= 28 && (cls == 2 || cls == 5) || iTile >= 29 && cls == 4)  // 最后无搭子
                    continue;
                if (iTile < 30 && iTile % 10 == 9 && cls == 5)             // 101嵌张搭子时，不能跨条万筒
                    continue;

                for (int j = 0; j < HuPattern[cls].Length; j++)
                    if ((cnt_table[iTile + j] < HuPattern[cls][j]))   // 与本模式不匹配
                        ret = false;
                if (ret)
                    Patter.Add(cls);
            }

            if (Patter.Contains(1) && Patter.Contains(3))                 // 有刻子后，不考虑11搭子
                Patter.Remove(3);
            if (Patter.Count == 0)
                Patter.Add(-1);                                           // 没有匹配的模式             
            return Patter;
        }
        bool ItemSame(ArrayList[] Items1, ArrayList[] Items2, int start, int end)
        {
            bool bSame = true;
            for (int i = start; i < end; i++)
                for (int j = 0; j < Items1[i].Count; j++)
                    if (Items1[i].Count != Items2[i].Count || ((int)Items1[i][j]) != ((int)Items2[i][j]))
                    {
                        bSame = false; break;
                    }
            return bSame;
        }
        void printJKS(ArrayList[] ItemsJKS, string name = "")
        {
            string str = name + "[";
            for (int i = 0; i < ItemsJKS.Length; i++)
            {
                str += "[";
                for (int j = 0; j < ItemsJKS[i].Count; j++)
                {
                    str += ItemsJKS[i][j];
                    if (j < ItemsJKS[i].Count - 1)
                        str += " ";
                }
                str += "]";
                if (i < ItemsJKS.Length - 1)
                    str += ",";
            }
            str += "]";
            Console.WriteLine(str);
        }

        int BasicFormHuRecursively(int[] yb31, bool has_Jang, int FuLu_cnt, int KeShun_cnt, int DaZi_cnt, int CollInfoGap = -1)
        {
            int[] KNC = KnownRemainCard;
            // 匹配5种模式， -1：没有匹配的模式

            HuCount += 1;
            int NowGap, max_cnt;
            int KeShun_need = 4 - FuLu_cnt - KeShun_cnt - DaZi_cnt;             // 需要完成的刻子、顺子搭子数量       
            NowGap = max_cnt = DaZi_cnt + KeShun_need * 2 + (has_Jang ? 0 : 1); // 当前胡牌的差距步数 

            if (!(FuLu_cnt + KeShun_cnt + DaZi_cnt >= 4 && has_Jang))
            {         // 未完成  
                for (int tile = 1; tile < KrcLen; tile++)
                {

                    if (yb31[tile] == 0)                                        //无牌
                        continue;
                    ArrayList Patter = PatternMatching(yb31, tile, has_Jang);
                    foreach (int cls in Patter)
                    {                               //可能同时出现0210 0210型的刻搭子、顺搭子
                        if (cls == -1)                                          //查找匹配模式， -1：没有匹配的模式
                            continue;
                        else if (cls == 3 && KNC[tile] == 0)                   //死对子
                            continue;
                        else if (cls == 4 && tile < 30 && KNC[tile - 1] + KNC[tile + 2] == 0)   //两头断张
                            continue;
                        else if (cls == 5 && KNC[tile + 1] == 0)                 //中间断张
                            continue;
                        if (cls > 0 && FuLu_cnt + KeShun_cnt + DaZi_cnt >= 4)    //刻子、顺子、刻搭子、顺搭子、顺嵌张已满，不再继续查找其他5种模式，可以找将牌
                            continue;

                        for (int i = 0; i < HuPattern[cls].Length; i++)    //削减各种模式相应的牌，后应还原
                            yb31[tile + i] -= HuPattern[cls][i];
                        HuJangKeShun[cls].Add(tile);                            //记录匹配模式信息，后应还原

                        bool Jang_plus1 = cls == 0 ? true : false;
                        int KeShun_plus1 = cls == 1 || cls == 2 ? 1 : 0;        // 将牌、顺刻、顺搭子变化
                        int DaZi_plus1 = cls > 2 ? 1 : 0;

                        int ret = BasicFormHuRecursively(yb31,
                            has_Jang || Jang_plus1, FuLu_cnt, KeShun_cnt + KeShun_plus1, DaZi_cnt + DaZi_plus1, CollInfoGap);

                        NowGap = Math.Min(NowGap, ret);
                        if (ret < HuMinGap)
                        {                                   // 胡牌差距步数变小，则初始化                         
                            HuMinGap = ret;
                            HuSet.Clear();
                        }
                        for (int i = 0; i < HuPattern[cls].Length; i++)    // 还原牌数 
                            yb31[tile + i] += HuPattern[cls][i];
                        HuJangKeShun[cls].RemoveAt(HuJangKeShun[cls].Count - 1);// 还原匹配模式信息 
                    }
                }
            }
            if (NowGap < HuMinGap)
            {                                                                   // 胡牌差距步数变小，则初始化                         
                HuMinGap = NowGap;
                HuSet.Clear();
            }

            if (max_cnt == HuMinGap)
            {                                                                   // 记录本级组牌、剩余牌、频次信息
                HuMinGapCount += 1;
                ArrayList surplusCards = new ArrayList();
                for (int i = 0; i < KrcLen; i++)                                    // 剩余牌   
                    if (yb31[i] > 0 && !HuJangKeShun[6].Contains(i))
                        surplusCards.Add(i);

                //JangKeShun = copy.deepcopy(HuJangKeShun);
                //for (it in JangKeShun)                          
                //    it.sort();
                foreach (int card in surplusCards)
                    HuJangKeShun[6].Add(card);                                   // 加入剩余牌、频次信息
                HuJangKeShun[7].Clear(); HuJangKeShun[8].Clear();
                HuJangKeShun[7].Add(max_cnt);
                HuJangKeShun[8].Add(1);

                ArrayList[] JangKeShun = new ArrayList[9];
                for (int i = 0; i < JangKeShun.Length; i++)
                    JangKeShun[i] = new ArrayList(HuJangKeShun[i]);
                if (JangKeShun[0].Count == 1)
                {
                    JangKeShun[3].Add(JangKeShun[0][0]);
                    JangKeShun[0].Clear();
                }
                for (int i = 0; i < JangKeShun.Length; i++)
                    JangKeShun[i].Sort();                               // 组牌排序

                //Console.WriteLine(); printJKS(JangKeShun, "JSK");
                //foreach (ArrayList[] huIt in HuSet)
                //    printJKS(huIt, "set");

                bool bSame = false;
                foreach (ArrayList[] huIt in HuSet)                  // 查找相同条目，频次加1
                {
                    if (ItemSame(JangKeShun, huIt, 0, 6))
                    {
                        int cc = (int)huIt[8][0];
                        huIt[8].Clear();
                        huIt[8].Add(cc + 1);
                        bSame = true;
                        break;
                    }
                }
                if (!bSame)                                           // 没有相同条目，追加
                {

                    HuSet.Add(JangKeShun);
                }
            }
            return NowGap;
        }

        // return: gap 到胡牌的最小步数，thisDiscards 本级无用要打的牌
        // yb31 手牌计数，FuLu_cnt 吃碰杠的副露数，KeShun_cnt 刻子顺子数（3张），DaZi_cnt 搭子数(2张) 副露
        // 搭子是指2张相连或间隔一张的序数牌
        ArrayList createNecessaryCards(ArrayList InHuSet, int[] yb31, int CollectInfoGap = -1)              // 将HuJangKeShun信息转换为需求牌
        {
            int[] KNC = KnownRemainCard;
            ArrayList NecessaryCards = new ArrayList();
            ArrayList NecessaryCount = new ArrayList();
            int[] Counts = new int[KrcLen];
            foreach (ArrayList[] jks in InHuSet)
            {
                for (int cls = 1; cls < 6; cls++)
                {
                    for (int num = 0; num < jks[cls].Count; num++)
                    {
                        int oneTile = (int)jks[cls][num];
                        if (cls == 3 && KNC[oneTile] > 0)              //刻搭子
                        {
                            if (!NecessaryCards.Contains(oneTile))
                                NecessaryCards.Add(oneTile);
                            Counts[oneTile] += (int)jks[jks.Length - 1][0];
                        }
                        if (cls == 4 && KNC[oneTile - 1] + KNC[oneTile + 2] > 0)    //顺搭子
                        {
                            if (KNC[oneTile - 1] > 0)
                            {
                                if (!NecessaryCards.Contains(oneTile - 1))
                                    NecessaryCards.Add(oneTile - 1);
                                Counts[oneTile - 1] += (int)jks[jks.Length - 1][0];
                            }
                            if (KNC[oneTile + 2] > 0)
                            {
                                if (!NecessaryCards.Contains(oneTile + 2))
                                    NecessaryCards.Add(oneTile + 2);
                                Counts[oneTile + 2] += (int)jks[jks.Length - 1][0];
                            }
                        }
                        if (cls == 5 && KNC[oneTile + 1] > 0)                   //嵌张搭子
                        {
                            if (!NecessaryCards.Contains(oneTile + 1))
                                NecessaryCards.Add(oneTile + 1);
                            Counts[oneTile + 1] += (int)jks[jks.Length - 1][0];
                        }
                    }
                }
                if (jks[3].Count == 0)
                {
                    int[] yb_KNC = new int[KnownRemainCard.Length];
                    yb31.CopyTo(yb_KNC, 0);
                    for (int i = 1; i < KnownRemainCard.Length; i++)
                        if (yb_KNC[i] > 1)
                            yb_KNC[i] = 1;
                    for (int i = 1; i < KnownRemainCard.Length; i++)
                        yb_KNC[i] *= KNC[i];

                    int danZhng = 0;
                    if (CollectInfoGap <= 1) danZhng = 3;
                    else if (CollectInfoGap == 2) danZhng = 2;
                    else danZhng = 1;
                    for (int i = 0; i < danZhng; i++)   //差距越小，可多按排些候选牌作将
                    {
                        int iMax = IndexOfMaxInArray(yb_KNC);
                        if (!NecessaryCards.Contains(iMax))
                            NecessaryCards.Add(iMax);
                        Counts[iMax] += (int)jks[jks.Length - 1][0];
                        yb_KNC[iMax] = 0;
                    }
                }
                int cc = 0;
                for (int i = 1; i < 6; i++)
                    cc += jks[i].Count;
                if (cc < 5)                  // 牌组不够时
                    foreach (int card in jks[6])
                        if (!NecessaryCards.Contains(card))
                            NecessaryCards.Add(card);
            }

            foreach (int it in Counts)
                if (it > 0)
                    NecessaryCount.Add(it);
            NecessaryCards.Sort();
            return NecessaryCards;
        }

        ArrayList createDiscardCards(ArrayList HuStSet)
        {
            ArrayList DiscardCards = new ArrayList();
            ArrayList DiscardCount = new ArrayList();
            int[] Counts = new int[KrcLen];
            foreach (ArrayList[] jks in HuStSet)
            {
                foreach (int card in jks[6])
                    if (!DiscardCards.Contains(card))
                        DiscardCards.Add(card);
                foreach (int card in jks[6])
                    Counts[card] += (int)jks[jks.Length - 1][0];
            }
            foreach (int it in Counts)
                if (it > 0)
                    DiscardCount.Add(it);
            DiscardCards.Sort();
            return DiscardCards;
        }

        public int MaxNormHuGap(int[] yb, int fixed_cnt = -1, int CollectInfoGap = -1)
        {
            int[] tmp31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    tmp31[yb[i]]++;

            if (fixed_cnt == -1)                            // 组合龙为3
                fixed_cnt = (14 - LongOfCardNZ(yb)) / 3;    // 已经吃碰杠的牌组数量
            HuMinGap = 9;                               // 胡牌差距步数
            HuMinGapCount = 0;
            HuCount = 0;

            HuSet = new ArrayList();
            HuDiscard = new ArrayList();
            HuJangKeShun = new ArrayList[9];    // 将牌、刻子、顺子、刻搭子、顺搭子、顺嵌张、将牌 
            for (int i = 0; i < HuJangKeShun.Length; i++)
                HuJangKeShun[i] = new ArrayList();

            int gap = BasicFormHuRecursively(tmp31, false, fixed_cnt, 0, 0, CollectInfoGap);
            ArrayList Necessary = createNecessaryCards(HuSet, tmp31, CollectInfoGap);
            ArrayList Discard = createDiscardCards(HuSet);
            HuMinGap = gap;
            return gap;
        }

        public int MaxZuHeLongKey(int[] yb, out int[] JinKey, out int JinNum, int card = 0)
        {
            int[] tmp1 = new int[14];
            int[] tmp2 = new int[14];
            JinNum = 0; int maxJin = 0; int ZuKey = 0;
            JinKey = new int[3];
            if (LongOfCardNZ(yb) < 10)
                return 0;

            yb.CopyTo(tmp1, 0);
            if (card > 0)
                tmp1[13] = card;
            Array.Sort(tmp1);

            MaxZHL147_258_369Key(tmp1, out JinKey, out maxJin);
            //去筋 
            for (int k = 0; k < tmp1.Length; k++)
            {
                if (tmp1[k] == 0 || tmp1[k] > 30 || k < tmp1.Length - 1 && tmp1[k] == tmp1[k + 1]) continue;
                if (tmp1[k] % 10 % 3 == JinKey[tmp1[k] / 10] % 3)
                {
                    tmp1[k] = 0;
                    JinNum++;
                }
            }
            Array.Sort(tmp1);
            int ccc = Mahjong.LongOfCardNZ(yb);
            int gap = MaxNormHuGap(tmp1, 3 + (14 - ccc) / 3);
            return 14 - (9 - JinNum + gap);
        }

        public int MaxQuanBuKaoKey(int[] yb, out int[] JinKey, out int jinNum, int card = 0)
        {
            int[] tmp = new int[14];
            jinNum = 0;
            int maxJin = 0;

            JinKey = new int[3];
            if (LongOfCardNZ(yb) < 13)
                return 0;

            yb.CopyTo(tmp, 0);
            if (card > 0)
                tmp[13] = card;
            Array.Sort(tmp);

            MaxQBK147_258_369Key(tmp, out JinKey, out maxJin);
            return maxJin;
        }

        public int Max13YaoKey(int[] yb, int card = 0)
        {
            if (LongOfCardNZ(yb) < 13)
                return 0;
            int[] InList13Yao = GenInList13Yao(yb);
            //13幺时，已有断张，肯定做不成
            for (int i = 0; i < InList13Yao.Length; i++)
                if (KnownRemainCard[InList13Yao[i]] == 0)
                {
                    bool DuanZhang = true;
                    for (int j = 0; j < yb.Length; j++)
                        if (InList13Yao[i] == yb[j])
                        {
                            DuanZhang = false;
                            break;
                        }
                    if (DuanZhang)
                        return 0;
                }

            int[] yb31 = new int[KnownRemainCard.Length];
            int max = 0;
            int[] tmp = new int[14];

            yb.CopyTo(tmp, 0);
            if (card > 0)
                tmp[13] = card;
            Array.Sort(tmp);
            if (LongOfCardNZ(tmp) < 13)
                return 0;

            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] > 0)
                    yb31[tmp[i]]++;

            for (int i = 0; i < yb31.Length; i++)
                if (i < 30 && i % 10 % 8 != 1)
                    yb31[i] = 0;
            for (int i = 0; i < yb31.Length; i++)
                if (yb31[i] > 0)
                    max++;
            for (int i = 0; i < yb31.Length; i++)
                if (yb31[i] > 1)
                {
                    max++;
                    break;
                }


            //// 张数为零的
            //int ZeroYao = 0;
            //for (int i = 0; i < InList13Yao.Length; i++)
            //    if (KnownRemainCard[InList13Yao[i]] == 0)
            //        ZeroYao++;

            return max;
        }


        public int Max147_258_369KeyNum(int[] yb)
        {
            int maxNum = 0;  //最多的筋 
            int[,] jinFB = new int[3, 3];//筋
            int[] JinKey = new int[3];//最多的筋  

            if (yb[0] > 0 && yb[0] < 30)
                jinFB[yb[0] / 10, yb[0] % 10 % 3]++; ;
            //统计3X3及牌数 //去重   jinFB[yb[i] / 10, yb[i] % 10 % 3]++;
            for (int i = 1; i < yb.Length; i++)
                if (yb[i] > 0 && yb[i] < 30 && yb[i] != yb[i - 1])
                {
                    int x = yb[i] / 10;
                    int y = yb[i] % 10 % 3;
                    jinFB[x, y]++;
                }

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                        if (i == j || i == k || k == j)
                            continue;
                        else if (jinFB[0, i] + jinFB[1, j] + jinFB[2, k] > maxNum)
                            maxNum = jinFB[0, i] + jinFB[1, j] + jinFB[2, k];

            return maxNum;
        }

        public int Max147_258_369KeyNum1(int[] yb)
        {
            int maxNum = 0;  //最多的筋 
            for (int i = 0; i < 6; i++)
            {
                int cc = 0;
                for (int j = 0; j < 9; j++)
                    if (FanCardData.HandIn31[ZuHeCard[i, j]] > 0)
                        cc++;
                if (cc > maxNum)
                    maxNum = cc;
            }
            return maxNum;
        }

        public bool IfMax147_258_369KeyNumIs9(int[] yb)
        {
            bool IsNine = true;  //最多的筋 
            for (int i = 0; i < 6; i++)
            {
                IsNine = true;
                for (int j = 0; j < 9; j++)
                    if (FanCardData.HandIn31[ZuHeCard[i, j]] == 0)
                    {
                        IsNine = false;
                        break;
                    }
                if (IsNine)
                    break;
            }
            return IsNine;
        }

        public void MaxZHL147_258_369Keys(int[] yb, out int[][] JinKeys, out int minGap)
        {
            minGap = 14;  //最多的筋 
            int[,] jinMax = new int[3, 3];//最多的筋
            int[][] currJinKey = new int[10][]; JinKeys = new int[0][];
            if (LongOfCardNZ(yb) <= 14 - 6)  //两碰后无组合龙、全不靠
                return;

            int[,] jinFB = new int[3, 3];//筋
            int[] tmp = new int[yb.Length];
            int[] tmp14 = new int[yb.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            yb.CopyTo(tmp, 0);

            for (int i = 1; i < tmp.Length; i++)
                if (tmp[i] > 0 && tmp[i] == tmp[i - 1])
                    tmp[i - 1] = 0;

            //统计3X3及牌数
            for (int i = 0; i < tmp.Length; i++)
            {
                if (tmp[i] > 0 && tmp[i] < 30)
                    jinFB[tmp[i] / 10, tmp[i] % 10 % 3]++;
                if (yb[i] > 0)
                    yb31[yb[i]]++;
            }

            int cc = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                    {
                        if (i == j || i == k || k == j)
                            continue;

                        currJinKey[cc] = new int[3];
                        currJinKey[cc][0] = i == 0 ? 3 : i;
                        currJinKey[cc][1] = j == 0 ? 3 : j;
                        currJinKey[cc][2] = k == 0 ? 3 : k;

                        int NeedNum = 0;
                        int[] NeedJin = new int[9];//需要的筋 
                        for (int ii = 1; ii < 30; ii++)
                            if (ii % 10 != 0 && yb31[ii] == 0 && currJinKey[cc][ii / 10] % 3 == ii % 10 % 3)
                                NeedJin[NeedNum++] = ii;
                        //需要的筋里的最小张数
                        int currMinZHang = 9;
                        for (int ii = 0; ii < NeedNum; ii++)
                            if (NeedJin[ii] > 0)
                                currMinZHang = Math.Min(currMinZHang, KnownRemainCard[NeedJin[ii]]);
                        if (currMinZHang == 0)//出现了断张
                        {
                            currJinKey[cc][0] = currJinKey[cc][1] = currJinKey[cc][2] = 0;
                            continue;
                        }
                        //去筋 
                        yb.CopyTo(tmp, 0);
                        for (int v = 0; v < tmp.Length; v++)
                        {
                            if (tmp[v] == 0 || tmp[v] > 30 || v < tmp.Length - 1 &&
                                tmp[v] == tmp[v + 1])
                                continue;
                            if (tmp[v] % 10 % 3 == currJinKey[cc][tmp[v] / 10] % 3)
                                tmp[v] = 0;
                        }

                        //同等筋数时，牌距最小
                        int yblen = Mahjong.LongOfCardNZ(yb);
                        int NormGap = MaxNormHuGap(tmp, 3 + (14 - yblen) / 3);
                        int currJinLen = jinFB[0, i] + jinFB[1, j] + jinFB[2, k];

                        if (9 - currJinLen + NormGap > minGap)
                        {
                            currJinKey[cc] = null;
                            continue;
                        }
                        else if (9 - currJinLen + NormGap < minGap)
                        {
                            cc = 0;
                            Array.Clear(currJinKey, 0, currJinKey.Length);
                            currJinKey[0] = new int[3];
                            currJinKey[0][0] = i == 0 ? 3 : i;
                            currJinKey[0][1] = j == 0 ? 3 : j;
                            currJinKey[0][2] = k == 0 ? 3 : k;
                        }
                        minGap = 9 - currJinLen + NormGap;
                        cc++;
                    }
            JinKeys = new int[cc][]; //== 0 ? 1 : cc
            for (int i = 0; i < cc; i++)
            {
                JinKeys[i] = new int[3];
                for (int j = 0; j < 3; j++)
                    JinKeys[i][j] = currJinKey[i][j];
            }
            if (cc > 2)
            { }
            else if (cc > 1)
            { }
            else
            { }
            return;
        }

        //组合龙的最大筋 
        public void MaxZHL147_258_369Key(int[] yb, out int[] JinKey, out int maxJin)
        {
            maxJin = 0;  //最多的筋 
            int[,] jinMax = new int[3, 3];//最多的筋
            JinKey = new int[3];
            if (LongOfCardNZ(yb) <= 14 - 6)  //两碰后无组合龙、全不靠
                return;

            int[,] jinFB = new int[3, 3];//筋
            int[] tmp = new int[yb.Length];
            int[] tmp14 = new int[yb.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            yb.CopyTo(tmp, 0);

            for (int i = 1; i < tmp.Length; i++)
                if (tmp[i] > 0 && tmp[i] == tmp[i - 1])
                    tmp[i - 1] = 0;

            //统计3X3及牌数
            for (int i = 0; i < tmp.Length; i++)
            {
                if (tmp[i] > 0 && tmp[i] < 30)
                    jinFB[tmp[i] / 10, tmp[i] % 10 % 3]++;
                if (yb[i] > 0)
                    yb31[yb[i]]++;
            }

            int[] NeedJin = new int[9];//需要的筋 
            int NeedNum = 0;
            int minGap = 88;
            int MaxMinZHang = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                    {
                        if (i == j || i == k || k == j)
                            continue;
                        if (jinFB[0, i] + jinFB[1, j] + jinFB[2, k] < maxJin)
                            continue;
                        //需要的有效筋
                        int[] currJinKey = new int[3];
                        NeedNum = 0;
                        currJinKey[0] = i == 0 ? 3 : i;
                        currJinKey[1] = j == 0 ? 3 : j;
                        currJinKey[2] = k == 0 ? 3 : k;
                        Array.Clear(NeedJin, 0, NeedJin.Length);
                        for (int ii = 1; ii < 30; ii++)
                            if (ii % 10 != 0 && yb31[ii] == 0 && currJinKey[ii / 10] % 3 == ii % 10 % 3)
                                NeedJin[NeedNum++] = ii;
                        //需要的筋里的最小张数
                        int currMinZHang = 9;
                        for (int ii = 0; ii < NeedNum; ii++)
                            if (NeedJin[ii] > 0)
                                currMinZHang = Math.Min(currMinZHang, KnownRemainCard[NeedJin[ii]]);
                        if (currMinZHang == 0)//出现了断张
                            continue;
                        int currJin = jinFB[0, i] + jinFB[1, j] + jinFB[2, k];

                        //去筋 
                        yb.CopyTo(tmp, 0);
                        for (int v = 0; v < tmp.Length; v++)
                        {
                            if (tmp[v] == 0 || tmp[v] > 30 || v < tmp.Length - 1 &&
                                tmp[v] == tmp[v + 1])
                                continue;
                            if (tmp[v] % 10 % 3 == currJinKey[tmp[v] / 10] % 3)
                                tmp[v] = 0;
                        }

                        //同等筋数时，牌距最小
                        int ccc = Mahjong.LongOfCardNZ(yb);
                        int gap = MaxNormHuGap(tmp, 3 + (14 - ccc) / 3);
                        if (gap + 9 - currJin == minGap && currMinZHang > MaxMinZHang)
                        {
                            MaxMinZHang = currMinZHang;
                            currJinKey.CopyTo(JinKey, 0);
                        }
                        if (gap + 9 - currJin < minGap)
                        {
                            maxJin = currJin;
                            MaxMinZHang = currMinZHang;
                            minGap = gap + 9 - currJin;
                            currJinKey.CopyTo(JinKey, 0);
                        }
                    }
            return;
        }

        //全不靠的最大筋 
        public void MaxQBK147_258_369Keys(int[] yb, out int[][] JinKeys, out int maxJin)
        {
            maxJin = 0;  //最多的筋 
            int[,] jinMax = new int[3, 3];//最多的筋
            JinKeys = new int[10][];
            if (LongOfCardNZ(yb) < 13)  //碰后无全不靠
                return;

            int[,] jinFB = new int[3, 3];//筋
            int[][] currJinKey = new int[10][];
            int[] tmp = new int[yb.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            yb.CopyTo(tmp, 0);

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    yb31[yb[i]]++;

            for (int i = 1; i < tmp.Length; i++)
                if (tmp[i] > 0 && tmp[i] == tmp[i - 1])
                    tmp[i - 1] = 0;

            //统计3X3及牌数
            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] > 0 && tmp[i] < 30)
                    jinFB[tmp[i] / 10, tmp[i] % 10 % 3]++;

            int[] KRC = KnownRemainCard;
            int cc = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                    {
                        if (i == j || i == k || k == j)
                            continue;
                        currJinKey[cc] = new int[3];
                        currJinKey[cc][0] = i == 0 ? 3 : i;
                        currJinKey[cc][1] = j == 0 ? 3 : j;
                        currJinKey[cc][2] = k == 0 ? 3 : k;

                        int[] NeedCard = new int[14];//需要的筋 
                        int NeedNum = 0;
                        int ZeroJin = 0;

                        for (int ii = 1; ii < 30; ii++)
                            if (ii % 10 != 0 && yb31[ii] == 0 && currJinKey[cc][ii / 10] % 3 == ii % 10 % 3)
                                NeedCard[NeedNum++] = ii;
                        for (int ii = 31; ii < yb31.Length; ii++)
                            if (NeedNum < NeedCard.Length && ii % 2 != 0 && yb31[ii] == 0)
                                NeedCard[NeedNum++] = ii;
                        for (int ii = 0; ii < NeedCard.Length; ii++)
                            if (NeedCard[ii] > 0 && KRC[NeedCard[ii]] == 0)
                                ZeroJin++;
                        if (ZeroJin > 2)
                        {
                            currJinKey[cc][0] = currJinKey[cc][1] = currJinKey[cc][2] = 0;
                            continue;
                        }

                        //需要的筋里的最小张数
                        int currMinZHang = 4;
                        for (int ii = 0; ii < NeedNum; ii++)
                            if (NeedCard[ii] > 0)
                                currMinZHang = Math.Min(currMinZHang, KnownRemainCard[NeedCard[ii]]);
                        int currJin = jinFB[0, i] + jinFB[1, j] + jinFB[2, k];
                        if (currJin < maxJin)
                        {
                            currJinKey[cc] = null;
                            continue;
                        }
                        else if (currJin > maxJin)
                        {
                            cc = 0;
                            Array.Clear(currJinKey, 0, currJinKey.Length);
                            currJinKey[0] = new int[3];
                            currJinKey[0][0] = i == 0 ? 3 : i;
                            currJinKey[0][1] = j == 0 ? 3 : j;
                            currJinKey[0][2] = k == 0 ? 3 : k;
                            maxJin = currJin;
                        }
                        cc++;
                    }
            JinKeys = new int[cc][];
            for (int i = 0; i < cc; i++)
            {
                JinKeys[i] = new int[3];
                for (int j = 0; j < 3; j++)
                    JinKeys[i][j] = currJinKey[i][j];
            }
            if (cc > 2)
            { }
            else if (cc > 1)
            { }
            else
            { }
            return;
        }

        //全不靠的最大筋 
        public int[,] MaxQBK147_258_369Key(int[] yb, out int[] JinKey, out int maxJin)
        {
            maxJin = 0;  //最多的筋 
            int[,] jinMax = new int[3, 3];//最多的筋
            JinKey = new int[3];
            if (LongOfCardNZ(yb) < 13)  //碰后无全不靠
                return jinMax;

            int[,] jinFB = new int[3, 3];//筋
            int[] tmp = new int[yb.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            yb.CopyTo(tmp, 0);

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    yb31[yb[i]]++;

            for (int i = 1; i < tmp.Length; i++)
                if (tmp[i] > 0 && tmp[i] == tmp[i - 1])
                    tmp[i - 1] = 0;

            //统计3X3及牌数
            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] > 0 && tmp[i] < 30)
                    jinFB[tmp[i] / 10, tmp[i] % 10 % 3]++;

            int[] KRC = KnownRemainCard;
            int MaxMinZHang = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                    {
                        if (i == j || i == k || k == j)
                            continue;
                        int[] currJinKey = new int[3];
                        currJinKey[0] = i == 0 ? 3 : i;
                        currJinKey[1] = j == 0 ? 3 : j;
                        currJinKey[2] = k == 0 ? 3 : k;

                        int[] NeedCard = new int[14];//需要的筋 
                        int NeedNum = 0;
                        int ZeroJin = 0;

                        for (int ii = 1; ii < 30; ii++)
                            if (ii % 10 != 0 && yb31[ii] == 0 && currJinKey[ii / 10] % 3 == ii % 10 % 3)
                                NeedCard[NeedNum++] = ii;
                        for (int ii = 31; ii < yb31.Length; ii++)
                            if (NeedNum < NeedCard.Length && ii % 2 != 0 && yb31[ii] == 0)
                                NeedCard[NeedNum++] = ii;
                        for (int ii = 0; ii < NeedCard.Length; ii++)
                            if (NeedCard[ii] > 0 && KRC[NeedCard[ii]] == 0)
                                ZeroJin++;
                        if (ZeroJin > 2)
                            continue;

                        //需要的筋里的最小张数
                        int currMinZHang = 4;
                        for (int ii = 0; ii < NeedNum; ii++)
                            if (NeedCard[ii] > 0)
                                currMinZHang = Math.Min(currMinZHang, KnownRemainCard[NeedCard[ii]]);
                        int currJin = jinFB[0, i] + jinFB[1, j] + jinFB[2, k];
                        if (currJin == maxJin && currMinZHang > MaxMinZHang)
                        {
                            MaxMinZHang = currMinZHang;
                            currJinKey.CopyTo(JinKey, 0);
                        }
                        if (currJin > maxJin)
                        {
                            maxJin = currJin;
                            MaxMinZHang = currMinZHang;
                            currJinKey.CopyTo(JinKey, 0);
                        }
                    }
            for (int i = 31; i < yb31.Length; i++)
                if (yb31[i] > 0)
                    maxJin++;
            //每个最大筋数
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    int a = jinFB[(i + 1) % 3, (j + 1) % 3] + jinFB[(i + 2) % 3, (j + 2) % 3];
                    int b = jinFB[(i + 1) % 3, (j + 2) % 3] + jinFB[(i + 2) % 3, (j + 1) % 3];
                    jinMax[i, j] = jinFB[i, j] + Math.Max(a, b);
                }
            return jinMax;
        }




        public double[] ClntPutOutCard(int[] yb, double[] PRC31)
        {
            Array.Sort(yb);
            double[] HuNum1 = new double[14];//一进一出
            double[] HuNum2 = new double[14];//二进二出
            double[] HuNum3 = new double[14];//三进三出 
            double[] HuNum5 = new double[14];//前四个按进度与权重汇总   

            return HuNum5;
        }


        /// <summary>
        /// 以最快胡牌为优先.
        /// IfShow：是不是显示调度信息
        /// </summary> 
        public double[] PriorityFastestHuCard(int[] yb, string Father = "", bool IfShow = false)
        {
            CheckHandCard(yb);
            Array.Sort(yb);
            for (int i = 0; i < AllFanTab.Length; i++) Array.Clear(AllFanTab[i], 0, AllFanTab[i].Length);

            int[] gap = new int[5];
            int[] JinZHLKey, JinQBKKey; int jinNum, jinNum1;
            gap[0] = MaxNormHuGap(yb, (14 - Mahjong.LongOfCardNZ(yb)) / 3);
            gap[1] = 14 - MaxZuHeLongKey(yb, out JinZHLKey, out jinNum);
            gap[2] = 14 - MaxQuanBuKaoKey(yb, out JinQBKKey, out jinNum1);
            gap[3] = 14 - Max13YaoKey(yb);
            gap[4] = Max7PairKey(yb);

            double[] HuNum = new double[14];//二进二出 
            double[] HuNum1 = new double[14];//二进二出 

            int Level = MinOfArray(gap);
            double[][] Nums = new double[8][]; 
            HuNum = HuCard(yb, Level, gap, out Nums, Father); 
            string StudyInfo = CreateAllFanTabInfo(yb);

            SafetyRatio14 = SafetyRatioBasedOutCard(yb, Level - 1);
            if (!this.AiPlay)//此处区别鉴定各类算法botzone
                for (int i = 0; i < HuNum.Length; i++)
                    HuNum[i] = SafetyRatio14[i] * HuNum[i];

            if (LongOfCardNZ(HuNum) == 0)
                HuNum[13] = 1;
            HuNum = NormalizeArrayDouble(HuNum, 1000, 0); 
            return HuNum;
        }

        public int[] MaxNormHuGapTest(int[] yb)
        {
            int[] yb31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                yb31[yb[i]] += 1;
            yb31[0] = 0;

            int[] gap14 = new int[yb.Length];
            int[] tmp = new int[yb.Length];
            for (int i = 0; i < yb.Length; i++)
            {
                yb.CopyTo(tmp, 0);
                tmp[i] = 0;
                Array.Sort(tmp);
                gap14[i] = MaxNormHuGap(tmp, (14 - Mahjong.LongOfCardNZ(tmp)) / 3);
            }

            int[] gap34 = new int[KnownRemainCard.Length];
            tmp = new int[yb.Length + 1];
            for (int i = 1; i < KnownRemainCard.Length - 1; i++)
            {
                if (i % 10 == 0 || i > 30 && i % 2 == 0)
                    continue;
                if (yb31[i - 1] + yb31[i] + yb31[i + 1] == 0)
                    continue;
                Array.Clear(tmp, 0, tmp.Length);
                yb.CopyTo(tmp, 0);
                tmp[14] = i;
                Array.Sort(tmp);
                gap34[i] = MaxNormHuGap(tmp, (tmp.Length - Mahjong.LongOfCardNZ(tmp)) / 3);
            }
            int max = MaxOfArray(gap34);
            for (int i = 1; i < KnownRemainCard.Length - 1; i++)
                if (gap34[i] == max)
                    gap34[i] = 0;
            int Len = LongOfCardNZ(gap34);
            int cc = 0;
            tmp = new int[Len];
            for (int i = 1; i < KnownRemainCard.Length - 1; i++)
                if (gap34[i] > 0 && (yb31[i] > 1 || i > 1 && yb31[i - 2] * yb31[i - 1] > 0 ||
                    yb31[i + 1] * yb31[i - 1] > 0 || i < 31 && yb31[i + 2] * yb31[i + 1] > 0))
                    tmp[cc++] = i;

            Len = LongOfCardNZ(tmp);
            cc = 0;
            int[] ret = new int[Len];
            for (int i = 0; i < Len; i++)
                ret[cc++] = tmp[i];
            return ret;
        }


        public double[] EvaluateCards13Test(int[] yb, double[] HuNumIn)
        {
            int[] tmp14 = new int[yb.Length];
            double[] evals = new double[yb.Length];
            int[] Levels = new int[yb.Length];
            double[][] Nums = new double[8][];

            for (int i = 0; i < yb.Length; i++)
                if (i > 0 && yb[i] == yb[i - 1])
                { evals[i] = evals[i - 1]; Levels[i] = Levels[i - 1]; }
                else if (yb[i] > 0)//* HuNumIn[i]
                {
                    yb.CopyTo(tmp14, 0);
                    tmp14[i] = 0;
                    evals[i] = evaluateCards13(tmp14, out Mahjong.EvalCPGHs[i]);
                }
            double[] evals2 = NormalizeArrayDouble(evals, 1000, 0);
            double[] evals3 = new double[yb.Length];
            int minLevel = MinOfArrayNZ(Levels);
            for (int i = 0; i < yb.Length; i++)
                if (Levels[i] > minLevel)
                {
                    evals3[i] = evals2[i];
                    evals2[i] = 0;
                }

            if (minLevel < 12 && MaxOfArray(evals2) < 1000)
            {
                for (int i = 0; i < AllFanTab.Length; i++)
                    Array.Clear(AllFanTab[i], 0, AllFanTab[i].Length);
                int[] gap = new int[5];
                int[] JinZHLKey, JinQBKKey; int jinNum, jinNum1;
                gap[0] = MaxNormHuGap(yb, (14 - Mahjong.LongOfCardNZ(yb)) / 3);
                gap[1] = 14 - MaxZuHeLongKey(yb, out JinZHLKey, out jinNum);
                gap[2] = 14 - MaxQuanBuKaoKey(yb, out JinQBKKey, out jinNum1);
                gap[3] = 14 - Max13YaoKey(yb);
                gap[4] = Max7PairKey(yb);
                double[] HuNum = new double[14];
                int Level = MinOfArray(gap);
                while (LongOfCardNZ(HuNum) == 0 && Level <= 6)
                    HuNum = Hu5C(yb, Level++, gap, out Nums);
                Level--;
                int ALLcount = 0;
                for (int i = 0; i < KnownRemainCard.Length; i++) ALLcount += KnownRemainCard[i];
                Debug.WriteLine("----------------" + ALLcount + " " + LongOfCardNZ(Mahjong.AllBottomCard));
                Debug.WriteLine(CreateAllFanTabInfo(yb));

                int maxInd = Math.Max(0, IndexOfMaxInArray(evals3));
                yb.CopyTo(tmp14, 0);
                tmp14[maxInd] = 0;
                double HuNum2 = evaluateCards13(tmp14, out Mahjong.EvalCPGHs[maxInd]);
                Debug.WriteLine(CreateAllFanTabInfo(tmp14));
            }
            return evals;
        }

        public double[] SafetyRatioBasedOutCard(int[] yb, int Level)
        {
            int[] KRC = KnownRemainCard;
            double[] RiskCoefficient = new double[KnownRemainCard.Length];
            double[] SafetyRatio31 = new double[KnownRemainCard.Length];
            double[] SafetyRatio14 = new double[yb.Length];
            int lenAPOC = LongOfCardNZ(Mahjong.AllPutOutCard);
            int[] tmp = new int[lenAPOC];
            Array.Copy(Mahjong.AllPutOutCard, tmp, lenAPOC);
            for (int i = 0; i < lenAPOC - 1; i++)
                for (int j = i + 1; j < lenAPOC; j++)
                    if (tmp[i] == tmp[j])
                    {
                        tmp[i] = 0;
                        break;
                    }
            lenAPOC = LongOfCardNZ(tmp);
            int cc = 0;
            int[] APOC = new int[lenAPOC];
            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] > 0)
                    APOC[cc++] = tmp[i];

            if (84 - LongOfCardNZ(Mahjong.AllPutOutCard) < 40)
            { }
            double pregress = LongOfCardNZ(Mahjong.AllBottomCard) / 84.0;
            double basicRisk = Math.Round((1 - pregress) / 5, 2);
            for (int i = 0; i < RiskCoefficient.Length; i++)
                if (i % 10 == 0 || i > 30 && i % 2 == 0)
                    RiskCoefficient[i] = -1;
                else
                    RiskCoefficient[i] = basicRisk;

            int stepNum = lenAPOC / 2;
            stepNum = stepNum == 0 ? 1 : stepNum;
            double stepLen = (basicRisk) / stepNum;

            for (int i = lenAPOC - 1; i >= stepNum; i--)
                RiskCoefficient[APOC[i]] = Math.Round(stepLen * (lenAPOC - 1 - i), 3);


            for (int i = 1; i < RiskCoefficient.Length - 1; i++)
                if (KRC[i - 1] + KRC[i] + KRC[i + 1] == 0)
                    RiskCoefficient[i] = 0;
            for (int i = 0; i < SafetyRatio14.Length; i++)
                if (yb[i] > 0)
                    SafetyRatio14[i] = 1 - RiskCoefficient[yb[i]];

            for (int i = 0; i < SafetyRatio14.Length; i++)
                if (yb[i] > 0)
                    SafetyRatio14[i] = Math.Round(Math.Pow(SafetyRatio14[i], Level * (1 - pregress)), 2);

            SafetyRatio14 = NormalizeArrayDouble(SafetyRatio14, 1);
            return SafetyRatio14;
        }

        public static double BinomialDistribution(int N, int k, double p)
        {
            if (N == 0 && k == 0)
                return 1.0;
            if (N < 0 || k < 0)
                return 0.0;
            return (1 - p) * BinomialDistribution(N - 1, k, p) + p * BinomialDistribution(N - 1, k - 1, p);
        }

        /// <summary>
        /// 还能摸几张山牌
        /// </summary>
        /// <returns></returns>
        public static double CanFetchCardNum()
        {
            double Progress = LongOfCardNZ(Mahjong.AllBottomCard);
            double num = Progress / Mahjong.PlayingNum;
            return num;
        }


        public int HowManyJiao(int[] yb, int winTile)
        {
            int[] tmp = new int[14];
            int[] jiao = new int[14];
            int count = 0;

            yb.CopyTo(tmp, 0);
            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] == winTile)
                {
                    tmp[i] = 0;
                    break;
                }
            what_jiao(tmp, jiao);
            for (int i = 0; i < jiao.Length; i++)
                if (jiao[i] > 0)
                    count++;
            return count;
        }

        /// <summary>
        /// 最多几张叫
        /// </summary> 
        public double HowManyJiao(int[] yb, double[] PRC31, double[] HuNum)
        {
            int[] tmp = new int[14];
            int[] jiao = new int[14];
            double count = 0, sum = 0;
            if (LongOfCardNZ(HuNum) == 0)
                return 0;

            yb.CopyTo(tmp, 0);
            int iMax = IndexOfMaxInArray(HuNum);
            tmp[iMax] = 0;

            what_jiao(tmp, jiao);
            for (int i = 0; i < jiao.Length; i++)
                if (jiao[i] > 0)
                    count += PRC31[jiao[i]];
            for (int i = 0; i < PRC31.Length; i++)
                sum += PRC31[i];
            if (count > 0)
            {
                //double prob = 1 - Math.Pow(1 - count / sum, sum / Mahjong.PlayingNum);
                //string str = count.ToString("F2") + "   ";
                //str += (count / sum).ToString("F2") + "  " + prob.ToString("F2");
                //Console.Write( str + " " + azimuth_char + "  Jiao=");
                //for (int i = 0; i < jiao.Length; i++)
                //    if(jiao[i] > 0)
                //    {
                //        Console.Write(jiao[i] + ":" + KnownRemainCard[jiao[i]] + "  "); 
                //    }
                //Console.Write(" BL=" + (int)(LongOfCardNZ(AllBottomCard) / Mahjong.PlayingNum) + " YB --> ");
                //PrintArray(tmp, "", 3);
                //if(prob > 0.5 || count / sum > 0.1)
                //{ }
            }
            return count;
        }

        public int DivideShunKe(int[] tmp31, int clearLen, int HuLen, int startPos)
        {
            if (clearLen >= HuLen)
                return clearLen;
            int maxLen = 0, k;
            for (k = startPos; k < tmp31.Length - 2 && maxLen < HuLen; k++)
            {
                if (tmp31[k] == 0)
                    continue;
                if (tmp31[k] >= 3 && maxLen < HuLen)
                {
                    tmp31[k] = tmp31[k] - 3;
                    clearLen += 3;
                    maxLen = Math.Max(maxLen, DivideShunKe(tmp31, clearLen, HuLen, k));
                    clearLen -= 3;
                    tmp31[k] = tmp31[k] + 3;
                }
                if (tmp31[k] * tmp31[k + 1] * tmp31[k + 2] > 0 && maxLen < HuLen)
                {
                    tmp31[k] -= 1; tmp31[k + 1] -= 1; tmp31[k + 2] -= 1;
                    clearLen += 3;
                    maxLen = Math.Max(maxLen, DivideShunKe(tmp31, clearLen, HuLen, k));
                    clearLen -= 3;
                    tmp31[k] += 1; tmp31[k + 1] += 1; tmp31[k + 2] += 1;
                }
            }
            return Math.Max(maxLen, clearLen);
        }

        public bool If_NotNormHu_Cards(int[] yb31, int HuLen)
        {
            int[] tmp31 = new int[yb31.Length + 1];
            int[] tmp = new int[yb31.Length + 1];
            yb31.CopyTo(tmp31, 0);
            int pair = 0, residue = 0;
            //存在010时不能胡
            for (int i = 1; i < tmp31.Length - 1; i++)
            {
                if (tmp31[i] == 0)
                    continue;
                if (tmp31[i] > 4 || tmp31[i] < 0)                   // 4张以上不得行
                    return true;
                if (tmp31[i - 1] + tmp31[i + 1] == 0 && tmp31[i] % 3 == 1) //010型 040型
                    tmp31[i]--;
                if (i < 29 && tmp31[i - 1] + tmp31[i + 2] == 0)  //01n0型 
                {
                    if (tmp31[i] % 3 == 1)
                        tmp31[i]--;
                    if (tmp31[i + 1] % 3 == 1)
                        tmp31[i + 1]--;
                }
            }

            for (int i = 1; i < tmp31.Length - 2; i++)
            {
                if (tmp31[i] == 0) continue;
                if (tmp31[i - 1] + tmp31[i + 1] == 0 && tmp31[i] % 2 == 0) //020型
                    pair += tmp31[i] / 2;
                if (i < 30 && tmp31[i - 1] + tmp31[i + 2] == 0 && tmp31[i] == 2 && tmp31[i + 1] == 2) //0220型
                    pair += 2;
                residue += tmp31[i];
            }
            if (pair > 1) residue -= (pair - 1) * 2;
            if (residue < HuLen)
                return true;

            for (int i = 1; i < 30 - 2; i++)
            {
                if (tmp31[i] == 0) continue;
                if (tmp31[i - 1] + tmp31[i + 3] == 0 && tmp31[i] * tmp31[i + 1] * tmp31[i + 2] == 2) //02110、01210、01120型
                    residue--;
                if (tmp31[i - 1] + tmp31[i + 3] == 0 && tmp31[i] * tmp31[i + 1] * tmp31[i + 2] == 4 &&  //02210、02120型
                    tmp31[i] + tmp31[i + 1] + tmp31[i + 2] == 5)
                    residue--;
                if (tmp31[i - 1] + tmp31[i + 3] == 0 && tmp31[i] * tmp31[i + 1] * tmp31[i + 2] == 6) //02310、03210、03120型
                    residue--;
                if (tmp31[i - 1] + tmp31[i + 4] == 0 && tmp31[i] * tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] == 1) //011110型
                    residue--;
                if (tmp31[i - 1] + tmp31[i + 4] == 0 && tmp31[i] * tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] == 4 &&
                    !(tmp31[i + 1] == 2 && tmp31[i + 2] == 2)) //021120、01212、1141、4111型
                    residue--;
                if (tmp31[i - 1] + tmp31[i + 4] == 0 &&
                    tmp31[i] * tmp31[i + 3] == 1 && tmp31[i + 1] * tmp31[i + 2] == 2) //012110型
                    residue -= 2;
                if (tmp31[i - 1] + tmp31[i + 5] == 0 &&
                    tmp31[i] * tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] * tmp31[i + 4] == 1) //0111110型
                    residue -= 2;
                if (tmp31[i - 1] + tmp31[i + 5] == 0 && tmp31[i + 2] != 2 &&
                    tmp31[i] * tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] * tmp31[i + 4] == 2) //0111110型
                    residue--;
                if (residue < HuLen)
                    return true;
            }

            for (int i = 0; i < tmp31.Length - 1; i++)
            {
                if (tmp31[i] < 2)
                    continue;  //先找到将牌 
                tmp31.CopyTo(tmp, 0);
                tmp[i] = tmp[i] - 2;            //去掉将牌  
                int clearLen = 2;
                if (DivideShunKe(tmp, clearLen, HuLen, 1) >= HuLen)
                    return false;
            }
            return true;
        }

        public bool If_NotNormHu_Cards_old(int[] yb31, int HuLen)
        {
            int[] tmp31 = new int[yb31.Length + 1];
            int[] tmp = new int[yb31.Length + 1];
            yb31.CopyTo(tmp31, 0);
            int pair = 0, residue = 0;
            //存在010时不能胡
            for (int i = 1; i < tmp31.Length - 1; i++)
            {
                if (tmp31[i] == 0)
                    continue;
                if (tmp31[i] > 4 || tmp31[i] < 0)                   // 4张以上不得行
                    return true;

                if (tmp31[i - 1] + tmp31[i + 1] == 0 && tmp31[i] % 3 == 1) //010型 040型
                    tmp31[i]--;

                if (i < 29 && tmp31[i - 1] + tmp31[i + 2] == 0)  //01n0型 
                {
                    if (tmp31[i] % 3 == 1)
                        tmp31[i]--;
                    if (tmp31[i + 1] % 3 == 1)
                        tmp31[i + 1]--;
                }
            }

            for (int i = 1; i < tmp31.Length - 1; i++)
            {
                if (tmp31[i] == 0) continue;
                if (tmp31[i - 1] + tmp31[i + 1] == 0 && tmp31[i] % 2 == 0) //020型
                    pair += tmp31[i] / 2;
                residue += tmp31[i];
            }
            //residue -= pair > 1 ? (pair - 1) * 2 : 0;
            if (pair > 1)
                residue -= (pair - 1) * 2;
            // 成型的牌小于胡牌长度
            if (residue < HuLen)
                return true;

            for (int i = 0; i < tmp31.Length - 1; i++)
            {
                if (tmp31[i] < 2)
                    continue;  //先找到将牌 
                tmp31.CopyTo(tmp, 0);
                tmp[i] = tmp[i] - 2;            //去掉将牌  
                int clearLen = 2;
                for (int k = 0; k < tmp31.Length - 1; k++)
                {
                    //这里可改为直接跳出
                    if (tmp[k] == 0)
                        continue;
                    //有连续的三张牌时, 组成一组, 清除掉。
                    while (tmp[k] > 0 && tmp[k + 1] > 0 && tmp[k + 2] > 0)
                    { tmp[k] -= 1; tmp[k + 1] -= 1; tmp[k + 2] -= 1; clearLen += 3; }
                    //前有三张相同牌时, 成一组, 清除掉。
                    if (tmp[k] >= 3)
                    { tmp[k] = tmp[k] - 3; clearLen += 3; }
                    //判断是否能胡牌，清除的牌能达成胡牌了。			 
                    if (clearLen >= HuLen)
                        return false;
                }
                if (clearLen >= HuLen)
                    return false;
            }
            return true;
        }

        public bool If_Not_5Class(int[] yb31, int HuLen)
        {
            int[] tmp31 = new int[yb31.Length + 1];
            int[] tmp = new int[yb31.Length + 1];
            yb31.CopyTo(tmp31, 0);
            int pair = 0, residue = 0, num19 = 0, JFnum = 0;int MaxKnitNum = 0;//筋 

            if (HuLen >= 10)//得到筋数
                for (int i = 0; i < 6; i++)
                {
                    int KnitNum = 0;
                    for (int j = 0; j < 9; j++)
                        if (yb31[ZuHeCard[i, j]] > 0)
                            KnitNum++;
                    if (KnitNum == 9)
                        return false;
                    if (KnitNum > MaxKnitNum)
                        MaxKnitNum = KnitNum;
                }

            if (HuLen >= 13)
            {
                for (int i = 31; i < KrcLen; i += 2)
                    JFnum += yb31[i] > 0 ? 1 : 0;
                for (int i = 1; i < 30; i++) 
                    if (i % 10 % 8 == 1 && yb31[i] > 0)
                        num19++;
                if (JFnum + num19 == 13)
                    return false;
                if (JFnum + MaxKnitNum == 14)
                    return false;
            }

            //存在010时不能胡
            for (int i = 1; i < tmp31.Length - 1; i++)
            {
                if (tmp31[i] == 0)
                    continue;
                if (tmp31[i] > 4 || tmp31[i] < 0) // 4张以上不得行
                    return true;
                if (tmp31[i - 1] + tmp31[i + 1] == 0 && tmp31[i] == 1) //010型
                    tmp31[i] = 0;
                if (i < 29 && tmp31[i - 1] + tmp31[i + 2] == 0 && tmp31[i] == 1) //01n0型                    
                    tmp31[i] = 0;
                if (i < 29 && tmp31[i - 1] + tmp31[i + 2] == 0 && tmp31[i + 1] == 1)//0n10型  
                    tmp31[i + 1] = 0;
            }
            for (int i = 1; i < tmp31.Length - 1; i++)
            {
                if (tmp31[i] == 0) continue;
                pair += tmp31[i] / 2; 
            }
            if (HuLen == 14 && pair >= 7)
                return false;

            pair = residue = 0;
            for (int i = 1; i < tmp31.Length - 2; i++)
            {
                if (tmp31[i] == 0) continue;
                if (tmp31[i - 1] + tmp31[i + 1] == 0 && tmp31[i] == 2) //020型
                    pair += tmp31[i] / 2;
                if (i < 30 && tmp31[i - 1] + tmp31[i + 2] == 0 && tmp31[i] == 2 && tmp31[i + 1] == 2) //0220型
                    pair += 2;
                residue += tmp31[i];
            }
            residue -= pair > 1 ? (pair - 1) * 2 : 0;
            // 成型的牌小于胡牌长度
            if (residue < HuLen)
                return true;

            for (int i = 0; i < tmp31.Length - 1; i++)
            {
                if (tmp31[i] < 2)
                    continue;  //先找到将牌 
                tmp31.CopyTo(tmp, 0);
                tmp[i] = tmp[i] - 2;            //去掉将牌  
                int clearLen = 2;
                if (DivideShunKe(tmp, clearLen, HuLen, 1) >= HuLen)
                    return false;
            }
            return true;
        }

        public bool If_NotZuHeLong_Cards(int[] yb31, int HuLen)
        {
            for (int i = 1; i < yb31.Length - 1; i++) // 4张以上不得行 
                if (yb31[i] > 4)
                    return true;
            bool IsNine = true;  //最多的筋 
            int numNine = -1;
            for (int i = 0; i < 6; i++)
            {
                IsNine = true;
                for (int j = 0; j < 9; j++)
                    if (yb31[ZuHeCard[i, j]] == 0)
                    {
                        IsNine = false;
                        break;
                    }
                if (IsNine)
                {
                    numNine = i;
                    break;
                }
            }
            if (!IsNine)
                return true;
            int[] tmp = new int[yb31.Length];
            yb31.CopyTo(tmp, 0);
            for (int i = 0; i < 9; i++)
                tmp[ZuHeCard[numNine, i]]--;
            bool bHu = If_NotNormHu_Cards(tmp, HuLen - 9);
            return bHu;
        }

        public bool If_NotBuKao_Cards(int[] yb31)
        {
            int cc1 = 0, cc2 = 0;
            for (int i = 31; i < KrcLen; i += 2)
                if (yb31[i] > 0)
                    cc1 += 1;
            for (int i = 0; i < 6; i++)
            {
                cc2 = 0;
                for (int j = 0; j < 9; j++)
                    if (yb31[ZuHeCard[i, j]] > 0)
                        cc2 += 1;
                if (cc1 + cc2 >= 14)
                    return false;
            }
            return true;
        }

        public bool If_Not13Yao_Cards(int[] yb31)
        {
            for (int i = 1; i < KrcLen; i++)
            {
                if (i < 30 && i % 10 % 8 == 1 && yb31[i] == 0)
                    return true;
                else if (i > 30 && i % 2 == 1 && yb31[i] == 0)
                    return true;
            }
            return false;
        }

        public bool If_Not7Pair_Cards(int[] yb31)
        {
            int cc = 0;
            for (int i = 1; i < yb31.Length - 1; i++)
                cc += yb31[i] / 2;
            if (cc < 7)
                return true;
            else
                return false;
        }

        public bool FastDetermineNoHu5Class(int[] yb)
        {
            int[] tmp31 = new int[KrcLen];
            for (int i = 0; i < yb.Length; i++)
                tmp31[yb[i]]++;
            tmp31[0] = 0;

            int ybLen = LongOfCardNZ(yb);
            int num19 = 0, JFnum = 0; int MaxKnitNum = 0;//筋 

            if (ybLen >= 10)//得到筋数
                for (int i = 0; i < 6; i++)
                {
                    int KnitNum = 0;
                    for (int j = 0; j < 9; j++)
                        if (tmp31[ZuHeCard[i, j]] > 0)
                            KnitNum++;
                    if (KnitNum == 9)
                        return false;
                    if (KnitNum > MaxKnitNum)
                        MaxKnitNum = KnitNum;
                }

            if (ybLen >= 13)
            {
                for (int i = 31; i < KrcLen; i += 2)
                    JFnum += tmp31[i] > 0 ? 1 : 0;
                for (int i = 1; i < 30; i++)
                    if (i % 10 % 8 == 1 && tmp31[i] > 0)
                        num19++;
                if (JFnum + num19 >= 13)
                    return false;
                if (JFnum + MaxKnitNum >= 14)
                    return false;
            }

            //存在010时不能胡
            for (int i = 0; i < tmp31.Length - 2; i++)
                if (tmp31[i] + tmp31[i + 2] == 0 && tmp31[i + 1] == 1)
                    return true;
            //存在01N0,0N10时不能胡
            for (int i = 0; i < tmp31.Length - 3; i++)
                if (tmp31[i] + tmp31[i + 3] == 0 && (tmp31[i + 1] == 1 || tmp31[i + 2] == 1))
                    return true;

            for (int i = 0; i < tmp31.Length - 4; i++)
            {
                int multiplication = tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3];
                if (tmp31[i] + tmp31[i + 4] == 0 && multiplication > 0)
                {
                    //存在01120,01210,02110时不能胡
                    if (multiplication == 2)
                        return true;
                    //存在01220,02120,02210时不能胡 
                    if (tmp31[i + 1] + tmp31[i + 2] + tmp31[i + 3] == 5 &&
                        multiplication == 4)
                        return true;
                    if (multiplication == 6)
                        return true;
                    { }
                }
            }
            return false;
        }
        public bool FastDetermineNoHu31(int[] tmp31)
        {
            //存在010、040时不能胡
            for (int i = 0; i < tmp31.Length - 2; i++)
                if (tmp31[i] + tmp31[i + 2] == 0 && tmp31[i + 1] % 3 == 1)
                    return true;

            for (int i = 0; i <= 27; i++)
                if (tmp31[i] + tmp31[i + 3] == 0)
                {
                    if (tmp31[i + 1] % 3 == 1 || tmp31[i + 2] % 3 == 1)//存在01N0,0N10时不能胡
                        return true;
                    if (tmp31[i + 1] * tmp31[i + 2] == 4)//存在0140,0220时不能胡
                        return true;
                }
            int num3 = 0;
            for (int i = 0; i <= 26; i++)
                if (tmp31[i] + tmp31[i + 4] == 0)
                {
                    num3 = tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3];
                    if (num3 == 2)     //存在01120,01210,02110时不能胡 
                        return true;
                    if (num3 == 4 &&  //存在01220,02120,02210时不能胡
                        tmp31[i + 1] + tmp31[i + 2] + tmp31[i + 3] == 5)
                        return true;
                    if (num3 == 6)     //存在03120时不能胡 
                        return true;
                    if (tmp31[i + 1] * tmp31[i + 3] == 4 && tmp31[i + 2] == 0)     //存在02020时不能胡 
                        return true;
                }
            int num2 = 0;
            for (int i = 0; i <= 25; i++)
                if (tmp31[i] + tmp31[i + 5] == 0)
                {
                    if (tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] * tmp31[i + 4] == 1)     //存在011110时不能胡 
                        return true;
                    num2 = tmp31[i + 2] * tmp31[i + 3];
                    if (tmp31[i + 1] * tmp31[i + 4] == 1 && (num2 == 2 || num2 == 3 || num2 == 6))     //存在012110、 011210时不能胡 
                        return true;
                    if (tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] * tmp31[i + 4] == 4 &&
                        !(tmp31[i + 2] == 2 && tmp31[i + 3] == 2))              //存在012120、 021210 2112时不能胡 
                        return true;
                }
            int num5 = 0;
            for (int i = 0; i <= 24; i++)
                if (tmp31[i] + tmp31[i + 6] == 0)
                {
                    num5 = tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] * tmp31[i + 4] * tmp31[i + 5];
                    if (num5 == 1)     //存在0111110、0131110时不能胡 
                        return true;
                    if (num5 == 2 && tmp31[i + 3] != 2)     //存在0111110时不能胡 
                        return true;
                    if (num5 == 3)     //存在0131110时不能胡 
                        return true;
                }
            int ct = 0;
            for (int i = 0; i <= 28; i++)
                if (tmp31[i] + tmp31[i + 2] == 1 && tmp31[i + 1] == 2)//计数021、120
                    if (++ct > 1)
                        return true;
            return false;
        }

        public bool FastDetermineNo7Pair(int[] yb)
        {
            Array.Sort(yb);
            //存在010时不能胡
            for (int i = 0; i < yb.Length - 1; i += 2)
                if (yb[i] == 0 || yb[i] != yb[i + 1])
                    return true;
            for (int i = 0; i < yb.Length - 5; i++)
                if (yb[i] == yb[i + 4])
                    return true;
            return false;
        }

        /// <summary>
        /// 快速判定不能胡牌。用于加快速度
        /// </summary>
        /// <param name="yb"></param>
        /// <returns></returns>不能胡，返回true
        public bool FastDetermineNoHu()
        {
            int[] tmp31 = FanCardData.HandIn31;

            //存在010时不能胡
            for (int i = tmp31.Length - 3; i >= 0; i--)
                if (tmp31[i] + tmp31[i + 2] == 0 && tmp31[i + 1] == 1)
                    return true;
            //存在01N0,0N10时不能胡
            for (int i = 0; i < tmp31.Length - 3; i++)
                if (tmp31[i] + tmp31[i + 3] == 0 && tmp31[i + 1] * tmp31[i + 2] > 0)
                    if (tmp31[i + 1] == 1 || tmp31[i + 2] == 1)
                        return true;

            for (int i = 0; i < tmp31.Length - 4; i++)
                if (tmp31[i] + tmp31[i + 4] == 0 && tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] > 0)
                {
                    //存在01120,01210,02110时不能胡
                    if (tmp31[i + 1] + tmp31[i + 2] + tmp31[i + 3] == 4)
                        return true;
                    //存在01220,02120,02210时不能胡 
                    if (tmp31[i + 1] + tmp31[i + 2] + tmp31[i + 3] == 5)
                        if (tmp31[i + 1] == 2 || tmp31[i + 2] == 2 || tmp31[i + 3] == 2)
                            return true;
                }
            //存在011110,011210,013110,012110时不能胡
            for (int i = 0; i < tmp31.Length - 5; i++)
                if (tmp31[i] + tmp31[i + 5] == 0 && tmp31[i + 1] * tmp31[i + 4] == 1)
                    if (tmp31[i + 2] * tmp31[i + 3] == 1 || tmp31[i + 2] * tmp31[i + 3] == 2
                        || tmp31[i + 2] * tmp31[i + 3] == 3)
                        return true;

            return false;
        }

        public bool FDNoHu_All_11_122()//0209
        {
            int[] tmp31 = FanCardData.HandIn31;
            //存在0110不能胡
            for (int i = 0; i < 30 - 2; i++)
                if (tmp31[i] + tmp31[i + 3] == 0 && tmp31[i + 1] * tmp31[i + 2] == 1)
                    return true;
            //存在01220,02120,02210时不能胡 
            for (int i = 0; i < 30 - 3; i++)
                if (tmp31[i] + tmp31[i + 4] == 0 && tmp31[i + 1] * tmp31[i + 2] * tmp31[i + 3] > 3)
                    if (tmp31[i + 1] + tmp31[i + 2] + tmp31[i + 3] == 5)
                        if (tmp31[i + 1] == 2 || tmp31[i + 2] == 2 || tmp31[i + 3] == 2)
                            return true;
            //存在01010时不能胡 
            //for (int i = 0; i < 3; i++)
            //    for (int j = 2; j < 9; j++)
            //        if (tmp31[i * 10 + j - 2] + tmp31[i * 10 + j + 2] == 0)
            //            if (tmp31[i * 10 + j - 1] * tmp31[i * 10 + j + 1] == 1 && tmp31[i * 10 + j] == 0)
            //                return true;
            return false;
        }

        public bool FDNotHu23(int[] tmp)
        {
            Array.Clear(FanCardData.HandIn31, 0, FanCardData.HandIn31.Length);
            for (int kk = 0; kk < tmp.Length; kk++)
                FanCardData.HandIn31[tmp[kk]]++;
            FanCardData.HandIn31[0] = 0;

            //存在01N0,0N10时不能胡
            for (int i = 0; i < FanCardData.HandIn31.Length - 3; i++)
                if (FanCardData.HandIn31[i] == 0 && FanCardData.HandIn31[i + 1] > 0 && FanCardData.HandIn31[i + 2] > 0 && FanCardData.HandIn31[i + 3] == 0)
                    if (FanCardData.HandIn31[i + 1] == 1 || FanCardData.HandIn31[i + 2] == 1)
                        return true;
            //存在01120,01210,02110时不能胡
            for (int i = 0; i < FanCardData.HandIn31.Length - 4; i++)
                if (FanCardData.HandIn31[i] == 0 && FanCardData.HandIn31[i + 1] > 0 && FanCardData.HandIn31[i + 2] > 0 && FanCardData.HandIn31[i + 3] > 0 && FanCardData.HandIn31[i + 4] == 0)
                    if (FanCardData.HandIn31[i + 1] + FanCardData.HandIn31[i + 2] + FanCardData.HandIn31[i + 3] == 4)
                        return true;
            //存在01220,02120,02210时不能胡
            for (int i = 0; i < FanCardData.HandIn31.Length - 4; i++)
                if (FanCardData.HandIn31[i] == 0 && FanCardData.HandIn31[i + 1] > 0 && FanCardData.HandIn31[i + 2] > 0 && FanCardData.HandIn31[i + 3] > 0 && FanCardData.HandIn31[i + 4] == 0)
                    if (FanCardData.HandIn31[i + 1] + FanCardData.HandIn31[i + 2] + FanCardData.HandIn31[i + 3] == 5)
                        if (FanCardData.HandIn31[i + 1] == 2 || FanCardData.HandIn31[i + 2] == 2 || FanCardData.HandIn31[i + 3] == 2)
                            return true;
            return false;
        }

        public static bool FastDeterNoHu(int[] yb)
        {
            int[] tmp31 = new int[KrcLen + 2];

            for (int i = 0; i < yb.Length; i++)
                tmp31[yb[i]]++;
            tmp31[0] = 0;
            //存在010时不能胡
            for (int i = tmp31.Length - 4; i > 0; i--)
                if (tmp31[i] == 0 && tmp31[i + 1] == 1 && tmp31[i + 2] == 0)
                    return true;
            //存在01N0,0N10时不能胡
            for (int i = 0; i < tmp31.Length - 3; i++)
                if (tmp31[i] == 0 && tmp31[i + 1] > 0 && tmp31[i + 2] > 0 && tmp31[i + 3] == 0)
                    if (tmp31[i + 1] == 1 || tmp31[i + 2] == 1)
                        return true;
            //存在01120,01210,02110时不能胡
            for (int i = 0; i < tmp31.Length - 4; i++)
                if (tmp31[i] == 0 && tmp31[i + 1] > 0 && tmp31[i + 2] > 0 && tmp31[i + 3] > 0 && tmp31[i + 4] == 0)
                    if (tmp31[i + 1] + tmp31[i + 2] + tmp31[i + 3] == 4)
                        return true;
            //存在01220,02120,02210时不能胡
            for (int i = 0; i < tmp31.Length - 4; i++)
                if (tmp31[i] == 0 && tmp31[i + 1] > 0 && tmp31[i + 2] > 0 && tmp31[i + 3] > 0 && tmp31[i + 4] == 0)
                    if (tmp31[i + 1] + tmp31[i + 2] + tmp31[i + 3] == 5)
                        if (tmp31[i + 1] == 2 || tmp31[i + 2] == 2 || tmp31[i + 3] == 2)
                            return true;
            return false;
        }


        /// <summary>
        /// 这个牌能杠的可能性增加，所以酌情保留
        /// 得分概率：6/4 + 3 * 4 * 1/3 * 1/4 = 10/4
        /// 胡牌得分概率：6/4 + 3 * 1 * 1/3 * 1/4 = 7/4
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="KnownRemain"></param>
        /// <returns></returns>
        public double ConsiderAnGang(int[] yb, int inc)
        {
            int[] tmp = new int[KnownRemainCard.Length];
            double[] coff = new double[] { 1.33, 1.66, 2 };

            for (int i = 0; i < yb.Length; i++)
                tmp[yb[i]]++;
            tmp[0] = 0;

            int count = 0;
            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] == 4 && i == inc)
                    count += 3;
            if (count > 0)
                return 2 * coff[Mahjong.PlayingNum - 2];
            else
                return 0;
        }

        /// 手中有一对，这个牌能杠的可能性增加，所以酌情保留
        /// 得分概率：6/(4 * 4) + 1/4 * (3 * 4 * 1/4 * 1/4) = 9/16
        /// (9/16) / (7/4) = 0.32
        public double ConsiderAnGang(int[] yb, int inc1, int inc2)
        {
            if (inc1 != inc2)
                return 0;

            int[] tmp = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                tmp[yb[i]]++;
            tmp[0] = 0;

            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] == 4 && i == inc1)
                    return 2;
            return 0;
        }

        /// <summary>
        /// 这个牌能巴杠的可能性增加，所以酌情保留
        /// 得分概率：3/4  (3/4) / (7/4) = 0.43
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="KnownRemain"></param>
        /// <returns></returns>
        public double ConsiderBaGang(int[] yb, int inc)
        {
            int count = 0;
            for (int i = 0; i < BumpCard.Length - 1; i++)
                if (BumpCard[i] > 0 && BumpCard[i] != BumpCard[i + 1])
                    for (int j = 0; j < yb.Length; j++)
                        if (BumpCard[i] == inc)
                        {
                            count += 1;
                            break;
                        }
            if (count > 0)
                return 1;
            else
                return 0;
        }

        /// <summary>
        /// 根据牌之间的最大跳数决定要打的牌.最大的一张
        /// 如:1 4 5 7,lap为:3 1 1 2
        /// </summary>
        /// <param name="yb"></param>
        /// <returns></returns>


        /// 计算牌之间的第一类跳数 .后向跳数
        public int[] Calculate_Lap_Class14(int[] yb)
        {
            int[] lap = new int[yb.Length];
            if (yb.Length < 2)
                return new int[yb.Length];
            //生成跳数  
            for (int i = 0; i < lap.Length - 1; i++)
                if (yb[i] == 0)
                    continue;
                else if (yb[i + 1] / 10 != yb[i] / 10)
                    lap[i] = 80;
                else if (yb[i + 1] == yb[i] && KnownRemainCard[yb[i]] == 0)//孤立死对子
                    lap[i] = 70;
                else if (yb[i + 1] - yb[i] == 2 && KnownRemainCard[yb[i] + 1] == 0)//孤立死嵌张507
                    lap[i] = 60;
                else if (yb[i + 1] - yb[i] == 1 &&
                    KnownRemainCard[yb[i] - 1] + KnownRemainCard[yb[i] + 2] == 0)//孤立死连张0560
                    lap[i] = 50;
                else
                    lap[i] = yb[i + 1] - yb[i];

            lap[lap.Length - 1] = 10;

            //处理跳数10->19,102->192,1021->1929,10210210->19290919
            for (int i = 0; i < lap.Length - 1; i++)
                if (yb[i] > 0)
                {
                    if (lap[i] <= 2 && lap[i + 1] <= 2)
                        lap[i + 1] = 10;
                }

            return lap;
        }

        //在最小剩余牌中计算跳数
        public int[] CalculateLapFormMinResidual(int[] yb31)
        {
            int cc = 0;
            for (int i = 1; i < yb31.Length; i++)
                cc += yb31[i];
            int[] KRC = new int[KnownRemainCard.Length + 2];
            int[] lap14 = new int[cc];
            int[] tmp14 = new int[cc];
            KnownRemainCard.CopyTo(KRC, 0);

            cc = 0;
            for (int k = 0; k < yb31.Length; k++)
                for (int j = 0; j < yb31[k]; j++)
                    tmp14[cc++] = k;
            Array.Sort(tmp14);

            cc = 0;
            for (int i = 1; i < yb31.Length - 1; i++)
            {
                if (yb31[i] == 0)
                    continue;
                else if (yb31[i] == 1)
                {
                    if (yb31[i - 1] + yb31[i + 1] == 0 && KRC[i - 1] + KRC[i] + KRC[i + 1] == 0)//孤立死子 
                        lap14[cc++] = 110;
                    //孤子 100
                    else if (yb31[i + 1] == 0)
                        lap14[cc++] = GetLap(yb31, i);
                    //110型
                    else if (yb31[i + 1] == 1 && yb31[i + 2] == 0)
                    {
                        if (KRC[i - 1] + KRC[i + 2] == 0)//死连张
                            lap14[cc++] = lap14[cc++] = 80;
                        else
                            lap14[cc++] = lap14[cc++] = 20;
                        i++;
                    }
                    //120型
                    else if (yb31[i + 1] == 2 && yb31[i + 2] == 0)
                    {
                        if (KRC[i - 1] + KRC[i + 1] + KRC[i + 2] == 0)//死连张死对子
                            lap14[cc++] = lap14[cc++] = lap14[cc++] = 80;
                        else if (KRC[i - 1] + KRC[i + 2] == 0)//死连张
                        {
                            lap14[cc++] = 80;
                            lap14[cc++] = lap14[cc++] = 10;
                        }
                        else if (KRC[i + 1] == 0)//死对子
                        {
                            lap14[cc++] = lap14[cc++] = 20;
                            lap14[cc++] = 80;
                        }
                        else
                        {
                            if (KRC[i + 1] * 2 >= KRC[i - 1] + KRC[i + 2])//对子形势好
                            {
                                lap14[cc++] = 40;
                                lap14[cc++] = lap14[cc++] = 10;
                            }
                            else//顺子形势好
                            {
                                lap14[cc++] = lap14[cc++] = 20;
                                lap14[cc++] = 40;
                            }
                        }
                        i += 2;
                    }
                    //1010型
                    else if (yb31[i + 2] == 1 && yb31[i + 1] + yb31[i + 3] == 0)
                    {
                        lap14[cc++] = lap14[cc++] = 30;
                        i += 2;
                    }
                    //1020型  
                    else if (yb31[i + 2] == 2 && yb31[i + 1] + yb31[i + 3] == 0)
                    {
                        if (KRC[i + 2] == 0)//死连张死对子
                        {
                            lap14[cc++] = lap14[cc++] = 30;
                            lap14[cc++] = 80;
                        }
                        else
                        {
                            if (KRC[i + 1] >= KRC[i + 2] * 2)
                            {
                                lap14[cc++] = lap14[cc++] = 30;
                                lap14[cc++] = 40;
                            }
                            else
                            {
                                lap14[cc++] = 40;
                                lap14[cc++] = lap14[cc++] = 10;
                            }
                        }
                        i += 2;
                    }
                    //10nn
                    else if (yb31[i + 1] == 0 && yb31[i + 2] > 0 && yb31[i + 3] > 0)
                        lap14[cc++] = 30;
                    else
                        lap14[cc++] = GetLap(yb31, i);
                }
                else if (yb31[i] == 2)
                {
                    //孤对200
                    if (yb31[i + 1] + yb31[i + 2] == 0)//孤立对子 
                    {
                        if (KRC[i] == 0)
                            lap14[cc++] = lap14[cc++] = 80;
                        else
                            lap14[cc++] = lap14[cc++] = 10;
                    }

                    //210型 
                    else if (yb31[i + 1] == 1 && yb31[i + 2] == 0)
                    {
                        if (KRC[i - 1] + KRC[i] + KRC[i + 2] == 0)//死连张死对子
                            lap14[cc++] = lap14[cc++] = lap14[cc++] = 80;
                        else if (KRC[i - 1] + KRC[i + 2] == 0)//死连张
                        {
                            lap14[cc++] = lap14[cc++] = 10;
                            lap14[cc++] = 80;
                        }
                        else if (KRC[i] == 0)//死对子
                        {
                            lap14[cc++] = 80;
                            lap14[cc++] = lap14[cc++] = 20;
                        }
                        else
                        {
                            if (KRC[i] * 2 >= KRC[i - 1] + KRC[i + 2])
                            {
                                lap14[cc++] = lap14[cc++] = 10;
                                lap14[cc++] = 40;
                            }
                            else
                            {
                                lap14[cc++] = 40;
                                lap14[cc++] = lap14[cc++] = 20;
                            }
                        }
                        i += 2;
                    }
                    //220型 
                    else if (yb31[i + 1] == 2 && yb31[i + 2] == 0)
                    {
                        if (KRC[i - 1] + KRC[i] + KRC[i + 1] + KRC[i + 2] == 0)//死连张死对子
                            lap14[cc++] = lap14[cc++] = lap14[cc++] = lap14[cc++] = 80;
                        else if (KRC[i - 1] + KRC[i + 1] + KRC[i + 2] == 0)//死连张
                        {
                            lap14[cc++] = lap14[cc++] = 10;
                            lap14[cc++] = lap14[cc++] = 80;
                        }
                        else if (KRC[i - 1] + KRC[i] + KRC[i + 2] == 0)//死连张
                        {
                            lap14[cc++] = lap14[cc++] = 80;
                            lap14[cc++] = lap14[cc++] = 10;
                        }
                        else //还是要打1张 
                            lap14[cc++] = lap14[cc++] = lap14[cc++] = lap14[cc++] = 10;
                        i += 2;
                    }
                    //2010型  
                    else if (yb31[i + 2] == 1 && yb31[i + 1] + yb31[i + 3] == 0)
                    {
                        if (KRC[i] == 0)
                        {
                            lap14[cc++] = 80;
                            lap14[cc++] = lap14[cc++] = 30;
                        }
                        else //还是要打1张
                        {
                            if (KRC[i + 1] >= KRC[i] * 2)
                            {
                                lap14[cc++] = lap14[cc++] = 30;
                                lap14[cc++] = 40;
                            }
                            else
                            {
                                lap14[cc++] = 40;
                                lap14[cc++] = lap14[cc++] = 10;
                            }
                        }
                        i += 2;
                    }
                    //2020
                    else if (yb31[i + 2] == 2 && yb31[i + 1] + yb31[i + 3] == 0)
                    {
                        if (KRC[i] * KRC[i + 2] == 0)
                        {
                            if (KRC[i + 1] == 1)
                            {
                                lap14[cc++] = 80;
                                lap14[cc++] = lap14[cc++] = 30;
                                lap14[cc++] = 80;
                            }
                            else
                                lap14[cc++] = lap14[cc++] = lap14[cc++] = lap14[cc++] = 30;
                        }
                        else
                            lap14[cc++] = lap14[cc++] = lap14[cc++] = lap14[cc++] = 10;
                        i += 2;
                    }
                    //20nn
                    else if (yb31[i + 1] == 0 && yb31[i + 2] > 0 && yb31[i + 3] > 0)
                    {
                        if (KRC[i] == 0)
                            lap14[cc++] = 80;
                        else
                            lap14[cc++] = 30;
                        lap14[cc++] = 30;
                    }
                    else
                        lap14[cc++] = lap14[cc++] = 10;
                    //lap14[cc++] = GetLap(yb31, i);
                }
                else if (yb31[i] == 3)
                    lap14[cc++] = lap14[cc++] = lap14[cc++] = 3;
                else if (yb31[i] == 4)
                    lap14[cc++] = lap14[cc++] = lap14[cc++] = lap14[cc++] = 1;

            }

            //int[] lap0 = Calculate_Lap_Raw31_Old(yb31);
            //for (int i = 0; i < tmp14.Length; i++)
            //    if (lap0[i] >= 30 && lap14[i] < 30 || lap0[i] < 30 && lap14[i] >= 30 || LongOfCardNZ(lap0) != LongOfCardNZ(lap14))
            //    { }
            return lap14;
        }


        public int GetLap(int[] yb31, int location)
        {
            int lap = 0, lap1 = 1, lap2 = 1;
            if (location >= yb31.Length - 1 || location < 1 || yb31[location] == 0) return 99;

            for (int i = location + 1; i < yb31.Length - 1; i++)
                if (i == 30)
                    lap1 = 8;
                else if (yb31[i] == 0 && KnownRemainCard[i] == 0 && i % 10 != 0)//遇到断张
                {
                    lap1 = 8;
                    break;
                }
                else if (yb31[i] == 0)
                    lap1++;
                else if (yb31[i] > 0)
                {
                    if (location / 10 != i / 10)
                        lap1 = 8;
                    break;
                }
            for (int i = location - 1; i >= 0; i--)
                if (i == 0)
                    lap2 = 10;
                else if (yb31[i] == 0 && KnownRemainCard[i] == 0 && i % 10 != 0)//遇到断张
                {
                    lap2 = 8;
                    break;
                }
                else if (yb31[i] == 0)
                    lap2++;
                else if (yb31[i] > 0)
                {
                    if (location / 10 != i / 10)
                        lap2 = 8;
                    break;
                }

            lap = (Math.Min(Math.Min(lap1, lap2), 8) + 1) * 10;
            return lap;
        }


        /// 计算牌之间的第二类跳数 . 并不完美，
        /// 处理2 4 5 6 6 6 7 8 9时不好，剩余2 4 5，应该2 6 6 
        /// 5 5 5 6 7
        /// 
        //    胡牌时，最多四个以内顺子或杠子，加一对将牌
        //    把牌分成4段，分别遍历
        //   ├────┼────┼────┼──────┤
        // id[0]      id[1]     id[2]     id[3]          id[4]

        public int[] MinResidualCards(int[] yb, int maxJin = 0)
        {
            int[] tmp14 = new int[yb.Length];
            int[] yb31 = new int[KnownRemainCard.Length];
            int[] tmp31 = new int[yb31.Length + 1];
            int[] med31 = new int[yb31.Length + 1];
            int[] out31 = new int[yb31.Length + 1];
            int minLen = 88, minChange = 88, minKRC = 88; ;//最少散牌 
            int[] id = new int[5]; int[] idMin = new int[5]; id[4] = tmp31.Length - 2;
            int iChange = 0, iLen = 0, snum1 = 0, snum2 = 0;

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    yb31[yb[i]]++;
            for (int t = 0; t < yb31.Length - 1; t++)
            {
                //t = 0时，没有取出将牌，直接处理顺子
                yb31.CopyTo(med31, 0);
                if (t > 0 && med31[t] < 2)
                    continue;
                else if (t > 0 && med31[t] >= 2) //先找到将牌   
                    med31[t] -= 2;

                for (id[1] = id[0]; id[1] < yb31.Length - 1; id[1]++)
                {
                    if (med31[id[1]] == 0) continue;
                    for (id[2] = id[1]; id[2] < yb31.Length - 1; id[2]++)
                    {
                        if (med31[id[2]] == 0) continue;
                        for (id[3] = id[2]; id[3] < yb31.Length - 1; id[3]++)
                        {
                            if (id[3] == 11)
                            { }
                            if (med31[id[3]] == 0) continue;
                            med31.CopyTo(tmp31, 0);
                            for (int j = 3; j >= 0; j--)//分成4段
                            {
                                for (int k = id[j]; k < id[j + 1] + 1; k++)//每段分别遍历
                                {
                                    if (tmp31[k] == 0)
                                        continue;
                                    else if (tmp31[k] > 0 && tmp31[k + 1] > 0 && tmp31[k + 2] > 0)
                                    {   //有连续的三张牌时, 组成一组, 清除掉。
                                        tmp31[k]--; tmp31[k + 1]--; tmp31[k + 2]--; k--;
                                    }
                                    else if (tmp31[k] >= 3)
                                    {   //前有三张相同牌时, 成一组, 清除掉。
                                        tmp31[k] = tmp31[k] - 3; k--;
                                    }
                                    snum1++;
                                }
                            }
                            snum2++;
                            iLen = 0;
                            for (int i = 0; i < tmp31.Length; i++)
                                iLen += tmp31[i];
                            tmp14 = new int[iLen];
                            iLen = 0;
                            for (int i = 1; i < tmp31.Length - 1; i++)
                                for (int j = 0; j < tmp31[i]; j++)
                                    tmp14[iLen++] = i;
                            iChange = ComputerChangeCardNum(tmp14, maxJin);
                            //1换牌数最小，2剩余牌长度最小，3将牌的KnownRemainCard最小
                            if (iChange < minChange || iChange == minChange && iLen < minLen
                                || iChange == minChange && iLen == minLen && KnownRemainCard[t] <= minKRC)
                            {
                                int oldlen = minLen;
                                int oldChange = minChange;
                                int oldKRC = minKRC;
                                minChange = iChange;
                                minLen = iLen;
                                minKRC = KnownRemainCard[t];
                                tmp31.CopyTo(out31, 0);
                                id.CopyTo(idMin, 0);
                                if (minLen == 0)
                                    goto Out;
                            }
                        }
                    }
                }
            }
        Out:
            iLen = 0;
            for (int j = 0; j < out31.Length; j++)
                iLen += out31[j];
            int[] tm = new int[iLen];
            iLen = 0;
            for (int k = 0; k < out31.Length; k++)
                for (int j = 0; j < out31[k]; j++)
                    tm[iLen++] = k;
            Array.Sort(tm);
            //当只有一对、非组合龙时，能胡牌,
            if (iLen == 2 && tm[0] == tm[1] && maxJin == 0)
                tm = new int[0];

            return tm;
        }
        //补齐筋
        public int[] MakUpJin(int[] yb, int maxJin = 0)
        {
            int Jin = maxJin;
            int[] tmp14 = new int[yb.Length + 2];
            yb.CopyTo(tmp14, 1);
            tmp14[tmp14.Length - 1] = 50;
            //while (Jin < 9 && LongOfCardNZ(tmp14) > 0)
            {
                for (int i = 1; i < tmp14.Length - 1; i++)
                {
                    if (tmp14[i] == 0)
                        continue;
                    else if (tmp14[i] > 30 && tmp14[i] - tmp14[i - 1] >= 2 && tmp14[i + 1] - tmp14[i] >= 2)
                    {
                        tmp14[i] = 0;
                        Jin++;
                    }
                    else if (tmp14[i] - tmp14[i - 1] >= 3 && tmp14[i + 1] - tmp14[i] >= 3)
                    {
                        tmp14[i] = 0;
                        Jin++;
                    }
                    else if (tmp14[i] % 10 == 1 && tmp14[i + 1] - tmp14[i] >= 3)
                    {
                        tmp14[i] = 0;
                        Jin++;
                    }
                    else if (tmp14[i] % 10 == 9 && tmp14[i] - tmp14[i - 1] >= 3)
                    {
                        tmp14[i] = 0;
                        Jin++;
                    }
                    Array.Sort(tmp14, 1, yb.Length);
                }
            }
            return tmp14;
        }
        //计算进入胡牌或成顺子状态时，需要调整的牌数
        public int ComputerChangeCardNum(int[] yb, int maxJin = 0)
        {
            int lapKey = 0, cc = 0;
            int[] tmp = new int[yb.Length];
            int[] lap0 = new int[yb.Length];
            int iResidual = 0, iPair = 0, i2ToShun = 0, i1ToShun = 0, iChange = 0;
            yb.CopyTo(tmp, 0);
            //组合龙情况
            cc = 0;
            if (maxJin > 0)
            {
                if (yb.Length < 9 - maxJin)
                    return 9 - maxJin;
                lap0 = Calculate_Lap_Raw14(yb);
                for (int i = 0; i < lap0.Length; i++)
                    lap0[i] = lap0[i] / 10 + 1;
                for (int i = maxJin; i < 9; i++)
                {
                    int max_ii = IndexOfMaxInArray(lap0);
                    lap0[max_ii] = 0;
                }
                tmp = new int[yb.Length - (9 - maxJin)];
                for (int i = 0; i < lap0.Length; i++)
                    if (lap0[i] > 0)
                        tmp[cc++] = yb[i];
            }
            //基桩牌：可以组成将、杠、顺的本手牌
            //将牌基桩牌数
            iPair = LongOfCardNZ(tmp) % 3 == 0 ? 0 : 1;
            if (iPair > 0)//需要将牌时,最好是死对子作将 
            {
                int minKRC = 88, minCard = -1;
                for (int i = 0; i < tmp.Length - 1; i++)
                    if (tmp[i] == tmp[i + 1] && KnownRemainCard[tmp[i]] <= minKRC)
                    {
                        minKRC = KnownRemainCard[tmp[i]];
                        minCard = tmp[i];
                    }
                for (int i = 0; i < tmp.Length - 1; i++)
                    if (tmp[i] == tmp[i + 1] && tmp[i] == minCard)
                    {
                        tmp[i] = tmp[i + 1] = 0;
                        iPair = 0;
                        Array.Sort(tmp);
                        tmp = NZArray(tmp);
                        break;
                    }
            }
            int[] lap1 = Calculate_Lap_Class14(tmp);

            //剩余数
            iResidual = LongOfCardNZ(tmp);
            for (int i = 0; i < lap1.Length; i++)
                if (tmp[i] > 0 && lap1[i] <= 2)
                    lapKey++;
            //2张关联牌基桩牌组数量（如23、24时，i2ToShun=1）
            i2ToShun = Math.Min(lapKey, iResidual / 3);
            //除去将牌基桩、关联基桩牌后，剩余的独立张基桩牌数量  
            i1ToShun = (int)((iResidual - 2 * iPair - 3 * i2ToShun) / 3.0);

            iChange = iPair + 1 * i2ToShun + 2 * i1ToShun;
            //如果只余2张时，这2张都是孤张，不能成将，则都要打出去
            if (iResidual == 2 && KnownRemainCard[tmp[0]] + KnownRemainCard[tmp[1]] == 0)
                iChange = 2;
            if (maxJin > 0)
                return iChange + 9 - maxJin;
            else
                return iChange;
        }

        //计算 胡全求人时，需要调整的牌
        //返回值：{对子，关联基桩数，独立张基桩数}
        public int ComputerChangeCardNumToQQR(int[] yb)
        {
            if (LongOfCardNZ(yb) % 3 != 2)
            { }

            int lapKey = 0;
            int[] tmp = new int[yb.Length];
            int[] lap0 = new int[yb.Length];
            int iResidual = 0, i2ToShun = 0, i1ToShun = 0, iChange = 0;
            yb.CopyTo(tmp, 0);
            //基桩牌：可以组成将、杠、顺的本手牌
            //将牌基桩牌数 
            int[] lap1 = Calculate_Lap_Class14(tmp);

            //剩余数
            iResidual = LongOfCardNZ(tmp);
            for (int i = 0; i < lap1.Length; i++)
                if (tmp[i] > 0 && lap1[i] <= 2)
                    lapKey++;
            //2张关联牌基桩牌组数量（如23、24时，i2ToShun=1）
            i2ToShun = Math.Min(lapKey, iResidual / 3);
            //除去将牌基桩、关联基桩牌后，剩余的独立张基桩牌数量  
            i1ToShun = (int)((iResidual - 2 - 3 * i2ToShun) / 3.0);
            iChange = 1 * i2ToShun + 2 * i1ToShun + 1;
            return iChange;
        }


        public int[] NZArray(int[] yb)
        {
            int cc = LongOfCardNZ(yb);
            int[] tmp = new int[cc];
            cc = 0;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    tmp[cc++] = yb[i];
            return tmp;

        }
        /// 计算牌之间的第二类跳数 .双向跳数??????????????一对，但也是相对孤张，1223

        public int[] Calculate_Lap_Raw14(int[] yb)
        {
            int[] tmp14 = new int[yb.Length + 2];
            int[] lap = new int[yb.Length];
            yb.CopyTo(tmp14, 1);
            Array.Sort(tmp14, 1, yb.Length);
            tmp14[tmp14.Length - 1] = 50;
            tmp14[0] = -10;

            int qianLap = 0, houLap = 0;
            for (int i = 1; i < tmp14.Length - 1; i++)
            {
                if (tmp14[i] == 0)
                    lap[i - 1] = -1;
                else if (tmp14[i] > 30)
                {
                    if (tmp14[i] - tmp14[i - 1] == 0 || tmp14[i + 1] - tmp14[i] == 0)
                        lap[i - 1] = 0;
                    else
                        lap[i - 1] = 9;
                }
                else if (tmp14[i] < 30)
                {
                    if (tmp14[i] - tmp14[i - 1] == 0 || tmp14[i + 1] - tmp14[i] == 0)
                        qianLap = houLap = 0;
                    else if (tmp14[i] % 10 == 1)
                    {
                        qianLap = 9;
                        houLap = tmp14[i + 1] - tmp14[i];
                    }
                    else if (tmp14[i] % 10 == 9)
                    {
                        houLap = 9; qianLap = tmp14[i] - tmp14[i - 1];
                    }
                    else
                    {
                        houLap = qianLap = 9;
                        if (tmp14[i] / 10 == tmp14[i - 1] / 10)
                            qianLap = tmp14[i] - tmp14[i - 1];
                        if (tmp14[i] / 10 == tmp14[i + 1] / 10)
                            houLap = tmp14[i + 1] - tmp14[i];
                    }
                    lap[i - 1] = Math.Min(houLap, qianLap);
                }
                lap[i - 1] = Math.Min(lap[i - 1], 9);
            }
            for (int i = 0; i < lap.Length; i++)
                if (yb[i] > 0)
                    lap[i] = lap[i] * 10 + 11 - Math.Min(10, KnownRemainCard[yb[i] - 1] +
                        KnownRemainCard[yb[i]] + KnownRemainCard[yb[i] + 1]);
            return lap;
        }




        private void CheckHandCard(int[] yb)
        {
            int len = LongOfCardNZ(yb);
            for (int i = 0; i < 4; i++)
            {
                if (FanCardData.ArrAgang[i] > 0)
                    len += 3;
                if (FanCardData.ArrMgang[i] > 0)
                    len += 3;
                if (FanCardData.ArrMke[i] > 0)
                    len += 3;
                if (FanCardData.ArrMshun[i] > 0)
                    len += 3;
            }
            if (len < 13)
            { }
        }

        /// <summary>
        /// 第4位是K，91034.8356， K=1
        /// </summary>
        /// <param name="yb"></param>
        /// <returns></returns>
        public double evaluateCards13(int[] yb, out double[] Eval_Level)//1209
        {
            Array.Sort(yb);
            for (int i = 0; i < AllFanTab.Length; i++)
                Array.Clear(AllFanTab[i], 0, AllFanTab[i].Length);

            int[] gap = new int[5];
            int[] JinZHLKey, JinQBKKey; int jinNum, jinNum1;
            gap[0] = MaxNormHuGap(yb, (14 - Mahjong.LongOfCardNZ(yb)) / 3);
            gap[1] = 14 - MaxZuHeLongKey(yb, out JinZHLKey, out jinNum);
            gap[2] = 14 - MaxQuanBuKaoKey(yb, out JinQBKKey, out jinNum1);
            gap[3] = 14 - Max13YaoKey(yb);
            gap[4] = Max7PairKey(yb);

            double EvalScore = 0;
            Eval_Level = new double[8];
            double[][] Hu_Nums = new double[8][];
            int Level = MinOfArrayNZ(gap);
            //while (LongOfCardNZ(HuNum) == 0 && Level <= 7)
            //{
            //    HuNum = HuCard(yb, Level, gap, "Eval");
            //    Level++;
            //}
            //Level--;
            //string StudyInfo = CreateAllFanTabInfo(yb);             

            //for (int i = 0; i < yb.Length; i++)                      
            //    EvalScore += HuNum[i];
            //if(Level > 1)
            //    EvalScore /= Level;

            //double[] Score = new double[8];
            //double[][] Nums = new double[Score.Length][];
            //double[] maxNums = new double[Score.Length];
            //int[] maxInd = new int[Score.Length];
            //for (int i = Level; i < Score.Length - 2 ||
            //    maxNums[i - 1] == 0 && i < maxNums.Length ; i++)
            //{
            //    Nums[i] = HuCard(yb, i, gap, "Eval");
            //    for (int j = 0; j < yb.Length; j++)
            //        Score[i] += Nums[i][j];
            //    //if (i > 1) Score[i] /= i;
            //    Score[i] = Math.Round(Score[i], 0);
            //    maxInd[i] = IndexOfMaxInArray(Nums[i]);
            //    maxNums[i] = Math.Round(MaxOfArray(Nums[i]), 0);
            //}
            //for (int i = Level; i < Score.Length; i++)
            //    if (i > 0 && Score[i - 1] * Score[i] > 0 && Score[i - 1] < Score[i])
            //    { break; }
            //EvalScore = Math.Round(ArraySum(Score) / Score.Length, 0);

            double[] HuNum = HuCard(yb, Level, gap, out Hu_Nums, "Eval");
            for (int i = 0; i < HuNum.Length; i++)
                EvalScore += HuNum[i];
            for (int i = 0; i < Hu_Nums.Length; i++)
                if (Hu_Nums[i] != null)
                    for (int j = 0; j < yb.Length; j++)
                        Eval_Level[i] += Hu_Nums[i][j];
            string StudyInfo = CreateAllFanTabInfo(yb);
            if (LongOfCardNZ(yb) % 3 != 1)
            { }
            return EvalScore;
        }

        public double[] If_Must_Gang(out string OutStr, bool IfShow = false)
        {
            int[] yb14 = new int[14];
            int[] yb1 = new int[14];//杠前   
            int[] yb2 = new int[14];//杠后 
            int[] usefuLen = new int[5];
            OutStr = "";
            string InfOriginal = "", InfoGang = "", OutStrBumpAndChi = "";
            double[] out_card_weight = new double[14];
            int[] tmp_card31 = new int[KnownRemainCard.Length];
            double[] RetArray = new double[8];
            for (int i = 0; i < Mahjong.EvalCPGHs.Length; i++) Mahjong.EvalCPGHs[i] = null;

            if (Can_Hu_OutCard(Mahjong.out_card) > 0)
            {
                OutStr = RetFanT();
                RetArray[0] = -9;
                return RetArray;
            }
            HandInCard.CopyTo(yb1, 0);
            HandInCard.CopyTo(yb14, 0);
            Array.Sort(yb1); Array.Sort(yb14);

            //   杠前分析形势   ///////////////////////////////////////////////////
            if (Mahjong.in_card > 0)
            {
                yb14[0] = yb1[0] = Mahjong.in_card;
                Array.Sort(yb1);
                out_card_weight = PriorityFastestHuCard(yb1, "Eval");
                int max_i = IndexOfMaxInArray(out_card_weight);
                yb1[max_i] = 0;   //把碰后最该打的牌打出去  
                Array.Sort(yb1); Array.Sort(yb14);
            }
            else
                Mahjong.WantGangCard = Mahjong.out_card;

            if (LongOfCardNZ(Mahjong.EvalCPGHs[1]) == 0)
            {
                RetArray[1] = RetCPG[1] = evaluateCards13(yb1, out Mahjong.EvalCPGHs[1]);
                GlobeCPGHs[1] = GlobeHuNums5Class;
            }
            else
                RetArray[1] = RetCPG[1];
            InfOriginal = CreateAllFanTabInfo(yb1);

            //   杠后分析形势///////////////////////////////////////////////  MinGapToKeyCard 
            yb14.CopyTo(yb2, 0);
            for (int j = 0; j < yb2.Length; j++)
                if (yb2[j] == Mahjong.WantGangCard)
                    yb2[j] = 0;
            Array.Sort(yb2);

            //   碰牌分析形势   /////////////////////////////////////////////////// 
            double[] BumpRe;
            if (Mahjong.out_card > 0 && LongOfCardNZ(EvalCPGHs[3]) == 0)
            {
                BumpRe = If_Must_Bump(out OutStrBumpAndChi, true);
                for (int i = 3; i < BumpRe.Length; i++)
                    RetArray[i] = BumpRe[i];
            }

            if (Mahjong.CountCardfArray(yb14, Mahjong.WantGangCard) == 1)//巴杠、后巴杠
            {
                Mahjong.AarryMinusNum(FanCardData.ArrMke, Mahjong.WantGangCard);
                FanCardData.ArrMgang[3] = Mahjong.WantGangCard;
            }
            else if (Mahjong.CountCardfArray(yb14, Mahjong.WantGangCard) == 4)
                FanCardData.ArrAgang[3] = Mahjong.WantGangCard;
            else if (Mahjong.out_card > 0)
                FanCardData.ArrMgang[3] = Mahjong.WantGangCard;         //明杠
            RetArray[2] = evaluateCards13(yb2, out Mahjong.EvalCPGHs[2]);
            InfoGang = CreateAllFanTabInfo(yb2);
            FanCardData.ArrMgang[3] = FanCardData.ArrAgang[3] = 0;
            if (Mahjong.CountCardfArray(yb14, Mahjong.WantGangCard) == 1)//巴杠、后巴杠
                Mahjong.AppendNum2Array(FanCardData.ArrMke, Mahjong.WantGangCard);
            GlobeCPGHs[2] = GlobeHuNums5Class;

            double[] RetArray1 = ReturnCPG();
            if (IndexOfMaxInArray(RetArray1) == 2)
                RetArray1[0] = 1;
            else
                RetArray1[0] = -1;
            OutStr = "结论：" + (RetArray1[0] > 0 ? "杠！" : "不杠！") + "  ";
            OutStr += "杠前:" + RetArray1[1] + "  杠后" + RetArray1[2] + "  碰后" + RetArray1[3];
            OutStr += "  吃后" + RetArray1[4] + ":" + RetArray1[5] + ":" + RetArray1[6] + "\n";
            OutStr += "  杠前：" + InfOriginal + "  杠后：" + InfoGang;
            OutStr += OutStrBumpAndChi;
            //double[] RetArray_Old = If_Must_Gang_old(out string OutStrOld);
            //if (RetArray_Old[0] != RetArray1[0])
            //{ }

            RetArray.CopyTo(RetCPG, 0);
            return RetArray1;
        }


        public double[] If_Must_Bump(out string OutStr, bool IfComeFromGang = false)//1209
        {
            int[] yb1 = new int[14];//碰前
            int[] yb2 = new int[14];//碰后  
            double[] out_card_weight = new double[14];
            double[] RetArray = new double[8];
            int[] usefuLen = new int[4];
            string InfOriginal = "", InfoBump = ""; OutStr = ""; int Level = 0;

            if (Can_Hu_OutCard(Mahjong.out_card) > 0)
            {
                OutStr = RetFanT();
                RetArray[0] = -9;
                return RetArray;
            }

            HandInCard.CopyTo(yb1, 0);
            Array.Sort(yb1);
            yb1.CopyTo(yb2, 0);
            if (LongOfCardNZ(Mahjong.EvalCPGHs[1]) == 0)
            {
                RetArray[1] = RetCPG[1] = evaluateCards13(yb1, out Mahjong.EvalCPGHs[1]);
                InfOriginal = CreateAllFanTabInfo(yb1);
                GlobeCPGHs[1] = GlobeHuNums5Class;
            }
            else
                RetArray[1] = RetCPG[1];

            //   碰后分析形势好坏/////////////////////  
            for (int i = 0; i < yb2.Length; i++)//碰掉
                if (yb2[i] == Mahjong.out_card)
                {
                    yb2[i] = yb2[i + 1] = 0;
                    break;
                }
            Array.Sort(yb2);
            FanCardData.ArrMke[3] = Mahjong.out_card;
            out_card_weight = PriorityFastestHuCard(yb2, "Eval");
            FanCardData.ArrMke[3] = 0;
            int max_i = IndexOfMaxInArray(out_card_weight);
            yb2[max_i] = 0;   //把碰后最该打的牌打出去   
            Array.Sort(yb2);

            string OutStrChi = "";
            double[] ChiRe;
            if (this.azimuth == (Mahjong.old_azimuth + 1) % 4 && this.Can_Chi_Cards(Mahjong.out_card))
                if (LongOfCardNZ(EvalCPGHs[4]) + LongOfCardNZ(EvalCPGHs[5]) + LongOfCardNZ(EvalCPGHs[6]) == 0)
                {
                    ChiRe = If_Must_Chi(out OutStrChi);
                    for (int i = 4; i < ChiRe.Length; i++)
                        RetArray[i] = ChiRe[i];
                }

            FanCardData.ArrMke[3] = Mahjong.out_card;
            RetArray[3] = evaluateCards13(yb2, out Mahjong.EvalCPGHs[3]);
            InfoBump = CreateAllFanTabInfo(yb2);
            FanCardData.ArrMke[3] = 0;
            GlobeCPGHs[3] = GlobeHuNums5Class;

            double[] RetArray1 = ReturnCPG();
            if (IndexOfMaxInArray(RetArray) == 3)
                RetArray[0] = 1;
            else RetArray[0] = -1;
            if (!IfComeFromGang)
            {

                OutStr = "结论：" + (RetArray1[0] > 0 ? "碰！" : "不碰！") + "  ";
                OutStr += "碰前:" + RetArray1[1] + "  碰后" + RetArray1[3];
                OutStr += "  吃后" + RetArray1[4] + ":" + RetArray1[5] + ":" + RetArray1[6] + "\n";
                OutStr += "  碰前:" + InfOriginal;
            }
            OutStr += "  碰后:" + InfoBump;
            OutStr += OutStrChi;
            //double[] RetArray_Old = If_Must_Bump_Old(out string OutStrOld, IfComeFromGang);

            RetArray.CopyTo(RetCPG, 0);

            if (RetArray[0] != RetArray1[0])
            { }
            return RetArray1;
        }

        public double[] If_Must_Chi(out string OutStr)
        {
            OutStr = "";
            double[] RetArray = new double[8];
            if (Can_Hu_OutCard(Mahjong.out_card) > 0)
            {
                OutStr = RetFanT();
                RetArray[0] = -9;
                return RetArray;
            }
            int[] yb1 = new int[14];//吃前 
            double[] out_card_weight = new double[14];
            int OutC = Mahjong.out_card;
            string InfOriginal = "", InfoChi = "";

            //   吃前分析形势好坏  
            Array.Sort(this.HandInCard);
            HandInCard.CopyTo(yb1, 0);
            Array.Sort(yb1);
            if (LongOfCardNZ(Mahjong.EvalCPGHs[1]) == 0)
            {
                RetArray[1] = RetCPG[1] = evaluateCards13(yb1, out Mahjong.EvalCPGHs[1]);
                InfOriginal = CreateAllFanTabInfo(yb1);
                GlobeCPGHs[1] = GlobeHuNums5Class;
            }
            else
                RetArray[1] = RetCPG[1];

            int[] tmp31 = new int[KnownRemainCard.Length + 1];
            int[][] yybb2 = new int[3][];
            int[][] ArrMshun = new int[3][];
            for (int i = 0; i < 3; i++)
            {
                yybb2[i] = new int[14];
                ArrMshun[i] = new int[4];
                FanCardData.ArrMshun.CopyTo(ArrMshun[i], 0);
            }
            for (int i = 0; i < 3; i++)
            {
                Array.Clear(tmp31, 0, tmp31.Length);
                for (int j = 0; j < yb1.Length; j++)
                    tmp31[yb1[j]]++;
                tmp31[0] = 0;
                tmp31[OutC]++;

                //   吃后分析形势好坏/////////////////////      
                if (OutC == 1 && tmp31[1] > 0 && tmp31[2] > 0 && tmp31[3] > 0)
                {
                    tmp31[1]--; tmp31[2]--; tmp31[3]--;
                    ArrMshun[i][3] = 2;
                }
                else if (tmp31[OutC - i] > 0 && tmp31[OutC - i + 1] > 0 && tmp31[OutC - i + 2] > 0)
                {
                    tmp31[OutC - i]--; tmp31[OutC - i + 1]--; tmp31[OutC - i + 2]--;
                    ArrMshun[i][3] = OutC - i + 1;
                }
                else
                    continue;

                int loc = 0;
                for (int j = 0; j < tmp31.Length; j++)
                    for (int k = 0; k < tmp31[j]; k++)
                        yybb2[i][loc++] = j;

                //把吃后最该打的牌打出去  
                Array.Sort(yybb2[i]);
                ArrMshun[i].CopyTo(FanCardData.ArrMshun, 0);
                out_card_weight = PriorityFastestHuCard(yybb2[i], "Eval");
                int max_i = IndexOfMaxInArray(out_card_weight);
                yybb2[i][max_i] = 0;
                Array.Sort(yybb2[i]);
                RetArray[4 + i] = evaluateCards13(yybb2[i], out Mahjong.EvalCPGHs[4 + i]);
                InfoChi += "   吃后:Loc=" + i + "  " + CreateAllFanTabInfo(yybb2[i]);
                FanCardData.ArrMshun[3] = 0;
                GlobeCPGHs[4 + i] = GlobeHuNums5Class;
                if (OutC == 1)
                    break;
            }
            double[] RetArray1 = ReturnCPG();

            if (IndexOfMaxInArray(RetArray) == 1)
                RetArray[0] = -1;
            else if (LongOfCardNZ(RetArray) == 0)
                RetArray[0] = -1;
            else
                RetArray[0] = 1;
            Double max_chi = Math.Max(RetArray[2], Math.Max(RetArray[3], RetArray[4]));

            OutStr = "结论：" + (RetArray1[0] > 0 ? "吃！" : "不吃！") + "  ";
            OutStr += "吃前" + RetArray1[1] + "  吃后" + RetArray1[4] + "-" + RetArray1[5] + "-" + RetArray1[6] + "\n";
            OutStr += "  吃前:" + InfOriginal;

            OutStr += InfoChi;
            //double[] RetArray_Old = If_Must_Chi_Old(out string OutStrOld, IfComeFromPengOrGang); 

            if (RetArray[0] != RetArray1[0])
            { }
            RetArray.CopyTo(RetCPG, 0);
            return RetArray1;
        }

        //吃碰杠数据层级对齐
        public double[] ReturnCPG()
        {
            double[] RetArray = new double[8];
            double[][] CPGs = new double[Mahjong.EvalCPGHs.Length][];
            for (int i = 0; i < CPGs.Length; i++)
                if (Mahjong.EvalCPGHs[i] != null)
                {
                    CPGs[i] = new double[Mahjong.EvalCPGHs[i].Length];
                    Mahjong.EvalCPGHs[i].CopyTo(CPGs[i], 0);
                }

            int[] maxLevels = new int[8];
            for (int i = 0; i < CPGs.Length; i++)
                if (CPGs[i] != null)
                    for (int j = CPGs[i].Length - 1; j >= 0; j--)
                        if (CPGs[i][j] > 0)
                        {
                            maxLevels[i] = j; break;
                        }
            int maxLev = MinOfArrayNZ(maxLevels);
            for (int i = 0; i < CPGs.Length; i++)
                if (CPGs[i] != null)
                    for (int j = maxLev + 1; j < CPGs[i].Length; j++)
                        if (CPGs[i][j] > 0)
                            CPGs[i][j] = 0.1;
            for (int i = 0; i < CPGs.Length; i++)
                if (CPGs[i] != null)
                    for (int j = 0; j < CPGs[i].Length; j++)
                        RetArray[i] += CPGs[i][j];
            if (LongOfCardNZ(RetArray) == 0)
                RetArray[0] = -1;
            else if (IndexOfMaxInArray(RetArray) == 1)
                RetArray[0] = -1;
            else
                RetArray[0] = 1;

            return RetArray;
        }


        public bool DecisionTimeOut()
        {
            double span = (DateTime.Now - TimeStart).TotalSeconds;
            string str = "--";
            if (span >= 4.500)
                str += " T:" + span.ToString().PadRight(4) + "ms";
            if (span >= TimeSpan)//TimeSpan
                return true;
            else
                return false;
        }
        /// <summary>
        /// 判别一张牌是否在另一牌组中
        /// </summary>
        /// <param name="yb"></param>
        /// <param name="in_card"></param>
        /// <returns></returns>
        public bool If_Card_In_Array(int[] yb, int card)
        {
            foreach (int i in yb)
            {
                if (i > 0 && i == card)
                    return true;
            }
            return false;
        }

        public static int[] NormalizeArray(double[] yb)
        {
            int[] tmp = new int[yb.Length];
            double max = MaxOfArray(yb);
            for (int i = 0; i < yb.Length; i++)
                if (max > 0)
                    tmp[i] = (int)(Math.Round(yb[i] * 99 / max));
            return tmp;
        }
        public static double[] NormalizeArrayDouble(double[] yb, double retMax = 99, int digit = 2)
        {
            double[] tmp = new double[yb.Length];
            double max = MaxOfArray(yb);
            for (int i = 0; i < yb.Length; i++)
                if (max > 0)
                    tmp[i] = Math.Round(yb[i] * retMax / max, digit);
            return tmp;
        }
        public int[] AddArray(params int[][] num)
        {
            int[] tmp = new int[num[0].Length];
            for (int i = 0; i < num.Length; i++)
                for (int j = 0; j < tmp.Length; j++)
                    tmp[j] += num[i][j];
            return tmp;
        }
        public double[] AddArray(params double[][] num)
        {
            double[] tmp = new double[num[0].Length];
            for (int i = 0; i < num.Length; i++)
                for (int j = 0; j < tmp.Length; j++)
                    tmp[j] += num[i][j];
            return tmp;
        }
        public double AddArrayToNum(params double[][] num)
        {
            double tmp = 0;
            for (int i = 0; i < num.Length; i++)
                for (int j = 0; j < num[i].Length; j++)
                    tmp += num[i][j];
            return tmp;
        }
        public static double ArraySum(double[] num)
        {
            double sum_num = 0;
            for (int i = 0; i < num.Length; i++)
                sum_num += num[i];
            return sum_num;
        }
        //找出最大值
        public static int MaxOfArray(int[] yb)
        {
            int max_num = 0;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > max_num)
                    max_num = yb[i];
            return max_num;
        }
        //找出最大值
        public static double MaxOfArray(double[] yb)
        {
            double max_num = -99999;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > max_num)
                    max_num = yb[i];
            return max_num;
        }
        //找出第二大值
        public static double SecondMaxOfArray(double[] yb)
        {
            double[] tmp = new double[yb.Length];
            yb.CopyTo(tmp, 0);

            double max_num = 0, secondMax_num = 0;
            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] > max_num)
                    max_num = tmp[i];
            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] == max_num)
                {
                    tmp[i] = 0; break;
                }
            for (int i = 0; i < tmp.Length; i++)
                if (tmp[i] > secondMax_num)
                    secondMax_num = tmp[i];
            return secondMax_num;
        }
        public static double MinOfArrayNZ(double[] yb)
        {
            double min_num = 99999;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] < min_num && yb[i] > 0)
                    min_num = yb[i];
            return min_num;
        }
        public static int MinOfArrayNZ(int[] yb)
        {
            int min_num = 99999;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] < min_num && yb[i] > 0)
                    min_num = yb[i];
            return min_num;
        }
        public static int MinOfArray(int[] yb)
        {
            int min_num = 99999;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] < min_num)
                    min_num = yb[i];
            return min_num;
        }
        //是数组的最大值吗？
        public bool IfMaxOfArray(int[] yb, int card)
        {
            for (int i = 0; i < yb.Length; i++)
                if (card < yb[i])
                    return false;
            return true;
        }
        //是数组的最大值吗？
        public static bool IfMaxOfArray(double[] yb, int num)
        {
            for (int i = 0; i < yb.Length; i++)
                if (yb[num] < yb[i])
                    return false;
            return true;
        }

        //找出最大值
        public static int IndexOfMaxInArray(int[] yb)
        {
            int max_i = 0, max_num = 0;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] >= max_num)
                {
                    max_num = yb[i];
                    max_i = i;
                }
            return max_i;
        }
        public static int IndexOfMaxInArray(double[] yb)
        {
            int max_i = -1;
            double max_num = 0;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0 && yb[i] > max_num)
                {
                    max_num = yb[i];
                    max_i = i;
                }
            return max_i;
        }
        public static int[] IndexsOfMax(int[] yb)
        {
            int[] IndMaxArray = new int[yb.Length];
            int max_num = 0;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > max_num)
                    max_num = yb[i];
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] == max_num)
                    IndMaxArray[i] = 1;
            return IndMaxArray;
        }


        public static int IndexOfMinNZInArray(double[] yb)
        {
            int min_i = 0;
            double min_num = 88888;

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0 && yb[i] < min_num)
                {
                    min_num = yb[i];
                    min_i = i;
                }
            //rdm1 = new Random(2);
            int seed = rdm1.Next(yb.Length);
            for (int i = seed; i < yb.Length; i++)
                if (min_num == yb[i])
                {
                    min_i = i;
                    break;
                }
            return min_i;
        }

        public static int IndexOfMinNZInArray(int[] yb)
        {
            int min_i = 0;
            int min_num = 88888;

            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0 && yb[i] < min_num)
                {
                    min_num = yb[i];
                    min_i = i;
                }
            //rdm1 = new Random(2);
            //int seed = rdm1.Next(yb.Length);
            //for (int i = seed; i < yb.Length; i++)
            //    if (min_num == yb[i])
            //    {
            //        min_i = i;
            //        break;
            //    }
            return min_i;
        }
        public static bool IfMinNZInArray(int[] yb, int ind)//ind数是非0数里最小的一个
        {
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0 && i != ind && yb[i] <= yb[ind])
                    return false;
            return true;
        }

        public static int IndexOfArray(int[] yb, int card)
        {
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] == card)
                    return i;
            return -1;
        }
        public static int CountCardfArray(int[] yb, int card)
        {
            if (card == 0)
                return 0;
            int cc = 0;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] == card)
                    cc++;
            return cc;
        }
        public static bool IfExit0110(int[] tmp31)
        {
            for (int i = 1; i < 31; i++) //0110
                if (tmp31[i] * tmp31[i + 1] == 1 && tmp31[i - 1] + tmp31[i + 2] == 0)
                    return true;
            return false;
        }
        public static bool IfExitSameItemInTwoArray(int[] Arr1, int[] Arr2)
        {
            for (int i = 0; i < Arr1.Length; i++)
                for (int j = 0; j < Arr2.Length; j++)
                    if (Arr1[i] == Arr2[j])
                        return true;
            return false;
        }
        public static bool IfSameTwoArray(int[] Arr1, int[] Arr2)
        {
            if (Arr1.Length != Arr2.Length)
                return false;
            for (int i = 0; i < Arr1.Length; i++)
                if (Arr1[i] != Arr2[i])
                    return false;
            return true;
        }
        public static int IndexArrayInList(ArrayList arrList, int[] Arr)
        {
            int j = 0;
            for (int i = 0; i < arrList.Count; i++)
                for (j = Arr.Length - 1; j >= 0; j--)
                { 
                    if (((int[])arrList[i])[j] != Arr[j])
                        break;
                    if(j == 0)
                        return i;
                }
            return -1;
        }

        /// <summary>
        /// Arr数组中，是否是相同的牌第二次要打出的。
        /// Ind为要打出的牌，当相同两张以上牌同时打出时返回False
        /// </summary>
        /// <param name="Arr"></param>
        /// <param name="Ind"></param>
        /// <returns></returns>
        public static bool IfSecondSameItemInTwoArray(int[] Arr, int[] Ind)
        {
            int cc = 0;
            for (int i = 1; i < Arr.Length; i++)
                if (Arr[i] == Arr[i - 1])
                {
                    if (Ind.Contains(i))
                    {
                        cc = 0;//ind中出现次数
                        for (int j = 0; j < Ind.Length; j++)
                            if (Arr[i] == Arr[Ind[j]])
                                cc++;
                        if (cc == 1)
                            return true;
                        else //打牌中同时出现两张以上相同的牌
                        { }
                    }
                }
            return false;
        }
        public static bool IfSameMaxOfTwoArray(double[] Arr1, double[] Arr2)
        {
            int[] MaxArr1 = new int[Arr1.Length];
            int[] MaxArr2 = new int[Arr2.Length];
            double max = 0.000001;
            for (int i = 0; i < Arr1.Length; i++)
                if (max < Arr1[i])
                    max = Arr1[i];
            for (int i = 0; i < Arr1.Length; i++)
                if (Math.Abs(max - Arr1[i]) < 0.001)
                    MaxArr1[i] = 1;
            max = 0.000001;
            for (int i = 0; i < Arr2.Length; i++)
                if (max < Arr2[i])
                    max = Arr2[i];
            for (int i = 0; i < Arr2.Length; i++)
                if (Math.Abs(max - Arr2[i]) < 0.001)
                    MaxArr2[i] = 1;
            for (int i = 0; i < MaxArr1.Length; i++)
                if (MaxArr1[i] > 0 && MaxArr2[i] > 0)
                    return true;
            return false;
        }
        //是否是第一个数字
        public static bool IfFirstNumInArray(int[] Arr1, int ind)
        {
            for (int i = 0; i < Arr1.Length; i++)
                if (Arr1[i] == Arr1[ind])
                {
                    if (i == ind)
                        return true;
                    else
                        return false;
                }
            return false;
        }

        public static void PrintArr(int[] yb)
        {
            if (yb == null)
                return;
            for (int i = 0; i < yb.Length; i++)
                if (yb[i] > 0)
                    Console.Write(yb[i].ToString().PadLeft(3));
                else
                    Console.Write(".".PadLeft(3));
            Console.WriteLine();
        }

        public static void PrintArray(int[] yb, string Name = "", int pad = 4)
        {
            double[] tmp = new double[yb.Length];
            for (int j = 0; j < yb.Length; j++)
                tmp[j] += yb[j];
            PrintArray(tmp, Name, pad);
        }
        public static void DebugPrintArray(double[] yb, int pad = 8)
        {
            String str = ""; ;
            for (int j = 0; j < yb.Length; j++)
                if (yb[j] < 0.000001) str += "0".PadLeft(pad);
                else str += yb[j].ToString("F3").PadLeft(pad);
            Debug.WriteLine(str);
        }

        public static void PrintArray(double[] yb, string Name, int pad)
        {
            for (int i = 0; i < yb.Length; i++)
            {
                String str = "";
                if (Math.Abs(Math.Round(yb[i]) - yb[i]) < 0.0001) str += yb[i].ToString("F0");
                else if (yb[i] < 1) str += yb[i].ToString("F" + (pad - 2)).Substring(1);
                else if (yb[i] < 10) str += yb[i].ToString("F" + (pad - 3));
                else if (yb[i] < 100) str += yb[i].ToString("F" + (pad - 4));
                else str += yb[i].ToString("F" + (pad - 5));
                if (ShowConsole) Console.Write(str.PadRight(pad, ' '));
            }
            if (ShowConsole)
                if (Name.Length > 0)
                    Console.Write("<--" + Name);
            if (ShowConsole) Console.WriteLine();
        }


        public static void PrintArray(double[,] yb, int Line, int Col, int pad, char ch)
        {

            for (int i = 0; i < Line; i++)
            {
                if (ShowConsole) Console.Write("{ ");
                for (int j = 0; j < Col; j++)
                {
                    String str = yb[i, j].ToString("F2");
                    if (ShowConsole) Console.Write(str.PadRight(pad, ch));
                }
                if (ShowConsole) Console.WriteLine(" },");
            }
            if (ShowConsole) Console.WriteLine();
        }

        public static void PrintArray(int[,] yb, int Line, int Col, int pad)
        {

            for (int i = 0; i < Line; i++)
            {
                for (int j = 0; j < Col; j++)
                {
                    String str = yb[i, j].ToString();
                    if (ShowConsole) Console.Write(str.PadRight(pad, ' '));
                }
                if (ShowConsole) Console.WriteLine();
            }
            if (ShowConsole) Console.WriteLine();
        }

        public static void PrintArray31(int[] yb, string Name, int pad)
        {
            double[] tmp = new double[KrcLen]; ;
            for (int j = 0; j < 31; j++)
                tmp[j] += yb[j];
            PrintArray31(tmp, Name, pad);
        }

        public static void PrintArray31(double[] yb, string Name, int pad)
        {
            double[] tmp = new double[KrcLen]; ;
            yb.CopyTo(tmp, 0);
            tmp[30] = tmp[0] = tmp[10] = tmp[20] = 0;
            for (int j = 0; j < 31; j++)
                tmp[j / 10 * 10] += tmp[j];
            tmp[30] = tmp[0] + tmp[10] + tmp[20];
            PrintArray(tmp, Name, pad);
        }
        public static void PrintArray31(double[,] yb, int Line, int Col, int pad)
        {
            double[] tmp = new double[KrcLen]; ;
            for (int i = 0; i < Line; i++)
            {
                for (int j = 0; j < 31; j++)
                    tmp[j] = yb[i, j];
                PrintArray31(tmp, "", pad);
            }
            if (ShowConsole) Console.WriteLine();
        }

        public static void PrintArrayList(ArrayList list, int ff, int pad)
        {
            if (list.Count == 0)
                return;
            if (ShowConsole)
                for (int j = 0; j < 10; j++)
                    Console.Write(j.ToString().PadRight(pad, ' '));
            if (ShowConsole) Console.WriteLine();
            for (int i = 0; i < list.Count; i++)
            {
                double[] data = list[i] as double[];
                for (int j = 0; j < data.Length; j++)
                {
                    String str = "";
                    if (j == 0 || j == data.Length - 1)
                        str += data[j].ToString("F0");
                    else if (data[j] == 0)
                        str += "0";
                    else
                        str += data[j].ToString("F" + ff);
                    if (ShowConsole) Console.Write(str.PadRight(pad, ' '));
                }
                if (ShowConsole) Console.WriteLine();
            }
            if (ShowConsole) Console.WriteLine();
        }
        public static void PrintArrayList(ArrayList list, int pad)
        {
            if (list.Count == 0)
                return; 
            for (int i = 0; i < list.Count; i++)
            {
                int[] data = list[i] as int[];
                for (int j = 0; j < data.Length; j++)
                {
                    String str = "";
                    if (j == 0 || j == data.Length - 1)
                        str += data[j].ToString();
                    else if (data[j] == 0)
                        str += "0";
                    else
                        str += data[j].ToString();
                    if (ShowConsole) Console.Write(str.PadRight(pad, ' '));
                }
                if (ShowConsole) Console.WriteLine();
            }
            if (ShowConsole) Console.WriteLine();
        }
        public static void printKnownRemainCard(int[] KRC)
        {
            for (int j = 0; j < 31; j++)
            {
                if (j % 10 != 0)
                    Console.Write(KRC[j].ToString().PadRight(2));
                else
                    Console.Write(("").PadRight(2));

            }
            if (KRC.Length > 40)
            {
                for (int j = 31; j < 45; j++)
                {
                    if (j == 38)
                        Console.Write(("").PadRight(2));
                    else if (j % 2 == 0)
                        continue;
                    else
                        Console.Write(KRC[j].ToString().PadRight(2));
                }
            }
            Console.WriteLine();
        }
        public static void printKnownRemainCard(int[] KRC, int pad)
        {
            int[] tmp = new int[KrcLen];
            KRC.CopyTo(tmp, 0);
            //for (int j = 0; j < 31; j++)
            //    tmp[j / 10 * 10] += tmp[j];

            //for (int j = 0; j < 31; j++)
            //{
            //    if (j % 10 != 0)
            //        Console.Write(j.ToString().PadRight(pad));
            //    else
            //        Console.Write(("").PadRight(pad));

            //}
            //Console.WriteLine();

            int sum = tmp[0] + tmp[10] + tmp[20];
            for (int j = 0; j < KRC.Length - 1; j++)
                Console.Write(tmp[j].ToString().PadRight(pad));
            Console.WriteLine(("=" + sum.ToString()).PadRight(pad) + " <-- KRC");
        }

        public static void printKnownRemainCard(double[] KRC, int ff, int pad)
        {
            double[] tmp = new double[KrcLen]; ;
            KRC.CopyTo(tmp, 0);
            for (int j = 0; j < 31; j++)
                tmp[j / 10 * 10] += tmp[j];

            //for (int j = 0; j < 31; j++)
            //{
            //    if (j % 10 != 0)
            //        Console.Write(j.ToString().PadRight(pad));
            //    else
            //        Console.Write(("").PadRight(pad));

            //}
            //Console.WriteLine();

            double sum = tmp[0] + tmp[10] + tmp[20];
            for (int j = 0; j < KRC.Length - 1; j++)
            {
                String str = "";
                if (tmp[j] < 0.0001)
                    str += ".";
                else if (Math.Abs(Math.Round(tmp[j]) - tmp[j]) < 0.001)
                    str += tmp[j].ToString("F0");
                else if (tmp[j] < 1)
                    str += tmp[j].ToString("F" + Math.Max(0, ff)).Substring(1);
                else if (tmp[j] > 1)
                    str += tmp[j].ToString("F" + Math.Max(0, ff - 1));
                if (ShowConsole) Console.Write(str.PadRight(pad));
            }

            if (ShowConsole) Console.WriteLine(("=" + sum.ToString("F2")).PadRight(pad) + " <-- KRC");
        }

        public static string GlobeHuNums2String(int padnum = 0, double[][][] HuNums = null)
        {
            string[] Name = new string[] { "基本", "组合龙", "全不靠", "十三幺", "七对" };
            string Str = "";
            if (HuNums == null) HuNums = Mahjong.GlobeHuNums5Class;
            double max = 1;
            for (int j = 0; j < HuNums.Length; j++)
                if (HuNums[j] != null)
                {
                    for (int k = 0; k < HuNums[j].Length; k++)
                        if (HuNums[j][k] != null && LongOfCardNZ(HuNums[j][k]) == 0)
                                HuNums[j][k] = null;    
                    if (LongOfCardNZ(HuNums[j]) == 0)
                        HuNums[j] = null;
                }
            if (padnum == 0)
            {
                for (int j = 0; j < HuNums.Length; j++)
                    if (HuNums[j] != null)
                    {
                        for (int k = 0; k < HuNums[j].Length; k++)
                            if (HuNums[j][k] != null)
                            {
                                for (int v = 0; v < HuNums[j][k].Length; v++)
                                    if (HuNums[j][k][v] > max)
                                        max = HuNums[j][k][v];
                                if (LongOfCardNZ(HuNums[j][k]) == 0)
                                    HuNums[j][k] = null;
                            }
                         
                        if (LongOfCardNZ(HuNums[j]) == 0)
                            HuNums[j] = null;
                    }
                padnum = (int)Math.Log10(max) + 2;
            }
            for (int i = 0; i < HuNums.Length; i++)
                if (HuNums[i] != null)
                {
                    Str += "  " + Name[i] + "型\n";
                    for (int j = 0; j < HuNums[i].Length; j++)
                    {
                        if (HuNums[i][j] != null && LongOfCardNZ(HuNums[i][j]) > 0)
                        {
                            Str += "    深度=" + j + "→";                            
                            for (int k = 0; k < 14; k++)
                                Str += HuNums[i][j][k].ToString("F0").PadLeft(padnum);
                            Str += "\n";
                        }
                    }
                }
            return Str;
        }

        public static string GlobeCPGHs2String(int[] yb = null)
        {
            string[] Name = new string[] { "", "原状", "杠后", "碰后", "吃一", "吃二", "吃三", "" };
            string Str = "";
            double max = 1;
            for (int i = 0; i < Mahjong.GlobeCPGHs.Length; i++)
                if (Mahjong.GlobeCPGHs[i] != null)
                    for (int j = 0; j < Mahjong.GlobeCPGHs[i].Length; j++)
                        if (Mahjong.GlobeCPGHs[i][j] != null)
                            for (int k = 0; k < Mahjong.GlobeCPGHs[i][j].Length; k++)
                                if (Mahjong.GlobeCPGHs[i][j][k] != null)
                                    for (int v = 0; v < Mahjong.GlobeCPGHs[i][j][k].Length; v++)
                                        if (Mahjong.GlobeCPGHs[i][j][k][v] > max)
                                            max = Mahjong.GlobeCPGHs[i][j][k][v];
            int padnum = (int)Math.Log10(max) + 2;
            //for (int i = 0; i < Mahjong.GlobeCPGHs.Length; i++)
            //    if (Mahjong.GlobeCPGHs[i] != null)
            //        for (int j = 0; j < Mahjong.GlobeCPGHs[i].Length; j++)
            //            if (Mahjong.GlobeCPGHs[i][j] != null)
            //                for (int k = 0; k < Mahjong.GlobeCPGHs[i][j].Length; k++)
            //                    if (Mahjong.GlobeCPGHs[i][j][k] != null)
            //                        for (int v = 0; v < Mahjong.GlobeCPGHs.Length; v++) 
            //                                Mahjong.GlobeCPGHs[i][j][k][v] = Math.Round(Mahjong.GlobeCPGHs[i][j][k][v] * 999 / max);

            for (int i = 0; i < Mahjong.GlobeCPGHs.Length; i++)
                if (Mahjong.GlobeCPGHs[i] != null)
                {
                    Str += Name[i] + "态\n";
                    Str += GlobeHuNums2String(padnum, Mahjong.GlobeCPGHs[i]);
                }
            if (yb == null)
            {
                Str += "        index=  ";
                for (int k = 0; k < 14; k++)
                    Str += k.ToString().PadLeft(padnum);
            }
            else
            {
                Str += "           yb=  ";
                for (int k = 0; k < 14; k++)
                    Str += yb[k].ToString().PadLeft(padnum);
            }
            Str += "\n";

            return Str;
        }


        /// <summary>
        /// 两数组相加
        /// </summary>
        /// <param name="source"></param>源数组
        /// <param name="destination"></param>目标数组
        private void AddArrayToOther(int[] source, int[] destination)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == 0) continue;
                for (int j = 0; j < destination.Length; j++)
                    if (destination[j] == 0)
                    {
                        destination[j] = source[i];
                        break;
                    }
            }
        }

        //合并两个数组，去重
        private int[] JoinArray(int[] arr1, int[] arr2)
        {
            int[] tmp = new int[LongOfCardNZ(arr1) + LongOfCardNZ(arr2)];
            int cc = 0;
            for (int i = 0; i < arr1.Length; i++)
                if (arr1[i] > 0)
                    tmp[cc++] = arr1[i];
            for (int i = 0; i < arr2.Length; i++)
                if (arr2[i] > 0)
                    tmp[cc++] = arr2[i];
            Array.Sort(tmp);

            for (int i = 1; i < tmp.Length; i++)
                if (tmp[i - 1] == tmp[i])
                    tmp[i - 1] = 0;
            Array.Sort(tmp);

            int[] tmp1 = new int[LongOfCardNZ(tmp)];
            cc = tmp.Length - tmp1.Length;
            for (int i = cc; i < tmp.Length; i++)
                tmp1[i - cc] = tmp[i];
            return tmp1;
        }

        /// <summary>
        /// 测量数组的非零元素的长度
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        public static int LongOfCardNZ(int[] ar)
        {
            int count = 0;
            for (int i = 0; i < ar.Length; i++)
                if (ar[i] > 0) count++;
            return count;
        }
        public static int LongOfCardNZ(double[] ar)
        {
            if (ar == null) return 0;
            int count = 0;
            for (int i = 0; i < ar.Length; i++)
                if (ar[i] > 0) count++;
            return count;
        }
        public static int LongOfCardNZ(double[][] ar)
        {
            if (ar == null) return 0;
            int count = 0;
            for (int i = 0; i < ar.Length; i++)
                if (ar[i] != null) count++;
            return count;
        }

        /// <summary>
        /// 数组中有某个数的个数
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        public static int HowManyNumberInArray(int[] ar, int card)
        {
            int count = 0;
            for (int i = 0; i < ar.Length; i++)
                if (ar[i] == card) count++;
            return count;
        }







        public static String[] fan_name = new String[]
        {
            "无","无","无","无",
            "牌","牌","牌","牌","牌","牌","牌",
            "牌","牌","牌","牌","牌","牌","牌",
            "大四喜", "大三元", "绿一色", "九莲宝灯", "四杠", "连七对", "十三幺",
            "清幺九", "小四喜", "小三元", "字一色", "四暗刻", "一色双龙会",
            "一色四同顺", "一色四节高",
            "一色四步高", "三杠", "混幺九",
            "七对", "七星不靠", "全双刻", "清一色", "一色三同顺", "一色三节高", "全大", "全中", "全小",
            "清龙", "三色双龙会", "一色三步高", "全带五", "三同刻", "三暗刻",
            "全不靠", "组合龙", "大于五", "小于五", "三风刻",
            "花龙", "推不倒", "三色三同顺", "三色三节高", "无番和", "妙手回春", "海底捞月", "杠上开花", "抢杠和",
            "碰碰和", "混一色", "三色三步高", "五门齐", "全求人", "双暗杠", "双箭刻",
            "全带幺", "不求人", "双明杠", "和绝张",
            "箭刻", "圈风刻", "门风刻", "门前清", "平和", "四归一", "双同刻", "双暗刻", "暗杠", "断幺",
            "一般高", "喜相逢", "连六", "老少副", "幺九刻", "明杠", "缺一门", "无字", "边张", "嵌张", "单钓将", "自摸",
            "花牌"
        #if SUPPORT_CONCEALED_KONG_AND_MELDED_KONG
            , "明暗杠",
        #endif
        };
        /**
     * @brief 番值
     */
        public static int[] fan_value_table = new int[] {
            0, 0, 0, 0,
            0,0,0,0,0,0,0, 0,0,0,0,0,0,0,  //yb14
            88, 88, 88, 88, 88, 88, 88,
            64, 64, 64, 64, 64, 64,
            48, 48,
            32, 32, 32,
            24, 24, 24, 24, 24, 24, 24, 24, 24,
            16, 16, 16, 16, 16, 16,
            12, 12, 12, 12, 12,
            8, 8, 8, 8, 8, 8, 8, 8, 8,
            6, 6, 6, 6, 6, 6, 6,
            4, 4, 4, 4,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1
        #if SUPPORT_CONCEALED_KONG_AND_MELDED_KONG
            , 5,
        #endif
            0
        };


        // 统一调整一些不计的
        public void adjust_fan_table()
        {
            // 大四喜不计三风刻、碰碰和、圈风刻、门风刻、幺九刻
            if (fan_table[(int)FanT.BIG_FOUR_WINDS] > 0)
            {
                fan_table[(int)FanT.BIG_THREE_WINDS] = 0;
                fan_table[(int)FanT.ALL_PUNGS] = 0;
                fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] = 0;
                fan_table[(int)FanT.PREVALENT_WIND] = 0;
                fan_table[(int)FanT.SEAT_WIND] = 0;
            }
            // 大三元不计双箭刻、箭刻（严格98规则不计缺一门）
            if (fan_table[(int)FanT.BIG_THREE_DRAGONS] > 0)
            {
                fan_table[(int)FanT.TWO_DRAGONS_PUNGS] = 0;
                fan_table[(int)FanT.DRAGON_PUNG] = 0;

                //fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
                if (fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] >= 3)
                    fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] -= 3;

            }
            // 绿一色不计混一色、缺一门
            if (fan_table[(int)FanT.ALL_GREEN] > 0)
            {
                fan_table[(int)FanT.HALF_FLUSH] = 0;
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
            }
            // 九莲宝灯不计清一色、门前清、缺一门、无字，减计1个幺九刻，把不求人修正为自摸
            if (fan_table[(int)FanT.NINE_GATES] > 0)
            {
                fan_table[(int)FanT.FULL_FLUSH] = 0;
                fan_table[(int)FanT.CONCEALED_HAND] = 0;
                if (fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] > 0)//1028
                    --fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS];
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
                if (fan_table[(int)FanT.FULLY_CONCEALED_HAND] > 0)
                {
                    fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 0;
                    fan_table[(int)FanT.SELF_DRAWN] = 1;
                }
            }
            // 四杠不计单钓将、对对胡、明杠、暗杠
            if (fan_table[(int)FanT.FOUR_KONGS] > 0)
            {
                fan_table[(int)FanT.SINGLE_WAIT] = 0;
                fan_table[(int)FanT.ALL_PUNGS] = 0;
                fan_table[(int)FanT.MELDED_KONG] = 0;
                fan_table[(int)FanT.CONCEALED_KONG] = 0;
                fan_table[(int)FanT.TWO_CONCEALED_KONGS] = 0;
                fan_table[(int)FanT.TWO_MELDED_KONGS] = 0;
            }
            // 三杠不计明杠、暗杠
            if (fan_table[(int)FanT.THREE_KONGS] > 0)
            {
                fan_table[(int)FanT.TWO_CONCEALED_KONGS] = 0;
                fan_table[(int)FanT.TWO_MELDED_KONGS] = 0;
                fan_table[(int)FanT.CONCEALED_KONG] = 0;
                fan_table[(int)FanT.MELDED_KONG] = 0;
            }
            // 连七对不计七对、清一色、门前清、缺一门、无字
            if (fan_table[(int)FanT.SEVEN_SHIFTED_PAIRS] > 0)
            {
                fan_table[(int)FanT.SEVEN_PAIRS] = 0;
                fan_table[(int)FanT.FULL_FLUSH] = 0;
                fan_table[(int)FanT.CONCEALED_HAND] = 0;
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
                //1029
                fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.ALL_CHOWS] = 0;
                fan_table[(int)FanT.SHORT_STRAIGHT] = 0;

                fan_table[(int)FanT.EDGE_WAIT] = 0;
                fan_table[(int)FanT.CLOSED_WAIT] = 0;
            }
            // 十三幺不计五门齐、门前清、单钓将、混幺九
            if (fan_table[(int)FanT.THIRTEEN_ORPHANS] > 0)
            {
                fan_table[(int)FanT.ALL_TYPES] = 0;
                fan_table[(int)FanT.CONCEALED_HAND] = 0;
                fan_table[(int)FanT.SINGLE_WAIT] = 0;
                fan_table[(int)FanT.ALL_TERMINALS_AND_HONORS] = 0;
                fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 0;
            }

            // 清幺九不计混幺九、碰碰胡、全带幺、幺九刻、无字（严格98规则不计双同刻、不计三同刻）
            if (fan_table[(int)FanT.ALL_TERMINALS] > 0)
            {
                fan_table[(int)FanT.ALL_TERMINALS_AND_HONORS] = 0;
                fan_table[(int)FanT.ALL_PUNGS] = 0;
                fan_table[(int)FanT.OUTSIDE_HAND] = 0;
                fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
                fan_table[(int)FanT.DOUBLE_PUNG] = 0;  // 通行计法不计双同刻

                //fan_table[(int)FanT.TRIPLE_PUNG] = 0;
                fan_table[(int)FanT.DOUBLE_PUNG] = 0;

            }

            // 小四喜不计三风刻
            if (fan_table[(int)FanT.LITTLE_FOUR_WINDS] > 0)
            {
                fan_table[(int)FanT.BIG_THREE_WINDS] = 0;
                // 小四喜的第四组牌如果是19的刻子，则是混幺九；如果是箭刻则是字一色；这两种都是不计幺九刻的
                // 如果是顺子或者2-8的刻子，则不存在多余的幺九刻
                // 所以这里将幺九刻置为0
                fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] = 0;
            }

            // 小三元不计双箭刻、箭刻（严格98规则不计缺一门）
            if (fan_table[(int)FanT.LITTLE_THREE_DRAGONS] > 0)
            {
                fan_table[(int)FanT.TWO_DRAGONS_PUNGS] = 0;
                fan_table[(int)FanT.DRAGON_PUNG] = 0;

                //fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0; 
                fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] -= 2;
            }

            // 字一色不计混幺九、碰碰胡、全带幺、幺九刻、缺一门
            if (fan_table[(int)FanT.ALL_HONORS] > 0)
            {
                fan_table[(int)FanT.ALL_TERMINALS_AND_HONORS] = 0;
                fan_table[(int)FanT.ALL_PUNGS] = 0;
                fan_table[(int)FanT.OUTSIDE_HAND] = 0;
                fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] = 0;
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
            }
            // 四暗刻不计碰碰和、门前清，把不求人修正为自摸、没有同顺了
            if (fan_table[(int)FanT.FOUR_CONCEALED_PUNGS] > 0)//1029
            {
                fan_table[(int)FanT.ALL_PUNGS] = 0;
                fan_table[(int)FanT.CONCEALED_HAND] = 0;
                if (fan_table[(int)FanT.FULLY_CONCEALED_HAND] > 0)
                {
                    fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 0;
                    fan_table[(int)FanT.SELF_DRAWN] = 1;
                }
                fan_table[(int)FanT.QUADRUPLE_CHOW] = 0;
            }
            // 一色双龙会不计七对、清一色、平和、一般高、老少副、缺一门、无字
            if (fan_table[(int)FanT.PURE_TERMINAL_CHOWS] > 0)
            {
                fan_table[(int)FanT.SEVEN_PAIRS] = 0;
                fan_table[(int)FanT.FULL_FLUSH] = 0;
                fan_table[(int)FanT.ALL_CHOWS] = 0;
                fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
            }

            // 一色四同顺不计一色三同顺、一般高、四归一（严格98规则不计缺一门）
            if (fan_table[(int)FanT.QUADRUPLE_CHOW] > 0)
            {
                fan_table[(int)FanT.PURE_SHIFTED_PUNGS] = 0;
                fan_table[(int)FanT.THREE_CONCEALED_PUNGS] = 0;
                fan_table[(int)FanT.TWO_CONCEALED_PUNGS] = 0;
                fan_table[(int)FanT.TILE_HOG] = 0;
                fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.SEVEN_PAIRS] = 0;
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;

            }
            // 一色四节高不计一色三节高、碰碰和（严格98规则不计缺一门）
            if (fan_table[(int)FanT.FOUR_PURE_SHIFTED_PUNGS] > 0)
            {
                fan_table[(int)FanT.PURE_TRIPLE_CHOW] = 0;
                fan_table[(int)FanT.ALL_PUNGS] = 0;
                fan_table[(int)FanT.SEVEN_PAIRS] = 0;//1029
                //fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
            }

            // 一色四步高不计一色三步高、老少副、连六（严格98规则不计缺一门）
            if (fan_table[(int)FanT.FOUR_PURE_SHIFTED_CHOWS] > 0)
            {
                fan_table[(int)FanT.PURE_SHIFTED_CHOWS] = 0;
                fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
                fan_table[(int)FanT.SHORT_STRAIGHT] = 0;
                fan_table[(int)FanT.SEVEN_PAIRS] = 0;//1029
                //fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;//202205
            }

            // 混幺九不计碰碰和、全带幺、幺九刻
            if (fan_table[(int)FanT.ALL_TERMINALS_AND_HONORS] > 0)
            {
                fan_table[(int)FanT.ALL_PUNGS] = 0;
                fan_table[(int)FanT.OUTSIDE_HAND] = 0;
                fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] = 0;
            }

            // 七星不靠不计五门齐、门前清、不求人、单钓将、全不靠。
            if (fan_table[(int)FanT.GREATER_HONORS_AND_KNITTED_TILES] > 0)
            {
                fan_table[(int)FanT.ALL_TYPES] = 0;
                fan_table[(int)FanT.CONCEALED_HAND] = 0;
                fan_table[(int)FanT.LESSER_HONORS_AND_KNITTED_TILES] = 0;
                fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 0;
                fan_table[(int)FanT.SINGLE_WAIT] = 0;
            }
            //全不靠不计门前清、五门齐、不求人
            if (fan_table[(int)FanT.LESSER_HONORS_AND_KNITTED_TILES] > 0)//202205
            {
                fan_table[(int)FanT.ALL_TYPES] = 0;
                fan_table[(int)FanT.CONCEALED_HAND] = 0;
                fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 0;
            }
            //七星不靠.全不靠.组合龙没有大于五、小于五、全带五、全大、全小、全中
            if (fan_table[(int)FanT.GREATER_HONORS_AND_KNITTED_TILES] + fan_table[(int)FanT.LESSER_HONORS_AND_KNITTED_TILES] + fan_table[(int)FanT.KNITTED_STRAIGHT] > 0)
            {
                fan_table[(int)FanT.UPPER_FOUR] = 0;
                fan_table[(int)FanT.LOWER_FOUR] = 0;
                fan_table[(int)FanT.ALL_FIVE] = 0;
                fan_table[(int)FanT.UPPER_TILES] = 0;
                fan_table[(int)FanT.MIDDLE_TILES] = 0;
                fan_table[(int)FanT.LOWER_TILES] = 0;
            }
            // 全双刻不计碰碰胡、断幺、无字
            if (fan_table[(int)FanT.ALL_EVEN_PUNGS] > 0)
            {
                fan_table[(int)FanT.ALL_PUNGS] = 0;
                fan_table[(int)FanT.ALL_SIMPLES] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            // 清一色不计缺一门、无字
            if (fan_table[(int)FanT.FULL_FLUSH] > 0)
            {
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            // 一色三同顺不计一色三节高、一般高
            if (fan_table[(int)FanT.PURE_TRIPLE_CHOW] > 0)
            {
                fan_table[(int)FanT.PURE_SHIFTED_PUNGS] = 0;
                fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.SEVEN_PAIRS] = 0;//1029
            }
            // 一色三节高不计一色三同顺  
            if (fan_table[(int)FanT.PURE_SHIFTED_PUNGS] > 0)
            {
                fan_table[(int)FanT.PURE_TRIPLE_CHOW] = 0;
                fan_table[(int)FanT.SEVEN_PAIRS] = 0;//1029
                fan_table[(int)FanT.PURE_STRAIGHT] = 0;
            }
            // 一色三步高不计清龙
            if (fan_table[(int)FanT.PURE_SHIFTED_CHOWS] > 0)
                fan_table[(int)FanT.PURE_STRAIGHT] = 0;
            // 清龙不计连六 老少副
            if (fan_table[(int)FanT.PURE_STRAIGHT] > 0)
            {
                fan_table[(int)FanT.SHORT_STRAIGHT] = 0;
                fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
            }
            // 全大不计大于五、无字、七对
            if (fan_table[(int)FanT.UPPER_TILES] > 0)
            {
                //fan_table[(int)FanT.SEVEN_PAIRS] = 0;//1029
                fan_table[(int)FanT.UPPER_FOUR] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            // 全中不计断幺、七对
            if (fan_table[(int)FanT.MIDDLE_TILES] > 0)
            {
                //fan_table[(int)FanT.SEVEN_PAIRS] = 0;//1029
                fan_table[(int)FanT.ALL_SIMPLES] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            // 全小不计小于五、无字、七对
            if (fan_table[(int)FanT.LOWER_TILES] > 0)
            {
                //fan_table[(int)FanT.SEVEN_PAIRS] = 0;//1029
                fan_table[(int)FanT.LOWER_FOUR] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            // 三暗刻不计平胡、老少胡
            if (fan_table[(int)FanT.THREE_CONCEALED_PUNGS] > 0)//1029
            {
                fan_table[(int)FanT.ALL_CHOWS] = 0;
                fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
            }
            // 七对不计门前清、单钓将、不求人 平和、喜相逢、老少副、嵌张、边张
            if (fan_table[(int)FanT.SEVEN_PAIRS] > 0)//1029
            {
                fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 0;
                fan_table[(int)FanT.CONCEALED_HAND] = 0;
                fan_table[(int)FanT.SINGLE_WAIT] = 0;

                fan_table[(int)FanT.ALL_FIVE] = 0;
                fan_table[(int)FanT.ALL_CHOWS] = 0;
                fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.MIXED_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.SHORT_STRAIGHT] = 0;
                fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
                fan_table[(int)FanT.CLOSED_WAIT] = 0;
                fan_table[(int)FanT.EDGE_WAIT] = 0;
                fan_table[(int)FanT.TWO_CONCEALED_PUNGS] = 0;
                fan_table[(int)FanT.THREE_CONCEALED_PUNGS] = 0;
            }
            // 三色双龙会不计平和、无字、喜相逢、老少副
            if (fan_table[(int)FanT.THREE_SUITED_TERMINAL_CHOWS] > 0)
            {
                fan_table[(int)FanT.ALL_CHOWS] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
                fan_table[(int)FanT.MIXED_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
            }
            // 全带五不计断幺、无字
            if (fan_table[(int)FanT.ALL_FIVE] > 0)
            {
                fan_table[(int)FanT.ALL_SIMPLES] = 0;
                fan_table[(int)FanT.NO_HONORS] = 0;
            }

            // 七星不靠不计五门齐、门前清
            if (fan_table[(int)FanT.LESSER_HONORS_AND_KNITTED_TILES] > 0)
            {
                fan_table[(int)FanT.ALL_TYPES] = 0;
                fan_table[(int)FanT.CONCEALED_HAND] = 0;
            }
            // 大于五不计无字
            if (fan_table[(int)FanT.UPPER_FOUR] > 0)
            {
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            // 小于五不计无字
            if (fan_table[(int)FanT.LOWER_FOUR] > 0)
            {
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            // 三风刻内部不再计幺九刻（严格98规则不计缺一门）
            if (fan_table[(int)FanT.BIG_THREE_WINDS] > 0)
            {
                // 如果不是字一色或混幺九，则要减去3个幺九刻
                if (fan_table[(int)FanT.ALL_HONORS] == 0 && fan_table[(int)FanT.ALL_TERMINALS_AND_HONORS] == 0)
                {
                    if (fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] >= 3)
                        fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] -= 3;
                }
                //fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
            }

            // 推不倒不计缺一门
            if (fan_table[(int)FanT.REVERSIBLE_TILES] > 0)
            {
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
            }
            // 妙手回春不计自摸
            if (fan_table[(int)FanT.LAST_TILE_DRAW] > 0)
            {
                fan_table[(int)FanT.SELF_DRAWN] = 0;
            }
            // 杠上开花不计自摸
            if (fan_table[(int)FanT.OUT_WITH_REPLACEMENT_TILE] > 0)
            {
                fan_table[(int)FanT.SELF_DRAWN] = 0;
            }
            // 抢杠和不计和绝张
            if (fan_table[(int)FanT.ROBBING_THE_KONG] > 0)
            {
                fan_table[(int)FanT.LAST_TILE] = 0;
            }
            // 双暗杠不计暗杠、双暗刻
            if (fan_table[(int)FanT.TWO_CONCEALED_KONGS] > 0)
            {
                fan_table[(int)FanT.TWO_CONCEALED_PUNGS] = 0;
                fan_table[(int)FanT.CONCEALED_KONG] = 0;
            }

            // 混一色不计缺一门
            if (fan_table[(int)FanT.HALF_FLUSH] > 0)
            {
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 0;
            }
            // 全求人不计单钓将
            if (fan_table[(int)FanT.MELDED_HAND] > 0)
            {
                fan_table[(int)FanT.SINGLE_WAIT] = 0;
            }
            // 双箭刻不计箭刻, 幺九刻减2
            if (fan_table[(int)FanT.TWO_DRAGONS_PUNGS] > 0)
            {
                fan_table[(int)FanT.DRAGON_PUNG] = 0;
                if (fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] >= 2)
                    fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] -= 2;
            }
            // 箭刻不计箭刻，幺九刻减1
            if (fan_table[(int)FanT.DRAGON_PUNG] > 0)
            {
                if (fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] >= 1)
                    fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] -= 1;
            }

            // 不求人不计自摸
            if (fan_table[(int)FanT.FULLY_CONCEALED_HAND] > 0)
            {
                fan_table[(int)FanT.SELF_DRAWN] = 0;
            }
            // 双明杠不计明杠
            if (fan_table[(int)FanT.TWO_MELDED_KONGS] > 0)
            {
                fan_table[(int)FanT.MELDED_KONG] = 0;
            }

            // 断幺不计无字
            if (fan_table[(int)FanT.ALL_SIMPLES] > 0)
            {
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            // 圈风刻的那副刻子不再计幺九刻
            if (fan_table[(int)FanT.PREVALENT_WIND] > 0)//1029
            {
                if (fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] >= 1)
                    fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS]--;
            }
            // 门风刻的那副刻子不再计幺九刻
            if (fan_table[(int)FanT.SEAT_WIND] > 0 && FanCardData.quan_wind != FanCardData.seat_wind)//202205
            {
                if (fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] >= 1)
                    fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS]--;
            }
            // 边张、嵌张时不计单钓将
            if (fan_table[(int)FanT.CLOSED_WAIT] + fan_table[(int)FanT.EDGE_WAIT] > 0)//1029 
                fan_table[(int)FanT.SINGLE_WAIT] = 0;
            // 嵌张没有边张
            if (fan_table[(int)FanT.CLOSED_WAIT] > 0)//1029 
                fan_table[(int)FanT.EDGE_WAIT] = 0;
            // 组合龙没有全带幺
            if (fan_table[(int)FanT.KNITTED_STRAIGHT] > 0)//1029 
            {
                fan_table[(int)FanT.OUTSIDE_HAND] = 0;
                //fan_table[(int)FanT.ALL_CHOWS] = 0; 
            }
            // 平和不计无字
            if (fan_table[(int)FanT.ALL_CHOWS] > 0)
            {
                fan_table[(int)FanT.NO_HONORS] = 0;
            }
            int sco = 0; //1028
            for (int tt = 0; tt < fan_table.Length; tt++)
                sco += fan_table[tt];
            if (sco == 0)// 无番和
                fan_table[(int)FanT.CHICKEN_HAND] = 1;

            if (fan_table[(int)FanT.MELDED_KONG] > 0 && fan_table[(int)FanT.CONCEALED_KONG] > 0)//202205 明暗杠
            {
                fan_table[(int)FanT.CONCEALED_KONG_AND_MELDED_KONG] = 1;
                fan_table[(int)FanT.MELDED_KONG]--;
                fan_table[(int)FanT.CONCEALED_KONG]--;
            }

            /*1998 年国家体育总局审定的《中国麻将竞赛规则（试行）》中，规定了 5 条国标麻将的计分原则：
            1.不重复原则
            当某个番种，由于组牌的条件所决定，在其成立的同时，必然并存在着其他番种，则其他番种不重复计分。
            2.不拆移原则
            确定一个番种后，不能将其自身再拆开互相组成新的番种计分。
            3.不得相同的原则
            凡已组合过某一番种的牌，不能再同其他一副牌组成相同的番种计分。
            4.就高不就低原则
            有两副以上的牌，有可能组成两个以上的番种，而只能选其中一种计分时，可选择分高的番种计分。
            5.套算一次原则
            如有尚未组合过的一副牌，只可同已组合过的相应的一副牌套算一次。*/

            int shunNum = 0;
            int[] shun = new int[4];
            int[] tmp = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0 && FanCardData.ArrMshun[i] < 30)
                    shun[shunNum++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0 && FanCardData.ArrAshun[i] < 30)
                    shun[shunNum++] = FanCardData.ArrAshun[i];

            //1.当四顺子番种（一色双龙会、一色四同顺、一色四步高、三色双龙会）存在时，不计般逢老连。
            if (fan_table[(int)FanT.PURE_TERMINAL_CHOWS] + fan_table[(int)FanT.QUADRUPLE_CHOW] +
                fan_table[(int)FanT.FOUR_PURE_SHIFTED_CHOWS] + fan_table[(int)FanT.THREE_SUITED_TERMINAL_CHOWS] > 0)
            {
                fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.MIXED_DOUBLE_CHOW] = 0;
                fan_table[(int)FanT.SHORT_STRAIGHT] = 0;
                fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
            }

            //2.当三顺子番种（一色三同顺、清龙、一色三步高、花龙、三色三同顺、三色三步高）存在时，般逢老连最多另加计 1 番。
            else if (fan_table[(int)FanT.PURE_TRIPLE_CHOW] + fan_table[(int)FanT.PURE_STRAIGHT] +
                fan_table[(int)FanT.PURE_SHIFTED_CHOWS] + fan_table[(int)FanT.MIXED_STRAIGHT] +
                fan_table[(int)FanT.MIXED_TRIPLE_CHOW] + fan_table[(int)FanT.MIXED_SHIFTED_CHOWS] > 0)
            {
                if (fan_table[(int)FanT.PURE_DOUBLE_CHOW] > 0)
                {
                    fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 1;
                    fan_table[(int)FanT.MIXED_DOUBLE_CHOW] = fan_table[(int)FanT.SHORT_STRAIGHT] = fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
                }
                else if (fan_table[(int)FanT.MIXED_DOUBLE_CHOW] > 0)
                {
                    fan_table[(int)FanT.MIXED_DOUBLE_CHOW] = 1;
                    fan_table[(int)FanT.SHORT_STRAIGHT] = fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
                }
                else if (fan_table[(int)FanT.SHORT_STRAIGHT] > 0)
                {
                    fan_table[(int)FanT.SHORT_STRAIGHT] = 1;
                    fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 0;
                }
                else if (fan_table[(int)FanT.TWO_TERMINAL_CHOWS] > 0)
                    fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 1;
            }
            //3.无上述番种存在时，4 副顺子之间，般逢老连最多计 3 番。
            else if (shunNum == 4 && fan_table[(int)FanT.PURE_DOUBLE_CHOW] + fan_table[(int)FanT.MIXED_DOUBLE_CHOW] +
                fan_table[(int)FanT.SHORT_STRAIGHT] + fan_table[(int)FanT.TWO_TERMINAL_CHOWS] > 3)
            {
                if (fan_table[(int)FanT.TWO_TERMINAL_CHOWS] > 0)
                    fan_table[(int)FanT.TWO_TERMINAL_CHOWS]--;
                if (fan_table[(int)FanT.PURE_DOUBLE_CHOW] + fan_table[(int)FanT.MIXED_DOUBLE_CHOW] +
                fan_table[(int)FanT.SHORT_STRAIGHT] + fan_table[(int)FanT.TWO_TERMINAL_CHOWS] > 3)
                    if (fan_table[(int)FanT.SHORT_STRAIGHT] > 0)
                        fan_table[(int)FanT.SHORT_STRAIGHT]--;
            }

            //4.无上述番种存在时，3 副顺子之间，般逢老连最多计 2 番。
            else if (shunNum == 3 && fan_table[(int)FanT.PURE_DOUBLE_CHOW] + fan_table[(int)FanT.MIXED_DOUBLE_CHOW] +
                fan_table[(int)FanT.SHORT_STRAIGHT] + fan_table[(int)FanT.TWO_TERMINAL_CHOWS] > 2)
            {
                if (fan_table[(int)FanT.TWO_TERMINAL_CHOWS] > 0)
                    fan_table[(int)FanT.TWO_TERMINAL_CHOWS]--;
                if (fan_table[(int)FanT.PURE_DOUBLE_CHOW] + fan_table[(int)FanT.MIXED_DOUBLE_CHOW] +
                fan_table[(int)FanT.SHORT_STRAIGHT] + fan_table[(int)FanT.TWO_TERMINAL_CHOWS] > 2)
                    if (fan_table[(int)FanT.SHORT_STRAIGHT] > 0)
                        fan_table[(int)FanT.SHORT_STRAIGHT]--;
                if (fan_table[(int)FanT.PURE_DOUBLE_CHOW] + fan_table[(int)FanT.MIXED_DOUBLE_CHOW] +
                fan_table[(int)FanT.SHORT_STRAIGHT] + fan_table[(int)FanT.TWO_TERMINAL_CHOWS] > 2)
                    if (fan_table[(int)FanT.MIXED_DOUBLE_CHOW] > 0)
                        fan_table[(int)FanT.MIXED_DOUBLE_CHOW]--;
            }
        }


        //--------------------------------------88
        //大四喜
        public int If_Da4Xi()
        {
            int[] tmp_card = FanCardData.HandIn31;

            if (tmp_card[31] > 2 && tmp_card[33] > 2 && tmp_card[35] > 2 && tmp_card[37] > 2)
            {
                fan_table[(int)FanT.BIG_FOUR_WINDS] = 1;
                return 88;
            }

            else
                return 0;
        }
        //大三元
        public int If_Da3Yuan()//202205
        {
            int Jianke = 0;
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
            {
                if (FanCardData.ArrAke[i] > 38)
                    Jianke++;
                if (FanCardData.ArrMke[i] > 38)
                    Jianke++;
                if (FanCardData.ArrMgang[i] > 38)
                    Jianke++;
                if (FanCardData.ArrAgang[i] > 38)
                    Jianke++;
            }
            if (Jianke > 2)
            {
                fan_table[(int)FanT.BIG_THREE_DRAGONS] = 1;
                return 88;
            }
            return 0;
        }

        // 九莲宝灯
        public int If_9LianBD()
        {
            if (LongOfCardNZ(FanCardData.HandIn14) < 14)
                return 0;
            if (FanCardData.winCard <= 0)
                return 0;

            int[] tmp31 = new int[FanCardData.HandIn31.Length];
            FanCardData.HandIn31.CopyTo(tmp31, 0);
            tmp31[FanCardData.winCard]--;
            for (int i = 0; i < 3; i++)
            {
                if (tmp31[i * 10 + 1] == 3 && tmp31[i * 10 + 9] == 3)
                    if (tmp31[i * 10 + 2] == 1 && tmp31[i * 10 + 3] == 1 && tmp31[i * 10 + 4] == 1 && tmp31[i * 10 + 5] == 1 &&
                        tmp31[i * 10 + 6] == 1 && tmp31[i * 10 + 7] == 1 && tmp31[i * 10 + 8] == 1)
                    {
                        fan_table[(int)FanT.NINE_GATES] = 1;
                        return 88;
                    }
            }
            return 0;
        }

        public int If_ThirteenOne(int[] yb)
        {
            int[] HandIn14 = new int[FanCardData.HandIn14.Length];
            int[] HandIn31 = new int[FanCardData.HandIn31.Length];
            FanCardData.HandIn14.CopyTo(HandIn14, 0);
            FanCardData.HandIn31.CopyTo(HandIn31, 0);
            Array.Clear(FanCardData.HandIn14, 0, FanCardData.HandIn14.Length);
            Array.Clear(FanCardData.HandIn31, 0, FanCardData.HandIn31.Length);

            yb.CopyTo(FanCardData.HandIn14, 0);
            for (int i = 0; i < yb.Length; i++)
                FanCardData.HandIn31[yb[i]]++;
            FanCardData.HandIn31[0] = 0;
            int ret = If_ThirteenOne();

            HandIn14.CopyTo(FanCardData.HandIn14, 0);
            HandIn31.CopyTo(FanCardData.HandIn31, 0);
            return ret;
        }
        /// 十三幺        
        public int If_ThirteenOne()
        {
            if (FanCardData.HandIn14[0] * FanCardData.HandIn14[13] == 0)
                return 0;

            for (int i = 1; i < FanCardData.HandIn31.Length; i++)
                if (i < 30 && i % 10 % 8 == 1 && (FanCardData.HandIn31[i] == 0 || FanCardData.HandIn31[i] > 2))
                    return 0;
                else if (i < 30 && i % 10 % 8 != 1 && FanCardData.HandIn31[i] > 0)
                    return 0;
                else if (i > 30 && i % 2 == 1 && (FanCardData.HandIn31[i] == 0 || FanCardData.HandIn31[i] > 2))
                    return 0;
            fan_table[(int)FanT.THIRTEEN_ORPHANS] = 1;
            return 88;
        }

        /// 绿一色        
        public int If_LvYiSe()
        {
            int[] lv = new int[] { 2, 3, 4, 6, 8, 41 };

            for (int i = 0; i < FanCardData.FullCard.Length; i++)
            {
                bool bb = false;
                for (int j = 0; j < lv.Length; j++)
                    if (FanCardData.FullCard[i] == lv[j])
                    { bb = true; break; }
                if (!bb) return 0;
            }
            fan_table[(int)FanT.ALL_GREEN] = 1;
            return 88;
        }
        /// 连七对        
        public int If_Lian7Dui()
        {
            int[] ac = FanCardData.FullCard;
            if (If_7Dui() > 0)
                if (ac[1] == ac[2] - 1 && ac[3] == ac[4] - 1 && ac[5] == ac[6] - 1 &&
                    ac[7] == ac[8] - 1 && ac[9] == ac[10] - 1 && ac[11] == ac[12] - 1)
                {
                    fan_table[(int)FanT.SEVEN_SHIFTED_PAIRS] = 1;
                    return 88;
                }
            return 0;
        }
        ///三杠     
        public int If_34Gang()
        {
            int cc = 0;
            for (int i = 0; i < FanCardData.ArrMgang.Length; i++)
                if (FanCardData.ArrMgang[i] > 0)
                    cc++;
            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0)
                    cc++;
            if (cc == 3)
            {
                fan_table[(int)FanT.THREE_KONGS] = 1;
                return 32;
            }
            else if (cc == 4)
            {
                fan_table[(int)FanT.FOUR_KONGS] = 1;
                return 88;
            }
            return 0;
        }
        //--------------------------------------64 
        /// 清幺九        
        public int If_QinYaoJiu()
        {
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 % 8 != 1 || FanCardData.FullCard[i] > 30)
                    return 0;
            fan_table[(int)FanT.ALL_TERMINALS] = 1;
            return 64;
        }
        // 一色双龙会 胡牌时，牌型由一种花色的两个老少副，5为将牌组成。
        public int If_1Se2LongHui()
        {
            if (FanCardData.FullCard[0] / 10 != FanCardData.FullCard[13] / 10)
                return 0;
            if (FanCardData.Jiang % 10 != 5)
                return 0;
            int cc = 0;
            int[] shun = new int[4];
            int[] tmp = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0 && FanCardData.ArrMshun[i] < 30)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0 && FanCardData.ArrAshun[i] < 30)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 4)
                return 0;
            Array.Sort(shun);
            if (shun[0] / 10 != shun[3] / 10)
                return 0;
            if (shun[0] % 10 == 2 && shun[1] % 10 == 2 &&
                shun[2] % 10 == 8 && shun[3] % 10 == 8)
            {
                fan_table[(int)FanT.PURE_TERMINAL_CHOWS] = 1;
                return 64;
            }
            return 0;
        }

        /// 字一色        
        public int If_ZiYiSe()
        {
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] < 30)
                    return 0;
            fan_table[(int)FanT.ALL_HONORS] = 1;
            return 64;
        }

        //------------------------------------------48分    
        ///一色四同顺 牌里有一种花色且序数相同的4副顺子。不记番:一色三节高、一般高、四归一，一色三同顺、七对。

        ///一色四节高 
        ///胡牌时牌里有一种花色且序数依次递增一位数的4副刻子(或杠子)。
        ///不记番:一色三同顺、一色三节高、碰碰和。
        public int If_1Se34JieGao()
        {
            int cc = 0;
            int[] ke = new int[4];
            int[] tmp = new int[4];//1029
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] > 0 && FanCardData.ArrAke[i] < 30)
                    ke[cc++] = FanCardData.ArrAke[i];
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0 && FanCardData.ArrMke[i] < 30)
                    ke[cc++] = FanCardData.ArrMke[i];
            for (int i = 0; i < FanCardData.ArrMgang.Length; i++)
                if (FanCardData.ArrMgang[i] > 0 && FanCardData.ArrMgang[i] < 30)
                    ke[cc++] = FanCardData.ArrMgang[i];
            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0 && FanCardData.ArrAgang[i] < 30)
                    ke[cc++] = FanCardData.ArrAgang[i];
            if (cc < 3)
                return 0;

            Array.Sort(ke);

            if (cc == 4)
            {
                if (ke[1] - ke[0] == 1 && ke[2] - ke[1] == 1 &&
                    ke[3] - ke[2] == 1)
                {
                    fan_table[(int)FanT.FOUR_PURE_SHIFTED_PUNGS] = 1;
                    return 48;
                }
                // [3 4 5 16]  [1 3 4 5]  [3 4 4 5]
                if (ke[0] + 1 == ke[1] && ke[1] + 1 == ke[2] ||
                    ke[1] + 1 == ke[2] && ke[2] + 1 == ke[3] ||
                    ke[0] + 1 == ke[2] && ke[2] + 1 == ke[3])
                {
                    fan_table[(int)FanT.PURE_SHIFTED_PUNGS] = 1;
                    return 24;
                }

            }
            else if (cc == 3)
            {
                if (ke[2] - ke[1] == 1 && ke[3] - ke[2] == 1)
                {
                    fan_table[(int)FanT.PURE_SHIFTED_PUNGS] = 1;
                    return 24;
                }
            }
            return 0;
        }



        //------------------------------------------32分    
        ///一色四步高 胡牌时，牌里有一种花色4副依次递增一位数或依次递增二位数的顺子。不记番:一色三步高。
        public int If_1Se34BuGao()
        {
            int cc = 0;
            int[] shun = new int[4];
            int[] tmp = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0 && FanCardData.ArrMshun[i] < 30)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0 && FanCardData.ArrAshun[i] < 30)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 3)
                return 0;

            Array.Sort(shun);
            for (int i = 1; i < 3; i++)
            {
                if (cc == 4)
                {
                    if (shun[1] - shun[0] == i && shun[2] - shun[1] == i &&
                        shun[3] - shun[2] == i && shun[0] / 10 == shun[3] / 10)//202205
                    {
                        fan_table[(int)FanT.FOUR_PURE_SHIFTED_CHOWS] = 1;///< 一色四步高
                        return 32;
                    }
                    // [3 4 5 16]  [1 3 4 5]  [3 4 4 5]
                    if (shun[0] + i == shun[1] && shun[1] + i == shun[2] && shun[0] / 10 == shun[2] / 10 ||
                        shun[1] + i == shun[2] && shun[2] + i == shun[3] && shun[1] / 10 == shun[3] / 10 ||
                        shun[0] + i == shun[2] && shun[2] + i == shun[3] && shun[0] / 10 == shun[3] / 10)
                    {
                        fan_table[(int)FanT.PURE_SHIFTED_CHOWS] = 1;///< 一色三步高
                        return 16;
                    }

                }
                else if (cc == 3)
                {
                    if (shun[2] - shun[1] == i && shun[3] - shun[2] == i && shun[1] / 10 == shun[3] / 10)
                    {
                        fan_table[(int)FanT.PURE_SHIFTED_CHOWS] = 1;
                        return 16;
                    }
                }
            }
            return 0;
        }


        /// 混幺九        
        public int If_HunYaoJiu()
        {
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 % 8 != 1 && FanCardData.FullCard[i] < 30)
                    return 0;
            fan_table[(int)FanT.ALL_TERMINALS_AND_HONORS] = 1;
            return 32;
        }



        //------------------------------------------24分    
        ///七对  胡牌时，胡牌时，牌型由7个对子组成。（不计门前清、不求人、单钓将）
        public int If_7Dui()
        {
            //1028
            if (LongOfCardNZ(FanCardData.HandIn14) < 14)
                return 0;
            //小七对   龙七对  
            int pair = 0, long_num = 0;   //龙数
            for (int i = 0; i < FanCardData.HandIn31.Length; i++)
            {
                if (FanCardData.HandIn31[i] == 4) long_num++;
                else if (FanCardData.HandIn31[i] == 2) pair++;
            }
            if (pair + long_num * 2 == 7)
            {
                fan_table[(int)FanT.SEVEN_PAIRS] = 1;
                return 16;
            } //龙七对,小七对
            return 0;
        }
        public bool If_7Dui(int[] yb)
        {
            int pair = 0;
            Array.Sort(yb);
            for (int i = 0; i < yb.Length - 1; i += 2)
                if (yb[i] > 0 && yb[i] == yb[i + 1])
                    pair++;
            if (pair == 7)
                return true;
            return false;
        }

        ///七星不靠
        public int If_7XinBuKao()
        {
            if (If_QuanBuKao() == 0)
                return 0;
            int cc1 = 0;
            for (int i = 30; i < FanCardData.HandIn31.Length; i++)
                if (FanCardData.HandIn31[i] > 0)
                    cc1++;
            if (cc1 == 7)
            {
                fan_table[(int)FanT.GREATER_HONORS_AND_KNITTED_TILES] = 1;
                return 24;
            }
            return 0;
        }


        ///一色三同顺   和牌时有一种花色3副序数相同的顺了。不计一色三节高 一般高
        ///一般高 一色四同顺
        public int If_1Se234TongShun()
        {
            int cc = 0;
            int[] shun = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 2)
                return 0;
            Array.Sort(shun);
            if (cc == 2)
            {
                if (shun[2] == shun[3])  //一般高               
                {
                    fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 1;
                    return 1;
                }
            }
            if (cc == 3)
            {
                if (shun[1] == shun[3])  //一色三同顺               
                {
                    fan_table[(int)FanT.PURE_TRIPLE_CHOW] = 1;
                    return 24;
                }
                else if (shun[1] == shun[2] || shun[2] == shun[3])//一般高 
                {
                    fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 1;
                    return 1;
                }
            }
            else if (cc == 4)
            {
                if (shun[0] == shun[3]) //一色四同顺 
                {
                    fan_table[(int)FanT.QUADRUPLE_CHOW] = 1;
                    return 48;
                }
                else if (shun[0] == shun[2] || shun[1] == shun[3])//一色三同顺 
                {
                    fan_table[(int)FanT.PURE_TRIPLE_CHOW] = 1;
                    return 24;
                }
                else if (shun[0] == shun[1] || shun[1] == shun[2] ||
                    shun[1] == shun[2] || shun[2] == shun[3])
                {
                    fan_table[(int)FanT.PURE_DOUBLE_CHOW] = 1;
                    if (shun[0] == shun[1] && shun[2] == shun[3])
                        fan_table[(int)FanT.PURE_DOUBLE_CHOW]++;
                    return fan_table[(int)FanT.PURE_DOUBLE_CHOW];
                }
            }
            return 0;
        }

        ///一色三节高 胡牌时，牌里有一种花色且依次递增一位数字的3副刻子。不记番： 一色三同顺
        public int If_1Se3JieGao()
        {
            int cc = 0;
            int[] ke = new int[4];
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] > 0)
                    ke[cc++] = FanCardData.ArrAke[i];
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0)
                    ke[cc++] = FanCardData.ArrMke[i];
            if (cc < 3)
                return 0;
            Array.Sort(ke);
            if (cc == 3)
            {
                //同顺  [0 3 4 5]
                if (ke[1] + 1 == ke[2] && ke[2] + 1 == ke[3])
                {
                    fan_table[(int)FanT.PURE_SHIFTED_PUNGS] = 1;
                    return 24;
                }
            }
            else if (cc == 4)
            {
                // [3 4 5 16]  [1 3 4 5]  [3 4 4 5]
                if (ke[0] + 1 == ke[1] && ke[1] + 1 == ke[2] ||
                    ke[1] + 1 == ke[2] && ke[2] + 1 == ke[3] ||
                    ke[0] + 1 == ke[2] && ke[2] + 1 == ke[3])
                {
                    fan_table[(int)FanT.PURE_SHIFTED_PUNGS] = 1;
                    return 24;
                }
            }
            return 0;
        }




        //------------------------------------------16分    
        ///清龙  胡牌时，有一种相同花色的123，456，789三付顺子即可。清龙就是清一色条龙。不记番:连6、老少副。    
        public int If_QingLong()
        {
            int cc = 0;
            int[] shun = new int[4];
            int[] tmp = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0 && FanCardData.ArrMshun[i] < 30)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0 && FanCardData.ArrAshun[i] < 30)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 3)
                return 0;



            for (int i = 0; i < shun.Length; i++)
                if (shun[i] > 0 && shun[i] % 10 % 3 != 2)
                    shun[i] = 0;
            Array.Sort(shun);//1028
            if (shun[0] == 0 && shun[1] / 10 != shun[3] / 10)
                return 0;
            if (shun[0] > 0 && shun[0] / 10 != shun[2] / 10 && shun[1] / 10 != shun[3] / 10)
                return 0;
            for (int i = 1; i < shun.Length; i++)
                if (shun[i] == shun[i - 1])
                    shun[i - 1] = 0;

            cc = LongOfCardNZ(shun);
            if (LongOfCardNZ(shun) < 3)
                return 0;

            Array.Sort(shun);//202205
            if (shun[1] / 10 == shun[3] / 10 && (
                shun[0] % 10 == 2 && shun[1] % 10 == 5 && shun[2] % 10 == 8 ||  //2 5 8 18
                shun[1] % 10 == 2 && shun[2] % 10 == 5 && shun[3] % 10 == 8 ||  //2 12 15 18 
                shun[0] % 10 == 2 && shun[1] % 10 == 5 && shun[3] % 10 == 8))    //2 5 5 8 
            {
                fan_table[(int)FanT.PURE_STRAIGHT] = 1;
                return 16;
            }
            return 0;
        }


        ///三色双龙会 胡牌时，牌里有一对5作将，另两花色老少副顺子。
        public int If_3Se2LongHui()
        {
            if (FanCardData.Jiang % 10 != 5 || FanCardData.Jiang > 30)
                return 0;
            int cc = 0;
            int[] shun = new int[4];
            int[] tmp = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0 && FanCardData.ArrMshun[i] < 30)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0 && FanCardData.ArrAshun[i] < 30)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 4)
                return 0;
            for (int i = 0; i < shun.Length; i++)
                if (shun[i] / 10 == FanCardData.Jiang / 10)
                    return 0;
            Array.Sort(shun);
            if (shun[0] % 10 == 2 && shun[1] % 10 == 8 &&
                shun[2] % 10 == 2 && shun[3] % 10 == 8)
                if (shun[0] / 10 == shun[1] / 10 && shun[2] / 10 == shun[3] / 10
                    && shun[1] / 10 != shun[2] / 10)
                    if (shun[0] / 10 + shun[2] / 10 + FanCardData.Jiang / 10 == 3)
                    {
                        fan_table[(int)FanT.THREE_SUITED_TERMINAL_CHOWS] = 1;
                        return 16;
                    }
            return 0;
        }

        ///一色三步高 胡牌时，牌里有一种花色的牌，依次递增一位或依次递增二位数字的3副顺子。
        ///连六
        public int If_1Se3BuGao_6Lian()
        {
            int cc = 0;
            int[] shun = new int[4];
            int[] tmp = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0 && FanCardData.ArrMshun[i] < 30)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0 && FanCardData.ArrAshun[i] < 30)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 2)
                return 0;

            Array.Sort(shun);
            if (cc == 2)
                return Lian6(shun); //连六  
            else if (cc == 3)
            {
                if (shun[1] / 10 == shun[3] / 10 && shun[1] != shun[3] &&
                    shun[2] - shun[1] == shun[3] - shun[2]) //一色三步高
                {
                    if (shun[2] - shun[1] == 3)
                        fan_table[(int)FanT.PURE_STRAIGHT] = 1;
                    else
                        fan_table[(int)FanT.PURE_SHIFTED_CHOWS] = 1;
                    return 16;
                }
                return Lian6(shun);//连六
            }
            else if (cc == 4)
            {
                if (shun[1] / 10 == shun[3] / 10 && shun[1] != shun[3] &&
                    shun[2] - shun[1] == shun[3] - shun[2] ||
                    shun[0] / 10 == shun[2] / 10 && shun[0] != shun[2] &&
                    shun[1] - shun[0] == shun[2] - shun[1])
                {
                    if (shun[2] - shun[1] == 3 || shun[1] - shun[0] == 3)
                        fan_table[(int)FanT.PURE_STRAIGHT] = 1;
                    else
                        fan_table[(int)FanT.PURE_SHIFTED_CHOWS] = 1;
                    Lian6(shun);//202205
                    return 16;
                }
                return Lian6(shun);//连六
            }
            return 0;
        }
        ////三同刻、三暗刻在后面与双同刻、双暗刻一并实现


        public int If_QuanBuKao(int[] yb)
        {
            int[] HandIn14 = new int[FanCardData.HandIn14.Length];
            int[] HandIn31 = new int[FanCardData.HandIn31.Length];
            FanCardData.HandIn14.CopyTo(HandIn14, 0);
            FanCardData.HandIn31.CopyTo(HandIn31, 0);
            Array.Clear(FanCardData.HandIn14, 0, FanCardData.HandIn14.Length);
            Array.Clear(FanCardData.HandIn31, 0, FanCardData.HandIn31.Length);

            yb.CopyTo(FanCardData.HandIn14, 0);
            for (int i = 0; i < yb.Length; i++)
                FanCardData.HandIn31[yb[i]]++;
            FanCardData.HandIn31[0] = 0;
            int ret = If_QuanBuKao();

            HandIn14.CopyTo(FanCardData.HandIn14, 0);
            HandIn31.CopyTo(FanCardData.HandIn31, 0);
            return ret;
        }

        //------------------------------------------12分   
        /// 全不靠        
        public int If_QuanBuKao()
        {
            if (FanCardData.HandIn14[0] * FanCardData.HandIn14[13] == 0)
                return 0;

            int cc1 = 0, cc2 = 0;
            for (int i = 31; i < FanCardData.HandIn31.Length; i += 2)
                if (FanCardData.HandIn31[i] > 0)
                    cc1++;
            if (cc1 < 5)//字牌小于5张时，5+9=14
                return 0;
            for (int i = 0; i < FanCardData.HandIn14.Length - 1; i++)//有对子时
                if (FanCardData.HandIn14[i] == FanCardData.HandIn14[i + 1])
                    return 0;

            for (int i = 0; i < FanCardData.HandIn14.Length; i++)
                if (FanCardData.HandIn14[i] < 30)
                    cc2++; ;
            if (cc2 < 7)//序数牌小于7张时，7+7=14
                return 0;

            for (int i = 0; i < 6; i++)
            {
                cc2 = cc1;
                for (int j = 0; j < 9; j++)
                    if (FanCardData.HandIn31[ZuHeCard[i, j]] > 0)
                        cc2++;
                if (cc2 == 14)
                {
                    fan_table[(int)FanT.LESSER_HONORS_AND_KNITTED_TILES] = 1;
                    if (Max147_258_369KeyNum(FanCardData.HandIn14) == 9)
                        fan_table[(int)FanT.KNITTED_STRAIGHT] = 1;
                    return 12;
                }
            }
            return 0;
        }

        public int If_ZuHeLong()
        {
            int[] yb = FanCardData.HandIn14;
            return If_ZuHeLong(yb);
        }

        public int If_ZuHeLong(int[] yb)
        {
            if (yb[3] * yb[4] * yb[5] == 0)
                return 0;
            if (!IfMax147_258_369KeyNumIs9(yb))
                return 0;

            int[] tmp31 = new int[KnownRemainCard.Length];
            int[] t31 = new int[tmp31.Length + 1];
            int[] tmp14 = new int[yb.Length];

            int[] JinKey; int maxJin;
            MaxZHL147_258_369Key(yb, out JinKey, out maxJin);

            //去筋 
            yb.CopyTo(tmp14, 0);
            for (int k = 0; k < tmp14.Length; k++)
            {
                if (tmp14[k] == 0 || tmp14[k] > 30 || k < tmp14.Length - 1 &&
                    tmp14[k] == tmp14[k + 1])
                    continue;
                if (tmp14[k] % 10 % 3 == JinKey[tmp14[k] / 10] % 3)
                    tmp14[k] = 0;
            }

            for (int i = 0; i < yb.Length; i++)
                tmp31[tmp14[i]]++;
            tmp31[0] = 0;
            //存在010时不能胡
            for (int i = tmp31.Length - 3; i > 0; i--)
                if (tmp31[i] == 0 && tmp31[i + 1] == 1 && tmp31[i + 2] == 0)
                    return 0;
            //存在01N0,0N10时不能胡
            for (int i = 0; i < tmp31.Length - 3; i++)
                if (tmp31[i] + tmp31[i + 3] == 0 && tmp31[i + 1] * tmp31[i + 2] > 0)
                    if (tmp31[i + 1] == 1 || tmp31[i + 2] == 1)
                        return 0;

            //认真计算 
            for (int ii = 1; ii < tmp31.Length - 1; ii++)
            {
                int AnKe = 0, AnShun = 0, MinKeNum = 0, MinShunNum = 0;
                if (tmp31[ii] < 2) continue;  //先找到将牌 
                tmp31.CopyTo(t31, 0);
                t31[ii] = t31[ii] - 2;       //去掉将牌  

                for (int k = 1; k < t31.Length - 1; k++)
                {
                    //这里可改为直接跳出
                    if (t31[k] == 0)
                        continue;
                    //前有三张相同牌时, 成一组, 清除掉。
                    if (t31[k] >= 3)
                    {
                        t31[k] = t31[k] - 3; AnKe = k; k--;
                    }
                    //有连续的三张牌时, 组成一组, 清除掉。
                    if (t31[k] > 0 && t31[k + 1] > 0 && t31[k + 2] > 0)
                    {
                        t31[k]--; t31[k + 1]--; t31[k + 2]--; AnShun = k + 1; k--;
                    }
                }
                //判断是否能胡牌 , 是否清空。               
                if (LongOfCardNZ(t31) == 0)
                {
                    fan_table[(int)FanT.KNITTED_STRAIGHT] = 1;  // 组合龙
                    for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                    {
                        if (FanCardData.ArrMshun[i] > 0)
                            MinShunNum++;
                        if (FanCardData.ArrMke[i] > 0)
                            MinKeNum++;
                        if (FanCardData.ArrMgang[i] > 0)
                            MinKeNum++;
                    }
                    FanCardData.Jiang = ii;//1029
                    if (FanCardData.Jiang < 30 && AnShun + MinShunNum > 0)
                        fan_table[(int)FanT.ALL_CHOWS] = 1;     // 雀头是数牌时，为平和  
                    if (MinKeNum + MinShunNum == 0 && FanCardData.winCard > 0)
                    {
                        if (FanCardData.wfSELF_DRAWN == 0)
                            fan_table[(int)FanT.CONCEALED_HAND] = 1;  // // 门前清（暗杠不影响）  
                        else
                            fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 1;  // // 不求人（暗杠不影响）  
                    }
                    return 12;
                }
            }
            return 0;
        }



        // 根据数牌的范围调整——涉及番种：大于五、小于五、全大、全中、全小
        // 全大 胡牌时，牌型由序数牌6 9的顺子、刻子、将牌组成 
        public int If_DaYuWu_XiaoYuWu_QuanDa_QuanZhong_QuanXiao()
        {

            //全大
            bool bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 < 7 || FanCardData.FullCard[i] > 30)
                { bb = false; break; }
            if (bb) { fan_table[(int)FanT.UPPER_TILES] = 1; return 24; }

            //全小
            bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 > 3 || FanCardData.FullCard[i] > 30)
                { bb = false; break; }
            if (bb) { fan_table[(int)FanT.LOWER_TILES] = 1; return 24; }

            //全中
            bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 < 4 || FanCardData.FullCard[i] % 10 > 6 || FanCardData.FullCard[i] > 30)
                { bb = false; break; }
            if (bb) { fan_table[(int)FanT.MIDDLE_TILES] = 1; return 24; }

            //大于五
            bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 <= 5 || FanCardData.FullCard[i] > 30)
                { bb = false; break; }
            if (bb) { fan_table[(int)FanT.UPPER_FOUR] = 1; return 12; }

            //小于五
            bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 >= 5 || FanCardData.FullCard[i] > 30)
                { bb = false; break; }
            if (bb) { fan_table[(int)FanT.LOWER_FOUR] = 1; return 12; }

            return 0;
        }


        ///三风刻  
        public int If_3FenKe()
        {
            int cc = 0;
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 30 && FanCardData.ArrMke[i] < 38)
                    cc++;
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] > 30 && FanCardData.ArrAke[i] < 38)
                    cc++;
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
            {
                if (FanCardData.ArrAgang[i] > 30 && FanCardData.ArrAgang[i] < 38)
                    cc++;
                if (FanCardData.ArrMgang[i] > 30 && FanCardData.ArrMgang[i] < 38)
                    cc++;
            }
            if (cc == 3)
            {
                fan_table[(int)FanT.BIG_THREE_WINDS] = 1;
                return 12;
            }
            return 0;
        }

        //------------------------------------------8分        
        ///花龙   三种花色三个顺子组成1-9
        public int If_HuaLong()
        {
            int cc = 0;
            int[] shun = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 3)
                return 0;

            Array.Sort(shun);
            if (cc == 3)
            {
                if (San3Se_258(shun))
                {
                    fan_table[(int)FanT.MIXED_STRAIGHT] = 1;
                    return 8;
                }
            }
            else if (cc == 4)
            {
                int[] temp1 = new int[4];
                int[] temp2 = new int[4];
                shun.CopyTo(temp1, 0);
                shun.CopyTo(temp2, 0);

                //有重复，分两组
                if (shun[0] / 10 == shun[1] / 10)
                    temp1[0] = temp2[1] = 0;
                else if (shun[1] / 10 == shun[2] / 10)
                    temp1[1] = temp2[2] = 0;
                else if (shun[2] / 10 == shun[3] / 10)
                    temp1[2] = temp2[3] = 0;
                if (San3Se_258(temp1) || San3Se_258(temp2))
                {
                    fan_table[(int)FanT.MIXED_STRAIGHT] = 1;
                    return 8;
                }
            }
            return 0;
        }



        ///三色三同顺 胡牌时，牌里有三种花色的3副相同顺子。
        ///喜相逢 胡牌时，牌里有2种花色的2副相同顺子。
        public int If_23Se23TongShun()
        {
            int cc = 0;
            int[] shun = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrAshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 2)
                return 0;
            Array.Sort(shun);
            if (cc == 2)
            {
                if (shun[2] / 10 != shun[3] / 10 && shun[2] % 10 == shun[3] % 10) //2色2同顺  一般高
                {
                    fan_table[(int)FanT.MIXED_DOUBLE_CHOW] = 1;//1028
                    return 1;
                }
            }
            else if (cc == 3)
            {
                //三色，同顺
                if (San3Se_X_Gao(shun, 0))
                {
                    fan_table[(int)FanT.MIXED_TRIPLE_CHOW] = 1;
                    return 8;
                }
                else
                    return TwoSeTongShun(shun);//2色2同顺
            }
            else if (cc == 4)
            {
                int[] tm1 = new int[4];
                int[] tm2 = new int[4];
                shun.CopyTo(tm1, 0);
                shun.CopyTo(tm2, 0);

                //有重复，分两组
                if (shun[0] / 10 == shun[1] / 10)
                    tm1[0] = tm2[1] = 0;
                else if (shun[1] / 10 == shun[2] / 10)
                    tm1[1] = tm2[2] = 0;
                else if (shun[2] / 10 == shun[3] / 10)
                    tm1[2] = tm2[3] = 0;

                if (San3Se_X_Gao(tm1, 0) || San3Se_X_Gao(tm2, 0))
                {
                    fan_table[(int)FanT.MIXED_TRIPLE_CHOW] = 1;
                    return 8;
                }
                else
                    return TwoSeTongShun(shun);//2色2同顺
            }
            return 0;
        }


        ///三色三节高 胡牌时，牌里有三种花色的3副相邻刻子。

        public int If_3Se3JiGao()
        {
            int cc = 0;
            int[] ke = new int[4];
            int[] tmp = new int[4];
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0 && FanCardData.ArrMke[i] < 30)
                    ke[cc++] = FanCardData.ArrMke[i];
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] > 0 && FanCardData.ArrAke[i] < 30)
                    ke[cc++] = FanCardData.ArrAke[i];
            for (int i = 0; i < FanCardData.ArrMgang.Length; i++)
                if (FanCardData.ArrMgang[i] > 0 && FanCardData.ArrMgang[i] < 30)
                    ke[cc++] = FanCardData.ArrMgang[i];
            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0 && FanCardData.ArrAgang[i] < 30)
                    ke[cc++] = FanCardData.ArrAgang[i];
            if (cc < 3)
                return 0;
            Array.Sort(ke);
            if (cc == 3)
            {
                if (San3Se_X_Gao(ke, 1))
                {
                    fan_table[(int)FanT.MIXED_SHIFTED_PUNGS] = 1;
                    return 8;
                }
            }
            else if (cc == 4)
            {
                int[] temp1 = new int[4];
                int[] temp2 = new int[4];
                ke.CopyTo(temp1, 0);
                ke.CopyTo(temp2, 0);

                if (ke[0] / 10 == ke[1] / 10)
                    temp1[0] = temp2[1] = 0;
                else if (ke[1] / 10 == ke[2] / 10)
                    temp1[1] = temp2[2] = 0;
                else if (ke[2] / 10 == ke[3] / 10)
                    temp1[2] = temp2[3] = 0;

                if (San3Se_X_Gao(temp1, 1) || San3Se_X_Gao(temp2, 1))
                {
                    fan_table[(int)FanT.MIXED_SHIFTED_PUNGS] = 1;
                    return 8;
                }
            }
            return 0;
        }

        // 根据和牌标记调整——涉及番种：和绝张、妙手回春、海底捞月、自摸
        public int If_HuJeuZhang_MiaoShouHC_HaiDeLY_ZiMu()
        {
            if (FanCardData.wf4TH_TILE > 0)
            {
                fan_table[(int)FanT.LAST_TILE] = 1;
                //return 4;
            }
            if (FanCardData.wfWALL_LAST > 0)
            {
                fan_table[FanCardData.wfSELF_DRAWN > 0 ? (int)FanT.LAST_TILE_DRAW : (int)FanT.LAST_TILE_CLAIM] = 1;
                //return 8;
            }
            if (FanCardData.wfABOUT_KONG > 0)
            {
                fan_table[FanCardData.wfSELF_DRAWN > 0 ? (int)FanT.OUT_WITH_REPLACEMENT_TILE : (int)FanT.ROBBING_THE_KONG] = 1;
                return 8;
            }
            if (FanCardData.wfSELF_DRAWN > 0)
            {
                fan_table[(int)FanT.SELF_DRAWN] = 1;
                return 1;
            }
            return 0;
        }

        /// 推不倒       
        public int If_TuiBuDao()
        {
            int[] tbd = new int[] { 2, 4, 5, 6, 8, 9, 21, 22, 23, 24, 25, 28, 29, 43 };
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
            {
                bool bb = false;
                for (int j = 0; j < tbd.Length; j++)
                    if (FanCardData.FullCard[i] == tbd[j])
                    { bb = true; break; }
                if (!bb) return 0;
            }
            fan_table[(int)FanT.REVERSIBLE_TILES] = 1;
            return 8;
        }



        //------------------------------------------6分         
        ///碰碰胡  胡牌时，牌型由4副刻子(或杠)、将牌组成。    
        public int If_PengPengHu()
        {
            int cc = 0;
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0)
                    cc++;
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrAke[i] > 0)
                    cc++;
            for (int i = 0; i < FanCardData.ArrMgang.Length; i++)
                if (FanCardData.ArrMgang[i] > 0)
                    cc++;
            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0)
                    cc++;

            if (cc == 4)
            {
                fan_table[(int)FanT.ALL_PUNGS] = 1;
                return 6;
            }
            return 0;
        }

        // 根据花色调整——涉及番种：无字、缺一门、混一色、清一色、五门齐
        public int If_WuZi_QiuYiMen_HunYiSe_QIYiSe_WuMenQi()
        {
            int[] twt = new int[3];
            int Fen = 0, Jian = 0;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
            {
                if (FanCardData.FullCard[i] > 0 && FanCardData.FullCard[i] < 30)
                    twt[FanCardData.FullCard[i] / 10]++;
                else if (FanCardData.FullCard[i] > 30 && FanCardData.FullCard[i] < 38)
                    Fen++;
                else if (FanCardData.FullCard[i] > 38)
                    Jian++;
            }
            Array.Sort(twt);

            if (twt[0] + twt[1] == 0 && Fen + Jian == 0)
            {
                fan_table[(int)FanT.FULL_FLUSH] = 1; // 清一色
                return 24;
            }
            if (twt[0] + twt[1] == 0 && twt[2] > 0 && Fen + Jian > 0)
            {
                fan_table[(int)FanT.HALF_FLUSH] = 1; // 混一色//1028
                return 6;
            }
            if (twt[0] > 0 && twt[1] > 0 && twt[2] > 0 &&
                Fen > 0 && Jian > 0)
            {
                fan_table[(int)FanT.ALL_TYPES] = 1; //五门齐 
                return 6;
            }
            if (Fen + Jian == 0)
                fan_table[(int)FanT.NO_HONORS] = 1; // 无字  
            if (twt[0] == 0 && twt[1] > 0)
                fan_table[(int)FanT.ONE_VOIDED_SUIT] = 1; // 缺一门 
            return fan_table[(int)FanT.NO_HONORS] + fan_table[(int)FanT.ONE_VOIDED_SUIT];
        }


        ///三色三步高 胡牌时，牌里有三种花色的牌，依次递增一位或依次递增二位数字的3副顺子。

        public int If_3Se3BuGao()
        {
            int cc = 0;
            int[] shun = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 3)
                return 0;
            Array.Sort(shun);
            if (cc == 3)
            {
                if (San3Se_X_Gao(shun, 1))//|| San3Se_X_Gao(shun, 2)
                {
                    fan_table[(int)FanT.MIXED_SHIFTED_CHOWS] = 1;
                    return 6;
                }
            }
            else if (cc == 4)
            {
                int[] temp1 = new int[4];
                int[] temp2 = new int[4];
                shun.CopyTo(temp1, 0);
                shun.CopyTo(temp2, 0);

                if (shun[0] / 10 == shun[1] / 10)
                    temp1[0] = temp2[1] = 0;
                else if (shun[1] / 10 == shun[2] / 10)
                    temp1[1] = temp2[2] = 0;
                else if (shun[2] / 10 == shun[3] / 10)
                    temp1[2] = temp2[3] = 0;

                if (San3Se_X_Gao(temp1, 1) || San3Se_X_Gao(temp2, 1))//|| San3Se_X_Gao(temp1, 2) || San3Se_X_Gao(temp2, 2)//202205
                {
                    fan_table[(int)FanT.MIXED_SHIFTED_CHOWS] = 1;
                    return 6;
                }
            }
            return 0;
        }
        ///一、双暗杠      
        public int If_12AnGang()
        {
            int cc = 0;
            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0)
                    cc++;
            if (cc == 1)
            {
                fan_table[(int)FanT.CONCEALED_KONG] = 1;
                return 2;
            }
            else if (cc == 2)
            {
                fan_table[(int)FanT.TWO_CONCEALED_KONGS] = 1;
                return 6;
            }
            return 0;
        }


        //------------------------------------------4分  

        // 根据牌组特征调整——涉及番种：全带幺、全带五、全双刻
        ///全带幺 胡牌时，每副牌、将牌都有幺或九牌。(胡牌时各组牌除了字牌都必须有一或九的序数牌)。        
        public int If_Quan_DaiYao_DaiWu_ShuangKe()
        {
            if (FanCardData.Jiang == 0)
                return 0;
            int cc = 0;
            int[] shun = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0)
                    shun[cc++] = FanCardData.ArrAshun[i];
            cc = 0;
            int[] ke = new int[4];
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0)
                    ke[cc++] = FanCardData.ArrMke[i];
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] > 0)
                    ke[cc++] = FanCardData.ArrAke[i];

            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0)
                    ke[cc++] = FanCardData.ArrAgang[i];
            for (int i = 0; i < FanCardData.ArrMgang.Length; i++)
                if (FanCardData.ArrMgang[i] > 0)
                    ke[cc++] = FanCardData.ArrMgang[i];

            bool bb = true;
            if (LongOfCardNZ(ke) + LongOfCardNZ(shun) == 4 &&
                (FanCardData.Jiang < 30 && FanCardData.Jiang % 10 % 8 == 1 || FanCardData.Jiang > 30))
            {
                bb = true;
                for (int i = 0; i < ke.Length; i++)
                {
                    if (ke[i] > 0 && ke[i] < 30 && ke[i] % 10 % 8 != 1)
                        bb = false;
                    if (shun[i] > 0 && shun[i] < 30 && shun[i] % 10 % 6 != 2)
                        bb = false;
                }
                if (bb)
                {
                    fan_table[(int)FanT.OUTSIDE_HAND] = 1;
                    return 4;
                }//全带幺

            }
            else if (FanCardData.Jiang < 30 && FanCardData.Jiang % 10 == 5)
            {
                bb = true;
                for (int i = 0; i < ke.Length; i++)
                {
                    if (ke[i] > 0 && ke[i] < 30 && ke[i] % 10 != 5)
                        bb = false;
                    if (ke[i] > 30)
                        bb = false;
                    if (shun[i] > 0 && shun[i] < 30 &&
                        (shun[i] % 10 < 4 || shun[i] % 10 > 6))
                        bb = false;
                }
                if (bb)
                {
                    fan_table[(int)FanT.ALL_FIVE] = 1;
                    return 16;
                }//全带5

            }
            else if (FanCardData.Jiang < 30 && FanCardData.Jiang % 2 == 0)
                if (LongOfCardNZ(ke) == 4)
                {
                    bb = true;
                    for (int i = 0; i < ke.Length; i++)
                        if (ke[i] > 0 && ke[i] % 2 != 0)
                            bb = false;
                    if (bb)
                    {
                        fan_table[(int)FanT.ALL_EVEN_PUNGS] = 1;
                        return 24;
                    }//全双刻 
                }
            return 0;
        }

        ///全求人   胡牌时，全靠吃牌、碰牌、单钓别人打出的牌胡牌。不记番:单钓。
        ///不求人;  胡牌时，4副牌及将中没有吃牌、碰牌(包括明杠)，自摸胡牌。
        /// 门前清  胡牌时，4副牌及将中没有吃牌、碰牌(包括明杠)，点胡
        public int If_BuQiuRen_QuanQiuRen_MenQianQin()
        {
            //全求人//202205
            if (LongOfCardNZ(FanCardData.ArrMgang) +
                LongOfCardNZ(FanCardData.ArrMke) +
                LongOfCardNZ(FanCardData.ArrMshun) == 4)
                if (FanCardData.wfDISCARD > 0 && LongOfCardNZ(FanCardData.HandIn14) == 2)
                {
                    fan_table[(int)FanT.MELDED_HAND] = 1;
                    return 6;
                }
            //20201027
            //不求人
            if (LongOfCardNZ(FanCardData.ArrMgang) +
                LongOfCardNZ(FanCardData.ArrMke) +
                LongOfCardNZ(FanCardData.ArrMshun) == 0)
                if (FanCardData.wfSELF_DRAWN > 0 && FanCardData.winCard > 0)
                {
                    fan_table[(int)FanT.FULLY_CONCEALED_HAND] = 1;
                    return 4;
                }
            // 门前清
            if (LongOfCardNZ(FanCardData.ArrMgang) +//1029
            LongOfCardNZ(FanCardData.ArrMshun) +
            LongOfCardNZ(FanCardData.ArrMke) == 0)
                if (FanCardData.wfDISCARD > 0 && FanCardData.winCard > 0)
                {
                    fan_table[(int)FanT.CONCEALED_HAND] = 1;
                    return 2;
                }
            //if (LongOfCardNZ(FanCardData.ArrMgang) +//20240430
            //LongOfCardNZ(FanCardData.ArrMshun) +
            //LongOfCardNZ(FanCardData.ArrMke) == 0)
            //    if ( FanCardData.wf4TH_TILE + FanCardData.wfDISCARD + FanCardData.wfSELF_DRAWN == -3)
            //    {
            //        fan_table[(int)FanT.CONCEALED_HAND] = 1;
            //        return 2;
            //    }
            return 0;
        }


        ///一、双明杠 胡牌时，牌里有2个明杠。不记番:明杠。        
        public int If_12MingGang()
        {
            int cc = 0;
            for (int i = 0; i < FanCardData.ArrMgang.Length; i++)
                if (FanCardData.ArrMgang[i] > 0)
                    cc++;
            if (cc == 1)
            {
                fan_table[(int)FanT.MELDED_KONG] = 1;
                return 1;
            }
            else if (cc == 2)
            {
                fan_table[(int)FanT.TWO_MELDED_KONGS] = 1;
                return 4;
            }
            return 0;
        }

        /// 胡绝张 胡牌时，胡牌池、桌面已亮明的3张牌所剩的第4张牌。       
        public int If_HuJueZhang()
        {
            return 0;
        }



        //------------------------------------------2分 
        /// 箭刻, 双箭刻
        /// 胡牌时，牌里有中、发、白，这3个牌中的任一个牌组成的1副刻子
        public int If_12JianKe()
        {
            int cc = 0;
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] >= 39)
                    cc++;
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] >= 39)
                    cc++;
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAgang[i] >= 39)
                    cc++;
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrMgang[i] >= 39)
                    cc++;
            if (cc == 1)
            {
                fan_table[(int)FanT.DRAGON_PUNG] = 1;
                return 2;
            }
            else if (cc == 2)
            {
                fan_table[(int)FanT.TWO_DRAGONS_PUNGS] = 1;
                return 6;
            }
            return 0;
        }

        //门风刻  胡牌时牌里有与门风相同的风刻
        public int If_MenFengKe()
        {
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0 && FanCardData.ArrMke[i] == (int)FanCardData.seat_wind ||
                    FanCardData.ArrAke[i] > 0 && FanCardData.ArrAke[i] == (int)FanCardData.seat_wind ||
                    FanCardData.ArrMgang[i] > 0 && FanCardData.ArrMgang[i] == (int)FanCardData.seat_wind ||
                    FanCardData.ArrAgang[i] > 0 && FanCardData.ArrAgang[i] == (int)FanCardData.seat_wind)
                {
                    fan_table[(int)FanT.SEAT_WIND] = 1;
                    return 2;
                }

            return 0;
        }

        //圈风刻  胡牌时牌里有与圈风相同的风刻
        public int If_QuanFengKe()
        {
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0 && FanCardData.ArrMke[i] == (int)FanCardData.quan_wind ||
                    FanCardData.ArrAke[i] > 0 && FanCardData.ArrAke[i] == (int)FanCardData.quan_wind ||
                    FanCardData.ArrMgang[i] > 0 && FanCardData.ArrMgang[i] == (int)FanCardData.quan_wind ||
                    FanCardData.ArrAgang[i] > 0 && FanCardData.ArrAgang[i] == (int)FanCardData.quan_wind)
                {
                    fan_table[(int)FanT.PREVALENT_WIND] = 1;
                    return 2;
                }
            return 0;
        }



        //根据雀头调整——涉及番种：平和、小三元、小四喜
        //平胡  胡牌时，牌型由4副顺子及序数牌作将组成。边、坎、钓不影响平和。
        public int If_PingHu_XiaoSiXi_XiaoSanYuan()
        {
            if (FanCardData.Jiang > 30)
            {
                int Fenke = 0, Jianke = 0;
                for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                {
                    if (FanCardData.ArrAke[i] > 30 && FanCardData.ArrAke[i] < 38)
                        Fenke++;
                    else if (FanCardData.ArrAke[i] > 38)
                        Jianke++;

                    if (FanCardData.ArrMke[i] > 30 && FanCardData.ArrMke[i] < 38)
                        Fenke++;
                    else if (FanCardData.ArrMke[i] > 38)
                        Jianke++;

                    if (FanCardData.ArrMgang[i] > 30 && FanCardData.ArrMgang[i] < 38)
                        Fenke++;
                    else if (FanCardData.ArrMgang[i] > 38)
                        Jianke++;

                    if (FanCardData.ArrAgang[i] > 30 && FanCardData.ArrAgang[i] < 38)
                        Fenke++;
                    else if (FanCardData.ArrAgang[i] > 38)
                        Jianke++;
                }
                if (FanCardData.Jiang < 38 && Fenke == 3)
                {
                    fan_table[(int)FanT.LITTLE_FOUR_WINDS] = 1;
                    return 64;
                }  //小四喜
                else if (FanCardData.Jiang > 38 && Jianke == 2)
                {
                    fan_table[(int)FanT.LITTLE_THREE_DRAGONS] = 1;
                    return 64;
                }   //小三元
            }

            int cc = 0;
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0)
                    cc++;
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0)
                    cc++;
            if (cc == 4 && FanCardData.Jiang < 30 && FanCardData.Jiang > 0)//平胡//1028
            {
                fan_table[(int)FanT.ALL_CHOWS] = 1;
                return 2;
            }
            return 0;//平胡
        }

        //四归一调整。有四张一样的牌
        public int If_SiGuiYi()
        {
            int[] tmp31 = new int[FanCardData.HandIn31.Length];
            int cc = 0;
            FanCardData.HandIn31.CopyTo(tmp31, 0);
            for (int i = 0; i < 4; i++)
                if (FanCardData.ArrMke[i] > 0)
                    tmp31[FanCardData.ArrMke[i]] += 3;

            for (int i = 0; i < 4; i++)
                if (FanCardData.ArrMshun[i] > 0)
                {
                    tmp31[FanCardData.ArrMshun[i] - 1]++;
                    tmp31[FanCardData.ArrMshun[i]]++;
                    tmp31[FanCardData.ArrMshun[i] + 1]++;
                }

            for (int i = 0; i < tmp31.Length; i++)
                if (tmp31[i] == 4)
                    cc++;
            fan_table[(int)FanT.TILE_HOG] = (ushort)(cc);
            return cc * 2;
        }

        //幺九、 双、三同刻  
        public int If_23TongKe_19Ke()
        {
            int[] ke1 = new int[4];
            ushort cc = 0;
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0 && FanCardData.ArrMke[i] < 30)
                    ke1[cc++] = FanCardData.ArrMke[i] % 10;
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] > 0 && FanCardData.ArrAke[i] < 30)
                    ke1[cc++] = FanCardData.ArrAke[i] % 10;
            for (int i = 0; i < FanCardData.ArrMgang.Length; i++)
                if (FanCardData.ArrMgang[i] > 0 && FanCardData.ArrMgang[i] < 30)
                    ke1[cc++] = FanCardData.ArrMgang[i] % 10;
            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0 && FanCardData.ArrAgang[i] < 30)
                    ke1[cc++] = FanCardData.ArrAgang[i] % 10;
            Array.Sort(ke1);
            cc = 0;
            if (ke1[0] > 0 && ke1[0] == ke1[2] || ke1[1] > 0 && ke1[1] == ke1[3])
                fan_table[(int)FanT.TRIPLE_PUNG] = 1;
            else
                for (int i = 1; i < ke1.Length; i++)
                    if (ke1[i] > 0 && ke1[i] == ke1[i - 1])
                        cc++;
            fan_table[(int)FanT.DOUBLE_PUNG] = cc;

            cc = 0;
            for (int i = 0; i < FanCardData.ArrMke.Length; i++)
                if (FanCardData.ArrMke[i] > 0 &&
                    (FanCardData.ArrMke[i] > 30 || FanCardData.ArrMke[i] % 10 % 8 == 1))
                    cc++;
            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] > 0 &&
                    (FanCardData.ArrAke[i] > 30 || FanCardData.ArrAke[i] % 10 % 8 == 1))
                    cc++;
            for (int i = 0; i < FanCardData.ArrMgang.Length; i++)
                if (FanCardData.ArrMgang[i] > 0 &&
                    (FanCardData.ArrMgang[i] > 30 || FanCardData.ArrMgang[i] % 10 % 8 == 1))
                    cc++;
            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0 &&
                    (FanCardData.ArrAgang[i] > 30 || FanCardData.ArrAgang[i] % 10 % 8 == 1))
                    cc++;
            fan_table[(int)FanT.PUNG_OF_TERMINALS_OR_HONORS] = (ushort)cc;
            return cc + fan_table[(int)FanT.TRIPLE_PUNG] * 16 + fan_table[(int)FanT.DOUBLE_PUNG] * 2;
        }

        //双、三、四暗刻  
        public int If_234AnKe()
        {
            int cc = 0;//1029
            //硬调整 
            //if (FanCardData.ArrAshun[0] > 0 &&
            //    FanCardData.ArrAshun[0] == FanCardData.ArrAshun[1] &&
            //    FanCardData.ArrAshun[1] == FanCardData.ArrAshun[2] &&
            //    FanCardData.ArrAshun[2] == FanCardData.ArrAshun[3])
            //    cc = 4;
            //if (FanCardData.ArrAshun[0] > 0 && FanCardData.ArrAshun[0] == FanCardData.ArrAshun[2] ||
            //    FanCardData.ArrAshun[1] > 0 && FanCardData.ArrAshun[1] == FanCardData.ArrAshun[3])
            //    cc = 3;

            for (int i = 0; i < FanCardData.ArrAke.Length; i++)
                if (FanCardData.ArrAke[i] > 0 && (FanCardData.ArrAke[i] != FanCardData.winCard || FanCardData.wfDISCARD == 0 || CountCardfArray(FanCardData.HandIn14, FanCardData.winCard) == 4))
                    cc++;
            for (int i = 0; i < FanCardData.ArrAgang.Length; i++)
                if (FanCardData.ArrAgang[i] > 0)
                    cc++;
            if (cc == 2)
            {
                fan_table[(int)FanT.TWO_CONCEALED_PUNGS] = 1;
                return 2;
            }
            else if (cc == 3)
            {
                fan_table[(int)FanT.THREE_CONCEALED_PUNGS] = 1;
                return 16;
            }
            else if (cc == 4)
            {
                fan_table[(int)FanT.FOUR_CONCEALED_PUNGS] = 1;
                return 64;
            }
            return 0;
        }


        /// 断幺        
        public int If_DuanYao()
        {
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 % 8 == 1 || FanCardData.FullCard[i] > 30)
                    return 0;
            fan_table[(int)FanT.ALL_SIMPLES] = 1;
            return 2;
        }

        //------------------------------------------1分 

        // 根据牌特性调整——涉及番种：断幺、推不倒、绿一色、字一色、清幺九、混幺九
        public void adjust_by_tiles_traits()
        {
            //断幺
            bool bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] > 30 || FanCardData.FullCard[i] % 10 % 8 == 1)
                { bb = false; break; }
            if (bb) fan_table[(int)FanT.ALL_SIMPLES] = 1;

            //推不倒
            int[] tbd = new int[] { 2, 4, 5, 6, 8, 9, 21, 22, 23, 24, 25, 28, 29, 43 };
            bool iTong = false;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
            {
                iTong = false;
                for (int j = 0; j < tbd.Length; j++)
                    if (FanCardData.FullCard[i] == tbd[j])
                    {
                        iTong = true;
                        break;
                    }
                if (!iTong)
                    break;
            }
            if (iTong) fan_table[(int)FanT.REVERSIBLE_TILES] = 1;

            //绿一色
            int[] ALL_GREEN = new int[] { 2, 3, 4, 6, 8, 41 };
            iTong = false;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
            {
                iTong = false;
                for (int j = 0; j < ALL_GREEN.Length; j++)
                    if (FanCardData.FullCard[i] == ALL_GREEN[j])
                    {
                        iTong = true;
                        break;
                    }
                if (!iTong)
                    break;
            }
            if (iTong) fan_table[(int)FanT.ALL_GREEN] = 1;

            // 如果断幺了就没必要检测字一色、清幺九、混幺九了
            if (fan_table[(int)FanT.ALL_SIMPLES] != 0)
                return;


            //字一色
            bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] < 30)
                { bb = false; break; }
            if (bb) fan_table[(int)FanT.ALL_HONORS] = 1;

            //清幺九
            bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] % 10 % 8 != 1)
                { bb = false; break; }
            if (bb) fan_table[(int)FanT.ALL_TERMINALS] = 1;

            //混幺九   胡牌时，由字牌和序数牌幺九的刻子及将牌组成的牌型。不记番:碰碰和、幺九刻、全带么。  
            bb = true;
            for (int i = 0; i < FanCardData.FullCard.Length; i++)
                if (FanCardData.FullCard[i] < 30 && FanCardData.FullCard[i] % 10 % 8 != 1)
                { bb = false; break; }
            if (bb) fan_table[(int)FanT.ALL_TERMINALS_AND_HONORS] = 1;
        }


        //------------------------------------------1分 
        ///一般高  胡牌时，牌里有一种花色且序数相同的2副顺子。 If_1Se234TongShun
        ///喜相逢 胡牌时，牌里有2种花色的2副相同顺子。 If_3Se3TongShun() 
        ///连六 If_1Se3BuGao_6Lian
        ///幺九刻 If_23TongKe_19Ke        
        ///明杠 If_12MingGang
        ///缺一门 If_HunYiSe_QinYiSe_Que1Men_WuZi 
        ///无字   If_HunYiSe_QinYiSe_Que1Men_WuZi



        ///老少副 胡牌时，牌里花色相同的123、789的顺子各一副。
        public int If_LaoShaoFu()
        {
            int cc = 0;
            int[] shun = new int[4];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrMshun[i] > 0)
                    shun[cc++] = FanCardData.ArrMshun[i];
            for (int i = 0; i < FanCardData.ArrMshun.Length; i++)
                if (FanCardData.ArrAshun[i] > 0)
                    shun[cc++] = FanCardData.ArrAshun[i];
            if (cc < 2)
                return 0;
            for (int i = 0; i < shun.Length; i++)
                if (shun[i] % 10 % 6 != 2)
                    shun[i] = 0;

            Array.Sort(shun);
            cc = LongOfCardNZ(shun);
            if (cc == 2)
            {
                if (shun[2] / 10 == shun[3] / 10 && shun[2] % 10 == 2 && shun[3] % 10 == 8)
                {
                    fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 1;
                    return 1;
                }
            }
            else if (cc == 3)
            {
                if (shun[1] / 10 == shun[2] / 10 && shun[1] % 10 == 2 && shun[2] % 10 == 8 ||
                    shun[2] / 10 == shun[3] / 10 && shun[2] % 10 == 2 && shun[3] % 10 == 8)
                {
                    fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = 1;
                    return 1;
                }
            }
            else if (cc == 4)
            {
                ushort count = 0;
                if (shun[0] / 10 == shun[1] / 10 && shun[0] % 10 == 2 && shun[1] % 10 == 8)
                    count++;
                if (shun[1] / 10 == shun[2] / 10 && shun[1] % 10 == 2 && shun[2] % 10 == 8)
                    count++;
                if (shun[2] / 10 == shun[3] / 10 && shun[2] % 10 == 2 && shun[3] % 10 == 8)
                    count++;
                fan_table[(int)FanT.TWO_TERMINAL_CHOWS] = count;
                return count;
            }
            return 0;
        }

        public int IfExitKanZhang(int[] inlist)
        {
            for (int k = 0; k < inlist.Length; k++)
            {
                FanCardData.winCard = inlist[k];
                int ret = If_KanZhang();
                FanCardData.winCard = -1;
                if (ret > 0)
                    return ret;
            }
            return 0;
        }

        /// 嵌张      嵌张   202205
        public int If_KanZhang()
        {
            int card = Mahjong.in_card + Mahjong.out_card;
            if (card > 0 && fan_table[(int)FanT.KNITTED_STRAIGHT] > 0)
                if (FanCardData.HandIn31[card - 1] > 0 && FanCardData.HandIn31[card] > 1 && FanCardData.HandIn31[card + 1] > 0)
                    return 0;
            for (int i = 0; i < FanCardData.ArrAshun.Length; i++)
                if (FanCardData.ArrAshun[i] == FanCardData.winCard)
                    if (HowManyJiao(FanCardData.HandIn14, FanCardData.winCard) == 1 || If_Hu_SpecialCards())
                    {
                        fan_table[(int)FanT.CLOSED_WAIT] = 1;
                        return 1;
                    }
            return 0;
        }

        public int IfExitBanZhang(int[] inlist)
        {
            for (int k = 0; k < inlist.Length; k++)
            {
                FanCardData.winCard = inlist[k];
                int ret = If_BanZhang();
                FanCardData.winCard = -1;
                if (ret > 0)
                    return ret;
            }
            return 0;
        }
        /// 边张        
        public int If_BanZhang()//1029
        {
            int card = Mahjong.in_card + Mahjong.out_card;
            if (card > 0 && fan_table[(int)FanT.KNITTED_STRAIGHT] > 0)
            {
                if (card < 30 && card % 10 == 3)
                    if (FanCardData.HandIn31[card - 2] > 0 && FanCardData.HandIn31[card - 1] > 0 && FanCardData.HandIn31[card] > 1)
                        return 0;
                if (card < 30 && card % 10 == 7)
                    if (FanCardData.HandIn31[card + 2] > 0 && FanCardData.HandIn31[card + 1] > 0 && FanCardData.HandIn31[card] > 1)
                        return 0;
            }
            for (int i = 1; i < FanCardData.HandIn31.Length - 1; i++)
                if (i < 30 && i == FanCardData.winCard && (i % 10 == 3 || i % 10 == 7) && i != FanCardData.Jiang)//1029
                {
                    for (int j = 0; j < 4; j++)
                        if (FanCardData.ArrAshun[j] == i + (i % 10 < 5 ? -1 : 1))
                        {
                            if (HowManyJiao(FanCardData.HandIn14, FanCardData.winCard) == 1 || If_Hu_SpecialCards())
                            {
                                fan_table[(int)FanT.EDGE_WAIT] = 1;
                                return 1;
                            }
                        }
                }
            return 0;
        }

        public int IfExitDanDiao(int[] inlist)
        {
            for (int k = 0; k < inlist.Length; k++)
            {
                FanCardData.winCard = inlist[k];
                int ret = If_DanDiao();
                FanCardData.winCard = -1;
                if (ret > 0)
                    return ret;
            }
            return 0;
        }
        /// 单钓将        
        public int If_DanDiao()
        {//1029
            if (FanCardData.Jiang == FanCardData.winCard && fan_table[(int)FanT.KNITTED_STRAIGHT] == 0)
                if (HowManyJiao(FanCardData.HandIn14, FanCardData.winCard) == 1)
                {
                    fan_table[(int)FanT.SINGLE_WAIT] = 1;
                    return 1;
                }
            if (fan_table[(int)FanT.KNITTED_STRAIGHT] > 0)
            {
                //去筋 
                int[] tmp14 = new int[FanCardData.HandIn14.Length];
                FanCardData.HandIn14.CopyTo(tmp14, 0);
                int maxJin = 0;
                int[] JinKey;
                MaxZHL147_258_369Key(tmp14, out JinKey, out maxJin);
                for (int k = 0; k < tmp14.Length; k++)
                {
                    if (tmp14[k] == 0 || tmp14[k] > 30 || k < tmp14.Length - 1 &&
                        tmp14[k] == tmp14[k + 1])
                        continue;
                    if (tmp14[k] % 10 % 3 == JinKey[tmp14[k] / 10] % 3)
                        tmp14[k] = 0;
                }
                int[][] JangShunKes = DivideToJiangShunKe(tmp14);
                int[] JSK = JangShunKes[0];
                for (int j = 0; j < JSK.Length; j++)
                    if (JSK[j] > 0 && JSK[j] < 100)
                        FanCardData.Jiang = JSK[j];
                if (FanCardData.Jiang == FanCardData.winCard)
                    if (HowManyJiao(tmp14, FanCardData.winCard) == 1)
                    {
                        fan_table[(int)FanT.SINGLE_WAIT] = 1;
                        return 1;
                    }
            }
            return 0;
        }

        //----------------------------------------------------------
        //只有三张牌, 三种牌，相差数列0，1，2，3
        ///可以判定 三色同，三色差一，三色差二，三色一龙
        public static bool San3Se_X_Gao(int[] ke, int X)
        {
            int[] tmp = new int[4];
            //三种花色            
            Array.Sort(ke);
            if (ke[1] / 10 + 1 != ke[2] / 10 || ke[2] / 10 + 1 != ke[3] / 10)
                return false;

            ke.CopyTo(tmp, 0);
            //三节高
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = tmp[i] % 10;
            Array.Sort(tmp);
            if (tmp[2] - tmp[1] != X || tmp[3] - tmp[2] != X)
                return false;
            return true;
        }

        ///可以判定 三色且含258，用于花龙
        ///只有三张牌, 三种牌，数列0，3，12，23
        public static bool San3Se_258(int[] shun)//1029
        {
            //三种花色            
            Array.Sort(shun);
            if (shun[1] / 10 == shun[2] / 10 || shun[2] / 10 == shun[3] / 10)
                return false;

            int[] tmp = new int[4];
            shun.CopyTo(tmp, 0);

            //258
            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = tmp[i] % 10;
            Array.Sort(tmp);
            if (tmp[1] == 2 && tmp[2] == 5 && tmp[3] == 8)
                return true;
            return false;
        }

        ///两色同数，喜相逢 
        ///4  5  6  7  8  9  26 26 27 27 28 28 29 29 ?? 
        ///Fan 8 门前清*1+平和*1+一般高*1+喜相逢*1+连六*1+缺一门*1
        ///7 番=门前清2 平和2 一般高1 连六1 缺一门1
        public int TwoSeTongShun(int[] shun)
        {
            ushort cc = 0;
            int[] sh = new int[shun.Length];

            Array.Sort(shun);
            shun.CopyTo(sh, 0);
            for (int i = 1; i < sh.Length; i++)
                if (sh[i] > 0 && sh[i] == sh[i - 1])
                    sh[i - 1] = 0;
            Array.Sort(sh);
            for (int i = 0; i < sh.Length - 1; i++)
                for (int j = i + 1; j < sh.Length; j++)
                    if (sh[i] > 0 && sh[i] / 10 != sh[j] / 10 && sh[i] % 10 == sh[j] % 10)
                        cc++;
            if (cc > 0)
                fan_table[(int)FanT.MIXED_DOUBLE_CHOW] = cc;
            return cc;
        }

        //连六
        public int Lian6(int[] shun)
        {
            ushort cc = 0;
            int[] sh = new int[shun.Length];

            shun.CopyTo(sh, 0);
            for (int i = 1; i < sh.Length; i++)
                if (sh[i] > 0 && sh[i] == sh[i - 1])
                    sh[i - 1] = 0;
            Array.Sort(sh);
            for (int i = 0; i < sh.Length - 1; i++)
                for (int j = i + 1; j < sh.Length; j++)
                    if (sh[i] > 0 && sh[i] / 10 == sh[j] / 10 && sh[j] - sh[i] == 3)//连六 
                        cc++;
            if (cc > 0 && shun[0] > 0 && shun[0] == shun[1] && shun[2] == shun[3])
                cc = 2;
            if (cc > 0)
                fan_table[(int)FanT.SHORT_STRAIGHT] = cc;
            return cc;
        }
        public static void AarryMinusNum(int[] arr, int card)
        {
            if (card == 0)
                return;
            for (int i = arr.Length - 1; i >= 0; i--)
                if (arr[i] == card)
                {
                    arr[i] = 0;
                    for (int j = i; j < arr.Length - 1; j++)
                    {
                        arr[j] = arr[j + 1];
                        arr[j + 1] = 0;
                    }
                    break;
                }
        }
        public static void AppendNum2Array(int[] arr, int card)
        {
            if (card == 0)
                return;
            for (int j = 0; j < arr.Length; j++)
                if (arr[j] == 0)
                {
                    arr[j] = card;
                    break;
                }
        }
    }
    ///番种 


    public class ErmjFanCardData
    {
        /** 将牌值 */
        public int Jiang = -1;
        /** 刻子数组 只列第一个 索引(包含暗刻 和碰) */
        public int[] ArrMke = new int[4];        /** 暗刻 */
        public int[] ArrAke = new int[4];
        /** 顺子数组 只列第一个 */
        /** 吃牌获得的顺子 */
        public int[] ArrMshun = new int[4];        /** 手牌中的顺子 */
        public int[] ArrAshun = new int[4];
        /** 杠数组 只列第一个 (包含明杠 暗杠) */
        /** 明杠 */
        public int[] ArrMgang = new int[4];        /** 暗杠 */
        public int[] ArrAgang = new int[4];

        /** 所有的牌 */
        public int[] HandIn14 = new int[14]; //手上牌，别人看不到的
        public int[] FullCard = new int[14]; //把手上牌、吃、碰、杠合在一起的牌 
        public int[] HandIn31 = new int[144 / 4 + 9];//手上牌的统计数

        /** 当前胡牌玩家门风 */
        public wind_t seat_wind = wind_t.EAST;          //< 门风
        public wind_t quan_wind = wind_t.EAST;     //< 圈风

        ///胡牌
        public int winCard = -1;

        public int wfDISCARD = 0;   ///< 点和
        public int wfSELF_DRAWN = 0;///< 自摸
        public int wf4TH_TILE = 0;///< 绝张
        public int wfABOUT_KONG = 0;///< 关于杠，复合点和时为枪杠和，复合自摸则为杠上开花
        public int wfWALL_LAST = 0;///< 牌墙最后一张，复合点和时为海底捞月，复合自摸则为妙手回春 
    }
    enum FanT
    {
        HuClass = (int)0,    //第一、二分数，排序用
        Score,                  //分数
        Frequency,              //频率
        InOutLever,             //深度

        YB0, YB1, YB2, YB3, YB4, YB5, YB6,  ///14张手牌
        YB7, YB8, YB9, YB10, YB11, YB12, YB13,

        BIG_FOUR_WINDS,                     ///< 大四喜17
        BIG_THREE_DRAGONS,                  ///< 大三元
        ALL_GREEN,                          ///< 绿一色
        NINE_GATES,                         ///< 九莲宝灯
        FOUR_KONGS,                         ///< 四杠
        SEVEN_SHIFTED_PAIRS,                ///< 连七对
        THIRTEEN_ORPHANS,                   ///< 十三幺

        ALL_TERMINALS,                      ///< 清幺九
        LITTLE_FOUR_WINDS,                  ///< 小四喜
        LITTLE_THREE_DRAGONS,               ///< 小三元
        ALL_HONORS,                         ///< 字一色
        FOUR_CONCEALED_PUNGS,               ///< 四暗刻
        PURE_TERMINAL_CHOWS,                ///< 一色双龙会

        QUADRUPLE_CHOW,                     ///< 一色四同顺
        FOUR_PURE_SHIFTED_PUNGS,            ///< 一色四节高

        FOUR_PURE_SHIFTED_CHOWS,            ///< 一色四步高
        THREE_KONGS,                        ///< 三杠
        ALL_TERMINALS_AND_HONORS,           ///< 混幺九

        SEVEN_PAIRS,                        ///< 七对
        GREATER_HONORS_AND_KNITTED_TILES,   ///< 七星不靠
        ALL_EVEN_PUNGS,                     ///< 全双刻
        FULL_FLUSH,                         ///< 清一色
        PURE_TRIPLE_CHOW,                   ///< 一色三同顺
        PURE_SHIFTED_PUNGS,                 ///< 一色三节高
        UPPER_TILES,                        ///< 全大
        MIDDLE_TILES,                       ///< 全中
        LOWER_TILES,                        ///< 全小

        PURE_STRAIGHT,                      ///< 清龙
        THREE_SUITED_TERMINAL_CHOWS,        ///< 三色双龙会
        PURE_SHIFTED_CHOWS,                 ///< 一色三步高
        ALL_FIVE,                           ///< 全带五
        TRIPLE_PUNG,                        ///< 三同刻
        THREE_CONCEALED_PUNGS,              ///< 三暗刻

        LESSER_HONORS_AND_KNITTED_TILES,    ///< 全不靠
        KNITTED_STRAIGHT,                   ///< 组合龙
        UPPER_FOUR,                         ///< 大于五
        LOWER_FOUR,                         ///< 小于五
        BIG_THREE_WINDS,                    ///< 三风刻

        MIXED_STRAIGHT,                     ///< 花龙
        REVERSIBLE_TILES,                   ///< 推不倒
        MIXED_TRIPLE_CHOW,                  ///< 三色三同顺
        MIXED_SHIFTED_PUNGS,                ///< 三色三节高
        CHICKEN_HAND,                       ///< 无番和
        LAST_TILE_DRAW,                     ///< 妙手回春
        LAST_TILE_CLAIM,                    ///< 海底捞月
        OUT_WITH_REPLACEMENT_TILE,          ///< 杠上开花
        ROBBING_THE_KONG,                   ///< 抢杠和

        ALL_PUNGS,                          ///< 碰碰和
        HALF_FLUSH,                         ///< 混一色
        MIXED_SHIFTED_CHOWS,                ///< 三色三步高
        ALL_TYPES,                          ///< 五门齐
        MELDED_HAND,                        ///< 全求人
        TWO_CONCEALED_KONGS,                ///< 双暗杠
        TWO_DRAGONS_PUNGS,                  ///< 双箭刻

        OUTSIDE_HAND,                       ///< 全带幺
        FULLY_CONCEALED_HAND,               ///< 不求人
        TWO_MELDED_KONGS,                   ///< 双明杠
        LAST_TILE,                          ///< 和绝张

        DRAGON_PUNG,                        ///< 箭刻
        PREVALENT_WIND,                     ///< 圈风刻
        SEAT_WIND,                          ///< 门风刻
        CONCEALED_HAND,                     ///< 门前清
        ALL_CHOWS,                          ///< 平和
        TILE_HOG,                           ///< 四归一
        DOUBLE_PUNG,                        ///< 双同刻
        TWO_CONCEALED_PUNGS,                ///< 双暗刻
        CONCEALED_KONG,                     ///< 暗杠
        ALL_SIMPLES,                        ///< 断幺

        PURE_DOUBLE_CHOW,                   ///< 一般高
        MIXED_DOUBLE_CHOW,                  ///< 喜相逢
        SHORT_STRAIGHT,                     ///< 连六
        TWO_TERMINAL_CHOWS,                 ///< 老少副
        PUNG_OF_TERMINALS_OR_HONORS,        ///< 幺九刻
        MELDED_KONG,                        ///< 明杠
        ONE_VOIDED_SUIT,                    ///< 缺一门
        NO_HONORS,                          ///< 无字
        EDGE_WAIT,                          ///< 边张
        CLOSED_WAIT,                        ///< 嵌张
        SINGLE_WAIT,                        ///< 单钓将
        SELF_DRAWN,                         ///< 自摸

        FLOWER_TILES,                       ///< 花牌

#if SUPPORT_CONCEALED_KONG_AND_MELDED_KONG
        CONCEALED_KONG_AND_MELDED_KONG,     ///< 明暗杠
#endif
        FAN_TABLE_SIZE
    };

    public enum wind_t
    {
        EAST = 31, SOUTH = 33, WEST = 35, NORTH = 37
    };
}