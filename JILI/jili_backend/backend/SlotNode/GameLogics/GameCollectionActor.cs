using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using GITProtocol;
using SlotGamesNode.Database;
using Akka.Event;
using MongoDB.Bson;
using SlotGamesNode.GameLogics.JiliGames;
using SlotGamesNode.GameLogics.JiliGames.SlotGamesNode.GameLogics.JiliGames;

namespace SlotGamesNode.GameLogics
{
    public class GameCollectionActor : ReceiveActor
    {
        private IActorRef                       _dbReader;
        private IActorRef                       _dbWriter;
        private IActorRef                       _redisWriter;
        private Dictionary<GAMEID, IActorRef>   _dicGameLogicActors = new Dictionary<GAMEID, IActorRef>();
        private HashSet<string>                 _hashAllChildActors = new HashSet<string>();
        protected readonly ILoggingAdapter      _logger             = Logging.GetLogger(Context);

        public GameCollectionActor(IActorRef dbReader, IActorRef dbWriter, IActorRef redisWriter)
        {
            _dbReader    = dbReader;
            _dbWriter    = dbWriter;
            _redisWriter = redisWriter;

            Receive<EnterGameRequest>               (_ => onEnterGameUser(_));
            Receive<string>                         (_ => processCommand(_));
            Receive<PayoutConfigUpdated>            (_ => onPayoutConfigUpdated(_));
            Receive<Terminated>                     (_ => onTerminated(_));
            ReceiveAsync<LoadSpinDataRequest>       (onLoadSpinDatabase);
            ReceiveAsync<PerformanceTestRequest>    (onPerformanceTest);
        }

        protected override void PreStart()
        {
            base.PreStart();
            createGameLogicActors();
        }

        protected void createGameLogicActors()
        {
            #region JILI Game
            _dicGameLogicActors.Add(GAMEID.SAJ,               Context.ActorOf(Props.Create(()     => new SajGameLogic()),                   "saj"));
            _dicGameLogicActors.Add(GAMEID.HULLHOUSE,         Context.ActorOf(Props.Create(()     => new FullhouseGameLogic()),             "fullhouse"));
            _dicGameLogicActors.Add(GAMEID.HULLHOUSE3,        Context.ActorOf(Props.Create(()     => new Fullhouse3GameLogic()),            "fullhouse3"));
            _dicGameLogicActors.Add(GAMEID.WILDACE,           Context.ActorOf(Props.Create(()     => new WaGameLogic()),                    "wa"));
            _dicGameLogicActors.Add(GAMEID.OLS2,              Context.ActorOf(Props.Create(()     => new Ols2GameLogic()),                  "ols2"));
            _dicGameLogicActors.Add(GAMEID.FGP,               Context.ActorOf(Props.Create(()     => new FGPGameLogic()),                   "fgp"));
            _dicGameLogicActors.Add(GAMEID.FG3,               Context.ActorOf(Props.Create(()     => new FG3GameLogic()),                   "fg3"));
            _dicGameLogicActors.Add(GAMEID.MW4,               Context.ActorOf(Props.Create(()     => new MW4GameLogic()),                   "mw4"));
            _dicGameLogicActors.Add(GAMEID.PS,                Context.ActorOf(Props.Create(()     => new PSGameLogic()),                    "ps"));
            _dicGameLogicActors.Add(GAMEID.PIRATE,            Context.ActorOf(Props.Create(()     => new PriateGameLogic()),                "pirate"));



            #endregion

            foreach (KeyValuePair<GAMEID, IActorRef> pair in _dicGameLogicActors)
            {
                pair.Value.Tell(new DBProxyInform(_dbReader, _dbWriter, _redisWriter));
                pair.Value.Tell("loadSetting");
                _hashAllChildActors.Add(pair.Value.Path.Name);
                Context.Watch(pair.Value);
            }
        }

        private async Task onLoadSpinDatabase(LoadSpinDataRequest request)
        {
            try
            {
                var infoDocuments = await Context.System.ActorSelection("/user/spinDBReaders").Ask<List<BsonDocument>>(new ReadInfoCollectionRequest(), TimeSpan.FromSeconds(10.0));
                foreach (BsonDocument infoDocument in infoDocuments)
                {
                    string strGameName = (string)infoDocument["name"];
                    IActorRef gameActor = Context.Child(strGameName);
                    if (gameActor != ActorRefs.Nobody)
                        gameActor.Tell(infoDocument);
                    else
                        continue;
                }
                Sender.Tell(true);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in GameCollectionActor::onLoadSpinDatabase {0}", ex);
                Sender.Tell(false);
            }
        }

        private async Task onPerformanceTest(PerformanceTestRequest request)
        {
            foreach (KeyValuePair<GAMEID, IActorRef> pair in _dicGameLogicActors)
            {
                await _dicGameLogicActors[pair.Key].Ask<bool>(request);
            }

            Sender.Tell(true);
        }

        private void onTerminated(Terminated terminated)
        {
            _hashAllChildActors.Remove(terminated.ActorRef.Path.Name);
            if(_hashAllChildActors.Count == 0)
            {
                Self.Tell(PoisonPill.Instance);
            }
        }
        private void processCommand(string command)
        {
            if(command == "terminate")
            {
                foreach (KeyValuePair<GAMEID, IActorRef> pair in _dicGameLogicActors)
                    pair.Value.Tell(PoisonPill.Instance);
            }
        }
        private void onEnterGameUser(EnterGameRequest enterGameMessage)
        {
            GAMEID gameID = (GAMEID) enterGameMessage.GameID;
            if (!_dicGameLogicActors.ContainsKey(gameID))
            {
                Sender.Tell(new EnterGameResponse((int) gameID, Self, 1));  //해당 게임이 존재하지 않음
                return;
            }

            _dicGameLogicActors[gameID].Forward(enterGameMessage);
        }
        private void onPayoutConfigUpdated(PayoutConfigUpdated updated)
        {
            GAMEID gameID = (GAMEID)updated.GameID;
            if (!_dicGameLogicActors.ContainsKey(gameID))
                return;

            _dicGameLogicActors[gameID].Tell(updated);
        }
    }

    public class LoadSpinDataRequest
    {

    }

    public class PerformanceTestRequest
    {
        public PerformanceTestRequest()
        {
        }
    }
}
