using Akka.Actor;
using GITProtocol;
using GITProtocol.Utils;
using Newtonsoft.Json;
using PCGSharp;
using SlotGamesNode.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Microsoft.Extensions.Logging;
using System.ServiceModel;
using Akka.Util;
using Google.Protobuf.WellKnownTypes;
using SlotGamesNode.GameLogics.BaseClasses;

namespace SlotGamesNode.GameLogics
{
    public class BaseJiliSlotGame : IGameLogicActor
    {
        protected double _spinDataDefaultBet = 0.0f;

        protected int _normalMaxID = 0;
        protected int _naturalSpinCount = 0;
        protected int _emptySpinCount = 0;

        //프리스핀구매기능이 있을떄만 필요하다. 디비안의 모든 프리스핀들의 오드별 아이디어레이
        protected double _totalFreeSpinWinRate = 0.0; //스핀디비안의 모든 프리스핀들의 배당평균값
        protected double _minFreeSpinWinRate = 0.0; //구매금액의 20% - 50%사이에 들어가는 모든 프리스핀들의 평균배당값

        //앤티베팅기능이 있을때만 필요하다.(앤티베팅시 감소시켜야할 빈스핀의 갯수)
        protected int _anteBetMinusZeroCount = 0;

        protected Dictionary<string, JiliFreeSpinInfo> _dicUserFreeSpinInfos = new Dictionary<string, JiliFreeSpinInfo>();
        //매유저의 베팅정보 
        protected Dictionary<string, BaseJiliSlotBetInfo> _dicUserBetInfos = new Dictionary<string, BaseJiliSlotBetInfo>();

        //유저의 게임이력정보
        protected Dictionary<string, BaseJiliHistory> _dicUserHistory = new Dictionary<string, BaseJiliHistory>();

        //유정의 마지막결과정보
        protected Dictionary<string, BaseJiliSlotSpinResult> _dicUserResultInfos = new Dictionary<string, BaseJiliSlotSpinResult>();

        //유저의 설정정보
        protected Dictionary<string, string> _dicUserSettings = new Dictionary<string, string>();

        //백업정보
        protected Dictionary<string, BaseJiliSlotSpinResult> _dicUserLastBackupResultInfos = new Dictionary<string, BaseJiliSlotSpinResult>();
        protected Dictionary<string, byte[]> _dicUserLastBackupBetInfos = new Dictionary<string, byte[]>();
        protected Dictionary<string, byte[]> _dicUserLastBackupHistory = new Dictionary<string, byte[]>();
        protected Dictionary<string, byte[]> _dicUserLastBackupFreeSpinInfos = new Dictionary<string, byte[]>();


        protected string _defaultSetting = "SoundState=true_true_true_true_true;FastPlay=false;Intro=true;StopMsg=0;TurboSpinMsg=0;BetInfo=0_0;BatterySaver=false;ShowCCH=true;ShowFPH=true;CustomGameStoredData=;Coins=false;Volume=1;InitialScreen=8,9,6_5,3,7_7,5,4_3,6,3_9,7,8;SBPLock=true";
        protected virtual int FreeSpinTypeCount
        {
            get
            {
                return 0; //유저가 선택가능한 프리스핀종류수
            }
        }
        protected virtual bool HasPurEnableOption
        {
            get { return false; }
        }

        protected virtual bool HasSelectableFreeSpin
        {
            get
            {
                return false;
            }
        }
        protected virtual string SymbolName
        {
            get
            {
                return "";
            }
        }

        protected virtual string GameID
        {
            get
            {
                return "";
            }
        }
        protected virtual bool SupportReplay
        {
            get
            {
                return true;
            }
        }
        protected virtual int ClientReqLineCount
        {
            get
            {
                return 1;
            }
        }
        protected virtual int ServerResLineCount
        {
            get { return 0; }
        }
        protected virtual int ROWS
        {
            get
            {
                return 3;
            }
        }
        protected virtual string InitDataString
        {
            get
            {
                return "";
            }
        }
        protected virtual bool SupportPurchaseFree
        {
            get { return false; }
        }
        protected virtual double PurchaseFreeMultiple
        {
            get { return 0.0; }
        }
        protected virtual bool SupportMoreBet
        {
            get { return false; }
        }
        protected virtual double MoreBetMultiple
        {
            get { return 0.0; }
        }

        public BaseJiliSlotGame()
        {
            Receive<SymbolAndReplayRequest>(_ =>
            {
                Sender.Tell(new SymbolAndReplayResponse(this.SymbolName, this.SupportReplay, this.GameName));
            });
        }
        protected override void LoadSetting()
        {
            base.LoadSetting();
            initGameData();
        }

        protected virtual void addDefaultParams(Dictionary<string, string> dicParams, double userBalance, int index, int counter)
        {
            dicParams["balance"] = Math.Round(userBalance, 2).ToString();        //밸런스
            dicParams["balance_cash"] = Math.Round(userBalance, 2).ToString();        //밸런스
            dicParams["balance_bonus"] = "0.0";
            dicParams["stime"] = GameUtils.GetCurrentUnixTimestampMillis().ToString();
            dicParams["index"] = index.ToString();
            dicParams["counter"] = (counter + 1).ToString();
            dicParams["sver"] = "5";

            //Added by Foresight(2023.03.12)
            if (!dicParams.ContainsKey("sh"))
                dicParams["sh"] = ROWS.ToString();
        }

        protected virtual void initGameData()
        {
        }
        protected override async Task onLoadSpinData(BsonDocument infoDocument)
        {
            try
            {
                _spinDataDefaultBet = (double)infoDocument["defaultbet"];
                _normalMaxID = (int)infoDocument["normalmaxid"];
                _emptySpinCount = (int)infoDocument["emptycount"];
                _naturalSpinCount = (int)infoDocument["normalselectcount"];
                if (SupportPurchaseFree)
                {
                    //var purchaseOdds = infoDocument["purchaseodds"] as BsonArray;
                    //_totalFreeSpinWinRate = (double)purchaseOdds[1];
                    //_minFreeSpinWinRate = (double)purchaseOdds[0];
                    _totalFreeSpinWinRate = 46f;
                    _minFreeSpinWinRate = 10f;
                }

                if (this.SupportMoreBet)
                {
                    _anteBetMinusZeroCount = (int)((1.0 - 1.0 / MoreBetMultiple) * (double)_naturalSpinCount);
                    if (_anteBetMinusZeroCount > _emptySpinCount)
                        _logger.Error("More Bet Probabily calculation doesn't work in {0}", this.GameName);
                }

                if (this.SupportPurchaseFree && this.PurchaseFreeMultiple > _totalFreeSpinWinRate)
                    _logger.Error("freespin win rate doesn't satisfy condition {0}", this.GameName);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in loading spin data of game {0} {1}", GameName, ex);
            }
        }

