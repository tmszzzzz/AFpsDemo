using System;

namespace Gameplay.LocalInput
{
    [Flags]
    public enum LocalButtons : uint
    {
        NONE   = 0,
        MOUSE_FIRE_PRI   = 1u << 0,
        MOUSE_FIRE_SEC = 1u << 1,
        BUTTON_JUMP = 1u << 2,
        BUTTON_ULTRA = 1u << 3,
        BUTTON_SKILL_E = 1u << 4,
        BUTTON_SKILL_SHIFT = 1u << 5,
        BUTTON_SKILL_CTRL = 1u << 6,
        BUTTON_HIT_V = 1u << 7,
        BUTTON_RELOAD = 1 << 8,
    }

    public struct LocalInputFrame
    {
        public float moveX;
        public float moveY;

        // 绝对角度（与现有协议一致）
        public float yaw;
        public float pitch;

        public LocalButtons buttons;

        public bool Has(LocalButtons b) => (buttons & b) != 0;
    }
}