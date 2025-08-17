using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using System.Net.Http;
using System.Net;
using Akka.Actor;
using System.Web;
using GITProtocol;
using System.Net.Http.Formatting;
using Akka.Routing;

[assembly: OwinStartup(typeof(ApiIntegration.HTTPService.CallbackApiController))]
namespace ApiIntegration.HTTPService
{
    public class CallbackApiController : ApiController
    {
        [HttpPost]
        [Route("GetBalance")]
        public async Task<HttpResponseMessage> doGetBalance(GetBalanceRequest request)
        {            
            try
            {
                GetBalanceResponse response = await HTTPServiceConfig.Instance.WorkerGroup.Ask<GetBalanceResponse>(request, TimeSpan.FromSeconds(10));
                return Request.CreateResponse(HttpStatusCode.OK, response, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception)
            {

            }
            return Request.CreateResponse(HttpStatusCode.OK, new GetBalanceResponse(1, "General Error", 0.0M), Configuration.Formatters.JsonFormatter);
        }

        [HttpPost]
        [Route("Withdraw")]
        public async Task<HttpResponseMessage> doWithdraw(WithdrawRequest request)
        {
            try
            {
                WithdrawResponse response = await HTTPServiceConfig.Instance.WorkerGroup.Ask<WithdrawResponse>(request, TimeSpan.FromSeconds(10));
                return Request.CreateResponse(HttpStatusCode.OK, response, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception)
            {

            }
            return Request.CreateResponse(HttpStatusCode.Unauthorized, new WithdrawResponse(1, "General Error"), Configuration.Formatters.JsonFormatter);
        }

        [HttpPost]
        [Route("Deposit")]
        public async Task<HttpResponseMessage> doDeposit(DepositRequest request)
        {
            try
            {
                DepositResponse response = await HTTPServiceConfig.Instance.WorkerGroup.Ask<DepositResponse>(request, TimeSpan.FromSeconds(10));
                return Request.CreateResponse(HttpStatusCode.OK, response, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {

            }
            return Request.CreateResponse(HttpStatusCode.Unauthorized, new DepositResponse(1, "General Error"), Configuration.Formatters.JsonFormatter);
        }
        
        [HttpPost]
        [Route("RollbackTransaction")]
        public async Task<HttpResponseMessage> doRollback(RollbackRequest request)
        {
            try
            {
                RollbackResponse response = await HTTPServiceConfig.Instance.WorkerGroup.Ask<RollbackResponse>(request, TimeSpan.FromSeconds(10));
                return Request.CreateResponse(HttpStatusCode.OK, response, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception)
            {

            }
            return Request.CreateResponse(HttpStatusCode.Unauthorized, new RollbackResponse(1, "General Error"), Configuration.Formatters.JsonFormatter);
        }
        
        [HttpPost]
        [Route("BetWin")]
        public async Task<HttpResponseMessage> doBetWin(BetWinRequest request)
        {
            try
            {
                BetWinResponse response = await HTTPServiceConfig.Instance.WorkerGroup.Ask<BetWinResponse>(request, TimeSpan.FromSeconds(10));
                return Request.CreateResponse(HttpStatusCode.OK, response, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception)
            {

            }
            return Request.CreateResponse(HttpStatusCode.Unauthorized, new BetWinResponse(1, "General Error"), Configuration.Formatters.JsonFormatter);
        }
    }

    public class GetBalanceRequest : IConsistentHashable
    {
        public string agentID   { get; set; }
        public string sign      { get; set; }
        public string userID    { get; set; }
        public int    gameID    { get; set; }

        public object ConsistentHashKey => string.Format("{0}_{1}", agentID, userID);
    }
    public class GetBalanceResponse
    {
        public int      code    { get; set; }
        public string   message { get; set; }
        public decimal  balance { get; set; }

        public GetBalanceResponse()
        {

        }
        public GetBalanceResponse(int code, string message, decimal balance)
        {
            this.code       = code;
            this.message    = message;
            this.balance    = balance;
        }
    }
    public class WithdrawRequest : IConsistentHashable
    {
        public string   agentID         { get; set; }
        public string   sign            { get; set; }
        public string   userID          { get; set; }
        public decimal  amount          { get; set;}
        public string   transactionID   { get; set; }
        public string   roundID         { get; set; }
        public int      gameID          { get; set; }

        public object   ConsistentHashKey => string.Format("{0}_{1}", agentID, userID);
    }
    public class WithdrawResponse
    {
        public int      code                    { get; set; }
        public string   message                 { get; set; }
        public string   platformTransactionID   { get; set; }
        public decimal  balance                 { get; set; }

        public WithdrawResponse()
        {

        }
        public WithdrawResponse(int code, string message)
        {
            this.code = code;
            this.message = message;
        }
    }
    public class DepositRequest : IConsistentHashable
    {
        public string   agentID             { get; set; }
        public string   sign                { get; set; }
        public string   userID              { get; set; }
        public decimal  amount              { get; set; }
        public string   transactionID       { get; set; }
        public string   refTransactionID    { get; set; }
        public string   roundID             { get; set; }
        public int      gameID              { get; set; }
        public bool     endRound            { get; set; }

        public object   ConsistentHashKey => string.Format("{0}_{1}", agentID, userID);
    }
    public class DepositResponse
    {
        public int      code                    { get; set; }
        public string   message                 { get; set; }
        public string   platformTransactionID   { get; set; }
        public decimal  balance                 { get; set; }

        public DepositResponse()
        {

        }
        public DepositResponse(int code, string message)
        {
            this.code       = code;
            this.message    = message;
        }
    }
    public class RollbackRequest : IConsistentHashable
    {
        public string   agentID             { get; set; }
        public string   sign                { get; set; }
        public string   userID              { get; set; }
        public string   refTransactionID    { get; set; }
        public int      gameID              { get; set; }
        public object   ConsistentHashKey => string.Format("{0}_{1}", agentID, userID);
    }
    public class RollbackResponse
    {
        public int      code    { get; set; }
        public string   message { get; set; }

        public RollbackResponse()
        {

        }
        public RollbackResponse(int code, string message)
        {
            this.code       = code;
            this.message    = message;
        }
    }
    public class BetWinRequest : IConsistentHashable
    {
        public string   agentID         { get; set; }
        public string   sign            { get; set; }
        public string   userID          { get; set; }
        public decimal  betAmount       { get; set; }
        public decimal  winAmount       { get; set; }
        public string   transactionID   { get; set; }
        public string   roundID         { get; set; }
        public int      gameID          { get; set; }

        public object   ConsistentHashKey => string.Format("{0}_{1}", agentID, userID);
    }
    public class BetWinResponse
    {
        public int      code                    { get; set; }
        public string   message                 { get; set; }
        public string   platformTransactionID   { get; set; }
        public decimal  balance                 { get; set; }

        public BetWinResponse()
        {

        }
        public BetWinResponse(int code, string message)
        {
            this.code       = code;
            this.message    = message;
        }
    }
    public enum CallBackResponseCodes
    {
        OK                      = 0,
        GeneralError            = 1,
        WrongInputParam         = 2,
        InvalidSign             = 3,
        InvalidAgent            = 4,
        UserIDNotFound          = 5,
        InsufficientFunds       = 6,
        InvalidAPIToken         = 7,
        CanNotFindRefTransId    = 8,
        AlreadyRolledBack       = 9,
        DuplicateTransaction    = 11,
    }
}
