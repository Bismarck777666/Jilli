using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using System.Data.SqlClient;
using System.Data;

namespace ApiIntegration.Database
{
    public class DBWriteWorker : ReceiveActor
    {
        private string                      _strConnectionString    = "";
        private ICancelable                 _schedulerCancel        = null;
        private readonly ILoggingAdapter    _logger                 = Logging.GetLogger(Context);

        public DBWriteWorker(string strConnectionString)
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
            return Akka.Actor.Props.Create(() => new DBWriteWorker(strConnString));
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

                        //플레이어잔고 변경을 디비에 기록한다.
                        await updateBalances(connection);

                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Exception has been occured in DBProxyWriter {0}", ex.ToString());
                }
                _schedulerCancel = Context.System.Scheduler.ScheduleTellOnceCancelable(1000, Self, "write", ActorRefs.NoSender);
            }
            else if(strCommand == "flush")
            {
                if (_schedulerCancel != null)
                    _schedulerCancel.Cancel();

                _logger.Info("Flush");

                do
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(_strConnectionString))
                        {
                            await connection.OpenAsync();
                            int balanceCount = await updateBalances(connection);
                            if (balanceCount == 0)
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Exception has been occured in DBProxyWriter {0}", ex.ToString());
                    }
                } while (true);

                _logger.Info("Flush Ended");
            }
        }
        private async Task<int> updateBalances(SqlConnection connection)
        {
            List<PlayerBalanceUpdateItem> balanceUpdates = null;
            try
            {
                balanceUpdates = await Context.Parent.Ask<List<PlayerBalanceUpdateItem>>("PopBalanceUpdates", TimeSpan.FromSeconds(5));
                if (balanceUpdates == null)
                    return 0;

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("balance",    typeof(decimal));
                dataTable.Columns.Add("id",         typeof(long));

                foreach (PlayerBalanceUpdateItem updateItem in balanceUpdates)
                {
                    if(updateItem.PlayerID <= 0)
                        _logger.Error("DBWriteWorker::updateBalances playerID <= 0 {0} {1}", updateItem.PlayerID, updateItem.BalanceIncrement);

                    dataTable.Rows.Add((decimal)updateItem.BalanceIncrement, updateItem.PlayerID);
                }

                SqlCommand command = new SqlCommand("UpdateBalance", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@tblBalances", dataTable));

                await command.ExecuteNonQueryAsync();
                return dataTable.Rows.Count;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBWriteProxy while updating balance : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                Context.Parent.Tell(balanceUpdates);
                return -1;
            }
        }
        protected override void PostStop()
        {
            base.PostStop();
        }
    }
}
