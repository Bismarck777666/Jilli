using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace UserNode.Database
{
    public class DBReportWriteWorker : ReceiveActor
    {
        private string                      _strConnectionString    = "";
        private ICancelable                 _schedulerCancel        = null;
        private readonly ILoggingAdapter    _logger                 = Logging.GetLogger(Context);
            
        public DBReportWriteWorker(string strConnectionString)
        {
            _strConnectionString = strConnectionString;
            ReceiveAsync<string>(processCommand);
        }
        
        protected override void PreStart()
        {
            if (_schedulerCancel != null)
                _schedulerCancel.Cancel();

            _schedulerCancel = Context.System.Scheduler.ScheduleTellOnceCancelable(1000, Self, "write", ActorRefs.NoSender);
        }

        public static Props Props(string strConnString)
        {
            return Akka.Actor.Props.Create(() => new DBReportWriteWorker(strConnString));
        }
        private async Task processCommand(string strCommand)
        {
            if (strCommand == "write")
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_strConnectionString))
                    {
                        await connection.OpenAsync();

                        await updateReports(connection);

                        await updateUserPoints(connection);
                        await updateAgentPoints(connection);

                        await updateGameReports(connection);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Exception has been occured in DBReportWriteWorker::processCommand {0}", ex.ToString());
                }
                _schedulerCancel = Context.System.Scheduler.ScheduleTellOnceCancelable(1000, Self, "write", ActorRefs.NoSender);
            }
            else if(strCommand == "flush")
            {
                if (_schedulerCancel != null)
                    _schedulerCancel.Cancel();

                _logger.Info("Flush Started");

                do
                {
                    using (SqlConnection connection = new SqlConnection(_strConnectionString))
                    {
                        await connection.OpenAsync();

                        var stopWatch = new System.Diagnostics.Stopwatch();
                        stopWatch.Start();

                        //플레이어 리포트변경을 디비에 기록한다.
                        int reportCount             = await updateReports(connection);
                        int userPointAddCount       = await updateUserPoints(connection);
                        int agentPointAddCount      = await updateAgentPoints(connection);
                        int gameReportCount         = await updateGameReports(connection);

                        stopWatch.Stop();

                        double elapsed = stopWatch.Elapsed.TotalMilliseconds;

                        _logger.Info("report updated at {0}, report count: {1}, agent point add: {2}, game report: {3}, user point add:{4} elapsed time:{5}", DateTime.Now, reportCount,
                            agentPointAddCount, gameReportCount, userPointAddCount, elapsed);

                        if (reportCount == 0 && agentPointAddCount == 0 && gameReportCount == 0 && userPointAddCount == 0)
                            break;
                    }
                } while (true);
                _logger.Info("Flush Ended");
            }
        }
        private async Task<int> updateGameReports(SqlConnection connection)
        {
            List<GameReportItem> reportUpdateItems = null;
            try
            {
                reportUpdateItems = await Context.Parent.Ask<List<GameReportItem>>("PopGameReportUpdates", TimeSpan.FromSeconds(5));
                if (reportUpdateItems == null)
                    return 0;

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("gameid",         typeof(int));
                dataTable.Columns.Add("agentid",        typeof(int));
                dataTable.Columns.Add("bet",            typeof(decimal));
                dataTable.Columns.Add("win",            typeof(decimal));
                dataTable.Columns.Add("reportdate",     typeof(DateTime));

                foreach (GameReportItem updateItem in reportUpdateItems)
                    dataTable.Rows.Add(updateItem.GameID, updateItem.AgentID, updateItem.BetMoney, updateItem.WinMoney, updateItem.ReportDate);


                SqlCommand command = new SqlCommand("UpdateGameReports", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@tblGameReports", dataTable));
                await command.ExecuteNonQueryAsync();
                return dataTable.Rows.Count;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBReportWriteWorker::updateGameReports while updating game reports : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                if (reportUpdateItems != null && reportUpdateItems.Count > 0)
                    Context.Parent.Tell(reportUpdateItems);

                return -1;
            }
        }
        private async Task<int> updateReports(SqlConnection connection)
        {
            List<ReportUpdateItem> reportUpdateItems = null;
            try
            {
                reportUpdateItems = await Context.Parent.Ask<List<ReportUpdateItem>>("PopReportUpdates", TimeSpan.FromSeconds(5));
                if (reportUpdateItems == null)
                    return 0;

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("username",   typeof(string));
                dataTable.Columns.Add("bet",        typeof(decimal));
                dataTable.Columns.Add("win",        typeof(decimal));
                dataTable.Columns.Add("reporttime", typeof(DateTime));
                dataTable.Columns.Add("agentid",    typeof(int));

                foreach (ReportUpdateItem updateItem in reportUpdateItems)
                    dataTable.Rows.Add(updateItem.UserID, updateItem.Bet, updateItem.Win, updateItem.ReportDateTime, updateItem.AgentID);


                SqlCommand command = new SqlCommand("UpdateReports", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@tblReports", dataTable));
                await command.ExecuteNonQueryAsync();
                return dataTable.Rows.Count;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBReportWriteWorker::updateReports while updating report : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                if (reportUpdateItems != null && reportUpdateItems.Count > 0)
                    Context.Parent.Tell(reportUpdateItems);

                return -1;
            }
        }

        
        private async Task<int> updateAgentPoints(SqlConnection connection)
        {
            List<AgentPointAdded> agentPointAdds = null;
            try
            {
                agentPointAdds = await Context.Parent.Ask<List<AgentPointAdded>>("PopAgentPointAdds", TimeSpan.FromSeconds(5));
                if (agentPointAdds == null)
                    return 0;

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("point",      typeof(decimal));
                dataTable.Columns.Add("id",         typeof(int));

                DataTable dataTable2 = new DataTable();
                dataTable2.Columns.Add("point", typeof(decimal));
                dataTable2.Columns.Add("id",    typeof(int));

                foreach (AgentPointAdded agentPointAdd in agentPointAdds)
                {
                    if(agentPointAdd.PointType == PointTypes.Rolling)
                        dataTable.Rows.Add(agentPointAdd.IncPoint, agentPointAdd.AgentDBID);
                    else
                        dataTable2.Rows.Add(agentPointAdd.IncPoint, agentPointAdd.AgentDBID);
                }

                if(dataTable.Rows.Count > 0 || dataTable2.Rows.Count > 0)
                {
                    SqlCommand command  = new SqlCommand("AddAgentPoint", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@tblRolling", dataTable));
                    command.Parameters.Add(new SqlParameter("@tblLosing",  dataTable2));
                    await command.ExecuteNonQueryAsync();
                }
                return dataTable.Rows.Count + dataTable2.Rows.Count;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBReportWriteWorker::updateAgentRolling while updating agent rolling : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                if (agentPointAdds != null && agentPointAdds.Count > 0)
                    Context.Parent.Tell(agentPointAdds);

                return -1;
            }
        }
        private async Task<int> updateUserPoints(SqlConnection connection)
        {
            List<UserRollPointAdded> userPointAdds = null;
            try
            {
                userPointAdds = await Context.Parent.Ask<List<UserRollPointAdded>>("PopUserPointAdds", TimeSpan.FromSeconds(5));
                if (userPointAdds == null)
                    return 0;

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("point",  typeof(decimal));
                dataTable.Columns.Add("id",     typeof(long));


                foreach (UserRollPointAdded userPoint in userPointAdds)
                    dataTable.Rows.Add(userPoint.IncPoint, userPoint.UserDBID);

                if (dataTable.Rows.Count > 0)
                {
                    SqlCommand command = new SqlCommand("AddUserPoint", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@tblRolling", dataTable));
                    await command.ExecuteNonQueryAsync();
                }
                return dataTable.Rows.Count;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBReportWriteWorker::updateUserPoints while updating agent rolling : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                if (userPointAdds != null && userPointAdds.Count > 0)
                    Context.Parent.Tell(userPointAdds);

                return -1;
            }
        }

    }

    public class AgentUpdateTable
    {
        public double Bet { get; set; }
        public double Win { get; set; }
        public double Turnover { get; set; }

        public AgentUpdateTable(double bet, double win, double turnover)
        {
            this.Bet = bet;
            this.Win = win;
            this.Turnover = turnover;
        }

        public void mergeUpdates(double bet, double win, double turnover)
        {
            this.Bet += bet;
            this.Win += win;
            this.Turnover += turnover;
        }
    }
}
