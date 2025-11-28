using System;
using System.Text;

namespace Net
{
    public static class ProtoSerializer
    {
        #region 写入工具

        private static void WriteU16(System.IO.Stream s, ushort v)
        {
            // 小端序
            s.WriteByte((byte)(v & 0xFF));
            s.WriteByte((byte)((v >> 8) & 0xFF));
        }

        private static void WriteU32(System.IO.Stream s, uint v)
        {
            s.WriteByte((byte)( v        & 0xFF));
            s.WriteByte((byte)((v >> 8)  & 0xFF));
            s.WriteByte((byte)((v >> 16) & 0xFF));
            s.WriteByte((byte)((v >> 24) & 0xFF));
        }

        private static void WriteString(System.IO.Stream s, string str)
        {
            if (str == null) str = string.Empty;
            var bytes = Encoding.UTF8.GetBytes(str);
            if (bytes.Length > ushort.MaxValue)
                throw new Exception("String too long");
            WriteU16(s, (ushort)bytes.Length);
            s.Write(bytes, 0, bytes.Length);
        }

        #endregion

        #region 读取工具

        private static bool ReadFully(System.IO.Stream s, byte[] buffer, int length)
        {
            int read = 0;
            while (read < length)
            {
                int r = s.Read(buffer, read, length - read);
                if (r <= 0) return false;
                read += r;
            }
            return true;
        }

        private static bool ReadU16(byte[] buf, ref int offset, out ushort v)
        {
            if (offset + 2 > buf.Length) { v = 0; return false; }
            v = (ushort)(buf[offset] | (buf[offset + 1] << 8));
            offset += 2;
            return true;
        }

        private static bool ReadU32(byte[] buf, ref int offset, out uint v)
        {
            if (offset + 4 > buf.Length) { v = 0; return false; }
            v = (uint)(buf[offset]
                       | (buf[offset + 1] << 8)
                       | (buf[offset + 2] << 16)
                       | (buf[offset + 3] << 24));
            offset += 4;
            return true;
        }

        private static bool ReadString(byte[] buf, ref int offset, out string s)
        {
            s = string.Empty;
            if (!ReadU16(buf, ref offset, out var len)) return false;
            if (offset + len > buf.Length) return false;
            s = Encoding.UTF8.GetString(buf, offset, len);
            offset += len;
            return true;
        }

        #endregion

        #region 编码各类消息

        public static NetMessage EncodeJoinRequest(JoinRequest jr)
        {
            using var ms = new System.IO.MemoryStream();

            // 先占位头部
            WriteU16(ms, 0); // length 占位
            WriteU16(ms, (ushort)MsgId.JoinRequest);
            WriteU32(ms, 0); // seq

            // 写 payload
            WriteU16(ms, jr.protocolVersion);
            WriteString(ms, jr.playerName);

            var bytes = ms.ToArray();
            ushort length = (ushort)bytes.Length;
            // 回写 length
            bytes[0] = (byte)(length & 0xFF);
            bytes[1] = (byte)((length >> 8) & 0xFF);

            var header = new MsgHeader { length = length, msgId = (ushort)MsgId.JoinRequest, seq = 0 };
            var payload = new byte[length - 8];
            Buffer.BlockCopy(bytes, 8, payload, 0, payload.Length);

            return new NetMessage { Header = header, Payload = payload };
        }

        public static NetMessage EncodePing(Ping ping)
        {
            using var ms = new System.IO.MemoryStream();
            WriteU16(ms, 0);
            WriteU16(ms, (ushort)MsgId.Ping);
            WriteU32(ms, 0);
            WriteU32(ms, ping.clientTime);

            var bytes = ms.ToArray();
            ushort length = (ushort)bytes.Length;
            bytes[0] = (byte)(length & 0xFF);
            bytes[1] = (byte)((length >> 8) & 0xFF);

            var header = new MsgHeader { length = length, msgId = (ushort)MsgId.Ping, seq = 0 };
            var payload = new byte[length - 8];
            Buffer.BlockCopy(bytes, 8, payload, 0, payload.Length);
            return new NetMessage { Header = header, Payload = payload };
        }

        #endregion

        #region 解码收到的消息

        public static bool DecodeHeader(byte[] buf, out MsgHeader header)
        {
            header = default;
            if (buf.Length < 8) return false;
            int offset = 0;
            if (!ReadU16(buf, ref offset, out header.length)) return false;
            if (!ReadU16(buf, ref offset, out header.msgId))   return false;
            if (!ReadU32(buf, ref offset, out header.seq))     return false;
            return true;
        }

        public static bool DecodeJoinAccept(NetMessage msg, out JoinAccept ja)
        {
            ja = default;
            int offset = 0;
            if (!ReadU32(msg.Payload, ref offset, out ja.playerId))              return false;
            if (!ReadU16(msg.Payload, ref offset, out ja.serverProtocolVersion)) return false;
            return true;
        }

        public static bool DecodePong(NetMessage msg, out Pong pong)
        {
            pong = default;
            int offset = 0;
            if (!ReadU32(msg.Payload, ref offset, out pong.clientTime)) return false;
            if (!ReadU32(msg.Payload, ref offset, out pong.serverTime)) return false;
            return true;
        }

        #endregion

        #region 辅助：构造完整包字节

        public static byte[] BuildPacket(NetMessage msg)
        {
            var bytes = new byte[msg.Header.length];
            // 写 header
            int offset = 0;
            bytes[offset++] = (byte)(msg.Header.length & 0xFF);
            bytes[offset++] = (byte)((msg.Header.length >> 8) & 0xFF);
            bytes[offset++] = (byte)(msg.Header.msgId & 0xFF);
            bytes[offset++] = (byte)((msg.Header.msgId >> 8) & 0xFF);
            bytes[offset++] = (byte)(msg.Header.seq & 0xFF);
            bytes[offset++] = (byte)((msg.Header.seq >> 8) & 0xFF);
            bytes[offset++] = (byte)((msg.Header.seq >> 16) & 0xFF);
            bytes[offset++] = (byte)((msg.Header.seq >> 24) & 0xFF);

            if (msg.Payload != null && msg.Payload.Length > 0)
            {
                Buffer.BlockCopy(msg.Payload, 0, bytes, offset, msg.Payload.Length);
            }
            return bytes;
        }

        #endregion
    }
}