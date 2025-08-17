using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Routing;
using GITProtocol;
using UserNode.Database;
using Microsoft.Owin.Hosting;

namespace UserNode
{
    public class BootstrapActor : ReceiveActor
    {
        private Config                      _configuration              = null;
        private readonly ILoggingAdapter    _logger                     = Logging.GetLogger(Context);
        private IActorRef                   _dbProxy                    = null;
        private IActorRef                   _retryWorkers               = null;
        private IActorRef                   _userManager                = null;
        private IActorRef                   _slotGamesRouter            = null;
        public BootstrapActor(Config configuration)
        {
            _configuration = configuration;

            Receive<string>(command =>
            {
                processCommand(command);
            });

            Receive<DBProxy.ReadyDBProxy>(dbActors =>
            {
                _logger.Info("Database Proxy has been successfully initialized.");
                _userManager        = Context.System.ActorOf(UserManager.Props(dbActors.Reader, dbActors.Writer), "userManager");
                _slotGamesRouter    = Context.System.ActorOf(Akka.Actor.Props.Empty.WithRouter(FromConfig.Instance), Constants.SlotGameRouterName);
                _retryWorkers       = Context.System.ActorOf(Akka.Actor.Props.Create(() => new RetryWorker(dbActors.Writer)).WithRouter(FromConfig.Instance), "retryWorkers");

                _retryWorkers.Tell(new Broadcast("tick"));
            });
            Receive<SlotsNodeShuttingDownMsg>   (_ => onSlotGamesNodeShutdown(_));
            ReceiveAsync<ShutdownSystemMessage> (onShutdownSystem);
        }

        public static Props Props(Config config)
        {
            return Akka.Actor.Props.Create(() => new BootstrapActor(config));
        }

        private void processCommand(string strCommand)
        {
            if(strCommand == "startService")
            {
                var dbConfig = _configuration.GetConfig("database");
                if (dbConfig == null)
                {
                    _logger.Error("config.hocon doesn't contain database configuration");
                    return;
                }

                _logger.Info("Initializing database proxy...");
                //자료기지련결부분을 초기화한다.
                _dbProxy = Context.System.ActorOf(DBProxy.Props(dbConfig), "dbproxy");
                _dbProxy.Tell("initialize");               
            }
        }

        private void onSlotGamesNodeShutdown(SlotsNodeShuttingDownMsg message)
        {
            Routee routee = new ActorSelectionRoutee(Context.System.ActorSelection(Sender.Path));
            _slotGamesRouter.Tell(new RemoveRoutee(routee));
            _userManager.Tell(new SlotGamesNodeShuttingdown(Sender.Path.ToString()));
        }

        private async Task onShutdownSystem(ShutdownSystemMessage message)
        {
            try
            {
                _logger.Info("Shutting down http service...");

                _logger.Info("Terminating user actors....");
                await _userManager.GracefulStop(TimeSpan.FromSeconds(300), "terminate");

                _logger.Info("Terminating database proxy actors....");
                await _dbProxy.GracefulStop(TimeSpan.FromSeconds(3600), "terminate");

                _logger.Info("Leaving from cluster....");
                var cluster = Akka.Cluster.Cluster.Get(Context.System);
                await cluster.LeaveAsync();
            }
            catch(Exception)
            {

            }
            Sender.Tell(true);
        }
    }

    public class ShutdownSystemMessage
    {

    }
}
