using Gameplay.LocalInput;
using Gameplay.ThirdPartyAdapters.Kinemation;
using UnityEngine;

namespace Gameplay.Players
{
    /// <summary>
    /// 本地玩家壳：把输入映射到相机/第三方表现控制器。
    /// </summary>
    public sealed class LocalPlayerFacade : MonoBehaviour
    {
        [Header("Camera")]
        public Transform cameraPivot;          // 你们的 camera pivot（推荐）
        public bool driveCameraRotation = true;

        [Header("Third-party")]
        public KinemationController kinemation;

        private void Reset()
        {
            if (kinemation == null) kinemation = GetComponentInChildren<KinemationController>();
        }

        public void ApplyLocalInput(in LocalInputFrame f)
        {
            if (driveCameraRotation && cameraPivot != null)
            {
                // 你们现有是 yaw 作用于角色根，pitch 作用于 cameraPivot。
                // 这里仅更新 pitch；yaw 由 ClientWorld/本地根对象转向决定（见 ClientWorld）。
                cameraPivot.localRotation = Quaternion.Euler(f.pitch, 0f, 0f);
            }

            if (kinemation != null)
            {
                kinemation.SetAiming(f.Has(LocalButtons.Aim));
                kinemation.SetFireHeld(f.Has(LocalButtons.Fire));

                if (f.Has(LocalButtons.Reload))
                    kinemation.Reload();
            }
        }
    }
}