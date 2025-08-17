using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using PCGSharp;

namespace SlotGamesNode.GameLogics
{
    public enum SpinTypes
    {
        NORMAL = 0,
        FREE = 1,
        FREELAST = 2,
    }

    public enum ActionTypes
    {
        NONE = 0,
        DOSPIN = 1,
        DOCOLLECT = 2,
        DOBONUS = 3,
        DOCOLLECTBONUS = 4,
        DOFSOPTION = 5,
        DOMYSTERY = 6,
        DOFSBONUS = 7,
        DOGAMBLEOPTION = 8,
        DOGAMBLE = 9,
    }
    public class BaseJiliSlotSpinData
    {
        public double SpinOdd { get; set; }
        public int SpinType { get; set; } //0:일반스핀, 1:시작프리스핀 + 프리스핀, 100:시작프리스핀, 200, 201, 202...: 그에 상응한 프리스핀조합 
        public List<string> SpinStrings { get; set; }
        public bool IsEvent { get; set; }

        public BaseJiliSlotSpinData()
        {
            this.IsEvent = false;
        }

        public BaseJiliSlotSpinData(int spinType, double spinOdd, List<string> spinStrings)
        {
            this.SpinType = spinType;
            this.SpinOdd = spinOdd;
            this.SpinStrings = spinStrings;
            this.IsEvent = false;
        }

