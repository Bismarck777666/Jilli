using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliSpinClasses
{
    public class PriateSpinClass
    {
        public List<RoundInfoPriate> RoundQueue { get; set; }
        public double FreeTotalWin { get; set; }
        public double TotalWin { get; set; }
        public string ShowIndex { get; set; }
        public double NowMoney { get; set; }
        public int AckType { get; set; }
        public double RTP { get; set; }
        public bool Extra { get; set; }
    }

    public class RoundInfoPriate
    {
        public List<ComboStageInfoPriate> ComboStageData { get; set; }
        public double RoundWin { get; set; }
        public double RoundMult { get; set; }
        public int AwardTypeFlag { get; set; }
        public int FreeRemainRound { get; set; }
        public int TargetSymbol { get; set; }
        public List<int> TargetPosVec { get; set; }
        public int CountRound { get; set; }
    }

    public class ComboStageInfoPriate
    {
        public List<Column> ComboStageSymbol { get; set; }
        public double ComboStageWin { get; set; }
        public List<Column> ComboStageMults { get; set; }
        public List<AwardDataPriate> AwardDataVec { get; set; }
        public List<Column> FillSymbols { get; set; }
        public List<Column> FillMults { get; set; }
    }


    public class AwardDataPriate
    {
        public int Symbol { get; set; }
        public int Count { get; set; }
        public double Win { get; set; }
        public List<int> PosVec { get; set; }
    }
}
