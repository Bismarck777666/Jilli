using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using GITProtocol;
using Akka.Routing;
using Akka.Event;
using SlotGamesNode.GameLogics;
using PCGSharp;

namespace SlotGamesNode.Database
{
    public class RedisWriter : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Logging.GetLogger(Context);
        public RedisWriter()
        {
            ReceiveAsync<UserBetInfoWrite>      (onUserBetInfoWrite);
            ReceiveAsync<UserResultInfoWrite>   (onUserResultInfoWrite);
        }

        private async Task onUserBetInfoWrite(UserBetInfoWrite writeInfo)
        {
            try
            {
                string strKey = string.Format("{0}_{1}", writeInfo.GlobalUserID, writeInfo.GameID);
                if (writeInfo.BetInfoData != null)
                {
                    await RedisDatabase.RedisCache.StringSetAsync(strKey, writeInfo.BetInfoData, TimeSpan.FromDays(2));
                }
                else
                {
                    await RedisDatabase.RedisCache.KeyDeleteAsync(strKey);
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Exception has been occurred in RedisWriter::onUserBetInfoWrite {0}", ex);
            }
            if(writeInfo.IsWait)
                Sender.Tell(true);
        }
        private async Task onUserSettingWrite(UserSettingWrite writeInfo)
        {
            try
            {
                string strKey = string.Format("{0}_{1}_setting", writeInfo.GlobalUserID, writeInfo.GameID);
                if (writeInfo.Setting != null)
                {
                    await RedisDatabase.RedisCache.StringSetAsync(strKey, writeInfo.Setting, TimeSpan.FromDays(2));
                }
                else
                {
                    await RedisDatabase.RedisCache.KeyDeleteAsync(strKey);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in RedisWriter::onUserSettingWrite {0}", ex);
            }
            if (writeInfo.IsWait)
                Sender.Tell(true);
        }
        private async Task onUserFreeSpinInfoWrite(UserFreeSpinInfoWrite writeInfo)
        {
            try
            {
                string strKey = string.Format("{0}_{1}_freespin", writeInfo.GlobalUserID, writeInfo.GameID);
                if (writeInfo.FreeSpinInfoData != null)
                {
                    await RedisDatabase.RedisCache.StringSetAsync(strKey, writeInfo.FreeSpinInfoData, TimeSpan.FromDays(2));
                }
                else
                {
                    await RedisDatabase.RedisCache.KeyDeleteAsync(strKey);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in RedisWriter::onUserFreeSpinInfoWrite {0}", ex);
            }
            if (writeInfo.IsWait)
                Sender.Tell(true);
        }
        private async Task onUserResultInfoWrite(UserResultInfoWrite writeInfo)
        {
            try
            {
                string strKey = string.Format("{0}_{1}_result", writeInfo.GlobalUserID, writeInfo.GameID);
                if (writeInfo.ResultInfoData != null)
                {
                    await RedisDatabase.RedisCache.StringSetAsync(strKey, writeInfo.ResultInfoData, TimeSpan.FromDays(2));
                }
                else
                {
                    await RedisDatabase.RedisCache.KeyDeleteAsync(strKey);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in RedisWriter::onUserResultInfoWrite {0}", ex);
            }
            if(writeInfo.IsWait)
                Sender.Tell(true);
        }
        protected override void PostStop()
        {
            base.PostStop();
        }
    }

    public class UserBetInfoWrite : IConsistentHashable
    {
        public string   GlobalUserID    { get; private set; }
        public GAMEID   GameID          { get; private set; }
        public byte[]   BetInfoData     { get; private set; }
        public bool     IsWait          { get; private set; }
        public object   ConsistentHashKey => GlobalUserID;

        public UserBetInfoWrite()
        {

        }
        public UserBetInfoWrite(string strGlobalUserID, GAMEID gameID, byte[] betInfoData, bool isWait)
        {
            this.GlobalUserID   = strGlobalUserID;
            this.GameID         = gameID;
            this.BetInfoData    = betInfoData;
            this.IsWait         = isWait;
        }        
    }

    public class UserResultInfoWrite : IConsistentHashable
    {
        public string   GlobalUserID        { get; private set; }
        public GAMEID   GameID              { get; private set; }
        public byte[]   ResultInfoData      { get; private set; }
        public bool     IsWait              { get; private set; }
        public object   ConsistentHashKey   => GlobalUserID;

        public UserResultInfoWrite()
        {

        }
        public UserResultInfoWrite(string strGlobalUserID, GAMEID gameID, byte[] resultInfoData, bool isWait)
        {
            this.GlobalUserID       = strGlobalUserID;
            this.GameID             = gameID;
            this.ResultInfoData     = resultInfoData;
            this.IsWait             = isWait;
        }
    }
    public class UserFreeSpinInfoWrite : IConsistentHashable
    {
        public string GlobalUserID { get; private set; }
        public GAMEID GameID { get; private set; }
        public byte[] FreeSpinInfoData { get; private set; }
        public bool IsWait { get; private set; }
        public object ConsistentHashKey
        {
            get
            {
                return GlobalUserID;
            }
        }

        public UserFreeSpinInfoWrite()
        {

        }
        public UserFreeSpinInfoWrite(string strGlobalUserID, GAMEID gameID, byte[] freeSpinInfoData, bool isWait)
        {
            this.GlobalUserID = strGlobalUserID;
            this.GameID = gameID;
            this.FreeSpinInfoData = freeSpinInfoData;
            this.IsWait = isWait;
        }
    }
    public class UserHistoryWrite : IConsistentHashable
    {
        public string GlobalUserID { get; private set; }
        public GAMEID GameID { get; private set; }
        public byte[] HistoryData { get; private set; }
        public bool IsWait { get; private set; }
        public object ConsistentHashKey
        {
            get
            {
                return GlobalUserID;
            }
        }

        public UserHistoryWrite()
        {

        }
        public UserHistoryWrite(string strGlobalUserID, GAMEID gameID, byte[] historyData, bool isWait)
        {
            this.GlobalUserID = strGlobalUserID;
            this.GameID = gameID;
            this.HistoryData = historyData;
            this.IsWait = isWait;
        }
    }
    public class UserSettingWrite : IConsistentHashable
    {
        public string GlobalUserID { get; private set; }
        public GAMEID GameID { get; private set; }
        public string Setting { get; private set; }
        public bool IsWait { get; private set; }
        public object ConsistentHashKey
        {
            get
            {
                return GlobalUserID;
            }
        }

        public UserSettingWrite()
        {

        }
        public UserSettingWrite(string strGlobalUserID, GAMEID gameID, string strSetting, bool isWait)
        {
            this.GlobalUserID = strGlobalUserID;
            this.GameID = gameID;
            this.Setting = strSetting;
            this.IsWait = isWait;
        }
    }
}
