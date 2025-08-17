using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliSpinClasses
{
    public class FullHouseSpinClass
    {
        public AllPlate     AllPlate {  get; set; }
        public int          SpinState  {  get; set; }
        public double       TotalWin { get; set; }
        public ulong        RoundIndex { get; set; }
        public int          MissionReward { get; set; }
        public double       PostMoney { get; set; }
        public double       PreMoney { get; set; }
        public string       Jp { get; set; }
        public string       Lottery { get; set; }
        public double       FullJpWin { get; set; }
        public int          FreeSpinCount { get; set; }
        public string       TrialResp { get; set; }
        public string       CardBookResp { get; set; }

    }

    public class AllPlate
    {
        public List<Plate> Plate { get; set; }
    }
    public class Plate
    {
        public List<ColumnData> Column { get; set; }
        public int SkipField { get; set; }
        public List<Combo> Combo { get; set; }
        public double Win { get; set; }
        public double BonusWin { get; set; }
        public bool IsBonus { get; set; }
        public int TotalFreeSpin { get; set; }
    }

    public class ColumnData
    {
        public List<int> Row { get; set; }
        public List<int> IsGold { get; set; }
    }

    public class Combo
    {
        public List<Cell> Change { get; set; }
        public List<AwardReel> Award { get; set; }
        public double Win { get; set; }
        public int ComboBonus { get; set; }
        public List<Cell> ExtraChange { get; set; }
    }

    public class Cell
    {
        public List<Cell> change { get; set; }
        public List<AwardReel> award { get; set; }
        public double win { get; set; }
        public uint comboBonus { get; set; }
        public List<Cell> extraChange { get; set; }
        public List<Block> goldReplace { get; set; }
        public List<int> bigGoldCount { get; set; }
    }

    public class AwardReel
    {
        public int Symbol { get; set; }
        public List<Block> Block { get; set; }
        public double Win { get; set; }
        public int Bonus { get; set; }
        public int MaxLen { get; set; }
    }

    public class Block
    {
        public int Row { get; set; }
        public int Column { get; set; }
    }

}
