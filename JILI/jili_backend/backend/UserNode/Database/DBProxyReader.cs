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

namespace UserNode.Database
{
    public class DBProxyReader : ReceiveActor
    {
        private readonly ILoggingAdapter _logger                = Logging.GetLogger(Context);
        private string                   _strConnString         = "";
        public DBProxyReader(string strConnString)
        {
            _strConnString = strConnString;

            //유저로그인 요청을 처리한다.
            ReceiveAsync<UserOfflineStateItem>              (onUserOfflineUpdate);
        }

        public static Props Props(string strConnString, int poolSize)
        {
            return Akka.Actor.Props.Create(() => new DBProxyReader(strConnString)).WithRouter(new RoundRobinPool(poolSize));
        }

        private async Task onUserOfflineUpdate(UserOfflineStateItem request)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();

                    string strQuery = "UPDATE users SET balance=balance+@balanceupdate, isonline=0, lastgameid=@lastgameid WHERE id=@id";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@id", request.PlayerID);
                    command.Parameters.AddWithValue("@balanceupdate", request.BalanceIncrement);
                    command.Parameters.AddWithValue("@lastgameid", request.GameID);

                    await command.ExecuteNonQueryAsync();

                    Sender.Tell(true);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBProxyReader::onUserOfflineUpdate {0}", ex);
            }
            Sender.Tell(false);
        }
    }
}
