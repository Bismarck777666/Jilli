using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliSpinClasses
{
    public class PSSpinClass
    {
        public List<RoundInfoPS> RoundQueue { get; set; }
        public double FreeTotalWin { get; set; }
        public double TotalWin { get; set; }
        public string ShowIndex { get; set; }
        public double NowMoney { get; set; }
        public int AckType { get; set; }
        public double Bet { get; set; }
        public double RTP { get; set; }
    }

    public class RoundInfoPS
    {
        public List<ComboStageInfoPS> ComboStageData { get; set; }
        public int AwardTypeFlag { get; set; }
        public int FreeRemainRound { get; set; }
        public int FreeNowRound { get; set; }
        public int FreeTotalRound { get; set; }
        public int AddRound { get; set; }
        public double RoundWin { get; set; }
        public List<int> SBGHeight { get; set; }
        public bool MaxFlag { get; set; }
    }

    public class ComboStageInfoPS
    {
        public List<MColumn> ComboStageSymbol { get; set; }
        public double ComboStageWin { get; set; }
        public List<AwardDataPS> AwardDataVec { get; set; }
        public List<MColumn> FillSymbols { get; set; }
        public List<MColumn> ChangeSymbols { get; set; }
        public List<PartyData> Party { get; set; }
        public bool MaxFlag { get; set; }
        public List<MColumn> ComboStageSymbolLog { get; set; }
    }

    public class MColumn
    {
        public List<MSymbolPS> Col { get; set; }
    }

    public class MSymbolPS
    {
        public int Symbol { get; set; }
        public int HSymbol { get; set; }
        public int HStack { get; set; }
    }

    public class PartyData
    {
        public int LR { get; set; }
        public int State { get; set; }
        public bool Worked { get; set; }
        public int StackCol { get; set; }
        public int StackRow { get; set; }
        public int HSymbol { get; set; }
    }

    public class AwardDataPS
    {
        public int Symbol { get; set; }
        public int Count { get; set; }
        public int Line { get; set; }
        public int Direction { get; set; }
        public double Win { get; set; }
    }
}
