using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Net;


namespace DavuxLib2.Net
{
    public class TSocket
    {
        public delegate void ConnectHandler(TSocket sock);
        public delegate void DataHandler(TSocket sock, DataPacket data);
        public delegate bool RawHandler(TSocket sock, string type, byte[] data);

        public event ConnectHandler Connected;
        public event ConnectHandler Disconnected;

        /// <summary>
        /// Got an Xml-Serialized object
        /// </summary>
        public event DataHandler XmlDataArrived;

        /// <summary>
        /// Got a packet, return true to handle it before XmlDataArrived or other handlers pull it off
        /// </summary>
        public event RawHandler BinaryDataArrived;

        private object sendLock = new object();

        public bool IsListening { get { return listener != null; } }

        public bool IsConnected { get { return sock != null && sock.Connected; } }

        public override string ToString()
        {
            if (sock == null)
            {
                if (listener != null)
                    return " [Listening " + listener.LocalEndpoint + "]";
            }
            else
            {
                if (sock.Connected)
                    return " [Connected: " + sock.Client.RemoteEndPoint + "]";
            }
            return "[Not Connected]";
        }

        private TcpClient sock = null;
        private TcpListener listener = null;
        BinaryWriter bw = null;

        public TSocket() { }
        private TSocket(TcpClient sock) { this.sock = sock; }


        private void StartReader()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.Name = "TSocket Reader";
                try
                {
                    bw = new BinaryWriter(sock.GetStream());
                    BinaryReader br = new BinaryReader(sock.GetStream());
                    string line = "";
                    string type = "";
                    string len = "";
                    while (sock != null && sock.Connected)
                    {
                        byte[] header = br.ReadBytes(16);

                        line = Encoding.UTF8.GetString(header).Trim();
                        if (string.IsNullOrEmpty(line))
                            break;
                        int m = line.IndexOf(' ');
                        if (m > -1 && m + 1 < line.Length)
                        {
                            type = line.Substring(1, m).Trim();
                            len = line.Substring(m + 1);

                            OnData(type, br.ReadBytes(int.Parse(len)));
                        }
                        else
                        {
                            Trace.WriteLine("Invalid Packet Header! " + line + " " + m);
                        }
                    }
                }
                catch (IOException ex)
                {
                    Trace.WriteLine("zObjSock I/O: " + ex.Message);
                }
                catch (SocketException ex)
                {
                    Trace.WriteLine("zObjSock Socket /I/O: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("TSocket Reader Error: " + ex);
                }
                finally
                {
                    if (Disconnected != null)
                        Disconnected(this);
                }
            }).Start();
        }

        private void OnData(string type, byte[] buffer)
        {
            if (BinaryDataArrived != null)
            {
                if (BinaryDataArrived(this, type, buffer)) // event handler will pick it off if needed
                {
                    return;
                }
            }
            if (XmlDataArrived != null)
            {
                if (type.Trim() == "p")
                {
                    string line = "";
                    try
                    {
                        line = Encoding.UTF8.GetString(buffer);
                        int c = line.IndexOf(' ');
                        if (c > -1 && c + 1 < line.Length)
                        {
                            string t = line.Substring(0, c).Trim();
                            string data = line.Substring(++c);
                            DataPacket dp = new DataPacket(t, data);
                            XmlDataArrived(this, dp);
                        }
                        else
                        {
                            Trace.WriteLine("TSocket Reader Missing Type: (msg): " + line);
                        }
                    }
                    catch (FormatException ex)
                    {
                        Trace.WriteLine("TSocket Reader FormatError: " + ex.Message + " (msg): " + line);
                    }
                }
                else
                {
                    Trace.WriteLine("unregistered type: |" + type + "|");
                }
            }
            else
            {
                Trace.WriteLine("no handler");
            }

        }

        public void Listen(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            new Thread((ThreadStart)delegate()
            {
                Thread.CurrentThread.Name = "TSocket Listen " + port;
                try
                {
                    while (listener.Server.IsBound)
                    {
                        if (Connected != null)
                        {
                            TSocket os = new TSocket(listener.AcceptTcpClient());
                            Connected(os);
                            os.StartReader();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("TSocket Listen: " + ex);
                }
                finally
                {
                    listener = null;
                    if (Disconnected != null)
                        Disconnected(this);
                }
            }).Start();
        }

        public void StopListen()
        {
            try
            {
                if (listener != null)
                    listener.Stop();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("TSocket Stop Listen: " + ex);
            }
        }

        public void Connect(string host, int port)
        {
            sock = new TcpClient();
            sock.Connect(host, port);
            StartReader();
        }

        public void Disconnect()
        {
            try
            {
                sock.Client.Close();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("TSocket Disconnect: " + ex);
            }
            finally
            {
                sock = null;
            }
        }


        public void Send(string type, byte[] buffer)
        {
            if (sock != null && sock.Connected)
            {
                while (bw == null)
                {
                    Thread.Sleep(5); // waiting for the reader to start.
                }
                byte[] bb = Encoding.UTF8.GetBytes(type + " " + buffer.Length + "\r\n");


                string header = type + " " + buffer.Length + "\r\n";
                header = header.PadRight(15, ' ');

                lock (sendLock)
                {
                    bw.Write(header);
                    bw.Flush();
                    bw.Write(buffer);
                    bw.Flush();
                }
            }
            else
            {
                throw new InvalidOperationException("Socket is not connected");
            }
        }

        public void Send(object obj)
        {
            Send(new DataPacket(obj));
        }

        public void Send(DataPacket data)
        {
            Send("p", Encoding.UTF8.GetBytes(data.Type + " " + data.Data));
        }

        public class DataPacket
        {
            public string Type { get; set; }
            public string Data { get; set; }

            public DataPacket() { }
            public DataPacket(object o) : this(o, o.GetType().ToString()) { }
            public DataPacket(object o, string Type)
            {
                MemoryStream memoryStream = new MemoryStream();
                XmlSerializer xs = new XmlSerializer(o.GetType());
                XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                xs.Serialize(xmlTextWriter, o);
                memoryStream = (MemoryStream)xmlTextWriter.BaseStream;

                this.Type = Type;
                this.Data = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            public DataPacket(string Type, string Data)
            {
                this.Data = Data; this.Type = Type;
            }

            public T GetObject<T>()
            {
                XmlSerializer xs = new XmlSerializer(typeof(T));
                MemoryStream memoryStream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(Data));
                XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                return (T)xs.Deserialize(memoryStream);
            }

            public override string ToString()
            {
                return "[DataPacket: " + Type + "/" + Data.Length + "]";
            }
        }
    }
}
