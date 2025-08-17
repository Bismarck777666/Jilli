using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.Database
{
    public class WriterSnapshot
    {

        private static WriterSnapshot   _sInstance  = new WriterSnapshot();
        public static WriterSnapshot    Instance    => _sInstance;

        private UpdatePayoutPoolStatus              _updatePayoutPoolItem   = null;

        public void updatePayoutPoolStatus(UpdatePayoutPoolStatus item)
        {
            _updatePayoutPoolItem = item;
        }
        public UpdatePayoutPoolStatus PopUpdatePayoutPoolStatus()
        {
            if (_updatePayoutPoolItem == null)
                return null;

            UpdatePayoutPoolStatus item = _updatePayoutPoolItem;
            _updatePayoutPoolItem       = null;
            return item;
        }
    }
}
