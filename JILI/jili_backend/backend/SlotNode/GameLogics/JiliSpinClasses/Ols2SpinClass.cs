using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliSpinClasses
{
    public class Ols2SpinClass
    {
        public int Award { get; set; }
        public List<int> PlateSymbol { get; set; }
        public int RespinTimes { get; set; }
        public double Mult { get; set; }
        public double PlateWin { get; set; }
        public double MultWin { get; set; }
        public double ExtraWin { get; set; }
        public double RespinWin { get; set; }
        public double TotalWin { get; set; }
        public string ShowIndex { get; set; }
        public double NowMoney { get; set; }
        public int AckType { get; set; }
    }
}
