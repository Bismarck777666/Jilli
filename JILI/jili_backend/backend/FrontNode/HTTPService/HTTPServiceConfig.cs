using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontNode.HTTPService
{
    public class HTTPServiceConfig
    {
        private static HTTPServiceConfig _sInstance = new HTTPServiceConfig();

        public static HTTPServiceConfig Instance
        {
            get
            {
                return _sInstance;
            }
        }

        public IActorRef WorkerGroup        { get; set; }
        public IActorRef AuthWorkerGroup    { get; set; }
    }
}
