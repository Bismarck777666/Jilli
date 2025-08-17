using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using System.Data.SqlClient;
using System.Data;

namespace UserNode.Database
{
    public class DBGameLogWriteWorker : ReceiveActor
    {
        private string                          _strConnectionString    = "";
        private             ICancelable         _schedulerCancel        = null;
        private readonly    ILoggingAdapter     _logger                 = Logging.GetLogger(Context);

        public DBGameLogWriteWorker(string strConnectionString)
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
            return Akka.Actor.Props.Create(() => new DBGameLogWriteWorker(strConnString));
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
                        await insertApiTransactions(connection);
                        await insertFailedTransactions(connection);
                        await updateDepositTransactions(connection);
                        await insertGameLogs(connection);
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

                do
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(_strConnectionString))
                        {
                            await connection.OpenAsync();

                            int transactionCount     = await insertApiTransactions(connection);
                            int insertedLogCount     = await insertGameLogs(connection);
                            int faildTransationCount = await insertFailedTransactions(connection);
                            int updateTransCount     = await updateDepositTransactions(connection);
                            if (insertedLogCount == 0 && transactionCount == 0 && faildTransationCount == 0 && updateTransCount == 0)
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Exception has been occured in DBProxyWriter {0}", ex.ToString());
                    }
                } while (true);
            }
        }
        private async Task<int> insertApiTransactions(SqlConnection connection)
        {
            List<ApiTransactionItem>  transactionItems = null;
            try
            {
                transactionItems = await Context.Parent.Ask<List<ApiTransactionItem>>("PopApiTransactionItems", TimeSpan.FromSeconds(5));
                if (transactionItems == null || transactionItems.Count == 0)
                    return 0;

                DataTable insertDataTable = new DataTable();
                insertDataTable.Columns.Add("agentid",                  typeof(string));
                insertDataTable.Columns.Add("userid",                   typeof(string));
                insertDataTable.Columns.Add("transactionid",            typeof(string));
                insertDataTable.Columns.Add("reltransactionid",         typeof(string));
                insertDataTable.Columns.Add("platformtransactionid",    typeof(string));
                insertDataTable.Columns.Add("transactiontype",          typeof(int));
                insertDataTable.Columns.Add("endtransaction",           typeof(bool));
                insertDataTable.Columns.Add("gameid",                   typeof(int));
                insertDataTable.Columns.Add("betamount",                typeof(decimal));
                insertDataTable.Columns.Add("winamount",                typeof(decimal));
                insertDataTable.Columns.Add("timestamp",                typeof(DateTime));


                foreach (ApiTransactionItem item in transactionItems)
                {
                    insertDataTable.Rows.Add(item.AgentID, item.UserID, item.TransactionID, item.RelTransactionID, item.PlatformTransID,
                        (int)item.TransactionType,item.EndTransaction, item.GameID, (decimal)item.BetAmount, (decimal)item.WinAmount, item.Timestamp);
                }

                if (insertDataTable.Rows.Count > 0)
                {
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection)) 
                    {
                        bulkCopy.DestinationTableName = "transactions";
                        for (int i = 0; i < insertDataTable.Columns.Count; i++)
                            bulkCopy.ColumnMappings.Add(i, i + 1);

                        await bulkCopy.WriteToServerAsync(insertDataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBWriteWorker::insertApiTransactions {0}", ex.ToString());
                if(transactionItems != null)
                    Context.Parent.Tell(transactionItems);
            }
            return transactionItems.Count;
        }        
        private async Task<int> updateDepositTransactions(SqlConnection connection)
        {
            List<DepositTransactionUpdateItem> updateItems = await Context.Parent.Ask<List<DepositTransactionUpdateItem>>("PopUpdatedDepositTransactions", TimeSpan.FromSeconds(5));
            if (updateItems == null)
                return 0;

            SqlTransaction transaction = null;
            try
            {
                string strQuery = "UPDATE transactions SET platformtransactionid=@platformtransactionid WHERE transactionid=@transactionid";

                transaction = connection.BeginTransaction();

                foreach(var item in updateItems)
                {
                    SqlCommand command = new SqlCommand(strQuery, connection, transaction);
                    command.Parameters.AddWithValue("@transactionid", item.TransactionID);
                    if (item.PlatformTransID == null)
                        command.Parameters.AddWithValue("@platformtransactionid", "");
                    else
                        command.Parameters.AddWithValue("@platformtransactionid", item.PlatformTransID);

                    await command.ExecuteNonQueryAsync();
                }
                transaction.Commit();
                return updateItems.Count;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBGameLogWriteWorker::updateDepositTransactions {0}", ex.ToString());
                try
                {
                    transaction.Rollback();
                }
                catch
                { 
                }
                Context.Parent.Tell(updateItems);
            }
            return updateItems.Count;
        }
        private async Task<int> insertFailedTransactions(SqlConnection connection)
        {
            List<FailedTransactionItem> insertItems = await Context.Parent.Ask<List<FailedTransactionItem>>("PopFailedTransactionItems", TimeSpan.FromSeconds(5));
            if (insertItems == null)
                return 0;

            try
            {

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("agentid",            typeof(string));
                dataTable.Columns.Add("userid",             typeof(string));
                dataTable.Columns.Add("transactiontype",    typeof(int));
                dataTable.Columns.Add("transactionid",      typeof(string));
                dataTable.Columns.Add("reftransactionid",   typeof(string));
                dataTable.Columns.Add("betamount",          typeof(decimal));
                dataTable.Columns.Add("winamount",          typeof(decimal));
                dataTable.Columns.Add("gameid",             typeof(int));
                dataTable.Columns.Add("timestamp",          typeof(DateTime));
                foreach (var item in insertItems)
                    dataTable.Rows.Add(item.AgentID, item.UserID, (int) item.TransactionType, item.TransactionID, item.RefTransactionID, (decimal) Math.Round(item.BetAmount, 2), (decimal)Math.Round(item.WinAmount, 2), item.GameID, item.UpdateTime);

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "faildtransactions";
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                        bulkCopy.ColumnMappings.Add(i, i + 1);

                    await bulkCopy.WriteToServerAsync(dataTable);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBGameLogWriteWorker::insertFailedTransactions {0}", ex.ToString());
                Context.Parent.Tell(insertItems);
            }
            return insertItems.Count;
        }

        private async Task<int> insertGameLogs(SqlConnection connection)
        {
            List<GameLogItem> insertItems = await Context.Parent.Ask<List<GameLogItem>>("PopGameLogItems", TimeSpan.FromSeconds(5));
            if (insertItems == null)
                return 0;

            Dictionary<int,      List<GameLogItem>> dicApiGameLogItems = new Dictionary<int,      List<GameLogItem>>();

            foreach (GameLogItem item in insertItems)
            {
                DateTime dateTime = item.LogTime;
                dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);

                if(!dicApiGameLogItems.ContainsKey(item.AgentID))
                    dicApiGameLogItems.Add(item.AgentID, new List<GameLogItem>());

                dicApiGameLogItems[item.AgentID].Add(item);
            }

            int totalCount = 0;

            foreach (KeyValuePair<int, List<GameLogItem>> pair in dicApiGameLogItems)
            {
                //먼저 해당날자의 표가 이미 창조된것인지를 검사한다.
                if (!WriterSnapshot.Instance.IsAgentGameLogTableCreated(pair.Key))
                {
                    try
                    {
                        SqlCommand command = new SqlCommand("usp_CreateAgentLogTables", connection);
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@agentid", pair.Key));
                        await command.ExecuteNonQueryAsync();

                        WriterSnapshot.Instance.HasCreatedAgentGameLogTable(pair.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Create GameLog Table {0} failed : {1}", pair.Key, ex.ToString());
                    }
                }

                //게임로그표에 게임로그들을 삽입한다.
                try
                {

                    DataTable dataTable = new DataTable();
                    dataTable.Columns.Add("username", typeof(string));
                    dataTable.Columns.Add("gameid", typeof(int));
                    dataTable.Columns.Add("gamename", typeof(string));
                    dataTable.Columns.Add("bettype", typeof(int));
                    dataTable.Columns.Add("bet", typeof(decimal));
                    dataTable.Columns.Add("win", typeof(decimal));
                    dataTable.Columns.Add("beginmoney", typeof(decimal));
                    dataTable.Columns.Add("endmoney", typeof(decimal));
                    dataTable.Columns.Add("gamelog", typeof(string));
                    dataTable.Columns.Add("logtime", typeof(DateTime));

                    foreach (GameLogItem item in pair.Value)
                        dataTable.Rows.Add(item.UserID, item.GameID, item.GameName, item.BetType, (decimal)Math.Round(item.Bet, 2), (decimal)Math.Round(item.Win, 2), (decimal)Math.Round(item.BeginMoney, 2), (decimal)Math.Round(item.EndMoney, 2), item.GameLog, item.LogTime);


                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                    {
                        bulkCopy.DestinationTableName = string.Format("gamelog_{0}", pair.Key);
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                            bulkCopy.ColumnMappings.Add(i, i + 1);

                        await bulkCopy.WriteToServerAsync(dataTable);
                    }

                    totalCount += dataTable.Rows.Count;
                }
                catch (Exception ex)
                {
                    _logger.Error("Exception has been occured in DBWriteWorker while inserting game logs : {0}", ex.ToString());

                    //기록에 실패한 항목들을 다시 넣는다.
                    Context.Parent.Tell(pair.Value);
                    break;
                }
            }

            return totalCount;
        }        
    }
}
