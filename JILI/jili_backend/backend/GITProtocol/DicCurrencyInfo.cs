using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GITProtocol
{
    public enum Currencies
    {
        USD     = 0,    //미딸라
        EUR     = 1,    //유로
        TND     = 2,    //뛰니지 디나르
        TRY     = 3,    //터키리라
        MAD     = 4,    //모로코 디람
        BRL     = 5,    //브러질 리라
        RTS     = 6,    //리소스토큰우
        KWD     = 7,    //쿠웨이트디나르
        CHF     = 8,    //스위스프랑
        EGP     = 9,    //에집트파운드
        PEN     = 10,   //페루솔
        AED     = 11,   //아랍추장국 디나르
        GBP     = 12,   //브리튼 파운드
        BOB     = 13,   //볼리비아 보리비아노
        NGN     = 14,   //나이제리아 나와라
        ZMW     = 15,   //잠비아 크와싸
        ARS     = 16,   //아르헨띠나 페소
        COP     = 17,   //콜롬비아 페소
        CRC     = 18,   //코스타리카 콜론
        DZD     = 19,   //알제리디나르
        XAF     = 20,   //중앙아프리카 프랑
        XOF     = 21,   //서아프리카 프랑
        GHS     = 22,   //가나이안 세디
        GNF     = 23,   //귀니안 프랑
        KES     = 24,   //케니아 쉴링
        RWF     = 25,   //르완다 프랑
        SYP     = 26,   //스위스 프랑
        IQD     = 27,   //이라크 디나르
        UAH     = 28,   //우크라이나 후르브니아
        RUB     = 29,   //러시아 루블
        KQD     = 30,   //쿠웨이트 디나르
        IDR     = 31,   //인도네시알 루피
        ZWL     = 32,   //짐바브웨 달러
        ZAR     = 33,   //남아프리카 랜드
        NAD     = 34,   //나미비아 달러
        AOA     = 35,   //앙골라 콴자
        MZN     = 36,   // 모잠비크 메티컬
        AUD     = 37,   //오스트랄리아 달러
        CAD     = 38,   //캐너더 달러
        NZD     = 39,   //뉴질랜드 달러
        LBP     = 40,   //레바논 파운드
        COUNT   = 41,
    }

    public class CurrencyObj
    {
        public string   CurrencyText    { get; set; }
        public string   CurrencySymbol  { get; set; }
        public int      Rate            { get; set; }
    }

    public class DicCurrencyInfo
    {
        private static  DicCurrencyInfo _sInstance  = new DicCurrencyInfo();
        public static   DicCurrencyInfo Instance    => _sInstance;

        public Dictionary<Currencies, CurrencyObj> _currencyInfo = new Dictionary<Currencies, CurrencyObj>()
        {
            { Currencies.USD, new CurrencyObj(){ CurrencyText = "USD",CurrencySymbol = "$",     Rate = 1        } },
            { Currencies.EUR, new CurrencyObj(){ CurrencyText = "EUR",CurrencySymbol = "€",     Rate = 1        } },
            { Currencies.TND, new CurrencyObj(){ CurrencyText = "TND",CurrencySymbol = "TND",   Rate = 1        } },
            { Currencies.TRY, new CurrencyObj(){ CurrencyText = "TRY",CurrencySymbol = "₺",     Rate = 10       } },
            { Currencies.MAD, new CurrencyObj(){ CurrencyText = "MAD",CurrencySymbol = "MAD",   Rate = 10       } },
            { Currencies.BRL, new CurrencyObj(){ CurrencyText = "BRL",CurrencySymbol = "R$",    Rate = 1        } },
            { Currencies.RTS, new CurrencyObj(){ CurrencyText = "RTS",CurrencySymbol = "RT",    Rate = 1        } },
            { Currencies.KWD, new CurrencyObj(){ CurrencyText = "KWD",CurrencySymbol = "KWD",   Rate = 1        } },
            { Currencies.CHF, new CurrencyObj(){ CurrencyText = "CHF",CurrencySymbol = "CHF",   Rate = 1        } },
            { Currencies.EGP, new CurrencyObj(){ CurrencyText = "EGP",CurrencySymbol = "EGP",   Rate = 10       } },
            { Currencies.PEN, new CurrencyObj(){ CurrencyText = "PEN",CurrencySymbol = "PEN",   Rate = 1        } },
            { Currencies.AED, new CurrencyObj(){ CurrencyText = "AED",CurrencySymbol = "AED",   Rate = 1        } },
            { Currencies.GBP, new CurrencyObj(){ CurrencyText = "GBP",CurrencySymbol = "GBP",   Rate = 1        } },
            { Currencies.BOB, new CurrencyObj(){ CurrencyText = "BOB",CurrencySymbol = "BOB",   Rate = 1        } },
            { Currencies.NGN, new CurrencyObj(){ CurrencyText = "NGN",CurrencySymbol = "NGN",   Rate = 1000     } },
            { Currencies.ZMW, new CurrencyObj(){ CurrencyText = "ZMW",CurrencySymbol = "ZMW",   Rate = 1        } },
            { Currencies.ARS, new CurrencyObj(){ CurrencyText = "ARS",CurrencySymbol = "ARS",   Rate = 100      } },
            { Currencies.COP, new CurrencyObj(){ CurrencyText = "COP",CurrencySymbol = "COP",   Rate = 1000     } },
            { Currencies.CRC, new CurrencyObj(){ CurrencyText = "CRC",CurrencySymbol = "CRC",   Rate = 500      } },
            { Currencies.DZD, new CurrencyObj(){ CurrencyText = "DZD",CurrencySymbol = "DZD",   Rate = 100      } },
            { Currencies.XAF, new CurrencyObj(){ CurrencyText = "XAF",CurrencySymbol = "XAF",   Rate = 500      } },
            { Currencies.XOF, new CurrencyObj(){ CurrencyText = "XOF",CurrencySymbol = "XOF",   Rate = 500      } },
            { Currencies.GHS, new CurrencyObj(){ CurrencyText = "GHS",CurrencySymbol = "GHS",   Rate = 10       } },
            { Currencies.GNF, new CurrencyObj(){ CurrencyText = "GNF",CurrencySymbol = "GNF",   Rate = 1000     } },
            { Currencies.KES, new CurrencyObj(){ CurrencyText = "KES",CurrencySymbol = "KES",   Rate = 100      } },
            { Currencies.RWF, new CurrencyObj(){ CurrencyText = "RWF",CurrencySymbol = "RWF",   Rate = 1000     } },
            { Currencies.SYP, new CurrencyObj(){ CurrencyText = "SYP",CurrencySymbol = "SYP",   Rate = 10       } },
            { Currencies.IQD, new CurrencyObj(){ CurrencyText = "IQD",CurrencySymbol = "IQD",   Rate = 1000     } },
            { Currencies.UAH, new CurrencyObj(){ CurrencyText = "UAH",CurrencySymbol = "UAH",   Rate = 10       } },
            { Currencies.RUB, new CurrencyObj(){ CurrencyText = "RUB",CurrencySymbol = "RUB",   Rate = 1        } },
            { Currencies.KQD, new CurrencyObj(){ CurrencyText = "KQD",CurrencySymbol = "KQD",   Rate = 1        } },
            { Currencies.IDR, new CurrencyObj(){ CurrencyText = "IDR",CurrencySymbol = "IDR",   Rate = 1000     } },
            { Currencies.ZWL, new CurrencyObj(){ CurrencyText = "ZWL",CurrencySymbol = "ZWL",   Rate = 1        } },
            { Currencies.ZAR, new CurrencyObj(){ CurrencyText = "ZAR",CurrencySymbol = "R",     Rate = 1        } },
            { Currencies.NAD, new CurrencyObj(){ CurrencyText = "NAD",CurrencySymbol = "NAD",   Rate = 1        } },
            { Currencies.AOA, new CurrencyObj(){ CurrencyText = "AOA",CurrencySymbol = "AOA",   Rate = 10       } },
            { Currencies.MZN, new CurrencyObj(){ CurrencyText = "MZN",CurrencySymbol = "MZN",   Rate = 1        } },
            { Currencies.AUD, new CurrencyObj(){ CurrencyText = "AUD",CurrencySymbol = "$",     Rate = 1        } },
            { Currencies.CAD, new CurrencyObj(){ CurrencyText = "CAD",CurrencySymbol = "$",     Rate = 1        } },
            { Currencies.NZD, new CurrencyObj(){ CurrencyText = "NZD",CurrencySymbol = "$",     Rate = 1        } },
            { Currencies.LBP, new CurrencyObj(){ CurrencyText = "LBP",CurrencySymbol = "LBP",   Rate = 10000    } },
        };
    }
}

    
