using UnityEngine;
using Gameplay.Movement.Core;

namespace Gameplay.Movement.Sources
{
    /// <summary>
    /// 一次性 dash 源：构造即视为开始冲刺，用完自动从 MovementSourceCollection 中移除。
    /// 行为：
    /// - 从构造时给定的 startPos 朝 targetPos 方向 dash，速度固定为 dashSpeed；
    /// - 实际 dash 距离不会超过 maxDashDistance；
    /// - dash 过程中每帧通过 MovementCommand.ForcedDisplacement 施加强制位移；
    /// - 动量清除逻辑统一由 CharacterMotor 在处理 ForcedDisplacement 时完成。
    /// 使用方式：
    ///   var dash = new DashOneShotMovementSource(_state.Position, targetPos, 20f, 15f);
    ///   _movementSources.AddSource(dash);
    /// 无需持有引用，也无需额外调用 Start/Stop。
    /// </summary>
    public class DashOneShotMovementSource : IMovementSource
    {
        public bool IsActive => _isDashing;
        public bool AutoRemoveWhenInactive => true; // 一次性源，用完自动清理

        // 配置参数
        private readonly float _dashSpeed;        // dash 速度（m/s）
        private readonly float _maxDashDistance;  // 最大 dash 距离

        // 内部状态
        private bool   _isDashing;
        private Vector3 _dashDir;             // 归一化 dash 方向（完整 3D，是否水平由调用方控制）
        private float   _remainingDistance;   // 剩余 dash 距离

        /// <summary>
        /// 构造即开始 dash。
        /// 注意：dashDir 由 startPos → targetPos 的 3D 向量决定，
        /// 若需“只水平 dash”，调用方应自行保证 startPos.y == targetPos.y。
        /// </summary>
        public DashOneShotMovementSource(Vector3 startPos, Vector3 targetPos, float dashSpeed, float maxDashDistance)
        {
            _dashSpeed       = Mathf.Max(0f, dashSpeed);
            _maxDashDistance = Mathf.Max(0f, maxDashDistance);

            Vector3 delta = targetPos - startPos; // 不再强制水平化，完整 3D 方向

            float dist = delta.magnitude;
            if (dist <= 0.001f || _dashSpeed <= 0.001f)
            {
                // 无有效 dash，保持非激活状态，下一帧会被 Collection 清理
                _isDashing         = false;
                _dashDir           = Vector3.zero;
                _remainingDistance = 0f;
                return;
            }

            if (dist > _maxDashDistance)
            {
                dist  = _maxDashDistance;
                delta = delta.normalized * dist;
            }

            _dashDir           = delta.normalized;
            _remainingDistance = dist;
            _isDashing         = true;
        }

        public void UpdateSource(PlayerState state, ref MovementCommand command, float deltaTime)
        {
            if (!_isDashing || deltaTime <= 0f)
                return;

            // 本帧应 dash 的距离：恒定速度 dashSpeed，但不能超过剩余距离
            float maxStepDist = _dashSpeed * deltaTime;
            float stepDist    = Mathf.Min(maxStepDist, _remainingDistance);

            if (stepDist <= 0f)
            {
                _isDashing = false;
                return;
            }

            // 计算本帧强制位移向量
            Vector3 step = _dashDir * stepDist;

            // 将本帧强制位移写入 MovementCommand：
            // 若已有其他强制位移（极少见），则进行叠加。
            command.HasForcedDisplacement = true;
            command.ForcedDisplacement   += step;

            _remainingDistance -= stepDist;
            if (_remainingDistance <= 0f)
            {
                _isDashing = false;
            }
        }
    }
}
