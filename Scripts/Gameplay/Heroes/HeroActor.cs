using Gameplay.Heroes;
using Gameplay.Movement.Core;
using UnityEngine;

namespace Gameplay.Heroes
{
    [RequireComponent(typeof(CharacterController))]
    public class HeroActor : MonoBehaviour
    {
        [Header("Hero Setup")]
        public HeroId heroId = HeroId.Generic;
        public HeroConfig heroConfig;

        [Header("View (optional, for local player)")]
        public Transform cameraPivot;  // 相机旋转枢轴
        public Camera   playerCamera;  // 本地玩家用

        private CharacterController _controller;
        private HeroCore            _hero;

        public HeroCore Core => _hero;   // 暴露给其他脚本（输入/技能）

        public bool IsLocalPlayer { get; set; } = true; // 以后网络化时用

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            _hero = new HeroCore(
                heroId,
                spawnPos: transform.position,
                config: heroConfig);

            if (IsLocalPlayer && playerCamera != null && cameraPivot != null)
            {
                playerCamera.transform.SetParent(cameraPivot, worldPositionStays: false);
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1. 让 HeroCore 跑一帧运动逻辑（这里只是算位移）
            _hero.TickMovement(dt, out MovementCommand cmd, out Vector3 desiredDisplacement);

            // 2. 用 CharacterController 做碰撞，并把结果回写到 MovementState
            if (_controller != null)
            {
                _controller.Move(desiredDisplacement);

                _hero.State.Movement.Position   = transform.position;
                _hero.State.Movement.IsGrounded = _controller.isGrounded;
            }
            else
            {
                // 没有 CharacterController 的退化路径（无碰撞）
                _hero.State.Movement.Position += desiredDisplacement;
                transform.position             = _hero.State.Movement.Position;
                _hero.State.Movement.IsGrounded = false;
            }

            // 3. 应用旋转到角色与相机
            transform.rotation = Quaternion.Euler(0f, _hero.State.Movement.Yaw, 0f);

            if (IsLocalPlayer && cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(_hero.State.Movement.Pitch, 0f, 0f);
            }
        }
    }
}
