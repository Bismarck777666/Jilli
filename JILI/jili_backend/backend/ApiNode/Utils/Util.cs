using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiNode.Utils
{
    public class Util
    {
        public static string ReverseDomain(string domain)
        {
            // 문자열 전체를 뒤집습니다
            char[] charArray = domain.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
