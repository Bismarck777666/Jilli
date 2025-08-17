using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GITProtocol;
using System.Collections.Concurrent;

namespace UserNode.Database
{
    public class DBMonitorSnapshot
    {
        private static DBMonitorSnapshot _sInstance = new DBMonitorSnapshot();
        public static DBMonitorSnapshot Instance
        {
            get
            {
                return _sInstance;
            }
        }

        public DateTime GameConfigUpdateTime    { get; set; }
        public long     LastQuitUserID          { get; set; }
        public DateTime LastAgentUpdateTime     { get; set; }
        public DateTime LastUserUpdateTime      { get; set; }
        public long     LastRangeEventPlayerID   { get; set; }
        public DateTime LastRangeEventUpdateTime { get; set; }

       

        protected ConcurrentDictionary<int, GameProviders>  _gameTypeMap            = new ConcurrentDictionary<int, GameProviders>();
        protected ConcurrentDictionary<string, int>[]       _gameSymbolToIDMap      = new ConcurrentDictionary<string, int>[(int)GameProviders.COUNT];
        public DBMonitorSnapshot()
        {
            this.GameConfigUpdateTime       = new DateTime(1, 1, 1);
            this.LastAgentUpdateTime        = new DateTime(1, 1, 1);
            this.LastUserUpdateTime         = new DateTime(1, 1, 1);
            this.LastQuitUserID             = -1;
            this.LastRangeEventPlayerID     = -1;
            this.LastRangeEventUpdateTime   = new DateTime(1, 1, 1);

        }

        public void setGameType(int gameID, GameProviders gameType)
        {
            _gameTypeMap.AddOrUpdate(gameID, gameType, (key, oldValue) => { return gameType; });
        }
        public void setGameSymbol(string strGameSymbol, GameProviders gameType, int gameID)
        {
            if (string.IsNullOrEmpty(strGameSymbol))
                return;

            int gameTypeID = (int)gameType - 1;
            if (gameTypeID < 0 || gameTypeID >= (int)GameProviders.COUNT)
                return;

            if (_gameSymbolToIDMap[gameTypeID] == null)
                _gameSymbolToIDMap[gameTypeID] = new ConcurrentDictionary<string, int>();

            string oldSymbol = null;
            foreach (KeyValuePair<string, int> pair in _gameSymbolToIDMap[gameTypeID])
            {
                if (gameID == pair.Value)
                {
                    oldSymbol = pair.Key;
                    break;
                }
            }

            if (oldSymbol != null)
            {
                int oldGameID = 0;
                _gameSymbolToIDMap[gameTypeID].TryRemove(oldSymbol, out oldGameID);
            }
            _gameSymbolToIDMap[gameTypeID].AddOrUpdate(strGameSymbol, gameID, (key, oldValue) => { return gameID; });
        }

        public string getGameSymbolFromID(GameProviders gameType, int gameID)
        {
            int gameTypeID = (int)gameType - 1;
            if (gameTypeID < 0 || gameTypeID >= (int)GameProviders.COUNT)
                return "";

            foreach (KeyValuePair<string, int> pair in _gameSymbolToIDMap[gameTypeID])
            {
                if (pair.Value == gameID)
                    return pair.Key;
            }
            return "";
        }
        public int getGameIDFromString(GameProviders gameType, string strGameID)
        {
            int gameID = 0;
            int gameTypeID = (int)gameType - 1;
            if (gameTypeID < 0 || gameTypeID >= (int)GameProviders.COUNT)
                return 0;

            if (_gameSymbolToIDMap[gameTypeID].TryGetValue(strGameID, out gameID))
                return gameID;

            return 0;
        }
        public GameProviders getGITGameProvider(int gameID)
        {
            GameProviders gameType = GameProviders.NONE;
            if (_gameTypeMap.TryGetValue(gameID, out gameType))
                return gameType;

            return GameProviders.NONE;
        }
    }    
}
