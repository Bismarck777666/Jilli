using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using GITProtocol;
using GITProtocol.Utils;
using SlotGamesNode.Database;
using PCGSharp;
using MongoDB.Bson;

namespace SlotGamesNode.GameLogics
{
    public abstract class IGameLogicActor : ReceiveActor
    {
        protected static readonly string        _logSplitString = "##";
        protected GameConfig                    _config         = null;
        protected GAMEID                        _gameID;

        protected readonly ILoggingAdapter      _logger         = Logging.GetLogger(Context);

        protected string                        GameName        { get; set; }

        protected Dictionary<string, IActorRef> _dicEnteredUsers    = new Dictionary<string, IActorRef>();
        protected IActorRef                     _dbReader           = null;
        protected IActorRef                     _dbWriter           = null;
        protected IActorRef                     _redisWriter        = null;

        protected static RealExtensions.Epsilon _epsilion       = new RealExtensions.Epsilon(0.001);


        #region 보너스정보
        protected GITMessage _bonusSendMessage;
        protected double _rewardedBonusMoney;
        protected bool _isRewardedBonus;
        #endregion

        #region 트랜잭션정보 
        protected string _roundID;
        #endregion

        protected Dictionary<int, double> _websitePayoutRates = new Dictionary<int, double>();

        public IGameLogicActor()
        {
            Receive<DBProxyInform>(inform =>
            {
                _dbReader    = inform.DBReader;
                _dbWriter    = inform.DBWriter;
                _redisWriter = inform.RedisWriter;
            });

            //유저게임입장메세지
            ReceiveAsync<EnterGameRequest>      (onEnterUserMessage);

            //유저게임탈퇴메세지
            ReceiveAsync<ExitGameRequest>       (onExitUserMessage);

            //유저메세지처리
            ReceiveAsync<FromUserMessage>       (onProcMessage);
            Receive<string>                     (onProcCommand);
            ReceiveAsync<BsonDocument>          (onLoadSpinData);
        }

        protected override void PostStop()
        {
            Context.System.EventStream.Unsubscribe(Self);
            base.PostStop();
        }
        protected virtual void onProcCommand(string strCommand)
        {
            if (strCommand == "loadSetting")
                LoadSetting();
        }
        protected virtual async Task onLoadSpinData(BsonDocument infoDocument)
        {

        }
        protected virtual void LoadSetting()
        {            
            if (PayoutConfigSnapshot.Instance.PayoutConfigs.ContainsKey(_gameID))
                _config = PayoutConfigSnapshot.Instance.PayoutConfigs[_gameID];
            else
                _config = new GameConfig(97.0f);

            if (PayoutConfigSnapshot.Instance.WebsitePayoutConfigs.ContainsKey(_gameID))
                _websitePayoutRates = PayoutConfigSnapshot.Instance.WebsitePayoutConfigs[_gameID];
            else
                _websitePayoutRates = new Dictionary<int, double>();
        }
        protected void sumUpWebsiteBetWin(int websiteID, double betMoney, double winMoney, int poolIndex = 0)
        {
            PayoutPoolConfig.Instance.PoolActor.Tell(new SumUpWebsiteBetWinRequest(websiteID, _gameID, betMoney, winMoney, poolIndex));
        }
        protected double getPayoutRate(int websiteID, bool isAffiliate = false)
        {
            double payoutRate = _config.PayoutRate;
            if (_websitePayoutRates.ContainsKey(websiteID))
                payoutRate = _websitePayoutRates[websiteID];
            return payoutRate;
        }
        protected async Task<bool> checkWebsitePayoutRate(int websiteID, double betMoney, double winMoney, int poolIndex = 0)
        {
            if (betMoney == 0.0 && winMoney == 0.0)
                return true;

            bool result = await PayoutPoolConfig.Instance.PoolActor.Ask<bool>(new CheckWebsitePayoutRequest(websiteID, _gameID, betMoney, winMoney, poolIndex));
            return result;
        }

        #region 메세지처리 함수들
        private async Task onEnterUserMessage(EnterGameRequest message)
        {
            string strGlobalUserID = string.Format("{0}_{1}", message.AgentID, message.UserID);
            _dicEnteredUsers[strGlobalUserID] = message.UserActor;

            bool isLoadSuccess = await loadUserHistoricalData(message.AgentID, message.UserID, message.NewEnter);

            //리력정보를 읽는 과정에 Redis오유가 발생했다면 
            if(!isLoadSuccess)
            {
                //입장실패메세지를 보낸다.
                Sender.Tell(new EnterGameResponse((int) _gameID, Self, 1));
                return;
            }

            EnterGameResponse response = new EnterGameResponse((int) _gameID, Self, 0);
            //게임에 새로 진입할 경우
            if (message.NewEnter)
                await onUserEnterGame(message.AgentID, message.UserID);

            Sender.Tell(response);  //게임입장성공메세지를 보낸다.
        }   
        protected virtual async Task onExitUserMessage(ExitGameRequest message)
        {
            await onUserExitGame(message.WebsiteID, message.UserID, message.UserRequested);
            _dicEnteredUsers.Remove(message.UserID);
            Sender.Tell(new ExitGameResponse());
        }
        private async Task onProcMessage(FromUserMessage message)
        {
            UserBonus userBonus = new UserBonus();
            await onProcMessage(message.UserID, message.WebsiteID, message.Message, userBonus, message.UserBalance, message.Currency, false);
        }
        #endregion

        #region 가상함수들
        protected virtual async Task onUserEnterGame(int websiteID, string strGlobalUserID)
        {

        }
        protected virtual async Task onUserExitGame(int agentID, string strUserID, bool userRequested)
        {

        }
        protected virtual async Task onProcMessage(string strUserID, int websiteID, GITMessage message, UserBonus userBonus, double userBalance, Currencies currency, bool isAffiliate)
        {

        }
        protected virtual async Task<bool> loadUserHistoricalData(int agentID, string strUserID, bool isNewEnter)
        {
            return true;
        }
        #endregion
        protected virtual async Task<bool> subtractEventMoney(int websiteID, string strUserID, double totalWin)
        {
            try
            {
                bool result = await Context.System.ActorSelection("/user/apiWorker").Ask<bool>(new SubtractEventMoneyRequest(websiteID, strUserID, Math.Round(totalWin, 2)), TimeSpan.FromSeconds(5.0));
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in IGameLogicActor::subtractEventMoney {0}", ex);
                return false;
            }
        }
        protected virtual void addEventLeftMoney(int websiteID, string strUserID, double leftMoney)
        {
            try
            {
                //Context.System.ActorSelection("/user/apiWorker").Tell(new AddEventLeftMoneyRequest(websiteID, strUserID, Math.Round(leftMoney, 2)));
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in IGameLogicActor::addEventLeftMoney {0}", ex);
            }
        }
    }
}
