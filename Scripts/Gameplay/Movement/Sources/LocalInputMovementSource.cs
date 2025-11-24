using Gameplay.Movement.Core;
using UnityEngine;

namespace Gameplay.Movement.Sources
{
    /// <summary>
    /// 本地输入源：将 Unity 输入（键鼠）转换为 MovementCommand 的贡献。
    /// 持有操作/英雄层参数（基础移速、鼠标灵敏度、跳跃初速度）。
    /// </summary>
    public class LocalInputMovementSource : IMovementSource
    {
        public bool IsActive { get; set; } = true;
        public bool AutoRemoveWhenInactive => false;

        [Header("Move Settings")]
        public float BaseMoveSpeed = 6f;      // 英雄基础移动速度

        [Header("Look Settings")]
        public float YawSensitivityDeg   = 2f;   // 每单位鼠标输入对应多少度
        public float PitchSensitivityDeg = 2f;

        [Header("Jump Settings")]
        public float JumpSpeed = 6f; // 近似跳跃起跳速度（m/s），以后可从英雄配置中注入

        [Header("Input Axes")]
        public string HorizontalAxis = "Horizontal";
        public string VerticalAxis   = "Vertical";
        public string MouseXAxis     = "Mouse X";
        public string MouseYAxis     = "Mouse Y";
        public string JumpButton     = "Jump";

        public void UpdateSource(PlayerState state, ref MovementCommand command, float deltaTime)
        {
            // 1. 平面移动输入
            float h = Input.GetAxisRaw(HorizontalAxis);
            float v = Input.GetAxisRaw(VerticalAxis);
            Vector2 axis = new Vector2(h, v);

            if (axis.sqrMagnitude > 1f)
                axis.Normalize();

            if (axis.sqrMagnitude > 0f)
            {
                // 使用当前状态的 Yaw，将本地前/右向转为世界空间方向
                Quaternion yawRot = Quaternion.Euler(0f, state.Yaw, 0f);
                Vector3 forward = yawRot * Vector3.forward;
                Vector3 right   = yawRot * Vector3.right;
                forward.y = 0f;
                right.y   = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 moveDir = forward * axis.y + right * axis.x;
                // 将基础移动速度贡献到期望速度（水平面）
                command.DesiredVelocity += moveDir * BaseMoveSpeed;
            }

            // 2. 视角输入（转为角度增量）
            float mx = Input.GetAxis(MouseXAxis);
            float my = Input.GetAxis(MouseYAxis);

            if (mx != 0f || my != 0f)
            {
                float yawDelta   = mx * YawSensitivityDeg;
                float pitchDelta = -my * PitchSensitivityDeg; // 鼠标上推视角抬起

                command.LookDelta += new Vector2(yawDelta, pitchDelta);
            }

            // 3. 跳跃 → 竖直冲量请求（是否允许起跳由上层规则决定）
            if (Input.GetButtonDown(JumpButton))
            {
                command.VelocityImpulse += new Vector3(0,JumpSpeed,0);
            }
        }
    }
}