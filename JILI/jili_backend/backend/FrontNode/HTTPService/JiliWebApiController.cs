using Akka.Actor;
using Akka.Configuration.Hocon;
using Akka.Event;
using Akka.IO;
using FrontNode.HTTPService.Models;
using GITProtocol;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Jili.Protocols.Aa;
using Jili.Protocols.Common;
using Jili.Protocols.DebrisProto;
using Jili.Protocols.LonghuProto;
using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using WebSlotFd.Protocols.JiliJpProto;

using ProtoRequest = Jili.Protocols.Common.Request;

[assembly: OwinStartup(typeof(FrontNode.HTTPService.JiliWebApiController))]

public class MissionInfoContent
{
    public string Desc { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public string StartTimestamp { get; set; }
    public string EndTimestamp { get; set; }
    public string CurrentDate { get; set; }
    public int OverDay { get; set; }
    public int OverDayMin { get; set; }
    public List<Mission> DailyMissionList { get; set; }
    public Dictionary<string, ItemInfo> ItemInfo { get; set; }
}

public class Mission
{
    public string Date { get; set; }
    public string ConditionDesc { get; set; }
    public double CurrentCount { get; set; }
    public double TotalCount { get; set; }
    public long ItemID { get; set; }
    public int ItemCount { get; set; }
    public bool Finished { get; set; }
    public int Vip { get; set; }
    public int Type { get; set; }
    public double Coin { get; set; }
    public int Show { get; set; }
    public bool Lock { get; set; }
    public int GameID { get; set; }
    public int ChainID { get; set; }
    public int MissionIndex { get; set; }
    public int ExtendID { get; set; }
}

public class ItemInfo
{
    public int GameID { get; set; }
    public string Game { get; set; }
    public string ItemName { get; set; }
    public int Icon { get; set; }
    public double ItemValue { get; set; }
    public double Bet { get; set; }
    public string GameName { get; set; }
    public int Star { get; set; }
    public int WinType { get; set; }
    public double WinMultiplier { get; set; }
    public double WinMaxMag { get; set; }
    public double PlayValue { get; set; }
}

namespace FrontNode.HTTPService
{
    [RoutePrefix("")]
    public class JiliWebApiController : ApiController
    {

        [HttpPost]
        [Route("sso-login.api")]
        public async Task<HttpResponseMessage> getSsoLoginApi()
        {
            var formData = await Request.Content.ReadAsFormDataAsync();

            Dictionary<string, string> dicParams = new Dictionary<string, string>();

            var query = Request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value);
            string key = query.ContainsKey("key") ? query["key"] : null;
            string lang = query.ContainsKey("lang") ? query["lang"] : null;

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(lang))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Missing required form fields.");
            }

            // 받은 key (ssoKey)
            byte[] encryptedBytes = System.Web.HttpServerUtility.UrlTokenDecode(key);

            // 복호화
            byte[] decryptedBytes = Crypto.DecryptAesCbc(encryptedBytes, FrontConfig.FrontTokenKey);

            // UTF-8 문자열로 변환
            string strTokenData = Encoding.UTF8.GetString(decryptedBytes);

            // 언더스코어("_")로 최대 5번만 split
            string[] strParts = strTokenData.Split(new[] { '_' }, 6);

