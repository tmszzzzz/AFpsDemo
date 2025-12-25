using UnityEngine;

namespace Gameplay.Heroes.View
{
    /// <summary>
    /// Hero Prefab 上的序列化引用集合（只做绑定，不写逻辑）。
    /// </summary>
    public sealed class HeroViewBindings : MonoBehaviour
    {
        [Header("Core")]
        public HeroActor heroActor;

        [Header("Character Animator")]
        public Animator characterAnimator;
        [Tooltip("上半身层，用于 Fire/Reload。若不用分层，填 -1。")]
        public int upperBodyLayer = -1;

        [Header("Weapon Animator (optional)")]
        public Animator weaponAnimator;
        [Tooltip("武器 Animator 上用于短动作的 layer，若不用分层，填 -1。")]
        public int weaponLayer = -1;

        [Header("View Pivot")]
        public Transform cameraPivot; // 必须与 HeroActor.cameraPivot 同一个

        [Header("Weapon Transforms")]
        public Transform weaponRoot;
        public Transform muzzle;
        public Transform gripL;
        public Transform gripR;

        [Header("Animation Rigging Targets")]
        public Transform leftHandTarget;
        public Transform rightHandTarget;
        public Transform aimTarget;

        [Header("Recoil")]
        public Transform recoilPivot; // 建议在 weaponRoot 下的一个空节点

        [Header("动画状态名（最小集，按资源改名）")]
        public string stateIdle = "Idle";
        public string stateLocomotion = "Locomotion";
        public string stateAim = "Aim";
        public string stateFire = "Fire";
        public string stateReload = "Reload";

        [Header("Animator 参数名（可选，按你们 Controller 改）")]
        public string paramSpeed = "Speed";
        public string paramGrounded = "Grounded";
        public string paramAiming = "Aiming";

        private void Reset()
        {
            if (heroActor == null) heroActor = GetComponent<HeroActor>();
            if (cameraPivot == null && heroActor != null) cameraPivot = heroActor.cameraPivot;
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
        }
    }
}