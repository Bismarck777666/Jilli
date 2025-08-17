using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using GITProtocol;
using Akka.Routing;
using StackExchange.Redis;
using Akka.Event;
using System.Diagnostics;

namespace ApiIntegration.Database
{
    public class RedisWriter : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Logging.GetLogger(Context);
        public RedisWriter()
        {
        }

        protected override void PreStart()
        {
            base.PreStart();

        }
    }    
}
