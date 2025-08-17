using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontNode.HTTPService.Models
{
    public class AuthResponse
    {
        public string result { get; set; }
        public string sessiontoken { get; set; }
        public string currency { get; set; }
        public string gamename { get; set; }
        public string gamedata { get; set; }
        public AuthResponse(string strResult, string strSessionToken, string currency, string gamename, string gamedata)
        {
            this.result = strResult;
            this.sessiontoken = strSessionToken;
            this.currency = currency;
            this.gamename = gamename;
            this.gamedata = gamedata;
        }
    }
}
