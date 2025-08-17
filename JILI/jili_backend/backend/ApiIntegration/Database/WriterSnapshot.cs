using GITProtocol;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ApiIntegration.Database
{
    public class WriterSnapshot
    {
        private static WriterSnapshot _sInstance = new WriterSnapshot();
        public static WriterSnapshot Instance => _sInstance;
        private Dictionary<long, PlayerBalanceUpdateItem>   _dicPlayerBalanceUpates     = new Dictionary<long, PlayerBalanceUpdateItem>();
        
        public void updatePlayerBalanceUpdate(PlayerBalanceUpdateItem balanceUpdateItem)
        {
            long playerID = balanceUpdateItem.PlayerID;
            if (_dicPlayerBalanceUpates.ContainsKey(playerID))
                _dicPlayerBalanceUpates[playerID].mergeUpdate(balanceUpdateItem);
            else
                _dicPlayerBalanceUpates[playerID] = balanceUpdateItem;
        }
        public List<PlayerBalanceUpdateItem> PopBalanceUpdates(int count = 5000)
        {
            if (_dicPlayerBalanceUpates.Count == 0)
                return null;

            List<PlayerBalanceUpdateItem> balanceUpdates = new List<PlayerBalanceUpdateItem>();
            foreach (KeyValuePair<long, PlayerBalanceUpdateItem> pair in _dicPlayerBalanceUpates)
            {
                balanceUpdates.Add(pair.Value);
                if (balanceUpdates.Count >= count)
                    break;
            }
            for (int i = 0; i < balanceUpdates.Count; i++)
                _dicPlayerBalanceUpates.Remove(balanceUpdates[i].PlayerID);

            return balanceUpdates;
        }
        public void PushBalanceUpdateItems(List<PlayerBalanceUpdateItem> balanceUpdates)
        {
            for (int i = 0; i < balanceUpdates.Count; i++)
            {
                long playerID = balanceUpdates[i].PlayerID;
                if (_dicPlayerBalanceUpates.ContainsKey(playerID))
                    _dicPlayerBalanceUpates[playerID].mergeUpdate(balanceUpdates[i]);
                else
                    _dicPlayerBalanceUpates[playerID] = balanceUpdates[i];
            }
        }
    }

    public class PlayerBalanceUpdateItem
    {
        public PlayerBalanceUpdateItem(long playerID, double balanceIncrement)
        {
            this.PlayerID           = playerID;
            this.BalanceIncrement   = balanceIncrement;
        }
            
        public void mergeUpdate(PlayerBalanceUpdateItem updateItem)
        {
            this.BalanceIncrement += updateItem.BalanceIncrement;
        }

        public void mergeUpdate(double balanceUpdate)
        {
            this.BalanceIncrement += balanceUpdate;
        }
        public long     PlayerID            { get; private set; }            
        public double   BalanceIncrement    { get; private set; }
    }
}
