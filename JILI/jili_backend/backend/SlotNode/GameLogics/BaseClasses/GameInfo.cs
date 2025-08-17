using Akka.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.BaseClasses
{
    public class GameInfo
    {
        public List<WalletInfo> WalletInfo { get; set; }
        public PreferRoundShow Prefer { get; set; }
        public int FreeSpinRemain { get; set; }
        public int skipField1 { get; set; }
        public string extraInfo { get; set; }
        public double rtp { get; set; }
        public List<double> jpUnlockBet { get; set; }
        public MallInfo mall { get; set; }
        public int ApiType { get; set; }
        public double FreeSpinBet { get; set; }
        public int skipField2 { get; set; }
        public GameMallInfo gameMall { get; set; }
        public double MaxOdd { get; set; }
        public freeSpinList freeSpin { get; set; }
        public int freeSpinType { get; set; }
        public Tournament tournament { get; set; }
    }

    public class PreferRoundShow
    {
        public double BaseRound { get; set; }
        public double SigmaRound { get; set; }
        public double CV { get; set; }
        public double HR { get; set; }
        public double MGR { get; set; }
        public int Range { get; set; }
        public int Show { get; set; }
    }

    public class MallInfo
    {
        public double PriceOdd { get; set; }
        public int DescType { get; set; }
        public double MaxBet { get; set; }
        public int AlterID { get; set; }
        public int Show { get; set; }
    }

    public class GameMallInfo
    {
        public double MaxBet { get; set; }
        public List<double> PriceOdd { get; set; }
        public List<int> AlterID { get; set; }
    }

    public class Tournament
    {
        public int id { get; set; }
        public double balance { get; set; }
        public double initBalance { get; set; }
        public double MinBet { get; set; }
        public double MaxBet { get; set; }
        public int goal { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public bool buyBonusOn { get; set; }
        public bool extraBetOn { get; set; }
        public int category { get; set; }
    }

    public class WalletInfo
    {
        public int currencyNumber { get; set; }
        public string currencyName { get; set; }
        public string currencySymbol { get; set; }
        public double coin { get; set; }
        public List<double> bet { get; set; }
        public double unit { get; set; }
        public double ratio { get; set; }
        public double rate { get; set; }
        public double upper { get; set; }
        public double lower { get; set; }
        public double cycle { get; set; }

        [JsonProperty("decimal")]
        public int decimal_ { get; set; }
    }

    public class freeSpinList
    {
        public List<freeData> free { get; set; }
    }

    public class freeData
    {
        public string id { get; set; }
        public int remain { get; set; }
        public int type { get; set; }
        public double bet { get; set; }
        public double maxWin { get; set; }
        public double nowWin { get; set; }
        public freeBonus bonusData { get; set; }
        public DateTime expired { get; set; }
        public bool isAllowStop { get; set; }
        public bool closeOnFinish { get; set; }
    }

    public class freeBonus
    {
        public double wager { get; set; }
        public double wagerOdds { get; set; }
        public int period { get; set; }
        public DateTime expired { get; set; }
        public int skipField { get; set; }
        public bool isInWager { get; set; }
        public double maxWin { get; set; }
        public bool isEnd { get; set; }
        public double baseWin { get; set; }
    }
}
