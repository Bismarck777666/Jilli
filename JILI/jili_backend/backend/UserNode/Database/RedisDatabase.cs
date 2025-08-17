using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace UserNode.Database
{
    public class RedisDatabase
    {
        private static string   _sRedisIP;
        private static int      _sRedisPort;
        private static string   _sPassword;
        private static int      _database;
        public static LuaScript CountDailyScript;
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(string.Format("{0}:{1},password={2}", _sRedisIP, _sRedisPort, _sPassword));
        });

        static RedisDatabase()
        {
            
        }
        public static void setRedisInfo(string strIP, int port, string strPassword, int database)
        {
            _sRedisIP   = strIP;
            _sRedisPort = port;
            _sPassword  = strPassword;
            _database   = database;
        }
        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }
        public static IDatabase RedisCache
        {
            get
            {
                return Connection.GetDatabase(_database);
            }
        }
    }
}