        public virtual void SerializeTo(BinaryWriter writer)
        {
            writer.Write(this.SpinType);
            writer.Write(this.SpinOdd);
            writer.Write(this.IsEvent);
            SerializeUtils.writeStringList(writer, this.SpinStrings);
        }
        public virtual void SerializeFrom(BinaryReader reader)
        {
            this.SpinType = reader.ReadInt32();
            this.SpinOdd = reader.ReadDouble();
            this.IsEvent = reader.ReadBoolean();
            this.SpinStrings = SerializeUtils.readStringList(reader);
        }
    }
    public class BaseJiliSlotSpinResult
    {
        public double TotalWin { get; set; }
        public ActionTypes NextAction { get; set; }
        public string ResultString { get; set; }
        public string BonusResultString { get; set; }
        public virtual double WinMoney
        {
            get
            {
                return this.TotalWin;
            }
        }
        public bool HasBonusResult
        {
            get { return !string.IsNullOrEmpty(BonusResultString); }
        }
        public virtual void SerializeTo(BinaryWriter writer)
        {
            writer.Write(this.TotalWin);
            writer.Write((byte)this.NextAction);
            writer.Write(this.ResultString);
            writer.Write(this.BonusResultString);

        }
        public virtual void SerializeFrom(BinaryReader reader)
        {
            this.TotalWin = reader.ReadDouble();
            this.NextAction = (ActionTypes)reader.ReadByte();
            this.ResultString = reader.ReadString();
            this.BonusResultString = reader.ReadString();
        }
        public byte[] convertToByte()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    this.SerializeTo(bw);
                }
                return ms.ToArray();
            }
        }
        public BaseJiliSlotSpinResult()
        {
            this.ResultString = "";
            this.BonusResultString = "";
        }
    }
    /// <summary>
    /// ///////////////////
    /// </summary>


    public class BaseJiliActionToResponse
    {
        public ActionTypes ActionType { get; set; }
        public string Response { get; set; }

        public BaseJiliActionToResponse(ActionTypes actionType, string strResponse)
        {
            this.ActionType = actionType;
            this.Response = strResponse;
        }
    }
    public class JiliFreeSpinInfo
    {
        public long BonusID { get; set; }
        public int RemainCount { get; set; }
        public double BetPerLine { get; set; }
        public double TotalWin { get; set; }
        public bool Pending { get; set; }
        public DateTime ExpireTime { get; set; }
        public string FreeSpinID { get; set; }
        public JiliFreeSpinInfo()
        {

        }
        public JiliFreeSpinInfo(long bonusID, int remainCount, double betPerLine, double totalWin, bool pending, DateTime expireTime, string freeSpinID)
        {
            BonusID = bonusID;
            RemainCount = remainCount;
            BetPerLine = betPerLine;
            TotalWin = totalWin;
            Pending = pending;
            ExpireTime = expireTime;
            FreeSpinID = freeSpinID;
        }
        public void SerializeFrom(BinaryReader reader)
        {
            this.BonusID = reader.ReadInt64();
            this.RemainCount = reader.ReadInt32();
            this.BetPerLine = reader.ReadDouble();
            this.TotalWin = reader.ReadDouble();
            this.Pending = reader.ReadBoolean();
            this.ExpireTime = DateTime.Parse(reader.ReadString());
            this.FreeSpinID = reader.ReadString();
        }
        public void SerializeTo(BinaryWriter writer)
        {
            writer.Write(BonusID);
            writer.Write(RemainCount);
            writer.Write(BetPerLine);
            writer.Write(TotalWin);
            writer.Write(Pending);
            writer.Write(ExpireTime.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.Write(FreeSpinID);
        }
        public byte[] convertToByte()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    this.SerializeTo(bw);
                }
                return ms.ToArray();
            }
        }
    }

    public class BaseJiliSlotBetInfo
    {
        public float BetPerLine { get; set; }
        public int LineCount { get; set; }
        public bool PurchaseFree { get; set; }
        public bool MoreBet { get; set; }
        public string RoundID { get; set; }
        public string BetTransactionID { get; set; }
        public BaseJiliSlotSpinData SpinData { get; set; }
        public List<BaseJiliActionToResponse> RemainReponses { get; set; }

        public bool HasRemainResponse
        {
            get
            {
                if (this.RemainReponses != null && this.RemainReponses.Count > 0)
                    return true;
                else
                    return false;
            }
        }

        public virtual float TotalBet
        {
            get
            {
                return BetPerLine * LineCount;
            }
        }
        public BaseJiliSlotBetInfo()
        {
            this.PurchaseFree = false;
            this.MoreBet = false;
        }
        public BaseJiliActionToResponse pullRemainResponse()
        {
            if (RemainReponses == null || RemainReponses.Count == 0)
                return null;

            BaseJiliActionToResponse response = RemainReponses[0];
            RemainReponses.RemoveAt(0);
            return response;
        }

        public void pushFrontResponse(BaseJiliActionToResponse response)
        {
            RemainReponses.Insert(0, response);
        }
        public virtual void SerializeFrom(BinaryReader reader)
        {
            this.BetPerLine = reader.ReadSingle();
            this.LineCount = reader.ReadInt32();
            this.PurchaseFree = reader.ReadBoolean();
            this.MoreBet = reader.ReadBoolean();
            int remainCount = reader.ReadInt32();
            if (remainCount == 0)
            {
                this.RemainReponses = null;
            }
            else
            {
                this.RemainReponses = new List<BaseJiliActionToResponse>();
                for (int i = 0; i < remainCount; i++)
                {
                    ActionTypes actionType = (ActionTypes)(byte)reader.ReadByte();
                    string strResponse = (string)reader.ReadString();
                    this.RemainReponses.Add(new BaseJiliActionToResponse(actionType, strResponse));
                }
            }
            int spinDataType = reader.ReadInt32();
            if (spinDataType == 0)
            {
                this.SpinData = null;
            }
            else if (spinDataType == 1)
            {
                this.SpinData = new BaseJiliSlotSpinData();
                this.SpinData.SerializeFrom(reader);
            }
            else
            {
                this.SpinData = new BaseJiliSlotStartSpinData();
                this.SpinData.SerializeFrom(reader);
            }
            bool hasValue = reader.ReadBoolean();
            if (hasValue)
                this.RoundID = reader.ReadString();

            hasValue = reader.ReadBoolean();
            if (hasValue)
                this.BetTransactionID = reader.ReadString();

        }
        public virtual void SerializeTo(BinaryWriter writer)
        {
            writer.Write(this.BetPerLine);
            writer.Write(this.LineCount);
            writer.Write(this.PurchaseFree);
            writer.Write(this.MoreBet);
            if (this.RemainReponses == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(this.RemainReponses.Count);
                for (int i = 0; i < this.RemainReponses.Count; i++)
                {
                    writer.Write((byte)this.RemainReponses[i].ActionType);
                    writer.Write((string)this.RemainReponses[i].Response);
                }
            }
            if (this.SpinData == null)
                writer.Write(0);
            else if (this.SpinData is BaseJiliSlotStartSpinData)
                writer.Write(2);
            else
                writer.Write(1);

            if (this.SpinData != null)
                this.SpinData.SerializeTo(writer);

            if (this.RoundID == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(this.RoundID);
            }
            if (this.BetTransactionID == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(this.BetTransactionID);
            }
        }
        public byte[] convertToByte()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    this.SerializeTo(bw);
                }
                return ms.ToArray();
            }
        }
    }

    public class BaseJiliGroupedSlotBetInfo : BaseJiliSlotBetInfo
    {
        public Dictionary<double, SelectedGroupSequence> SeqPerBet { get; set; }
        public BaseJiliGroupedSlotBetInfo()
        {
            SeqPerBet = new Dictionary<double, SelectedGroupSequence>();
        }
        public void undoSequence()
        {
            double totalBet = Math.Round(this.TotalBet, 2);
            if (!SeqPerBet.ContainsKey(totalBet))
                return;

            SeqPerBet[totalBet].undoSample();
        }

        public override void SerializeTo(BinaryWriter writer)
        {
            base.SerializeTo(writer);
            writer.Write(this.SeqPerBet.Count);
            foreach (KeyValuePair<double, SelectedGroupSequence> pair in this.SeqPerBet)
            {
                writer.Write(pair.Key);
                pair.Value.SerializeTo(writer);
            }
        }
        public override void SerializeFrom(BinaryReader reader)
        {
            base.SerializeFrom(reader);
            int count = reader.ReadInt32();
            this.SeqPerBet = new Dictionary<double, SelectedGroupSequence>();
            for (int i = 0; i < count; i++)
            {
                double key = reader.ReadDouble();
                SelectedGroupSequence selectedSeq = new SelectedGroupSequence();
                selectedSeq.SerializeFrom(reader);
                this.SeqPerBet.Add(key, selectedSeq);
            }
        }
    }

    public class SelectedGroupSequence
    {
        public int LastID { get; set; }
        public List<GroupSequenceSample> Samples { get; set; }
        public double CreditMoney { get; set; }
        public bool IsEnded
        {
            get { return Samples.Count == LastID; }
        }

        public SelectedGroupSequence()
        {
            this.LastID = 0;
            this.Samples = new List<GroupSequenceSample>();
        }

        public void undoSample()
        {
            if (this.LastID > 0)
                this.LastID--;
        }
        public GroupSequenceSample nextSample()
        {
            if (Samples == null || LastID >= Samples.Count)
                return null;

            LastID++;
            return Samples[LastID - 1];
        }
        public void SerializeFrom(BinaryReader reader)
        {
            this.LastID = reader.ReadInt32();
            int count = reader.ReadInt32();
            this.Samples = new List<GroupSequenceSample>();
            for (int i = 0; i < count; i++)
            {
                GroupSequenceSample sample = new GroupSequenceSample();
                sample.SerializeFrom(reader);
                this.Samples.Add(sample);
            }
        }
        public void SerializeTo(BinaryWriter writer)
        {
            writer.Write(this.LastID);
            writer.Write(this.Samples.Count);
            for (int i = 0; i < this.Samples.Count; i++)
                this.Samples[i].SerializeTo(writer);
        }
    }
    public class GroupSequenceSample
    {
        public int Index { get; set; }
        public int CumulatedGroupSum { get; set; }
        public int Group { get; set; }

        public GroupSequenceSample()
        {

        }
        public GroupSequenceSample(int index, int cumulatedGroupSum, int group)
        {
            this.Index = index;
            this.CumulatedGroupSum = cumulatedGroupSum;
            this.Group = group;
        }
        public void SerializeFrom(BinaryReader reader)
        {
            this.Index = reader.ReadInt32();
            this.CumulatedGroupSum = reader.ReadInt32();
            this.Group = reader.ReadInt32();
        }
        public void SerializeTo(BinaryWriter writer)
        {
            writer.Write(this.Index);
            writer.Write(this.CumulatedGroupSum);
            writer.Write(this.Group);
        }
    }
    public class BaseJiliHistory
    {
        public int error { get; set; }
        public string description { get; set; }
        public string init { get; set; }
        public List<BaseJiliHistoryItem> log { get; set; }

        [JsonIgnore]
        public double bet { get; set; }

        [JsonIgnore]
        public double baseBet { get; set; }

        [JsonIgnore]
        public double win { get; set; }

        [JsonIgnore]
        public double rtp { get; set; }
        public BaseJiliHistory()
        {
            this.error = 0;
            this.description = "OK";
            this.log = new List<BaseJiliHistoryItem>();
        }

        public byte[] convertToByte()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    this.serializeTo(bw);
                }
                return ms.ToArray();
            }
        }
        public void serializeTo(BinaryWriter writer)
        {
            writer.Write(this.error);
            writer.Write(this.description);
            writer.Write(this.init);
            writer.Write(this.log.Count);
            for (int i = 0; i < this.log.Count; i++)
            {
                writer.Write(this.log[i].cr);
                writer.Write(this.log[i].sr);
            }
            writer.Write(bet);
            writer.Write(baseBet);
            writer.Write(win);
            writer.Write(rtp);
        }
        public void serializeFrom(BinaryReader reader)
        {
            this.error = reader.ReadInt32();
            this.description = reader.ReadString();
            this.init = reader.ReadString();
            int logCount = reader.ReadInt32();
            this.log.Clear();
            for (int i = 0; i < logCount; i++)
            {
                BaseJiliHistoryItem item = new BaseJiliHistoryItem();
                item.cr = reader.ReadString();
                item.sr = reader.ReadString();
                this.log.Add(item);
            }
            this.bet = reader.ReadDouble();
            this.baseBet = reader.ReadDouble();
            this.win = reader.ReadDouble();
            this.rtp = reader.ReadDouble();
        }
    }
    public class BaseJiliHistoryItem
    {
        public string cr { get; set; }  //client request
        public string sr { get; set; }  //server response
    }

}
