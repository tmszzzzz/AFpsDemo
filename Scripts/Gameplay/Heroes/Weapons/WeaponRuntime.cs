namespace Gameplay.Heroes.Weapons
{
    /// <summary>
    /// 纯逻辑武器时序：决定何时 Fire/Reload。
    /// 这里先不做弹药数，仅做节奏，后续你可以把 Ammo/Clip/Mag 机制加进去。
    /// </summary>
    public sealed class WeaponRuntime
    {
        public enum WeaponAction
        {
            None,
            Fire,
            Reload
        }

        public struct Output
        {
            public WeaponAction action;
            public bool firedThisTick;
            public bool reloadedThisTick;
        }

        private readonly float _fireInterval;
        private readonly float _reloadDuration;
        private readonly bool _fullAuto;

        private float _fireCooldown;
        private bool _reloading;
        private float _reloadT;

        public WeaponRuntime(float fireInterval, float reloadDuration, bool fullAuto)
        {
            _fireInterval = fireInterval <= 0f ? 0.12f : fireInterval;
            _reloadDuration = reloadDuration <= 0f ? 2.0f : reloadDuration;
            _fullAuto = fullAuto;
        }

        public Output Tick(float dt, bool fireHeld, bool fireDown, bool reloadPressed)
        {
            var o = new Output { action = WeaponAction.None };

            _fireCooldown = _fireCooldown > 0f ? _fireCooldown - dt : 0f;

            // reload 时序
            if (_reloading)
            {
                _reloadT += dt;
                if (_reloadT >= _reloadDuration)
                {
                    _reloading = false;
                    _reloadT = 0f;
                    o.action = WeaponAction.None;
                    o.reloadedThisTick = true;
                }
                return o;
            }

            if (reloadPressed)
            {
                _reloading = true;
                _reloadT = 0f;
                o.action = WeaponAction.Reload;
                return o;
            }

            bool wantsFire = fireDown || (_fullAuto && fireHeld);
            if (wantsFire && _fireCooldown <= 0f)
            {
                _fireCooldown = _fireInterval;
                o.action = WeaponAction.Fire;
                o.firedThisTick = true;
            }

            return o;
        }

        public void ForceStopReload()
        {
            _reloading = false;
            _reloadT = 0f;
        }
    }
}