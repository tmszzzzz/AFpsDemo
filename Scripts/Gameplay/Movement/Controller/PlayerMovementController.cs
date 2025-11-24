

using Gameplay.Movement.Core;
using Gameplay.Movement.Sources;
using UnityEngine;

namespace Gameplay.Movement.Controller
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera   playerCamera;

        [Header("World Physics")]
        [SerializeField] private float gravity = 20f;

        [Header("View Limits")]
        [SerializeField] private float minPitch = -89f;
        [SerializeField] private float maxPitch = 89f;

        [Header("Local Move Settings")]
        [SerializeField] private float baseMoveSpeed       = 6f;
        [SerializeField] private float yawSensitivityDeg   = 2f;
        [SerializeField] private float pitchSensitivityDeg = 2f;
        [SerializeField] private float jumpSpeed           = 9f;

        private CharacterController      _controller;
        private CharacterMotor           _motor;
        private MovementSourceCollection _movementSources;
        private LocalInputMovementSource _localInputSource;

        [SerializeField] private PlayerState _state;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            _motor = new CharacterMotor
            {
                Gravity  = gravity,
                MinPitch = minPitch,
                MaxPitch = maxPitch
            };

            _movementSources = new MovementSourceCollection();

            _localInputSource = new LocalInputMovementSource
            {
                BaseMoveSpeed       = baseMoveSpeed,
                YawSensitivityDeg   = yawSensitivityDeg,
                PitchSensitivityDeg = pitchSensitivityDeg,
                JumpSpeed           = jumpSpeed
            };

            _movementSources.AddSource(_localInputSource);

            _state = new PlayerState
            {
                Position     = transform.position,
                Velocity     = Vector3.zero,
                Yaw          = transform.rotation.eulerAngles.y,
                Pitch        = 0f,
                IsGrounded   = _controller.isGrounded,
            };

            if (playerCamera != null && cameraPivot != null)
            {
                playerCamera.transform.SetParent(cameraPivot, worldPositionStays: false);
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            // 1. 合成本帧 MovementCommand
            MovementCommand cmd = _movementSources.BuildCommand(_state, dt);

            // 2. Motor 执行命令，输出期望位移
            _motor.Step(ref _state, in cmd, dt, out Vector3 desiredDisplacement);

            // 3. 由 CharacterController 处理碰撞并更新位置/接地状态
            if (_controller != null)
            {
                _controller.Move(desiredDisplacement);

                _state.Position   = transform.position;
                _state.IsGrounded = _controller.isGrounded;

                if (_state.IsGrounded && _state.Velocity.y < 0f)
                {
                    _state.Velocity = new Vector3(_state.Velocity.x, -2f, _state.Velocity.z);
                }
            }
            else
            {
                _state.Position += desiredDisplacement;
                transform.position = _state.Position;
                _state.IsGrounded  = false;
            }

            // 4. 应用旋转到角色与相机
            transform.rotation = Quaternion.Euler(0f, _state.Yaw, 0f);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(_state.Pitch, 0f, 0f);
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                _movementSources.AddSource(new DashOneShotMovementSource(transform.position,transform.position+10*cameraPivot.forward,20f,20f));
            }
        }
    }
}