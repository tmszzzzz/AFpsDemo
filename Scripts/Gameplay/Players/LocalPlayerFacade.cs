using Game;
using Gameplay.LocalInput;
using Gameplay.Movement.Core;
using Gameplay.ThirdPartyAdapters.Kinemation;
using Net;
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
        
        
        /// <summary>
        /// 服务器权威回放：本地与远端一致，都由快照驱动移动/朝向。
        /// 字段名以你们 Net.WorldSnapshot 的 player 元素为准。
        /// </summary>
        public void ApplyServerSnapshot(PlayerSnapshot p)
        {
            transform.position = new Vector3(p.posX, p.posY, p.posZ);
            if(!driveCameraRotation)
            {
                transform.rotation = Quaternion.Euler(0f, p.yaw, 0f);
                if (cameraPivot != null)
                {
                    cameraPivot.localRotation = Quaternion.Euler(p.pitch, 0f, 0f);
                }
            }
        }

        public void ApplyLocalInput(in LocalInputFrame f)
        {
            if (driveCameraRotation && cameraPivot != null)
            {
                if (kinemation != null) kinemation.SetPitch(f.pitch);
                ClientWorld.Instance.ApplyLocalYaw(f.yaw);
            }

            if (kinemation != null)
            {
                kinemation.SetAiming(f.Has(LocalButtons.MOUSE_FIRE_SEC));
                kinemation.SetFireHeld(f.Has(LocalButtons.MOUSE_FIRE_PRI));

                if (f.Has(LocalButtons.BUTTON_RELOAD))
                    kinemation.Reload();
            }
        }
    }
}