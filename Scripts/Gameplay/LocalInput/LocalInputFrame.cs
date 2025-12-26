using System;

namespace Gameplay.LocalInput
{
    [Flags]
    public enum LocalButtons : uint
    {
        None   = 0,
        Jump   = 1 << 0,
        Use    = 1 << 1,
        Sprint = 1 << 2,

        // 下面三项：默认只用于本地表现（可选择不发给服务器）
        Aim    = 1 << 8,
        Fire   = 1 << 9,
        Reload = 1 << 10,
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