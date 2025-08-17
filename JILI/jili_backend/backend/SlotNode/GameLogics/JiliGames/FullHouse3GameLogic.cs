using GITProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SlotGamesNode.GameLogics.JiliSpinClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlotGamesNode.GameLogics.JiliGames
{
    class Fullhouse3GameLogic : BaseJiliSlotGame
    {
        protected override string SymbolName
        {
            get
            {
                return "fullhouse3";
            }
        }

        protected override string GameID
        {
            get
            {
                return "403";
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

        public Fullhouse3GameLogic()
        {
            _gameID = GAMEID.HULLHOUSE3;
            GameName = "fullhouse3";
        }

        protected override BaseJiliSlotSpinResult calculateResult(BaseJiliSlotBetInfo betInfo, string strSpinResponse, bool isFirst, JiliFreeSpinInfo freeSpinInfo, double userBalance = 0, double winMoney = 0)
        {
            try
            {
                BaseJiliSlotSpinResult spinResult = new BaseJiliSlotSpinResult();

                FullHouseSpinClass spinResultJson = new FullHouseSpinClass();
                AllPlate allPlate = JsonConvert.DeserializeObject<AllPlate>(strSpinResponse);
                spinResultJson.AllPlate = allPlate;

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

                spinResultJson.TotalWin = winMoney;
                if (spinResultJson.TotalWin > 0)
                {

                }
                spinResultJson.FullJpWin = getRealWinByBet(spinResultJson.FullJpWin, betInfo.TotalBet);

                spinResultJson.PostMoney = userBalance - realBetMoney + winMoney;
                spinResultJson.PreMoney = userBalance;

                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.None
                };
                spinResult.TotalWin = spinResultJson.TotalWin;
                spinResult.ResultString = JsonConvert.SerializeObject(spinResultJson,settings);
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
