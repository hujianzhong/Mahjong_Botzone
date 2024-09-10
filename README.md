# 通用麻将搜索算法
作者 胡建中
## 【关键字】麻将 搜索 算法 人工智能 C#
### 【摘要】麻将历史渊源流长，在中、日、韩极为流行，玩家有数亿之众，部分人员相当痴迷，不能自拔。麻将是一种信息不完全的四人策略游戏，复杂的游戏规则、巨大的搜索空间、多个对手等是开发麻将AI的主要挑战。作者们巧妙设计了的麻将的数据结构、胡牌算法、牌面评估算法、打牌决策算法、吃牌决策算法、碰牌决策算法、杠牌决策算法，成功解决了麻将的非完全信息决策问题，本算法速度快、计算准、水平高、可解释，且具搜索空间完备性。

2024年6月，北京大学人工智能研究室联合北京微智娱科技有限公司，面向全球举办人工智能对抗比赛，即IJCAI2024麻将人工智能比赛，比赛采用《中国麻将竞赛规则》。本次比赛一共有36支队伍报名，作者派出的“王叶艳嫔”名列24。但其他35支队伍基本全是基于深度学习，而作者的则为搜索算法，具的独创性。当然，因作者仅了解国标麻将规则而不会打牌，不太清楚从实战出发如何改进算法，导致软件的水平较深度学习有一定差距。

