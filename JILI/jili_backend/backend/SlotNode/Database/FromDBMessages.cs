using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GITProtocol;

namespace SlotGamesNode.Database
{
    public class PayoutConfigUpdated
    {
        public PayoutConfigUpdated(GAMEID gameID)
        {
            this.GameID = gameID;
        }
        public GAMEID GameID { get; private set; }
    }

    public class AgentPayoutConfigUpdated
    {
        public GAMEID   GameID          { get; private set; }
        public int      AgentID         { get; private set; }
        public bool     ChangedPayout   { get; private set; }
        public AgentPayoutConfigUpdated(GAMEID gameID, int agentID, bool changedPayout)
        {
            this.GameID         = gameID;
            this.AgentID        = agentID;
            this.ChangedPayout  = changedPayout;
        }
    }
}
