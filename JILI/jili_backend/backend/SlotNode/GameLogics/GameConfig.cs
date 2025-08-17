using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics
{
    public class GameConfig
    {
        public double   PayoutRate          { get; set; }
        public GameConfig(double payoutRate)
        {
            this.PayoutRate         = payoutRate;
        }
    }
}
