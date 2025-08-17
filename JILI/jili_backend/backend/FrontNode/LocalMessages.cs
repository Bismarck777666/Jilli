using Akka.Actor;
using Akka.Routing;
using GITProtocol;
using Jili.Protocols.Gem3;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontNode
{
    public class HTTPEnterGameRequest : IConsistentHashable
    {
        public int AgentID { get; set; }
        public string UserID { get; private set; }
        public string SessionToken { get; private set; }
        public GameProviders GameType { get; private set; }
        public string GameIdentifier { get; private set; }

        public HTTPEnterGameRequest(int agentID, string strUserID, string strSessionToken, GameProviders gameType, string strGameIdentifier)
        {
            this.AgentID = agentID;
            this.UserID = strUserID;
            this.SessionToken = strSessionToken;
            this.GameType = gameType;
            this.GameIdentifier = strGameIdentifier;
        }

        public object ConsistentHashKey
        {
            get
            {
                return UserID;
            }
        }
    }

    public enum HTTPEnterGameResults
    {
        OK = 0,
        INVALIDTOKEN = 1,
        INVALIDGAMEID = 2,
        INVALIDACTION = 3,
    }
    public enum PlatformTypes
    {
        MOBILE  = 0,
        WEB     = 1,
    }
    public enum LoginResult
    {
        OK                  = 1,
        ALREADYLOGGEDIN     = 3,
        IDPASSWORDMISMATCH  = 5,
        COUNTRYMISMATCH     = 6,
        ACCOUNTDISABLED     = 10,
        UNKNOWNERROR        = 11,
    }

    public enum ToHTTPSessionMsgResults
    {
        OK = 0,
        INVALIDTOKEN = 1,
        INVALIDACTION = 2,
    }

    public class UserLoginResponse
    {
        public LoginResult  Result           { get; private set; }
        public long         UserDBID         { get; private set; }
        public string       UserID           { get; private set; }
        public double       UserBalance      { get; private set; }
        public string       PassToken        { get; private set; }
        public string       AgentID          { get; private set; }
        public int          AgentDBID        { get; private set; }
        public long         LastScoreCounter { get; private set; }
        public Currencies   Currency         { get; private set; }
        public UserLoginResponse()
        {

        }
        public UserLoginResponse(LoginResult result)
        {
            this.Result = result;
        }
        public string       GlobalUserID => string.Format("{0}_{1}", AgentDBID, UserID);

        public UserLoginResponse(string agentID, int agentDBID, long userDBID, string strUserID, string strPassToken, double userBalance, long lastScoreCounter, int currency)
        {
            Result              = LoginResult.OK;
            AgentID             = agentID;
            AgentDBID           = agentDBID;
            UserDBID            = userDBID;
            UserID              = strUserID;
            PassToken           = strPassToken;
            UserBalance         = userBalance;
            LastScoreCounter    = lastScoreCounter;
            Currency            = (Currencies)currency;
        }
    }
    public class UserLoginRequest
    {
        public int              AgentID     { get; private set; }
        public string           UserID      { get; private set; }
        public string           Password    { get; private set; }
        public string           IPAddress   { get; private set; }
        public PlatformTypes    Platform    { get; private set; }
        public UserLoginRequest(int agentID, string strUserID, string strPassword, string strIPAddress, PlatformTypes platform)
        {
            AgentID     = agentID;
            UserID      = strUserID;
            Password    = strPassword;
            IPAddress   = strIPAddress;
            Platform    = platform;
        }
    }                   

    #region Message related to HTTP Session    
    public class HTTPAuthRequest : IConsistentHashable
    {
        public int AgentID { get; private set; }
        public string UserID { get; private set; }
        public string PasswordMD5 { get; private set; }
        public string IPAddress { get; private set; }
        public string GameSymbol { get; private set; }

        public HTTPAuthRequest(int agentID, string strUserID, string strPasswordMD5, string strIPAddress, string strSymbol)
        {
            this.AgentID = agentID;
            this.UserID = strUserID;
            this.PasswordMD5 = strPasswordMD5;
            this.IPAddress = strIPAddress;
            this.GameSymbol = strSymbol;
        }
        public object ConsistentHashKey
        {
            get
            {
                return string.Format("{0}_{1}", AgentID, UserID);
            }
        }
    }
    public class HTTPAuthResponse
    {
        public HttpAuthResults Result { get; private set; }
        public string SessionToken { get; private set; }
        public string Currency { get; private set; }
        public string GameName { get; private set; }
        public string GameData { get; private set; }
        public HTTPAuthResponse()
        {

        }
        public HTTPAuthResponse(HttpAuthResults result)
        {
            this.Result = result;
        }
        public HTTPAuthResponse(string strToken, string currency, string gameName, string gameData)
        {
            this.Result = HttpAuthResults.OK;
            this.SessionToken = strToken;
            this.Currency = currency;
            this.GameName = gameName;
            this.GameData = gameData;
        }
    }
    public enum HttpAuthResults
    {
        OK                  = 0,
        IDPASSWORDERROR     = 1,
        SERVERMAINTENANCE   = 2,
        INVALIDGAMESYMBOL   = 3,
        ALREADYONLINE       = 4,
    }
    #endregion

    public class CheckUserPathFromRedis
    {
        public UserLoginResponse Response { get; private set; }

        public CheckUserPathFromRedis(UserLoginResponse response)
        {
            this.Response = response;
        }
    }

    ////////////////////////////////////////////////////
    ///

    public class FromHTTPSessionMessage : IConsistentHashable
    {
        public int AgentID { get; private set; }
        public string UserID { get; private set; }
        public string SessionToken { get; private set; }
        public GITMessage Message { get; private set; }

        public FromHTTPSessionMessage(int agentID, string strUserID, string strSessionToken, GITMessage message)
        {
            this.AgentID = agentID;
            this.UserID = strUserID;
            this.SessionToken = strSessionToken;
            this.Message = message;
        }

        public object ConsistentHashKey
        {
            get
            {
                return this.UserID;
            }
        }
    }


    public class HttpLogoutRequest
    {
        public string Token { get; private set; }
        public string UserID { get; private set; }

        public int AgentID { get; private set;}
        public HttpLogoutRequest(int agentID, string strUserID, string strToken)
        {
            this.AgentID = agentID;
            this.UserID = strUserID;
            this.Token = strToken;
        }
    }

    public class ToHTTPSessionMessage
    {
        public ToHTTPSessionMsgResults Result { get; private set; }
        public GITMessage Message { get; private set; }
        public ToHTTPSessionMessage(ToHTTPSessionMsgResults result)
        {
            this.Result = result;
        }

        public ToHTTPSessionMessage()
        {

        }

        public ToHTTPSessionMessage(GITMessage message)
        {
            this.Result = ToHTTPSessionMsgResults.OK;
            this.Message = message;
        }
    }
}
