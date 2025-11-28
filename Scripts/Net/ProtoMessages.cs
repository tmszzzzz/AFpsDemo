using System;

namespace Net
{
    public enum MsgId : ushort
    {
        JoinRequest = 1,
        JoinAccept  = 2,
        Ping        = 10,
        Pong        = 11,
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

    public struct NetMessage
    {
        public MsgHeader Header;
        public byte[]    Payload; // 不含头部
    }
}