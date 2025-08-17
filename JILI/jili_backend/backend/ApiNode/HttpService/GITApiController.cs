using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net;
using Akka.Actor;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Akka.Util.Internal;
using Topshelf.Hosts;
using QueenApiNode.Database;

[assembly: OwinStartup(typeof(QueenApiNode.HttpService.GITApiController))]
namespace QueenApiNode.HttpService
{
    public class GITApiController : ApiController
    {
        [HttpGet]
        [Route("gamelist")]
        public async Task<HttpResponseMessage> doGetGameList()
        {
            try
            {
                if (!Request.Headers.Contains("Authorization"))
                    return Request.CreateResponse(HttpStatusCode.OK, new QueenResponse(ResponseCodes.InvalidApiToken, "invalid auth token"), Configuration.Formatters.JsonFormatter);

                string authToken  = Request.Headers.GetValues("Authorization").First();
                string strContent = await Request.Content.ReadAsStringAsync();

                string response = await ApiConfig.WorkActorGroup.Ask<string>(new ApiConsistentRequest(authToken, new GetGameListRequest()), TimeSpan.FromSeconds(10.0));
                var responseMessage = Request.CreateResponse(HttpStatusCode.OK);
                responseMessage.Content = new StringContent(response, Encoding.UTF8, "application/json");
                return responseMessage;
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new QueenResponse(ResponseCodes.GeneralError, "general error"), Configuration.Formatters.JsonFormatter);
            }
        }
              
        [HttpPost]
        [Route("userAuth")]
        public async Task<HttpResponseMessage> doUserAuth()
        {
            try
            {
                if (!Request.Headers.Contains("Authorization"))
                    return Request.CreateResponse(HttpStatusCode.OK, new QueenResponse(ResponseCodes.InvalidApiToken, "invalid auth token"), Configuration.Formatters.JsonFormatter);

                string authToken  = Request.Headers.GetValues("Authorization").First();
                string strContent = await Request.Content.ReadAsStringAsync();

                UserAuthRequest     request     = JsonConvert.DeserializeObject<UserAuthRequest>(strContent);
                string              response    = await ApiConfig.WorkActorGroup.Ask<string>(new ApiConsistentRequest(authToken, request), TimeSpan.FromSeconds(10.0));
                var responseMessage             = Request.CreateResponse(HttpStatusCode.OK);
                responseMessage.Content         = new StringContent(response, Encoding.UTF8, "application/json");
                return responseMessage;
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new QueenResponse(ResponseCodes.GeneralError, "general error"), Configuration.Formatters.JsonFormatter);
            }
        }
    }
    public enum ResponseCodes
    {
        OK                              = 0,
        WrongInputParameters            = 2,
        InvalidApiToken                 = 7,
        DuplicateUserID                 = 102,
        UserIDMismatchRule              = 103,
        InvalidUserID                   = 104,
        InvalidVendorID                 = 105,
        InvalidGameID                   = 106,
        NotEnoughAgentBalance           = 107,
        InvalidObjectID                 = 108,
        AgentDisabled                   = 109,
        RateLimited                     = 110,
        InvalidCategory                 = 111,
        InvalidAmount                   = 112,
        InvalidDateTimeFormat           = 113,
        GeneralError                    = 130,
    }
    public class QueenResponse
    {
        [JsonProperty(Order = 1)]
        public int      code    { get; set; }

        [JsonProperty(Order = 2)]
        public string   message { get; set; }

        public QueenResponse()
        {

        }
        public QueenResponse(ResponseCodes code, string message)
        {
            this.code       = (int) code;
            this.message    = message;
        }
    }    
    public class GetGameListResponse : QueenResponse
    {
        [JsonProperty(Order = 3)]
        public List<ApiGame> data { get; set; }

        public GetGameListResponse()
        {

        }
        public GetGameListResponse(List<ApiGame> data) : base(ResponseCodes.OK, "OK")
        {
            this.data = data;
        }
    }
    public class UserAuthResponse : QueenResponse
    {
        [JsonProperty(Order = 3)]
        public string url { get; set; }

        public UserAuthResponse()
        {

        }
        public UserAuthResponse(string url) : base(ResponseCodes.OK, "OK")
        {
            this.url = url;
        }
    }
    public class GetGameListRequest
    {
    }
    public class UserAuthRequest
    {
        public string agentID   { get; set; }
        public string userID    { get; set; }
        public int    gameid    { get; set; }
        public string lang      { get; set; }
        public string lobbyUrl  { get; set; }
        public UserAuthRequest()
        {
            lang        = "en";
            lobbyUrl    = "";
        }
    }
}

