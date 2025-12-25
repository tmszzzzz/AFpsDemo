using Gameplay.Heroes.Ik;
using Gameplay.Heroes.Weapons;
using UnityEngine;

namespace Gameplay.Heroes.View
{
    /// <summary>
    /// Hero Prefab 上唯一的“表现驱动”组件（纯本地临时输入版）：
    /// - 读取 HeroCore.State（速度/在地）驱动 Animator 参数
    /// - 读取本地输入（Fire/Reload/Aim）驱动短动作点播与后坐力
    /// - 驱动 Animation Rigging 的目标点（手握把、AimTarget）
    /// - 后坐力只写 recoilPivot，不影响 HeroActor 的 yaw/pitch 约束
    /// </summary>
    [DefaultExecutionOrder(200)] // 保证晚于 HeroActor(默认0) 更新后再驱动 view
    public sealed class HeroViewDriver : MonoBehaviour
    {
        [SerializeField] private HeroViewBindings b;

        [Header("Aim")] [SerializeField] private float aimDistance = 50f;
        [SerializeField] private LayerMask aimMask = ~0;

        [Header("Weapon Timing")] [SerializeField]
        private float fireInterval = 0.12f;

        [SerializeField] private float reloadDuration = 2.0f;
        [SerializeField] private bool fullAuto = true;

        [Header("Recoil")] [SerializeField] private float kickPitchDeg = 2.0f;
        [SerializeField] private float kickYawDeg = 0.5f;
        [SerializeField] private float returnSpeed = 18f;
        [SerializeField] private float damping = 22f;

        private FpsIkRigDriver _ik;
        private WeaponRuntime _weapon;
        private WeaponAnimatorBridge _weaponAnim;
        private RecoilSpring _recoil;

        public struct LocalViewInput
        {
            public bool fireDown;
            public bool fireHeld;
            public bool fireUp;
            public bool reload;
            public bool aim;
        }

// 本地输入（由 LocalFpsDemoInput 喂入；后续可替换为网络/事件桥接）
        private LocalViewInput _local;
        private bool _hasLocalInput;

        private void Awake()
        {
            if (b == null) b = GetComponent<HeroViewBindings>();
            if (b == null)
            {
                Debug.LogError("HeroViewDriver requires HeroViewBindings.");
                enabled = false;
                return;
            }

            if (b.heroActor == null) b.heroActor = GetComponent<Heroes.HeroActor>();
            if (b.cameraPivot == null && b.heroActor != null) b.cameraPivot = b.heroActor.cameraPivot;

            _ik = new FpsIkRigDriver(
                b.cameraPivot,
                b.muzzle,
                b.gripL,
                b.gripR,
                new IkTargets(b.leftHandTarget, b.rightHandTarget, b.aimTarget),
                aimDistance,
                aimMask);

            _weapon = new WeaponRuntime(fireInterval, reloadDuration, fullAuto);

            _weaponAnim = new WeaponAnimatorBridge(
                b.characterAnimator, b.upperBodyLayer,
                b.weaponAnimator, b.weaponLayer,
                b.stateFire, b.stateReload);

            _recoil = new RecoilSpring(b.recoilPivot, kickPitchDeg, kickYawDeg, returnSpeed, damping);
        }

        /// <summary>
        /// 由 ClientGame 在发送 InputCommand 后立即调用：用于本地即时表现（Fire/Reload）。
        /// </summary>
        /// <summary>
        /// 由纯本地临时输入脚本调用：用于驱动 Fire/Reload/Aim 的即时表现。
        /// </summary>
        public void ApplyLocalInput(in LocalViewInput input)
        {
            _local = input;
            _hasLocalInput = true;
        }

        private void LateUpdate()
        {
            if (b.heroActor == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1) IK 目标点（依赖 cameraPivot 已被 HeroActor 更新）
            _ik.Tick();

            // 2) Animator 参数（locomotion）
            ApplyLocomotionParams();

            // 3) 本地按钮驱动 Fire/Reload（MVP：只看本地转发；网络化后可改为 server event）
            bool fireHeld = false;
            bool fireDown = false;
            bool reloadPressed = false;

            if (_hasLocalInput)
            {
                fireDown = _local.fireDown;
                fireHeld = _local.fireHeld;
                reloadPressed = _local.reload;
                // aiming 参数如果你们 Animator 需要，可在 ApplyLocomotionParams 里用 _local.aim 写入
            }

            var o = _weapon.Tick(dt, fireHeld, fireDown, reloadPressed);
            if (o.firedThisTick)
            {
                _weaponAnim.PlayFire();
                _recoil.Kick();
            }

            if (o.action == WeaponRuntime.WeaponAction.Reload)
            {
                _weaponAnim.PlayReload();
            }

            // 4) 后坐力收敛
            _recoil.Tick(dt);

            // 5) 清掉本地输入（保证 fireDown/reload 等“事件键”只生效一帧）
            _hasLocalInput = false;
        }

        private void ApplyLocomotionParams()
        {
            if (b.characterAnimator == null) return;

            var s = b.heroActor.Core.State.Movement;
            float speed = new Vector2(s.Velocity.x, s.Velocity.z).magnitude;

            if (!string.IsNullOrEmpty(b.paramSpeed))
                b.characterAnimator.SetFloat(b.paramSpeed, speed);

            if (!string.IsNullOrEmpty(b.paramGrounded))
                b.characterAnimator.SetBool(b.paramGrounded, s.IsGrounded);

            // aiming 参数（若你们 Animator 需要）先用本地输入驱动
            if (!string.IsNullOrEmpty(b.paramAiming))
                b.characterAnimator.SetBool(b.paramAiming, _hasLocalInput && _local.aim);
        }
    }
}