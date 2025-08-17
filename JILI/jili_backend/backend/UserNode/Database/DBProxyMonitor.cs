using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using System.Data.SqlClient;
using System.Data.Common;
using GITProtocol;
using Akka.Event;
using System.Diagnostics;

namespace UserNode.Database
{
    public class DBProxyMonitor : ReceiveActor
    {
        private string                      _strConnString          = "";
        private ICancelable                 _monitorCancelable      = null;
        private ICancelable                 _monitorScoreCancelable = null;
        private readonly ILoggingAdapter    _logger                 = Logging.GetLogger(Context);
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

            if (_monitorScoreCancelable != null)
                _monitorScoreCancelable.Cancel();

            base.PostStop();
        }
        protected override void PostRestart(Exception reason)
        {
            base.PostRestart(reason);
            Self.Tell("initialize");
        }

        private async Task processCommand(string strCommand)
        {
            if(strCommand == "initialize")
            {
                await initializeMonitor();
                _monitorCancelable      = Context.System.Scheduler.ScheduleTellOnceCancelable(5000, Self, "monitor", ActorRefs.NoSender);
                _monitorScoreCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(1000, Self, "monitorScores", ActorRefs.NoSender);

                Sender.Tell("dbInitialized");
            }
            else if(strCommand == "monitor")
            {
                await monitorTables();
            }
        }
        private async Task initializeMonitor()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();                    
                    if (DBMonitorSnapshot.Instance.LastQuitUserID == -1)
                    {
                        string strQuery = "SELECT TOP 1 id FROM quitusers ORDER BY id DESC";
                        SqlCommand command = new SqlCommand(strQuery, connection);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                DBMonitorSnapshot.Instance.LastQuitUserID = reader.GetInt64(0);
                            }
                        }

                    }
                    if(DBMonitorSnapshot.Instance.LastAgentUpdateTime == new DateTime(1, 1, 1))
                    {
                        DBMonitorSnapshot.Instance.LastAgentUpdateTime = new DateTime(1970, 1, 1);
                        string strQuery = "SELECT TOP 1 updatetime FROM agents ORDER BY updatetime DESC";
                        SqlCommand command = new SqlCommand(strQuery, connection);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if(await reader.ReadAsync())
                            {
                                if (!(reader["updatetime"] is DBNull))
                                    DBMonitorSnapshot.Instance.LastAgentUpdateTime = (DateTime)reader["updatetime"];
                            }
                        }
                    }
                    if (DBMonitorSnapshot.Instance.LastUserUpdateTime == new DateTime(1, 1, 1))
                    {
                        DBMonitorSnapshot.Instance.LastUserUpdateTime = new DateTime(1970, 1, 1);
                        string strQuery = "SELECT TOP 1 updatetime FROM users ORDER BY updatetime DESC";
                        SqlCommand command = new SqlCommand(strQuery, connection);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                if (!(reader["updatetime"] is DBNull))
                                    DBMonitorSnapshot.Instance.LastUserUpdateTime = (DateTime)reader["updatetime"];
                            }
                        }
                    }                                       
                    if (DBMonitorSnapshot.Instance.GameConfigUpdateTime == new DateTime(1, 1, 1))
                    {
                        DBMonitorSnapshot.Instance.GameConfigUpdateTime = new DateTime(1970, 1, 1);
                        string strQuery         = "SELECT gameid, gametype, openclose, gamesymbol, updatetime FROM gameconfigs_copy1";
                        SqlCommand command      = new SqlCommand(strQuery, connection);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int         gameID          = (int)             reader["gameid"];
                                bool        isOpened        = (bool)            reader["openclose"];
                                DateTime    updateTime      = (DateTime)        reader["updatetime"];
                                GameProviders    gameType   = (GameProviders)(int)   reader["gametype"];
                                string      gameSymbol      = (string)          reader["gamesymbol"];

                                if(!isOpened)
                                    continue;

                                DBMonitorSnapshot.Instance.setGameType(gameID, gameType);
                                DBMonitorSnapshot.Instance.setGameSymbol(gameSymbol, gameType, gameID);
                                if (DBMonitorSnapshot.Instance.GameConfigUpdateTime < updateTime)
                                    DBMonitorSnapshot.Instance.GameConfigUpdateTime = updateTime;
                            }
                        }
                    }
                    if (DBMonitorSnapshot.Instance.LastRangeEventPlayerID == -1)
                    {
                        string strQuery = "SELECT TOP 1 id FROM userrangeevents ORDER BY id DESC";
                        SqlCommand command = new SqlCommand(strQuery, connection);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                DBMonitorSnapshot.Instance.LastRangeEventPlayerID = reader.GetInt64(0);
                            }
                        }
                    }
                    if (DBMonitorSnapshot.Instance.LastRangeEventUpdateTime == new DateTime(1, 1, 1))
                    {
                        DBMonitorSnapshot.Instance.LastRangeEventUpdateTime = new DateTime(1970, 1, 1);
                        string strQuery = "SELECT TOP 1 updatetime FROM userrangeevents WHERE updatetime is not null ORDER BY updatetime DESC";
                        SqlCommand command = new SqlCommand(strQuery, connection);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                DBMonitorSnapshot.Instance.LastRangeEventUpdateTime = reader.GetDateTime(0);
                            }
                        }
                    }
                    if (AgentSnapshot.Instance.LastUpdateTime == new DateTime(1, 1, 1))
                    {
                        AgentSnapshot.Instance.LastUpdateTime = new DateTime(1970, 1, 1);
                        string      strQuery = "SELECT * FROM agents ORDER BY updatetime";
                        SqlCommand  command  = new SqlCommand(strQuery, connection);
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int     agentID     = (int)reader["id"];
                                string  secretKey   = (string)reader["secretkey"];
                                string  callbackURL = (string)reader["callbackurl"];
                                string  apiToken    = (string)reader["apitoken"];
                                DateTime updateTime = (DateTime)reader["updatetime"];

                                AgentSnapshot.Instance.updateAgentConfig(agentID, new AgentConfig(apiToken, secretKey, callbackURL));
                                AgentSnapshot.Instance.LastUpdateTime = updateTime;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
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

                    //싱글게임설정을 감시한다.
                    string strQuery = "SELECT gameid, gametype, openclose, gamesymbol, updatetime FROM gameconfigs_copy1 WHERE updatetime > @updatetime ORDER BY updatetime";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@updatetime", DBMonitorSnapshot.Instance.GameConfigUpdateTime);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int             gameID              = (int)             reader["gameid"];
                            GameProviders   gameType            = (GameProviders) (int)  reader["gametype"];
                            string          gameSymbol          = (string)          reader["gamesymbol"];
                            bool            isOpened            = (bool)reader["openclose"];

                            if (!isOpened)
                                continue;

                            DBMonitorSnapshot.Instance.setGameType  (gameID, gameType);
                            DBMonitorSnapshot.Instance.setGameSymbol(gameSymbol, gameType, gameID);
                            DBMonitorSnapshot.Instance.GameConfigUpdateTime = (DateTime)reader["updatetime"];
                        }
                    }

                    strQuery = "SELECT * FROM agents WHERE updatetime > @updatetime ORDER BY updatetime";
                    command  = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@updatetime", AgentSnapshot.Instance.LastUpdateTime);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int         agentID     = (int)reader["id"];
                            string      secretKey   = (string)reader["secretkey"];
                            string      callbackURL = (string)reader["callbackurl"];
                            string      apiToken    = (string)reader["apitoken"];
                            DateTime    updateTime  = (DateTime)reader["updatetime"];
                            AgentSnapshot.Instance.updateAgentConfig(agentID, new AgentConfig(apiToken, secretKey, callbackURL));
                            AgentSnapshot.Instance.LastUpdateTime = updateTime;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Exception has been occurred in DBProxyMonitor::monitorTables {0}", ex);
            }
            _monitorCancelable = Context.System.Scheduler.ScheduleTellOnceCancelable(5000, Self, "monitor", ActorRefs.NoSender);
        }
    }
}
