using Akka.Actor;
using Akka.Event;
using BrakePedal;
using GITProtocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueenApiNode.HttpService;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using QueenApiNode.Database;
using System.Globalization;
using GITProtocol;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using static Akka.Actor.Status;

namespace QueenApiNode.Agent
{
    class AgentActor : ReceiveActor
    {
        #region 에이전트정보
        private int             _dbID       = 0;
        private string          _strAgentID = "";
        private string          _strAuthKey = "";
        private bool            _isEnabled  = true;
        private Currencies      _currency   = Currencies.USD;
        private Languages       _language   = Languages.EN;
        private 
        #endregion

        protected static RealExtensions.Epsilon _epsilion   = new RealExtensions.Epsilon(0.01);
        private          IActorRef              _dbReader   = null;
        private          IActorRef              _dbWriter   = null;
        private readonly ILoggingAdapter        _logger     = Logging.GetLogger(Context);

        public static Props Props(CreateNewAgentActorMsg message)
        {
            return Akka.Actor.Props.Create(() => new AgentActor(message));
        }
        public AgentActor(CreateNewAgentActorMsg message)
        {
            _dbReader   = message.DBReader;
            _dbWriter   = message.DBWriter;
            _dbID       = message.AgentDBID;
            _strAgentID = message.AgentID;
            _strAuthKey = message.AuthToken;

            if(message.Currency >= 0 && message.Currency < (int) Currencies.COUNT)
                _currency   = (Currencies) message.Currency;
            
            if(message.Language >= 0 && message.Language < (int) Languages.COUNT)
                _language = (Languages) message.Language;

            ReceiveAsync<ApiConsistentRequest>  (onAPIRequest);
            Receive<AgentUpdated>               (onAgentStateUpdate);
        }
        private void onAgentStateUpdate(AgentUpdated update)
        {
            _isEnabled = (update.State == 1);
        }
        private async Task onAPIRequest(ApiConsistentRequest request)
        {
            try
            {
                if (!_isEnabled)
                {
                    Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.AgentDisabled, "Agent has been disabled")));
                    return;
                }
                if (request.Request is GetGameListRequest)
                    onGetGameList(request.Request as GetGameListRequest);
                else if (request.Request is UserAuthRequest)
                    await onUserAuthRequest(request.Request as UserAuthRequest);
                
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in AgentActor::onAPIRequest {0}", ex);
            }
        }
        private void onGetGameList(GetGameListRequest request)
        {
            try
            {
                List<GameInfo> gameInfos = DBMonitorSnapshot.Instance.getAllGameInfo();
                List<ApiGame>  apiGames  = new List<ApiGame>();

                foreach(GameInfo gameInfo in gameInfos)
                {
                    if(gameInfo.Symbol == "LuckyPiggies2" || gameInfo.Symbol == "BillysGang")
                        continue;

                    ApiGame apiGame  = new ApiGame();
                    apiGame.name     = gameInfo.Name;
                    apiGame.vendorid = "Jili";
                    apiGame.gameid   = gameInfo.GameID;
                    apiGame.symbol   = gameInfo.Symbol;
                    apiGame.iconurl1 = string.Format("https://amaticgame.net/ImageIcons/Amatic/{0}.jpg", gameInfo.Symbol);
                    apiGame.iconurl2 = string.Format("https://amaticgame.net/ImageIcons/Amatic/{0}.jpg", gameInfo.Symbol);

                    apiGames.Add(apiGame);
                }
                Sender.Tell(JsonConvert.SerializeObject(new GetGameListResponse(apiGames)));
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in AgentActor::onGetGameList {0}", ex);
                Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.GeneralError, "general error")));
            }
        }       
        private async Task onUserAuthRequest(UserAuthRequest request)
        {
            try
            {
                if(string.IsNullOrEmpty(request.userID))
                {
                    Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.WrongInputParameters, "user id doesn't match condition")));
                    return;
                }
                Regex regex = new Regex("^[a-zA-Z0-9]{4,48}$");
                Match match = regex.Match(request.userID);
                if (!match.Success)
                {
                    Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.WrongInputParameters, "user id doesn't match condition")));
                    return;
                }

                DBGetUserInfoResponse response = await _dbReader.Ask<DBGetUserInfoResponse>(new DBGetUserInfoRequest(_dbID, request.userID), TimeSpan.FromSeconds(10.0));
                string strUserID   = null;
                string strPassword = null;
                if (response == null)
                {
                    strUserID       = request.userID;
                    strPassword     = getMD5Hash(Guid.NewGuid().ToString());
                    bool bSuccess   = await _dbReader.Ask<bool>(new DBCreateUserRequest(strUserID, strPassword, _dbID, _strAgentID, _currency, _language));
                    if(!bSuccess)
                    {
                        Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.WrongInputParameters, "user id doesn't match condition")));
                        return;
                    }
                }
                else
                {
                    strUserID   = response.UserName;
                    strPassword = response.Password;
                }
                GameInfo gameInfo = DBMonitorSnapshot.Instance.getGameInfo(request.gameid);
                if (gameInfo == null)
                {
                    Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.WrongInputParameters, "undefined game id")));
                    return;
                }


                if (gameInfo.Symbol == "LuckyPiggies2" || gameInfo.Symbol == "BillysGang")
                {
                    Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.WrongInputParameters, "undefined game id")));
                    return;
                }

                string url = ApiConfig.GameBackendGS;
                string[] parts = url.Split('.');
                string mainGSDomain = parts.Length >= 2 ? parts[1] : "";
                if (mainGSDomain == "")
                {
                    Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.WrongInputParameters, "GS domain format error")));
                    return;
                }

                string token1 = $"{_dbID}_{strUserID}_{gameInfo.Symbol}_{strPassword}_{gameInfo.GameID}_{DateTime.UtcNow.AddMinutes(5.0):yyyy-MM-dd HH:mm:ss}";

                // ✅ AES 암호화
                byte[] encrypted = Crypto.EncryptAesCbc(Encoding.UTF8.GetBytes(token1), ApiConfig.FrontTokenKey);

                // ✅ Base64 URL-safe 인코딩
                string ssoKey = System.Web.HttpServerUtility.UrlTokenEncode(encrypted);

                string strGameURL1 = $"{ApiConfig.GameFrontUrl}/{gameInfo.Symbol}/?ssoKey={ssoKey}&lang={request.lang}&legalLang=true&gameId={gameInfo.Code}&gs={new string(ApiConfig.GameBackendGS.Reverse().ToArray())}&domain_platform={new string(ApiConfig.GameBackendGS.Reverse().ToArray())}&domain_gs={new string(mainGSDomain.Reverse().ToArray())}&be={new string(ApiConfig.GameBackendBE.Reverse().ToArray())}&iu=true&apiId=2";

                Sender.Tell(JsonConvert.SerializeObject(new UserAuthResponse(strGameURL1)));
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in AgentActor::onUserGetGameURL {0} {1}", ex, _strAgentID);
                Sender.Tell(JsonConvert.SerializeObject(new QueenResponse(ResponseCodes.GeneralError, "general error")));
            }
        }
        private string getMD5Hash(string input)
        {
            try
            {
                MD5 md5Hash = MD5.Create();
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