        #region 메세지처리함수들
        //protected override async Task onProcMessage(string strUserID, int agentID, GITMessage message, double userBalance, Currencies currency, bool isAffiliate)
        protected override async Task onProcMessage(string strUserID, int agentID, GITMessage message, UserBonus userBonus, double userBalance, Currencies currency, bool isAffiliate)
        {
            string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
            if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_DOINIT)
            {
                onDoInit(strGlobalUserID, message, userBonus, userBalance, currency);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_SAVESETTING)
            {
                onSaveSetting(strGlobalUserID, message, userBonus, userBalance);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_DOSPIN)
            {
                await onDoSpin(strUserID, agentID, message, userBonus, userBalance, currency, isAffiliate);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_DOCOLLECT)
            {
                onDoCollect(agentID, strUserID, message, userBalance, currency);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_RELOADBALANCE)
            {
                onReloadBalance(strUserID, message, userBalance);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_NOTPROCDRESULT)
            {
                onDoUndoUserSpin(strGlobalUserID);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_DOBONUS)
            {
                onDoBonus(agentID, strUserID, message, userBalance, currency, isAffiliate);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_DOCOLLECTBONUS)
            {
                onDoCollectBonus(agentID, strUserID, message, userBalance, currency);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_DOFSBONUS)
            {
                onDoFSBonus(agentID, strUserID, message, userBalance);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_DOGAMBLEOPTION)
            {
                onDoGambleOption(agentID, strUserID, message, userBalance);
            }
            else if (message.MsgCode == (ushort)CSMSG_CODE.CS_JILI_DOGAMBLE)
            {
                await onDoGamble(agentID, strUserID, message, userBalance, isAffiliate);
            }
        }
        protected virtual void onDoInit(string strGlobalUserID, GITMessage message, UserBonus userBonus, double userBalance, Currencies currency)
        {
            try
            {
                int index = (int)message.Pop();
                int counter = (int)message.Pop();

                string strResponse = genInitResponse(strGlobalUserID, userBalance, index, counter, false, currency, userBonus);

                GITMessage responseMessage = new GITMessage((ushort)SCMSG_CODE.SC_JILI_DOINIT);
                responseMessage.Append(strResponse);

                Sender.Tell(new ToUserMessage((int)_gameID, responseMessage), Self);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::onDoInit GameID: {0}, {1}", _gameID, ex);
            }
        }
        protected virtual void onReloadBalance(string strUserID, GITMessage message, double userBalance)
        {
            try
            {
                Dictionary<string, string> responseParams = new Dictionary<string, string>();
                responseParams.Add("balance", Math.Round(userBalance, 2).ToString());
                responseParams.Add("balance_cash", Math.Round(userBalance, 2).ToString());
                responseParams.Add("balance_bonus", "0.00");
                responseParams.Add("stime", GameUtils.GetCurrentUnixTimestampMillis().ToString());
                GITMessage reponseMessage = new GITMessage((ushort)SCMSG_CODE.SC_JILI_RELOADBALANCE);
                reponseMessage.Append(convertKeyValuesToString(responseParams));
                Sender.Tell(new ToUserMessage((int)_gameID, reponseMessage), Self);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::onDoCollect {0}", ex);
            }
        }
        protected virtual void onSaveSetting(string strGlobalUserID, GITMessage message, UserBonus userBonus, double userBalance)
        {
            try
            {
                bool isLoad = (bool)message.Pop();
                if (!isLoad)
                {
                    string strNewSetting = (string)message.Pop();
                    _dicUserSettings[strGlobalUserID] = strNewSetting;
                }

                GITMessage responseMessage = new GITMessage((ushort)SCMSG_CODE.SC_JILI_SAVESETTING);
                if (_dicUserSettings.ContainsKey(strGlobalUserID))
                    responseMessage.Append(_dicUserSettings[strGlobalUserID]);
                else
                    responseMessage.Append(_defaultSetting);

                Sender.Tell(new ToUserMessage((int)_gameID, responseMessage), Self);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::onSaveSetting GameID: {0}, {1}", _gameID, ex);
            }
        }
        private long getUnixMiliTimestamp()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
            return unixTimeMilliseconds;
        }
        private long getUnixTimestamp(DateTime time)
        {
            long timestamp = (long)time.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return timestamp;
        }
        protected string createRoundID()
        {
            return string.Format("{0}{1}", getUnixMiliTimestamp(), Guid.NewGuid().ToString().Replace("-", ""));
        }
        protected string createTransactionID()
        {
            return string.Format("{0}{1}", getUnixMiliTimestamp(), Guid.NewGuid().ToString().Replace("-", ""));
        }
        protected JiliFreeSpinInfo checkFreeSpinEvent(string strGlobalUserID, bool isNewBet, GITMessage message, UserBonus userBonus)
        {
            JiliFreeSpinInfo freeSpinInfo = null;
            if (_dicUserFreeSpinInfos.TryGetValue(strGlobalUserID, out freeSpinInfo))
            {
                if (isNewBet)
                {
                    if (freeSpinInfo.Pending)
                    {
                        if (message.FreeSpinPlayLater)
                        {
                            freeSpinInfo = null;
                            _dicUserFreeSpinInfos.Remove(strGlobalUserID);
                        }
                        else
                        {
                            if (userBonus != null && (userBonus is UserFreeSpinBonus))
                            {
                                UserFreeSpinBonus fromUser = (UserFreeSpinBonus)userBonus;
                                if (fromUser.BonusID == freeSpinInfo.BonusID)
                                {
                                    freeSpinInfo.Pending = false;
                                    var freeBetInfo = newBetInfo();
                                    freeBetInfo.BetPerLine = (float)Math.Round(freeSpinInfo.BetPerLine, 2);
                                    freeBetInfo.LineCount = this.ClientReqLineCount;
                                    _dicUserBetInfos[strGlobalUserID] = freeBetInfo;
                                    _isRewardedBonus = true;
                                }
                                else
                                {
                                    freeSpinInfo = null;
                                    _dicUserFreeSpinInfos.Remove(strGlobalUserID);
                                }
                            }
                            else
                            {
                                freeSpinInfo = null;
                                _dicUserFreeSpinInfos.Remove(strGlobalUserID);
                            }
                        }
                    }
                }
                else
                {
                    if (freeSpinInfo.Pending)
                        freeSpinInfo = null;
                }
            }
            return freeSpinInfo;
        }
        protected virtual async Task onDoSpin(string strUserID, int agentID, GITMessage message, UserBonus userBonus, double userBalance, Currencies currency, bool isAffiliate)
        {
            try
            {
                _isRewardedBonus = false;
                _bonusSendMessage = null;
                _rewardedBonusMoney = 0.0;

                string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
                BaseJiliSlotBetInfo betInfo = null;
                bool isNewBet = true;
                //if (_dicUserBetInfos.TryGetValue(strGlobalUserID, out betInfo))
                //{
                //    if (betInfo.HasRemainResponse)
                //        isNewBet = false;
                //}

                BaseJiliSlotSpinResult lastResult = null;
                if (_dicUserResultInfos.ContainsKey(strGlobalUserID))
                    lastResult = _dicUserResultInfos[strGlobalUserID];

                //if (lastResult != null && lastResult.NextAction != ActionTypes.DOSPIN)
                //{
                //    GITMessage resmessage = new GITMessage((ushort)SCMSG_CODE.SC_JILI_DOSPIN);
                //    resmessage.Append(string.Format("balance={0}&balance_cash={0}&balance_bonus=0.0&frozen=Internal+server+error.+The+game+will+be+restarted.+&msg_code=11&ext_code=SystemError", Math.Round(userBalance, 2)));
                //    ToUserMessage toUserResult = new ToUserMessage((int)_gameID, resmessage);
                //    Sender.Tell(toUserResult, Self);
                //    _logger.Warning("{0} user did DOSPIN but last result's next action is {1} : 01 {2}", strGlobalUserID, lastResult.NextAction, betInfo.LineCount);
                //    return;
                //}

                int index = 0;
                int counter = 0;
                var freeSpinInfo = checkFreeSpinEvent(strGlobalUserID, isNewBet, message, userBonus);
                if (freeSpinInfo == null)
                {
                    readBetInfoFromMessage(message, strGlobalUserID);
                    index = (int)message.Pop();
                    counter = (int)message.Pop();
                }
                else
                {
                    if (this.SupportMoreBet)
                    {
                        index = (int)message.GetData(3);
                        counter = (int)message.GetData(4);
                    }
                    else
                    {
                        index = (int)message.GetData(2);
                        counter = (int)message.GetData(3);
                    }
                }
                if (!_dicUserHistory.ContainsKey(strGlobalUserID))
                    _dicUserHistory.Add(strGlobalUserID, new BaseJiliHistory());

                if (_dicUserHistory[strGlobalUserID].log.Count == 0)
                    _dicUserHistory[strGlobalUserID].init = genInitResponse(strGlobalUserID, userBalance, index, counter, true, currency, userBonus);


                await spinGame(strUserID, agentID, userBonus, userBalance, index, counter, currency, isNewBet, isAffiliate, freeSpinInfo);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::onDoSpin GameID: {0}, {1}", _gameID, ex);
            }
        }
        protected virtual void onDoCollect(int agentID, string strUserID, GITMessage message, double userBalance, Currencies currency)
        {
            try
            {
                string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
                if (!_dicUserResultInfos.ContainsKey(strGlobalUserID))
                {
                    _logger.Error("{0} result information has not been found in BaseJiliSlotGame::onDoCollect.", strGlobalUserID);
                    return;
                }

                BaseJiliSlotSpinResult result = _dicUserResultInfos[strGlobalUserID];
                if (result.NextAction != ActionTypes.DOCOLLECT)
                {
                    _logger.Error("{0} next action is not DOCOLLECT just {1} in BaseJiliSlotGame::onDoCollect.", strGlobalUserID, result.NextAction);
                    return;
                }

                int index = (int)message.Pop();
                int counter = (int)message.Pop();
                Dictionary<string, string> responseParams = new Dictionary<string, string>();
                responseParams.Add("balance", Math.Round(userBalance, 2).ToString());
                responseParams.Add("balance_cash", Math.Round(userBalance, 2).ToString());
                responseParams.Add("balance_bonus", "0.00");
                responseParams.Add("na", "s");
                responseParams.Add("stime", GameUtils.GetCurrentUnixTimestampMillis().ToString());
                responseParams.Add("sver", "5");
                responseParams.Add("index", index.ToString());
                responseParams.Add("counter", (counter + 1).ToString());

                JiliFreeSpinInfo freeSpinInfo = null;
                GITMessage reponseMessage = new GITMessage((ushort)SCMSG_CODE.SC_JILI_DOCOLLECT);
                string strCollectResponse = convertKeyValuesToString(responseParams);
                reponseMessage.Append(strCollectResponse);

                addActionHistory(strGlobalUserID, "doCollect", strCollectResponse, index, counter);
                //saveHistory(agentID, strUserID, index, counter, userBalance, currency);

                if (_dicUserFreeSpinInfos.TryGetValue(strGlobalUserID, out freeSpinInfo))
                {
                    addFreeSpinBonusParams(reponseMessage, freeSpinInfo, strGlobalUserID, 0.0);
                    checkFreeSpinCompletion(reponseMessage, _dicUserFreeSpinInfos[strGlobalUserID], strGlobalUserID);
                }
                result.NextAction = ActionTypes.DOSPIN;
                saveBetResultInfo(strGlobalUserID);
                Sender.Tell(new ToUserMessage((int)_gameID, reponseMessage), Self);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::onDoCollect {0}", ex);
            }
        }

        protected virtual void onDoCollectBonus(int agentID, string strUserID, GITMessage message, double userBalance, Currencies currency)
        {
            try
            {
                int index = (int)message.Pop();
                int counter = (int)message.Pop();

                string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
                GITMessage responseMessage = new GITMessage((ushort)SCMSG_CODE.SC_JILI_DOCOLLECTBONUS);
                if (!_dicUserResultInfos.ContainsKey(strGlobalUserID) || !_dicUserBetInfos.ContainsKey(strGlobalUserID))
                {
                    responseMessage.Append("unlogged");
                }
                else
                {
                    BaseJiliSlotBetInfo betInfo = _dicUserBetInfos[strGlobalUserID];
                    BaseJiliSlotSpinResult result = _dicUserResultInfos[strGlobalUserID];
                    if (!betInfo.HasRemainResponse || result.NextAction != ActionTypes.DOCOLLECTBONUS)
                    {
                        responseMessage.Append("unlogged");
                    }
                    else
                    {
                        BaseJiliActionToResponse actionResponse = betInfo.pullRemainResponse();
                        if (actionResponse.ActionType != ActionTypes.DOCOLLECTBONUS)
                        {
                            responseMessage.Append("unlogged");
                        }
                        else
                        {
                            Dictionary<string, string> dicParams = splitResponseToParams(actionResponse.Response);
                            addDefaultParams(dicParams, userBalance, index, counter);
                            convertWinsByBet(dicParams, betInfo.TotalBet);
                            convertBetsByBet(dicParams, betInfo.BetPerLine, betInfo.TotalBet);
                            responseMessage.Append(convertKeyValuesToString(dicParams));

                            result.NextAction = convertStringToActionType(dicParams["na"]);

                            //히스토리보관 및 초기화
                            if (_dicUserHistory.ContainsKey(strGlobalUserID) && _dicUserHistory[strGlobalUserID].log.Count > 0)
                            {
                                addActionHistory(strGlobalUserID, "doCollectBonus", convertKeyValuesToString(dicParams), index, counter);
                                //saveHistory(agentID, strUserID, index, counter, userBalance, currency);
                                if (_dicUserFreeSpinInfos.ContainsKey(strGlobalUserID))
                                {
                                    addFreeSpinBonusParams(responseMessage, _dicUserFreeSpinInfos[strGlobalUserID], strGlobalUserID, 0.0);
                                    checkFreeSpinCompletion(responseMessage, _dicUserFreeSpinInfos[strGlobalUserID], strGlobalUserID);
                                }
                            }
                        }
                        if (!betInfo.HasRemainResponse)
                            betInfo.RemainReponses = null;

                        saveBetResultInfo(strGlobalUserID);
                    }
                    Sender.Tell(new ToUserMessage((int)_gameID, responseMessage), Self);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::onDoBonus {0}", ex);
            }
        }
        protected virtual void onDoBonus(int agentID, string strUserID, GITMessage message, double userBalance, Currencies currency, bool isAffiliate)
        {
            try
            {
                int index = (int)message.Pop();
                int counter = (int)message.Pop();

                GITMessage responseMessage = new GITMessage((ushort)SCMSG_CODE.SC_JILI_DOBONUS);
                double realWin = 0.0;
                string strGameLog = "";
                string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
                ToUserResultMessage resultMsg = null;

                if (!_dicUserResultInfos.ContainsKey(strGlobalUserID) || !_dicUserBetInfos.ContainsKey(strGlobalUserID) ||
                    !_dicUserBetInfos[strGlobalUserID].HasRemainResponse)
                {
                    responseMessage.Append("unlogged");
                }
                else
                {
                    BaseJiliSlotSpinResult result = _dicUserResultInfos[strGlobalUserID];
                    BaseJiliSlotBetInfo betInfo = _dicUserBetInfos[strGlobalUserID];
                    BaseJiliActionToResponse actionResponse = betInfo.pullRemainResponse();
                    if (actionResponse.ActionType != ActionTypes.DOBONUS)
                    {
                        responseMessage.Append("unlogged");
                    }
                    else
                    {
                        Dictionary<string, string> dicParams = splitResponseToParams(actionResponse.Response);

                        convertWinsByBet(dicParams, betInfo.TotalBet);
                        convertBetsByBet(dicParams, betInfo.BetPerLine, betInfo.TotalBet);

                        result.BonusResultString = convertKeyValuesToString(dicParams);

                        addDefaultParams(dicParams, userBalance, index, counter);

                        ActionTypes nextAction = convertStringToActionType(dicParams["na"]);
                        string strResponse = convertKeyValuesToString(dicParams);

                        responseMessage.Append(strResponse);

                        //히스토리보관 및 초기화
                        if (_dicUserHistory.ContainsKey(strGlobalUserID) && _dicUserHistory[strGlobalUserID].log.Count > 0)
                            addActionHistory(strGlobalUserID, "doBonus", strResponse, index, counter);

                        if (nextAction == ActionTypes.DOCOLLECT || nextAction == ActionTypes.DOCOLLECTBONUS)
                        {
                            realWin = double.Parse(dicParams["tw"]);
                            strGameLog = strResponse;

                            if (realWin > 0.0f)
                            {
                                _dicUserHistory[strGlobalUserID].baseBet = betInfo.TotalBet;
                                _dicUserHistory[strGlobalUserID].win = realWin;
                            }
                            resultMsg = new ToUserResultMessage((int)_gameID, responseMessage, 0.0, realWin, new GameLogInfo(this.GameName, "0", strGameLog), UserBetTypes.Normal); ;
                            resultMsg.BetTransactionID = betInfo.BetTransactionID;
                            resultMsg.RoundID = betInfo.RoundID;
                            resultMsg.TransactionID = createTransactionID();
                            if (_dicUserFreeSpinInfos.ContainsKey(strGlobalUserID) && !_dicUserFreeSpinInfos[strGlobalUserID].Pending)
                            {
                                resultMsg.FreeSpinID = _dicUserFreeSpinInfos[strGlobalUserID].FreeSpinID;
                                addFreeSpinBonusParams(responseMessage, _dicUserFreeSpinInfos[strGlobalUserID], strGlobalUserID, realWin);
                            }
                        }
                        copyBonusParamsToResult(dicParams, result);
                        result.NextAction = nextAction;
                    }
                    if (!betInfo.HasRemainResponse)
                        betInfo.RemainReponses = null;

                    saveBetResultInfo(strGlobalUserID);
                }
                if (resultMsg == null)
                    Sender.Tell(new ToUserMessage((int)_gameID, responseMessage), Self);
                else
                    Sender.Tell(resultMsg, Self);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::onDoBonus {0}", ex);
            }
        }
        protected void copyBonusParamsToResult(Dictionary<string, string> dicParams, BaseJiliSlotSpinResult result)
        {
            Dictionary<string, string> dicResultParams = splitResponseToParams(result.ResultString);
            copyBonusParamsToResultParams(dicParams, dicResultParams);
            result.ResultString = convertKeyValuesToString(dicResultParams);
        }
        protected virtual void copyBonusParamsToResultParams(Dictionary<string, string> bonusParams, Dictionary<string, string> resultParams)
        {

        }
        protected void onDoUndoUserSpin(string strGlobalUserID)
        {
            undoUserResultInfo(strGlobalUserID);
            undoUserHistory(strGlobalUserID);
            undoUserBetInfo(strGlobalUserID);
            undoUserFreeSpinInfo(strGlobalUserID);
            saveBetResultInfo(strGlobalUserID);
            if (_dicUserHistory.ContainsKey(strGlobalUserID))
            {
                byte[] historyByteData = _dicUserHistory[strGlobalUserID].convertToByte();
                _redisWriter.Tell(new UserHistoryWrite(strGlobalUserID, _gameID, historyByteData, false));
                _dicUserHistory.Remove(strGlobalUserID);
            }
            else
            {
                _redisWriter.Tell(new UserHistoryWrite(strGlobalUserID, _gameID, null, false));
            }
        }
        #endregion
        protected virtual void readBetInfoFromMessage(GITMessage message, string strGlobalUserID)
        {
            try
            {
                BaseJiliSlotBetInfo betInfo = new BaseJiliSlotBetInfo();
                
                betInfo.BetPerLine = (float)message.Pop();
                betInfo.LineCount = 1;
                betInfo.PurchaseFree = (int)message.Pop() == 1;

                if (betInfo.BetPerLine <= 0.0f || float.IsNaN(betInfo.BetPerLine) || float.IsInfinity(betInfo.BetPerLine))
                {
                    _logger.Error("{0} betInfo.BetPerLine <= 0 in BaseJiliSlotGame::readBetInfoFromMessage {1}", strGlobalUserID, betInfo.BetPerLine);
                    return;
                }

                if (betInfo.LineCount != this.ClientReqLineCount)
                {
                    _logger.Error("{0} betInfo.LineCount is not matched {1}", strGlobalUserID, betInfo.LineCount);
                    return;
                }

                BaseJiliSlotBetInfo oldBetInfo = null;
                if (_dicUserBetInfos.TryGetValue(strGlobalUserID, out oldBetInfo))
                {
                    //만일 유저에게 남은 응답이 존재하는 경우
                    if (oldBetInfo.HasRemainResponse)
                        return;

                    oldBetInfo.BetPerLine = betInfo.BetPerLine;
                    oldBetInfo.LineCount = betInfo.LineCount;
                    oldBetInfo.PurchaseFree = betInfo.PurchaseFree;
                }
                else
                {
                    _dicUserBetInfos.Add(strGlobalUserID, betInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::readBetInfoFromMessage {0}", ex);
            }
        }
        protected virtual void undoUserResultInfo(string strGlobalUserID)
        {
            try
            {
                if (!_dicUserLastBackupResultInfos.ContainsKey(strGlobalUserID))
                    return;

                BaseJiliSlotSpinResult lastResult = _dicUserLastBackupResultInfos[strGlobalUserID];
                if (lastResult == null)
                    _dicUserResultInfos.Remove(strGlobalUserID);
                else
                    _dicUserResultInfos[strGlobalUserID] = lastResult;
                _dicUserLastBackupResultInfos.Remove(strGlobalUserID);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::undoUserResultInfo {0}", ex);
            }
        }
        protected virtual void undoUserHistory(string strGlobalUserID)
        {
            try
            {
                if (!_dicUserLastBackupHistory.ContainsKey(strGlobalUserID))
                    return;

                byte[] userHistoryBytes = _dicUserLastBackupHistory[strGlobalUserID];
                if (userHistoryBytes == null)
                {
                    _dicUserHistory.Remove(strGlobalUserID);
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream(userHistoryBytes))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(ms))
                        {
                            BaseJiliHistory history = restoreHistory(binaryReader);
                            _dicUserHistory[strGlobalUserID] = history;
                        }
                    }
                }
                _dicUserLastBackupHistory.Remove(strGlobalUserID);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::undoUserHistory {0}", ex);
            }
        }
        protected virtual void undoUserFreeSpinInfo(string strGlobalUserID)
        {
            try
            {
                if (!_dicUserLastBackupFreeSpinInfos.ContainsKey(strGlobalUserID))
                    return;

                byte[] userFreeSpinBytes = _dicUserLastBackupFreeSpinInfos[strGlobalUserID];
                if (userFreeSpinBytes == null)
                {
                    _dicUserFreeSpinInfos.Remove(strGlobalUserID);
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream(userFreeSpinBytes))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(ms))
                        {
                            var freeSpinInfo = restoreFreeSpinInfo(strGlobalUserID, binaryReader);
                            _dicUserFreeSpinInfos[strGlobalUserID] = freeSpinInfo;
                        }
                    }
                }
                _dicUserLastBackupFreeSpinInfos.Remove(strGlobalUserID);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::undoUserFreeSpinInfo {0}", ex);
            }
        }
        protected virtual void undoUserBetInfo(string strGlobalUserID)
        {
            try
            {
                if (!_dicUserLastBackupBetInfos.ContainsKey(strGlobalUserID))
                    return;

                byte[] userBetInfoBytes = _dicUserLastBackupBetInfos[strGlobalUserID];
                using (MemoryStream ms = new MemoryStream(userBetInfoBytes))
                {
                    using (BinaryReader binaryReader = new BinaryReader(ms))
                    {
                        BaseJiliSlotBetInfo betInfo = restoreBetInfo(strGlobalUserID, binaryReader);
                        _dicUserBetInfos[strGlobalUserID] = betInfo;
                    }
                }
                _dicUserLastBackupBetInfos.Remove(strGlobalUserID);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::undoUserBetInfo {0}", ex);
            }
        }
        protected void addIndActionHistory(string strGlobalUserID, string strAction, string strResponse, int index, int counter, int ind)
        {
            if (!_dicUserHistory.ContainsKey(strGlobalUserID) || _dicUserHistory[strGlobalUserID].log.Count == 0)
                return;

            if (_dicUserHistory[strGlobalUserID].bet == 0.0)
                return;

            BaseJiliHistoryItem item = new BaseJiliHistoryItem();
            item.cr = string.Format("symbol={0}&repeat=0&action={3}&index={1}&counter={2}&ind={4}", SymbolName, index, counter, strAction, ind);
            item.sr = strResponse;
            _dicUserHistory[strGlobalUserID].log.Add(item);
        }
        protected void addActionHistory(string strGlobalUserID, string strAction, string strResponse, int index, int counter)
        {
            if (!_dicUserHistory.ContainsKey(strGlobalUserID) || _dicUserHistory[strGlobalUserID].log.Count == 0)
                return;

            if (_dicUserHistory[strGlobalUserID].bet == 0.0)
                return;

            BaseJiliHistoryItem item = new BaseJiliHistoryItem();
            item.cr = string.Format("symbol={0}&repeat=0&action={3}&index={1}&counter={2}", SymbolName, index, counter, strAction);
            item.sr = strResponse;
            _dicUserHistory[strGlobalUserID].log.Add(item);
        }
        protected void getMinMaxChip(string sc, ref double minChip, ref double maxChip)
        {
            string[] chips = sc.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            minChip = double.Parse(chips[0]);
            maxChip = double.Parse(chips[chips.Length - 1]);
        }

        protected virtual string genInitResponse(string strGlobalUserID, double userBalance, int index, int counter, bool useDefault, Currencies currency, UserBonus userBonus)
        {
            var ack = new GameInfo
            {
                ApiType = 8,
                MaxOdd = 10000,
                freeSpinType = -1,
                WalletInfo = new List<WalletInfo>(),
                freeSpin = new freeSpinList(),
                rtp = 96.5f
            };

            var walletInfo = new WalletInfo
            {
                currencyNumber = (int)currency,
                currencyName = DicCurrencyInfo.Instance._currencyInfo[currency].CurrencyText,
                currencySymbol = DicCurrencyInfo.Instance._currencyInfo[currency].CurrencySymbol,
                coin = userBalance,
                unit = 1,
                ratio = 0.1,
                rate = DicCurrencyInfo.Instance._currencyInfo[currency].Rate,
                decimal_ = 4,
                bet = new List<double>()
            };

            walletInfo.bet.AddRange(new List<double> { 0.1, 0.2, 0.3, 0.5, 0.8, 1, 2, 5, 10, 20, 30, 40, 50, 70, 100 });
            ack.WalletInfo.Add(walletInfo);
            
            //byte[] extraInfoBytes = new byte[] { 8, 1, 18, 16, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 248, 63 };
            byte[] extraInfoBytes = new byte[] { 50, 16, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 248, 63, 74, 96, 0, 0, 0, 0, 0, 64, 143, 64, 0, 0, 0, 0, 0, 0, 36, 64, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 62, 64, 0, 0, 0, 0, 0, 0, 20, 64, 0, 0, 0, 0, 0, 0, 73, 64, 0, 0, 0, 0, 0, 0, 105, 64, 0, 0, 0, 0, 0, 0, 32, 64, 0, 0, 0, 0, 0, 0, 89, 64, 0, 0, 0, 0, 0, 0, 8, 64, 0, 0, 0, 0, 0, 0, 52, 64, 0, 0, 0, 0, 0, 0, 46, 64 };

            string base64 = Convert.ToBase64String(extraInfoBytes);
            ack.extraInfo = base64;

            if (this.SupportPurchaseFree)
            {
                var gameMall = new GameMallInfo
                {
                    MaxBet = 500,
                    PriceOdd = new List<double>(),
                    AlterID = new List<int>()
                };
                gameMall.PriceOdd.Add(this.PurchaseFreeMultiple);
                gameMall.AlterID.Add(50);
                ack.gameMall = gameMall;

                // MallInfo 초기화
                var mallInfo = new MallInfo
                {
                    AlterID = 50,
                    DescType = 1,
                    MaxBet = 500,
                    PriceOdd = this.PurchaseFreeMultiple,
                    Show = 2,
                };

                ack.mall = mallInfo;
            }

            return JsonConvert.SerializeObject(ack);
        }
        protected string makeDefaultSpinResultString(double userBalance, int index, int counter, string initString)
        {
            Dictionary<string, string> dicParams = new Dictionary<string, string>();
            setupDefaultResultParams(dicParams, userBalance, index, counter, initString);
            return convertKeyValuesToString(dicParams);
        }
        protected virtual void setupDefaultResultParams(Dictionary<string, string> dicParams, double userBalance, int index, int counter, string initString)
        {
            Dictionary<string, string> dicInitParams = splitResponseToParams(initString);
            dicParams.Add("balance", Math.Round(userBalance, 2).ToString());
            dicParams.Add("balance_cash", Math.Round(userBalance, 2).ToString());
            dicParams.Add("balance_bonus", "0.00");
            dicParams.Add("na", "s");
            dicParams.Add("stime", GameUtils.GetCurrentUnixTimestampMillis().ToString());
            dicParams.Add("sver", "5");
            if (dicInitParams.ContainsKey("def_sa"))
                dicParams.Add("sa", dicInitParams["def_sa"]);
            if (dicInitParams.ContainsKey("def_sb"))
                dicParams.Add("sb", dicInitParams["def_sb"]);
            if (dicInitParams.ContainsKey("def_s"))
                dicParams.Add("s", dicInitParams["def_s"]); //def_s
            dicParams.Add("sh", ROWS.ToString());
            if(dicInitParams.ContainsKey("defc"))
                dicParams.Add("c", dicInitParams["defc"]);  //defc
            dicParams.Add("l", ServerResLineCount.ToString());
            if (index > 0)
            {
                dicParams.Add("index", index.ToString());
                dicParams.Add("counter", (counter + 1).ToString());
            }
        }
        protected virtual void overrideSomeParams(BaseJiliSlotBetInfo betInfo, Dictionary<string, string> dicParams)
        {

        }
        protected virtual void writeRoundID(BaseJiliSlotBetInfo betInfo, Dictionary<string, string> dicParams)
        {
            dicParams["rid"] = betInfo.RoundID.Substring(0, 13);
        }
        protected virtual string makeSpinResultString(BaseJiliSlotBetInfo betInfo, BaseJiliSlotSpinResult spinResult, double betMoney, double userBalance, int index, int counter, bool isInit)
        {
            Dictionary<string, string> dicParams = splitResponseToParams(spinResult.ResultString);

            if (spinResult.HasBonusResult)
            {
                Dictionary<string, string> dicBonusParams = splitResponseToParams(spinResult.BonusResultString);
                dicParams = mergeSpinToBonus(dicParams, dicBonusParams);
            }

            dicParams["balance_bonus"] = "0.00";
            dicParams["stime"] = GameUtils.GetCurrentUnixTimestampMillis().ToString();
            dicParams["sver"] = "5";
            dicParams["l"] = ServerResLineCount.ToString();
            dicParams["sh"] = ROWS.ToString();
            dicParams["c"] = Math.Round(betInfo.BetPerLine, 2).ToString();
            if (index > 0)
            {
                dicParams["index"] = index.ToString();
                dicParams["counter"] = (counter + 1).ToString();
            }

            ActionTypes nextAction = convertStringToActionType(dicParams["na"]);
            if (isInit)
            {
                dicParams["na"] = convertActionTypeToString(spinResult.NextAction);
                dicParams["action"] = convertActionTypeToFullString(spinResult.NextAction);
            }
            else
            {
                dicParams["na"] = convertActionTypeToString(spinResult.NextAction);
            }

            dicParams["balance"] = Math.Round(userBalance - (isInit ? 0.0 : betMoney), 2).ToString();        //밸런스
            dicParams["balance_cash"] = Math.Round(userBalance - (isInit ? 0.0 : betMoney), 2).ToString();        //밸런스케시

            if (SupportPurchaseFree && betInfo.PurchaseFree)
                dicParams["puri"] = "0";
            else
                dicParams.Remove("puri");

            if (SupportMoreBet)
            {
                if (betInfo.MoreBet)
                    dicParams["bl"] = "1";
                else
                    dicParams["bl"] = "0";
            }
            if (isInit)
                supplementInitResult(dicParams, betInfo, spinResult);

            overrideSomeParams(betInfo, dicParams);
            writeRoundID(betInfo, dicParams);

            return convertKeyValuesToString(dicParams);
        }

        protected virtual void supplementInitResult(Dictionary<string, string> dicParams, BaseJiliSlotBetInfo betInfo, BaseJiliSlotSpinResult spinResult)
        {

        }
        protected virtual Dictionary<string, string> mergeSpinToBonus(Dictionary<string, string> spinParams, Dictionary<string, string> bonusParams)
        {
            return spinParams;
        }
        protected string convertWinByBet(string strWin, float currentBet)
        {
            double win = double.Parse(strWin);
            win = win / _spinDataDefaultBet * currentBet;
            return Math.Round(win, 2).ToString();
        }

        protected double getRealWinByBet(double originWin, float currentBet)
        {
            originWin = originWin / _spinDataDefaultBet * currentBet;
            return Math.Round(originWin, 2);
        }
        protected virtual void convertBetsByBet(Dictionary<string, string> dicParams, float betPerLine, float totalBet)
        {
            if (dicParams.ContainsKey("coef"))
            {
                double coef = Math.Round(double.Parse(dicParams["coef"]), 2);
                if (coef == _spinDataDefaultBet)
                    dicParams["coef"] = Math.Round(totalBet, 2).ToString();
                else
                    dicParams["coef"] = Math.Round(betPerLine, 2).ToString();
            }
        }
        protected virtual void convertWinsByBet(Dictionary<string, string> dicParams, float currentBet)
        {
            if (dicParams.ContainsKey("tw"))
                dicParams["tw"] = convertWinByBet(dicParams["tw"], currentBet);

            if (dicParams.ContainsKey("w"))
                dicParams["w"] = convertWinByBet(dicParams["w"], currentBet);

            if (dicParams.ContainsKey("rw"))
                dicParams["rw"] = convertWinByBet(dicParams["rw"], currentBet);

            if (dicParams.ContainsKey("fswin"))
                dicParams["fswin"] = convertWinByBet(dicParams["fswin"], currentBet);

            if (dicParams.ContainsKey("fsres"))
                dicParams["fsres"] = convertWinByBet(dicParams["fsres"], currentBet);

            if (dicParams.ContainsKey("fswin_total"))
                dicParams["fswin_total"] = convertWinByBet(dicParams["fswin_total"], currentBet);

            if (dicParams.ContainsKey("fsres_total"))
                dicParams["fsres_total"] = convertWinByBet(dicParams["fsres_total"], currentBet);

            if (dicParams.ContainsKey("fscwin_total"))
                dicParams["fscwin_total"] = convertWinByBet(dicParams["fscwin_total"], currentBet);

            if (dicParams.ContainsKey("fscres_total"))
                dicParams["fscres_total"] = convertWinByBet(dicParams["fscres_total"], currentBet);

            if (dicParams.ContainsKey("bpw"))
                dicParams["bpw"] = convertWinByBet(dicParams["bpw"], currentBet);

            if (dicParams.ContainsKey("e_aw"))
                dicParams["e_aw"] = convertWinByBet(dicParams["e_aw"], currentBet);

            if (dicParams.ContainsKey("fscres_total"))
                dicParams["fscres_total"] = convertWinByBet(dicParams["fscres_total"], currentBet);

            //스캐터정보
            if (dicParams.ContainsKey("psym"))
            {
                string[] strParts = dicParams["psym"].Split(new string[] { "~" }, StringSplitOptions.RemoveEmptyEntries);
                if (strParts.Length == 3)
                {
                    strParts[1] = convertWinByBet(strParts[1], currentBet);
                    dicParams["psym"] = string.Join("~", strParts);
                }
            }

            //WinLine정보
            int winLineID = 0;
            do
            {
                string strKey = string.Format("l{0}", winLineID);
                if (!dicParams.ContainsKey(strKey))
                    break;

                string[] strParts = dicParams[strKey].Split(new string[] { "~" }, StringSplitOptions.None);
                if (strParts.Length >= 2)
                {
                    strParts[1] = convertWinByBet(strParts[1], currentBet);
                    dicParams[strKey] = string.Join("~", strParts);
                }
                winLineID++;
            } while (true);
        }
        protected virtual BaseJiliSlotSpinResult calculateResult(BaseJiliSlotBetInfo betInfo, string strSpinResponse, bool isFirst, JiliFreeSpinInfo freeSpinInfo, double userBalance = 0, double winMoney = 0)
        {
            try
            {
                BaseJiliSlotSpinResult spinResult = new BaseJiliSlotSpinResult();

                var spinResultJson = JsonConvert.DeserializeObject<SajSpinResultClass>(strSpinResponse);


                //Dictionary<string, string> dicParams = splitResponseToParams(strSpinResponse);

                //모든 당첨값들을 현재의 베팅금액상태로 전환한다.
                //convertWinsByBet(dicParams, betInfo.TotalBet);
                double realBetMoney = betInfo.TotalBet;

                if (SupportPurchaseFree && betInfo.PurchaseFree)
                    realBetMoney = realBetMoney * getPurchaseMultiple(betInfo);

                foreach (RoundQueueData item in spinResultJson.RoundQueue)
                {
                    item.RoundWin = getRealWinByBet(item.RoundWin, betInfo.TotalBet);
                    foreach(ComboStageData comboItem in item.ComboStageData)
                    {
                        comboItem.ComboStageWin = getRealWinByBet(comboItem.ComboStageWin, betInfo.TotalBet);
                        foreach (JObject awardItem in comboItem.AwardDataVec)
                        {
                            awardItem["Win"] = getRealWinByBet(awardItem["Win"].Value<double>(), betInfo.TotalBet);
                        }
                    }
                }

                spinResultJson.FreeTotalWin = getRealWinByBet(spinResultJson.FreeTotalWin, betInfo.TotalBet);
                spinResultJson.TotalWin = getRealWinByBet(spinResultJson.TotalWin, betInfo.TotalBet);
                
                spinResultJson.NowMoney = userBalance - realBetMoney + spinResultJson.TotalWin;
                spinResultJson.Bet = Math.Round( betInfo.TotalBet,2);
                Random random = new Random();
                int index = random.Next(1, 10000);
                string historyIndex = index.ToString("D6");
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string showIndex = timestamp.ToString() + historyIndex + this.GameID;

                if (showIndex.Length == 19)
                {
                    string part1 = showIndex.Substring(0, 5);   // 18155
                    string part2 = showIndex.Substring(5, 6);   // 304040
                    string part3 = showIndex.Substring(11, 8);  // 05390409

                    spinResultJson.ShowIndex = $"{part1}-{part2}-{part3}";
                }

                spinResult.TotalWin = spinResultJson.TotalWin;

                if (spinResult.TotalWin > 0)
                {

                }

                //convertBetsByBet(dicParams, betInfo.BetPerLine, betInfo.TotalBet);

                //if (SupportPurchaseFree && betInfo.PurchaseFree && isFirst)
                //    dicParams["purtr"] = "1";

                //spinResult.NextAction = convertStringToActionType(dicParams["na"]);
                //if (spinResult.NextAction == ActionTypes.DOCOLLECT || spinResult.NextAction == ActionTypes.DOCOLLECTBONUS)
                //    spinResult.TotalWin = spinResultJson.TotalWin;
                //spinResult.TotalWin = double.Parse(dicParams["tw"]);
                //else
                //    spinResult.TotalWin = 0.0;

                if (freeSpinInfo != null)
                {
                    //dicParams["fra"] = Math.Round(freeSpinInfo.TotalWin, 2).ToString();
                    //dicParams["frn"] = freeSpinInfo.RemainCount.ToString();
                    //dicParams["frt"] = "N";
                }
                //spinResult.ResultString = convertKeyValuesToString(dicParams);
                spinResult.ResultString = JsonConvert.SerializeObject(spinResultJson);
                return spinResult;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::calculateResult {0}", ex);
                return null;
            }
        }
        protected Dictionary<string, string> splitResponseToParams(string strResponse)
        {
            string[] strParts = strResponse.Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
            if (strParts == null || strParts.Length == 0)
                return new Dictionary<string, string>();

            Dictionary<string, string> dicParamValues = new Dictionary<string, string>();
            for (int i = 0; i < strParts.Length; i++)
            {
                string[] strParamValues = strParts[i].Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                if (strParamValues.Length == 2)
                    dicParamValues[strParamValues[0]] = strParamValues[1];
                else if (strParamValues.Length == 1)
                    dicParamValues[strParamValues[0]] = null;
            }
            return dicParamValues;
        }
        protected string convertKeyValuesToString(Dictionary<string, string> keyValues)
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, string> pair in keyValues)
            {
                if (pair.Value == null)
                    parts.Add(string.Format("{0}=", pair.Key));
                else
                    parts.Add(string.Format("{0}={1}", pair.Key, pair.Value));
            }
            return string.Join("&", parts.ToArray());
        }
        protected ActionTypes convertStringToActionType(string strAction)
        {
            switch (strAction.Trim())
            {
                case "s":
                    return ActionTypes.DOSPIN;
                case "c":
                    return ActionTypes.DOCOLLECT;
                case "cb":
                    return ActionTypes.DOCOLLECTBONUS;
                case "m":
                    return ActionTypes.DOMYSTERY;
                case "fso":
                    return ActionTypes.DOFSOPTION;
                case "b":
                    return ActionTypes.DOBONUS;
                case "fsb":
                    return ActionTypes.DOFSBONUS;
                case "go":
                    return ActionTypes.DOGAMBLEOPTION;
                case "g":
                    return ActionTypes.DOGAMBLE;

            }
            return ActionTypes.NONE;
        }
        protected string convertActionTypeToString(ActionTypes actionType)
        {
            switch (actionType)
            {
                case ActionTypes.DOSPIN:
                    return "s";
                case ActionTypes.DOCOLLECT:
                    return "c";
                case ActionTypes.DOCOLLECTBONUS:
                    return "cb";
                case ActionTypes.DOMYSTERY:
                    return "m";
                case ActionTypes.DOFSOPTION:
                    return "fso";
                case ActionTypes.DOBONUS:
                    return "b";
                case ActionTypes.DOFSBONUS:
                    return "fsb";
                case ActionTypes.DOGAMBLEOPTION:
                    return "go";
                case ActionTypes.DOGAMBLE:
                    return "g";

            }
            return "";
        }
        protected string convertActionTypeToFullString(ActionTypes actionType)
        {
            switch (actionType)
            {
                case ActionTypes.DOSPIN:
                    return "doSpin";
                case ActionTypes.DOCOLLECT:
                    return "doCollect";
                case ActionTypes.DOCOLLECTBONUS:
                    return "doCollectBonus";
                case ActionTypes.DOMYSTERY:
                    return "doMysteryScatter";
                case ActionTypes.DOFSOPTION:
                    return "doFSOption";
                case ActionTypes.DOBONUS:
                    return "doBonus";
            }
            return "";
        }
        protected List<BaseJiliActionToResponse> buildResponseList(List<string> responseList)
        {
            List<BaseJiliActionToResponse> actionResponseList = new List<BaseJiliActionToResponse>();
            for (int i = 1; i < responseList.Count; i++)
            {
                Dictionary<string, string> dicParamValues = splitResponseToParams(responseList[i - 1]);
                if (!dicParamValues.ContainsKey("na"))
                    continue;

                ActionTypes actionType = convertStringToActionType(dicParamValues["na"]);
                actionResponseList.Add(new BaseJiliActionToResponse(actionType, responseList[i]));
            }
            return actionResponseList;
        }
        protected List<BaseJiliActionToResponse> buildResponseList(List<string> responseList, ActionTypes action)
        {
            List<BaseJiliActionToResponse> actionResponseList = new List<BaseJiliActionToResponse>();
            for (int i = 0; i < responseList.Count; i++)
            {
                if (i >= 1)
                {
                    Dictionary<string, string> dicParamValues = splitResponseToParams(responseList[i - 1]);
                    if (!dicParamValues.ContainsKey("na"))
                        continue;

                    ActionTypes actionType = convertStringToActionType(dicParamValues["na"]);
                    actionResponseList.Add(new BaseJiliActionToResponse(actionType, responseList[i]));
                }
                else
                {
                    actionResponseList.Add(new BaseJiliActionToResponse(action, responseList[i]));

                }
            }
            return actionResponseList;
        }
        protected virtual double getPurchaseMultiple(BaseJiliSlotBetInfo betInfo)
        {
            return this.PurchaseFreeMultiple;
        }
        protected virtual async Task<BaseJiliSlotSpinResult> generateSpinResult(BaseJiliSlotBetInfo betInfo, string strUserID,  int websiteID, UserBonus userBonus, bool usePayLimit, bool isAffiliate, JiliFreeSpinInfo freeSpinInfo, double userBalance)
        {
            BaseJiliSlotSpinData spinData = null;
            BaseJiliSlotSpinResult result = null;

            //if (betInfo.HasRemainResponse)
            //{
            //    BaseJiliActionToResponse nextResponse = betInfo.pullRemainResponse();
            //    result = calculateResult(betInfo, nextResponse.Response, false, freeSpinInfo);

            //    if (!betInfo.HasRemainResponse)
            //        betInfo.RemainReponses = null;
            //    return result;
            //}

            float totalBet = betInfo.TotalBet;
            double realBetMoney = totalBet;

            if (SupportPurchaseFree && betInfo.PurchaseFree)
                realBetMoney = totalBet * getPurchaseMultiple(betInfo);

            if (SupportMoreBet && betInfo.MoreBet)
                realBetMoney = totalBet * MoreBetMultiple;

            spinData = await selectRandomStop(websiteID, userBonus, totalBet, false, betInfo, isAffiliate);

            double totalWin = totalBet * spinData.SpinOdd;
            if (isAffiliate || (freeSpinInfo != null) || !usePayLimit || spinData.IsEvent || await checkWebsitePayoutRate(websiteID, realBetMoney, totalWin))
            {
                do
                {
                    if (spinData.IsEvent)
                    {
                        //bool checkRet = await subtractEventMoney(websiteID, strUserID, totalWin);
                        //if (!checkRet)
                        //    break;

                        _bonusSendMessage = null;
                        _rewardedBonusMoney = totalWin;
                        _isRewardedBonus = true;
                    }
                    result = calculateResult(betInfo, spinData.SpinStrings[0], true, freeSpinInfo, userBalance, totalWin);
                    //if (spinData.SpinStrings.Count > 1)
                    //    betInfo.RemainReponses = buildResponseList(spinData.SpinStrings);
                    return result;
                } while (false);
            }

            double emptyWin = 0.0;
            if (SupportPurchaseFree && betInfo.PurchaseFree)
            {
                spinData = await selectMinStartFreeSpinData(betInfo);
                emptyWin = totalBet * spinData.SpinOdd;
                result = calculateResult(betInfo, spinData.SpinStrings[0], true, freeSpinInfo, userBalance, emptyWin);

                if (spinData.SpinStrings.Count > 1)
                    betInfo.RemainReponses = buildResponseList(spinData.SpinStrings);
            }
            else
            {
                spinData = await selectEmptySpin(websiteID, betInfo, isAffiliate);
                result = calculateResult(betInfo, spinData.SpinStrings[0], true, freeSpinInfo, userBalance, emptyWin);
            }
            sumUpWebsiteBetWin(websiteID, realBetMoney, emptyWin);
            return result;
        }

        protected byte[] backupBetInfo(BaseJiliSlotBetInfo betInfo)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    betInfo.SerializeTo(writer);
                }
                return ms.ToArray();
            }
        }
        protected byte[] backupHistoryInfo(string strGlobalUserID)
        {
            if (!_dicUserHistory.ContainsKey(strGlobalUserID))
                return null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    _dicUserHistory[strGlobalUserID].serializeTo(writer);
                }
                return ms.ToArray();
            }
        }
        protected byte[] backupFreeSpinInfo(string strGlobalUserID)
        {
            if (!_dicUserFreeSpinInfos.ContainsKey(strGlobalUserID))
                return null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    _dicUserFreeSpinInfos[strGlobalUserID].SerializeTo(writer);
                }
                return ms.ToArray();
            }
        }
        protected virtual async Task spinGame(string strUserID, int agentID, UserBonus userBonus, double userBalance, int index, int counter, Currencies currency, bool isNewBet, bool isAffiliate, JiliFreeSpinInfo freeSpinInfo)
        {
            try
            {
                string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
                BaseJiliSlotBetInfo betInfo = null;
                if (!_dicUserBetInfos.TryGetValue(strGlobalUserID, out betInfo))
                    return;

                byte[] betInfoBytes = backupBetInfo(betInfo);
                byte[] historyBytes = backupHistoryInfo(strGlobalUserID);
                byte[] freeSpinBytes = backupFreeSpinInfo(strGlobalUserID);

                BaseJiliSlotSpinResult lastResult = null;
                if (_dicUserResultInfos.ContainsKey(strGlobalUserID))
                    lastResult = _dicUserResultInfos[strGlobalUserID];

                double betMoney = betInfo.TotalBet;
                //if (betInfo.HasRemainResponse)
                //    betMoney = 0.0;

                UserBetTypes betType = UserBetTypes.Normal;
                //베팅머니의 100배로 프리스핀구입
                if (this.SupportPurchaseFree && betInfo.PurchaseFree)
                {
                    betMoney = Math.Round(betMoney * getPurchaseMultiple(betInfo), 2);
                    if (betMoney > 0.0)
                        betType = UserBetTypes.PurchaseFree;
                }
                else if (this.SupportMoreBet && betInfo.MoreBet)
                {
                    betMoney = Math.Round(betMoney * this.MoreBetMultiple, 2);
                    if (betMoney > 0.0)
                        betType = UserBetTypes.AnteBet;
                }
                //if (lastResult != null && lastResult.NextAction != ActionTypes.DOSPIN)
                //{
                //    GITMessage message = new GITMessage((ushort)SCMSG_CODE.SC_JILI_DOSPIN);
                //    message.Append(string.Format("balance={0}&balance_cash={0}&balance_bonus=0.0&frozen=Internal+server+error.+The+game+will+be+restarted.+&msg_code=11&ext_code=SystemError", Math.Round(userBalance, 2)));
                //    ToUserMessage toUserResult = new ToUserMessage((int)_gameID, message);
                //    Sender.Tell(toUserResult, Self);
                //    _logger.Warning("{0} user did DOSPIN but last result's next action is {1} : 02 {2}", strGlobalUserID, lastResult.NextAction, betInfo.LineCount);
                //    return;
                //}
                //만일 베팅머니가 유저의 밸런스보다 크다면 끝낸다.(2020.02.15)
                if (freeSpinInfo == null && (userBalance.LT(betMoney, _epsilion) || betMoney < 0.0))
                {
                    GITMessage message = new GITMessage((ushort)CSMSG_CODE.CS_JILI_NOTENOUGHMONEY);
                    //message.Append(string.Format("balance={0}&balance_cash={0}&balance_bonus=0.0&frozen=Internal+server+error.+The+game+will+be+restarted.+&msg_code=11&ext_code=SystemError", Math.Round(userBalance, 2)));
                    //message.Append("balanceError");
                    ToUserMessage toUserResult = new ToUserMessage((int)_gameID, message);
                    Sender.Tell(toUserResult, Self);
                    _logger.Warning("user balance is less than bet money in BaseJiliSlotGame::spinGame {0} balance:{1}, bet money: {2} game id:{3}",
                        strUserID, userBalance, Math.Round(betMoney, 2), _gameID);
                    return;
                }

                if (isNewBet)
                {
                    betInfo.BetTransactionID = createTransactionID();
                    betInfo.RoundID = createRoundID();
                    if (freeSpinInfo != null)
                        freeSpinInfo.RemainCount -= 1;
                }

                //결과를 생성한다.
                BaseJiliSlotSpinResult spinResult = await this.generateSpinResult(betInfo, strUserID, agentID, userBonus, true, isAffiliate, freeSpinInfo, userBalance);
                //await overrideResult(betInfo, spinResult, agentID, isAffiliate);

                //게임로그
                string strGameLog = spinResult.ResultString;
                _dicUserResultInfos[strGlobalUserID] = spinResult;

                //결과를 보내기전에 베팅정보를 디비에 보관한다.(2018.06.12)
                saveBetResultInfo(strGlobalUserID);

                //생성된 게임결과를 유저에게 보낸다.
                sendGameResult(betInfo, spinResult, strUserID, agentID, betMoney, spinResult.WinMoney, strGameLog, userBalance, index, counter, betType, currency, freeSpinInfo);

                _dicUserLastBackupBetInfos[strGlobalUserID] = betInfoBytes;
                _dicUserLastBackupResultInfos[strGlobalUserID] = lastResult;
                _dicUserLastBackupHistory[strGlobalUserID] = historyBytes;
                _dicUserLastBackupFreeSpinInfos[strGlobalUserID] = freeSpinBytes;

            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::spinGame {0}", ex);
            }
        }
        protected virtual async Task overrideResult(BaseJiliSlotBetInfo betInfo, BaseJiliSlotSpinResult result, int agentID, bool isAffiliate)
        {

        }

        protected virtual void checkFreeSpinCompletion(GITMessage message, JiliFreeSpinInfo freeSpinInfo, string strGlobalUserID)
        {
            if (freeSpinInfo == null || freeSpinInfo.RemainCount > 0)
                return;

            string strResultMsg = (string)message.Pop();
            var dicParams = splitResponseToParams(strResultMsg);

            dicParams["ev"] = string.Format("FR1~{0},{1},{2},,", Math.Round(freeSpinInfo.BetPerLine, 2), ServerResLineCount, Math.Round(freeSpinInfo.TotalWin, 2));
            message.Append(convertKeyValuesToString(dicParams));
            _dicUserFreeSpinInfos.Remove(strGlobalUserID);
        }
        protected virtual void addFreeSpinBonusParams(GITMessage message, JiliFreeSpinInfo freeSpinInfo, string strGlobalUserID, double win)
        {
            if (freeSpinInfo == null)
                return;

            freeSpinInfo.TotalWin += win;

            string strResultMsg = (string)message.Pop();
            var dicParams = splitResponseToParams(strResultMsg);
            dicParams["fra"] = Math.Round(freeSpinInfo.TotalWin, 2).ToString();
            dicParams["frn"] = freeSpinInfo.RemainCount.ToString();
            dicParams["frt"] = "N";
            message.Append(convertKeyValuesToString(dicParams));
        }

        protected virtual void sendGameResult(BaseJiliSlotBetInfo betInfo, BaseJiliSlotSpinResult spinResult, string strUserID, int agentID, double betMoney, double winMoney, string strGameLog, double userBalance, int index, int counter, UserBetTypes betType, Currencies currency, JiliFreeSpinInfo freeSpinInfo)
        {
            //string strSpinResult = "";
            //if (freeSpinInfo != null && !freeSpinInfo.Pending)
            //    strSpinResult = makeSpinResultString(betInfo, spinResult, 0.0, userBalance, index, counter, false);
            //else
            //    strSpinResult = makeSpinResultString(betInfo, spinResult, betMoney, userBalance, index, counter, false);

            GITMessage message = new GITMessage((ushort)SCMSG_CODE.SC_JILI_DOSPIN);
            message.Append(spinResult.ResultString);

            ToUserResultMessage toUserResult = new ToUserResultMessage((int)_gameID, message, betMoney, winMoney, new GameLogInfo(GameName, "0", strGameLog), betType);
            string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
            if (_isRewardedBonus)
            {
                toUserResult.setBonusReward(_rewardedBonusMoney);
                toUserResult.insertFirstMessage(_bonusSendMessage);
            }

            toUserResult.RoundID = betInfo.RoundID;

            //for bet money win money process
            toUserResult.TransactionID = betInfo.BetTransactionID;
            
            toUserResult.BetTransactionID = betInfo.BetTransactionID;
            if (_dicUserHistory.ContainsKey(strGlobalUserID))
            {
                if (_dicUserHistory[strGlobalUserID].log.Count == 0)
                    _dicUserHistory[strGlobalUserID].bet = betMoney;
            }
            if (freeSpinInfo != null && !freeSpinInfo.Pending)
                toUserResult.FreeSpinID = freeSpinInfo.FreeSpinID;

            //빈스핀인 경우에 히스토리보관을 여기서 진행한다.
            //if (addSpinResultToHistory(strGlobalUserID, index, counter, strSpinResult, betInfo, spinResult))
            //{
            //    //if (freeSpinInfo != null && !freeSpinInfo.Pending)
            //    //    saveHistory(agentID, strUserID, index, counter, userBalance, currency);
            //    //else
            //    //    saveHistory(agentID, strUserID, index, counter, userBalance - betMoney, currency);

            //    toUserResult.TransactionID = createTransactionID();
            //    if (freeSpinInfo != null && !freeSpinInfo.Pending)
            //        checkFreeSpinCompletion(message, freeSpinInfo, strGlobalUserID);
            //}
            //else if (spinResult.NextAction == ActionTypes.DOCOLLECT || spinResult.NextAction == ActionTypes.DOCOLLECTBONUS)
            //{
            //    toUserResult.TransactionID = createTransactionID();
            //    if (freeSpinInfo != null && !freeSpinInfo.Pending)
            //        addFreeSpinBonusParams(message, freeSpinInfo, strGlobalUserID, winMoney);
            //}
            Sender.Tell(toUserResult, Self);
        }
        protected virtual UserSpinItem createUserSpinItem(int agentID, string strUserID, BaseJiliSlotBetInfo betInfo, double betMoney)
        {
            if (this.SupportMoreBet)
                return new UserSpinItem(agentID, strUserID, this.SymbolName, betInfo.BetPerLine, betInfo.LineCount, betInfo.MoreBet ? 1 : 0, betInfo.PurchaseFree ? 0 : -1, betMoney);
            else
                return new UserSpinItem(agentID, strUserID, this.SymbolName, betInfo.BetPerLine, betInfo.LineCount, -1, betInfo.PurchaseFree ? 0 : -1, betMoney);
        }

        //protected virtual void saveHistory(int agentID, string strUserID, int index, int counter, double userBalance, Currencies currency)
        //{
        //    string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
        //    //히스토리보관 및 초기화
        //    if (_dicUserHistory.ContainsKey(strGlobalUserID) && _dicUserHistory[strGlobalUserID].log.Count > 0 && _dicUserHistory[strGlobalUserID].bet > 0.0)
        //    {
        //        //리플레이데이터를 디비에 보관
        //        if (SupportReplay)
        //        {
        //            string strDetailLog = JsonConvert.SerializeObject(_dicUserHistory[strGlobalUserID]);
        //            _dicUserHistory[strGlobalUserID].rtp = Math.Round(_dicUserHistory[strGlobalUserID].win / _dicUserHistory[strGlobalUserID].baseBet, 2);
        //            if (_dicUserHistory[strGlobalUserID].rtp >= 10.0)
        //            {
        //                _dbWriter.Tell(new JiliGameHistoryDBItem(agentID, strUserID, (int)_gameID, _dicUserHistory[strGlobalUserID].bet, _dicUserHistory[strGlobalUserID].baseBet, _dicUserHistory[strGlobalUserID].win, _dicUserHistory[strGlobalUserID].rtp,
        //                    strDetailLog, GameUtils.GetCurrentUnixTimestampMillis(), currency.ToString()));
        //            }
        //        }

        //        //게임히스토리디베에 보관
        //        string strHistoryDetail = JsonConvert.SerializeObject(_dicUserHistory[strGlobalUserID].log);
        //        _dbWriter.Tell(new JiliGameRecentHistoryDBItem(agentID, strUserID, (int)_gameID, userBalance, _dicUserHistory[strGlobalUserID].bet, _dicUserHistory[strGlobalUserID].win, "", strHistoryDetail, GameUtils.GetCurrentUnixTimestampMillis(), currency.ToString()));
        //    }
        //    _dicUserHistory.Remove(strGlobalUserID);
        //}
        protected virtual bool addSpinResultToHistory(string strGlobalUserID, int index, int counter, string strSpinResult, BaseJiliSlotBetInfo betInfo, BaseJiliSlotSpinResult spinResult)
        {
            if (!_dicUserHistory.ContainsKey(strGlobalUserID))
                return false;

            BaseJiliHistoryItem historyItem = new BaseJiliHistoryItem();
            historyItem.cr = string.Format("symbol={0}&c={1}&repeat=0&action=doSpin&index={2}&counter={3}&l={4}", SymbolName, betInfo.BetPerLine, index, counter, ClientReqLineCount);
            if (SupportPurchaseFree && betInfo.PurchaseFree)
                historyItem.cr += "&pur=0";
            if (SupportMoreBet)
            {
                if (betInfo.MoreBet)
                    historyItem.cr += "&bl=1";
                else
                    historyItem.cr += "&bl=0";
            }
            historyItem.sr = strSpinResult;

            _dicUserHistory[strGlobalUserID].log.Add(historyItem);
            if (betInfo.HasRemainResponse)
                return false;

            _dicUserHistory[strGlobalUserID].baseBet = betInfo.TotalBet;
            _dicUserHistory[strGlobalUserID].win = spinResult.TotalWin;

            //빈스핀인 경우이다.
            if (spinResult.NextAction == ActionTypes.DOSPIN)
                return true;

            return false;
        }

        #region 스핀자료처리부분
        protected OddAndIDData selectOddAndIDFromProbsWithRange(SortedDictionary<double, int[]> oddProbs, int totalCount, double minOdd, double maxOdd)
        {
            int random = Pcg.Default.Next(0, totalCount);
            int sum = 0;
            foreach (KeyValuePair<double, int[]> pair in oddProbs)
            {
                if (pair.Key < minOdd)
                    continue;

                if (pair.Key > maxOdd)
                    break;

                sum += pair.Value.Length;
                if (random < sum)
                {
                    OddAndIDData ret = new OddAndIDData();
                    ret.ID = pair.Value[Pcg.Default.Next(0, pair.Value.Length)];
                    ret.Odd = pair.Key;
                    return ret;
                }
            }
            return null;
        }
        protected OddAndIDData selectOddAndIDFromProbsWithRange(SortedDictionary<double, int[]> oddProbs, double minOdd, double maxOdd)
        {
            int totalCount = 0;
            foreach (KeyValuePair<double, int[]> pair in oddProbs)
            {
                if (pair.Key < minOdd)
                    continue;
                if (pair.Key > maxOdd)
                    break;
                totalCount += pair.Value.Length;
            }

            if (totalCount == 0)
                return null;

            int random = Pcg.Default.Next(0, totalCount);
            int sum = 0;
            foreach (KeyValuePair<double, int[]> pair in oddProbs)
            {
                if (pair.Key < minOdd)
                    continue;

                if (pair.Key > maxOdd)
                    break;

                sum += pair.Value.Length;
                if (random < sum)
                {
                    OddAndIDData ret = new OddAndIDData();
                    ret.ID = pair.Value[Pcg.Default.Next(0, pair.Value.Length)];
                    ret.Odd = pair.Key;
                    return ret;
                }
            }
            return null;
        }
        protected OddAndIDData selectOddAndIDFromProbs(SortedDictionary<double, int[]> oddProbs, int totalCount)
        {
            int random = Pcg.Default.Next(0, totalCount);
            int sum = 0;
            foreach (KeyValuePair<double, int[]> pair in oddProbs)
            {
                sum += pair.Value.Length;
                if (random < sum)
                {
                    OddAndIDData ret = new OddAndIDData();
                    ret.ID = pair.Value[Pcg.Default.Next(0, pair.Value.Length)];
                    ret.Odd = pair.Key;
                    return ret;
                }
            }
            return null;
        }
        protected double selectOddFromProbs(SortedDictionary<double, int> oddProbs, int totalCount)
        {
            int random = Pcg.Default.Next(0, totalCount);
            int sum = 0;
            foreach (KeyValuePair<double, int> pair in oddProbs)
            {
                sum += pair.Value;
                if (random < sum)
                    return pair.Key;
            }
            return oddProbs.First().Key;
        }

        protected virtual async Task<BaseJiliSlotSpinData> selectRandomStop(int websiteID, BaseJiliSlotBetInfo betInfo, bool isMoreBet, bool isAffiliate)
        {
            OddAndIDData selectedOddAndID = await selectRandomOddAndID(websiteID, betInfo, isMoreBet, isAffiliate);

            var spinDataDocument = await Context.System.ActorSelection("/user/spinDBReaders").Ask<BsonDocument>(new SelectSpinDataByIDRequest(GameName, selectedOddAndID.ID), TimeSpan.FromSeconds(10.0));
            return convertBsonToSpinData(spinDataDocument);
        }
        protected virtual async Task<OddAndIDData> selectRandomOddAndID(int websiteID, BaseJiliSlotBetInfo betInfo, bool isMoreBet, bool isAffiliate)
        {
            double payoutRate = getPayoutRate(websiteID, isAffiliate);
            double randomDouble = Pcg.Default.NextDouble(0.0, 100.0);
            int selectedID = 0;
            int affliateCnt = 0;

            if (randomDouble >= payoutRate || payoutRate == 0.0)
                selectedID = Pcg.Default.Next(1, _emptySpinCount + 1);
            else if (SupportMoreBet && isMoreBet)
            {
                if (!isAffiliate)
                    selectedID = _anteBetMinusZeroCount + Pcg.Default.Next(1, _naturalSpinCount - _anteBetMinusZeroCount + 1);
                else
                {
                    affliateCnt = _naturalSpinCount - _anteBetMinusZeroCount;
                    selectedID = _anteBetMinusZeroCount + Pcg.Default.Next(1, _naturalSpinCount - _anteBetMinusZeroCount + 1);
                }
            }
            else
            {
                if (!isAffiliate)
                    selectedID = Pcg.Default.Next(1, _naturalSpinCount + 1);
                else
                {
                    selectedID = Pcg.Default.Next(1, _naturalSpinCount + 1);
                }
            }

            OddAndIDData selectedOddAndID = new OddAndIDData();
            selectedOddAndID.ID = selectedID;
            return selectedOddAndID;
        }

        protected virtual async Task<BaseJiliSlotSpinData> selectEmptySpin(int websiteID, BaseJiliSlotBetInfo betInfo, bool isAffiliate)
        {
            int id = Pcg.Default.Next(1, _emptySpinCount + 1);
            var spinDataDocument = await Context.System.ActorSelection("/user/spinDBReaders").Ask<BsonDocument>(new SelectSpinDataByIDRequest(GameName, id), TimeSpan.FromSeconds(10.0));
            return convertBsonToSpinData(spinDataDocument);
        }
        protected virtual BaseJiliSlotSpinData convertBsonToSpinData(BsonDocument document)
        {
            int spinType = (int)document["spintype"];
            double spinOdd = (double)document["odd"];
            string strData = (string)document["data"];

            List<string> spinResponses = new List<string>(strData.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries));

            return new BaseJiliSlotSpinData(spinType, spinOdd, spinResponses);
        }
        protected virtual async Task<BaseJiliSlotSpinData> selectRandomStartFreeSpinData(BaseJiliSlotBetInfo betInfo)
        {
            try
            {
                var spinDataDocument = await Context.System.ActorSelection("/user/spinDBReaders").Ask<BsonDocument>(
                        new SelectPurchaseSpinRequest(GameName, HasPurEnableOption ? StartSpinSearchTypes.SPECIFIC : StartSpinSearchTypes.GENERAL),
                        TimeSpan.FromSeconds(10.0));

                return convertBsonToSpinData(spinDataDocument);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::selectRandomStartFreeSpinData {0}", ex);
                return null;
            }
        }
        protected virtual async Task<BaseJiliSlotSpinData> selectMinStartFreeSpinData(BaseJiliSlotBetInfo betInfo)
        {
            try
            {
                BsonDocument spinDataDocument = null;
                if (HasPurEnableOption)
                {
                    spinDataDocument = await Context.System.ActorSelection("/user/spinDBReaders").Ask<BsonDocument>(
                            new SelectSpinTypeOddRangeRequest(GameName, -1, PurchaseFreeMultiple * 0.2, PurchaseFreeMultiple * 0.5, 0), TimeSpan.FromSeconds(10.0));
                }
                else
                {
                    spinDataDocument = await Context.System.ActorSelection("/user/spinDBReaders").Ask<BsonDocument>(
                            new SelectSpinTypeOddRangeRequest(GameName, 1, PurchaseFreeMultiple * 0.2, PurchaseFreeMultiple * 0.5), TimeSpan.FromSeconds(10.0));
                }
                return convertBsonToSpinData(spinDataDocument);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::selectMinStartFreeSpinData {0}", ex);
                return null;
            }
        }
        #endregion

        protected virtual async Task<BaseJiliSlotSpinData> selectPurchaseFreeSpin(int websiteID, BaseJiliSlotBetInfo betInfo, double baseBet, UserBonus userBonus, bool isAffiliate)
        {
            double payoutRate = getPayoutRate(websiteID, isAffiliate);
            double targetC = PurchaseFreeMultiple * payoutRate / 100.0;
            if (targetC >= _totalFreeSpinWinRate)
                targetC = _totalFreeSpinWinRate;

            if (targetC < _minFreeSpinWinRate)
                targetC = _minFreeSpinWinRate;

            double x = (_totalFreeSpinWinRate - targetC) / (_totalFreeSpinWinRate - _minFreeSpinWinRate);
            double y = 1.0 - x;

            BaseJiliSlotSpinData spinData = null;
            if (Pcg.Default.NextDouble(0.0, 1.0) <= x)
                spinData = await selectMinStartFreeSpinData(betInfo);
            else
                spinData = await selectRandomStartFreeSpinData(betInfo);
            return spinData;
        }
        protected virtual async Task<BaseJiliSlotSpinData> selectRangeSpinData(int websiteID, double minOdd, double maxOdd, BaseJiliSlotBetInfo betInfo, bool isAffiliate)
        {
            var spinDataDocument = await Context.System.ActorSelection("/user/spinDBReaders").Ask<BsonDocument>(
                    new SelectSpinTypeOddRangeRequest(GameName, -1, minOdd, maxOdd), TimeSpan.FromSeconds(10.0));

            if (spinDataDocument == null)
                return null;

            return convertBsonToSpinData(spinDataDocument);
        }
        public virtual async Task<BaseJiliSlotSpinData> selectRandomStop(int websiteID, UserBonus userBonus, double baseBet, bool isChangedLineCount, BaseJiliSlotBetInfo betInfo, bool isAffiliate)
        {
            //프리스핀구입을 먼저 처리한다.
            if (this.SupportPurchaseFree && betInfo.PurchaseFree)
                return await selectPurchaseFreeSpin(websiteID, betInfo, baseBet, userBonus, isAffiliate);

            if (userBonus != null && userBonus is UserRangeOddEventBonus)
            {
                UserRangeOddEventBonus rangeOddBonus = userBonus as UserRangeOddEventBonus;
                if (baseBet.LE(rangeOddBonus.MaxBet, _epsilion))
                {
                    BaseJiliSlotSpinData spinDataEvent = await selectRangeSpinData(websiteID, rangeOddBonus.MinOdd, rangeOddBonus.MaxOdd, betInfo, isAffiliate);
                    if (spinDataEvent != null)
                    {
                        spinDataEvent.IsEvent = true;
                        return spinDataEvent;
                    }
                }
            }
            if (SupportMoreBet && betInfo.MoreBet)
                return await selectRandomStop(websiteID, betInfo, true, isAffiliate);
            else
                return await selectRandomStop(websiteID, betInfo, false, isAffiliate);
        }
        protected override async Task<bool> loadUserHistoricalData(int agentID, string strUserID, bool isNewEnter)
        {
            try
            {
                string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
                string strKey = string.Format("{0}_{1}", strGlobalUserID, _gameID);
                byte[] betInfoData = await RedisDatabase.RedisCache.StringGetAsync(strKey);
                if (betInfoData != null)
                {
                    using (var stream = new MemoryStream(betInfoData))
                    {
                        BinaryReader reader = new BinaryReader(stream);
                        BaseJiliSlotBetInfo betInfo = restoreBetInfo(strGlobalUserID, reader);
                        if (betInfo != null)
                            _dicUserBetInfos[strGlobalUserID] = betInfo;
                    }
                }

                strKey = string.Format("{0}_{1}_result", strGlobalUserID, _gameID);
                byte[] resultInfoData = await RedisDatabase.RedisCache.StringGetAsync(strKey);
                if (resultInfoData != null)
                {
                    using (var stream = new MemoryStream(resultInfoData))
                    {
                        BinaryReader reader = new BinaryReader(stream);
                        BaseJiliSlotSpinResult resultInfo = restoreResultInfo(strGlobalUserID, reader);
                        if (resultInfo != null)
                            _dicUserResultInfos[strGlobalUserID] = resultInfo;
                    }
                }
                strKey = string.Format("{0}_{1}_freespin", strGlobalUserID, _gameID);
                byte[] freeSpinInfoData = await RedisDatabase.RedisCache.StringGetAsync(strKey);
                if (freeSpinInfoData != null)
                {
                    using (var stream = new MemoryStream(freeSpinInfoData))
                    {
                        BinaryReader reader = new BinaryReader(stream);
                        JiliFreeSpinInfo freeSpinInfo = restoreFreeSpinInfo(strGlobalUserID, reader);
                        if (freeSpinInfo != null)
                            _dicUserFreeSpinInfos[strGlobalUserID] = freeSpinInfo;
                    }
                }
                strKey = string.Format("{0}_{1}_history", strGlobalUserID, _gameID);
                byte[] historyInfoData = await RedisDatabase.RedisCache.StringGetAsync(strKey);
                if (historyInfoData != null)
                {
                    using (var stream = new MemoryStream(historyInfoData))
                    {
                        BinaryReader reader = new BinaryReader(stream);
                        BaseJiliHistory userHistory = restoreHistory(reader);
                        if (userHistory != null)
                            _dicUserHistory[strGlobalUserID] = userHistory;
                    }
                }
                strKey = string.Format("{0}_{1}_setting", strGlobalUserID, _gameID);
                string strSetting = await RedisDatabase.RedisCache.StringGetAsync(strKey);
                if (strSetting != null)
                    _dicUserSettings[strGlobalUserID] = strSetting;
            }

            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::loadUserHistoricalData {0}", ex);
                return false;
            }
            return await base.loadUserHistoricalData(agentID, strUserID, isNewEnter);
        }
        protected virtual BaseJiliSlotBetInfo restoreBetInfo(string strUserID, BinaryReader reader)
        {
            BaseJiliSlotBetInfo betInfo = new BaseJiliSlotBetInfo();
            betInfo.SerializeFrom(reader);
            return betInfo;
        }
        protected virtual BaseJiliSlotBetInfo newBetInfo()
        {
            return new BaseJiliSlotBetInfo();
        }
        protected BaseJiliHistory restoreHistory(BinaryReader reader)
        {
            try
            {
                BaseJiliHistory history = new BaseJiliHistory();
                history.serializeFrom(reader);
                return history;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::restoreHistory {0}", ex);
                return null;
            }
        }
        protected virtual BaseJiliSlotSpinResult restoreResultInfo(string strGlobalUserID, BinaryReader reader)
        {
            BaseJiliSlotSpinResult result = new BaseJiliSlotSpinResult();
            result.SerializeFrom(reader);
            return result;
        }
        protected virtual JiliFreeSpinInfo restoreFreeSpinInfo(string strGlobalUserID, BinaryReader reader)
        {
            JiliFreeSpinInfo freeSpinInfo = new JiliFreeSpinInfo();
            freeSpinInfo.SerializeFrom(reader);
            return freeSpinInfo;
        }

        protected virtual void saveBetResultInfo(string strGlobalUserID)
        {
            try
            {
                if (_dicUserBetInfos.ContainsKey(strGlobalUserID))
                {
                    byte[] betInfoBytes = _dicUserBetInfos[strGlobalUserID].convertToByte();
                    _redisWriter.Tell(new UserBetInfoWrite(strGlobalUserID, _gameID, betInfoBytes, false), Self);
                }
                else
                {
                    _redisWriter.Tell(new UserBetInfoWrite(strGlobalUserID, _gameID, null, false), Self);
                }
                if (_dicUserResultInfos.ContainsKey(strGlobalUserID))
                {
                    byte[] resultInfoBytes = _dicUserResultInfos[strGlobalUserID].convertToByte();
                    _redisWriter.Tell(new UserResultInfoWrite(strGlobalUserID, _gameID, resultInfoBytes, false), Self);
                }
                else
                {
                    _redisWriter.Tell(new UserResultInfoWrite(strGlobalUserID, _gameID, null, false), Self);
                }
                if (_dicUserFreeSpinInfos.ContainsKey(strGlobalUserID))
                {
                    byte[] freeSpinInfoBytes = _dicUserFreeSpinInfos[strGlobalUserID].convertToByte();
                    _redisWriter.Tell(new UserFreeSpinInfoWrite(strGlobalUserID, _gameID, freeSpinInfoBytes, false), Self);
                }
                else
                {
                    _redisWriter.Tell(new UserFreeSpinInfoWrite(strGlobalUserID, _gameID, null, false), Self);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::saveBetInfo {0}", ex);
            }
        }
        protected void removeJTokenField(JToken token, string strFieldName)
        {
            JContainer container = token as JContainer;
            if (container == null)
                return;

            JToken removeToken = null;
            foreach (JToken el in container.Children())
            {
                JProperty p = el as JProperty;
                if (p != null && p.Name == strFieldName)
                {
                    removeToken = el;
                    break;
                }
            }
            if (removeToken != null)
                removeToken.Remove();
        }
        protected string serializeJsonSpecial(JToken token)
        {
            var serializer = new JsonSerializer();
            var stringWriter = new StringWriter();
            using (var writer = new JsonTextWriter(stringWriter))
            {
                writer.QuoteName = false;
                serializer.Serialize(writer, token);
            }
            return stringWriter.ToString();
        }

        protected override async Task onUserExitGame(int agentID, string strUserID, bool userRequested)
        {
            try
            {
                string strGlobalUserID = string.Format("{0}_{1}", agentID, strUserID);
                if (_dicUserBetInfos.ContainsKey(strGlobalUserID))
                {
                    byte[] betInfoBytes = _dicUserBetInfos[strGlobalUserID].convertToByte();
                    await _redisWriter.Ask(new UserBetInfoWrite(strGlobalUserID, _gameID, betInfoBytes, true));
                    _dicUserBetInfos.Remove(strGlobalUserID);
                }
                else
                {
                    await _redisWriter.Ask(new UserBetInfoWrite(strGlobalUserID, _gameID, null, true));
                }
                if (_dicUserResultInfos.ContainsKey(strGlobalUserID))
                {
                    byte[] resultInfoBytes = _dicUserResultInfos[strGlobalUserID].convertToByte();
                    await _redisWriter.Ask(new UserResultInfoWrite(strGlobalUserID, _gameID, resultInfoBytes, true));
                    _dicUserResultInfos.Remove(strGlobalUserID);
                }
                else
                {
                    await _redisWriter.Ask(new UserResultInfoWrite(strGlobalUserID, _gameID, null, true));
                }
                if (_dicUserFreeSpinInfos.ContainsKey(strGlobalUserID))
                {
                    byte[] freeSpinInfoBytes = _dicUserFreeSpinInfos[strGlobalUserID].convertToByte();
                    await _redisWriter.Ask(new UserFreeSpinInfoWrite(strGlobalUserID, _gameID, freeSpinInfoBytes, true));
                    _dicUserFreeSpinInfos.Remove(strGlobalUserID);
                }
                else
                {
                    await _redisWriter.Ask(new UserFreeSpinInfoWrite(strGlobalUserID, _gameID, null, true));
                }

                if (_dicUserHistory.ContainsKey(strGlobalUserID))
                {
                    byte[] historyByteData = _dicUserHistory[strGlobalUserID].convertToByte();
                    await _redisWriter.Ask(new UserHistoryWrite(strGlobalUserID, _gameID, historyByteData, true));
                    _dicUserHistory.Remove(strGlobalUserID);
                }
                else
                {
                    await _redisWriter.Ask(new UserHistoryWrite(strGlobalUserID, _gameID, null, true));
                }
                if (_dicUserSettings.ContainsKey(strGlobalUserID))
                {
                    string strSetting = _dicUserSettings[strGlobalUserID];
                    await _redisWriter.Ask(new UserSettingWrite(strGlobalUserID, _gameID, strSetting, true));
                    _dicUserSettings.Remove(strGlobalUserID);
                }
                _dicUserLastBackupResultInfos.Remove(strGlobalUserID);
                _dicUserLastBackupBetInfos.Remove(strGlobalUserID);
                _dicUserLastBackupHistory.Remove(strGlobalUserID);
                _dicUserLastBackupFreeSpinInfos.Remove(strGlobalUserID);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::onUserExitGame GameID:{0} {1}", _gameID, ex);
            }

            await base.onUserExitGame(agentID, strUserID, userRequested);
        }


        //protected override void onUserFreeSpinCancel(FreeSpinCancelRequest request)
        //{
        //    string strGlobalUserID = string.Format("{0}_{1}", request.AgentID, request.UserID);
        //    if (!_dicUserFreeSpinInfos.ContainsKey(strGlobalUserID))
        //        return;

        //    if (!_dicUserFreeSpinInfos[strGlobalUserID].Pending)
        //        return;

        //    _dicUserFreeSpinInfos.Remove(strGlobalUserID);
        //    return;
        //}
        protected virtual void onDoFSBonus(int companyID, string strUserID, GITMessage message, double userBalance)
        {

        }
        protected virtual void onDoGambleOption(int companyID, string strUserID, GITMessage message, double userBalance)
        {

        }
        protected virtual async Task onDoGamble(int companyID, string strUserID, GITMessage message, double userBalance, bool isAffiliate)
        {

        }
        protected override async Task onUserEnterGame(int agentID, string strUserID)
        {
        }
    }

    public class OddAndIDData
    {
        public int ID { get; set; }
        public double Odd { get; set; }

        public OddAndIDData()
        {

        }
        public OddAndIDData(int id, double odd)
        {
            this.ID = id;
            this.Odd = odd;
        }
    }
    public class SymbolAndReplayRequest
    {

    }
    public class SymbolAndReplayResponse
    {
        public string Symbol { get; set; }
        public bool SupportReplay { get; set; }
        public string GameName { get; set; }

        public SymbolAndReplayResponse(string symbol, bool supportReplay, string strGameName)
        {
            this.Symbol = symbol;
            this.SupportReplay = supportReplay;
            this.GameName = strGameName;
        }
    }
}
