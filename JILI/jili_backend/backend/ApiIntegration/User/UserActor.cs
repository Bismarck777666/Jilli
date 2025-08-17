using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using GITProtocol.Utils;
using ApiIntegration.Database;
using System.Net;
using Akka.Routing;
using System.Threading;
using StackExchange.Redis;
using Akka.Cluster;
using Newtonsoft.Json;
using System.Net.Http;
using Pipelines.Sockets.Unofficial;
using System.Transactions;
using Newtonsoft.Json.Linq;
using ApiIntegration.HTTPService;

namespace ApiIntegration
{
    //로그인된 사용자를 표현하는 클라스 
    public class UserActor : ReceiveActor
    {
        #region 사용자정보
        private long                    _userDBID               = 0;
        private string                  _strUserID              = "";
        private double                  _balance                = 0.0;
        #endregion

        #region 유저의 상태변수들       
        private bool                    _userDisconnected       = false;
        #endregion

        private IActorRef                       _dbReader       = null;
        private IActorRef                       _dbWriter       = null;
        private readonly ILoggingAdapter        _logger         = Logging.GetLogger(Context);
        protected static RealExtensions.Epsilon _epsilion       = new RealExtensions.Epsilon(0.001);
        private DateTime                        _lastActiveTime = DateTime.Now;

        private ICancelable     _conCheckCancel         = null;
        public UserActor(CreateNewUserMsg message, IActorRef dbReader, IActorRef dbWriter)
        {
            _dbReader           = dbReader;
            _dbWriter           = dbWriter;
            _userDBID           = message.UserDBID;
            _strUserID          = message.UserID;
            _balance            = message.Balance;

            Receive<string>                 (onCommand);
            Receive<GetBalanceRequest>      (onGetBalance);
            ReceiveAsync<WithdrawRequest>   (onWithdraw);
            ReceiveAsync<DepositRequest>    (onDeposit);
            ReceiveAsync<RollbackRequest>   (onRollback);
            ReceiveAsync<BetWinRequest>     (onBetWin);
        }

        public static Props Props(CreateNewUserMsg message, IActorRef dbReader, IActorRef dbWriter)
        {
            return Akka.Actor.Props.Create(() => new UserActor(message, dbReader, dbWriter));
        }
        protected override void PreStart()
        {
            base.PreStart();
            _logger.Info("{0} has been logged in successfully", _strUserID);
            _conCheckCancel = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(1000, 1000, Self, "checkConn", Self);
        }
        protected override void PostStop()
        {
            if (_conCheckCancel != null)
                _conCheckCancel.Cancel();
            base.PostStop();
        }
        
