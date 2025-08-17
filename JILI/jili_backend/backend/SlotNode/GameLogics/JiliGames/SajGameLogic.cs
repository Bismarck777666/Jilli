using GITProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliGames
{
    class SajGameLogic : BaseJiliSlotGame
    {
        protected override string SymbolName
        {
            get
            {
                return "saj";
            }
        }

        protected override string GameID
        {
            get
            {
                return "409";
            }
        }

        protected override bool SupportPurchaseFree
        {
            get
            {
                return true;
            }
        }

        protected override double PurchaseFreeMultiple
        {
            get
            {
                return 41.5f;
            }
        }

        protected override bool HasPurEnableOption
        {
            get { return true; }
        }

        public SajGameLogic()
        {
            _gameID = GAMEID.SAJ;
            GameName = "saj";
        }

        protected override BaseJiliSlotSpinResult calculateResult(BaseJiliSlotBetInfo betInfo, string strSpinResponse, bool isFirst, JiliFreeSpinInfo freeSpinInfo, double userBalance = 0, double winMoney = 0)
        {
            try
            {
                BaseJiliSlotSpinResult spinResult = new BaseJiliSlotSpinResult();

                var spinResultJson = JsonConvert.DeserializeObject<SajSpinResultClass>(strSpinResponse);

                double realBetMoney = betInfo.TotalBet;

                if (SupportPurchaseFree && betInfo.PurchaseFree)
                    realBetMoney = realBetMoney * getPurchaseMultiple(betInfo);

                foreach (RoundQueueData item in spinResultJson.RoundQueue)
                {
                    item.RoundWin = getRealWinByBet(item.RoundWin, betInfo.TotalBet);
                    foreach (ComboStageData comboItem in item.ComboStageData)
                    {
                        comboItem.ComboStageWin = getRealWinByBet(comboItem.ComboStageWin, betInfo.TotalBet);
                        foreach (JObject awardItem in comboItem.AwardDataVec)
                        {
                            awardItem["Win"] = getRealWinByBet(awardItem["Win"].Value<double>(), betInfo.TotalBet);
                        }
                    }
                }

                spinResultJson.FreeTotalWin = getRealWinByBet(spinResultJson.FreeTotalWin, betInfo.TotalBet);
                spinResultJson.TotalWin = getRealWinByBet(spinResultJson.TotalWin, betInfo.TotalBet);

                spinResultJson.NowMoney = userBalance - realBetMoney + spinResultJson.TotalWin;
                spinResultJson.Bet = Math.Round(betInfo.TotalBet, 2);
                Random random = new Random();
                int index = random.Next(1, 10000);
                string historyIndex = index.ToString("D6");
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string showIndex = timestamp.ToString() + historyIndex + this.GameID;

                if (showIndex.Length == 19)
                {
                    string part1 = showIndex.Substring(0, 5);   // 18155
                    string part2 = showIndex.Substring(5, 6);   // 304040
                    string part3 = showIndex.Substring(11, 8);  // 05390409

                    spinResultJson.ShowIndex = $"{part1}-{part2}-{part3}";
                }

                spinResult.TotalWin = spinResultJson.TotalWin;

                if (spinResult.TotalWin > 0)
                {

                }

                spinResult.ResultString = JsonConvert.SerializeObject(spinResultJson);
                return spinResult;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occurred in BaseJiliSlotGame::calculateResult {0}", ex);
                return null;
            }
        }
    }
}
