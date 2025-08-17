using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using GITProtocol;
using Akka.Routing;
using Newtonsoft.Json;

/****
 * 
 *          Created by Foresight(2021.03.12)
 *          해당 노드내부에서 교환되는 메세지들을 정의한다.
 * 
 */

namespace UserNode
{            
    public class QuitUserMessage
    {
        public int    AgentID   { get; private set; }
        public string UserID    { get; private set; }

        public string GlobalUserID
        {
            get { return string.Format("{0}_{1}", AgentID, UserID); }
        }
        public QuitUserMessage(int agentID, string strUserID)
        {
            this.AgentID    = agentID;
            this.UserID     = strUserID;
        }
    }
    
    public class SlotGamesNodeShuttingdown
    {
        public string Path { get; private set; }
        public SlotGamesNodeShuttingdown(string strPath)
        {
            this.Path = strPath;
        }
    }
    
    public class QuitAndNotifyMessage
    {
    }
}
