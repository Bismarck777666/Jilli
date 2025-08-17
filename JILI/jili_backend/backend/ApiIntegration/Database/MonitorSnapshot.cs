using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GITProtocol;
using System.Collections.Concurrent;

namespace ApiIntegration.Database
{
    public class DBMonitorSnapshot
    {
        private static DBMonitorSnapshot _sInstance = new DBMonitorSnapshot();
        public static DBMonitorSnapshot Instance
        {
            get
            {
                return _sInstance;
            }
        }

        public DBMonitorSnapshot()
        {

        }
    }    
}
