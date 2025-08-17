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
using ApiIntegration.Database;
using ApiIntegration.HTTPService;
using Microsoft.Owin.Hosting;

namespace ApiIntegration
{
    public class BootstrapActor : ReceiveActor
    {
        private Config                      _configuration              = null;
        private readonly ILoggingAdapter    _logger                     = Logging.GetLogger(Context);
        private IActorRef                   _dbProxy                    = null;
        private IActorRef                   _redisWriter                = null;
        private IActorRef                   _userManager                = null;
        private IActorRef                   _httpWorkActorGroup         = null;
        private IDisposable                 _httpWebService             = null;

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

                _redisWriter            = Context.System.ActorOf(Akka.Actor.Props.Create(() => new RedisWriter()).WithRouter(FromConfig.Instance), "redisWriter");
                _userManager            = Context.System.ActorOf(UserManager.Props(dbActors.Reader, dbActors.Writer), "userManager");
                _httpWorkActorGroup     = Context.System.ActorOf(Akka.Actor.Props.Create(() => new HTTPWorkActor()).WithRouter(FromConfig.Instance),  "httpWorkers");

                HTTPServiceConfig.Instance.WorkerGroup          = _httpWorkActorGroup;

                var httpConfig = _configuration.GetConfig("http");
                if (httpConfig != null)
                {
                    string baseAddress = httpConfig.GetString("baseurl", "http://127.0.0.1/");
                    _httpWebService = WebApp.Start<Startup>(url: baseAddress);
                }                               
            });

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

        private async Task onShutdownSystem(ShutdownSystemMessage message)
        {
            try
            {
                _logger.Info("Shutting down http service...");

                if (_httpWebService != null)
                    _httpWebService.Dispose();

                await _httpWorkActorGroup.GracefulStop(TimeSpan.FromSeconds(300), new Broadcast("terminate"));

                _logger.Info("Terminating redis write actors....");
                _redisWriter.Tell(new Broadcast(PoisonPill.Instance));
                await _redisWriter.GracefulStop(TimeSpan.FromSeconds(300));

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
