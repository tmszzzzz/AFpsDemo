// ==== NEW FILE: Game/NetworkPlayerView.cs ====
using Net;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 纯粹由服务器 WorldSnapshot 驱动的“网络玩家视图”。
    /// 当前版本不做插值/预测，只做位置与朝向的瞬时同步。
    /// </summary>
    public class NetworkPlayerView : MonoBehaviour
    {
        public uint PlayerId { get; private set; }

        // 如果需要区分本地玩家，可通过 Owner.PlayerId == PlayerId 判断
        public ClientGame Owner { get; private set; }

        public bool IsLocalPlayer =>
            Owner != null && Owner.PlayerId == PlayerId;

        [Header("Projectile FX")]
        public Material projectileLineMaterial;
        public GameObject projectileHitEffectPrefab;
        public GameObject muzzleFlashPrefab;
        public Transform muzzle;

        public void Initialize(uint playerId, ClientGame owner)
        {
            PlayerId = playerId;
            Owner    = owner;

            gameObject.name = $"PlayerView_{playerId}";
        }

        /// <summary>
        /// 最简单版本：直接将 transform 对齐到 snapshot。
        /// 以后可以在这里加插值 / 状态同步 / 动画驱动等。
        /// </summary>
        public void ApplySnapshot(PlayerSnapshot p)
        {
            transform.position = new Vector3(p.posX, p.posY, p.posZ);
            transform.rotation = Quaternion.Euler(0f, p.yaw, 0f);

            // 这里暂时不处理 vel/状态/血量等，将来可拓展：
            // - 根据 locomotionState 切换跑/站动画
            // - 显示血条等 UI
        }

        public Material GetProjectileLineMaterial() => projectileLineMaterial;
        public GameObject GetProjectileHitEffect() => projectileHitEffectPrefab;
        public GameObject GetMuzzleFlashEffect() => muzzleFlashPrefab;
        public Vector3 GetMuzzlePosition(Vector3 fallback) => muzzle != null ? muzzle.position : fallback;

        // 3.4 的 Dash 播放钩子会放在这里，目前按要求暂时不实现 PlayDash()
        // public void PlayDash() { ... }
    }
}
