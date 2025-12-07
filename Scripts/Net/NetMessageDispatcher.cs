using System;

namespace Net
{
    public class NetMessageDispatcher
    {
        private readonly Game.ClientGame _clientGame;

        public NetMessageDispatcher(Game.ClientGame clientGame)
        {
            _clientGame = clientGame;
        }

        public void Dispatch(NetMessage msg)
        {
            var id = (MsgId)msg.Header.msgId;
            switch (id)
            {
                case MsgId.JoinAccept:
                    if (ProtoSerializer.DecodeJoinAccept(msg, out var ja))
                    {
                        _clientGame.OnJoinAccept(ja);
                    }

                    break;

                case MsgId.Pong:
                    if (ProtoSerializer.DecodePong(msg, out var pong))
                    {
                        _clientGame.OnPong(pong);
                    }

                    break;
                case MsgId.WorldSnapshot:
                    if (ProtoSerializer.DecodeWorldSnapshot(msg, out var ws))
                    {
                        _clientGame.OnWorldSnapshot(ws);
                    }

                    break;
                default:
                    // 里程碑 2 仅处理上述两类消息
                    break;
            }
        }
    }
}