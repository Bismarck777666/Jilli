using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliSpinClasses
{
    public class FgpSpinClass
    {
        public List<ColumnFGP> PlateSymbol { get; set; }
        public List<AwardDataFGP> AwardDataVec { get; set; }
        public int AwardTypeFlag { get; set; }
        public double LineMult { get; set; }
        public double LineWin { get; set; }
        public double WheelOdd { get; set; }
        public double WheelMult { get; set; }
        public double WheelWin { get; set; }
        public double TotalWin { get; set; }
        public List<double> MultShowArray { get; set; }
        public List<int> LightOnPos { get; set; }
        public double Bet { get; set; }
        public bool Extra { get; set; }
        public string ShowIndex { get; set; }
        public double NowMoney { get; set; }
        public int AckType { get; set; }
        public bool IsBonus { get; set; }
    }

    public class ColumnFGP
    {
        public List<int> Col { get; set; }
    }

    public class AwardDataFGP
    {
        public int Symbol { get; set; }
        public int Count { get; set; }
        public double Win { get; set; }
    }
}
