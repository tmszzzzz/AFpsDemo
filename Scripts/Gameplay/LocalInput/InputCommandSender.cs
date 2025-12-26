using Net;
using UnityEngine;

namespace Gameplay.LocalInput
{
    /// <summary>
    /// 负责：采样 -> 组包 -> 发送，同时把同一份输入喂给本地表现。
    /// </summary>
    public sealed class InputCommandSender
    {
        private readonly NetClient _netClient;
        private readonly LocalInputSampler _sampler;
        private readonly InputCommandBuilder _builder;

        private ushort _seq;
        private uint _clientTick;

        private uint _playerId;
        private bool _hasPlayerId;

        private Gameplay.Players.LocalPlayerFacade _local;

        public InputCommandSender(NetClient netClient, LocalInputSampler sampler, InputCommandBuilder builder)
        {
            _netClient = netClient;
            _sampler = sampler;
            _builder = builder;
        }

        public void SetPlayerId(uint playerId)
        {
            _playerId = playerId;
            _hasPlayerId = true;
        }

        public void BindLocalPlayer(Gameplay.Players.LocalPlayerFacade local)
        {
            _local = local;
        }

        public void Tick(float dt)
        {
            if (!_hasPlayerId) return;

            _clientTick++;

            var frame = _sampler.Sample();

            // 1) 本地表现（立即反馈）
            _local?.ApplyLocalInput(in frame);

            // 2) 发给服务器
            var ic = _builder.Build(_playerId, _seq++, _clientTick, in frame);
            _netClient.SendInputCommand(ic);
        }
    }
}