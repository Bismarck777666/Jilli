using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Akka.Routing;

namespace QueenApiNode.Database
{
    public class DBProxy : ReceiveActor
    {
        private string      _strConnectionString = "";
        private IActorRef   _readerRouter;
        private IActorRef   _monitorActor;
        private IActorRef   _writeActor;
        private int         _poolSize = 0;

        private readonly ILoggingAdapter _logger = Logging.GetLogger(Context);

        public DBProxy(Config config)
        {
            //ConnectionString 을 빌드한다.
            SqlConnectionStringBuilder connStringBuilder = new SqlConnectionStringBuilder();
            connStringBuilder.DataSource        = config.GetString("ip", "127.0.0.1");
            connStringBuilder.InitialCatalog    = config.GetString("dbname", "xe888");
            connStringBuilder.UserID            = config.GetString("user", "root");
            connStringBuilder.Password          = config.GetString("pass", "akduifwkro");
            _poolSize                           = (int)config.GetInt("poolcount", 2);
            _strConnectionString                = connStringBuilder.ConnectionString;

            ReceiveAsync<string>(processCommand);
        }

        public static Props Props(Config config)
        {
            return Akka.Actor.Props.Create(() => new DBProxy(config));
        }
        private async Task processCommand(string command)
        {
            if (command == "initialize")
            {
                //자료기지서버의 련결가능성을 검사한다.
                if (!await checkDBConnection())
                {
                    _logger.Error("Can not connect to database. Please check if database is correctly configured.");
                    return;
                }

                //모니터액터를 초기화한다.
                await _monitorActor.Ask("initialize");
                Sender.Tell(new ReadyDBProxy(_readerRouter, _writeActor));
            }
            else if (command == "terminate")
            {
                _monitorActor.Tell(PoisonPill.Instance);
                _readerRouter.Tell(new Broadcast(PoisonPill.Instance));
                _writeActor.Tell("terminate");

                Become(ShuttingDown);
            }
        }

        private void ShuttingDown()
        {
            Receive<Terminated>(terminated =>
            {
                if (_monitorActor != null && terminated.ActorRef.Equals(_monitorActor))
                    _monitorActor = null;
                else if (_readerRouter != null && terminated.ActorRef.Equals(_readerRouter))
                    _readerRouter = null;

                if (_monitorActor == null && _readerRouter == null)
                    Self.Tell(PoisonPill.Instance);
            });
        }
        private async Task<bool> checkDBConnection()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnectionString))
                {
                    await connection.OpenAsync();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override void PreStart()
        {
            //자식액터들을 창조한다.
            _readerRouter   = Context.ActorOf(DBProxyReader.Props(_strConnectionString, _poolSize), "readers");
            _monitorActor   = Context.ActorOf(DBProxyMonitor.Props(_strConnectionString), "monitor");
            _writeActor     = Context.ActorOf(DBProxyWriter.Props(_strConnectionString), "writer");


            Context.Watch(_readerRouter);
            Context.Watch(_monitorActor);
            Context.Watch(_writeActor);

            base.PreStart();
        }

        #region Messages
        public class ReadyDBProxy
        {
            public IActorRef Reader { get; private set; }
            public IActorRef Writer { get; private set; }
            public ReadyDBProxy(IActorRef reader, IActorRef writer)
            {
                this.Reader = reader;
                this.Writer = writer;
            }
        }
        #endregion

    }
}
