using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using ApiIntegration.Database;
using StackExchange.Redis;
using GITProtocol;
using ApiIntegration.HTTPService;
using System.Data.Entity.Core.Metadata.Edm;

namespace ApiIntegration.HTTPService
{
    public class HTTPWorkActor : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Logging.GetLogger(Context);
        public HTTPWorkActor()
        {
            ReceiveAsync<GetBalanceRequest> (onGetBalance);
            ReceiveAsync<WithdrawRequest>   (onWithdrawRequest);
            ReceiveAsync<DepositRequest>    (onDepositRequest);
            ReceiveAsync<RollbackRequest>   (onRollbackRequest);
            ReceiveAsync<BetWinRequest>     (onBetWinRequest);
            Receive<string>                 (onProcCommand);
        }

        private void onProcCommand(string strCommand)
        {
            if (strCommand == "terminate")
            {
                Self.Tell(PoisonPill.Instance);
            }
        }
        private async Task onGetBalance(GetBalanceRequest request)
        {
            try
            {
                string strSign = createDataSign(HTTPServiceConfig.Instance.SecretKey, string.Format("{0}{1}{2}", request.agentID,
                    request.userID, request.gameID));
                if(strSign != request.sign)
                {
                    Sender.Tell(new GetBalanceResponse((int)CallBackResponseCodes.InvalidSign, "Invalid Sign", 0.0M));
                    return;
                }
                UserLoginResponse loginResponse = await Context.System.ActorSelection("/user/dbproxy/readers").Ask<UserLoginResponse>(new IUserLoginRequest(request.userID), TimeSpan.FromSeconds(30.0));
                if(loginResponse.Result != LoginResult.OK)
                {
                    Sender.Tell(new GetBalanceResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found", 0.0M));
                    return;
                }

                IActorRef userActor = await Context.System.ActorSelection("/user/userManager").Ask<IActorRef>(new CreateNewUserMsg(loginResponse.UserDBID, loginResponse.UserID, loginResponse.UserBalance), TimeSpan.FromSeconds(30.0));
                if(userActor == ActorRefs.Nobody)
                {
                    Sender.Tell(new GetBalanceResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found", 0.0M));
                    return;
                }
                double balance = await userActor.Ask<double>(request, TimeSpan.FromSeconds(10.0));
                Sender.Tell(new GetBalanceResponse((int)CallBackResponseCodes.OK, "", (decimal)balance));
            }
            catch
            {

            }
        }
        private async Task onBetWinRequest(BetWinRequest request)
        {
            try
            {
                string strSign = createDataSign(HTTPServiceConfig.Instance.SecretKey, string.Format("{0}{1}{2}{3}{4}{5}{6}", request.agentID,
                    request.userID, request.betAmount.ToString("0.00"), request.winAmount.ToString("0.00"), request.transactionID, request.roundID, request.gameID));
                if (strSign != request.sign)
                {
                    Sender.Tell(new BetWinResponse((int)CallBackResponseCodes.InvalidSign, "Invalid Sign"));
                    return;
                }
                UserLoginResponse loginResponse = await Context.System.ActorSelection("/user/dbproxy/readers").Ask<UserLoginResponse>(new IUserLoginRequest(request.userID), TimeSpan.FromSeconds(30.0));
                if (loginResponse.Result != LoginResult.OK)
                {
                    Sender.Tell(new BetWinResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found"));
                    return;
                }

                IActorRef userActor = await Context.System.ActorSelection("/user/userManager").Ask<IActorRef>(new CreateNewUserMsg(loginResponse.UserDBID, loginResponse.UserID, loginResponse.UserBalance), TimeSpan.FromSeconds(30.0));
                if (userActor == ActorRefs.Nobody)
                {
                    Sender.Tell(new BetWinResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found"));
                    return;
                }
                BetWinResponse response = await userActor.Ask<BetWinResponse>(request, TimeSpan.FromSeconds(10.0));
                Sender.Tell(response);
            }
            catch
            {

            }
        }
        private async Task onWithdrawRequest(WithdrawRequest request)
        {
            try
            {
                string strSign = createDataSign(HTTPServiceConfig.Instance.SecretKey, string.Format("{0}{1}{2}{3}{4}{5}", request.agentID,
                    request.userID, request.amount.ToString("0.00"), request.transactionID, request.roundID, request.gameID));
                if (strSign != request.sign)
                {
                    Sender.Tell(new WithdrawResponse((int)CallBackResponseCodes.InvalidSign, "Invalid Sign"));
                    return;
                }
                UserLoginResponse loginResponse = await Context.System.ActorSelection("/user/dbproxy/readers").Ask<UserLoginResponse>(new IUserLoginRequest(request.userID), TimeSpan.FromSeconds(30.0));
                if (loginResponse.Result != LoginResult.OK)
                {
                    Sender.Tell(new WithdrawResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found"));
                    return;
                }

                IActorRef userActor = await Context.System.ActorSelection("/user/userManager").Ask<IActorRef>(new CreateNewUserMsg(loginResponse.UserDBID, loginResponse.UserID, loginResponse.UserBalance), TimeSpan.FromSeconds(30.0));
                if (userActor == ActorRefs.Nobody)
                {
                    Sender.Tell(new WithdrawResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found"));
                    return;
                }
                WithdrawResponse response = await userActor.Ask<WithdrawResponse>(request, TimeSpan.FromSeconds(10.0));
                Sender.Tell(response);
            }
            catch
            {

            }
        }
        private async Task onDepositRequest(DepositRequest request)
        {
            try
            {
                string strSign = createDataSign(HTTPServiceConfig.Instance.SecretKey, string.Format("{0}{1}{2}{3}{4}{5}{6}", request.agentID,
                    request.userID, request.amount.ToString("0.00"), request.refTransactionID, request.transactionID, request.roundID, request.gameID));

                if (strSign != request.sign)
                {
                    Sender.Tell(new DepositResponse((int)CallBackResponseCodes.InvalidSign, "Invalid Sign"));
                    return;
                }
                UserLoginResponse loginResponse = await Context.System.ActorSelection("/user/dbproxy/readers").Ask<UserLoginResponse>(new IUserLoginRequest(request.userID), TimeSpan.FromSeconds(30.0));
                if (loginResponse.Result != LoginResult.OK)
                {
                    Sender.Tell(new DepositResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found"));
                    return;
                }

                IActorRef userActor = await Context.System.ActorSelection("/user/userManager").Ask<IActorRef>(new CreateNewUserMsg(loginResponse.UserDBID, loginResponse.UserID, loginResponse.UserBalance), TimeSpan.FromSeconds(30.0));
                if (userActor == ActorRefs.Nobody)
                {
                    Sender.Tell(new DepositResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found"));
                    return;
                }
                DepositResponse response = await userActor.Ask<DepositResponse>(request, TimeSpan.FromSeconds(10.0));
                Sender.Tell(response);
            }
            catch(Exception ex)
            {

            }
        }
        private async Task onRollbackRequest(RollbackRequest request)
        {
            try
            {
                string strSign = createDataSign(HTTPServiceConfig.Instance.SecretKey, string.Format("{0}{1}{2}{3}", request.agentID,
                    request.userID,  request.refTransactionID, request.gameID));

                if (strSign != request.sign)
                {
                    Sender.Tell(new DepositResponse((int)CallBackResponseCodes.InvalidSign, "Invalid Sign"));
                    return;
                }
                UserLoginResponse loginResponse = await Context.System.ActorSelection("/user/dbproxy/readers").Ask<UserLoginResponse>(new IUserLoginRequest(request.userID), TimeSpan.FromSeconds(30.0));
                if (loginResponse.Result != LoginResult.OK)
                {
                    Sender.Tell(new DepositResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found"));
                    return;
                }

                IActorRef userActor = await Context.System.ActorSelection("/user/userManager").Ask<IActorRef>(new CreateNewUserMsg(loginResponse.UserDBID, loginResponse.UserID, loginResponse.UserBalance), TimeSpan.FromSeconds(30.0));
                if (userActor == ActorRefs.Nobody)
                {
                    Sender.Tell(new DepositResponse((int)CallBackResponseCodes.UserIDNotFound, "User Id not found"));
                    return;
                }
                RollbackResponse response = await userActor.Ask<RollbackResponse>(request, TimeSpan.FromSeconds(10.0));
                Sender.Tell(response);
            }
            catch
            {

            }
        }
        public static string createDataSign(string key, string message)
        {
            var hmac = System.Security.Cryptography.HMAC.Create("HMACSHA256");
            hmac.Key = System.Text.Encoding.UTF8.GetBytes(key);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }    
}