![image](https://github.com/user-attachments/assets/66130cf8-da55-4e90-88e9-a51d9867f184)

1.  Program.cs文件中代码   
    static bool bCompitition = true;//-------------------参加botzone比赛   
    static bool bCompitition = false 调试botzone的国标麻将simple.txt   
    CheckJCAIdata("sample.txt", 16, 1118);//------------------------复盘sample.txt数据
    
3. 麻将牌表示  
对于条：用整数1表示1条，2表示2条...,9表示9条。  
对于万：用整数11表示1万，12表示2万...,19表示9万。  
对于筒：用整数21表示1筒，22表示2筒...,29表示9筒。  
对于风牌：用整数31表示东风，33表示南风,35表示西风，37表示北风。  
对于箭牌：用整数39表示中，41表示发,35表示白。  
最后，用整数45表示8张花牌。  
用整数0到45即可表示所有牌，虽然0、10、20、30、32、34、36、38、40、42、44这11个数字没有用到，但对后续计算与调试极为方便。

3. 牌组表示  
软件中，作者用HandInCard数组表示各家手牌。以上牌组，作者用数组{1,4,6,11,15,16,18,18,18,25,27,33,41},{13}表示。  
以上牌组，作者用BumpCard数组表示各家吃、碰、杠后展示的牌，用数组{19,19,19,25,24,26,0,...,0}表示。HandInCard数组为{0, ..., 0,21,21,22,22,27,28,29}  

4. 牌组计数数组  
用KnownRemainCard表示为计数数组，表示自己看不见的牌的数量，数组长度为46，分别对应隐藏牌的张数，数组下标1～9分别表示一至九条的数量，下标11～19分别表示一至九万的数量，下标21～29分别表示一至九筒的数量，31、33、35、37表示东南西北，39、41、41表示中发白。数组下标0、10、20、30、32、34、36、38、40、42、44值为0。如：{0,3,4,2,1,4,0,3...3,2}，  表示外面还有3张一条、4张二条、2张三条、1张四条、4张五条、0张六条、3张七条...3张发财、2张白板。如此数据结构会方便后续程序计算。  
程序中的HandIn31、tmp31等命名的变量，表示手牌的计数。  

5. 吃碰杠胡评估数据结构  
Mahjong.EvalCPGHs的定义：public static double[][][][] GlobeCPGHs = new double[8][][][];  
GlobeCPGHs长度为8。GlobeCPGHs[1]用于记录原牌组估值，GlobeCPGHs[2]用于记录杠牌后的牌组估值，GlobeCPGHs[3]用于记录碰牌后的牌组估值，GlobeCPGHs[4]用于记录第1种吃法牌后的牌组估值，GlobeCPGHs[5]用于记录第2种吃法牌后的牌组估值，GlobeCPGHs[6]用于记录第3种吃法牌后的牌组估值。  
GlobeCPGHs[1]长度为5。GlobeCPGHs[1][0]表示当前牌组基本型之对应数据，GlobeCPGHs[1][1]表示当组合龙，GlobeCPGHs[1][2]表示全不靠，GlobeCPGHs[1][3]表示十三幺，GlobeCPGHs[1][4]表示七对。  
GlobeCPGHs[1][5]长度为7，GlobeCPGHs[1][5][0]表示当前牌组胡牌得分，GlobeCPGHs[1][5][1]表示当前牌组1进1出胡牌得分的牌面价值或牌组估值，GlobeCPGHs[1][5][2]表示当前牌组2进2出胡牌得分的牌面价值或牌组估值，GlobeCPGHs[1][5][n]表示当前牌组n进n出胡牌得分的牌面价值或牌组估值。  
例如：GlobeCPGHs[1][0][3][11]表示：[原牌组][基本型][3进3出][第11张牌]的牌面评分。  

6.  基本胡牌型  
即网上流传的公式，简单易懂：n*AAA + m*ABC + DD，AAA就是三个一样的牌(刻子)，ABC就是顺子，DD就是对子。m、n可以为0。加起来一共14张牌即为和牌，少任何一张则为听牌。  
当n=0时，m=4，除了1个对子外有4个顺子，此时和牌的牌型属于平胡，是较为普通的牌型。  
当m=0时，n=4，除了1个对子外有4个刻子，此时听牌一般听两个对子，摸到其中一个对子的牌，凑足4个刻子，即可和牌。刻子可以是碰来的，也可以是摸来的，如果都是碰来的，就叫“碰碰和”。  
n和m都不为0时，n+m <= 4，除特殊情况外，基本属于平胡状态。完整代码如下：  
```
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
```

7.  基本型牌面估值算法  
    是时候贴出核心代码了，此为n*AAA + m*ABC + DD基本型打牌、牌组评估代码。  
    打牌时，程序返回每张手牌yb的出牌权重数组，值越大越该打出该张牌。  
    牌组评估时，返回的出牌权重数组的最大值越大，表示这组牌形势越好。  
    为了便于理解，以下贴出的均为原程序中的核心主干代码：  
  ```
        ///yb: 长度14、11、8...的牌组时计算打牌的牌面价值，长度13、10、7...的牌组时计算牌组估值
        ///Level: 胡牌深度        
        public double[] HuCardNorm(int[] yb, int Level, string Father = "")
        {
            double count = 0; int lenHu = 0, outLevel = 0, inLevel = 0;
            outLevel = inLevel = Level;
            int lenYb = lenHu = LongOfCardNZ(yb);
            if (lenYb % 3 == 1)//牌组评估时，打出的牌比摸进的牌少1张
            {
                outLevel--;
                lenHu = lenYb + 1;
            }
            int[] InList = GenInListNorm(yb, inLevel, Father);//生成相关进张牌组，以降低复杂度
            int[] OutList = yb;
            int[] tmp = new int[yb.Length];
            int[] HandIn31 = new int[KnownRemainCard.Length];
            for (int i = 0; i < yb.Length; i++)
                HandIn31[yb[i]] += 1;
            HandIn31[0] = 0;
            int[] inCards = new int[inLevel];   //进张牌组
            int[] outCards = new int[outLevel]; //出张牌组
            int[] tmp31 = new int[KnownRemainCard.Length];
            double[] outone_hu_num = new double[14];
            ArrayList comb_In = combinations_replacement(InList, inLevel, true); //从InList数组中取出inLevel个数进行可以重复的组合             
            ArrayList comb_Out = combinations(OutList, outLevel);  //从非0的OutList数组中取出outLevel个数进行组合              
            foreach (int[] ic in comb_In)
            {
                HandIn31.CopyTo(tmp31, 0);
                for (int i = 0; i < inLevel; i++) tmp31[InList[ic[i]]] += 1;//进牌
                if (If_NotNormHu_Cards(tmp31, lenHu))    //快速确定不能胡牌
                    continue;
                if (DecisionTimeOut())      //计算超时退出
                    break;
                for (int i = 0; i < inLevel; i++) inCards[i] = InList[ic[i]];//生成进张牌组
                double comb = 1; //计算进张的组合数
                for (int i = 0; i < inLevel; i++)
                    for (int j = i + 1; j < inLevel + 1; j++)
                        if (j == inLevel || inCards[i] != inCards[j])
                        {
                            comb *= CombNum(KnownRemainCard[inCards[i]], j - i, KnownRemainCard[inCards[i]]);
                            i = j - 1;
                            break;
                        }
                if (comb == 0 && Level > 0) continue; //组合数为0，不能胡牌
                foreach (int[] oc in comb_Out)
                {
                    for (int i = 0; i < outLevel; i++) outCards[i] = OutList[oc[i]]; //生成出张牌组
                    if (inLevel > 1 && IfExitSameItemInTwoArray(inCards, outCards))  //进张与出张牌组存在相同牌
                        continue;
                    HandIn31.CopyTo(tmp31, 0);
                    for (int i = 0; i < outLevel; i++)//打牌
                    {
                        tmp31[outCards[i]]--;
                        tmp31[inCards[i]]++;
                    }
                    if (lenYb % 3 == 1)//牌组评估时
                        tmp31[inCards[ic.Length - 1]]++;
                    if (FastDetermineNoHu31(tmp31))//快速确定不能胡牌
                        continue;
                    yb.CopyTo(tmp, 0);
                    for (int i = 0; i < outLevel; i++)
                        tmp[oc[i]] = InList[ic[i]];//打牌
                    if (lenYb % 3 == 1)//牌组评估时
                        tmp[0] = InList[ic[ic.Length - 1]];
                    Array.Sort(tmp);
                    if (If_NormHu_Cards(tmp) > 0)//能够达成基本胡牌型
                    {
                        Array.Sort(tmp);
                        AdjusHandCard(tmp);
                        AdjustWinFlag(inCards);
                        int sco2 = AdjustHandShunKe_ComputerScore(tmp);//计算胡牌时番数 
                        count = comb * ConvertScore(tmp, sco2, InList, ic, inLevel);
                        if (count == 0) continue; //胡牌番数不够时，不计数 
                        if (outLevel + inLevel == 1)//听牌
                            outone_hu_num[13] += count;
                        for (int i = 0; i < outLevel; i++)//对打出的牌进行计数
                            outone_hu_num[oc[i]] += count;
                        RecordHuFanInfo(tmp, inLevel, 0); //记录胡牌信息
                    }
                }
            }
            return Counts2OutNum(outone_hu_num, inLevel);  //将胡牌计数转换为每张牌的得分 
        }
```
