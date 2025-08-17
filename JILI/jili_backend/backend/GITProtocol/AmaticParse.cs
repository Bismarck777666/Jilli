using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GITProtocol
{
    public enum AmaticMessageType
    {
        HeartBeat       = 0,
        NormalSpin      = 3,
        Collect         = 4,
        FreeTrigger     = 5,
        FreeSpin        = 6,
        GamblePick      = 7,
        GambleHalf      = 8,
        NoCollectSpin   = 9,    //웨이즈 노멀 스핀
        ExtendFree      = 10,   //프리게임중 추가프리스핀
        FreeReopen      = 11,   //프리게임때 껏다 켤때
        LastFree        = 12,
        FreeOption      = 16,   //프로옵션게임때 픽크
        WheelTrigger    = 20, 
        Wheel           = 21,
        LastWheel       = 22,
        RespinTrigger   = 30,
        Respin          = 31,
        LastRespin      = 32,
        FreeRespinStart = 34,   //프리스핀안에서 리스핀 시작
        FreeRespin      = 35,   //프리스핀안에서 리스핀
        FreeRespinEnd   = 36,   //프리스핀안에서 리스핀 끝
        TriggerPower    = 37,   //파워리스핀 시작
        PowerRespin     = 38,   //파워리스핀
        LastPower       = 39,   //파워리스핀 끝
        BonusTrigger    = 45,   
        BonusSpin       = 46,
        BonusEnd        = 47,
        FreeCashStart   = 52,
        FreeCashSpin    = 53,
        FreeCashEnd     = 54,
        DiamondStart    = 57,
        DiamondSpin     = 58,
        DiamondEnd      = 59,
        
        PurFree         = 66,   //프리구매
    }

    public class AmaticDecrypt
    {
        private long hexStringToLong(string strHex)
        {
            long n = Int64.Parse(strHex, System.Globalization.NumberStyles.HexNumber);
            return n;
        }

        public long Read1BitHexToDec(string message, int curPoint, out int point)
        {
            string strHex = message.Substring(curPoint, 1);
            point = curPoint + 1;

            return hexStringToLong(strHex);
        }

        public long Read2BitHexToDec(string message, int curPoint, out int point)
        {
            string strHex = message.Substring(curPoint, 2);
            point = curPoint + 2;

            return hexStringToLong(strHex);
        }

        public long ReadLengthAndDec(string message, int curPoint, out int point)
        {
            string strLengthHex = message.Substring(curPoint, 1);
            int length = (int)hexStringToLong(strLengthHex);
            point = curPoint + 1;

            string strHex = message.Substring(point, length);
            point = point + length;

            return hexStringToLong(strHex);
        }

        public List<long> Read1BitNumArray(string message, int curPoint, out int point)
        {
            List<long> numArray = new List<long>();
            long arrayLength = ReadLengthAndDec(message, curPoint, out point);
            
            for(long i = 0; i < arrayLength; i++)
            {
                long num = Read1BitHexToDec(message, point, out point);

                numArray.Add(num);
            }

            return numArray;
        }

        public List<long> Read2BitNumArray(string message, int curPoint, out int point)
        {
            List<long> numArray = new List<long>();
            long arrayLength = ReadLengthAndDec(message, curPoint, out point);

            for (long i = 0; i < arrayLength; i++)
            {
                long num = Read2BitHexToDec(message, point, out point);

                numArray.Add(num);
            }

            return numArray;
        }

        public string ReadLeftHexString(string message, int curPoint, out int point)
        {
            string strHex = message.Substring(curPoint);
            point = curPoint + strHex.Length;

            return strHex;
        }

        public string ReadHexString(string message, int curPoint, int length, out int point)
        {
            string strHex = message.Substring(curPoint, length);
            point = curPoint + strHex.Length;

            return strHex;
        }
    }

    public class AmaticEncrypt 
    { 
        private string longToHexString(long num)
        {
            return string.Format("{0:X}", num);
        }
        
        public string WriteDecHex(string message, long num)
        {
            return string.Format("{0}{1}", message, longToHexString(num));
        }

        public string WriteDec2Hex(string message, long num)
        {
            string strNum = longToHexString(num);
            if (strNum.Length == 1)
                strNum = string.Format("0{0}", strNum);

            return string.Format("{0}{1}", message, strNum);
        }

        public string WriteLengthAndDec(string message, long num)
        {
            string numStr = longToHexString(num);
            string lenStr = longToHexString(numStr.Length);

            return string.Format("{0}{1}{2}", message, lenStr, numStr);
        }

        public string Write1BitNumArray(string message, List<long> numArray)
        {
            int numArrayLength = numArray.Count;
            message = WriteLengthAndDec(message, numArrayLength);
            
            for (int i = 0; i < numArrayLength; i++)
            {
                message = WriteDecHex(message, numArray[i]);
            }

            return message;
        }

        public string Write2BitNumArray(string message, List<long> numArray)
        {
            int numArrayLength = numArray.Count;
            message = WriteLengthAndDec(message, numArrayLength);

            for (int i = 0; i < numArrayLength; i++)
            {
                message = WriteDec2Hex(message, numArray[i]);
            }

            return message;
        }

        public string WriteLeftHexString(string message, string strHex)
        {
            return string.Format("{0}{1}", message, strHex);
        }
    }

    public class AmaticPacket
    {
        public int          curpoint        { get; set; }   //파켓을 읽을때 현재위치
        public long         messageheader   { get; set; }
        public long         messagetype     { get; set; }
        public long         sessionclose    { get; set; }
        public long         messageid       { get; set; }
        public long         balance         { get; set; }
        public long         win             { get; set; }
        public List<long>   reelstops       { get; set; }
        public long         betstep         { get; set; }
        public long         betline         { get; set; }
        public long         totalfreecnt    { get; set; }
        public long         curfreecnt      { get; set; }
        public long         curfreewin      { get; set; }
        public long         freeunparam1    { get; set; }
        public long         freeunparam2    { get; set; }
        public long         totalfreewin    { get; set; }
        public List<long>   freereelstops   { get; set; }
        public List<long>   linewins        { get; set; }
        public List<long>   gamblelogs      { get; set; }

        public AmaticPacket(string message, int reelCnt, int freeReelCnt)
        {
            AmaticDecrypt amaConverter = new AmaticDecrypt();
            int point = 0;
            messageheader   = amaConverter.Read1BitHexToDec(message, point, out point);
            messagetype     = amaConverter.Read2BitHexToDec(message, point, out point);
            sessionclose    = amaConverter.Read1BitHexToDec(message, point, out point);
            messageid       = amaConverter.ReadLengthAndDec(message, point, out point);
            messageid       = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            balance         = amaConverter.ReadLengthAndDec(message, point, out point);
            win             = amaConverter.ReadLengthAndDec(message, point, out point);

            reelstops = new List<long>();
            reelCnt = reelCnt > 5 ? reelCnt : 5;
            for(int i = 0; i < reelCnt; i++)
            {
                reelstops.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            }
            betstep         = amaConverter.Read2BitHexToDec(message, point, out point);
            betline         = amaConverter.Read2BitHexToDec(message, point, out point);


            totalfreecnt    = amaConverter.ReadLengthAndDec(message, point, out point);
            curfreecnt      = amaConverter.ReadLengthAndDec(message, point, out point);
            curfreewin      = amaConverter.ReadLengthAndDec(message, point, out point);
            freeunparam1    = amaConverter.ReadLengthAndDec(message, point, out point);
            freeunparam2    = amaConverter.ReadLengthAndDec(message, point, out point);
            totalfreewin    = amaConverter.ReadLengthAndDec(message, point, out point);

            freereelstops = new List<long>();
            freeReelCnt = freeReelCnt > 5 ? freeReelCnt : 5;
            for (int i = 0; i < freeReelCnt; i++)
            {
                freereelstops.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            }

            linewins = new List<long>();
            long lineCnt = amaConverter.Read2BitHexToDec(message, point, out point);
            for(int i = 0; i < lineCnt; i++)
            {
                linewins.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            }

            gamblelogs = new List<long>();
            for (int i = 0; i < 8; i++)
            {
                gamblelogs.Add(amaConverter.Read2BitHexToDec(message, point, out point));
            }

            curpoint = point;
        }

        public AmaticPacket(int reelCnt, int freeReelCnt, int msgType, int lineCnt)
        {
            messageheader   = 1;
            messagetype     = msgType;
            sessionclose    = 0;
            messageid       = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            balance         = 0;
            win             = 0;

            reelstops = new List<long>();
            reelCnt = reelCnt > 5 ? reelCnt : 5;
            for (int i = 0; i < reelCnt; i++)
                reelstops.Add(0);
            
            betstep         = 0;
            betline         = 0;

            totalfreecnt    = 0;
            curfreecnt      = 0;
            curfreewin      = 0;
            freeunparam1    = 0;
            freeunparam2    = 0;
            totalfreewin    = 0;

            freereelstops = new List<long>();
            freeReelCnt = freeReelCnt > 5 ? freeReelCnt : 5;
            for (int i = 0; i < freeReelCnt; i++)
                freereelstops.Add(0);
            
            linewins = new List<long>();
            for (int i = 0; i < lineCnt + 1; i++)
                linewins.Add(0);

            gamblelogs = new List<long>();
            for (int i = 0; i < 8; i++)
                gamblelogs.Add(0);
        }
    }

    public class InitPacket
    {
        public int              curpoint        { get; set; }
        public long             messageheader   { get; set; }
        public List<List<long>> reelset         { get; set; }
        public List<List<long>> freereelset     { get; set; }
        public long             messagetype     { get; set; }
        public long             sessionclose    { get; set; }
        public List<long>       reelstops       { get; set; }
        public long             messageid       { get; set; }
        public long             balance         { get; set; }
        public long             win             { get; set; }
        public long             laststep        { get; set; }
        public long             minbet          { get; set; }
        public long             maxbet          { get; set; }
        public long             lastline        { get; set; }
        public long             totalfreecnt    { get; set; }
        public long             curfreecnt      { get; set; }
        public long             curfreewin      { get; set; }
        public long             freeunparam1    { get; set; }
        public long             freeunparam2    { get; set; }
        public long             totalfreewin    { get; set; }   
        public long             unknownparam1   { get; set; }   //0
        public long             minbetline      { get; set; }   //베팅라인 최소
        public long             maxbetline      { get; set; }   //베팅라인 최대
        public long             unitbetline     { get; set; }   //베팅라인간격
        public long             unknownparam2   { get; set; }   //11
        public long             unknownparam3   { get; set; }   //00
        public List<long>       freereelstops   { get; set; }
        public List<long>       gamblelogs      { get; set; }
        public List<long>       betstepamount   { get; set; }
        public List<long>       linewins        { get; set; }
        public InitPacket(string message, int reelCnt, int freeReelCnt, int reelsetColBitCnt, int reelsetBitCnt)
        {
            AmaticDecrypt amaConverter = new AmaticDecrypt();

            int point = 0;
            messageheader   = amaConverter.Read1BitHexToDec(message, point, out point);
            long reelsetCnt = 0;
            if(reelsetColBitCnt == 1)
                reelsetCnt = amaConverter.Read1BitHexToDec(message, point, out point);
            else if(reelsetColBitCnt == 2)
                reelsetCnt = amaConverter.Read2BitHexToDec(message, point, out point);
            reelset = new List<List<long>>();
            for (int i = 0; i < reelsetCnt; i++)
            {
                List<long> reel = new List<long>();
                if (reelsetBitCnt == 1)
                    reel = amaConverter.Read1BitNumArray(message, point, out point);
                else if (reelsetBitCnt == 2)
                    reel = amaConverter.Read2BitNumArray(message, point, out point);

                reelset.Add(reel);
            }

            long freeReelsetCnt = amaConverter.Read1BitHexToDec(message, point, out point);
            freereelset = new List<List<long>>();
            for(int i = 0; i < freeReelsetCnt; i++)
            {
                List<long> reel = new List<long>();
                if (reelsetBitCnt == 1)
                    reel = amaConverter.Read1BitNumArray(message, point, out point);
                else if (reelsetBitCnt == 2)
                    reel = amaConverter.Read2BitNumArray(message, point, out point);

                freereelset.Add(reel);
            }
            messagetype     = amaConverter.Read2BitHexToDec(message, point, out point);
            sessionclose    = amaConverter.Read1BitHexToDec(message, point, out point);
            reelstops = new List<long>();
            reelCnt = reelCnt > 5 ? reelCnt : 5;
            for (int i = 0; i < reelCnt; i++)
            {
                reelstops.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            }

            messageid       = amaConverter.ReadLengthAndDec(message, point, out point);
            messageid       = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            balance         = amaConverter.ReadLengthAndDec(message, point, out point);
            win             = amaConverter.ReadLengthAndDec(message, point, out point);
            laststep        = amaConverter.Read2BitHexToDec(message, point, out point);
            minbet          = amaConverter.ReadLengthAndDec(message, point, out point);
            maxbet          = amaConverter.ReadLengthAndDec(message, point, out point);
            lastline        = amaConverter.Read2BitHexToDec(message, point, out point);

            totalfreecnt    = amaConverter.ReadLengthAndDec(message, point, out point);
            curfreecnt      = amaConverter.ReadLengthAndDec(message, point, out point);
            curfreewin      = amaConverter.ReadLengthAndDec(message, point, out point);
            freeunparam1    = amaConverter.ReadLengthAndDec(message, point, out point);
            freeunparam2    = amaConverter.ReadLengthAndDec(message, point, out point);
            totalfreewin    = amaConverter.ReadLengthAndDec(message, point, out point);

            unknownparam1   = amaConverter.ReadLengthAndDec(message, point, out point);
            minbetline      = amaConverter.Read2BitHexToDec(message, point, out point);
            maxbetline      = amaConverter.Read2BitHexToDec(message, point, out point);
            unitbetline     = amaConverter.Read2BitHexToDec(message, point, out point);
            unknownparam2   = amaConverter.ReadLengthAndDec(message, point, out point);
            unknownparam3   = amaConverter.Read2BitHexToDec(message, point, out point);

            freereelstops   = new List<long>();
            freeReelCnt = freeReelCnt > 5 ? freeReelCnt : 5;
            for (int i = 0; i < freeReelCnt; i++)
            {
                freereelstops.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            }
            
            gamblelogs = new List<long>();
            for (int i = 0; i < 8; i++)
            {
                gamblelogs.Add(amaConverter.Read2BitHexToDec(message, point, out point));
            }

            long betStepCnt = amaConverter.Read2BitHexToDec(message, point, out point);
            betstepamount = new List<long>();
            for (int i = 0; i < betStepCnt; i++)
            {
                betstepamount.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            }
            long linewinCnt = amaConverter.Read2BitHexToDec(message, point, out point);
            linewins = new List<long>();
            for(int i = 0; i < linewinCnt; i++)
            {
                linewins.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            }

            curpoint = point;
        }
    }

    public class RoulettePacket
    {
        public int          curpoint        { get; set; }   
        public long         messageheader   { get; set; }
        public long         messagetype     { get; set; }
        public long         sessionclose    { get; set; }
        public long         messageid       { get; set; }
        public long         balance         { get; set; }
        public long         win             { get; set; }
        public long         winnumber       { get; set; }   //룰렛당첨숫자
        public long         unknowparam1    { get; set; }   // 0 (-)

        public RoulettePacket(string message)
        {
            AmaticDecrypt amaConverter = new AmaticDecrypt();
            int point = 0;
            messageheader   = amaConverter.Read1BitHexToDec(message, point, out point);
            messagetype     = amaConverter.Read2BitHexToDec(message, point, out point);
            sessionclose    = amaConverter.Read1BitHexToDec(message, point, out point);
            messageid       = amaConverter.ReadLengthAndDec(message, point, out point);
            messageid       = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            balance         = amaConverter.ReadLengthAndDec(message, point, out point);
            win             = amaConverter.ReadLengthAndDec(message, point, out point);
            winnumber       = amaConverter.ReadLengthAndDec(message, point, out point);
            curpoint = point;
        }
        public RoulettePacket()
        {

        }
    }

    public class RouletteInitPacket
    {
        public int          curpoint        { get; set; }
        public long         messageheader   { get; set; }
        public long         messagetype     { get; set; }
        public long         sessionclose    { get; set; }
        public long         messageid       { get; set; }
        public long         balance         { get; set; }
        public long         win             { get; set; }
        public long         winnumber       { get; set; }   //룰렛당첨숫자
        public long         unknownparam1   { get; set; }   //0
        public List<long>   betbuttons      { get; set; }
        public long         maxbetamount    { get; set; }
        public List<long>   betlimits       { get; set; }
        public List<long>   gamblelogs      { get; set; }
        public List<long>   winnumberlogs   { get; set; }

        public RouletteInitPacket(string message)
        {
            AmaticDecrypt amaConverter = new AmaticDecrypt();

            int point = 0;
            messageheader   = amaConverter.Read1BitHexToDec(message, point, out point);
            messagetype     = amaConverter.Read2BitHexToDec(message, point, out point);
            sessionclose    = amaConverter.Read1BitHexToDec(message, point, out point);
            messageid       = amaConverter.ReadLengthAndDec(message, point, out point);
            messageid       = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            balance         = amaConverter.ReadLengthAndDec(message, point, out point);
            win             = amaConverter.ReadLengthAndDec(message, point, out point);
            winnumber       = amaConverter.ReadLengthAndDec(message, point, out point);
            unknownparam1   = amaConverter.ReadLengthAndDec(message, point, out point);

            betbuttons = new List<long>();
            for(int i = 0; i < 5; i++)
                betbuttons.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            
            maxbetamount = amaConverter.ReadLengthAndDec(message, point, out point);

            betlimits = new List<long>();
            for (int i = 0; i < 7; i++)
                betlimits.Add(amaConverter.ReadLengthAndDec(message, point, out point));

            gamblelogs = new List<long>();
            for (int i = 0; i < 8; i++)
                gamblelogs.Add(amaConverter.ReadLengthAndDec(message, point, out point));

            winnumberlogs = new List<long>();
            for (int i = 0; i < 17; i++)
            {
                winnumberlogs.Add(amaConverter.ReadLengthAndDec(message, point, out point));
            }
            curpoint = point;
        }
    }
}
