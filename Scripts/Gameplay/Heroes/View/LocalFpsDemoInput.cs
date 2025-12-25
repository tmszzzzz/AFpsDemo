using UnityEngine;

namespace Gameplay.Heroes.View
{
    /// <summary>
    /// 纯本地临时输入：
    /// - 不依赖 ClientGame/Net/InputCommand
    /// - 只负责把 Unity 输入喂给 HeroViewDriver
    /// 
    /// 你们后续接网络/预测时：删掉这个脚本，换成“从网络/事件喂输入”的桥接即可。
    /// </summary>
    public sealed class LocalFpsDemoInput : MonoBehaviour
    {
        [SerializeField] private HeroViewDriver view;

        [Header("Buttons")]
        [SerializeField] private KeyCode reloadKey = KeyCode.R;
        [SerializeField] private int fireMouseButton = 0;
        [SerializeField] private int aimMouseButton = 1;

        private void Reset()
        {
            if (view == null) view = GetComponentInChildren<HeroViewDriver>();
        }

        private void Update()
        {
            if (view == null) return;

            var input = new HeroViewDriver.LocalViewInput
            {
                fireDown = Input.GetMouseButtonDown(fireMouseButton),
                fireHeld = Input.GetMouseButton(fireMouseButton),
                fireUp = Input.GetMouseButtonUp(fireMouseButton),
                reload = Input.GetKeyDown(reloadKey),
                aim = Input.GetMouseButton(aimMouseButton)
            };

            view.ApplyLocalInput(input);
        }
    }
}