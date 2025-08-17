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

                        //플레이어 상태변경을 디비에 기록한다.
                        await updatePlayerStates(connection);

                        //유저의 베트머니를 업데이트한다.
                        await updateUserBetMoney(connection);
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

                            //플레이어잔고 변경을 디비에 기록한다.
                            int balanceCount = await updateBalances(connection);

                            //플레이어 상태변경을 디비에 기록한다.
                            int stateCount  = await updatePlayerStates(connection);

                            if (balanceCount == 0 && stateCount == 0)
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
        private async Task<int> updatePlayerStates(SqlConnection connection)
        {
            List<UserStateUpdateItem> updatedPlayerStates = null;
            try
            {
                updatedPlayerStates = await Context.Parent.Ask<List<UserStateUpdateItem>>("PopPlayerStates", TimeSpan.FromSeconds(10));
            }
            catch(Exception ex)
            {
                _logger.Error("Exception has been occured in DBWriteWorker while updating player states : {0}", ex.ToString());
            }

            if (updatedPlayerStates == null || updatedPlayerStates.Count == 0)
                return 0;

            List<UserStateUpdateItem> updateLoginItems = new List<UserStateUpdateItem>();
            DataTable updateLoginTable = new DataTable();
            updateLoginTable.Columns.Add("id",          typeof(Int32));

            List<UserStateUpdateItem> updateGameItems = new List<UserStateUpdateItem>();
            DataTable updateGameTable = new DataTable();
            updateGameTable.Columns.Add("id",           typeof(Int32));
            updateGameTable.Columns.Add("isonline",     typeof(Int32));
            updateGameTable.Columns.Add("lastgameid",   typeof(Int32));


            List<UserStateUpdateItem> updateLobbyOffItems = new List<UserStateUpdateItem>();
            DataTable updateLobbyOffTable = new DataTable();
            updateLobbyOffTable.Columns.Add("id", typeof(Int32));
            updateLobbyOffTable.Columns.Add("lastgameid",   typeof(Int32));
            updateLobbyOffTable.Columns.Add("balance",      typeof(decimal));

            try
            {
                foreach (UserStateUpdateItem updateItem in updatedPlayerStates)
                {
                    if (updateItem is UserLoginStateItem)
                    {
                        updateLoginTable.Rows.Add(updateItem.PlayerID);
                        updateLoginItems.Add(updateItem);
                    }
                    else if (updateItem is UserGameStateItem)
                    {
                        updateGameTable.Rows.Add(updateItem.PlayerID, (updateItem as UserGameStateItem).State, (updateItem as UserGameStateItem).GameID);
                        updateGameItems.Add(updateItem);
                    }
                    else
                    {
                        updateLobbyOffTable.Rows.Add(updateItem.PlayerID, (updateItem as UserOfflineStateItem).GameID, (updateItem as UserOfflineStateItem).BalanceIncrement);
                        updateLobbyOffItems.Add(updateItem);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Exception has been occured in DBWriteWorker while updating player states : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                if (updatedPlayerStates != null)
                    Context.Parent.Tell(updatedPlayerStates);
            }

            try
            {
                if (updateLoginTable.Rows.Count > 0)
                {
                    SqlCommand command  = new SqlCommand("UpdateUserOnline", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@tblUpdates", updateLoginTable));
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Exception has been occured in DBWriteWorker while updating player states : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                if (updateLoginItems.Count > 0)
                    Context.Parent.Tell(updateLoginItems);
            }

            try
            {
                if (updateGameTable.Rows.Count > 0)
                {
                    SqlCommand command = new SqlCommand("UpdateUserGame", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@tblUpdates", updateGameTable));
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBWriteWorker while updating player states : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                if (updateGameItems.Count > 0)
                    Context.Parent.Tell(updateGameItems);
            }

            try
            {
                if (updateLobbyOffTable.Rows.Count > 0)
                {
#if FORTEST
                    SqlCommand command = new SqlCommand("UpdateTestUserOffline", connection);
#else
                    SqlCommand command = new SqlCommand("UpdateUserOffline", connection);
#endif
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@tblUpdates", updateLobbyOffTable));
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBWriteWorker while updating player states : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                if(updateLobbyOffItems.Count > 0)
                    Context.Parent.Tell(updateLobbyOffItems);
            }
            return updatedPlayerStates.Count;
        }
        private async Task<int> updateUserBetMoney(SqlConnection connection)
        {
            List<UserBetMoneyUpdateItem> betMoneyUpdates = null;
            try
            {
                betMoneyUpdates = await Context.Parent.Ask<List<UserBetMoneyUpdateItem>>("PopUserBetMoneyUpdates", TimeSpan.FromSeconds(5));
                if (betMoneyUpdates == null)
                    return 0;

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("id", typeof(long));
                dataTable.Columns.Add("betmoney", typeof(decimal));

                foreach (UserBetMoneyUpdateItem updateItem in betMoneyUpdates)
                    dataTable.Rows.Add(updateItem.UserDBID, updateItem.UserBetMoney);

                SqlCommand command = new SqlCommand("UpdateUserBetMoney", connection);

                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@tblBetMoney", dataTable));

                await command.ExecuteNonQueryAsync();
                return dataTable.Rows.Count;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBWriteProxy while updating user betmoney : {0}", ex.ToString());

                //기록에 실패한 항목들을 다시 넣는다.
                Context.Parent.Tell(betMoneyUpdates);
                return -1;
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
                        _logger.Error("DBWriteWorker::updateBalances playerID <= 0 {0} {1}", updateItem.PlayerID, updateItem.Balance);

                    dataTable.Rows.Add((decimal)updateItem.Balance, updateItem.PlayerID);
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
