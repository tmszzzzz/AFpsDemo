using System.Collections.Generic;
using Gameplay.Movement.Sources;

namespace Gameplay.Movement.Core
{
    public class MovementSourceCollection
    {
        private readonly List<IMovementSource> _sources = new List<IMovementSource>();

        public void AddSource(IMovementSource source)
        {
            if (source != null && !_sources.Contains(source))
            {
                _sources.Add(source);
            }
        }

        public void RemoveSource(IMovementSource source)
        {
            _sources.Remove(source);
        }

        /// <summary>
        /// 合成一帧的 MovementCommand：所有激活的 Source 各自对 command 进行叠加。
        /// </summary>
        public MovementCommand BuildCommand(PlayerState state, float deltaTime)
        {
            var cmd = MovementCommand.CreateEmpty();

            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                var src = _sources[i];

                if (src == null || (!src.IsActive && src.AutoRemoveWhenInactive))
                {
                    _sources.RemoveAt(i);
                    continue;
                }

                if (src != null && src.IsActive)
                {
                    src.UpdateSource(state, ref cmd, deltaTime);
                }
            }

            return cmd;
        }
    }
}