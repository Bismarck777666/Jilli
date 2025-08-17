using GITProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserNode.Database
{
    public class ReportSnapshot
    {
        private Dictionary<string,  ReportUpdateItem>    _dicReportUpdateItems       = new Dictionary<string, ReportUpdateItem>();
        private Dictionary<string,  AgentPointAdded>     _dicAgentPointAdds          = new Dictionary<string, AgentPointAdded>();
        private Dictionary<string,  GameReportItem>      _dicGameReports             = new Dictionary<string, GameReportItem>();
        private Dictionary<long,    UserRollPointAdded>  _dicUserPointAdds           = new Dictionary<long, UserRollPointAdded>();
        
        private static ReportSnapshot _sInstance = new ReportSnapshot();
        public static ReportSnapshot Instance
        {
            get
            {
                return _sInstance;
            }
        }

        public void updateGameReport(GameReportItem reportItem)
        {
            string strKey = string.Format("{0}_{1}_{2}",  reportItem.GameID, reportItem.AgentID, reportItem.ReportDate.ToString("yyyyMMddHHmm"));
            if (_dicGameReports.ContainsKey(strKey))
                _dicGameReports[strKey].mergeReport(reportItem);
            else
                _dicGameReports[strKey] = reportItem;

        }

        public List<GameReportItem> PopGameReportUpdates(int maxCount = 5000)
        {
            if (_dicGameReports.Count == 0)
                return null;
            
            List<GameReportItem> reportUpdates = new List<GameReportItem>();
            foreach (KeyValuePair<string, GameReportItem> pair in _dicGameReports)
            {
                reportUpdates.Add(pair.Value);
                if (reportUpdates.Count >= maxCount)
                    break;
            }
            for (int i = 0; i < reportUpdates.Count; i++)
            {
                string strKey = string.Format("{0}_{1}_{2}", reportUpdates[i].GameID, reportUpdates[i].AgentID, reportUpdates[i].ReportDate.ToString("yyyyMMddHHmm"));
                _dicGameReports.Remove(strKey);
            }
            return reportUpdates;
        }
        public void PushGameReportUpdates(List<GameReportItem> reportUpdateItems)
        {
            for (int i = 0; i < reportUpdateItems.Count; i++)
            {
                string strKey = string.Format("{0}_{1}_{2}", reportUpdateItems[i].GameID, reportUpdateItems[i].AgentID, reportUpdateItems[i].ReportDate.ToString("yyyyMMddHHmm"));
                if (_dicGameReports.ContainsKey(strKey))
                    _dicGameReports[strKey].mergeReport(reportUpdateItems[i]);
                else
                    _dicGameReports[strKey] = reportUpdateItems[i];
            }
        }
        public void updateReports(ReportUpdateItem reportUpdateItem)
        {
            string strKey = string.Format("{0}_{1}", reportUpdateItem.UserID, reportUpdateItem.ReportDateTime.ToString("yyyyMMddHHmm"));
            if (_dicReportUpdateItems.ContainsKey(strKey))
                _dicReportUpdateItems[strKey].mergeReport(reportUpdateItem);
            else
                _dicReportUpdateItems[strKey] = reportUpdateItem;
        }

        public List<ReportUpdateItem> PopReportUpdates(int maxCount = 5000)
        {
            if (_dicReportUpdateItems.Count == 0)
                return null;

            List<ReportUpdateItem> reportUpdates = new List<ReportUpdateItem>();
            foreach (KeyValuePair<string, ReportUpdateItem> pair in _dicReportUpdateItems)
            {                
                reportUpdates.Add(pair.Value);
                if (reportUpdates.Count >= maxCount)
                    break;
            }
            for (int i = 0; i < reportUpdates.Count; i++)
            {
                string strKey = string.Format("{0}_{1}", reportUpdates[i].UserID,reportUpdates[i].ReportDateTime.ToString("yyyyMMddHHmm"));
                _dicReportUpdateItems.Remove(strKey);
            }
            return reportUpdates;
        }
        public void PushReportUpdateItems(List<ReportUpdateItem> reportUpdateItems)
        {
            for (int i = 0; i < reportUpdateItems.Count; i++)
            {
                string strKey = string.Format("{0}_{1}", reportUpdateItems[i].UserID, reportUpdateItems[i].ReportDateTime.ToString("yyyyMMddHHmm"));
                if (_dicReportUpdateItems.ContainsKey(strKey))
                    _dicReportUpdateItems[strKey].mergeReport(reportUpdateItems[i]);
                else
                    _dicReportUpdateItems[strKey] = reportUpdateItems[i];
            }
        }
        

        public void addUserPoint(UserRollPointAdded userRollPointAdded)
        {
            if (_dicUserPointAdds.ContainsKey(userRollPointAdded.UserDBID))
                _dicUserPointAdds[userRollPointAdded.UserDBID].merge(userRollPointAdded);
            else
                _dicUserPointAdds[userRollPointAdded.UserDBID] = userRollPointAdded;
        }

        public List<UserRollPointAdded> PopUserPointAdds(int count = 5000)
        {
            if (_dicUserPointAdds.Count == 0)
                return null;

            List<UserRollPointAdded> userPointAdds = new List<UserRollPointAdded>();
            foreach (KeyValuePair<long, UserRollPointAdded> pair in _dicUserPointAdds)
            {
                userPointAdds.Add(pair.Value);
                if (userPointAdds.Count >= count)
                    break;
            }
            for (int i = 0; i < userPointAdds.Count; i++)
                _dicUserPointAdds.Remove(userPointAdds[i].UserDBID);
            return userPointAdds;
        }

        public void PushUserPointAdds(List<UserRollPointAdded> userRollingAdds)
        {
            for (int i = 0; i < userRollingAdds.Count; i++)
                addUserPoint(userRollingAdds[i]);
        }
        public void addAgentPoints(AgentPointAdded agentPointAdded)
        {
            string strKey = string.Format("{0}_{1}", agentPointAdded.AgentDBID, (int)agentPointAdded.PointType);
            if (_dicAgentPointAdds.ContainsKey(strKey))
                _dicAgentPointAdds[strKey].merge(agentPointAdded);
            else
                _dicAgentPointAdds[strKey] = agentPointAdded;
        }

        public List<AgentPointAdded> PopAgentPointAdds(int count = 5000)
        {
            if (_dicAgentPointAdds.Count == 0)
                return null;

            List<AgentPointAdded> agentPointAdds = new List<AgentPointAdded>();
            foreach (KeyValuePair<string, AgentPointAdded> pair in _dicAgentPointAdds)
            {
                agentPointAdds.Add(pair.Value);
                if (agentPointAdds.Count >= count)
                    break;
            }
            for (int i = 0; i < agentPointAdds.Count; i++)
                _dicAgentPointAdds.Remove(string.Format("{0}_{1}", agentPointAdds[i].AgentDBID, (int)agentPointAdds[i].PointType));

            return agentPointAdds;
        }

        public void PushAgentPointAdds(List<AgentPointAdded> agentRollingAdds)
        {
            for (int i = 0; i < agentRollingAdds.Count; i++)
                addAgentPoints(agentRollingAdds[i]);
        }


    }
    public enum PointTypes
    {
        Rolling = 0,
        Losing  = 1,
    }
    public class UserRollPointAdded
    {
        public long     UserDBID { get; private set; }
        public double   IncPoint { get; private set;}

        public UserRollPointAdded(long userDBID, double incPoint)
        {
            UserDBID = userDBID;
            IncPoint = incPoint;
        }
        public void merge(UserRollPointAdded other)
        {
            this.IncPoint += other.IncPoint;
        }
    }
    public class AgentPointAdded
    {
        public int          AgentDBID   { get; private set; }        
        public double       IncPoint    { get; private set; }
        public PointTypes   PointType   { get; private set; }

        public AgentPointAdded(int agentDBID, double incPoint, PointTypes pointType)
        {
            this.AgentDBID  = agentDBID;
            this.IncPoint   = incPoint;
            this.PointType  = pointType;
        }

        public void merge(AgentPointAdded other)
        {
            this.IncPoint += other.IncPoint;
        }
    }

    public class GameReportItem
    {
        public int          GameID      { get; private set; }
        public int          AgentID     { get; private set; }
        public double       BetMoney    { get; private set; }
        public double       WinMoney    { get; private set; }
        public DateTime     ReportDate  { get; private set; }

        public GameReportItem(int gameID, int agentID, double betMoney, double winMoney, DateTime reportDate)
        {
            this.GameID     = gameID;
            this.AgentID    = agentID;
            this.BetMoney   = betMoney;
            this.WinMoney   = winMoney;
            this.ReportDate = reportDate;
        }

        public void mergeReport(GameReportItem item)
        {
            this.BetMoney += item.BetMoney;
            this.WinMoney += item.WinMoney;
        }
    }
    public class ReportUpdateItem
    {
        public ReportUpdateItem(string strUserID, int agentID, DateTime reportTime, double bet, double win)
        {
            this.UserID         = strUserID;
            this.ReportDateTime = reportTime;
            this.Bet            = bet;
            this.Win            = win;
            this.AgentID        = agentID;
        }
        public void mergeReport(ReportUpdateItem other)
        {
            this.Bet        += other.Bet;
            this.Win        += other.Win;
        }       
        public string       UserID          { get; private set; }
        public DateTime     ReportDateTime  { get; private set; }
        public double       Bet             { get; private set; }
        public double       Win             { get; private set; }
        public int          AgentID         { get; private set; }
    }
    
}
