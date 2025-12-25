using UnityEngine;

namespace Gameplay.Heroes.Weapons
{
    /// <summary>
    /// 单源后坐力：只写 recoilPivot.localRotation。
    /// </summary>
    public sealed class RecoilSpring
    {
        private readonly Transform _pivot;

        private readonly float _kickPitch;
        private readonly float _kickYaw;
        private readonly float _returnSpeed;
        private readonly float _damping;

        private Vector3 _vel;
        private Vector3 _cur;
        private Vector3 _target;

        public RecoilSpring(Transform pivot, float kickPitchDeg = 2f, float kickYawDeg = 0.5f, float returnSpeed = 18f, float damping = 22f)
        {
            _pivot = pivot;
            _kickPitch = kickPitchDeg;
            _kickYaw = kickYawDeg;
            _returnSpeed = Mathf.Max(0.01f, returnSpeed);
            _damping = Mathf.Max(0.01f, damping);
        }

        public void Kick()
        {
            if (_pivot == null) return;
            float yaw = Random.Range(-_kickYaw, _kickYaw);
            _target += new Vector3(-_kickPitch, yaw, 0f);
        }

        public void Tick(float dt)
        {
            if (_pivot == null) return;

            _cur = Vector3.SmoothDamp(_cur, _target, ref _vel, 1f / _returnSpeed, Mathf.Infinity, dt);
            _target = Vector3.Lerp(_target, Vector3.zero, 1f - Mathf.Exp(-_damping * dt));
            _pivot.localRotation = Quaternion.Euler(_cur);
        }

        public void Reset()
        {
            _vel = Vector3.zero;
            _cur = Vector3.zero;
            _target = Vector3.zero;
            if (_pivot != null) _pivot.localRotation = Quaternion.identity;
        }
    }
}