using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace DavuxLib2.HTTP
{
    public class Response
    {
        public Server.StatusCodes Code = Server.StatusCodes.NOT_IMPLEMENTED;
        public byte[] Body = new byte[0];
        public string BodyString = "";
        public string MimeType = "application/unknown";

        private TcpClient tcp = null;
        private List<string> Headers = new List<string>();
        private bool Committed = false;

        internal Response(TcpClient tcp)
        {
            this.tcp = tcp;
        }

        public void AddHeader(string key, string value)
        {
            Headers.Add(key + ": " + value);
        }

        public void Commit()
        {
            if (Committed) return;
            Committed = true;

            try
            {
                BinaryWriter br = new BinaryWriter(tcp.GetStream());

                string resp_header = "HTTP/1.1 " + (int)Code + " " + Code.ToString() + "\r\n";

                foreach (string header in Headers)
                {
                    resp_header += header + "\r\n";
                }

                if (Body.Length == 0 && BodyString.Length > 0)
                {
                    Body = Encoding.UTF8.GetBytes(BodyString);
                }

                resp_header += "Content-Length: " + Body.Length + "\r\n\r\n";

                Trace.WriteLine(resp_header);

                br.Write(Encoding.UTF8.GetBytes(resp_header));
                br.Write(Body);
                br.Flush();
            }
            catch (SocketException ex)
            {
                Trace.WriteLine(ex);
            }
            catch (IOException ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }
}
