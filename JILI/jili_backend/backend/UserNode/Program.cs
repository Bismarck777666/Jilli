using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace UserNode
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x => {

                x.Service<UserNodeService>(serviceConfig => {
                    serviceConfig.ConstructUsing(name => new UserNodeService());
                    serviceConfig.WhenStarted(os => os.Start());
                    serviceConfig.WhenStopped(os => os.StopAsync().Wait());
                });

                x.RunAsLocalSystem();

                x.SetDescription("Amatic UserNode for GIT IGaming Solution");
                x.SetServiceName("Amatic UserNode");
                x.SetDisplayName("Amatic UserNode");
                x.StartAutomatically();
            });
        }
        

    }
}
