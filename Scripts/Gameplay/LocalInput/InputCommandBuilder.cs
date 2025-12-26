using Net;

namespace Gameplay.LocalInput
{
    /// <summary>
    /// LocalInputFrame -> Net.InputCommand。
    /// 说明：服务器目前未必消费 Fire/Aim/Reload，默认可屏蔽这些位。
    /// </summary>
    public sealed class InputCommandBuilder
    {
        public bool sendWeaponButtonsToServer = false;

        public InputCommand Build(uint playerId, ushort seq, uint clientTick, in LocalInputFrame f)
        {
            uint mask = (uint)f.buttons;

            // 仅在你扩展了服务器协议后再打开。
            if (!sendWeaponButtonsToServer)
            {
                mask &= ~((uint)(LocalButtons.Aim | LocalButtons.Fire | LocalButtons.Reload));
            }

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