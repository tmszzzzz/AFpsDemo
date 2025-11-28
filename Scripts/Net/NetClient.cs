using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Net
{
    public class NetClient
    {
        public bool   IsConnected => _tcpClient != null && _tcpClient.Connected;
        public string LastError   { get; private set; }

        private TcpClient         _tcpClient;
        private NetworkStream     _tcpStream;
        private UdpClient         _udpClient;
        private IPEndPoint        _udpRemoteEndPoint;
        private Thread            _tcpThread;
        private Thread            _udpThread;
        private volatile bool     _running;

        private readonly ConcurrentQueue<NetMessage> _recvQueue = new();

        public event Action<NetMessage> OnMessageReceived;

        public void Connect(string host, int tcpPort, int udpPort, JoinRequest joinReq)
        {
            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(host, tcpPort);
                _tcpStream = _tcpClient.GetStream();

                // 发送 JoinRequest
                var joinMsg = ProtoSerializer.EncodeJoinRequest(joinReq);
                var bytes   = ProtoSerializer.BuildPacket(joinMsg);
                _tcpStream.Write(bytes, 0, bytes.Length);

                // UDP
                _udpClient = new UdpClient();
                _udpClient.Connect(host, udpPort);
                _udpRemoteEndPoint = new IPEndPoint(IPAddress.Parse(host), udpPort);

                _running = true;
                _tcpThread = new Thread(TcpRecvLoop) { IsBackground = true };
                _udpThread = new Thread(UdpRecvLoop) { IsBackground = true };
                _tcpThread.Start();
                _udpThread.Start();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _running  = false;
            }
        }

        public void Shutdown()
        {
            _running = false;
            try { _tcpClient?.Close(); } catch { }
            try { _udpClient?.Close(); } catch { }
        }

        public void SendTcp(NetMessage msg)
        {
            if (_tcpStream == null || !_tcpStream.CanWrite) return;
            var bytes = ProtoSerializer.BuildPacket(msg);
            try
            {
                _tcpStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        public void SendUdp(NetMessage msg)
        {
            if (_udpClient == null) return;
            var bytes = ProtoSerializer.BuildPacket(msg);
            try
            {
                _udpClient.Send(bytes, bytes.Length);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }

        public void PumpMessages()
        {
            while (_recvQueue.TryDequeue(out var msg))
            {
                OnMessageReceived?.Invoke(msg);
            }
        }

        private void TcpRecvLoop()
        {
            var headerBuf = new byte[8];
            try
            {
                while (_running)
                {
                    // 读 header
                    int read = 0;
                    while (read < 8)
                    {
                        int r = _tcpStream.Read(headerBuf, read, 8 - read);
                        if (r <= 0) throw new Exception("TCP closed");
                        read += r;
                    }

                    if (!ProtoSerializer.DecodeHeader(headerBuf, out var header))
                        throw new Exception("Invalid header");

                    int payloadSize = header.length - 8;
                    byte[] payload = payloadSize > 0 ? new byte[payloadSize] : Array.Empty<byte>();
                    int pRead = 0;
                    while (pRead < payloadSize)
                    {
                        int r = _tcpStream.Read(payload, pRead, payloadSize - pRead);
                        if (r <= 0) throw new Exception("TCP closed");
                        pRead += r;
                    }

                    _recvQueue.Enqueue(new NetMessage { Header = header, Payload = payload });
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _running  = false;
            }
        }

        private void UdpRecvLoop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (_running && _udpClient != null)
                {
                    byte[] buf = _udpClient.Receive(ref remote);
                    if (buf == null || buf.Length < 8) continue;
                    if (!ProtoSerializer.DecodeHeader(buf, out var header)) continue;

                    int payloadSize = header.length - 8;
                    if (payloadSize <= 0 || payloadSize > buf.Length - 8) payloadSize = 0;
                    byte[] payload = payloadSize > 0 ? new byte[payloadSize] : Array.Empty<byte>();
                    if (payloadSize > 0)
                    {
                        Buffer.BlockCopy(buf, 8, payload, 0, payloadSize);
                    }

                    _recvQueue.Enqueue(new NetMessage { Header = header, Payload = payload });
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _running  = false;
            }
        }
    }
}