using Gameplay.Movement.Core;
using Gameplay.Movement.Sources;
namespace Gameplay.Heroes
{
    /// <summary>
    /// 英雄核心逻辑：不依赖 Unity 组件。
    /// - 持有 HeroState（含 PlayerState）。
    /// - 持有 CharacterMotor + MovementSourceCollection。
    /// - 暴露 TickMovement / AddMovementSource / ApplyDamage 等接口。
    /// </summary>
    public class HeroCore
    {
        public HeroState State;

        private readonly CharacterMotor           _motor;
        private readonly MovementSourceCollection _movementSources;

        public HeroCore(HeroId heroId, UnityEngine.Vector3 spawnPos, HeroConfig config)
        {
            _motor = new CharacterMotor
            {
                Gravity  = config.Gravity,
                MinPitch = config.MinPitch,
                MaxPitch = config.MaxPitch,
                HorizontalAccelerationGround = config.HorizontalAccelerationGround,
                HorizontalDecelerationGround = config.HorizontalDecelerationGround,
                HorizontalAccelerationAir    = config.HorizontalAccelerationAir,
                HorizontalDecelerationAir    = config.HorizontalDecelerationAir
            };

            _movementSources = new MovementSourceCollection();

            State = new HeroState
            {
                HeroId = heroId,
                Movement = new PlayerState
                {
                    Position   = spawnPos,
                    Velocity   = UnityEngine.Vector3.zero,
                    Yaw        = 0f,
                    Pitch      = 0f,
                    IsGrounded = false
                },
                Hp    = config.MaxHp,
                MaxHp = config.MaxHp
            };
        }

        /// <summary>
        /// 一帧运动逻辑：
        /// - 由所有 MovementSource 合成 MovementCommand
        /// - 交给 CharacterMotor 计算“理想位移”
        /// - 不做碰撞，碰撞由外层负责
        /// </summary>
        public void TickMovement(float deltaTime, out MovementCommand cmd, out UnityEngine.Vector3 desiredDisplacement)
        {
            cmd = _movementSources.BuildCommand(State.Movement, deltaTime);
            _motor.Step(ref State.Movement, in cmd, deltaTime, out desiredDisplacement);
        }

        /// <summary>
        /// 对英雄施加伤害（最基础版）。
        /// </summary>
        public void ApplyDamage(float amount)
        {
            State.Hp = UnityEngine.Mathf.Clamp(State.Hp - amount, 0f, State.MaxHp);
        }

        /// <summary>
        /// 对外暴露的运动源入口：本地输入、Dash、击退等都从这里注册。
        /// </summary>
        public void AddMovementSource(IMovementSource source)
        {
            _movementSources.AddSource(source);
        }
    }
}
