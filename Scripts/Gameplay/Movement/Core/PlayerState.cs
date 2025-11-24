using System;
using UnityEngine;

namespace Gameplay.Movement.Core
{
    /// <summary>
    /// 运动系统眼中的角色状态（不包含 HP / 武器等）。
    /// </summary>
    [Serializable]
    public struct PlayerState
    {
        public Vector3 Position;
        public Vector3 Velocity;

        public float Yaw;   // 水平朝向（度）
        public float Pitch; // 垂直视角（度）

        public bool IsGrounded;
    }
}