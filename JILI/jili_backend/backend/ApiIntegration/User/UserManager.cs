using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using StackExchange.Redis;
using Akka.Cluster;
using ApiIntegration.Database;
using Akka.Event;
using System.Web.Http.ValueProviders;

namespace ApiIntegration
{
    public class UserManager : ReceiveActor
    {
        private HashSet<string>             _userHashMap    = new HashSet<string>();
        private bool                        _isShuttingDown = false;
        private readonly ILoggingAdapter    _logger         = Logging.GetLogger(Context);
        private IActorRef _dbReader;
        private IActorRef _dbWriter;

        public UserManager(IActorRef dbReader, IActorRef dbWriter)
        {
            Receive<CreateNewUserMsg>(message =>
            {
                //해당 유저액터가 이미 존재하는가를 검사한다.
                IActorRef userActor = Context.Child(message.UserID);
                if (userActor != ActorRefs.Nobody)
                {
                    Sender.Tell(userActor);
                    return;
                }

                userActor = Context.ActorOf(UserActor.Props(message, _dbReader, _dbWriter), message.UserID);
                _userHashMap.Add(message.UserID);
                Context.Watch(userActor);
                Sender.Tell(userActor);
            });
            
            Receive<string>(command =>
            {
                if (command != "terminate")
                    return;

                Context.ActorSelection("*").Tell(PoisonPill.Instance);
                _isShuttingDown = true;
                if (_userHashMap.Count == 0)
                    Context.Stop(Self);
            });

            Receive<Terminated>(terminated =>
            {
                _userHashMap.Remove(terminated.ActorRef.Path.Name);
                if (_isShuttingDown && _userHashMap.Count == 0)
                    Context.Stop(Self);
            });

            _dbReader = dbReader;
            _dbWriter = dbWriter;

        }            
        public static Props Props(IActorRef dbReader, IActorRef dbWriter)
        {
            return Akka.Actor.Props.Create(() => new UserManager(dbReader, dbWriter));
        }

        protected override void PreStart()
        {
            base.PreStart();
        }
    }    

    public class CreateNewUserMsg
    {
        public long     UserDBID { get; private set; }
        public string   UserID   { get; private set; }
        public double   Balance  { get; private set; }
        public CreateNewUserMsg(long userDBID, string strUserID, double balance)
        {
            this.UserDBID   = userDBID;
            this.UserID     = strUserID;
            this.Balance    = balance;
        }
    }
}
