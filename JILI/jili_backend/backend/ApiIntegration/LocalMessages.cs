using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using GITProtocol;
using Akka.Routing;
using Newtonsoft.Json;

namespace ApiIntegration
{
    public enum LoginResult
    {
        OK                  = 1,
        ALREADYLOGGEDIN     = 3,
        IDPASSWORDMISMATCH  = 5,
        COUNTRYMISMATCH     = 6,
        ACCOUNTDISABLED     = 10,
        UNKNOWNERROR        = 11,
    }

    public class UserLoginResponse
    {
        public LoginResult  Result           { get; private set; }
        public long         UserDBID         { get; private set; }
        public string       UserID           { get; private set; }
        public double       UserBalance      { get; private set; }
        public UserLoginResponse()
        {

        }
        public UserLoginResponse(LoginResult result)
        {
            this.Result = result;
        }
        public UserLoginResponse(long userDBID, string strUserID, double userBalance)
        {
            this.Result             = LoginResult.OK;
            this.UserDBID           = userDBID;
            this.UserID             = strUserID;
            this.UserBalance        = userBalance;
        }
    }
    public class IUserLoginRequest
    {
        public string       UserID      { get; private set; }
        public IUserLoginRequest(string strUserID)
        {
            this.UserID     = strUserID;
        }
    }
}
