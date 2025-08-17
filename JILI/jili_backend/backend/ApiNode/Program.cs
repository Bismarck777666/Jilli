using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace QueenApiNode
{
    internal class Program
    {
        static void Main(string[] args)
        {

            HostFactory.Run(x => {

                x.Service<APIService>(serviceConfig => {
                    serviceConfig.ConstructUsing(name => new APIService());
                    serviceConfig.WhenStarted(os => os.Start());
                    serviceConfig.WhenStopped(os => os.StopAsync().Wait());
                });

                x.RunAsLocalSystem();

                x.SetDescription("Amatic ApiNode Service for GodGaming Solution");
                x.SetServiceName("Amatic ApiNode");
                x.SetDisplayName("Amatic ApiNode");
                x.StartAutomatically();
            });
        }        
    }
}
