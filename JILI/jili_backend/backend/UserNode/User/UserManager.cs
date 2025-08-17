using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Routing;
using GITProtocol;
using StackExchange.Redis;
using Akka.Cluster;
using UserNode.Database;
using Akka.Event;
using System.Web.Http.ValueProviders;

namespace UserNode
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
            Receive<CreateNewUserMessage>(message =>
            {
                //해당 유저액터가 이미 존재하는가를 검사한다.
                if (Context.Child(message.GlobalUserID) != ActorRefs.Nobody)
                {
                    Sender.Tell(ActorRefs.Nobody);
                    return;
                }

                var userActor = Context.ActorOf(UserActor.Props(message, _dbReader, _dbWriter), message.GlobalUserID);
                _userHashMap.Add(message.GlobalUserID);
                Context.Watch(userActor);
                
                //액터의 패스를 레디스에 등록한후에 리턴한다.
                registerUserPathToRedis(message.GlobalUserID, userActor).PipeTo(Sender);

            });
            Receive<ForceLogoutMesssage>(message =>
            {
                var userActor = Context.Child(message.GlobalUserID);
                //만일 해당 유저가 로그인한 상태가 아니라면 
                if (userActor.Equals(ActorRefs.Nobody))
                    return;

                userActor.Tell(new QuitUserMessage(message.AgentID, message.UserID));
            });
            Receive<QuitUserMessage>(message =>
            {
                var userActor = Context.Child(message.GlobalUserID);
                //만일 해당 유저가 로그인한 상태가 아니라면 
                if (userActor.Equals(ActorRefs.Nobody))
                    return;

                userActor.Tell(message);
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
            Receive<SlotGamesNodeShuttingdown>(message =>
            {
                if (_userHashMap.Count > 0)
                   Context.ActorSelection("*").Tell(message);
                else
                    _logger.Info("SlotGamesNodeShuttingdown Message Received but no users online for now");
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
            
        private string getActorRemotePath(IActorRef actor)
        {
            string strActorPath      = actor.Path.ToString();
            string strClusterAddress = Cluster.Get(Context.System).SelfAddress.ToString();
            int    start             = strActorPath.IndexOf("/user");
            string strRemotePath     = strClusterAddress + strActorPath.Substring(start);
            return strRemotePath;
        }

        private async Task<IActorRef> registerUserPathToRedis(string strGlobalUserID, IActorRef userActor)
        {
            try
            {
                //레디스에 액터패스, 유저토큰을 등록한다.
                string strUserPathFieldName  = string.Format("{0}_path", strGlobalUserID);
                await RedisDatabase.RedisCache.HashSetAsync("onlineusers", strUserPathFieldName, getActorRemotePath(userActor));

                return userActor;
            }
            catch (Exception)
            {
                return ActorRefs.Nobody;
            }
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
}
