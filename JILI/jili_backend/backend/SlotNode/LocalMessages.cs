using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode
{
    public class DBProxyInform
    {
        public IActorRef DBReader       { get; private set; }
        public IActorRef DBWriter       { get; private set; }
        public IActorRef RedisWriter    { get; private set; }
        public DBProxyInform(IActorRef dbReader, IActorRef dbWriter, IActorRef redisWriter)
        {
            this.DBReader       = dbReader;
            this.DBWriter       = dbWriter;
            this.RedisWriter    = redisWriter;
        }
    }
}
