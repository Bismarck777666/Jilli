using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GITProtocol
{
    public enum GameProviders
    {
        NONE        = 0,
        PP          = 1,
        BNG         = 2,
        CQ9         = 3,
        HABANERO    = 4,
        PLAYSON     = 5,
        AMATIC      = 6,
        JILI        = 7,
        COUNT       = 7,
    }

    public enum GAMEID : ushort
    {

        #region JILI games
        SAJ                     = 7001,
        HULLHOUSE               = 7002,
        WILDACE                 = 7003,
        HULLHOUSE3              = 7004,
        OLS2                    = 7005,
        FGP                     = 7006,
        FG3                     = 7007,
        MW4                     = 7008,
        PS                      = 7009,
        PIRATE                  = 7010
        #endregion
    }

    public enum CSMSG_CODE : ushort
    {
        CS_HEARTBEAT            = 0,
        CS_LOGIN                = 1,
        CS_ENTERGAME            = 18,
        CS_FORCEOUTUSER         = 26,   //유저강퇴

        CS_JILI_DOINIT = 2000,
        CS_JILI_DOSPIN = 2001,
        CS_JILI_DOCOLLECT = 2002,
        CS_JILI_RELOADBALANCE = 2003,
        CS_JILI_NOTPROCDRESULT = 2004,
        CS_JILI_SAVESETTING = 2005,
        CS_JILI_DOBONUS = 2006,
        CS_JILI_DOMYSTERYSCATTER = 2007,
        CS_JILI_FSOPTION = 2008,
        CS_JILI_DOCOLLECTBONUS = 2009,
        CS_JILI_DOFSBONUS = 2010,
        CS_JILI_DOGAMBLEOPTION = 2011,
        CS_JILI_DOGAMBLE = 2012,
        CS_SLOTGAMEEND = 2012,

        CS_JILI_PROMOSTART = 2020,
        CS_JILI_PROMOACTIVE = 2020,
        CS_JILI_PROMOTOURDETAIL = 2021,
        CS_JILI_PROMORACEDETAIL = 2022,
        CS_JILI_PROMOTOURLEADER = 2023,
        CS_JILI_PROMORACEPRIZES = 2024,
        CS_JILI_PROMORACEWINNER = 2025,
        CS_JILI_PROMOV3TOURLEADER = 2026,
        CS_JILI_PROMOV2RACEWINNER = 2027,
        CS_JILI_PROMOTOUROPTIN = 2028,
        CS_JILI_PROMORACEOPTIN = 2029,
        CS_JILI_PROMOSCORES = 2030,
        CS_JILI_GETMINILOBBY = 2031,
        CS_JILI_PROMOV3RACEWINNER = 2032,
        CS_JILI_PROMOEND = 2032,

        CS_JILI_NOTENOUGHMONEY = 9999
    }

    public enum SCMSG_CODE : ushort
    {
        SC_LOGIN = 1,
        SC_ENTERGAME = 18,
        SC_JILI_DOINIT = 2000,
        SC_JILI_DOSPIN = 2001,
        SC_JILI_DOCOLLECT = 2002,
        SC_JILI_RELOADBALANCE = 2003,
        SC_JILI_SAVESETTING = 2004,
        SC_JILI_DOBONUS = 2005,
        SC_JILI_DOMYSTERYSCATTER = 2006,
        SC_JILI_DOCOLLECTBONUS = 2007,
        SC_JILI_DOFSBONUS = 2008,
        SC_JILI_DOGAMBLE = 2009,
        SC_JILI_DOGAMBLEOPTION = 2010,

        SC_JILI_PROMOACTIVE = 2020,
        SC_JILI_PROMOTOURDETAIL = 2021,
        SC_JILI_PROMORACEDETAIL = 2022,
        SC_JILI_PROMOTOURLEADER = 2023,
        SC_JILI_PROMORACEPRIZES = 2024,
        SC_JILI_PROMORACEWINNER = 2025,
        SC_JILI_PROMOTOUROPTIN = 2026,
        SC_JILI_PROMORACEOPTIN = 2027,
        SC_JILI_PROMOTOURSCORE = 2028,
        SC_JILI_GETMINILOBBY = 2029,
    }
}
