using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DavuxLib2.HTTP
{
    public class MimeType
    {
        static Dictionary<string, string> Cache = new Dictionary<string, string>();
        public static string Default = "application/unknown";

        static MimeType()
        {
            Cache.Add("html", "text/html");
            Cache.Add("htm", "text/html");
            Cache.Add("js", "text/javascript");
            Cache.Add("css", "text/css");
        }


        public static string DetermineFromFile(string fileName)
        {
            return DetermineFromExtension(System.IO.Path.GetExtension(fileName).ToLower());
        }

        public static string DetermineFromExtension(string ext)
        {
            foreach (string key in Cache.Keys)
            {
                if (key == ext)
                {
                    return Cache[key];
                }
            }
            return MimeFromRegistry(ext);
        }

        private static string MimeFromRegistry(string ext)
        {
            string mimeType = Default;
            try
            {
                Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
                if (regKey != null && regKey.GetValue("Content Type") != null)
                {
                    mimeType = regKey.GetValue("Content Type").ToString();
                }
            }
            catch
            {

            }
            return mimeType;
        }
    }
}
