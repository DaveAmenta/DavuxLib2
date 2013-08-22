using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using DavuxLib2.Extensions;

namespace DavuxLib2.Net
{
    public class USocket
    {
        public class DataPacket
        {
            public string Type { get; set; }
            public string Data { get; set; }

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

            public T Decode<T>()
            {
                XmlSerializer xs = new XmlSerializer(typeof(T));
                MemoryStream memoryStream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(Data));
                XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                return (T)xs.Deserialize(memoryStream);
            }

            public string Encode()
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(Type + " " + Data));
            }

            public override string ToString()
            {
                return "[Data: type=" + Type + " length=" + Data.Length + "]";
            }
        }

        IPEndPoint _endpoint = null;
        Socket _sock = null;            // for binding
        UdpClient _udp = null;          // for sending

        public Action<USocket, DataPacket> DataArrived;

        public USocket(int port) { _endpoint = new IPEndPoint(IPAddress.Any, port); }
        public USocket(IPEndPoint endpoint) { _endpoint = endpoint; }

        /// <summary>
        /// Bind to the port specified in the constructor, and begin receiving data (DataArrived).
        /// </summary>
        public void Bind()
        {
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sock.Bind(_endpoint);
            _sock.Blocking = true;

            if (DataArrived == null) throw new InvalidOperationException("Must attach DataArrived before calling Bind");

            new Thread(() =>
            {
                try
                {
                    byte[] buffer = new byte[_sock.ReceiveBufferSize];
                    EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    while (true)
                    {
                        int length = _sock.ReceiveFrom(buffer, ref ep);
                        _Dispatch(ep, buffer, length);
                    }
                }
                catch (SocketException ex)
                {
                    Trace.WriteLine("USocket/BindReader/Err: " + ex.Message);
                }
                finally
                {
                    _sock = null;
                }
            }).Start();
        }

        public void Send(object o) { Send(new DataPacket(o)); }
        public void Send(DataPacket data)
        {
            byte[] sendBytes = Encoding.UTF8.GetBytes(data.Encode());
            if (_udp == null)
            {
                _udp = new UdpClient();
                _udp.Client.Blocking = true;

                _udp.Send(sendBytes, sendBytes.Length, _endpoint);
                new Thread(() =>
                {
                    if (DataArrived != null)
                    {
                        try
                        {
                            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                            while (true)
                            {
                                byte[] buffer = _udp.Receive(ref ep);
                                _Dispatch(ep, buffer, buffer.Length);
                            }
                        }
                        catch (SocketException ex)
                        {
                            Trace.WriteLine("USocket/SendReader/SocketError: " + ex.Message);
                        }
                    }
                }).Start();
            }
        }

        public void Stop()
        {
            _sock.Close();
            _sock = null;
            _udp.Close();
            _udp = null;
        }

        public IPAddress IPAddress { get { return _endpoint.Address; } }

        public override string ToString()
        {
            return _endpoint.ToString();
        }

        private void _Dispatch(EndPoint ep, byte[] buffer, int length)
        {
            if (((IPEndPoint)ep).IsLocalComputer()) return;

            if (length > 0)
            {
                if (DataArrived != null)
                {
                    string line = Encoding.UTF8.GetString(buffer, 0, length).Trim();
                    try
                    {
                        line = Encoding.UTF8.GetString(Convert.FromBase64String(line));
                        int c = line.IndexOf(' ');
                        if (c > -1 && c + 1 < line.Length)
                        {
                            string type = line.Substring(0, c);
                            string data = line.Substring(++c);
                            DataPacket dp = new DataPacket(type, data);
                            DataArrived(new USocket((IPEndPoint)ep), dp);
                        }
                        else
                        {
                            Trace.WriteLine("ObjectSocket/_Dispatch/Missing Type: (msg): " + line);
                        }
                    }
                    catch (FormatException ex)
                    {
                        Trace.WriteLine("ObjectSocket/_Dispatch/FormatError (invalid frame): " + ex.Message + " (msg): " + line);
                    }
                }
            }
        }
    }
}
