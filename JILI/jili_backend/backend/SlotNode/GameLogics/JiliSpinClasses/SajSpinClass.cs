using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics
{

    public class ColData
    {
        public List<int> Col { get; set; }
    }

    public class ComboStageData
    {
        public List<ColData> ComboStageSymbol { get; set; }
        public List<ColData> ComboStageSymbolLog { get; set; }
        public double ComboStageWin { get; set; }
        public List<object> AwardDataVec { get; set; }
        public List<ColData> FillSymbols { get; set; }
        public List<ColData> ChangeSymbols { get; set; }
        public double Mult { get; set; }
        public bool MaxFlag { get; set; }
        public int MysteryResult { get; set; }
    }

    public class RoundQueueData
    {
        public List<ComboStageData> ComboStageData { get; set; }
        public double RoundWin { get; set; }
        public int AwardTypeFlag { get; set; }
        public int FreeRemainRound { get; set; }
        public int FreeNowRound { get; set; }
        public int FreeTotalRound { get; set; }
        public int AddRound { get; set; }
        public bool MaxFlag { get; set; }
    }
    public class SajSpinResultClass
    {
        public List<RoundQueueData> RoundQueue { get; set; }
        public double FreeTotalWin { get; set; }
        public double TotalWin { get; set; }
        public string ShowIndex { get; set; }
        public double NowMoney { get; set; }
        public int AckType { get; set; }
        public double RTP { get; set; }
        public double Bet { get; set; }
    }
}
