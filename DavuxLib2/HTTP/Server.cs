using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Net;

namespace DavuxLib2.HTTP
{
    public class Server
    {
        // http://www.w3.org/Protocols/rfc2616/rfc2616-sec6.html#sec6.1.1
        public enum StatusCodes
        {
            //  1xx: Informational - Request received, continuing process
            CONTINUE = 100,

            //  2xx: Success - The action was successfully received, understood, and accepted
            OK = 200,
            NO_CONTENT = 204,

            //  3xx: Redirection - Further action must be taken in order to complete the request
            MOVED_PERMANENTLY = 301,
            FOUND = 302,

            //  4xx: Client Error - The request contains bad syntax or cannot be fulfilled
            BAD_REQUEST = 400,
            UNAUTHORIZED = 401,
            NOT_FOUND = 404,
            FORBIDDEN = 403,
            REQUEST_URI_TOO_LONG = 414,

            //  5xx: Server Error - The server failed to fulfill an apparently valid request
            INTERNAL_SERVER_ERROR = 500,
            NOT_IMPLEMENTED = 501,
            HTTP_VERSION_NOT_SUPPORTED = 505,
        }


        public delegate void RequestHandler(Request req, Server server);
        public event RequestHandler OnRequest;

        private TcpListener _tcpListener = null;
        private Thread _listenThread = null;

        public int Port { get; set; }

        public Server(int Port)
        {
            this.Port = Port;
        }

        public void Start()
        {
            if (_listenThread != null)
            {
                throw new ApplicationException("Already called Start");
            }

            _tcpListener = new TcpListener(IPAddress.Any, Port);
            _tcpListener.Start();

            _listenThread = new Thread(ListenThreadEntry);
            _listenThread.Start();
        }

        /// <summary>
        /// Immediately signal the Webserver to stop accepting connections.  Pending requests will be completed.
        /// </summary>
        public void Stop()
        {
            if (_listenThread == null)
            {
                throw new Exception("Already called stop");
            }

            try
            {
                _tcpListener.Stop();
            }
            catch (Exception) { }

            try
            {
                _listenThread.Abort();
            }
            catch (Exception) { }

            _listenThread = null;
        }

        private void ListenThreadEntry()
        {
            Thread.CurrentThread.Name = "Davux/HTTP/Server/Listen";
            while (_tcpListener.Server.IsBound)
            {
                try
                {
                    TcpClient tcpConnection = _tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(new WaitCallback(_ConnectionThreadEntry), tcpConnection);
                }
                catch (ThreadAbortException)
                {
                    //
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("HTTP/Server/Listen Failed to accept client: " + ex.Message);
                }
            }
        }

        private void _ConnectionThreadEntry(object oTcpClient)
        {
            Thread.CurrentThread.Name = "Davux/HTTP/Server/ClientThread";
            try
            {
                Request r = new Request((TcpClient)oTcpClient, this);
                r.Accept();
            }
            catch (ThreadAbortException)
            {
                //
            }
            catch (Exception ex)
            {
                Trace.WriteLine("HTTP/Server/ConnectionEntry Error Accepting Request: " + ex);
            }
        }

        internal void OnRequestEvent(Request req)
        {
            if (OnRequest != null)
            {
                try
                {
                    OnRequest.Invoke(req, this);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("HTTP/Server/OnRequest Unhandled Exception: " + ex);
                }
            }
            else
            {
                throw new Exception("HTTP Request Handler OnRequest Not Attached.");
            }
        }

        public string LocalEndPoint
        {
            // _tcpListener.Server.LocalEndPoint
            get
            {
                IPHostEntry host;
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork
                        && !ip.ToString().StartsWith("127.0"))
                    {
                        return ip.ToString();
                    }
                }
                return Dns.GetHostName();
            }
        }
    }
}
