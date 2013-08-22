using System.Net;

namespace DavuxLib2.Extensions
{
    public static class IPEndPointExtensions
    {
        /// <summary>
        /// Compare the given IPEndPoint to all IPEndPoints that refer to the local system.
        /// </summary>
        /// <param name="ep">IPEndPoint to compare</param>
        /// <returns>True if the given IPEndPoint is present in the local AddressList</returns>
        public static bool IsLocalComputer(this IPEndPoint ep)
        {
            foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (IPAddress.Equals(ip, ep.Address)) return true;
            }
            return false;
        }
    }
}
