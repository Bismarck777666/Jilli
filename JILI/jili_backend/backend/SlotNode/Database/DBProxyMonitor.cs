using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using System.Data.SqlClient;
using System.Data.Common;
using GITProtocol;
using SlotGamesNode.GameLogics;
using Akka.Routing;
using Akka.Event;

namespace SlotGamesNode.Database
{
    public class DBProxyMonitor : ReceiveActor
    {
        private string                      _strConnString          = "";
        private ICancelable                 _monitorCancelable      = null;
        private readonly ILoggingAdapter    _logger                 = Logging.GetLogger(Context);

        public DBProxyMonitor(string strConnString)
        {
            _strConnString = strConnString;

            ReceiveAsync<string>(processCommand);
            Receive<PoisonPill>(message =>
            {

            });
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
            if(strCommand == "initialize")
            {
                await initializeMonitor();
                _monitorCancelable = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(5000, 5000, Self, "monitor", ActorRefs.NoSender);
                Sender.Tell("dbInitialized");
            }
            else if(strCommand == "monitor")
            {
                await monitorTables();
            }
        }
        
        private async Task initializeMonitor()
        {
            using (SqlConnection connection = new SqlConnection(_strConnString))
            {
                await connection.OpenAsync();
                             
                if(DBMonitorSnapshot.Instance.GameConfigUpdateTime == new DateTime(1970, 1, 1))
                {
                    PayoutConfigSnapshot.Instance.PayoutConfigs.Clear();

                    string strQuery         = "SELECT gameid, gametype,openclose, gamesymbol,payoutrate, updatetime FROM gameconfigs_copy1 ORDER BY updatetime";
                    SqlCommand command      = new SqlCommand(strQuery, connection);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            GAMEID  gameID           = (GAMEID)(int)reader["gameid"];
                            bool    isOpened         = (bool)       reader["openclose"];
                            double  payoutrate       = (double) (decimal) reader["payoutrate"];
                            DateTime updateTime      = (DateTime)reader["updatetime"];
                            GameProviders gameType        = (GameProviders)(int)reader["gametype"];
                            string gameSymbol        = (string)reader["gamesymbol"];

                            if (!isOpened)
                                continue;

                            PayoutConfigSnapshot.Instance.PayoutConfigs[gameID] = new GameConfig(payoutrate);

                            DBMonitorSnapshot.Instance.setGameType((int)gameID, gameType);
                            DBMonitorSnapshot.Instance.setGameSymbol(gameSymbol, gameType, (int)gameID);

                            if (DBMonitorSnapshot.Instance.GameConfigUpdateTime < updateTime)
                                DBMonitorSnapshot.Instance.GameConfigUpdateTime = updateTime;
                        }
                    }
                }