        protected void onGetBalance(GetBalanceRequest request)
        {
            Sender.Tell(_balance);
        }
        protected async Task onBetWin(BetWinRequest request)
        {
            try
            {
                var redisValue = await RedisDatabase.RedisCache.StringGetAsync(request.transactionID);
                if (!redisValue.IsNullOrEmpty)
                {
                    Sender.Tell(new WithdrawResponse((int)CallBackResponseCodes.DuplicateTransaction, "Duplicate transaction"));
                    return;
                }
                if (_balance.LT((double)request.betAmount, _epsilion))
                {
                    Sender.Tell(new WithdrawResponse((int)CallBackResponseCodes.InsufficientFunds, "Insufficient funds"));
                    return;
                }

                _balance -= (double)request.betAmount;
                _balance += (double)request.winAmount;
                _dbWriter.Tell(new PlayerBalanceUpdateItem(_userDBID, (double)(request.winAmount - request.betAmount)));
                await RedisDatabase.RedisCache.StringSetAsync(request.transactionID, string.Format("{0}_{1}", request.betAmount.ToString("0.00"), request.winAmount.ToString("0.00")), TimeSpan.FromMinutes(30.0));
                var response                    = new BetWinResponse((int)CallBackResponseCodes.OK, "");
                response.balance                = (decimal)Math.Round(_balance, 2);
                response.platformTransactionID  = Guid.NewGuid().ToString().Replace("-", "");
                _lastActiveTime                 = DateTime.Now;
                Sender.Tell(response);
            }
            catch
            {

            }
        }
        protected async Task onWithdraw(WithdrawRequest request)
        {
            try
            {
                var redisValue = await RedisDatabase.RedisCache.StringGetAsync(request.transactionID);
                if(!redisValue.IsNullOrEmpty)
                {
                    Sender.Tell(new WithdrawResponse((int)CallBackResponseCodes.DuplicateTransaction, "Duplicate transaction"));
                    return;
                }
                if(_balance.LT((double) request.amount, _epsilion))
                {
                    Sender.Tell(new WithdrawResponse((int)CallBackResponseCodes.InsufficientFunds, "Insufficient funds"));
                    return;
                }
                
                _balance -= (double)request.amount;
                _dbWriter.Tell(new PlayerBalanceUpdateItem(_userDBID, -(double)request.amount));
                await RedisDatabase.RedisCache.StringSetAsync(request.transactionID, request.amount.ToString("0.00"), TimeSpan.FromMinutes(30.0));
                var response                    = new WithdrawResponse((int)CallBackResponseCodes.OK, "");
                response.balance                = (decimal) Math.Round(_balance, 2);
                response.platformTransactionID  = Guid.NewGuid().ToString().Replace("-", "");
                _lastActiveTime                 = DateTime.Now;
                Sender.Tell(response);
            }
            catch
            {

            }
        }
        protected async Task onDeposit(DepositRequest request)
        {
            try
            {
                var redisValue = await RedisDatabase.RedisCache.StringGetAsync(request.transactionID);
                if (!redisValue.IsNullOrEmpty)
                {
                    var depositResponse                     = new DepositResponse((int)CallBackResponseCodes.DuplicateTransaction, "Duplicate transaction");
                    depositResponse.balance                 = (decimal) Math.Round(_balance, 2);
                    depositResponse.platformTransactionID   = Guid.NewGuid().ToString().Replace("-", "");
                    Sender.Tell(depositResponse);
                    return;
                }

                _balance += (double)request.amount;
                _dbWriter.Tell(new PlayerBalanceUpdateItem(_userDBID, (double)request.amount));
                await RedisDatabase.RedisCache.StringSetAsync(request.transactionID, "Deposit", TimeSpan.FromMinutes(30.0));
                var response                    = new DepositResponse((int)CallBackResponseCodes.OK, "");
                response.balance                = (decimal)Math.Round(_balance, 2);
                response.platformTransactionID  = Guid.NewGuid().ToString().Replace("-", "");
                _lastActiveTime                 = DateTime.Now;
                Sender.Tell(response);
            }
            catch(Exception ex)
            {

            }
        }
        protected async Task onRollback(RollbackRequest request)
        {
            try
            {
                var redisValue = await RedisDatabase.RedisCache.StringGetAsync(request.refTransactionID);
                if (redisValue.IsNullOrEmpty)
                {
                    Sender.Tell(new RollbackResponse((int)CallBackResponseCodes.CanNotFindRefTransId, "Could not find reference transaction id"));
                    return;
                }
                if (redisValue == "Rollbacked")
                {
                    Sender.Tell(new RollbackResponse((int)CallBackResponseCodes.AlreadyRolledBack, "Transaction is already rolled back"));
                    return;
                }
                double amount = double.Parse(redisValue);
                _balance += amount;
                _dbWriter.Tell(new PlayerBalanceUpdateItem(_userDBID, amount));
                await RedisDatabase.RedisCache.StringSetAsync(request.refTransactionID, "Rollbacked", TimeSpan.FromMinutes(30.0));
                Sender.Tell(new RollbackResponse((int)CallBackResponseCodes.OK, ""));
            }
            catch
            {

            }
        }
        
        #region 각종 사건처리부        
        private void onCommand(string strCommand)
        {
            if(strCommand == "checkConn")
            {
                if (DateTime.Now.Subtract(_lastActiveTime) <= TimeSpan.FromMinutes(15.0))
                    return;

                _userDisconnected = true;
                _logger.Info("{0} user has been logged out", _strUserID);
                Context.Stop(Self);
            }
        }                        
        #endregion
    }
}
