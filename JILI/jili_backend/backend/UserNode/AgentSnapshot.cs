using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserNode
{
    public class AgentSnapshot
    {
        private static AgentSnapshot _sInstance = new AgentSnapshot();
        public static AgentSnapshot Instance 
        { 
            get { return _sInstance; } 
        }
        public DateTime LastUpdateTime { get; set; }
        protected ConcurrentDictionary<int, AgentConfig> _agentConfigs = new ConcurrentDictionary<int, AgentConfig>();

        public AgentSnapshot()
        {
            this.LastUpdateTime = new DateTime(1, 1, 1);
        }
        public void updateAgentConfig(int agentID, AgentConfig agentConfig)
        {
            _agentConfigs.AddOrUpdate(agentID, agentConfig, (key, oldValue) => agentConfig);
        }
        public AgentConfig findAgentConfig(int agentID)
        {
            AgentConfig agentConfig = null;
            if (_agentConfigs.TryGetValue(agentID, out agentConfig))
                return agentConfig;
            return null;
        }
    }
    public class AgentConfig
    {
        public string CallbackURL   { get; set; }
        public string ApiToken      { get; set; }
        public string SecretKey     { get; set; }

        public AgentConfig(string apiToken, string secrectKey, string callbackURL) 
        {
            this.ApiToken       = apiToken;
            this.SecretKey      = secrectKey;
            this.CallbackURL    = callbackURL;
        }

    }
}
