using UnityEngine;

namespace Gameplay.Movement.Core
{
    /// <summary>
    /// 角色运动内核：在统一的世界规则（如重力、视角约束）下
    /// 执行 MovementCommand 对 PlayerState 的影响。
    /// 不包含“操作/英雄层”的参数（如基础移速、跳高、鼠标灵敏度）。
    /// </summary>
    public class CharacterMotor
    {
        public float Gravity  = 20f;   // 世界重力加速度（m/s^2，向下）
        public float MinPitch = -89f;  // 视角约束（度）
        public float MaxPitch = 89f;

        /// <summary>
        /// 水平速度收敛参数：地面与空中分离，便于实现“地面摩擦大、空中控制弱”的手感。
        /// </summary>
        public float HorizontalAccelerationGround = 80f; // 地面有输入时的最大加速度（m/s^2）
        public float HorizontalDecelerationGround = 60f; // 地面无输入/减速时的最大减速度（m/s^2）
        public float HorizontalAccelerationAir    = 30f; // 空中有输入时的最大加速度（m/s^2）
        public float HorizontalDecelerationAir    = 20f; // 空中无输入/减速时的最大减速度（m/s^2）

        /// <summary>
        /// 执行一帧的运动：
        /// - 根据 LookDelta 更新视角；
        /// - 若存在 ForcedDisplacement，则本帧按强制位移移动；
        /// - 否则根据当前速度 + DesiredVelocity + VelocityImpulse + Gravity 更新新速度；
        /// - 输出“在真空中”应有的位移向量，由上层控制器结合碰撞裁剪实际位置。
        /// </summary>
        public void Step(ref PlayerState state, in MovementCommand cmd, float deltaTime, out Vector3 desiredDisplacement)
        {
            if (deltaTime <= 0f)
            {
                desiredDisplacement = Vector3.zero;
                return;
            }

            // 1. 视角更新（增量由 Source 决定，Motor 只做累加与 clamp）
            state.Yaw   += cmd.LookDelta.x;
            state.Pitch += cmd.LookDelta.y;

            state.Pitch = Mathf.Clamp(state.Pitch, MinPitch, MaxPitch);

            if (state.Yaw > 360f) state.Yaw -= 360f;
            if (state.Yaw < 0f)   state.Yaw += 360f;

            // 2. 若存在强制位移，本帧仅按 ForcedDisplacement 运动：
            //    - 忽略 DesiredVelocity / VelocityImpulse / Gravity；
            //    - 仅由上层的碰撞系统对该位移进行裁剪（如撞墙/撞地）。
            if (cmd.HasForcedDisplacement)
            {
                desiredDisplacement = cmd.ForcedDisplacement;
                state.Velocity = Vector3.zero;
                return;
            }

            // 3. 普通运动路径：从上一帧状态取出速度
            Vector3 v = state.Velocity;

            // 3.1 水平速度：在“当前速度”和“期望速度”之间用加减速/摩擦收敛，
            //     地面与空中使用不同的加减速参数，避免水平冲量被立即抹掉，同时体现空中控制较弱。
            Vector3 currentHorizontal = new Vector3(v.x, 0f, v.z);
            Vector3 desiredHorizontal = new Vector3(cmd.DesiredVelocity.x, 0f, cmd.DesiredVelocity.z);

            bool   hasInput = desiredHorizontal.sqrMagnitude > 0.0001f;
            float  accel, decel;

            if (state.IsGrounded)
            {
                accel = HorizontalAccelerationGround;
                decel = HorizontalDecelerationGround;
            }
            else
            {
                accel = HorizontalAccelerationAir;
                decel = HorizontalDecelerationAir;
            }

            float usedAccel      = hasInput ? accel : decel;
            float maxSpeedChange = usedAccel * deltaTime;
            Vector3 newHorizontal = Vector3.MoveTowards(currentHorizontal, desiredHorizontal, maxSpeedChange);

            v.x = newHorizontal.x;
            v.z = newHorizontal.z;

            // 3.2 叠加本帧瞬时冲量（任意方向 Δv）：
            //     这一步可能同时修改水平与垂直分量，其效果通过写回 v 持续到后续帧，
            //     再由上面的收敛逻辑与重力/摩擦逐步“吃掉”这次冲量。
            if (cmd.VelocityImpulse != Vector3.zero)
            {
                v += cmd.VelocityImpulse;
            }

            // 3.3 世界级重力（仅作用在竖直方向）
            if (state.IsGrounded)
            {
                // 贴地时若竖直速度向下，轻微压住避免抖动，同时形成简单的“地面吸附”效果。
                if (v.y < 0f)
                    v.y = -5f;
            }
            else
            {
                v.y -= Gravity * deltaTime;
            }

            // 4. 写回速度，并计算本帧“理想位移”
            state.Velocity      = v;
            desiredDisplacement = v * deltaTime;
        }
    }
}
