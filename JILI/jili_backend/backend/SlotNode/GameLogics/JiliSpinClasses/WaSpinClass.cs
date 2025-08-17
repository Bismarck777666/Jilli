using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliSpinClasses
{
    using System.Collections.Generic;

    public class WaSpinClass
    {
        public List<RoundInfo> RoundQueue { get; set; }
        public double FreeTotalWin { get; set; }
        public double TotalWin { get; set; }
        public string ShowIndex { get; set; }
        public double NowMoney { get; set; }
        public int AckType { get; set; }
        public double RTP { get; set; }
    }

    public class RoundInfo
    {
        public List<ComboStageInfo> ComboStageData { get; set; }
        public double RoundWin { get; set; }
        public int AwardTypeFlag { get; set; }
        public int FreeRemainRound { get; set; }
        public int AddRound { get; set; }
        public bool Plus { get; set; }
        public bool MaxFlag { get; set; }
    }

    public class ComboStageInfo
    {
        public List<Column> ComboStageSymbol { get; set; }
        public double ComboStageWin { get; set; }
        public int ComboLevel { get; set; }
        public List<AwardData> AwardDataVec { get; set; }
        public List<Column> FillSymbols { get; set; }
        public List<int> UpgradeVec { get; set; }
        public double Mult { get; set; }
        public bool MaxFlag { get; set; }
    }

    public class Column
    {
        public List<int> Col { get; set; }
    }

    public class AwardData
    {
        public int Symbol { get; set; }
        public int Count { get; set; }
        public int LineNum { get; set; }
        public double Win { get; set; }
        public double Mult { get; set; }
        public List<int> PosVec { get; set; }
    }

}
