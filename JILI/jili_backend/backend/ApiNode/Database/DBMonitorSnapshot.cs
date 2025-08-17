using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GITProtocol;

namespace QueenApiNode.Database
{
    class DBMonitorSnapshot
    {
        private static DBMonitorSnapshot _sInstance = new DBMonitorSnapshot();

        public static DBMonitorSnapshot Instance    => _sInstance;

        protected ConcurrentDictionary<int, GameInfo>   _gameInfos      = new ConcurrentDictionary<int, GameInfo>();
        protected List<GameInfo>                        _sortedGameList = new List<GameInfo>();

        public Dictionary<string, string>       AgentHashKeys           { get; set; }
        public DateTime                         LastAgentUpdateTime     { get; set; }
        public DateTime                         GameConfigUpdateTime    { get; set; }

        public DBMonitorSnapshot()
        {
            this.LastAgentUpdateTime  = new DateTime(1970, 1, 1);
            this.AgentHashKeys        = new Dictionary<string, string>();
            this.GameConfigUpdateTime = new DateTime(1, 1, 1);
        }

        public void setGameInfo(int gameID, int gameType, string symbol, string name, DateTime releaseDate, int code)
        {
            GameInfo gameInfo = new GameInfo(gameID, gameType, name, symbol, releaseDate, code);
            if (!_gameInfos.ContainsKey(gameID))
            {
                _sortedGameList.Add(gameInfo);
                _gameInfos[gameID] = gameInfo;
            }
            else
            {
                _gameInfos[gameID].updateInfo(gameInfo);
            }
        }
        public void sortGameInfos()
        {
            for (int i = 0; i < _sortedGameList.Count - 1; i++)
            {
                for(int j = i + 1; j < _sortedGameList.Count; j++)
                {
                    if (_sortedGameList[i].ReleaseDate > _sortedGameList[j].ReleaseDate)
                    {
                        GameInfo gameInfo  = _sortedGameList[i];
                        _sortedGameList[i] = _sortedGameList[j];
                        _sortedGameList[j] = gameInfo;
                    }
                }
            }
        }
        public GameInfo getGameInfo(int gameID)
        {
            GameInfo gameInfo = null;
            if (_gameInfos.TryGetValue(gameID, out gameInfo))
                return gameInfo;
            else
                return null;
        }
        public List<GameInfo> getAllGameInfo()
        {
            List<GameInfo> gameInfos = new List<GameInfo>(_sortedGameList);
            return gameInfos;
        }
    }
    public class GameInfo
    {
        public int      GameProvider    { get; set; }
        public string   Symbol          { get; set; }
        public int      Code            { get; set; }
        public string   Name            { get; set; }
        public int      GameID          { get; set; }
        public DateTime ReleaseDate     { get; set; }
        public GameInfo(int gameID, int gameType, string name, string symbol, DateTime releaseDate, int gameCode)
        {
            this.GameID         = gameID;
            this.Code           = gameCode;
            this.GameProvider   = gameType;
            this.Name           = name;
            this.Symbol         = symbol;
            this.ReleaseDate    = releaseDate;
        }
        public void updateInfo(GameInfo updatedInfo)
        {
            this.Symbol         = updatedInfo.Symbol;
            this.GameProvider   = updatedInfo.GameProvider;
            this.Name           = updatedInfo.Name;
            this.ReleaseDate    = updatedInfo.ReleaseDate;
            this.Code           = updatedInfo.Code;
        }
    }
}
