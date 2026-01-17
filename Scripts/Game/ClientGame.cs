using UnityEngine;
using System.Collections.Generic;
using Net;
using Gameplay.LocalInput;
using Gameplay.Players;

namespace Game
{
    public sealed class ClientGame : MonoBehaviour
    {

        private struct PendingProjectile
        {
            public Vector3 origin;
            public Vector3 dir;
            public float spawnTime;
            public byte kind;
            public uint casterId;
            public Material material;
        }

        private readonly Dictionary<uint, PendingProjectile> _pendingProjectiles = new();

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

            var builder = new InputCommandBuilder();
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
            ProcessPendingProjectiles();

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
            var local = _world.GetLocal();
            bool isLocal = local != null && ev.casterPlayerId == PlayerId;

            switch (ev.type)
            {
                case GameEventType.WeaponFired:
                    if (isLocal) local.OnWeaponFired();
                    Debug.Log($"[GameEvent] WeaponFired caster={ev.casterPlayerId} mag={ev.u8Param0} tick={ev.serverTick}");
                    break;
                case GameEventType.WeaponReloadStarted:
                    if (isLocal) local.OnWeaponReloadStarted();
                    Debug.Log($"[GameEvent] WeaponReloadStarted caster={ev.casterPlayerId} mag={ev.u8Param0} reload={ev.f32Param0:F2} tick={ev.serverTick}");
                    break;
                case GameEventType.WeaponReloadFinished:
                    if (isLocal) local.OnWeaponReloadFinished();
                    Debug.Log($"[GameEvent] WeaponReloadFinished caster={ev.casterPlayerId} mag={ev.u8Param0} tick={ev.serverTick}");
                    break;
                case GameEventType.MeleeHit:
                    // 客户端暂无近战表现，先略过
                    Debug.Log($"[GameEvent] MeleeHit caster={ev.casterPlayerId} tick={ev.serverTick}");
                    break;
                case GameEventType.ProjectileSpawn:
                    SpawnProjectileLine(ev);
                    SpawnMuzzleFlash(ev);
                    break;
                case GameEventType.ProjectileHitWorld:
                case GameEventType.ProjectileHitActor:
                    ResolveProjectileLine(ev);
                    SpawnHitEffect(ev);
                    break;
                case GameEventType.DashStarted:
                default:
                    Debug.Log($"[GameEvent] type={ev.type} caster={ev.casterPlayerId} tick={ev.serverTick}");
                    break;
            }
        }

        private void SpawnProjectileLine(GameEvent ev)
        {
            Vector3 origin = new(ev.f32Param0, ev.f32Param1, ev.f32Param2);
            var local = _world.GetLocal();
            var remote = _world.GetRemote(ev.casterPlayerId);
            if (local != null && ev.casterPlayerId == PlayerId)
            {
                origin = local.GetMuzzlePosition(origin);
            }
            else if (remote != null)
            {
                origin = remote.GetMuzzlePosition(origin);
            }
            Vector3 dir = new(ev.f32Param3, ev.f32Param4, ev.f32Param5);
            if (dir.sqrMagnitude <= 1e-6f) return;
            dir.Normalize();

            Material mat = null;
            if (local != null && ev.casterPlayerId == PlayerId)
                mat = local.GetProjectileLineMaterial();
            else if (remote != null)
                mat = remote.GetProjectileLineMaterial();
            if (mat == null) return;

            var pending = new PendingProjectile
            {
                origin = origin,
                dir = dir,
                spawnTime = Time.time,
                kind = ev.u8Param0,
                casterId = ev.casterPlayerId,
                material = mat
            };

            if (ev.u8Param0 == 0)
            {
                _pendingProjectiles[ev.u32Param0] = pending;
                return;
            }

            DrawProjectileLine(pending, origin + dir * 15f, 0.2f);
        }


        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                if (Pool.Count > 0) return Pool.Pop();
                return new List<T>();
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }

        private void ProcessPendingProjectiles()
        {
            if (_pendingProjectiles.Count == 0) return;

            var toRemove = ListPool<uint>.Get();
            foreach (var kv in _pendingProjectiles)
            {
                var pending = kv.Value;
                if (pending.kind != 0) continue;

                if (Time.time - pending.spawnTime >= 0.1f)
                {
                    DrawProjectileLine(pending, pending.origin + pending.dir * 30f, 0.05f);
                    toRemove.Add(kv.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; ++i)
                _pendingProjectiles.Remove(toRemove[i]);
            ListPool<uint>.Release(toRemove);
        }

        private void ResolveProjectileLine(GameEvent ev)
        {
            if (!_pendingProjectiles.TryGetValue(ev.u32Param0, out var pending))
                return;

            Vector3 hitPoint = new(ev.f32Param0, ev.f32Param1, ev.f32Param2);
            DrawProjectileLine(pending, hitPoint, 0.05f);
            _pendingProjectiles.Remove(ev.u32Param0);
        }

        private void DrawProjectileLine(in PendingProjectile pending, Vector3 end, float duration)
        {
            var go = new GameObject($"ProjectileLine_{pending.casterId}_{Time.frameCount}");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = pending.material;
            lr.startWidth = 0.03f;
            lr.endWidth = 0.01f;
            lr.positionCount = 2;
            Vector3 start = Vector3.Lerp(pending.origin, end, 0.2f);
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            Destroy(go, duration);
        }

        private void SpawnMuzzleFlash(GameEvent ev)
        {
            var local = _world.GetLocal();
            var remote = _world.GetRemote(ev.casterPlayerId);

            Transform muzzle = null;
            GameObject prefab = null;

            if (local != null && ev.casterPlayerId == PlayerId)
            {
                muzzle = local.muzzle;
                prefab = local.GetMuzzleFlashEffect();
            }
            else if (remote != null)
            {
                muzzle = remote.muzzle;
                prefab = remote.GetMuzzleFlashEffect();
            }

            if (prefab == null || muzzle == null) return;

            var go = Instantiate(prefab, muzzle.position, muzzle.rotation,muzzle);
            Destroy(go, 1.5f);
        }

        private void SpawnHitEffect(GameEvent ev)
        {
            Vector3 pos = new(ev.f32Param0, ev.f32Param1, ev.f32Param2);
            Vector3 normal = new(ev.f32Param3, ev.f32Param4, ev.f32Param5);
            var local = _world.GetLocal();
            var remote = _world.GetRemote(ev.casterPlayerId);
            GameObject prefab = null;
            if (local != null && ev.casterPlayerId == PlayerId)
                prefab = local.GetProjectileHitEffect();
            else if (remote != null)
                prefab = remote.GetProjectileHitEffect();
            if (prefab == null) return;
            Quaternion rot = normal.sqrMagnitude > 1e-6f ? Quaternion.FromToRotation(Vector3.up, normal) : Quaternion.identity;
            var go = Instantiate(prefab, pos, rot);
            Destroy(go, 2.0f);
        }
    }
}
