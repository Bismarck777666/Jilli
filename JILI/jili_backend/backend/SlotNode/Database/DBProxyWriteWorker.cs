using Akka.Actor;
using Akka.Event;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.Database
{
    public class DBProxyWriteWorker : ReceiveActor
    {
        private string                      _strConnectionString    = "";
        private ICancelable                 _schedulerCancel        = null;
        private readonly ILoggingAdapter    _logger                 = Logging.GetLogger(Context);

        public DBProxyWriteWorker(string strConnectionString)
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
            return Akka.Actor.Props.Create(() => new DBProxyWriteWorker(strConnString));
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

                        await updatePayoutPoolStatus(connection);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Exception has been occured in DBProxyWriteWorker::processCommand {0}", ex.ToString());
                }
                _schedulerCancel = Context.System.Scheduler.ScheduleTellOnceCancelable(1000, Self, "write", ActorRefs.NoSender);
            }
            else if (strCommand == "flush")
            {
                if (_schedulerCancel != null)
                    _schedulerCancel.Cancel();
            }
        }       
        
        private async Task updatePayoutPoolStatus(SqlConnection connection)
        {
            UpdatePayoutPoolStatus updateItem = null;
            try
            {
                updateItem = await Context.Parent.Ask<UpdatePayoutPoolStatus>("PopUpdatePayoutPoolStatus", TimeSpan.FromSeconds(5));
                if (updateItem == null)
                    return;

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("agentid",    typeof(int));
                dataTable.Columns.Add("bet",        typeof(decimal));
                dataTable.Columns.Add("win",        typeof(decimal));

                foreach (KeyValuePair<int, double[]> pair in updateItem.PoolStatus)
                    dataTable.Rows.Add(pair.Key, pair.Value[0], pair.Value[1]);

                SqlCommand command = new SqlCommand("UpdatePayoutPool", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@tblReports", dataTable));
                await command.ExecuteNonQueryAsync();
                return;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBProxyWriteWorker::updatePayoutPoolStatus {0}", ex.ToString());
            }
        }
    }
}
