using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
namespace ApiIntegration.HTTPService
{
    public class HTTPServiceConfig
    {
        private static HTTPServiceConfig _sInstance = new HTTPServiceConfig();
        public static HTTPServiceConfig Instance    => _sInstance;

        public string       SecretKey       { get; set; }
        public IActorRef    WorkerGroup     { get; set; }

        public HTTPServiceConfig()
        {
            this.SecretKey = "9688abd2730146d8adf2b40fec2be20f";
        }
    }
}
