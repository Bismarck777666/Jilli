using GITProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SlotGamesNode.GameLogics.JiliSpinClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliGames
{
    class FullhouseGameLogic : BaseJiliSlotGame
    {
        protected override string SymbolName
        {
            get
            {
                return "fullhouse";
            }
        }

        protected override string GameID
        {
            get
            {
                return "49";
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

        public FullhouseGameLogic()
        {
            _gameID = GAMEID.HULLHOUSE;
            GameName = "fullhouse";
        }

        protected override BaseJiliSlotSpinResult calculateResult(BaseJiliSlotBetInfo betInfo, string strSpinResponse, bool isFirst, JiliFreeSpinInfo freeSpinInfo, double userBalance = 0, double winMoney = 0)
        {
            try
            {
                BaseJiliSlotSpinResult spinResult = new BaseJiliSlotSpinResult();

                var spinResultJson = JsonConvert.DeserializeObject<FullHouseSpinClass>(strSpinResponse);

                double realBetMoney = betInfo.TotalBet;

                if (SupportPurchaseFree && betInfo.PurchaseFree)
                    realBetMoney = realBetMoney * getPurchaseMultiple(betInfo);

                foreach (Plate item in spinResultJson.AllPlate.Plate)
                {
                    item.Win = getRealWinByBet(item.Win, betInfo.TotalBet);
                    item.BonusWin = getRealWinByBet(item.BonusWin, betInfo.TotalBet);
                    if (item.Combo != null && item.Combo.Count > 0)
                    {
                        foreach (Combo comboItem in item.Combo)
                        {
                            comboItem.Win = getRealWinByBet(comboItem.Win, betInfo.TotalBet);
                            if (comboItem.Award != null && comboItem.Award.Count > 0)
                            {
                                foreach (AwardReel awardItem in comboItem.Award)
                                {
                                    awardItem.Win = getRealWinByBet(awardItem.Win, betInfo.TotalBet);
                                }
                            }
                        }
                    }
                }
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Random random = new Random();
                int index = random.Next(1, 9999);
                spinResultJson.RoundIndex = (ulong)(timestamp * 1000000 + index * 1000 + int.Parse(this.GameID));
                spinResultJson.TotalWin = getRealWinByBet(spinResultJson.TotalWin, betInfo.TotalBet);
                if (spinResultJson.TotalWin > 0)
                {

                }
                spinResultJson.FullJpWin = getRealWinByBet(spinResultJson.FullJpWin, betInfo.TotalBet);

                spinResultJson.PostMoney = userBalance - realBetMoney + spinResultJson.TotalWin;
                spinResultJson.PreMoney = userBalance;

                spinResult.TotalWin = spinResultJson.TotalWin;

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
