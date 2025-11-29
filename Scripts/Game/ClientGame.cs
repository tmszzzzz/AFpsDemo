using System;
using UnityEngine;
using Net;

namespace Game
{
    public class ClientGame : MonoBehaviour
    {
        [Header("Server Config")]
        public string ServerHost = "127.0.0.1";
        public int    TcpPort    = 5000;
        public int    UdpPort    = 5001;
        public string PlayerName = "Player";

        public uint   PlayerId      { get; private set; }
        public bool   IsJoined      { get; private set; }
        public uint   LastPingMs    { get; private set; }
        public uint   LastServerTime{ get; private set; }
        public string LastError     => _netClient.LastError;

        private NetClient           _netClient;
        private NetMessageDispatcher _dispatcher;

        private void Awake()
        {
            _netClient  = new NetClient();
            _dispatcher = new NetMessageDispatcher(this);
        }

        private void Start()
        {
            var jr = new JoinRequest
            {
                protocolVersion = 1,
                playerName      = string.IsNullOrEmpty(PlayerName) ? "Player" : PlayerName,
            };

            _netClient.OnMessageReceived += OnRawMessage;
            _netClient.Connect(ServerHost, TcpPort, UdpPort, jr);
        }

        private void Update()
        {
            _netClient.PumpMessages();

            // 定时发送 Ping（例如每 1 秒）
            _pingTimer += Time.deltaTime;
            if (_pingTimer >= 1.0f && IsJoined)
            {
                _pingTimer = 0f;

                uint clientTime = (uint)(Time.realtimeSinceStartup * 1000.0f);
                var ping = new Net.Ping { clientTime = clientTime };
                var msg  = ProtoSerializer.EncodePing(ping);
                _netClient.SendUdp(msg);
            }
        }

        private float _pingTimer = 0f;

        private void OnDestroy()
        {
            _netClient?.Shutdown();
        }

        private void OnRawMessage(NetMessage msg)
        {
            _dispatcher.Dispatch(msg);
        }

        public void OnJoinAccept(JoinAccept ja)
        {
            PlayerId = ja.playerId;
            IsJoined = true;
            
            // 建立 UDP 绑定：告知服务器此 UDP socket 属于哪个 playerId
            _netClient.SendUdpBind(PlayerId);
            
            Debug.Log($"Joined server as player {PlayerId}, serverProto={ja.serverProtocolVersion}");
        }

        public void OnPong(Pong pong)
        {
            uint nowClientTime = (uint)(Time.realtimeSinceStartup * 1000.0f);
            uint rtt = nowClientTime - pong.clientTime;

            LastPingMs     = rtt;
            LastServerTime = pong.serverTime;
        }
    }
}