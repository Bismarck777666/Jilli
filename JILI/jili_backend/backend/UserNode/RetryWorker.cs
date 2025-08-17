using Akka.Actor;
using Akka.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UserNode.Database;

namespace UserNode
{
    public class RetryWorker : ReceiveActor
    {
        private IActorRef                           _dbWriter   = null;
        private List<CallbackApiRequestWithRetry>   _retryTasks = new List<CallbackApiRequestWithRetry>();
        HttpClient                                  _httpClient = new HttpClient();

        public RetryWorker(IActorRef dbWriter)
        {
            Receive<CallbackApiRequestWithRetry>(_ =>
            {
                _retryTasks.Add(_);
            });
            ReceiveAsync<string>(onProcCommand);
            _dbWriter = dbWriter;
        }
        private async Task onProcCommand(string strCommand)
        {
            try
            {
                List<CallbackApiRequestWithRetry> toDelItems = new List<CallbackApiRequestWithRetry>();
                foreach (var item in _retryTasks)
                {
                    if (DateTime.Now.Subtract(item.LastTriedTime) < TimeSpan.FromSeconds(10.0))
                        continue;

                    if (item is RollbackRequestWithRetry)
                    {
                        var request = item as RollbackRequestWithRetry;
                        var response = await onRollback(request as RollbackRequestWithRetry);
                        if (response == null)
                        {
                            request.RetryCount--;
                            if (request.RetryCount < 0)
                            {
                                toDelItems.Add(request);
                                _dbWriter.Tell(new FailedTransactionItem(request.AgentID, request.UserID, TransactionTypes.Rollback, request.TransactionID, "", request.BetAmount, request.WinAmount, request.GameID, DateTime.UtcNow));
                            }
                        }
                        else if (response.code == 0 || response.code == 8 || response.code == 9)
                        {
                            toDelItems.Add(request);
                        }
                        else
                        {
                            toDelItems.Add(request);
                            _dbWriter.Tell(new FailedTransactionItem(request.AgentID, request.UserID, TransactionTypes.Rollback, request.TransactionID, "", request.BetAmount, request.WinAmount, request.GameID, DateTime.UtcNow));
                        }
                    }
                    else
                    {
                        var request = item as DepositRequestWithRetry;
                        var response = await onDeposit(request);
                        if (response == null)
                        {
                            request.RetryCount--;
                            if (request.RetryCount < 0)
                            {
                                toDelItems.Add(request);
                                _dbWriter.Tell(new FailedTransactionItem(request.AgentID, request.UserID, TransactionTypes.Deposit, request.TransactionID, request.RefTransactionID, 0.0, request.Amount, request.GameID, DateTime.UtcNow));
                            }
                        }
                        else if (response.code == 0 || response.code == 11)
                        {
                            toDelItems.Add(request);
                            _dbWriter.Tell(new DepositTransactionUpdateItem(request.TransactionID, response.platformTransactionID, DateTime.UtcNow));
                        }
                        else
                        {
                            toDelItems.Add(request);
                            _dbWriter.Tell(new FailedTransactionItem(request.AgentID, request.UserID, TransactionTypes.Deposit, request.TransactionID, request.RefTransactionID, 0.0, request.Amount, request.GameID, DateTime.UtcNow));
                        }
                    }
                }

                foreach (var item in toDelItems)
                    _retryTasks.Remove(item);
            }
            catch(Exception ex)
            {

            }
            finally
            {
                Context.System.Scheduler.ScheduleTellOnce(500, Self, "tick", Self);
            }
        }
        private async Task<RollbackResponse> onRollback(RollbackRequestWithRetry request)
        {
            try
            {
                var rollbackRequest = new RollbackRequest();
                rollbackRequest.agentID             = request.AgentID;
                rollbackRequest.userID              = request.UserID;
                rollbackRequest.refTransactionID    = request.TransactionID;
                rollbackRequest.gameID              = request.GameID;
                rollbackRequest.sign                = UserActor.createDataSign(request.SecretKey, string.Format("{0}{1}{2}{3}",
                    request.AgentID, request.UserID, request.TransactionID, request.GameID));

                string strURL = string.Format("{0}/RollbackTransaction", request.ApiURL);

                HttpResponseMessage message = await _httpClient.PostAsync(strURL, new StringContent(JsonConvert.SerializeObject(rollbackRequest), Encoding.UTF8, "application/json"));
                message.EnsureSuccessStatusCode();

                string  strContent  = await message.Content.ReadAsStringAsync();
                var     response    = JsonConvert.DeserializeObject<RollbackResponse>(strContent);
                return response;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private async Task<DepositResponse> onDeposit(DepositRequestWithRetry request)
        {
            try
            {
                var depositRequest              = new DepositRequest();
                depositRequest.agentID          = request.AgentID;
                depositRequest.userID           = request.UserID;
                depositRequest.transactionID    = request.TransactionID;
                depositRequest.refTransactionID = request.RefTransactionID;
                depositRequest.gameID           = request.GameID;
                depositRequest.amount           = (decimal) Math.Round(request.Amount, 2);
                depositRequest.roundID          = request.RoundID;
                depositRequest.endRound         = request.RoundEnd;
                depositRequest.sign             = UserActor.createDataSign(request.SecretKey, string.Format("{0}{1}{2}{3}{4}{5}{6}",
                    request.AgentID, request.UserID, request.Amount.ToString("0.00"), request.RefTransactionID, request.TransactionID, request.RoundID, request.GameID));

                string strURL = string.Format("{0}/Deposit", request.ApiURL);

                HttpResponseMessage message = await _httpClient.PostAsync(strURL, new StringContent(JsonConvert.SerializeObject(depositRequest), Encoding.UTF8, "application/json"));
                message.EnsureSuccessStatusCode();

                string strContent = await message.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<DepositResponse>(strContent);
                return response;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }

    public class CallbackApiRequestWithRetry : IConsistentHashable
    {
        public string   ApiURL          { get; private set; }
        public string   UserID          { get; private set; }
        public string   AgentID         { get; private set; }
        public int      GameID          { get; private set; }
        public string   SecretKey       { get; private set; }
        public DateTime LastTriedTime   { get; set; }
        public int      RetryCount      { get; set; }
        public object   ConsistentHashKey => string.Format("{0}_{1}", AgentID, UserID);
        public CallbackApiRequestWithRetry(string apiURL, string agentID, string userID, string secretKey, int gameID, int retryCount, DateTime lastTriedTime)
        {
            this.ApiURL         = apiURL;
            this.UserID         = userID;
            this.AgentID        = agentID;
            this.GameID         = gameID;
            this.SecretKey      = secretKey;
            this.RetryCount     = retryCount;
            this.LastTriedTime  = lastTriedTime;
        }
    }
    public class RollbackRequestWithRetry : CallbackApiRequestWithRetry
    {
        public double BetAmount         { get; private set; }
        public double WinAmount         { get; private set; }
        public string TransactionID     { get; private set; }
        public RollbackRequestWithRetry(string apiURL, string agentID, string userID, string secretKey, int gameID, int retryCount, DateTime lastTriedTime, string transactionID, double betAmount, double winAmount) :
            base(apiURL, agentID, userID, secretKey, gameID, retryCount, lastTriedTime)
        {
            this.TransactionID  = transactionID;
            this.BetAmount      = betAmount;
            this.WinAmount      = winAmount;
        }
    }
    public class DepositRequestWithRetry : CallbackApiRequestWithRetry
    {
        public string   TransactionID       { get; private set; }
        public string   RefTransactionID    { get; private set; }
        public string   RoundID             { get; private set; }
        public double   Amount              { get; private set; }
        public bool     RoundEnd            { get; private set; }

        public DepositRequestWithRetry(string apiURL, string agentID, string userID, string secretKey, int gameID, int retryCount, DateTime lastTriedTime, string transactionID, string refTransactionID, string roundID, double amount, bool roundEnd) :
            base(apiURL, agentID, userID, secretKey, gameID, retryCount, lastTriedTime)
        {
            this.TransactionID      = transactionID;
            this.RefTransactionID   = refTransactionID;
            this.RoundID            = roundID;
            this.Amount             = amount;
            this.RoundEnd           = roundEnd;
        }
    }
    public class WithdrawRequest
    {
        public string   agentID         { get; set; }
        public string   sign            { get; set; }
        public string   userID          { get; set; }
        public int      gameID          { get; set; }
        public decimal  amount          { get; set; }
        public string   transactionID   { get; set; }
        public string   roundID         { get; set; }
    }
    public class WithdrawResponse
    {
        public int      code                    { get; set; }
        public string   message                 { get; set; }
        public string   platformTransactionID   { get; set; }
        public decimal  balance                 { get; set; }

        public WithdrawResponse()
        {
            this.code = -1;
        }
    }
    public class DepositRequest
    {
        public string   agentID             { get; set; }
        public string   sign                { get; set; }
        public string   userID              { get; set; }
        public int      gameID              { get; set; }
        public decimal  amount              { get; set; }
        public string   transactionID       { get; set; }
        public string   refTransactionID    { get; set; }
        public string   roundID             { get; set; }
        public bool     endRound            { get; set; }
    }
    public class DepositResponse
    {
        public int      code                    { get; set; }
        public string   message                 { get; set; }
        public string   platformTransactionID   { get; set; }
        public decimal  balance                 { get; set; }

        public DepositResponse()
        {
            this.code = -1;
        }
    }
    public class RollbackRequest
    {
        public string   agentID             { get; set; }
        public string   sign                { get; set; }
        public string   userID              { get; set; }
        public string   refTransactionID    { get; set; }
        public int      gameID              { get; set; }
    }
    public class RollbackResponse
    {
        public int      code    { get; set; }
        public string   message { get; set; }
    }
    public class BetWinRequest
    {
        public string   agentID         { get; set; }
        public string   sign            { get; set; }
        public string   userID          { get; set; }
        public int      gameID          { get; set; }
        public decimal  betAmount       { get; set; }
        public decimal  winAmount       { get; set; }
        public string   transactionID   { get; set; }
        public string   roundID         { get; set; }
    }
}
