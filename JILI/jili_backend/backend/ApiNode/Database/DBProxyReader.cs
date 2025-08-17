using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Security.Cryptography;
using GITProtocol;
using System.Reflection;
using QueenApiNode.HttpService;
using Newtonsoft.Json;

namespace QueenApiNode.Database
{
    public class DBProxyReader : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Logging.GetLogger(Context);
        private string  _strConnString = "";
        public DBProxyReader(string strConnString)
        {
            _strConnString = strConnString;
            ReceiveAsync<DBCreateUserRequest>       (onCreateUserRequest);
            ReceiveAsync<DBGetUserInfoRequest>      (onGetUserInfoRequest);
            ReceiveAsync<DBGetAgentInfoRequest>     (onGetAgentInfo);
            ReceiveAsync<DBGetAgentInfoByIDRequest> (onGetAgentInfoByID);
        }
        public static Props Props(string strConnString, int poolSize)
        {
            return Akka.Actor.Props.Create(() => new DBProxyReader(strConnString)).WithRouter(new RoundRobinPool(poolSize));
        }
        private async Task onGetAgentInfo(DBGetAgentInfoRequest request)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();
                    string strQuery = "SELECT id, username, apitoken, state, currency, language FROM agents WHERE username=@username";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@username", request.AgentID);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (reader["apitoken"] is DBNull || (int) reader["state"] == 0)
                            {
                                Sender.Tell(new DBAgentInfoResponse(0, null, null, 0, 0));
                            }
                            else
                            {
                                DBAgentInfoResponse response = new DBAgentInfoResponse((int)reader["id"], (string)reader["username"],
                                    (string)reader["apitoken"], (int)reader["currency"], (int)reader["language"]);
                                Sender.Tell(response);
                            }
                        }
                        else
                        {
                            Sender.Tell(new DBAgentInfoResponse(0, null, null, 0, 0));
                        }
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBProxyReader::onGetWebsiteInfo {0}", ex);
            }
            Sender.Tell(null);
        }
        private async Task onGetAgentInfoByID(DBGetAgentInfoByIDRequest request)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();
                    string strQuery = "SELECT id, username, apitoken, state, currency, language FROM agents WHERE id=@id";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@id", request.AgentID);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (reader["apitoken"] is DBNull || (int)reader["state"] == 0)
                            {
                                Sender.Tell(new DBAgentInfoResponse(0, null, null, 0, 0));
                            }
                            else
                            {
                                DBAgentInfoResponse response = new DBAgentInfoResponse((int)reader["id"], (string)reader["username"],
                                    (string)reader["apitoken"], (int)reader["currency"], (int)reader["language"]);
                                Sender.Tell(response);
                            }
                        }
                        else
                        {
                            Sender.Tell(new DBAgentInfoResponse(0, null, null, 0, 0));
                        }
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBProxyReader::onGetWebsiteInfo {0}", ex);
            }
            Sender.Tell(null);
        }
        private async Task onCreateUserRequest(DBCreateUserRequest request)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();
                    string strQuery = "INSERT INTO users (agentid, username, password, agentname, balance, currency, updatetime, registertime) VALUES " +
                        "(@agentid, @username, @password, @agentname, @balance, @currency, @updatetime, @registertime)";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@agentid",     request.AgentDBID);
                    command.Parameters.AddWithValue("@username",    request.UserName);
                    command.Parameters.AddWithValue("@password",    request.Password);
                    command.Parameters.AddWithValue("@agentname",   request.AgentID);
                    command.Parameters.AddWithValue("@balance",     0.0);
                    command.Parameters.AddWithValue("@currency",    (int) request.Currency);
                    command.Parameters.AddWithValue("@updatetime",  DateTime.Now);
                    command.Parameters.AddWithValue("@registertime",DateTime.Now);
                    await command.ExecuteNonQueryAsync();
                    Sender.Tell(true);
                    return;
                }
            }
            catch (Exception)
            {
            }
            Sender.Tell(false);
        }
        private async Task onGetUserInfoRequest(DBGetUserInfoRequest request)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_strConnString))
                {
                    await connection.OpenAsync();
                    string strQuery = "SELECT username, password, state FROM users WHERE agentid=@agentid and username=@username";
                    SqlCommand command = new SqlCommand(strQuery, connection);
                    command.Parameters.AddWithValue("@agentid",  request.AgentID);
                    command.Parameters.AddWithValue("@username", request.UserName);
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Sender.Tell(new DBGetUserInfoResponse((string)reader["username"], (string)reader["password"],
                                ((int)reader["state"] == 1)));
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception has been occured in DBProxyReader::onGetUserInfoRequest {0}", ex);
            }
            Sender.Tell(null);
        }
    }

    public class DBCreateUserRequest
    {
        public string       UserName    { get; private set; }
        public string       Password    { get; private set; }
        public int          AgentDBID   { get; private set; }
        public string       AgentID     { get; private set; }
        public Currencies   Currency    { get; private set; }
        public Languages    Language    { get; private set; }
        public DBCreateUserRequest(string strUserName, string strPassword, int agentDBID, string strAgentID, Currencies currency, Languages langugage)
        {
            this.UserName   = strUserName;
            this.Password   = strPassword;
            this.AgentDBID  = agentDBID;
            this.AgentID    = strAgentID;
            this.Currency   = currency;
            this.Language   = langugage;
        }
    }
    public class DBGetUserInfoRequest
    {
        public int      AgentID     { get; private set; }
        public string   UserName    { get; private set; }
        public DBGetUserInfoRequest(int agentID, string strUserName)
        {
            this.AgentID    = agentID;
            this.UserName   = strUserName;
        }
    }
    public class DBGetUserInfoResponse
    {
        public string   UserName    { get; private set; }
        public string   Password    { get; private set; }
        public bool     IsEnabled   { get; private set; }
        public DBGetUserInfoResponse(string strUserName, string strPassword, bool isEnabeld)
        {
            this.UserName   = strUserName;
            this.Password   = strPassword;
            this.IsEnabled  = isEnabeld;
        }
    }
    public class ApiProvider
    {
        public string vendorid  { get; set; }
        public string name      { get; set; }
        public string category  { get; set; }
    }
    public class ApiGame
    {
        public string vendorid  { get; set; }
        public int    gameid    { get; set; }
        public string name      { get; set; }
        public string symbol    { get; set; }
        public string iconurl1  { get; set; }
        public string iconurl2  { get; set; }
    }
    public class DBGetAgentInfoRequest
    {
        public string AgentID { get; private set; }

        public DBGetAgentInfoRequest(string strAgentID)
        {
            this.AgentID = strAgentID;
        }
    }
    public class DBGetAgentInfoByIDRequest
    {
        public int AgentID { get; private set; }

        public DBGetAgentInfoByIDRequest(int agentID)
        {
            this.AgentID = agentID;
        }
    }
    public class DBAgentInfoResponse
    {
        public string   AgentID     { get; private set; }
        public int      AgentDBID   { get; private set; }
        public string   AuthToken   { get; private set; }
        public int      Currency    { get; private set; }
        public int      Language    { get; private set; }
        public DBAgentInfoResponse(int dbID, string strAgentID, string authToken, int currency, int language)
        {
            this.AgentDBID  = dbID;
            this.AgentID    = strAgentID;
            this.AuthToken  = authToken;
            this.Currency   = currency;
            this.Language   = language;
        }
    }
}
