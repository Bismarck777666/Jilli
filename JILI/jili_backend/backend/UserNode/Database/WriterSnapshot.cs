using GITProtocol;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UserNode.Database
{
    public class WriterSnapshot
    {
        private static WriterSnapshot _sInstance = new WriterSnapshot();

        public static WriterSnapshot Instance
        {
            get
            {
                return _sInstance;
            }
        }

        private List<BaseInsertItem>                        _dbInsertItems              = new List<BaseInsertItem>();
        private Dictionary<long, UserStateUpdateItem>       _dicPlayerStateUpdates      = new Dictionary<long, UserStateUpdateItem>();
        private Dictionary<long, PlayerBalanceUpdateItem>   _dicPlayerBalanceUpates     = new Dictionary<long, PlayerBalanceUpdateItem>();
        private Dictionary<long, UserBetMoneyUpdateItem>    _dicUserBetMoneyUpdates     = new Dictionary<long, UserBetMoneyUpdateItem>();
        private List<GameLogItem>                           _gameLogItems               = new List<GameLogItem>();
        private List<int>                                   _createdAgentGameLogTables  = new List<int>();
        private Dictionary<string, ApiTransactionItem>      _apiTransactions            = new Dictionary<string, ApiTransactionItem>();
        private List<DepositTransactionUpdateItem>          _updatedDepositTransactions = new List<DepositTransactionUpdateItem>();
        private List<FailedTransactionItem>                 _failedTransactions         = new List<FailedTransactionItem>();

        public void insertDBItem(BaseInsertItem insertItem)
        {
            _dbInsertItems.Add(insertItem);
        }

        public void insertApiTransaction(ApiTransactionItem item)
        {
            _apiTransactions[item.TransactionID] = item;
        }
        public void updateDepositTransaction(DepositTransactionUpdateItem item)
        {
            if(_apiTransactions.ContainsKey(item.TransactionID))
            {
                _apiTransactions[item.TransactionID].PlatformTransID = item.PlatformTransID;
                _apiTransactions[item.TransactionID].Timestamp       = item.Timestamp;
            }
            else
            {
                _updatedDepositTransactions.Add(item);
            }
        }
        public List<ApiTransactionItem> fetchApiTransactionItems(int count = 1000)
        {
            List<ApiTransactionItem> items = new List<ApiTransactionItem>();
            foreach(KeyValuePair<string, ApiTransactionItem> pair in _apiTransactions)
            {
                items.Add(pair.Value);
                if (items.Count >= count)
                    break;            
            }
            foreach(var item in items)
                _apiTransactions.Remove(item.TransactionID);
            return items;
        }
        public void PushApiTransactionItems(List<ApiTransactionItem> items)
        {
            foreach (var item in items)
                _apiTransactions[item.TransactionID] = item;
        }
        public void updatePlayerBalanceUpdate(PlayerBalanceUpdateItem balanceUpdateItem)
        {
            long playerID = balanceUpdateItem.PlayerID;
            if (!_dicPlayerBalanceUpates.ContainsKey(playerID))
                _dicPlayerBalanceUpates[playerID] = balanceUpdateItem;
        }
        public void updatePlayerState(UserStateUpdateItem stateUpdateItem)
        {
            _dicPlayerStateUpdates[stateUpdateItem.PlayerID] = stateUpdateItem;
        }
        public double fetchUserBalanceUpdates(long userDBID)
        {
            double balanceIncrement = 0.0;
            if (_dicPlayerBalanceUpates.ContainsKey(userDBID))
            {
                balanceIncrement = _dicPlayerBalanceUpates[userDBID].Balance;
                _dicPlayerBalanceUpates.Remove(userDBID);
            }
            _dicPlayerStateUpdates.Remove(userDBID);
            return balanceIncrement;
        }
        
        public void updateUserBetMoney(UserBetMoneyUpdateItem item)
        {
            _dicUserBetMoneyUpdates[item.UserDBID] = item;
        }

        public void PushUserBetMoneyUpdates(List<UserBetMoneyUpdateItem> updateItems)
        {
            for(int i = 0; i < updateItems.Count; i++)
            {
                if (_dicUserBetMoneyUpdates.ContainsKey(updateItems[i].UserDBID))
                    continue;
                _dicUserBetMoneyUpdates[updateItems[i].UserDBID] = updateItems[i];
            }
        }
        public List<UserBetMoneyUpdateItem> PopUserBetMoneyUpdates(int count = 100)
        {
            if (_dicUserBetMoneyUpdates.Count == 0)
                return null;

            List<UserBetMoneyUpdateItem> updateItems = new List<UserBetMoneyUpdateItem>();
            foreach (KeyValuePair<long, UserBetMoneyUpdateItem> pair in _dicUserBetMoneyUpdates)
            {
                updateItems.Add(pair.Value);
                if (updateItems.Count >= count)
                    break;
            }
            for (int i = 0; i < updateItems.Count; i++)
                _dicUserBetMoneyUpdates.Remove(updateItems[i].UserDBID);

            return updateItems;
        }
        
        public void insertGameLog(GameLogItem gameLogItem)
        {
            _gameLogItems.Add(gameLogItem);
        }
        public void insertFailedTransaction(FailedTransactionItem item)
        {
            _failedTransactions.Add(item);
        }
        public List<BaseInsertItem> PopDBInsertItems(int count = 2000)
        {
            if (_dbInsertItems.Count == 0)
                return null;

            List<BaseInsertItem> insertItems = new List<BaseInsertItem>();
            for (int i = 0; i < _dbInsertItems.Count; i++)
            {
                insertItems.Add(_dbInsertItems[i]);
                if (insertItems.Count >= count)
                    break;
            }
            _dbInsertItems.RemoveRange(0, insertItems.Count);
            return insertItems;
        }

        public void PushDBInsertItems(List<BaseInsertItem> items)
        {
            _dbInsertItems.InsertRange(0, items);
        }

        public List<GameLogItem> PopGameLogItems(int count = 5000)
        {
            if (_gameLogItems.Count == 0)
                return null;

            List<GameLogItem> gameLogItems = new List<GameLogItem>();
            foreach (GameLogItem logItem in _gameLogItems)
            {
                gameLogItems.Add(logItem);
                if (gameLogItems.Count >= count)
                    break;
            }

            _gameLogItems.RemoveRange(0, gameLogItems.Count);
            return gameLogItems;
        }
        public List<FailedTransactionItem> PopFaildTransactions(int count = 5000)
        {
            if (_failedTransactions.Count == 0)
                return null;

            List<FailedTransactionItem> items = new List<FailedTransactionItem>();
            foreach (FailedTransactionItem item in _failedTransactions)
            {
                items.Add(item);
                if (items.Count >= count)
                    break;
            }
            _failedTransactions.RemoveRange(0, items.Count);
            return items;
        }
        public List<DepositTransactionUpdateItem> PopUpdatedDepositTransactions(int count = 5000)
        {
            if (_updatedDepositTransactions.Count == 0)
                return null;

            List<DepositTransactionUpdateItem> items = new List<DepositTransactionUpdateItem>();
            foreach (DepositTransactionUpdateItem item in _updatedDepositTransactions)
            {
                items.Add(item);
                if (items.Count >= count)
                    break;
            }
            _updatedDepositTransactions.RemoveRange(0, items.Count);
            return items;
        }
        public void PushGameLogItems(List<GameLogItem> gameLogItems)
        {
            _gameLogItems.InsertRange(0, gameLogItems);
        }
        public void PushFailedTransactionItems(List<FailedTransactionItem> items)
        {
            _failedTransactions.InsertRange(0, items);
        }
        public void PushUpdatedDepositTransactions(List<DepositTransactionUpdateItem> items)
        {
            _updatedDepositTransactions.InsertRange(0, items);
        }
        public List<UserStateUpdateItem> PopPlayerStates(int count = 5000)
        {
            if (_dicPlayerStateUpdates.Count == 0)
                return null;

            List<UserStateUpdateItem> playerStateUpdates = new List<UserStateUpdateItem>();
            foreach (KeyValuePair<long, UserStateUpdateItem> pair in _dicPlayerStateUpdates)
            {
                //Changed by Foresight(2019.09.23)
                UserStateUpdateItem stateUpdateItem = pair.Value;
                if (stateUpdateItem is UserGameStateItem)
                {
                    UserGameStateItem gameStateItem = stateUpdateItem as UserGameStateItem;
                    if (gameStateItem.State == 0)
                    {
                        //유저가 오프라인될때 현재 펜딩중인 밸런스업데이트를 함께 진행하여 출금조작시 나타날수 있는 오류를 방지한다.
                        double balanceIncrement = 0.0;
                        if (_dicPlayerBalanceUpates.ContainsKey(gameStateItem.PlayerID))
                        {
                            balanceIncrement = _dicPlayerBalanceUpates[gameStateItem.PlayerID].Balance;
                            _dicPlayerBalanceUpates.Remove(gameStateItem.PlayerID);
                        }
                        playerStateUpdates.Add(new UserOfflineStateItem(gameStateItem.PlayerID, gameStateItem.GameID, balanceIncrement));
                    }
                    else
                    {
                        playerStateUpdates.Add(pair.Value);
                    }
                }
                else
                {
                    playerStateUpdates.Add(pair.Value);
                }
                if (playerStateUpdates.Count >= count)
                    break;
            }

            for (int i = 0; i < playerStateUpdates.Count; i++)
                _dicPlayerStateUpdates.Remove(playerStateUpdates[i].PlayerID);

            return playerStateUpdates;
        }

        public void PushPlayerStates(List<UserStateUpdateItem> playerStateUpdates)
        {
            for (int i = 0; i < playerStateUpdates.Count; i++)
            {
                if (playerStateUpdates[i] is UserOfflineStateItem)
                {
                    UserOfflineStateItem offlineStateItem = playerStateUpdates[i] as UserOfflineStateItem;
                    if (offlineStateItem.BalanceIncrement != 0.0)
                    {
                        if (!_dicPlayerBalanceUpates.ContainsKey(offlineStateItem.PlayerID))
                            _dicPlayerBalanceUpates[offlineStateItem.PlayerID] = new PlayerBalanceUpdateItem(offlineStateItem.PlayerID, offlineStateItem.BalanceIncrement);
                    }

                    if (_dicPlayerStateUpdates.ContainsKey(offlineStateItem.PlayerID))
                        continue;

                    _dicPlayerStateUpdates[offlineStateItem.PlayerID] = new UserGameStateItem(offlineStateItem.PlayerID, 0, offlineStateItem.GameID);
                }
                else
                {
                    if (_dicPlayerStateUpdates.ContainsKey(playerStateUpdates[i].PlayerID))
                        continue;

                    _dicPlayerStateUpdates[playerStateUpdates[i].PlayerID] = playerStateUpdates[i];
                }
            }
        }


        public List<PlayerBalanceUpdateItem> PopBalanceUpdates(int count = 5000)
        {
            if (_dicPlayerBalanceUpates.Count == 0)
                return null;

            List<PlayerBalanceUpdateItem> balanceUpdates = new List<PlayerBalanceUpdateItem>();
            foreach (KeyValuePair<long, PlayerBalanceUpdateItem> pair in _dicPlayerBalanceUpates)
            {
                balanceUpdates.Add(pair.Value);
                if (balanceUpdates.Count >= count)
                    break;
            }
            for (int i = 0; i < balanceUpdates.Count; i++)
                _dicPlayerBalanceUpates.Remove(balanceUpdates[i].PlayerID);

            return balanceUpdates;
        }
        public void PushBalanceUpdateItems(List<PlayerBalanceUpdateItem> balanceUpdates)
        {
            for (int i = 0; i < balanceUpdates.Count; i++)
            {
                long playerID = balanceUpdates[i].PlayerID;
                if (!_dicPlayerBalanceUpates.ContainsKey(playerID))
                    _dicPlayerBalanceUpates[playerID] = balanceUpdates[i];
            }
        }
        public bool IsAgentGameLogTableCreated(int agentID)
        {
            if (_createdAgentGameLogTables.Contains(agentID))
                return true;

            return false;
        }
        public void HasCreatedAgentGameLogTable(int agentID)
        {
            if (!_createdAgentGameLogTables.Contains(agentID))
                _createdAgentGameLogTables.Add(agentID);
        }
    }

    public class BaseInsertItem
    {

    }
    public class LoginIPInsertItem : BaseInsertItem
    {
        public LoginIPInsertItem(string strUserID, string ipAddress, string strLoginData, DateTime loginTime)
        {
            this.UserID     = strUserID;
            this.IPAddress  = ipAddress;
            this.LoginData  = strLoginData;
            this.LoginTime  = loginTime;
        }

        public string       UserID      { get; private set; }
        public string       IPAddress   { get; private set; }
        public string       LoginData   { get; private set; }
        public DateTime     LoginTime   { get; private set; }
    }
    public class UserStateUpdateItem
    {
        public UserStateUpdateItem(long playerID)
        {
            this.PlayerID = playerID;
        }
        public long PlayerID { get; protected set; }
    }

    public class UserBetMoneyUpdateItem
    {
        public long     UserDBID        { get; private set; }
        public double   UserBetMoney    { get; private set; }

        public UserBetMoneyUpdateItem(long userDBID, double userBetMoney)
        {
            this.UserDBID       = userDBID;
            this.UserBetMoney   = userBetMoney;
        }
    }
    public class UserLoginStateItem : UserStateUpdateItem
    {
        public UserLoginStateItem(long playerID) : base(playerID)
        {
        }
    }

    //유저가 게임에 입장하거나 게임
    public class UserGameStateItem : UserStateUpdateItem
    {
        public UserGameStateItem(long playerID, int state, int gameID) : base(playerID)
        {
            this.State      = state;
            this.GameID     = gameID;
        }
        public int          State          { get; private set; }
        public int          GameID         { get; private set; }
    }
    public class UserOfflineStateItem : UserStateUpdateItem
    {
        public UserOfflineStateItem(long playerID, int gameID, double balanceIncrement) : base(playerID)
        {
            this.GameID             = gameID;
            this.BalanceIncrement   = balanceIncrement;
        }
        public int          GameID              { get; private set; }
        public double       BalanceIncrement    { get; private set; }
    }
    public class PlayerBalanceUpdateItem
    {
        public PlayerBalanceUpdateItem(long playerID, double balanceIncrement)
        {
            this.PlayerID  = playerID;
            this.Balance   = balanceIncrement;
        }
            
        public long     PlayerID            { get; private set; }            
        public double   Balance             { get; private set; }
    }
    public class GameLogItem
    {
        public GameLogItem(string strUserID, int gameID, string strGameName, double bet, double win, double beginMoney, double endMoney, string gameLog, int betType, DateTime logTime, int agentID)
        {
            this.UserID     = strUserID;
            this.GameID     = gameID;
            this.GameName   = strGameName;
            this.Bet        = bet;
            this.Win        = win;
            this.BeginMoney = beginMoney;
            this.EndMoney   = endMoney;
            this.GameLog    = gameLog;
            this.LogTime    = logTime;
            this.AgentID    = agentID;
            this.BetType    = betType;
        }
        
        public string       UserID      { get; private set; }
        public int          GameID      { get; private set; }
        public string       GameName    { get; private set; }
        public int          BetType     { get; private set; }
        public double       Bet         { get; private set; }
        public double       Win         { get; private set; }
        public double       BeginMoney  { get; private set; }
        public double       EndMoney    { get; private set; }
        public string       GameLog     { get; private set; }
        public DateTime     LogTime     { get; private set; }
        public int          AgentID     { get; private set; }
    }
    
    public class FetchUserBalanceUpdate
    {
        public long UserDBID { get; private set; }

        public FetchUserBalanceUpdate(long userDBID)
        {
            this.UserDBID = userDBID;
        }
    }
    
    public class ApiTransactionItem 
    {
        public string           AgentID             { get; private set; }
        public string           UserID              { get; private set; }
        public int              GameID              { get; private set; }
        public double           BetAmount           { get; private set; }
        public double           WinAmount           { get; private set; }
        public string           TransactionID       { get; private set; }
        public string           RelTransactionID    { get; private set; }
        public string           PlatformTransID     { get; set; }
        public string           RoundID             { get; private set; }
        public TransactionTypes TransactionType     { get; private set; }
        public bool             EndTransaction      { get; private set; }
        public DateTime         Timestamp           { get; set; }

        public ApiTransactionItem(string agentID, string userID, int gameID, double betAmount, double winAmount, string transactionID, string relTransactionID, string platformTransID, string roundID, TransactionTypes transactionType, bool endTransaction, DateTime timestamp)
        {
            this.AgentID            = agentID;
            this.UserID             = userID;
            this.GameID             = gameID;
            this.BetAmount          = betAmount;
            this.WinAmount          = winAmount;
            this.TransactionID      = transactionID;
            this.RelTransactionID   = relTransactionID;
            this.PlatformTransID    = platformTransID;
            this.RoundID            = roundID;
            this.TransactionType    = transactionType;
            this.EndTransaction     = endTransaction;
            this.Timestamp          = timestamp;
        }
    }

    public class DepositTransactionUpdateItem
    {
        public string   TransactionID     { get; private set; }
        public string   PlatformTransID   { get; private set; }
        public DateTime Timestamp         { get; private set; }

        public DepositTransactionUpdateItem(string transactionID, string platformTransID, DateTime timestamp)
        {
            TransactionID = transactionID;
            PlatformTransID = platformTransID;
            Timestamp = timestamp;
        }
    }
    public enum TransactionTypes
    {
        Withdraw        = 0,
        Deposit         = 1,
        Rollback        = 2,
        BetWin          = 3,
    }
    public class FailedTransactionItem
    {
        public string           AgentID          { get; private set; }
        public string           UserID           { get; private set; }
        public TransactionTypes TransactionType  { get; private set; }
        public string           TransactionID    { get; private set; }
        public string           RefTransactionID { get; private set; }
        public int              GameID           { get; private set; }
        public double           BetAmount        { get; private set; }
        public double           WinAmount        { get; private set; }
        public DateTime         UpdateTime       { get; private set; }
        public FailedTransactionItem(string agentID, string userID, TransactionTypes transactionType, string transactionID, string refTransactionID, double betAmount, double winAmount, int gameID, DateTime updateTime)
        {
            this.AgentID            = agentID;
            this.UserID             = userID;
            this.TransactionType    = transactionType;
            this.TransactionID      = transactionID;
            this.RefTransactionID   = refTransactionID;
            this.UpdateTime         = updateTime;
            this.BetAmount          = betAmount;
            this.WinAmount          = winAmount;
            this.GameID             = gameID;
        }
    }
}
