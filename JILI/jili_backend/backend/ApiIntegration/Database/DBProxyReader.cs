using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using System.Data.Common;
using Akka.Routing;
using GITProtocol;
using Akka.Event;
using System.Data.SqlClient;
using System.Drawing.Imaging;

namespace ApiIntegration.Database
{
    public class DBProxyReader : ReceiveActor
    {
        private readonly ILoggingAdapter _logger                = Logging.GetLogger(Context);
        private string                   _strConnString         = "";
        public DBProxyReader(string strConnString)
        {
            _strConnString = strConnString;

            //유저로그인 요청을 처리한다.
            ReceiveAsync<IUserLoginRequest>                  (doLoginRequest);
        }
        public static Props Props(string strConnString, int poolSize)
        {
            return Akka.Actor.Props.Create(() => new DBProxyReader(strConnString)).WithRouter(new RoundRobinPool(poolSize));
        }        
        private async Task doLoginRequest(IUserLoginRequest request)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();
                    string      strQuery    = "SELECT * FROM users WHERE username=@username";
                    SqlCommand  command     = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@username", request.UserID);

                    UserLoginResponse response = null;
                    LoginResult resultCode = LoginResult.IDPASSWORDMISMATCH;

                    long    dbID                = 0;
                    string  strUserID           = "";
                    double  balance             = 0.0;
                    do
                    {
                        using (DbDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (!await reader.ReadAsync())
                                break;

                            dbID             = (long)                reader["id"];
                            strUserID        = (string)              reader["username"];                         
                            balance          = (double) (decimal)    reader["balance"];

                            int state = (int)reader["state"];
                            if (state != 1)
                            {
                                resultCode = LoginResult.ACCOUNTDISABLED;
                                break;
                            }
                        }
                        response = new UserLoginResponse(dbID, strUserID, balance);
                    } while (false);

                    if (response == null)
                        response = new UserLoginResponse(resultCode);

                    //응답을 보낸다.
                    Sender.Tell(response);
                }
            }
            catch(Exception ex)
            {
                _logger.Error("Exception has been occured in DBProxyReader::doLoginRequest {0}", ex);
            }
        }      
    }
}
