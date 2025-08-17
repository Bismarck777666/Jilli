using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using QueenApiNode.Database;
using Akka.Routing;
using QueenApiNode.HttpService;
using Microsoft.Owin.Hosting;
using GITProtocol;
using QueenApiNode.Agent;

namespace QueenApiNode
{
    public class BootstrapActor : ReceiveActor
    {
        private Config                      _configuration      = null;
        private readonly ILoggingAdapter    _logger             = Logging.GetLogger(Context);
        private IActorRef                   _dbProxy            = null;
        private IDisposable                 _httpWebService     = null;
        private IActorRef                   _agentManager       = null;


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

                _agentManager               = Context.System.ActorOf(Akka.Actor.Props.Create(() => new AgentManager()), "agentManager");
                _agentManager.Tell(DBMonitorSnapshot.Instance.AgentHashKeys);

                ApiConfig.WorkActorGroup    = Context.System.ActorOf(Akka.Actor.Props.Create(() => new HTTPWorkActor(dbActors.Reader, dbActors.Writer)).WithRouter(FromConfig.Instance), "httpWorkers");

                //웹요청서비스
                var httpConfig = _configuration.GetConfig("http");
                if (httpConfig != null)
                {
                    string baseAddress = httpConfig.GetString("baseurl", "http://127.0.0.1/");
                    _httpWebService = WebApp.Start<Startup>(url: baseAddress);
                }
                var gameFrontConfig = _configuration.GetConfig("gameFront");
                if (gameFrontConfig != null)
                {
                    ApiConfig.GameFrontUrl  = gameFrontConfig.GetString("url");
                    ApiConfig.FrontTokenKey = gameFrontConfig.GetString("tokenkey");
                }

                var gameBackendConfig = _configuration.GetConfig("gameBackend");
                if (gameBackendConfig != null)
                {
                    ApiConfig.GameBackendGS = gameBackendConfig.GetString("gs");
                    ApiConfig.GameBackendBE = gameBackendConfig.GetString("be");
                }

            });
            ReceiveAsync<APINodeShutdownMsg>(onShutdownSystem);
        }

        public static Props Props(Config config)
        {
            return Akka.Actor.Props.Create(() => new BootstrapActor(config));
        }
        private void processCommand(string strCommand)
        {
            if (strCommand == "startService")
            {
                var dbConfig = _configuration.GetConfig("database");
                if (dbConfig == null)
                {
                    _logger.Error("config.hocon doesn't contain database configuration");
                    return;
                }

                _logger.Info("Initializing database proxy...");

                //첫단계로 자료기지련결부분을 초기화한다.
                _dbProxy = Context.System.ActorOf(DBProxy.Props(dbConfig), "dbproxy");
                _dbProxy.Tell("initialize");
            }
        }
        private async Task onShutdownSystem(APINodeShutdownMsg message)
        {
            try
            {
                //먼저 소켓서버들을 종료시킨다.
                _logger.Info("Shutting down tcp and web socket server...");

                if (_httpWebService != null)
                    _httpWebService.Dispose();

                await ApiConfig.WorkActorGroup.GracefulStop(TimeSpan.FromSeconds(300), new Broadcast("terminate"));

                //기본자료기지액터들을 중지한다.
                _logger.Info("Terminating database proxy actors....");
                await _dbProxy.GracefulStop(TimeSpan.FromSeconds(3600), "terminate");

                //클라스터에서 탈퇴한다.
                _logger.Info("Leaving from cluster....");
                var cluster = Akka.Cluster.Cluster.Get(Context.System);
                await cluster.LeaveAsync();
            }
            catch (Exception)
            {

            }
            Sender.Tell(true);
        }
    }

    public class ShutdownSystemMessage
    {

    }
}