            string strIPAddress = Request.GetOwinContext().Request.RemoteIpAddress;
            try
            {
                if (strParts.Length != 6)
                {
                    return new HttpResponseMessage() { Content = new StringContent("token is invalid") };
                }

                int agentID = int.Parse(strParts[0]);
                string strUserID = strParts[1];
                string strSymbol = strParts[2];
                string strPassword = strParts[3];
                string gameID = strParts[4];
                string expireTime = strParts[5];

                dicParams.Add("agentID", agentID.ToString());
                dicParams.Add("userID", strUserID);
                dicParams.Add("symbol", strSymbol);
                dicParams.Add("password", strPassword);
                dicParams.Add("gameID", gameID);
                dicParams.Add("expireTime", expireTime);

                HTTPAuthResponse response = await HTTPServiceConfig.Instance.AuthWorkerGroup.Ask<HTTPAuthResponse>(new HTTPAuthRequest(agentID, strUserID, strPassword, strIPAddress, strSymbol), TimeSpan.FromSeconds(20));


                if (response.Result == HttpAuthResults.OK)
                {
                    var responseJson = new
                    {
                        certArea = 0.6,
                        certId = 0,
                        platformVersion = "thai_release_1.0.80",
                        lobbyMode = 0,
                        disableFullScreen = 0,
                        country = "Russia",
                        clientApiParam = new { },
                        itaAuthId = "",
                        thousandthMode = "",
                        homeUrl = "",    // 새 JSON 값 적용
                        linecode = 0,
                        profile = new
                        {
                            id = "",
                            aid = 400224312,
                            apiId = 400040165,
                            transactionMode = 0,
                            subAgentCode = 0,
                            isLobbyOpen = false,
                            meta = new { agentAccount = "demo_usd@api-400040165.game" },
                            platform = "web",
                            lobbyMode = 0,
                            switchOffs = new[] { 26, 27, 43, 44, 49, 54 },
                            wallets = (object)null,
                            nickname = "demo_154_44_186_57",
                            newNickname = "",
                            siteId = 648008459,
                            account = "demo_154_44_186_57@api-400040165.game",
                            coin = 10000,
                            isJPEnabled = 0,
                            linecode = 0,
                            prefix = "",
                            clientMode = new[] {
                                new { eventId = 8, value = new[] { 3600 } }
                            },
                            betLevel = -1,
                            license = 0,
                            isGiftCodeOpen = false,
                            freeSpinBetValue = 0,
                            apiType = 0,
                            walletType = 1
                        },
                        //token = "1d355bb2a3d042c24312abd507c83dd4862daaf2",
                        token = response.SessionToken,
                        response = new
                        {
                            error = 0,
                            message = "",
                            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()   // 혹은 1752413472 사용
                        },
                    };

                    HTTPEnterGameResults enterGameResult = await procEnterGame(agentID, strUserID, response.SessionToken, strSymbol);
                    if (enterGameResult != HTTPEnterGameResults.OK)
                    {
                        return new HttpResponseMessage() { Content = new StringContent("unlogged") };
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, responseJson);
                }
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Missing required form fields.");

            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Missing required form fields.");
            }
        }


        [HttpGet]
        [Route("webservice/event/trigger")]
        public HttpResponseMessage getTrigger()
        {
            // 요청 파라미터들을 읽고 로깅하거나 무시해도 됨
            var queryParams = Request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value);

            // 예: 필요한 값이 있다면 추출 가능
            string gameId = queryParams.ContainsKey("GameID") ? queryParams["GameID"] : null;
            string eventId = queryParams.ContainsKey("EventID") ? queryParams["EventID"] : null;
            string ssoKey = queryParams.ContainsKey("SSOKey") ? queryParams["SSOKey"] : null;

            // 로그 출력이나 검증, 저장 등의 처리 가능 (원한다면)

            // 빈 JSON 객체 `{}` 응답
            return Request.CreateResponse(HttpStatusCode.OK, new { });
        }

        [HttpPost]
        [Route("vipservice/V2/VIPGet")]
        public HttpResponseMessage VIPGet()
        {
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [HttpGet]
        [Route("webservice/event/user")]
        public HttpResponseMessage EventUser()
        {
            var eventUser = new { };

            return Request.CreateResponse(HttpStatusCode.OK, eventUser);
        }


        [HttpGet]
        [Route("webservice/event/loading")]
        public HttpResponseMessage getLoading()
        {
            // 요청 파라미터들을 읽고 로깅하거나 무시해도 됨
            var queryParams = Request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value);

            // 예: 필요한 값이 있다면 추출 가능
            string gameId = queryParams.ContainsKey("GameID") ? queryParams["GameID"] : null;
            string eventId = queryParams.ContainsKey("EventNo") ? queryParams["EventNo"] : null;
            string ssoKey = queryParams.ContainsKey("SSOKey") ? queryParams["SSOKey"] : null;

            // 로그 출력이나 검증, 저장 등의 처리 가능 (원한다면)

            // 빈 JSON 객체 `{}` 응답
            return Request.CreateResponse(HttpStatusCode.OK, new { });
        }

        [HttpPost]
        [Route("subagentservice/V2/MakeUserSubAgent")]
        public HttpResponseMessage getSubAgentService()
        {
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("smartnotice/notice/getReqV2")]
        public HttpResponseMessage getReqV2()
        {
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }


        [HttpPost]
        [HttpGet]
        [Route("promotionservice/OnLoginV3")]
        public HttpResponseMessage promoOnLoginV3()
        {
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [HttpGet]
        [Route("longhu/GetLonghuListV4")]
        public async Task<HttpResponseMessage> getLonghuListV4()
        {

            byte[] body = await Request.Content.ReadAsByteArrayAsync();
            Console.WriteLine("Raw bytes: " + BitConverter.ToString(body).Replace("-", ""));

            // ✅ Request proto 파싱
            var reqWrapper = ProtoRequest.Parser.ParseFrom(body);
            Console.WriteLine("Decoded GetlonghuListRequest wrapper:");
            Console.WriteLine(reqWrapper.ToString());

            var getLonghuListRequestBytes = reqWrapper.Req.ToByteArray();
            var getLonghuListRequestReq = GetLonghuListRequest.Parser.ParseFrom(getLonghuListRequestBytes);
            Console.WriteLine("Decoded InfoReq:");
            Console.WriteLine(getLonghuListRequestReq.ToString());

            var longhuListResponseV3 = BuildLonghuList();

            // GaiaResponse 직렬화
            var responseBytes = longhuListResponseV3.ToByteArray();

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(responseBytes)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-protobuf");

            return response;
        }

        [HttpGet]
        [Route("rankingservice/user/GetRankingListV2")]
        public HttpResponseMessage GetRankingListV2()
        {
            // 요청 파라미터들을 읽고 로깅하거나 무시해도 됨
            var queryParams = Request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value);

            // 예: 필요한 값이 있다면 추출 가능
            string GameId = queryParams.ContainsKey("GameId") ? queryParams["GameId"] : null;
            string Games = queryParams.ContainsKey("Games") ? queryParams["Games"] : null;
            string ApiId = queryParams.ContainsKey("ApiId") ? queryParams["ApiId"] : null;
            string Type = queryParams.ContainsKey("Type") ? queryParams["Type"] : null;
            string AccountId = queryParams.ContainsKey("AccountId") ? queryParams["AccountId"] : null;
            string siteId = queryParams.ContainsKey("siteId") ? queryParams["siteId"] : null;
            string SubAgentCode = queryParams.ContainsKey("SubAgentCode") ? queryParams["SubAgentCode"] : null;
            string Lang = queryParams.ContainsKey("Lang") ? queryParams["Lang"] : null;

            var responseObj = new
            {
                    Data = (object)null,
                    Error = "ranking not exist or begin",
                    ExtraWebView = 0,
                    NeedWebView = false
            };

            return Request.CreateResponse(HttpStatusCode.OK, responseObj);
        }

        [HttpGet]
        [Route("favoriteservice/OnLogin")]
        public HttpResponseMessage favOnLogin()
        {
            // 요청 파라미터들을 읽고 로깅하거나 무시해도 됨
            var queryParams = Request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value);

            // 예: 필요한 값이 있다면 추출 가능
            string apiId = queryParams.ContainsKey("apiId") ? queryParams["apiId"] : null;
            string accountId = queryParams.ContainsKey("accountId") ? queryParams["accountId"] : null;
            string logingameId = queryParams.ContainsKey("logingameId") ? queryParams["logingameId"] : null;
            string alterId = queryParams.ContainsKey("alterId") ? queryParams["alterId"] : null;
            string token = queryParams.ContainsKey("token") ? queryParams["token"] : null;

            var responseObj = new
            {
                cmdType = 4,
                content = new
                {
                    BigWined = false,
                    DAU = (object)null,
                    Enabled = true,
                    Expired = (object)null,
                    Favorites = (object)null,
                    Promotions = (object)null
                }
            };

            return Request.CreateResponse(HttpStatusCode.OK, responseObj);
        }

        [HttpPost]
        [HttpGet]
        [Route("{gamecode}/req")]
        public async Task<HttpResponseMessage> GameReq(string gamecode)
        {

            Dictionary<string, string> dicParams = new Dictionary<string, string>();

            Console.WriteLine($"Received POST request to /{gamecode}/req");

            AckType reqAck = AckType.Spin; // default value

            foreach (KeyValuePair<string, string> pair in Request.GetQueryNameValuePairs())
            {
                if (pair.Key == "D")
                    if (int.TryParse(pair.Value, out int value))
                    {
                        if (System.Enum.IsDefined(typeof(AckType), value))
                            reqAck = (AckType)value;
                    }
            }

            Console.WriteLine("Request AckType ===> D === " + reqAck);

            byte[] body = await Request.Content.ReadAsByteArrayAsync();
            Console.WriteLine("Raw bytes: " + BitConverter.ToString(body).Replace("-", ""));

            string token = Request.Headers.GetValues("token").FirstOrDefault();
            try
            {
                // ✅ Request proto 파싱
                var reqWrapper = ProtoRequest.Parser.ParseFrom(body);
                Console.WriteLine("Decoded Request wrapper:");
                Console.WriteLine(reqWrapper.ToString());

                Console.WriteLine("Request AckType enum: " + reqWrapper.Ack);
                Console.WriteLine("AckType value as int: " + (int)reqWrapper.Ack);


                if (reqWrapper.Ack == AckType.Info)
                {
                    Console.WriteLine("Received info request");
                }

                string strGlobalUserID = findUserIDFromToken(token);
                string strUserID = strGlobalUserID.Split('_')[1];
                int agentID = int.Parse(strGlobalUserID.Split('_')[0]);
                if (strUserID == null)
                    return new HttpResponseMessage() { Content = new StringContent("unlogged") };

                byte[] encryptedBytes = null;

                if ((int)reqWrapper.Ack == 55)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent);
                }

                GITMessage requestMessage = null;

                switch (reqWrapper.Ack)
                {
                    case AckType.Spin:             // 0
                        var spinReqBytes = reqWrapper.Req.ToByteArray();
                        var spinReq = SpinReq.Parser.ParseFrom(spinReqBytes);
                        Console.WriteLine($"Decoded SpinReq=============>:{spinReq.ToString()}");

                        requestMessage = buildSpinMessage(spinReq);

                        try
                        {
                            SpinResponseData spinResponse = await HTTPServiceConfig.Instance.WorkerGroup.Ask<SpinResponseData>(new FromHTTPSessionMessage(agentID, strUserID, token, requestMessage), TimeSpan.FromSeconds(20));
                            var spinAck = BuildSpinAck(spinResponse.spindata, gamecode);
                            encryptedBytes = EncryptAck(spinAck, token);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }

                        Console.WriteLine("AckType: Spin");
                        break;

                    case AckType.Info:             // 1
                        var infoReqBytes = reqWrapper.Req.ToByteArray();
                        var infoReq = InfoReq.Parser.ParseFrom(infoReqBytes);
                        Console.WriteLine("Decoded InfoReq:");
                        Console.WriteLine(infoReq.ToString());

                        requestMessage = buildDoInitMessage(infoReq);

                        try
                        {
                            SpinResponseData spinResponse = await HTTPServiceConfig.Instance.WorkerGroup.Ask<SpinResponseData>(new FromHTTPSessionMessage(agentID, strUserID, token, requestMessage), TimeSpan.FromSeconds(20));
                            var spinAck = BuildInfoAck(spinResponse.spindata,gamecode);
                            encryptedBytes = EncryptAck(spinAck, token);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }

                        break;

                    case AckType.Notice:           // 2
                        var noticeReqBytes = reqWrapper.Req.ToByteArray();
                        var noticeReq = NoticeReq.Parser.ParseFrom(noticeReqBytes);
                        Console.WriteLine("Decoded InfoReq:");
                        Console.WriteLine(noticeReq.ToString());

                        var noticeAck = BuildNoticeAck();
                        encryptedBytes = EncryptAck(noticeAck, token);

                        Console.WriteLine("AckType: Spin");
                        break;

                    case AckType.Logout:           // 3
                        Console.WriteLine("Logout processing login");
                        break;

                    case AckType.LastSpin:         // 4
                        Console.WriteLine("AckType: LastSpin");
                        break;

                    case AckType.GetBackPack:      // 5
                        Console.WriteLine("AckType: GetBackPack");

                        var backPackReqBytes = reqWrapper.Req.ToByteArray();
                        var backPackReq = BackPackInfo.Parser.ParseFrom(backPackReqBytes);
                        Console.WriteLine("Decoded GetBackPackReq:");
                        Console.WriteLine(backPackReq.ToString());

                        var backPackAck = BuildBackPackAck();
                        encryptedBytes = EncryptAck(backPackAck, token);

                        Console.WriteLine("AckType: GetBackPack");
                        break;

                    case AckType.CheckItem:        // 6
                        Console.WriteLine("AckType: CheckItem");
                        break;

                    case AckType.GetMail:          // 7
                        Console.WriteLine("AckType: GetMail");
                        var GetMailReqBytes = reqWrapper.Req.ToByteArray();
                        var mailReq = MailReq.Parser.ParseFrom(GetMailReqBytes);
                        Console.WriteLine("Decoded GetBackPackReq:");
                        Console.WriteLine(mailReq.ToString());

                        var MailAck = BuildMailAck();
                        encryptedBytes = EncryptAck(MailAck, token);
                        Console.WriteLine("AckType: GetMail");
                        break;

                    case AckType.ReadMail:         // 8
                        Console.WriteLine("AckType: ReadMail");
                        break;

                    case AckType.GetDailyMission:  // 9
                        Console.WriteLine("AckType: GetDailyMission");
                        break;

                    case AckType.GetNowMission:    // 10

                        Console.WriteLine("AckType: GetNowMission");
                        var getNowMissionBytes = reqWrapper.Req.ToByteArray();
                        var getNowMissionReq = DailyMissionInfo.Parser.ParseFrom(getNowMissionBytes);
                        Console.WriteLine("Decoded FullJpInfo:");
                        Console.WriteLine(getNowMissionReq.ToString());

                        var getNowMissionAck = BuildGetNowMissionAck();
                        encryptedBytes = EncryptAck(getNowMissionAck, token);

                        break;

                    case AckType.CustomAck:        // 11
                        Console.WriteLine("AckType: CustomAck");
                        break;

                    case AckType.CheckSignUpStatus: // 12
                        Console.WriteLine("AckType: CheckSignUpStatus");
                        break;

                    case AckType.TrialInfo:       // 14
                        Console.WriteLine("AckType: TrialInfo");
                        break;

                    case AckType.TrialSignUp:     // 16
                        Console.WriteLine("AckType: TrialSignUp");
                        break;

                    case AckType.TrialGiveUp:     // 17
                        Console.WriteLine("AckType: TrialGiveUp");
                        break;

                    case AckType.GetAllItemCanUse: // 18
                        Console.WriteLine("AckType: GetAllItemCanUse");
                        var getAllItemCanUseBytes = reqWrapper.Req.ToByteArray();
                        var getAllItemCanUseReq = CheckUseItemReq.Parser.ParseFrom(getAllItemCanUseBytes);
                        Console.WriteLine("Decoded FullJpInfo:");
                        Console.WriteLine(getAllItemCanUseReq.ToString());

                        var getAllItemCanUseAck = BuildAllItemCanUse();
                        encryptedBytes = EncryptAck(getAllItemCanUseAck, token);
                        break;

                    case AckType.VipSignInfo:     // 19

                        Console.WriteLine("AckType: VipSignInfo");
                        var vipSignInfoBytes = reqWrapper.Req.ToByteArray();
                        var vipSignInfoReq = VipSignInfoResp.Parser.ParseFrom(vipSignInfoBytes);
                        Console.WriteLine("Decoded FullJpInfo:");
                        Console.WriteLine(vipSignInfoReq.ToString());

                        var vipSigninfoAck = BuildVipSignInfo();
                        encryptedBytes = EncryptAck(vipSigninfoAck, token);

                        break;

                    case AckType.VipSignReward:   // 20
                        Console.WriteLine("AckType: VipSignReward");
                        break;

                    case AckType.JpInfo:          // 21

                        Console.WriteLine("AckType: JpInfo");
                        var jpInfoBytes = reqWrapper.Req.ToByteArray();
                        var jpInfoReq = JpInfo.Parser.ParseFrom(jpInfoBytes);
                        Console.WriteLine("Decoded JpInfo:");
                        Console.WriteLine(jpInfoReq.ToString());

                        var jpInfoAck = BuildJpInfoAck();
                        encryptedBytes = EncryptAck(jpInfoAck, token);
                        break;

                    case AckType.JpHistory:       // 22
                        Console.WriteLine("AckType: JpHistory");
                        break;

                    case AckType.LotteryPlayerInfo: // 23
                        Console.WriteLine("AckType: LotteryPlayerInfo");
                        break;

                    case AckType.LotteryReward:   // 24
                        Console.WriteLine("AckType: LotteryReward");
                        break;

                    case AckType.FullJpInfo:      // 25

                        Console.WriteLine("AckType: FullJpInfo");
                        var fullJpInfoInfoBytes = reqWrapper.Req.ToByteArray();
                        var fullJpInfoReq = JpInfo.Parser.ParseFrom(fullJpInfoInfoBytes);
                        Console.WriteLine("Decoded FullJpInfo:");
                        Console.WriteLine(fullJpInfoReq.ToString());

                        var fullJpInfoAck = BuildJpInfoAck();
                        encryptedBytes = EncryptAck(fullJpInfoAck, token);

                        break;

                    case AckType.FullJpHistory:   // 26
                        Console.WriteLine("AckType: FullJpHistory");
                        break;

                    case AckType.FullJpInfoAll:   // 27

                        Console.WriteLine("AckType: FullJpInfoAll");
                        var fullJpInfoAllBytes = reqWrapper.Req.ToByteArray();
                        var fullJpInfoAllReq = JpInfo.Parser.ParseFrom(fullJpInfoAllBytes);
                        Console.WriteLine("Decoded FullJpInfo:");
                        Console.WriteLine(fullJpInfoAllReq.ToString());

                        var fullJpInfoAllAck = BuildJpInfoAllAck();
                        encryptedBytes = EncryptAck(fullJpInfoAllAck, token);
                        break;

                    case AckType.DebrisInfo:      // 29
                        Console.WriteLine("AckType: DebrisInfo");

                        var debrisInfoBytes = reqWrapper.Req.ToByteArray();
                        var debrisInfoReq = DebrisReq.Parser.ParseFrom(debrisInfoBytes);
                        Console.WriteLine("Decoded FullJpInfo:");
                        Console.WriteLine(debrisInfoReq.ToString());

                        var debrisInfoAck = BuildDebrisInfoAck();
                        encryptedBytes = EncryptAck(debrisInfoAck, token);
                        break;

                    case AckType.DebrisPlayerInfo: // 30
                        Console.WriteLine("AckType: DebrisPlayerInfo");
                        break;

                    case AckType.DebrisGet:       // 31
                        Console.WriteLine("AckType: DebrisGet");
                        break;

                    case AckType.DebrisExchange:  // 32
                        Console.WriteLine("AckType: DebrisExchange");
                        break;

                    case AckType.JpShowData:      // 33
                        Console.WriteLine("AckType: JpShowData");
                        var jpShowDataBytes = reqWrapper.Req.ToByteArray();
                        var jpShowDataReq = JpInfo.Parser.ParseFrom(jpShowDataBytes);
                        Console.WriteLine("Decoded JpShowData:");
                        Console.WriteLine(jpShowDataReq.ToString());

                        var jpShowDataAck = BuildJpShowDataAck();
                        encryptedBytes = EncryptAck(jpShowDataAck, token);

                        break;

                    case AckType.GetPlayerLevel:  // 34
                        Console.WriteLine("AckType: GetPlayerLevel");
                        break;

                    case AckType.Unshow:          // 36
                        Console.WriteLine("AckType: Unshow");
                        break;

                    case AckType.Spinend:         // 37
                        Console.WriteLine("AckType: Spinend");
                        break;

                    case AckType.Mallinfo:        // 38
                        Console.WriteLine("AckType: Mallinfo");
                        break;

                    case AckType.Giftcodeverify:  // 39
                        Console.WriteLine("AckType: Giftcodeverify");
                        break;

                    case AckType.Giftcodecheckblock: // 40
                        Console.WriteLine("AckType: Giftcodecheckblock");
                        break;

                    case AckType.FreeAbandon:     // 41
                        Console.WriteLine("AckType: FreeAbandon");
                        break;

                    case AckType.FreeGetAll:      // 42
                        Console.WriteLine("AckType: FreeGetAll");
                        break;

                    case AckType.FreeGetHistory:  // 43
                        Console.WriteLine("AckType: FreeGetHistory");
                        break;

                    case AckType.FreeBonusReward: // 44
                        Console.WriteLine("AckType: FreeBonusReward");
                        break;

                    case AckType.BuffGetBack:     // 45
                        Console.WriteLine("AckType: BuffGetBack");
                        break;

                    case AckType.BuffGetNow:      // 46
                        Console.WriteLine("AckType: BuffGetNow");
                        break;

                    case AckType.BuffUseReport:   // 47
                        Console.WriteLine("AckType: BuffUseReport");
                        break;

                    case AckType.BuffGetParam:    // 48
                        Console.WriteLine("AckType: BuffGetParam");
                        break;

                    case AckType.GetMission2:     // 49
                        return Request.CreateResponse(HttpStatusCode.NoContent);
                    //Console.WriteLine("AckType: GetMission2");
                    //break;

                    case AckType.JilijpHistory:   // 50
                        Console.WriteLine("AckType: JilijpHistory");
                        break;

                    case AckType.JilijpSetting:   // 51
                        Console.WriteLine("Processing JilijpSetting request");
                        var jiliJpSetting = new JiliJpSetting
                        {
                            IsOpen = true,
                        };
                        jiliJpSetting.Win.AddRange(new List<double> { });
                        jiliJpSetting.MinBet.AddRange(new List<double> { });

                        byte[] jiliJpSettingBytes = jiliJpSetting.ToByteArray();
                        encryptedBytes = Crypto.EncryptAesCbc(jiliJpSettingBytes, token);
                        break;

                    case AckType.JilijpRecentWin: // 52
                        Console.WriteLine("AckType: JilijpRecentWin");
                        break;

                    case AckType.FreeGetAutoInfo: // 53
                        Console.WriteLine("AckType: FreeGetAutoInfo");
                        break;

                    case AckType.JilijpGameList:  // 54
                        Console.WriteLine("AckType: JilijpGameList");
                        break;

                    default:
                        Console.WriteLine("Unknown Request AckType: " + reqAck);
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid AckType");
                }

                

                if (encryptedBytes == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No encrypted response generated");
                }

                var gaiaResponse = new GaiaResponse
                {
                    Type = (int)reqWrapper.Ack,

                    Data = Google.Protobuf.ByteString.CopyFrom(encryptedBytes),
                    Now = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
                };


                // GaiaResponse 직렬화
                var responseBytes = gaiaResponse.ToByteArray();

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(responseBytes)
                };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-protobuf");

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse protobuf: " + ex.Message);
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid protobuf");
            }
        }

        [HttpGet]
        [Route("rankingservice/user/GetMailList")]
        public HttpResponseMessage GetMailList()
        {
            var queryParams = Request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value);

            string accountId = queryParams.ContainsKey("AccountId") ? queryParams["AccountId"] : null;
            string lang = queryParams.ContainsKey("Lang") ? queryParams["Lang"] : null;
            string token = queryParams.ContainsKey("token") ? queryParams["token"] : null;

            // 빈 JSON 객체
            return Request.CreateResponse(HttpStatusCode.OK, new { Mails = new object[] { }, Error = "" });
        }

        private static GameInfoAck BuildInfoAck(string data, string gamecode)
        {
            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            GameInfoAck gameAct = parser.Parse<GameInfoAck>(data);
            //string token = "78636b549ca049f1a8a3787c5fd785e4ce87ebe6";
            //byte[] temp111 = new byte[] { 122, 215, 252, 69, 85, 139, 203, 1, 247, 94, 26, 91, 10, 253, 209, 112, 103, 99, 128, 233, 76, 66, 226, 68, 231, 152, 162, 13, 145, 143, 108, 100, 68, 49, 150, 26, 73, 165, 46, 139, 5, 35, 201, 232, 118, 21, 131, 225, 66, 245, 199, 228, 19, 1, 71, 159, 31, 165, 78, 82, 57, 93, 213, 156, 242, 181, 77, 123, 99, 48, 89, 25, 16, 186, 107, 195, 76, 171, 230, 64, 251, 58, 39, 4, 161, 241, 208, 76, 122, 196, 156, 79, 135, 0, 123, 166, 174, 17, 73, 50, 14, 127, 195, 225, 81, 43, 59, 168, 149, 31, 220, 0, 12, 208, 137, 68, 89, 132, 170, 17, 101, 46, 79, 227, 62, 151, 207, 112, 106, 219, 135, 32, 2, 232, 50, 193, 208, 72, 212, 122, 171, 48, 72, 18, 17, 208, 248, 150, 159, 243, 187, 45, 120, 238, 108, 236, 213, 221, 83, 93, 65, 189, 153, 118, 31, 81, 238, 53, 94, 3, 177, 182, 192, 116, 84, 38, 198, 155, 34, 139, 249, 103, 10, 141, 23, 117, 198, 208, 220, 198, 201, 212, 27, 144, 10, 90, 28, 188, 232, 173, 3, 116, 232, 131, 213, 223, 195, 249, 89, 166, 64, 62, 164, 167, 167, 59, 4, 27, 197, 213, 139, 146, 98, 219, 193, 125, 4, 1, 245, 247, 86, 161, 248, 31, 195, 241, 98, 184, 116, 137, 40, 46, 254, 66, 122, 99, 37, 169, 125, 120, 165, 150, 53, 189, 22, 221, 191, 248, 164, 118, 215, 175, 236, 229, 58, 119, 98, 36, 176, 167, 241, 78, 178, 218, 100, 248, 252, 167, 239, 28, 255, 118, 26, 190, 102, 230, 139, 53, 152, 104, 199, 26, 15, 1, 178, 108, 241, 252, 68, 187, 216, 36, 97, 216, 186, 195, 175, 241, 48, 237, 244, 153, 224, 175, 37, 247, 150, 161, 163, 2 };
            //byte[] IV = {122,22,118,142,146,198,37,121,50,161,152,32,53,106,35,42};
            //byte[] key = Encoding.UTF8.GetBytes(token.Substring(0, 32));
            //var actTemp = Crypto.DecryptAesCbc(temp111, key, IV);
            //GameInfoAck ttt = GameInfoAck.Parser.ParseFrom(actTemp);
            //gameAct.WalletInfo = ttt.WalletInfo;
            return gameAct;
        }

        private static SpinResponse BuildSpinAck(string spinData, string type)
        {
            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            byte[] data = null;
            double totalWin = 0;
            double postMoney = 0;
            long showIndex = 0;
            switch (type)
            {
                case "saj":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Saj.SpinAck>(spinData);
                        data = spinAck.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.NowMoney;
                        showIndex = Convert.ToInt64(spinAck.ShowIndex.Replace("-", ""));
                    }
                    break;
                case "wa":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Wa.SpinAck>(spinData);
                        data = spinAck.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.NowMoney;
                        showIndex = Convert.ToInt64(spinAck.ShowIndex.Replace("-", ""));
                    }
                    break;
                case "fullhouse":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Fullhouse.FullHouseSpinAck>(spinData);
                        data = spinAck.AllPlate.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.PostMoney;

                        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        Random random = new Random();
                        int index = random.Next(1, 9999);
                        showIndex = timestamp * 1000000 + index * 1000 + 49;
                    }
                    break;
                case "fullhouse3":
                    {
                        JObject dataSpin = JObject.Parse(spinData);
                        var spinAck = parser.Parse<Jili.Protocols.Fullhouse3.SpinAck>(dataSpin["allPlate"]?.ToString(Formatting.None));
                        data = spinAck.ToByteArray();
                        totalWin = dataSpin["totalWin"].Value<double>();
                        postMoney = dataSpin["postMoney"].Value<double>();
                        showIndex = dataSpin["roundIndex"].Value<long>();
                    }
                    break;
                case "ols2":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Ols2.SpinAck>(spinData);
                        data = spinAck.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.NowMoney;
                        showIndex = Convert.ToInt64(spinAck.ShowIndex.Replace("-", ""));
                    }
                    break;
                case "fgp":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Fgp.SpinAck>(spinData);
                        data = spinAck.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.NowMoney;
                        showIndex = Convert.ToInt64(spinAck.ShowIndex.Replace("-", ""));
                    }
                    break;
                case "fg3":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Fg3.SpinAck>(spinData);
                        data = spinAck.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.NowMoney;
                        showIndex = Convert.ToInt64(spinAck.ShowIndex.Replace("-", ""));
                    }
                    break;
                case "mw4":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Mw4.SpinAck>(spinData);
                        data = spinAck.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.NowMoney;
                        showIndex = Convert.ToInt64(spinAck.ShowIndex.Replace("-", ""));
                    }
                    break;
                case "ps":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Ps.SpinAck>(spinData);
                        data = spinAck.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.NowMoney;
                        showIndex = Convert.ToInt64(spinAck.ShowIndex.Replace("-", ""));
                    }
                    break;

                case "pirate":
                    {
                        var spinAck = parser.Parse<Jili.Protocols.Pirate.SpinAck>(spinData);
                        data = spinAck.ToByteArray();
                        totalWin = spinAck.TotalWin;
                        postMoney = spinAck.NowMoney;
                        showIndex = Convert.ToInt64(spinAck.ShowIndex.Replace("-", ""));
                    }
                    break;
                default:
                    break;
            }
            
            var spinResp = new SpinResponse
            {
                Data = Google.Protobuf.ByteString.CopyFrom(data),
                Service = { },
                TotalWin = totalWin,
                PostMoney = postMoney,
                BaseBet = 0.1,
                RealBet = 0.1,
                SpinType = 12,
                RoundIndexV2 = showIndex
            };
            
            return spinResp;
        }

        private static NoticeResponse BuildNoticeAck()
        {
            var spinResp = new NoticeResponse
            {
                Notice = { },
            };
            return spinResp;
        }

        private static VipSignInfoResp BuildVipSignInfo()
        {
            var vipSignInfoResp = new VipSignInfoResp
            {
                Error = 1,
            };

            return vipSignInfoResp;
        }

        private static GetUserMail BuildMailAck()
        {
            var mailInfo = new MailInfo
            {
                Isread = 1
            };

            var getMailInfo = new GetUserMail
            {
                Info = mailInfo
            };

            return getMailInfo;
        }

        private static CheckUseItem BuildAllItemCanUse()
        {
            var checkUseItem = new CheckUseItem
            {
                Info = null, // ← 이러면 proto에서 "info" 필드는 포함되지 않음
                Ret = 0
            };

            return checkUseItem;
        }

        private static Debris BuildDebrisInfoAck()
        {
            var debrisResult = new Debris
            {

            };

            return debrisResult;
        }

        private static LonghuListResponseV3 BuildLonghuList()
        {
            var longhuList = new LonghuListResponseV3
            {
                ErrorCode = 1,
                Message = "Record Not Exist"
            };

            return longhuList;
        }

        private static BackPackInfo BuildBackPackAck()
        {
            var ack = new BackPackInfo
            {
                Result = 0
            };

            var item = new Itemdata
            {
                Itemindex = 1,
                Itemid = 11802513,
                Amount = 1,
                Gameid = 180,
                Itemname = "Freegame(2 ★)",
                Icon = 22180,
                Itemvalue = 9.9,
                Bet = 0.02,
                Wintype = 2,
                Star = 2,
                Gamename = "Freegame",
                Itemdesc = "Freegame reward",
                StartTime = "2025-07-13T07:00:00+07:00",
                Expiredtime = "2025-07-14T07:00:00+07:00",
                Start = 1752364800,
                Expired = 1752451200
            };

            ack.Playeritemdata.Add(item);

            return ack;
        }

        private static DailyMissionInfo BuildGetNowMissionAck()
        {
            // 지정된 타임존 오프셋 (예: UTC+7)
            var timezoneOffset = TimeSpan.FromHours(7);

            // 오늘 날짜 기준 UTC 자정
            var todayUtc = DateTime.UtcNow.Date;

            // 그 날짜를 UTC+7 기준으로 DateTimeOffset 객체 생성
            var startTime = new DateTimeOffset(todayUtc, TimeSpan.Zero).ToOffset(timezoneOffset);
            var endTime = startTime.AddDays(1);

            var startTimeStr = startTime.ToString("yyyy-MM-ddTHH:mm:sszzz");
            var endTimeStr = endTime.ToString("yyyy-MM-ddTHH:mm:sszzz");
            var startTimestamp = startTime.ToUnixTimeSeconds();
            var endTimestamp = endTime.ToUnixTimeSeconds();
            var currentDate = startTime.ToString("yyyy-MM-dd");

            var missionInfo = new MissionInfoContent
            {
                Desc = "Daily Mission",
                StartTime = startTimeStr,
                EndTime = endTimeStr,
                StartTimestamp = startTimestamp.ToString(),
                EndTimestamp = endTimestamp.ToString(),
                CurrentDate = currentDate,
                OverDay = 7,
                OverDayMin = 0,
                DailyMissionList = new List<Mission>
        {
            new Mission
            {
                Date = currentDate,
                ConditionDesc = "Total bet over $109 ",
                CurrentCount = 0,
                TotalCount = 109,
                ItemID = 11802522,
                ItemCount = 1,
                Finished = false,
                Vip = 0,
                Type = 0,
                Coin = 0.0,
                Show = 1,
                Lock = false,
                GameID = 180,
                ChainID = 805,
                MissionIndex = 43847,
                ExtendID = 0
            },
            new Mission
            {
                Date = currentDate,
                ConditionDesc = "Total bet over $140 (bet $0.2 or more) ",
                CurrentCount = 0,
                TotalCount = 140,
                ItemID = 11802523,
                ItemCount = 1,
                Finished = false,
                Vip = 0,
                Type = 0,
                Coin = 0.0,
                Show = 1,
                Lock = true,
                GameID = 180,
                ChainID = 805,
                MissionIndex = 43848,
                ExtendID = 0
            },
            new Mission
            {
                Date = currentDate,
                ConditionDesc = "Total bet over $184 (bet $0.2 or more) ",
                CurrentCount = 0,
                TotalCount = 184,
                ItemID = 11802524,
                ItemCount = 1,
                Finished = false,
                Vip = 0,
                Type = 0,
                Coin = 0.0,
                Show = 1,
                Lock = true,
                GameID = 180,
                ChainID = 805,
                MissionIndex = 43849,
                ExtendID = 0
            }
        },
                ItemInfo = new Dictionary<string, ItemInfo>
                {
                    ["11802522"] = new ItemInfo
                    {
                        GameID = 180,
                        Game = "",
                        ItemName = "Freegame(2 ★)",
                        Icon = 22180,
                        ItemValue = 9.9,
                        Bet = 0.02,
                        GameName = "Freegame",
                        Star = 2,
                        WinType = 2,
                        WinMultiplier = 0.0,
                        WinMaxMag = 0.0,
                        PlayValue = 0.0
                    },
                    ["11802523"] = new ItemInfo
                    {
                        GameID = 180,
                        Game = "",
                        ItemName = "Freegame(2 ★)",
                        Icon = 22180,
                        ItemValue = 9.9,
                        Bet = 0.02,
                        GameName = "Freegame",
                        Star = 2,
                        WinType = 2,
                        WinMultiplier = 0.0,
                        WinMaxMag = 0.0,
                        PlayValue = 0.0
                    },
                    ["11802524"] = new ItemInfo
                    {
                        GameID = 180,
                        Game = "",
                        ItemName = "Freegame(3 ★)",
                        Icon = 23180,
                        ItemValue = 9.9,
                        Bet = 0.02,
                        GameName = "Freegame",
                        Star = 3,
                        WinType = 2,
                        WinMultiplier = 0.0,
                        WinMaxMag = 0.0,
                        PlayValue = 0.0
                    }
                }
            };

            var jsonString = JsonConvert.SerializeObject(missionInfo);

            var ack = new DailyMissionInfo
            {
                Message = jsonString,
            };

            return ack;
        }


        private static FullJpListResp BuildJpInfoAck()
        {
            var jpInfoResp = new FullJpListResp
            {
                
            };
            return jpInfoResp;
        }

        private static FullJpListResp BuildJpInfoAllAck()
        {
            var fullJpListResp = new FullJpListResp
            {

            };

            return fullJpListResp;
        }
        public static JpShowData BuildJpShowDataAck()
        {
            var newInfo1 = new JpInfo
            {
                Type = 1,
            };
            var newInfo2 = new JpInfo
            {
                Type = 2,
            };
            var newInfo3 = new JpInfo
            {
                Type = 3,
            };

            var jpShowData = new JpShowData();

            jpShowData.Info.Add(newInfo1);
            jpShowData.Info.Add(newInfo2);
            jpShowData.Info.Add(newInfo3);

            jpShowData.Game.AddRange(new int[] { });

            return jpShowData;
        }

        private static byte[] EncryptAck<T>(T ack, string token) where T : Google.Protobuf.IMessage
        {
            // Ack 객체를 ProtoBuf 바이너리로 직렬화
            var ackBytes = ack.ToByteArray();

            // AES CBC 방식으로 암호화
            return Crypto.EncryptAesCbc(ackBytes, token);
        }

        private async Task<HTTPEnterGameResults> procEnterGame(int agentID, string strUserID, string strSessionToken, string gameSymbol)
        {
            HTTPEnterGameResults enterGameResult = HTTPEnterGameResults.INVALIDTOKEN;
            try
            {
                var response = await HTTPServiceConfig.Instance.WorkerGroup.Ask<object>(new HTTPEnterGameRequest(agentID, strUserID, strSessionToken, GameProviders.JILI, gameSymbol), TimeSpan.FromSeconds(10));
                //실패(유저의 불법액션이므로 세션을 종료한다.)
                if (response is string)
                {
                    enterGameResult = HTTPEnterGameResults.INVALIDACTION;
                }
                else
                {
                    GITMessage responseMessage = (response as SendMessageToUser).Message;
                    byte result = (byte)responseMessage.Pop();
                    if (result == 0)
                        enterGameResult = HTTPEnterGameResults.OK;
                    else
                        enterGameResult = HTTPEnterGameResults.INVALIDGAMEID;
                }
            }
            catch
            {
                enterGameResult = HTTPEnterGameResults.INVALIDGAMEID;
            }
            return enterGameResult;
        }

        private async Task<ToHTTPSessionMessage> procMessage(int agentID, string strUserID, string strSessionToken, GITMessage requestMessage)
        {
            try
            {
                var response = await HTTPServiceConfig.Instance.WorkerGroup.Ask<object>(new FromHTTPSessionMessage(agentID, strUserID, strSessionToken, requestMessage), TimeSpan.FromSeconds(25.0));
                if (response is string)
                {
                    return new ToHTTPSessionMessage(ToHTTPSessionMsgResults.INVALIDTOKEN);
                }
                else
                {
                    GITMessage message = (response as SendMessageToUser).Message;
                    return new ToHTTPSessionMessage((response as SendMessageToUser).Message);
                }
            }
            catch
            {
                return new ToHTTPSessionMessage(ToHTTPSessionMsgResults.INVALIDTOKEN);
            }
        }
        private string findUserIDFromToken(string strToken)
        {
            string[] strParts = strToken.Split(new string[] { "@" }, StringSplitOptions.RemoveEmptyEntries);
            if (strParts.Length != 2)
                return null;

            return strParts[0];
        }

        private GITMessage buildSpinMessage(SpinReq spinReq)
        {
            try
            {
                GITMessage message = new GITMessage((ushort)CSMSG_CODE.CS_JILI_DOSPIN);
                message.Append((float)spinReq.Bet);
                if(spinReq.Mall != null)
                { 
                    //FreePurcharge
                    message.Append(1);
                }
                else
                {
                    //Normal
                    message.Append(0);
                }
                //var properties = spinReq.GetType().GetProperties();
                //foreach (var prop in properties)
                //{
                //    var value = prop.GetValue(spinReq);
                //    if (value != null)
                //    {
                //        if (value is int intValue)
                //            message.Append(intValue);
                //        else if (value is float floatValue)
                //            message.Append(floatValue);
                //        else if (value is double doubleValue)
                //            message.Append((float)doubleValue); // 필요한 경우 float로 캐스팅
                //        else if (value is bool boolValue)
                //            message.Append(boolValue ? 1 : 0);
                //        // 필요한 경우 string 등 추가
                //    }
                //}
                message.Append(1);
                message.Append(1);
                return message;
            }
            catch (Exception)
            {
                return null;
            }
        }
        private GITMessage buildDoInitMessage(InfoReq infoReq)
        {
            try
            {
                GITMessage message = new GITMessage((ushort)CSMSG_CODE.CS_JILI_DOINIT);
                var properties = infoReq.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(infoReq);
                    if (value != null)
                    {
                        if (value is int intValue)
                            message.Append(intValue);
                        else if (value is float floatValue)
                            message.Append(floatValue);
                        else if (value is double doubleValue)
                            message.Append((float)doubleValue); // 필요한 경우 float로 캐스팅
                        else if (value is bool boolValue)
                            message.Append(boolValue ? 1 : 0);
                        // 필요한 경우 string 등 추가
                    }
                }
                message.Append(1);
                message.Append(1);
                return message;
            }
            catch (Exception)
            {
                return null;
            }
        }


    }
}
