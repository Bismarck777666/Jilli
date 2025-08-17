using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliSpinClasses
{
    public class Fg3SpinClass
    {
        public List<ColumnFG3> PlateSymbol { get; set; }
        public List<AwardDataFG3> AwardDataVec { get; set; }
        public List<SplitInfo> Split { get; set; }
        public int AwardTypeFlag { get; set; }
        public double ReelMult { get; set; }
        public double TotalWin { get; set; }
        public string ShowIndex { get; set; }
        public double NowMoney { get; set; }
        public int AckType { get; set; }
        public bool Extra { get; set; }
        public List<AwardDataFG3> PreAwardDataVec { get; set; }
        public double PreTotalWin { get; set; }
        public double RTP { get; set; }
        public List<ColumnFG3> PlateSymbolLog { get; set; }
        public bool IsBonus { get; set; }
    }

    public class ColumnFG3
    {
        public List<int> Col { get; set; }
    }

    public class AwardDataFG3
    {
        public int Symbol { get; set; }
        public int Line { get; set; }
        public double Win { get; set; }
        public double Mult { get; set; }
    }

    public class SplitInfo
    {
        public int Pos { get; set; }
        public int Level { get; set; }
    }
}
