using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Routing;
using SlotGamesNode.GameLogics;
using GITProtocol;
using SlotGamesNode.Database;

namespace SlotGamesNode
{
    public class BootstrapActor : ReceiveActor
    {
        private Config                      _configuration      = null;
        private readonly ILoggingAdapter    _logger             = Logging.GetLogger(Context);
        private IActorRef                   _dbProxy            = null;
        private IActorRef                   _gameManager        = null;
        private IActorRef                   _redisWriter        = null;
        private string                      _serverID           = "";
        private IActorRef                   _commServerGroup    = null;

        public BootstrapActor(Config configuration)
        {
            _configuration = configuration;

            Receive<string>(command =>
            {
                processCommand(command);
            });

            ReceiveAsync<DBProxy.ReadyDBProxy>      (onReadyDBProxy);
            ReceiveAsync<SlotsNodeShuttingDownMsg>  (onShutdownServer);
        }

        public static Props Props(Config config)
        {
            return Akka.Actor.Props.Create(() => new BootstrapActor(config));
        }

        private async Task onReadyDBProxy(DBProxy.ReadyDBProxy dbActors)
        {
            _logger.Info("Database Proxy has been successfully initialized.");

            //Redis자료기지액터를 창조한다.
            _redisWriter        = Context.System.ActorOf(Akka.Actor.Props.Create(() => new RedisWriter()).WithRouter(FromConfig.Instance), "redisWriter");
            //련결서버루터를 창조한다.
            _commServerGroup    = Context.System.ActorOf(Akka.Actor.Props.Empty.WithRouter(FromConfig.Instance), "commServers");

            Context.System.ActorOf(Akka.Actor.Props.Empty.WithRouter(FromConfig.Instance), "apiWorker");

            PayoutPoolConfig.Instance.PoolActor = Context.System.ActorOf(Akka.Actor.Props.Create(() => new PayoutPoolActor()), "payoutPool");
            Context.System.ActorOf(Akka.Actor.Props.Create(() => new SpinDBReader()).WithRouter(FromConfig.Instance), "spinDBReaders");

            //자료기지가 초기화되면 게임로직을 시작한다.
            var props    = Akka.Actor.Props.Create(() => new GameCollectionActor(dbActors.Reader, dbActors.Writer, _redisWriter)).WithRouter(FromConfig.Instance);
            _gameManager = Context.System.ActorOf(props, "gameManager");

            _logger.Info("started loading game database....");
            bool iSuccess = await _gameManager.Ask<bool>(new LoadSpinDataRequest());

            //bool isPerformed = await _gameManager.Ask<bool>(new PerformanceTestRequest());
            //return;

            if (!iSuccess)
            {
                _logger.Error("failed in  loading of game database.");
                return;
            }
            _logger.Info("completed loading of game database successfully.");
            _logger.Info("cluster joining...");

            var cluster = Akka.Cluster.Cluster.Get(Context.System);
            IList<string> strSeedNodeList = _configuration.GetStringList("seednodes");
            List<Address> seedNodeAddressList = new List<Address>();
            foreach (string strAddress in strSeedNodeList)
                seedNodeAddressList.Add(Address.Parse(strAddress));

            await cluster.JoinSeedNodesAsync(seedNodeAddressList);
        }
        private async Task onShutdownServer(SlotsNodeShuttingDownMsg message)
        {
            _logger.Info("Shutting down server....");

            _logger.Info("Inform connect servers to remove this server from router....");
            
            //연결서버들에 게임서버가 인차 종료된다는것을 알린다.            
            _commServerGroup.Tell(new Broadcast(new SlotsNodeShuttingDownMsg()), _gameManager);

            _logger.Info("Waiting for 20 seconds until all the users connected to this server can connect to other servers");
            //20초동안 대기한다.
            await Task.Delay(20000);
            
            //먼저 게임로직들을 중지한다.
            _logger.Info("Terminating game logic actors....");
            await _gameManager.GracefulStop(TimeSpan.FromSeconds(10), "terminate");

            //Redis자료기지액터를 중지한다.
            _logger.Info("Terminating redis write actors....");
            await _redisWriter.GracefulStop(TimeSpan.FromSeconds(10));
            
            //기본자료기지액터들을 중지한다.
            _logger.Info("Terminating database proxy actors....");
            await _dbProxy.GracefulStop(TimeSpan.FromSeconds(30), "terminate");

            Sender.Tell(true);
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

                _serverID = _configuration.GetString("identifider", "");
                _logger.Info("Start Single Game Server(id:{0})...", _serverID);
                _logger.Info("Initializing database proxy...");

                //첫단계로 자료기지련결부분을 초기화한다.
                _dbProxy        = Context.System.ActorOf(DBProxy.Props(dbConfig), "dbproxy");
                _dbProxy.Tell("initialize");
            }
            else
            {

            }
        }
    }
}
