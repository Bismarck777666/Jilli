using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace GITProtocol
{

    public class CreateNewUserMessage
    {
        public string SessionToken { get; private set; }
        public long UserDBID { get; private set; }
        public string UserID { get; private set; }
        public double UserBalance { get; private set; }
        public string PassToken { get; private set; }
        public string AgentID { get; private set; }
        public int AgentDBID { get; private set; }
        public long LastScoreCounter { get; private set; }
        public Currencies Currency { get; private set; }
        public bool IsAffiliate { get; private set; }
        public CreateNewUserMessage(string sessionToken, long userDBID, string strUserID, double userBalance, string passToken,
            int agentDBID, string agentID, long lastScoreCounter, Currencies currency, bool isAffiliate)
        {
            this.SessionToken = sessionToken;
            this.UserID = strUserID;
            this.UserDBID = userDBID;
            this.UserBalance = userBalance;
            this.PassToken = passToken;
            this.AgentDBID = agentDBID;
            this.LastScoreCounter = lastScoreCounter;
            this.Currency = currency;
            this.AgentID = agentID;
            this.IsAffiliate = isAffiliate;
        }
        public string GlobalUserID
        {
            get { return string.Format("{0}_{1}", this.AgentDBID, this.UserID); }
        }
    }

    public class FromConnRevMessage
    {
        public object       Connection      { get; private set; }
        public GITMessage   Message         { get; private set; }

        public FromConnRevMessage(object connection, GITMessage message)
        {
            this.Connection   = connection;
            this.Message        = message;
        }
    }
    public class SendMessageToUser
    {
        public GITMessage   Message { get; private set; }
        public double       Balance { get; private set; }
        public double       Delay   { get; private set; }
        public SendMessageToUser(GITMessage message, double balance, double delay)
        {
            Message = message;
            Balance = balance;
            Delay   = delay;
        }
    }

    //게임입장요청메세지
    public class EnterGameRequest
    {
        public int          AgentID     { get; private set; }
        public string       UserID      { get; private set; }
        public int          GameID      { get; private set; }
        public bool         NewEnter    { get; private set; }
        public IActorRef    UserActor   { get; private set; }
        public EnterGameRequest(int gameID, int agentID, string userID, IActorRef userActor, bool newEnter = true)
        {
            AgentID     = agentID;
            GameID      = gameID;
            UserID      = userID;
            NewEnter    = newEnter;
            UserActor   = userActor;
        }
    }
    //게임입장응답메세지
    public class EnterGameResponse
    {
        public IActorRef        GameActor   { get; private set; }
        public int              GameID      { get; private set; }
        public int              Ack         { get; private set; }       //0: 입장성공, 기타: 입장실패
        public List<GITMessage> SubMessages { get; private set; }       //게임입장후에 서버에서 유저에게 보낼 메세지들

        public EnterGameResponse(int gameID, IActorRef gameActor, int ack)
        {
            GameID      = gameID;
            Ack         = ack;
            GameActor   = gameActor;
            SubMessages = new List<GITMessage>();
        }
    }
    //게임탈퇴요청메세지
    public class ExitGameRequest
    {
        public string       UserID              { get; private set; }
        public int          WebsiteID           { get; private set; }
        public double       Balance             { get; private set; }
        public bool         UserRequested       { get; private set; }       //유저요청에 의한것인가? 아님 게임서버노드의 shutdown으로 인한것인가?
        public Currencies   Currency            { get; private set; }       //화페
        public bool         IsNewServerReady    { get; private set; }

        public ExitGameRequest(string userID, int websiteID, double balance, Currencies currency, bool userRequested, bool isNewServerReady)
        {
            UserID              = userID;
            WebsiteID           = websiteID;
            Balance             = balance;
            Currency            = currency;
            UserRequested       = userRequested;
            IsNewServerReady    = isNewServerReady;
        }
    }
    //게임탙퇴응답메세지
    public class ExitGameResponse
    {

    }
    public class AmaticExitResponse : ExitGameResponse
    {
        public ToUserResultMessage ResultMsg { get; set; }
        public AmaticExitResponse(ToUserResultMessage resultMsg)
        {
            this.ResultMsg = resultMsg;
        }
    }
    public class FromUserMessage
    {
        public string       UserID          { get; private set; }   //유저아이디
        public int          WebsiteID       { get; private set; }   //웹사이트아이디
        public GITMessage   Message         { get; private set; }   //클라에서 보낸 메세지
        public double       UserBalance     { get; private set; }   //유저잔고
        public IActorRef    UserActor       { get; private set; }   //유저액터
        public Currencies   Currency        { get; private set; }   //유저의 화페단위
        public FromUserMessage(string strUserID, int websiteID, double userBalance, IActorRef userActor, GITMessage message, Currencies currency)
        {
            UserID          = strUserID;
            WebsiteID       = websiteID;
            UserBalance     = userBalance;
            UserActor       = userActor;
            Message         = message;
            Currency        = currency;
        }
    }
    public class SubtractEventMoneyRequest : IConsistentHashable
    {
        public int AgentID { get; private set; }
        public string UserID { get; private set; }
        public double EventMoney { get; private set; }
        public object ConsistentHashKey
        {
            get
            {
                return AgentID;
            }
        }
        public SubtractEventMoneyRequest(int websiteID, string strUserID, double eventMoney)
        {
            this.AgentID = websiteID;
            this.UserID = strUserID;
            this.EventMoney = eventMoney;
        }
    }
    public class GameLogInfo
    {
        public string GameName  { get; private set; }
        public string TableName { get; private set; }
        public string LogString { get; private set; }
        public GameLogInfo()
        {

        }
        public GameLogInfo(string strGameName, string strTableName, string strGameLog)
        {
            GameName    = strGameName;
            TableName   = strTableName;
            LogString   = strGameLog;
        }
    }
    public class ToUserMessage
    {
        public int              GameID              { get; private set; }
        public List<GITMessage> Messages            { get; private set; }
        public bool             IsCountAsSpin       { get; private set; }
        public bool             IsRewardedBonus     { get; private set; }
        public double           RewardBonusMoney    { get; private set; }

        public void addMessage(GITMessage message)
        {
            if (message != null)
                Messages.Add(message);
        }
        public void insertFirstMessage(GITMessage message)
        {
            if (message == null)
                return;

            Messages.Insert(0, message);
        }
        public ToUserMessage()
        {
            IsRewardedBonus     = false;
            RewardBonusMoney    = 0.0;
        }
        public ToUserMessage(int gameID, GITMessage message)
        {
            GameID              = gameID;
            Messages            = new List<GITMessage>();
            if (message != null)
                Messages.Add(message);

            IsRewardedBonus     = false;
            RewardBonusMoney    = 0.0;
        }
        public void setBonusReward(double bonusMoney)
        {
            IsRewardedBonus     = true;
            RewardBonusMoney    = bonusMoney;
        }
        public void setCountAsSpin(bool isCountAsSpin)
        {
            IsCountAsSpin = isCountAsSpin;
        }
    }
    
    public enum UserBetTypes
    {
        Normal          = 0,
        PurchaseFree    = 1,
        AnteBet         = 2,
    }
    public class ToUserResultMessage : ToUserMessage
    {
        public double       BetMoney            { get; private set; }
        public double       WinMoney            { get; private set; }
        public double       TurnOver            { get; protected set; }
        public GameLogInfo  GameLog             { get; private set; }
        public UserBetTypes BetType             { get; private set; }        
        public string       BetTransactionID    { get; set; }
        public string       TransactionID       { get; set; }
        public string       RoundID             { get; set; }
        public bool         RoundEnd            { get; set; }
        public string FreeSpinID { get; set; }

        public ToUserResultMessage()
        {
        }

        public ToUserResultMessage(int gameID, GITMessage message, double betMoney, double winMoney, GameLogInfo gameLog, UserBetTypes betType, double turnOver = -1.0) : base(gameID, message)
        {
            this.BetMoney   = betMoney;
            this.WinMoney   = winMoney;
            this.GameLog    = gameLog;
            this.BetType    = betType;
            if (turnOver == -1.0)
                this.TurnOver = betMoney;
            else
                this.TurnOver = turnOver;
        }
    }
    
    public class SocketConnectionAdded
    {

    }
    public class SocketConnectionClosed
    {

    }
    public class SlotsNodeShuttingDownMsg
    {

    }
    public class ForceLogoutMesssage
    {
        public int    AgentID { get; private set; }
        public string UserID  { get; private set; }

        public string GlobalUserID
        {
            get
            {
                return string.Format("{0}_{1}", AgentID, UserID);
            }
        }
        public ForceLogoutMesssage(string strUserID)
        {
            this.UserID = strUserID;
        }
    }
    public class UserSpinItem : IConsistentHashable
    {
        public int      AgentID         { get; private set; }
        public string   UserID          { get; private set; }
        public string   Symbol          { get; private set; }
        public double   BetPerLine      { get; private set; }
        public int      LineCount       { get; private set; }
        public int      MoreBetID       { get; private set; } 
        public int      PurchaseID      { get; private set; }
        public double   BetMoney        { get; private set; }

        public UserSpinItem(int agentID, string strUserID, string symbol, double betPerLine, int lineCount, int moreBetID, int purchaseID, double betMoney)
        {
            AgentID     = agentID;
            UserID      = strUserID;
            Symbol      = symbol;
            BetPerLine  = betPerLine;
            LineCount   = lineCount;
            MoreBetID   = moreBetID;
            PurchaseID  = purchaseID;
            BetMoney    = betMoney;
        }
        public object ConsistentHashKey     => string.Format("{0}_{1}", AgentID, UserID);
    }
    public enum Languages
    {
        EN    = 0,
        FR    = 1,
        COUNT = 2,
    }
    public enum UserBonusType
    {
        GAMEJACKPOT = 0,
        USEREVENT = 1,
        REPACKET = 2,
        RACEPRIZE = 3,
        FREESPIN = 4,
    }
    public class UserBonus
    {
        public UserBonusType BonusType { get; protected set; }
        public long BonusID { get; protected set; }
    }
    public class UserRangeOddEventBonus : UserBonus
    {
        public double MinOdd { get; private set; }
        public double MaxOdd { get; private set; }
        public double MaxBet { get; private set; }
        public UserRangeOddEventBonus(long bonusID, double minOdd, double maxOdd, double maxBet)
        {
            this.BonusID = bonusID;
            this.MinOdd = minOdd;
            this.MaxOdd = maxOdd;
            this.MaxBet = maxBet;
            this.BonusType = UserBonusType.USEREVENT;
        }
    }

    public class HttpSessionClosed
    {
        public string SessionToken { get; private set; }
        public HttpSessionClosed(string strSessionToken)
        {
            this.SessionToken = strSessionToken;
        }
    }
    public class HttpSessionAdded
    {
        public string SessionToken { get; private set; }
        public HttpSessionAdded(string strSessionToken)
        {
            this.SessionToken = strSessionToken;
        }
    }
    public class UserFreeSpinBonus : UserBonus
    {
        public int GameID { get; private set; }
        public int BetLevel { get; private set; }
        public int FreeSpinCount { get; private set; }
        public DateTime ExpireTime { get; private set; }
        public string FreeSpinID { get; private set; }
        public UserFreeSpinBonus(long bonusID, int gameid, int betLevel, int freeSpinCount, DateTime expireTime, string freeSpinID)
        {
            this.BonusID = bonusID;
            this.GameID = gameid;
            this.BetLevel = betLevel;
            this.FreeSpinCount = freeSpinCount;
            this.ExpireTime = expireTime;
            this.FreeSpinID = freeSpinID;
            this.BonusType = UserBonusType.FREESPIN;
        }
    }

    public class SpinResponseData
    {
        public string spindata { get; set; }
        public double balance { get; set; }    
        public double totalWin {  get; set; }
        public double totalBet { get; set; }

        public SpinResponseData(string data, double userBalance, double totalWin, double totalBet)
        {
            this.spindata = data;
            this.balance = userBalance;
            this.totalWin = totalWin;
            this.totalBet = totalBet;   
        }

    }

}