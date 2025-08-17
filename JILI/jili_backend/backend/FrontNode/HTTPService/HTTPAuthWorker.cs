using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using FrontNode.Database;
using FrontNode.HTTPService.Models;
using GITProtocol;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

/***
 * 
 *          Created by Foresight(2021.03.18)
 */
namespace FrontNode.HTTPService
{
    public class HTTPAuthWorker : ReceiveActor
    {
        private readonly ILoggingAdapter    _logger         = Logging.GetLogger(Context);

        public HTTPAuthWorker()
        {
            ReceiveAsync<HTTPAuthRequest>           (onCreateNewToken);            
            Receive<string>                         (onProcCommand);
        }

        private void onProcCommand(string strCommand)
        {
            if (strCommand != "terminate")
                return;

            Self.Tell(PoisonPill.Instance);
        }
        private string createNewSessionToken(string strUserToken)
        {
            string strSessionToken = strUserToken + "_" + Guid.NewGuid().ToString();
            return strSessionToken;
        }
        private async Task onCreateNewToken(HTTPAuthRequest request)
        {

            try
            {
                var gameConfigItem = DBMonitorSnapshot.Instance.getGameConfigFromSymbol(GameProviders.JILI, request.GameSymbol);
                if (gameConfigItem == null)
                {
                    Sender.Tell(new HTTPAuthResponse(HttpAuthResults.IDPASSWORDERROR));
                    return;
                }
                //디비에서 조회한다.
                UserLoginResponse loginResponse = await Context.System.ActorSelection("/user/dbproxy/readers").Ask<UserLoginResponse>(new UserLoginRequest(request.AgentID, request.UserID, request.PasswordMD5, request.IPAddress, PlatformTypes.WEB), TimeSpan.FromSeconds(10.0));
                if (loginResponse.Result != LoginResult.OK)
                {
                    //실패결과 리턴
                    Sender.Tell(new HTTPAuthResponse(HttpAuthResults.INVALIDGAMESYMBOL));
                    return;
                }
                string strNewSesionToken = createNewSessionToken(loginResponse.GlobalUserID + "@" + loginResponse.PassToken);
                string strHashKey = string.Format("{0}_tokens", loginResponse.GlobalUserID);

                //유저액터가 이미 존재하는가를 검사한다.
                RedisValue redisValue = await RedisDatabase.RedisCache.HashGetAsync("onlineusers", loginResponse.GlobalUserID + "_path");
                if (!redisValue.IsNullOrEmpty)
                {
                    string strUserActorPath = (string)redisValue;
                    await RedisDatabase.RedisCache.HashSetAsync(strHashKey, strNewSesionToken, strUserActorPath);

                    Context.System.ActorSelection(strUserActorPath).Tell(new HttpSessionAdded(strNewSesionToken));
                    Sender.Tell(new HTTPAuthResponse(strNewSesionToken, loginResponse.Currency.ToString(), gameConfigItem.Name, GetSHA1(strNewSesionToken)));
                    return;
                }

                //유저새로 로그인
                bool isNotOnline = await RedisDatabase.RedisCache.HashSetAsync("onlineusers", loginResponse.GlobalUserID, true, When.NotExists);
                if (isNotOnline)
                {
                    //새로운 세션토큰을 만든다.
                    IActorRef userActor = await Context.System.ActorSelection("/user/userRouter").Ask<IActorRef>(new CreateNewUserMessage(strNewSesionToken, loginResponse.UserDBID, loginResponse.UserID, loginResponse.UserBalance, loginResponse.PassToken, loginResponse.AgentDBID, loginResponse.AgentID, loginResponse.LastScoreCounter, loginResponse.Currency, false), TimeSpan.FromSeconds(10.0));
                    string strUserActorPath = userActor.Path.ToString();

                    await RedisDatabase.RedisCache.HashSetAsync(strHashKey, strNewSesionToken, strUserActorPath);
                    Sender.Tell(new HTTPAuthResponse(strNewSesionToken, loginResponse.Currency.ToString(), gameConfigItem.Name, GetSHA1(strNewSesionToken)));
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in HTTPAuthWorker::onCreateNewToken {0}", ex);
                Sender.Tell(new HTTPAuthResponse(HttpAuthResults.IDPASSWORDERROR));
            }

        }


        string GetSHA1(string input)
        {
            var sha1 = SHA1.Create();
            return string.Concat(sha1.ComputeHash(Encoding.UTF8.GetBytes(input)).Select(b => b.ToString("x2")));
        }


        public class CheckUserPathFromRedis
        {
            public IActorRef    Sender          { get; private set; }
            public string       UserID          { get; private set; }
            public string       SessionToken    { get; private set; }
            public int          RetryCount      { get; private set; }

            public CheckUserPathFromRedis(string strUserID, string strSessionToken, IActorRef sender, int retryCount)
            {
                this.UserID         = strUserID;
                this.SessionToken   = strSessionToken;
                this.Sender         = sender;
                this.RetryCount     = retryCount;
            }
        }
    }
}
