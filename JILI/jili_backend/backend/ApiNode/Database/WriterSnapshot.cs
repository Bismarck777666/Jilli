using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueenApiNode.Database
{
    public class WriterSnapshot
    {
        private static WriterSnapshot _instance = new WriterSnapshot();
        public static WriterSnapshot Instance   => _instance;
    }
}
