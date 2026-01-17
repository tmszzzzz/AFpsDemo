using System.Collections.Generic;
using Gameplay.Players;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 负责玩家视图的 spawn / 更新。
    /// 
    /// 设计选择：
    /// - 本地玩家：使用 LocalPlayerFacade（KINEMATION 全套）。
    /// - 远端玩家：继续用你们现有 NetworkPlayerView（占位同步）。
    /// </summary>
    public sealed class ClientWorld
    {
        private static ClientWorld _instance;

        public static ClientWorld Instance => _instance;
        private readonly Transform _spawnRoot;
        private readonly Dictionary<uint, NetworkPlayerView> _remote = new();

        private LocalPlayerFacade _local;
        private uint _localPlayerId;

        public ClientWorld(Transform spawnRoot)
        {
            _instance = this;
            _spawnRoot = spawnRoot;
        }

        public LocalPlayerFacade SpawnLocal(uint playerId, LocalPlayerFacade prefab)
        {
            _localPlayerId = playerId;

            _local = Object.Instantiate(prefab, _spawnRoot);
            _local.name = $"LocalPlayer_{playerId}";
            return _local;
        }

        public NetworkPlayerView GetOrSpawnRemote(uint playerId, NetworkPlayerView prefab, ClientGame owner)
        {
            if (_remote.TryGetValue(playerId, out var v)) return v;

            var inst = Object.Instantiate(prefab, _spawnRoot);
            inst.name = $"RemotePlayer_{playerId}";
            inst.Initialize(playerId, owner);
            _remote[playerId] = inst;
            return inst;
        }

        public LocalPlayerFacade GetLocal() => _local;

        public NetworkPlayerView GetRemote(uint playerId)
        {
            _remote.TryGetValue(playerId, out var v);
            return v;
        }

        public void ApplySnapshot(Net.WorldSnapshot ws, NetworkPlayerView remotePrefab, ClientGame owner)
        {

            foreach (var p in ws.players)
            {
                if (p.playerId == _localPlayerId)
                {
                    if (_local != null)
                    {
                        _local.ApplyServerSnapshot(p);
                    }
                    continue;
                }

                var rv = GetOrSpawnRemote(p.playerId, remotePrefab, owner);
                rv.ApplySnapshot(p);
            }
        }

        public void ApplyLocalYaw(float yaw)
        {
            if (_local == null) return;
            _local.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
