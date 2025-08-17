using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using System.IO;
using UserNode.Database;
using Topshelf;
using Akka.Event;

namespace UserNode
{
    public class UserNodeService
    {
        private ActorSystem                 _actorSystem;
        private IActorRef                   _bootstrapActor;

        public bool Start()
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info("Starting Connect Service...");

            //먼저 설정정보를 검사한다.
            Config clusterConfig = null;
            try
            {
                clusterConfig = ConfigurationFactory.ParseString(File.ReadAllText("config.hocon"));
            }
            catch
            {
                logger.Error("Please make sure if config.hocon file is correctly formatted");
                return false;
            }

            var userNodeConfig = clusterConfig.GetConfig("user");
            if(userNodeConfig == null)
            {
                logger.Error("config.hocon doesn't contain connector server configuration");
                return false;
            }

            //Redis 자료기지정보를 설정한다.
            var redisConfig = userNodeConfig.GetConfig("redis");
            if (redisConfig == null)
            {
                logger.Error("config.hocon doesn't contain redis configuration");
                return false;
            }
            setRedisInfo(redisConfig);
            
            string systemName   = userNodeConfig.GetString("actorsystem", "gitigaming");
            _actorSystem        = ActorSystem.Create(systemName, clusterConfig);

            _bootstrapActor     = _actorSystem.ActorOf(BootstrapActor.Props(userNodeConfig), "bootstrapper");
            _bootstrapActor.Tell("startService");
            return true;
        }
        private void setRedisInfo(Config redisConfig)
        {
            string  strRedisIP      = redisConfig.GetString("ip", "127.0.0.1");
            int     redisPort       = redisConfig.GetInt("port", 6379);
            string  redisPassword   = redisConfig.GetString("password", "");
            int     database        = redisConfig.GetInt("database",    0);

            RedisDatabase.setRedisInfo(strRedisIP, redisPort, redisPassword, database);
        }
        public Task TerminationHandle => _actorSystem.WhenTerminated;
        public async Task StopAsync()
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info("Stop Service Started...");
            try
            {
                await _bootstrapActor.Ask<bool>(new ShutdownSystemMessage(), TimeSpan.FromSeconds(60));
            }
            catch(Exception ex)
            {
                logger.Error("Exception has been occured in Stopping bootstrap actor {0}", ex.ToString());
            }
            await CoordinatedShutdown.Get(_actorSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);    
        }
    }
}
