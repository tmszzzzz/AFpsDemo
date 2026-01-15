using System;

namespace Net
{
    public enum MsgId : ushort
    {
        JoinRequest = 0x1,
        JoinAccept  = 0x2,
        Ping        = 0x10,
        Pong        = 0x11,
        UdpBind     = 0x20,
        // === M3 新增：玩家输入 / 世界快照 / 事件 ===
        InputCommand  = 0x30,  // 客户端 -> 服务端：输入命令
        WorldSnapshot = 0x40,  // 服务端 -> 客户端：世界状态快照
        GameEvent     = 0x50,  // 服务端 -> 客户端：游戏事件
    }

    public struct MsgHeader
    {
        public ushort length;  // 包总长（含头）
        public ushort msgId;   // MsgId
        public uint   seq;     // 里程碑 2 中可固定为 0
    }

    public struct JoinRequest
    {
        public ushort protocolVersion;
        public string playerName;
    }

    public struct JoinAccept
    {
        public uint   playerId;
        public ushort serverProtocolVersion;
    }

    public struct Ping
    {
        public uint clientTime; // 客户端时间戳/计数（毫秒）
    }

    public struct Pong
    {
        public uint clientTime;
        public uint serverTime;
    }
    
    public struct UdpBind
    {
        public uint playerId; // 要绑定到 UDP 通道的玩家 ID
    }
    
    public struct InputCommand
    {
        public uint playerId;

        public ushort seq;        // 输入序号
        public uint   clientTick; // 客户端本地tick

        public float moveX;       // -1..1
        public float moveY;       // -1..1
        public float yaw;
        public float pitch;

        public uint buttonMask;   // 按键bitmask
    }
    
    public static class InputButtons
    {
        public const uint MOUSE_FIRE_PRI = 1u << 0;
        public const uint MOUSE_FIRE_SEC = 1u << 1;
        public const uint BUTTON_JUMP = 1u << 2;
        public const uint BUTTON_ULTRA = 1u << 3;
        public const uint BUTTON_SKILL_E = 1u << 4;
        public const uint BUTTON_SKILL_SHIFT = 1u << 5;
        public const uint BUTTON_SKILL_CTRL = 1u << 6;
        public const uint BUTTON_HIT_V = 1u << 7;
        public const uint BUTTON_RELOAD = 1u << 8;
    }
    
    public struct PlayerSnapshot
    {
        public uint playerId;
        public uint heroId;

        public float posX, posY, posZ;
        public float velX, velY, velZ;
        public float yaw, pitch;

        public byte locomotionState;
        public byte actionState;

        public byte activeSkillSlot;
        public byte activeSkillPhase;

        public uint statusFlags;

        public ushort health;
        public ushort energy;
    }

    public struct WorldSnapshot
    {
        public uint serverTick;
        public PlayerSnapshot[] players;
    }
    
    public enum GameEventType : byte
    {
        DashStarted = 1,
        WeaponFired = 2,
        WeaponDryFire = 3,
        WeaponReloadStarted = 4,
        WeaponReloadFinished = 5,
        MeleeHit = 6,
    }

    public struct GameEvent
    {
        public GameEventType type;
        public uint          serverTick;
        public uint          casterPlayerId;

        public uint          targetId;

        public byte          u8Param0;
        public byte          u8Param1;

        public uint          u32Param0;

        public float         f32Param0;
        public float         f32Param1;
    }

    public struct NetMessage
    {
        public MsgHeader Header;
        public byte[]    Payload; // 不含头部
    }
}
