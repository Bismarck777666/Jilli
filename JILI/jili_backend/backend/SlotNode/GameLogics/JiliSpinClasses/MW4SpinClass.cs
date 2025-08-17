using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliSpinClasses
{
    public class MW4SpinClass
    {
        public List<RoundInfoMW4> RoundQueue { get; set; }
        public double FreeTotalWin { get; set; }
        public double TotalWin { get; set; }
        public long LogIndex { get; set; }
        public string ShowIndex { get; set; }
        public double NowMoney { get; set; }
        public int AckType { get; set; }
        public double RTP { get; set; }
        public bool Extra { get; set; }
    }

    public class RoundInfoMW4
    {
        public List<ComboStageInfoMW4> ComboStageData { get; set; }
        public double RoundWin { get; set; }
        public int AwardTypeFlag { get; set; }
        public int FreeRemainRound { get; set; }
        public int ScatterCount { get; set; }
        public bool MaxFlag { get; set; }
    }

    public class ComboStageInfoMW4
    {
        public List<MColumnData> ComboStageSymbol { get; set; }
        public double ComboStageWin { get; set; }
        public double ComboStageMult { get; set; }
        public List<AwardDataMW4> AwardDataVec { get; set; }
        public List<MColumnData> FillSymbols { get; set; }
        public List<CSymbol> ChangeSymbols { get; set; }
        public List<GridJR> JokerReplace { get; set; }
        public bool MaxFlag { get; set; }
    }

    public class MColumnData
    {
        public List<MSymbol> MColumn { get; set; }
    }

    public class MSymbol
    {
        public int Symbol { get; set; }
        public int Length { get; set; }
    }

    public class CSymbol
    {
        public int Symbol { get; set; }
        public int Length { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
    }

    public class GridJR
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public Grid Source { get; set; }
    }

    public class Grid
    {
        public int Col { get; set; }
        public int Row { get; set; }
    }

    public class AwardDataMW4
    {
        public int Symbol { get; set; }
        public int Count { get; set; }
        public int LineNum { get; set; }
        public double Win { get; set; }
        public List<GridHit> GridVec { get; set; }
    }

    public class GridHit
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public bool ChangeFlag { get; set; }
    }
}
