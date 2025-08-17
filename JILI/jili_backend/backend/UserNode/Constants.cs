using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserNode
{
    public class Constants
    {
        public static readonly TimeSpan RemoteTimeOut           = TimeSpan.FromSeconds(10);
        public static readonly string   SlotGameRouterName      = "slotgameRouter";
        public static readonly string   SlotGameRouterPath      = "/user/slotgameRouter";
        public static readonly string   CheckConnectionCommand  = "checkConnections";
        public static readonly TimeSpan HeartbeatTimeout        = TimeSpan.FromSeconds(30);

    }
}
