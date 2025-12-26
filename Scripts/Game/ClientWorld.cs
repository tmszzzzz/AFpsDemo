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
        private readonly Transform _spawnRoot;
        private readonly Dictionary<uint, NetworkPlayerView> _remote = new();

        private LocalPlayerFacade _local;
        private uint _localPlayerId;

        public ClientWorld(Transform spawnRoot)
        {
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

        public void ApplySnapshot(Net.WorldSnapshot ws, NetworkPlayerView remotePrefab, ClientGame owner)
        {
            // 你们现有的 WorldSnapshot 解码结构在 Net 侧。
            // 这里按 client.md 的惯例：ws.players 是列表，每个含 playerId/position/yaw/pitch。

            foreach (var p in ws.players)
            {
                if (p.playerId == _localPlayerId)
                {
                    // 本地玩家：一般不做 transform 回放（避免与本地摄像机/武器表现冲突）。
                    // 但你们可以在此做 debug 校验或做“服务器校正”。
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