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
using ApiIntegration.HTTPService;
using System.Diagnostics;

namespace ApiIntegration.Database
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
