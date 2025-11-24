using Gameplay.Movement.Core;

namespace Gameplay.Movement.Sources
{
    /// <summary>
    /// 所有“运动源”的统一接口。
    /// 例如：本地输入、Dash、Knockback、Teleport 等。
    /// </summary>
    public interface IMovementSource
    {
        bool IsActive { get; }                 // 本帧是否生效
        bool AutoRemoveWhenInactive { get; }   // 不活跃时是否自动从 Collection 移除

        /// <summary>
        /// 在给定状态下，为本帧的 MovementCommand 贡献自己的部分。
        /// 可以修改 command 中的字段（叠加或覆盖）。
        /// </summary>
        void UpdateSource(PlayerState state, ref MovementCommand command, float deltaTime);
    }
}