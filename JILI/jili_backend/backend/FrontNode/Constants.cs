using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontNode
{
    public class Constants
    {
        public static readonly string   CheckConnectionCommand  = "checkConnections";
        public static readonly TimeSpan HeartbeatTimeout        = TimeSpan.FromSeconds(300);
    }
}
