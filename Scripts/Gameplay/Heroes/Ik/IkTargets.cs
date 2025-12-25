using UnityEngine;

namespace Gameplay.Heroes.Ik
{
    public readonly struct IkTargets
    {
        public readonly Transform leftHandTarget;
        public readonly Transform rightHandTarget;
        public readonly Transform aimTarget;

        public IkTargets(Transform l, Transform r, Transform aim)
        {
            leftHandTarget = l;
            rightHandTarget = r;
            aimTarget = aim;
        }
    }
}