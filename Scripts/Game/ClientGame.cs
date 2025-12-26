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

            // 1) 输入 -> 本地表现 + 发包
            _sender.Tick(Time.deltaTime);

            // 2) 把 yaw 同步到本地玩家根（你们原先是用 Camera.main.eulerAngles.y）
            //    现在改用 inputSampler 的绝对 yaw。
            if (inputSampler != null)
            {
                var frame = inputSampler.Sample();
                _world.ApplyLocalYaw(frame.yaw);
            }
        }

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

            // spawn local
            var local = _world.SpawnLocal(PlayerId, LocalPlayerPrefab);
            _sender.SetPlayerId(PlayerId);
            _sender.BindLocalPlayer(local);
        }

        public void OnPong(Pong pong)
        {
            LastServerTime = pong.serverTime;
            // ping 计算逻辑你们原先在 ClientGame 里有，可按需迁移。
        }

        public void OnWorldSnapshot(WorldSnapshot ws)
        {
            // 远端占位更新
            _world.ApplySnapshot(ws, RemotePlayerPrefab, this);
        }

        public void OnGameEvent(GameEvent ev)
        {
            // 未来扩展：把服务器 game event 映射到本地/远端表现
        }
    }
}