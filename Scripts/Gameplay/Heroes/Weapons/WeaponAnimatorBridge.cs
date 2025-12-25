using UnityEngine;

namespace Gameplay.Heroes.Weapons
{
    /// <summary>
    /// 把 WeaponRuntime 的输出映射到 Animator：
    /// - Fire/Reload 用 CrossFade/Play 点播（不靠图里堆转移）。
    /// </summary>
    public sealed class WeaponAnimatorBridge
    {
        private readonly Animator _charAnim;
        private readonly int _charLayer;
        private readonly Animator _weaponAnim;
        private readonly int _weaponLayer;

        private readonly string _stateFire;
        private readonly string _stateReload;

        public WeaponAnimatorBridge(
            Animator characterAnimator, int upperBodyLayer,
            Animator weaponAnimator, int weaponLayer,
            string stateFire, string stateReload)
        {
            _charAnim = characterAnimator;
            _charLayer = upperBodyLayer;
            _weaponAnim = weaponAnimator;
            _weaponLayer = weaponLayer;
            _stateFire = stateFire;
            _stateReload = stateReload;
        }

        public void PlayFire()
        {
            PlayBoth(_stateFire);
        }

        public void PlayReload()
        {
            PlayBoth(_stateReload);
        }

        private void PlayBoth(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return;

            if (_charAnim != null)
            {
                if (_charLayer >= 0)
                    _charAnim.CrossFade(state, 0.02f, _charLayer, 0f);
                else
                    _charAnim.CrossFade(state, 0.02f, -1, 0f);
            }

            if (_weaponAnim != null)
            {
                if (_weaponLayer >= 0)
                    _weaponAnim.CrossFade(state, 0.02f, _weaponLayer, 0f);
                else
                    _weaponAnim.CrossFade(state, 0.02f, -1, 0f);
            }
        }
    }
}