                if (DBMonitorSnapshot.Instance.PayoutPoolConfigUpdateTime == new DateTime(1970, 1, 1))
                {
                    PayoutPoolConfig.Instance.WebsitePayoutRedundency.Clear();

                    string strQuery = "SELECT * FROM payoutpoolconfig ORDER BY updatetime";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int agentID             = (int)reader["agentid"];
                            double poolRedundency   = (double)(decimal)reader["poolredundency"];
                            DateTime updateTime     = (DateTime)reader["updatetime"];

                            PayoutPoolConfig.Instance.WebsitePayoutRedundency[agentID] = poolRedundency;
                            if (DBMonitorSnapshot.Instance.PayoutPoolConfigUpdateTime < updateTime)
                                DBMonitorSnapshot.Instance.PayoutPoolConfigUpdateTime = updateTime;
                        }
                    }
                }

                if (DBMonitorSnapshot.Instance.AgentGameConfigUpdateTime == new DateTime(1970, 1, 1))
                {
                    PayoutConfigSnapshot.Instance.WebsitePayoutConfigs.Clear();
                    string strQuery = "SELECT gameid, agentid, payoutrate, updatetime FROM agentgameconfigs ORDER BY updatetime";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            GAMEID gameID       = (GAMEID)(int)reader["gameid"];
                            int websiteID       = (int)reader["agentid"];
                            double payoutRate   = (double)(decimal)reader["payoutrate"];
                            DateTime updateTime = (DateTime)reader["updatetime"];

                            if (!PayoutConfigSnapshot.Instance.WebsitePayoutConfigs.ContainsKey(gameID))
                                PayoutConfigSnapshot.Instance.WebsitePayoutConfigs.Add(gameID, new Dictionary<int, double>());

                            PayoutConfigSnapshot.Instance.WebsitePayoutConfigs[gameID][websiteID] = payoutRate;

                            if (DBMonitorSnapshot.Instance.AgentGameConfigUpdateTime < updateTime)
                                DBMonitorSnapshot.Instance.AgentGameConfigUpdateTime = updateTime;
                        }
                    }

                    foreach (GAMEID gameID in PayoutConfigSnapshot.Instance.PayoutConfigs.Keys)
                    {
                        if (!PayoutConfigSnapshot.Instance.WebsitePayoutConfigs.ContainsKey(gameID))
                            PayoutConfigSnapshot.Instance.WebsitePayoutConfigs.Add(gameID, new Dictionary<int, double>());
                    }
                }

                if (DBMonitorSnapshot.Instance.PayoutPoolResetLastID == 0)
                {
                    string strQuery = "SELECT TOP 1 id FROM payoutpoolresets ORDER BY id DESC";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            DBMonitorSnapshot.Instance.PayoutPoolResetLastID = (int)reader["id"];
                    }
                }
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
                    string strQuery = "SELECT gameid, gametype, gamesymbol, payoutrate, updatetime FROM gameconfigs_copy1 WHERE updatetime > @updatetime ORDER BY updatetime";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@updatetime", DBMonitorSnapshot.Instance.GameConfigUpdateTime);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            GAMEID          gameID           = (GAMEID) (int)     reader["gameid"];
                            double          payoutrate       = (double) (decimal) reader["payoutrate"];
                            DateTime        updateTime       = (DateTime)         reader["updatetime"];
                            GameProviders   gameType         = (GameProviders)(int)reader["gametype"];
                            string          gameSymbol       = (string)reader["gamesymbol"];


                            DBMonitorSnapshot.Instance.GameConfigUpdateTime = updateTime;
                            DBMonitorSnapshot.Instance.setGameType((int)gameID, gameType);
                            DBMonitorSnapshot.Instance.setGameSymbol(gameSymbol, gameType, (int)gameID);

                            if (!PayoutConfigSnapshot.Instance.PayoutConfigs.ContainsKey(gameID))
                                continue;

                            GameConfig payoutConfig     = PayoutConfigSnapshot.Instance.PayoutConfigs[gameID];
                            payoutConfig.PayoutRate     = payoutrate;

                            Context.System.ActorSelection("/user/gameManager").Tell(new Broadcast(new PayoutConfigUpdated(gameID)));
                        }
                    }

                    strQuery = "SELECT * FROM payoutpoolconfig WHERE updatetime > @updatetime ORDER BY updatetime";
                    command  = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@updatetime", DBMonitorSnapshot.Instance.PayoutPoolConfigUpdateTime);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int         agentID         = (int)reader["agentid"];
                            double      poolRedundency  = (double)(decimal)reader["poolredundency"];
                            DateTime    updateTime      = (DateTime)reader["updatetime"];
                            bool        isOpened        = (bool)            reader["openclose"];

                            if (!isOpened)
                                continue;

                            PayoutPoolConfig.Instance.WebsitePayoutRedundency[agentID] = poolRedundency;
                            if (DBMonitorSnapshot.Instance.PayoutPoolConfigUpdateTime < updateTime)
                                DBMonitorSnapshot.Instance.PayoutPoolConfigUpdateTime = updateTime;
                            PayoutPoolConfig.Instance.PoolActor.Tell(new WebsiteRedundencyUpdated(agentID, poolRedundency));
                        }
                    }

                    //웹사이트별게임설정을 감시한다.
                    strQuery = "SELECT gameid, agentid, payoutrate, updatetime FROM agentgameconfigs WHERE updatetime > @updatetime ORDER BY updatetime";
                    command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@updatetime", DBMonitorSnapshot.Instance.AgentGameConfigUpdateTime);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            GAMEID  gameID      = (GAMEID)(int)reader["gameid"];
                            int     websiteID   = (int) reader["agentid"];
                            double  payoutRate  = (double)(decimal)reader["payoutrate"];
                            DateTime updateTime = (DateTime)reader["updatetime"];

                            DBMonitorSnapshot.Instance.AgentGameConfigUpdateTime = updateTime;
                            if (!PayoutConfigSnapshot.Instance.WebsitePayoutConfigs.ContainsKey(gameID) || !PayoutConfigSnapshot.Instance.PayoutConfigs.ContainsKey(gameID))
                                continue;

                            double oldPayoutRate = PayoutConfigSnapshot.Instance.PayoutConfigs[gameID].PayoutRate;
                            if (PayoutConfigSnapshot.Instance.WebsitePayoutConfigs[gameID].ContainsKey(websiteID))
                                oldPayoutRate = PayoutConfigSnapshot.Instance.WebsitePayoutConfigs[gameID][websiteID];

                            PayoutConfigSnapshot.Instance.WebsitePayoutConfigs[gameID][websiteID] = payoutRate;
                        }
                    }
                    
                    strQuery = "SELECT id, agentid FROM payoutpoolresets WHERE id > @id ORDER BY id";
                    command  = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@id", DBMonitorSnapshot.Instance.PayoutPoolResetLastID);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int     id          = (int)reader["id"];
                            int     websiteID   = (int)reader["agentid"];
                            if (DBMonitorSnapshot.Instance.PayoutPoolResetLastID < id)
                                DBMonitorSnapshot.Instance.PayoutPoolResetLastID = id;
                            PayoutPoolConfig.Instance.PoolActor.Tell(new ResetWebsitePayoutPoolRequest(websiteID));
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Exception has been occurred in DBProxyMonitor::monitorTables {0}", ex);
            }
        }

    }
}
