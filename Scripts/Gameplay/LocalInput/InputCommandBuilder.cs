using Net;

namespace Gameplay.LocalInput
{
    /// <summary>
    /// LocalInputFrame -> Net.InputCommand。
    /// 说明：服务器目前未必消费 Fire/Aim/Reload，默认可屏蔽这些位。
    /// </summary>
    public sealed class InputCommandBuilder
    {
        public InputCommand Build(uint playerId, ushort seq, uint clientTick, in LocalInputFrame f)
        {
            uint mask = (uint)f.buttons;

            return new Net.InputCommand
            {
                playerId = playerId,
                seq = seq,
                clientTick = clientTick,
                moveX = f.moveX,
                moveY = f.moveY,
                yaw = f.yaw,
                pitch = f.pitch,
                buttonMask = mask,
            };
        }
    }
}