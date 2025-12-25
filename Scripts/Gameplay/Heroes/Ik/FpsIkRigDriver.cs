using UnityEngine;

namespace Gameplay.Heroes.Ik
{
    /// <summary>
    /// 只负责“目标点更新”。RigBuilder/约束本身由 prefab 里搭建。
    /// </summary>
    public sealed class FpsIkRigDriver
    {
        private readonly Transform _cameraPivot;
        private readonly Transform _muzzle;
        private readonly Transform _gripL;
        private readonly Transform _gripR;
        private readonly IkTargets _targets;

        private readonly float _aimDistance;
        private readonly LayerMask _aimMask;

        public FpsIkRigDriver(
            Transform cameraPivot,
            Transform muzzle,
            Transform gripL,
            Transform gripR,
            IkTargets targets,
            float aimDistance,
            LayerMask aimMask)
        {
            _cameraPivot = cameraPivot;
            _muzzle = muzzle;
            _gripL = gripL;
            _gripR = gripR;
            _targets = targets;
            _aimDistance = aimDistance;
            _aimMask = aimMask;
        }

        public void Tick()
        {
            // 1) 双手握把：Target = Grip 的世界位姿
            if (_targets.leftHandTarget != null && _gripL != null)
            {
                _targets.leftHandTarget.position = _gripL.position;
                _targets.leftHandTarget.rotation = _gripL.rotation;
            }

            if (_targets.rightHandTarget != null && _gripR != null)
            {
                _targets.rightHandTarget.position = _gripR.position;
                _targets.rightHandTarget.rotation = _gripR.rotation;
            }

            // 2) AimTarget：沿 cameraPivot.forward（优先）做 raycast
            if (_targets.aimTarget == null) return;

            Vector3 origin;
            Vector3 dir;

            if (_cameraPivot != null)
            {
                origin = _cameraPivot.position;
                dir = _cameraPivot.forward;
            }
            else if (_muzzle != null)
            {
                origin = _muzzle.position;
                dir = _muzzle.forward;
            }
            else
            {
                return;
            }

            var ray = new Ray(origin, dir);
            if (Physics.Raycast(ray, out var hit, _aimDistance, _aimMask, QueryTriggerInteraction.Ignore))
                _targets.aimTarget.position = hit.point;
            else
                _targets.aimTarget.position = origin + dir * _aimDistance;
        }
    }
}