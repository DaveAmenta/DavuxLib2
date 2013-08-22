using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using DavuxLib2.Extensions;

namespace DavuxLib2.HTTP
{
    public class Headers
    {
        private TcpClient tcp = null;
        private List<KeyValuePair<string, string>> headers = new List<KeyValuePair<string, string>>();
        private Request req = null;

        public string Method { get; private set; }
        public string HTTPVersion { get; private set; }
        public string URL { get; private set; }
        public string Body { get; private set; }
        public Dictionary<string, string> QueryString { get; private set; }

        public string AuthenticatedUser { get; private set; }
        public string AuthenticatedPassword { get; private set; }


        public Headers(TcpClient tcp, Request req)
        {
            this.tcp = tcp;
            this.req = req;
            QueryString = new Dictionary<string, string>();
        }

        /// <summary>
        /// Read the entire HTTP header, including Data indicated by the Content-Length header.
        /// </summary>
        /// <exception cref="System.IO.IOException"></exception>
        public bool Read()
        {
            try
            {
                StreamReader sr = new StreamReader(tcp.GetStream());
                string requestLine = sr.ReadLine();
                if (requestLine == null)
                {
                    return false;
                }
                string[] rlParts = requestLine.Split(new char[] { ' ' });

                if (rlParts.Length == 3)
                {
                    Method = rlParts[0];        // GET
                    URL = rlParts[1];           // /path/to/file

                    if (URL.Contains('?'))
                    {
                        string qs = URL.Substring(URL.IndexOf('?'));
                        if (qs.Length > 1)
                        {
                            qs = qs.Substring(1); // remove ?
                        }
                        URL = URL.Substring(0, URL.IndexOf('?'));
                        QueryString = MakeQueryString(qs);
                    }

                    HTTPVersion = rlParts[2];
                    if (req.FirstRequest)
                    {
                        req.FirstRequest = false;
                        Trace.WriteLine(Method + " " + URL + " " + HTTPVersion + " " + tcp.Client.RemoteEndPoint);
                    }
                    if (HTTPVersion == "HTTP/1.1" || HTTPVersion == "HTTP/1.0")
                    {
                        while (true)
                        {
                            string h = sr.ReadLine();  // read each header line
                            Trace.WriteLine(h);
                            if (string.IsNullOrEmpty(h))
                            {
                                break;
                            }
                            else
                            {
                                int c = h.IndexOf(':');
                                if (c > -1)
                                {
                                    headers.Add(new KeyValuePair<string, string>(h.Substring(0, c).ToLower(), h.Substring(c + 1).Trim()));
                                }
                            }
                        }

                        int cl = GetInt("Content-Length");
                        if (cl > 0)
                        {
                            BinaryReader bn = new BinaryReader(tcp.GetStream());
                            byte[] buff = bn.ReadBytes(cl);
                            Body = Encoding.UTF8.GetString(buff);
                        }

                        string auth = this["authorization"];
                        if (auth.IndexOf(' ') > -1)
                        {
                            string[] p = auth.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (p.Length == 2)
                            {
                                if (p[0].ToLower() == "basic")
                                {
                                    p[1] = p[1].FromBase64();
                                    string[] parts = p[1].Split(':');
                                    if (parts.Length == 2)
                                    {
                                        AuthenticatedUser = parts[0];
                                        AuthenticatedPassword = parts[1];
                                    }
                                    else
                                    {
                                        Trace.WriteLine("HTTP/Headers/Read: Authorization failure: " + p[1]);
                                    }
                                }
                            }
                        }

                        return true;
                    }
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("HTTP/Server/Headers Error Reading Headers: " + ex);
            }
            return false;
        }

        public string this[string key]
        {
            get
            {
                string v = headers.Find(item => item.Key.ToLower() == key.ToLower()).Value;
                return string.IsNullOrEmpty(v) ? "" : v;
            }
        }

        public int GetInt(string key)
        {
            try
            {
                string v = this[key];
                if (string.IsNullOrEmpty(v))
                {
                    return 0;
                }
                else
                {
                    return int.Parse(v);
                }
            }
            catch (FormatException)
            {
                return 0;
            }
            catch (OverflowException)
            {
                return int.MaxValue;
            }
        }

        private static Dictionary<string, string> MakeQueryString(string qs)
        {
            Dictionary<string, string> q = new Dictionary<string, string>();

            if (qs.Contains('&'))
            {
                // multiple values
                string[] pairs = qs.Split(new char[] { '&' });
                foreach (string s in pairs)
                {
                    KeyValuePair<string, string> kv = GetQueryStringValue(s);
                    q.Add(kv.Key, kv.Value);
                }
            }
            else
            {
                KeyValuePair<string, string> kv = GetQueryStringValue(qs);
                q.Add(kv.Key, kv.Value);
            }
            return q;
        }

        private static KeyValuePair<string, string> GetQueryStringValue(string q)
        {
            string key = "";
            string value = "";
            if (q.IndexOf('=') > -1)
            {
                key = q.Substring(0, q.IndexOf('=')).ToLower();
                value = q.Substring(key.Length);
                if (value.Length > 0)
                {
                    value = value.Substring(1);
                }
            }
            else
            {
                key = q;
                value = "";
            }
            return new KeyValuePair<string, string>(key, value);
        }
    }
}
