using Gameplay.Movement.Core;

namespace Gameplay.Heroes
{
    public enum HeroId
    {
        None = 0,
        Generic = 1,
        // to be added...
    }

    /// <summary>
    /// 英雄配置，用来给 HeroCore 注入参数（可从 ScriptableObject 或 JSON 来）。
    /// </summary>
    [System.Serializable]
    public struct HeroConfig
    {
        public float MaxHp;

        // 把 Motor 需要的世界级参数也放进来，方便以后不同英雄差一点手感
        public float Gravity;
        public float MinPitch;
        public float MaxPitch;
        public float HorizontalAccelerationGround;
        public float HorizontalDecelerationGround;
        public float HorizontalAccelerationAir;
        public float HorizontalDecelerationAir;
    }

    /// <summary>
    /// 英雄整体状态（不含详细技能冷却等），包含：英雄 ID + 移动状态 + HP。
    /// </summary>
    [System.Serializable]
    public struct HeroState
    {
        public HeroId HeroId;
        public PlayerState Movement;

        public float Hp;
        public float MaxHp;
    }
}