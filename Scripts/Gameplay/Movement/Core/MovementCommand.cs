using UnityEngine;

namespace Gameplay.Movement.Core
{
    /// <summary>
    /// 单帧综合后的运动指令，只描述“想要怎样动”而不是直接改状态：
    /// - DesiredVelocity：期望的世界空间连续速度（通常用于持续位移，如行走/奔跑）。
    /// - VelocityImpulse：本帧附加在速度上的任意方向瞬时冲量（Δv），只在本帧生效。
    /// - ForcedDisplacement：本帧的强制位移（如果存在，则覆盖普通速度/冲量/重力的影响）。
    /// - LookDelta：本帧希望改变的视角角度（单位：度）。
    /// </summary>
    public struct MovementCommand
    {
        /// <summary>
        /// 本帧期望的世界空间连续速度（一般来自移动/持续效果）。
        /// </summary>
        public Vector3 DesiredVelocity;

        /// <summary>
        /// 本帧额外叠加到速度上的瞬时冲量（任意方向的 Δv）。
        /// 单帧生效，用完即清零；持久效果通过源每帧贡献 DesiredVelocity 实现。
        /// </summary>
        public Vector3 VelocityImpulse;
        
        /// <summary>
        /// 本帧的强制位移（世界空间）。
        /// 当 HasForcedDisplacement 为 true 时：
        /// - Motor 将忽略本帧的 DesiredVelocity、VelocityImpulse 与重力；
        /// - 实际位移完全由 ForcedDisplacement 决定（再由碰撞系统裁剪）。
        /// </summary>
        public Vector3 ForcedDisplacement;
        
        /// <summary>
        /// 是否启用本帧强制位移。
        /// </summary>
        public bool HasForcedDisplacement;

        /// <summary>
        /// 本帧视角变化（度）：X=YawDelta, Y=PitchDelta。
        /// </summary>
        public Vector2 LookDelta;
        
        /*
         * 以上这些字段是可拓展的，我们可以便捷地引入更多种运动种类，例如启用飞行、重力减缓等等。
         * 然后我们在CharacterMotor中修改对应的计算逻辑。
         */

        public static MovementCommand CreateEmpty()
        {
            return new MovementCommand
            {
                DesiredVelocity      = Vector3.zero,
                VelocityImpulse      = Vector3.zero,
                ForcedDisplacement   = Vector3.zero,
                HasForcedDisplacement = false,
                LookDelta            = Vector2.zero
            };
        }
    }
}