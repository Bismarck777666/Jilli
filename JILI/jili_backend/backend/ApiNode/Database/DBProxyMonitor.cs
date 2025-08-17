using Akka.Actor;
using Akka.Event;
using GITProtocol;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace QueenApiNode.Database
{
    public class DBProxyMonitor : ReceiveActor
    {
        private string          _strConnString      = "";
        private ICancelable     _monitorCancelable  = null;
        private readonly ILoggingAdapter _logger    = Logging.GetLogger(Context);

        public DBProxyMonitor(string strConnString)
        {
            _strConnString = strConnString;
            ReceiveAsync<string>(processCommand);
        }
        public static Props Props(string strConnString)
        {
            return Akka.Actor.Props.Create(() => new DBProxyMonitor(strConnString));
        }
        protected override void PreStart()
        {
            base.PreStart();
        }
        protected override void PostStop()
        {
            if (_monitorCancelable != null)
                _monitorCancelable.Cancel();

            base.PostStop();
        }
        protected override void PostRestart(Exception reason)
        {
            base.PostRestart(reason);
            Self.Tell("initialize");
        }

        private async Task processCommand(string strCommand)
        {
            if (strCommand == "initialize")
            {
                await initializeMonitor();
                _monitorCancelable = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(5000, 5000, Self, "monitor", ActorRefs.NoSender);
                Sender.Tell("dbInitialized");
            }
            else if (strCommand == "monitor")
            {
                await monitorTables();
            }
        }
        private async Task initializeMonitor()
        {
            try
            {
                DBMonitorSnapshot.Instance.AgentHashKeys = new Dictionary<string, string>();
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();

                    string strQuery = "SELECT username, apitoken, updatetime FROM agents WHERE apitoken IS NOT NULL ORDER BY updatetime";
                    var command = new SqlCommand(strQuery, connection);
                    using(var reader = await command.ExecuteReaderAsync())
                    {
                        while(await reader.ReadAsync())
                        {
                            DBMonitorSnapshot.Instance.AgentHashKeys.Add((string)reader["username"], (string)reader["apitoken"]);
                            DBMonitorSnapshot.Instance.LastAgentUpdateTime = (DateTime)reader["updatetime"]; 
                        }
                    }

                    strQuery = "SELECT gameid, gamename, gametype, gamecode, openclose, gamesymbol, updatetime, releasedate FROM gameconfigs_copy1";
                    command = new SqlCommand(strQuery, connection);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        DBMonitorSnapshot.Instance.GameConfigUpdateTime = new DateTime(1970, 1, 1);
                        while (await reader.ReadAsync())
                        {
                            int             gameID      = (int)         reader["gameid"];
                            DateTime        updateTime  = (DateTime)    reader["updatetime"];
                            int             gameType    = (int)         reader["gametype"];
                            string          gameSymbol  = (string)      reader["gamesymbol"];
                            string          gameName    = (string)      reader["gamename"];
                            int             gameCode = (int)      reader["gamecode"];
                            DateTime        releaseDate = (DateTime)    reader["releasedate"];
                            bool            isOpened    = (bool)        reader["openclose"];

                            if(!isOpened)
                                continue;

                            DBMonitorSnapshot.Instance.setGameInfo(gameID, gameType, gameSymbol, gameName, releaseDate, gameCode);
                            if (DBMonitorSnapshot.Instance.GameConfigUpdateTime < updateTime)
                                DBMonitorSnapshot.Instance.GameConfigUpdateTime = updateTime;
                        }
                    }
                    DBMonitorSnapshot.Instance.sortGameInfos();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in DBProxyMonitor::initializeMonitor {0}", ex);
            }
        }
        private async Task monitorTables()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();

                    string  strQuery = "SELECT username, apitoken, state, updatetime FROM agents WHERE (apitoken is not null) and (updatetime > @updatetime) ORDER BY updatetime";
                    var     command  = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@updatetime", DBMonitorSnapshot.Instance.LastAgentUpdateTime);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string strAgentID   = (string)reader["username"];
                            string strAuthToken = (string)reader["apitoken"];
                            int state           = (int)reader["state"];

                            Context.System.ActorSelection("/user/agentManager").Tell(new AgentUpdated(strAgentID, strAuthToken, state));
                            DBMonitorSnapshot.Instance.LastAgentUpdateTime = (DateTime)reader["updatetime"];
                        }
                    }

                    strQuery = "SELECT gameid, gamename, gametype, openclose, gamesymbol, gamecode, updatetime, releasedate FROM gameconfigs_copy1 WHERE updatetime > @updatetime ORDER BY updatetime";
                    command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@updatetime", DBMonitorSnapshot.Instance.GameConfigUpdateTime);
                    bool isGameConfigUpdated = false;
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int             gameID      = (int)     reader["gameid"];
                            int             gameType    = (int)     reader["gametype"];
                            int             gameCode    = (int)     reader["gamecode"];
                            string          gameSymbol  = (string)  reader["gamesymbol"];
                            string          gameName    = (string)  reader["gamename"];
                            DateTime        releaseDate = (DateTime)reader["releasedate"];
                            bool            isOpened    = (bool)        reader["openclose"];

                            if (!isOpened)
                                continue;

                            DBMonitorSnapshot.Instance.setGameInfo(gameID, gameType, gameSymbol, gameName, releaseDate, gameCode);
                            DBMonitorSnapshot.Instance.GameConfigUpdateTime = (DateTime)reader["updatetime"];
                            isGameConfigUpdated = true;
                        }
                    }
                    if (isGameConfigUpdated)
                        DBMonitorSnapshot.Instance.sortGameInfos();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in DBProxyMonitor::monitorTables {0}", ex);
            }

        }
    }
    public class AgentUpdated
    {
        public string   AgentID     { get; set; }
        public string   AuthToken   { get; set; }
        public int      State       { get; set; }
        
        public AgentUpdated(string agentID, string authToken, int state)
        {
            AgentID     = agentID;
            AuthToken   = authToken;
            State       = state;
        }
    }
}
