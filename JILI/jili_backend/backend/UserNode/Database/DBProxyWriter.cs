using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace UserNode.Database
{
    public class DBProxyWriter : ReceiveActor
    {

        private string                      _strConnString                  = "";
        private readonly  ILoggingAdapter   _logger                         = Logging.GetLogger(Context);
        private IActorRef                   _workerChild                    = null;
        private IActorRef                   _workerChildForGameLog          = null;
        private IActorRef                   _workerChildForReport           = null;
        private bool                        _isShuttingDown                 = false;

        public DBProxyWriter(string strConnStriing)
        {
            _strConnString = strConnStriing;

            Receive<string>(mesasge => processCommand(mesasge));
            Receive<BaseInsertItem>(item =>
            {
                WriterSnapshot.Instance.insertDBItem(item);
            });
            Receive<PlayerBalanceUpdateItem>(updateItem =>
            {
                WriterSnapshot.Instance.updatePlayerBalanceUpdate(updateItem);
            });
            Receive<FailedTransactionItem>(_ =>
            {
                WriterSnapshot.Instance.insertFailedTransaction(_);
            });
            Receive<DepositTransactionUpdateItem>(_ =>
            {
                WriterSnapshot.Instance.updateDepositTransaction(_);
            });
            Receive<UserStateUpdateItem>(updateItem =>
            {
                WriterSnapshot.Instance.updatePlayerState(updateItem);
            });
            Receive<GameLogItem>(item =>
            {
                WriterSnapshot.Instance.insertGameLog(item);
            });
            Receive<ReportUpdateItem>(updateItem =>
            {
                ReportSnapshot.Instance.updateReports(updateItem);
            });
            Receive<List<ReportUpdateItem>>(message =>
            {
                ReportSnapshot.Instance.PushReportUpdateItems(message);
            });
            Receive<GameReportItem>(updateItem =>
            {
                ReportSnapshot.Instance.updateGameReport(updateItem);
            });
            Receive<List<GameReportItem>>(message =>
            {
                ReportSnapshot.Instance.PushGameReportUpdates(message);
            });
            Receive<AgentPointAdded>(_ =>
            {
                ReportSnapshot.Instance.addAgentPoints(_);
            });
            Receive<UserRollPointAdded>(_ =>
            {
                ReportSnapshot.Instance.addUserPoint(_);
            });
            Receive<List<UserRollPointAdded>>(_ =>
            {
                ReportSnapshot.Instance.PushUserPointAdds(_);
            });
            Receive<List<AgentPointAdded>>(_ =>
            {
                ReportSnapshot.Instance.PushAgentPointAdds(_);
            });
            Receive<List<BaseInsertItem>>(message =>
            {
                WriterSnapshot.Instance.PushDBInsertItems(message);
            });
            Receive<List<UserStateUpdateItem>>(message =>
            {
                WriterSnapshot.Instance.PushPlayerStates(message);
            });
            Receive<List<PlayerBalanceUpdateItem>>(message =>
            {
                WriterSnapshot.Instance.PushBalanceUpdateItems(message);
            });
            Receive<List<GameLogItem>>(message =>
            {
                WriterSnapshot.Instance.PushGameLogItems(message);
            });
            Receive<List<FailedTransactionItem>>(_ =>
            {
                WriterSnapshot.Instance.PushFailedTransactionItems(_);
            });
            Receive<List<DepositTransactionUpdateItem>>(_ =>
            {
                WriterSnapshot.Instance.PushUpdatedDepositTransactions(_);
            });
            Receive<UserBetMoneyUpdateItem>(item =>
            {
                WriterSnapshot.Instance.updateUserBetMoney(item);
            });
            Receive<FetchUserBalanceUpdate>(item =>
            {
                double balanceUpdate = WriterSnapshot.Instance.fetchUserBalanceUpdates(item.UserDBID);
                Sender.Tell(balanceUpdate);
            });
            Receive<ApiTransactionItem>(item =>
            {
                WriterSnapshot.Instance.insertApiTransaction(item);
            });
            Receive<List<ApiTransactionItem>>(items =>
            {
                WriterSnapshot.Instance.PushApiTransactionItems(items);
            });
            Receive<Terminated>(_ =>
            {
                _logger.Info("DBProxyWriter::Terminated");
                if(_isShuttingDown)
                {
                    if (_workerChild != null &&  _.ActorRef.Equals(_workerChild))
                        _workerChild = null;

                    if (_workerChildForGameLog != null && _.ActorRef.Equals(_workerChildForGameLog))
                        _workerChildForGameLog = null;

                    if (_workerChildForReport != null && _.ActorRef.Equals(_workerChildForReport))
                        _workerChildForReport = null;

                    if (_workerChild == null && _workerChildForGameLog == null && _workerChildForReport == null)
                    {
                        _logger.Info("DB Proxy Terminated : Ended");
                        Self.Tell(PoisonPill.Instance);
                    }
                }
            });
        }

        public static Props Props(string strConnString)
        {
            return Akka.Actor.Props.Create(() => new DBProxyWriter(strConnString));
        }


        private void processCommand(string strCommand)
        {
            if (strCommand == "PopPlayerStates")
            {
                List<UserStateUpdateItem> updatedPlayerStates = WriterSnapshot.Instance.PopPlayerStates();
                Sender.Tell(updatedPlayerStates);
            }
            else if (strCommand == "PopBalanceUpdates")
            {
                List<PlayerBalanceUpdateItem> updateBalanceItems = WriterSnapshot.Instance.PopBalanceUpdates();
                Sender.Tell(updateBalanceItems);
            }
            else if (strCommand == "PopGameLogItems")
            {
                List<GameLogItem> gameLogItems = WriterSnapshot.Instance.PopGameLogItems();
                Sender.Tell(gameLogItems);
            }
            else if (strCommand == "PopFailedTransactionItems")
            {
                List<FailedTransactionItem> items = WriterSnapshot.Instance.PopFaildTransactions();
                Sender.Tell(items);
            }
            else if(strCommand == "PopUpdatedDepositTransactions")
            {
                List<DepositTransactionUpdateItem> items = WriterSnapshot.Instance.PopUpdatedDepositTransactions();
                Sender.Tell(items);
            }
            else if (strCommand == "PopInsertItems")
            {
                List<BaseInsertItem> insertItems = WriterSnapshot.Instance.PopDBInsertItems();
                Sender.Tell(insertItems);
            }
            else if (strCommand == "PopReportUpdates")
            {
                List<ReportUpdateItem> updateReportItems = ReportSnapshot.Instance.PopReportUpdates();
                Sender.Tell(updateReportItems);
            }
            else if (strCommand == "PopAgentPointAdds")
            {
                List<AgentPointAdded> items = ReportSnapshot.Instance.PopAgentPointAdds();
                Sender.Tell(items);
            }
            else if (strCommand == "PopUserPointAdds")
            {
                List<UserRollPointAdded> items = ReportSnapshot.Instance.PopUserPointAdds();
                Sender.Tell(items);
            }
            else if (strCommand == "PopGameReportUpdates")
            {
                List<GameReportItem> items = ReportSnapshot.Instance.PopGameReportUpdates();
                Sender.Tell(items);
            }
            else if(strCommand == "PopUserBetMoneyUpdates")
            {
                List<UserBetMoneyUpdateItem> items = WriterSnapshot.Instance.PopUserBetMoneyUpdates();
                Sender.Tell(items);
            }
            else if(strCommand == "PopApiTransactionItems")
            {
                List<ApiTransactionItem> items = WriterSnapshot.Instance.fetchApiTransactionItems();
                Sender.Tell(items);
            }
            else if (strCommand == "terminate")
            {
                _isShuttingDown = true;
                _workerChild.Tell("flush");
                _workerChild.Tell(PoisonPill.Instance);

                _workerChildForGameLog.Tell("flush");
                _workerChildForGameLog.Tell(PoisonPill.Instance);

                _workerChildForReport.Tell("flush");
                _workerChildForReport.Tell(PoisonPill.Instance);

            }
        }

        protected override void PreStart()
        {
            base.PreStart();
            _workerChild                = Context.ActorOf(DBWriteWorker.Props(_strConnString), "writeWorker");
            _workerChildForGameLog      = Context.ActorOf(DBGameLogWriteWorker.Props(_strConnString), "writeWorkerForGameLog");
            _workerChildForReport       = Context.ActorOf(DBReportWriteWorker.Props(_strConnString), "writeWorkerForReport");
            Context.Watch(_workerChild);
            Context.Watch(_workerChildForGameLog);
            Context.Watch(_workerChildForReport);

        }

        protected override void PreRestart(Exception reason, object message)
        {
            Self.Tell(message);
            base.PreRestart(reason, message);
        }
    }


}
