using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DavuxLib2.Extensions
{
    public static class ByteArrayExtensions
    {
        public static string ToUTF8String(this byte[] ba)
        {
            return new UTF8Encoding().GetString(ba);
        }
    }
}
