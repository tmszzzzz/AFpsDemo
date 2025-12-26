using UnityEngine;

// 直接引用第三方类型（你已导入 KINEMATION）
using KINEMATION.FPSAnimationPack.Scripts.Player;
using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;

namespace Gameplay.ThirdPartyAdapters.Kinemation
{
    /// <summary>
    /// 第三方适配入口：把“你们的输入/网络事件”转换为对 KINEMATION 的调用。
    /// 
    /// 两种使用方式：
    /// A) fpsPlayer.enabled = true：KINEMATION 自己更新 gait/层权重等（但在 Legacy 路径下它也会读 Input）。
    /// B) fpsPlayer.enabled = false：你们只用它的武器/反冲/声音（推荐先做，最少冲突）。
    /// </summary>
    public sealed class KinemationController : MonoBehaviour
    {
        [Header("References")]
        public FPSPlayer fpsPlayer;
        public FPSPlayerSound playerSound;

        [Tooltip("可选：直接指定武器；不指定则用 fpsPlayer.GetActiveWeapon()")]
        public FPSWeapon explicitWeapon;

        [Header("Aim")]
        public bool driveAimSound = true;

        // RecoilAnimation 来自 KINEMATION.ProceduralRecoilAnimationSystem.Runtime
        // 为了避免命名空间变动导致编译失败，这里用 Component + 反射设置 isAiming。
        public Component recoilAnimation;

        private bool _fireHeld;
        private bool _aiming;

        private void Reset()
        {
            if (fpsPlayer == null) fpsPlayer = GetComponentInChildren<FPSPlayer>();
            if (playerSound == null) playerSound = GetComponentInChildren<FPSPlayerSound>();
        }

        private FPSWeapon GetWeapon()
        {
            if (explicitWeapon != null) return explicitWeapon;
            if (fpsPlayer != null) return fpsPlayer.GetActiveWeapon();
            return null;
        }

        public void SetAiming(bool aiming)
        {
            if (_aiming == aiming) return;
            _aiming = aiming;

            if (driveAimSound && playerSound != null)
                playerSound.PlayAimSound(_aiming);

            // best-effort: recoilAnimation.isAiming = aiming
            if (recoilAnimation != null)
            {
                var t = recoilAnimation.GetType();
                var p = t.GetProperty("isAiming");
                if (p != null && p.CanWrite)
                {
                    p.SetValue(recoilAnimation, aiming);
                }
                else
                {
                    var f = t.GetField("isAiming");
                    if (f != null) f.SetValue(recoilAnimation, aiming);
                }
            }
        }

        public void SetFireHeld(bool held)
        {
            if (_fireHeld == held) return;
            _fireHeld = held;

            var w = GetWeapon();
            if (w == null) return;

            if (_fireHeld)
                w.OnFirePressed();
            else
                w.OnFireReleased();
        }

        public void Reload()
        {
            var w = GetWeapon();
            if (w == null) return;
            w.OnReload();
        }

        public void ChangeFireMode()
        {
            var w = GetWeapon();
            if (w == null) return;
            w.OnFireModeChange();
            if (playerSound != null) playerSound.PlayFireModeSwitchSound();
        }

        /*
         此方法疑似不存在
        public void Inspect()
        {
            var w = GetWeapon();
            if (w == null) return;
            w.OnInspect();
        }
        */
    }
}