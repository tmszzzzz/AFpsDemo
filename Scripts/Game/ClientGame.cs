using System;
using System.Collections.Generic;
using UnityEngine;
using Net;

namespace Game
{
    public class ClientGame : MonoBehaviour
    {
        [Header("Server Config")] public string ServerHost = "127.0.0.1";
        public int TcpPort = 5000;
        public int UdpPort = 5001;
        public string PlayerName = "Player";

        [Header("Player View Prefab")] public NetworkPlayerView PlayerViewPrefab;

        public uint PlayerId { get; private set; }
        public bool IsJoined { get; private set; }
        public uint LastPingMs { get; private set; }
        public uint LastServerTime { get; private set; }
        public string LastError => _netClient.LastError;

        private NetClient _netClient;
        private NetMessageDispatcher _dispatcher;

        // ==== M3 3.2 本地输入状态 ====
        private ushort _inputSeq = 0;
        private uint _clientTick = 0;

        // ==== M3 3.3 网络玩家视图表 ====
        private readonly Dictionary<uint, NetworkPlayerView> _playerViews
            = new Dictionary<uint, NetworkPlayerView>();

        private void Awake()
        {
            _netClient = new NetClient();
            _dispatcher = new NetMessageDispatcher(this);
        }

        private void Start()
        {
            var jr = new JoinRequest
            {
                protocolVersion = 1,
                playerName = string.IsNullOrEmpty(PlayerName) ? "Player" : PlayerName,
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
                var msg = ProtoSerializer.EncodePing(ping);
                _netClient.SendUdp(msg);
            }

            // ==== M3 3.2：采集本地输入并发送 InputCommand ====
            if (IsJoined)
            {
                _clientTick++;

                // 1. 读取 WASD 轴输入
                float moveX = Input.GetAxis("Horizontal");
                float moveY = Input.GetAxis("Vertical");

                // 2. 从主摄像机读取 yaw/pitch（简化：直接用欧拉角）
                float yaw = 0f;
                float pitch = 0f;
                var cam = Camera.main;
                if (cam != null)
                {
                    var euler = cam.transform.rotation.eulerAngles;
                    yaw = euler.y;
                    float rawPitch = euler.x;
                    if (rawPitch > 180f) rawPitch -= 360f; // 转为 [-180,180]，便于与服务器约定
                    pitch = rawPitch;
                }

                // 3. 按键 bitmask（这里只做一个最小集，后续可扩展）
                uint buttonMask = 0;
                if (Input.GetButton("Jump"))
                    buttonMask |= InputButtons.BUTTON_JUMP;
                if (Input.GetKey(KeyCode.E))
                    buttonMask |= InputButtons.BUTTON_SKILL_E;
                if (Input.GetKey(KeyCode.LeftShift))
                    buttonMask |= InputButtons.BUTTON_SKILL_SHIFT;

                // 4. 组装 InputCommand
                var ic = new InputCommand
                {
                    playerId = PlayerId,
                    seq = _inputSeq++,
                    clientTick = _clientTick,
                    moveX = moveX,
                    moveY = moveY,
                    yaw = yaw,
                    pitch = pitch,
                    buttonMask = buttonMask,
                };

                // 5. 通过 NetClient 发送（UDP）
                _netClient.SendInputCommand(ic);
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

            LastPingMs = rtt;
            LastServerTime = pong.serverTime;
        }

        // ==== M3 3.3：处理 WorldSnapshot，更新所有玩家视图 ====
        public void OnWorldSnapshot(WorldSnapshot ws)
        {
            if (ws.players == null || ws.players.Length == 0)
                return;

            foreach (var p in ws.players)
            {
                if (!_playerViews.TryGetValue(p.playerId, out var view) || view == null)
                {
                    NetworkPlayerView newView = null;

                    if (PlayerViewPrefab != null)
                    {
                        newView = Instantiate(PlayerViewPrefab);
                    }
                    else
                    {
                        // 没有配置 prefab 的情况下，临时用一个 Capsule 占位
                        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        newView = go.AddComponent<NetworkPlayerView>();
                    }

                    newView.Initialize(p.playerId, this);
                    _playerViews[p.playerId] = newView;
                    view = newView;
                }

                view.ApplySnapshot(p);
            }

            // 简单版本先不处理“离线玩家”的清理；后续可根据 ws.players 做差集删掉多余视图
        }

        public void OnGameEvent(GameEvent ev)
        {
            // 1) 最小验收：先打印
            Debug.Log($"[GameEvent] type={ev.type} caster={ev.casterPlayerId} tick={ev.serverTick}");

            // 2) 后续扩展：分发给对应的玩家视图 / 特效系统 / UI 系统
            if (_playerViews.TryGetValue(ev.casterPlayerId, out var view) && view != null)
            {
                switch (ev.type)
                {
                    case GameEventType.DashStarted:
                        break;

                    default:
                        break;
                }
            }
        }
    }
}