using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics
{
    class PayoutPoolConfig
    {
        private static PayoutPoolConfig _sInstance  = new PayoutPoolConfig();
        public static PayoutPoolConfig  Instance    => _sInstance;
        public IActorRef                PoolActor                   { get; set; }
        public Dictionary<int, double>  WebsitePayoutRedundency     { get; set; }

        public PayoutPoolConfig()
        {
            this.WebsitePayoutRedundency = new Dictionary<int, double>();
        }

    }
}
