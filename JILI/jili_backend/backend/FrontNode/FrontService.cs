using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using System.IO;
using FrontNode.Database;
using Topshelf;
using Akka.Event;

namespace FrontNode
{
    public class FrontService
    {
        private ActorSystem                 _actorSystem;
        private IActorRef                   _bootstrapActor;

        public bool Start()
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info("Starting FrontNode Service...");

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

            var frontConfig = clusterConfig.GetConfig("front");
            if(frontConfig == null)
            {
                logger.Error("config.hocon doesn't contain connector server configuration");
                return false;
            }

            //Redis 자료기지정보를 설정한다.
            var redisConfig = frontConfig.GetConfig("redis");
            if (redisConfig == null)
            {
                logger.Error("config.hocon doesn't contain redis configuration");
                return false;
            }
            setRedisInfo(redisConfig);

            //먼저 설정파일에서 액터시스템의 이름을 얻는다.
            string systemName = frontConfig.GetString("actorsystem", "gitigaming");

            //액터시스템을 창조한다.
            _actorSystem =  ActorSystem.Create(systemName, clusterConfig);

            //부트스트랩액터를 창조한다.
            _bootstrapActor = _actorSystem.ActorOf(BootstrapActor.Props(frontConfig), "bootstrapper");
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
