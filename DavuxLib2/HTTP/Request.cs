using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace DavuxLib2.HTTP
{
    public class Request
    {
        private TcpClient tcp = null;
        private Server server = null;

        public Headers Headers = null;
        public Response Response = null;
        public bool FirstRequest = true;

        public bool KeepAlive
        {
            get
            {
                return Headers["Connection"].ToLower() != "close" && Headers.HTTPVersion == "HTTP/1.1";
            }
        }

        internal Request(TcpClient tcp, Server server)
        {
            this.server = server;
            this.tcp = tcp;
        }

        internal void Accept()
        {
            try
            {
                while (true)
                {
                    Headers = new Headers(tcp, this);
                    if (!Headers.Read())
                    {
                        // connection ended
                        return;
                    }
                    Response = new Response(tcp);
                    string conn = Headers["connection"].ToLower().Trim();
                    if (conn != "close" && conn != "keep-alive")
                    {
                        conn = "keep-alive";
                    }

                    // TODO re-enable keep-alive
                    // Response.AddHeader("Connection", conn);
                    try
                    {
                        if (this.Headers != null)
                        {
                            server.OnRequestEvent(this);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("HTTP/Request/Accept: Unhandled Exception: " + ex);
                    }
                    finally
                    {
                        Response.AddHeader("Content-Type", Response.MimeType);
                        Response.Commit();
                    }
                    /*
                    if (!Settings.Get("KeepAlive", true))
                    {
                        //tcp.Close();
                        tcp.Client.Close();
                        break;
                    }
                    if (!KeepAlive)
                    {
                        break;
                    }
                    */
                    tcp.Client.Close();
                    break;
                }
            }
            catch (SocketException)
            {

            }
            catch (IOException)
            {

            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error Accepting Connection: " + ex);
            }
        }



    }
}
