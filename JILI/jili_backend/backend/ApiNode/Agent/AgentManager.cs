using Akka.Actor;
using Akka.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QueenApiNode.Database;

namespace QueenApiNode.Agent
{
    class AgentManager : ReceiveActor
    {
        private readonly ILoggingAdapter        _logger            = Logging.GetLogger(Context);
        private bool                            _isShuttingDown    = false;
        private Dictionary<string, IActorRef>   _agentActors       = new Dictionary<string, IActorRef>();
        private Dictionary<string, string>      _authTokenMaps     = new Dictionary<string, string>();
        private Dictionary<int,    string>      _agentIDsMap       = new Dictionary<int, string>();

        public AgentManager()
        {
            Receive<CreateNewAgentActorMsg>(message =>
            {
                //해당 유저액터가 이미 존재하는가를 검사한다.
                IActorRef agentActor = Context.Child(message.AgentID);
                if (agentActor != ActorRefs.Nobody)
                {
                    Sender.Tell(agentActor);
                    return;
                }
                agentActor                      = Context.ActorOf(AgentActor.Props(message), message.AgentID);
                _agentIDsMap[message.AgentDBID] = message.AgentID;
                Context.Watch(agentActor);
                _agentActors[message.AgentID]   = agentActor;
                Sender.Tell(agentActor);
            });
            Receive<GetAgentActorMsg>(message =>
            {
                var agentActor = Context.Child(message.AgentID);
                Sender.Tell(agentActor);
            });
            Receive<Terminated>(terminated =>
            {
                int agentDBID = 0;
                foreach (KeyValuePair<int, string> pair in _agentIDsMap)
                {
                    if (pair.Value == terminated.ActorRef.Path.Name)
                    {
                        agentDBID = pair.Key;
                        break;
                    }
                }
                if (agentDBID > 0)
                    _agentIDsMap.Remove(agentDBID);
                _agentActors.Remove(terminated.ActorRef.Path.Name);
                if (_isShuttingDown && _agentActors.Count == 0)
                    Context.Stop(Self);
            });
            Receive<Dictionary<string, string>> (onAgentAuthTokens);
            Receive<AgentUpdated>               (onAgentUpdated);
            Receive<string>                     (onGetAgentID);
        }
        private void onAgentAuthTokens(Dictionary<string, string> agentAuthTokens)
        {
            _authTokenMaps.Clear();
            foreach (KeyValuePair<string, string> pair in agentAuthTokens)
                _authTokenMaps[pair.Value] = pair.Key;
        }
        private void onAgentUpdated(AgentUpdated updated)
        {
            string strAuthKey = null;
            foreach(KeyValuePair<string,string> pair in _authTokenMaps)
            {
                if(pair.Value == updated.AgentID)
                {
                    strAuthKey = pair.Key;
                    break;
                }
            }
            if (strAuthKey == null)
            {
                _authTokenMaps[updated.AuthToken] = updated.AgentID;
            }
            else
            {
                _authTokenMaps.Remove(strAuthKey);
                _authTokenMaps[updated.AuthToken] = updated.AgentID;
            }

            var agentActor = Context.Child(updated.AgentID);
            if (agentActor != null)
                agentActor.Forward(updated);
        }
        private void onGetAgentID(string strAuthToken)
        {
            if (_authTokenMaps.ContainsKey(strAuthToken))
                Sender.Tell(_authTokenMaps[strAuthToken]);
            else
                Sender.Tell(string.Empty);
        }
    }
    public class CreateNewAgentActorMsg
    {
        public string       AgentID     { get; private set; }
        public int          AgentDBID   { get; private set; }
        public string       AuthToken   { get; private set; }
        public IActorRef    DBReader    { get; private set; }
        public IActorRef    DBWriter    { get; private set; }
        public int          Currency    { get; private set; }
        public int          Language    { get; private set; }

        public CreateNewAgentActorMsg(int dbID, string strAgentID, string authToken, int currency, int language, IActorRef dbReader, IActorRef dbWriter)
        {
            this.AgentDBID  = dbID;
            this.AgentID    = strAgentID;
            this.AuthToken  = authToken;
            this.DBReader   = dbReader;
            this.DBWriter   = dbWriter;
            this.Currency   = currency;
            this.Language   = language;
        }
    }
    public class GetAgentActorMsg
    {
        public string AgentID { get; private set; }
        public GetAgentActorMsg(string strWebsiteID)
        {
            this.AgentID = strWebsiteID;
        }
    }
}
