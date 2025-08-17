using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PCGSharp;

namespace SlotGamesNode.GameLogics
{
    public class GameUtils
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static int selectFromProbs(int[] probs)
        {
            int sum = 0;
            for (int i = 0; i < probs.Length; i++)
                sum += probs[i];

            int random = Pcg.Default.Next(0, sum);
            sum = 0;
            for(int i = 0; i < probs.Length; i++)
            {
                sum += probs[i];
                if (random < sum)
                    return i;
            }
            return 0;
        }

        public static long GetCurrentUnixTimestampMillis()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }

        public static long GetCurrentUnixTimestamp()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
        }
    }
}
