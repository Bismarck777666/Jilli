using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GITProtocol;
using System.Collections.Concurrent;

namespace FrontNode.Database
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
     
        protected ConcurrentDictionary<int, GameConfigItem>  _gameConfigs            = new ConcurrentDictionary<int, GameConfigItem>();
        protected ConcurrentDictionary<string, int>[]        _gameSymbolToIDMap      = new ConcurrentDictionary<string, int>[(int)GameProviders.COUNT];
        public DBMonitorSnapshot()
        {
            this.GameConfigUpdateTime       = new DateTime(1, 1, 1);

        }

        public void setGameConfig(int gameID, GameProviders provider, string strGameName)
        {
            GameConfigItem configItem   = new GameConfigItem();
            configItem.Provider         = provider;
            configItem.Name             = strGameName;
            _gameConfigs.AddOrUpdate(gameID, configItem, (key, oldValue) => { return configItem; });
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
        public GameConfigItem getGameConfigFromSymbol(GameProviders gameType, string strSymbol)
        {
            int gameTypeID = (int)gameType - 1;
            if (gameTypeID < 0 || gameTypeID >= (int)GameProviders.COUNT)
                return null;

            int gameID = 0;
            if (!_gameSymbolToIDMap[gameTypeID].TryGetValue(strSymbol, out gameID))
                return null;

            GameConfigItem configItem = null;
            if (!_gameConfigs.TryGetValue(gameID, out configItem))
                return null;

            return configItem;
        }
    }
    public class GameConfigItem
    {
        public GameProviders    Provider    { get; set; }
        public string           Name        { get; set; }
    }
}
