using UnityEngine;
using Net;
using Gameplay.LocalInput;
using Gameplay.Players;

namespace Game
{
    public sealed class ClientGame : MonoBehaviour
    {
        [Header("Server Config")]
        public string ServerHost = "127.0.0.1";
        public int TcpPort = 5000;
        public int UdpPort = 5001;
        public string PlayerName = "Player";

        [Header("Prefabs")]
        public LocalPlayerFacade LocalPlayerPrefab;
        public NetworkPlayerView RemotePlayerPrefab;
        public Transform SpawnRoot;

        [Header("Input")]
        public LocalInputSampler inputSampler;
        public bool sendWeaponButtonsToServer = false;

        public uint PlayerId { get; private set; }
        public bool IsJoined { get; private set; }
        public uint LastPingMs { get; private set; }
        public uint LastServerTime { get; private set; }

        private NetClient _netClient;
        private NetMessageDispatcher _dispatcher;

        private ClientWorld _world;
        private InputCommandSender _sender;
        public string LastError => _netClient.LastError;

        private void Awake()
        {
            if (SpawnRoot == null) SpawnRoot = transform;
            if (inputSampler == null) inputSampler = FindObjectOfType<LocalInputSampler>();

            _netClient = new NetClient();
            _netClient.OnMessageReceived += OnRawMessage;

            _dispatcher = new NetMessageDispatcher(this);

            _world = new ClientWorld(SpawnRoot);

            var builder = new InputCommandBuilder { sendWeaponButtonsToServer = sendWeaponButtonsToServer };
            _sender = new InputCommandSender(_netClient, inputSampler, builder);
        }

        private void Start()
        {
            var jr = new JoinRequest { playerName = PlayerName };
            _netClient.Connect(ServerHost, TcpPort, UdpPort, jr);
        }

        private void Update()
        {
            _netClient.PumpMessages();

            if (!IsJoined) return;
            
            // 定时发送 Ping
            _pingTimer += Time.deltaTime;
            if (_pingTimer >= 1.0f && IsJoined)
            {
                _pingTimer = 0f;

                uint clientTime = (uint)(Time.realtimeSinceStartup * 1000.0f);
                var ping = new Net.Ping { clientTime = clientTime };
                var msg = ProtoSerializer.EncodePing(ping);
                _netClient.SendUdp(msg);
            }

            // 1) 输入 -> 本地表现 + 发包
            _sender.Tick(Time.deltaTime);
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

        // ====== NetMessageDispatcher callbacks ======

        public void OnJoinAccept(JoinAccept ja)
        {
            PlayerId = ja.playerId;
            IsJoined = true;
            _netClient.SendUdpBind(PlayerId);

            // spawn local
            var local = _world.SpawnLocal(PlayerId, LocalPlayerPrefab);
            _sender.SetPlayerId(PlayerId);
            _sender.BindLocalPlayer(local);
        }

        public void OnPong(Pong pong)
        {
            uint nowClientTime = (uint)(Time.realtimeSinceStartup * 1000.0f);
            uint rtt = nowClientTime - pong.clientTime;

            LastPingMs = rtt;
            LastServerTime = pong.serverTime;
        }

        public void OnWorldSnapshot(WorldSnapshot ws)
        {
            // 远端占位更新
            _world.ApplySnapshot(ws, RemotePlayerPrefab, this);
        }

        public void OnGameEvent(GameEvent ev)
        {
            // 1) 最小验收：先打印
            Debug.Log($"[GameEvent] type={ev.type} caster={ev.casterPlayerId} tick={ev.serverTick}");
        }
    }
}