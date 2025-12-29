using UnityEngine;

// KINEMATION third-party types (original)
using KINEMATION.FPSAnimationPack.Scripts.Player;
using KINEMATION.FPSAnimationPack.Scripts.Sounds;
using KINEMATION.FPSAnimationPack.Scripts.Weapon;
using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.ProceduralRecoilAnimationSystem.Runtime; // 如果你们工程里 KTransform/KMath/KCurves/KTwoBoneIK 在别的命名空间，请按实际改 using

namespace Gameplay.ThirdPartyAdapters.Kinemation
{
    /// <summary>
    /// Replace FPSPlayer (presentation interface) with minimal deviation:
    /// - No input polling, no character controller movement, no weapon switching.
    /// - Keep: ADS weight, gait smoothing, animator layers, IK chain in LateUpdate, IKMotion playback.
    /// - Expose: SetMove/SetLook/SetAiming/SetFireHeld/Reload/ChangeFireMode/Inspect...
    /// </summary>
    public sealed class KinemationController : MonoBehaviour
    {
        public float AdsWeight => _adsWeight;

        public FPSPlayerSettings playerSettings;

        [Header("Skeleton (same as FPSPlayer)")]
        [SerializeField] private Transform skeletonRoot;
        [SerializeField] private Transform weaponBone;
        [SerializeField] private Transform weaponBoneAdditive;
        [SerializeField] private Transform cameraPoint;
        [SerializeField] private IKTransforms rightHand;
        [SerializeField] private IKTransforms leftHand;

        [Header("Components")]
        [SerializeField] private KinemationWeapon weapon;                 // single weapon (hero prefab)
        [SerializeField] private KinemationPlayerSound playerSound;       // if you have KinemationPlayerSound, swap type
        [SerializeField] private RecoilAnimation recoilAnimation;  // procedural recoil component

        private KTwoBoneIkData _rightHandIk;
        private KTwoBoneIkData _leftHandIk;

        private float _adsWeight;

        private Animator _animator;

        private static readonly int RIGHT_HAND_WEIGHT = Animator.StringToHash("RightHandWeight");
        private static readonly int TAC_SPRINT_WEIGHT = Animator.StringToHash("TacSprintWeight");
        private static readonly int GRENADE_WEIGHT = Animator.StringToHash("GrenadeWeight");
        private static readonly int GAIT = Animator.StringToHash("Gait");
        private static readonly int IS_IN_AIR = Animator.StringToHash("IsInAir");
        private static readonly int INSPECT = Animator.StringToHash("Inspect");

        private int _tacSprintLayerIndex;
        private int _triggerDisciplineLayerIndex;
        private int _rightHandLayerIndex;

        private bool _isAiming;

        // “接口层输入状态”（由上层喂入）
        private Vector2 _moveInput;   // normalized
        private float _smoothGait;

        // FPSPlayer: _lookInput.y accumulative pitch, _lookInput.x yaw delta
        private Vector2 _lookInput;

        private bool _bSprinting;
        private bool _bTacSprinting;

        // IK Motion (copy from FPSPlayer)
        private float _ikMotionPlayBack;
        private KTransform _ikMotion = KTransform.Identity;
        private KTransform _cachedIkMotion = KTransform.Identity;
        private IKMotion _activeMotion;

        private KTransform _localCameraPoint;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (recoilAnimation == null) recoilAnimation = GetComponent<RecoilAnimation>();
            if (playerSound == null) playerSound = GetComponent<KinemationPlayerSound>();

            // Single-weapon: find in children if not wired
            if (weapon == null) weapon = GetComponentInChildren<KinemationWeapon>();

            // Cache animator layer indices (same as FPSPlayer.Start)
            if (_animator != null)
            {
                _triggerDisciplineLayerIndex = _animator.GetLayerIndex("TriggerDiscipline");
                _rightHandLayerIndex = _animator.GetLayerIndex("RightHand");
                _tacSprintLayerIndex = _animator.GetLayerIndex("TacSprint");
            }

            // Your requirement: weapon.Initialize() in Awake
            if (weapon != null)
            {
                weapon.Initialize(gameObject);
            }
        }

        private void Start()
        {
            // Copy FPSPlayer cameraPoint local transform cache
            KTransform root = new KTransform(transform);
            _localCameraPoint = root.GetRelativeTransform(new KTransform(cameraPoint), false);

            // Copy FPSPlayer pose computation, but for a single existing weapon instance
            // Equivalent to per-weapon loop in FPSPlayer.Start
            if (weapon != null && weaponBone != null)
            {
                KTransform weaponT = new KTransform(weaponBone);

                // rightHandPose = relative to weaponBone
                weapon.rightHandPose = new KTransform(rightHand.tip).GetRelativeTransform(weaponT, false);

                // localWeapon = root relative weaponBone
                var localWeapon = root.GetRelativeTransform(weaponT, false);
                localWeapon.rotation *= weapon.weaponSettings.rotationOffset;

                // adsPose computed from local camera point
                weapon.adsPose.position = _localCameraPoint.position - localWeapon.position;
                weapon.adsPose.rotation = Quaternion.Inverse(localWeapon.rotation);

                weapon.gameObject.SetActive(true);
                weapon.OnEquipped();
            }
        }

        // -------------------------
        // Presentation interface (called by LocalPlayerFacade / network events)
        // -------------------------

        public void SetMove(Vector2 move01)
        {
            _moveInput = move01;
            if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();
        }

        /// <summary>
        /// Equivalent to FPSPlayer.OnLook delta logic:
        /// - pitch accumulative with clamp [-90,90]
        /// - yaw is "delta yaw this frame" (or could be absolute if you want, but original uses delta)
        /// </summary>
        public void AddLookDelta(float yawDelta, float pitchDelta)
        {
            _lookInput.y = Mathf.Clamp(_lookInput.y - pitchDelta, -90f, 90f);
            _lookInput.x = yawDelta;
        }

        /// <summary>
        /// If you prefer directly setting absolute pitch (e.g., from your sampler), use this:
        /// (FPSPlayer stores pitch in _lookInput.y)
        /// </summary>
        public void SetPitch(float pitchAbs)
        {
            _lookInput.y = Mathf.Clamp(pitchAbs, -90f, 90f);
        }

        public void SetSprint(bool pressed)
        {
            _bSprinting = pressed;
            if (!_bSprinting) _bTacSprinting = false;
        }

        public void SetTacSprint(bool pressed)
        {
            if (!_bSprinting) return;
            _bTacSprinting = pressed;
        }

        public void SetAiming(bool pressed)
        {
            bool wasAiming = _isAiming;
            _isAiming = pressed;

            if (recoilAnimation != null)
                recoilAnimation.isAiming = _isAiming;

            // Copy FPSPlayer: only on change -> play sound + PlayIkMotion(aimingMotion)
            if (wasAiming != _isAiming)
            {
                if (playerSound != null) playerSound.PlayAimSound(_isAiming);
                if (playerSettings != null) PlayIkMotion(playerSettings.aimingMotion);
            }
        }

        public void SetFireHeld(bool held)
        {
            if (weapon == null) return;

            if (held) weapon.OnFirePressed();
            else weapon.OnFireReleased();
        }

        public void Reload()
        {
            if (weapon == null) return;
            weapon.OnReload(); // Copy FPSPlayer.OnReload
        }

        public void ChangeFireMode()
        {
            if (weapon == null) return;

            // Copy FPSPlayer.OnChangeFireMode
            var prevFireMode = weapon.ActiveFireMode;
            weapon.OnFireModeChange();

            if (prevFireMode != weapon.ActiveFireMode)
            {
                if (playerSound != null) playerSound.PlayFireModeSwitchSound();
                if (playerSettings != null) PlayIkMotion(playerSettings.fireModeMotion);
            }
        }

        public void Inspect()
        {
            if (_animator == null) return;
            _animator.CrossFade(INSPECT, 0.1f); // Copy FPSPlayer.OnInspect
        }

        public void JumpAnimOnly()
        {
            if (_animator == null) return;
            _animator.SetBool(IS_IN_AIR, true);
            Invoke(nameof(OnLand), 0.4f); // Copy FPSPlayer.OnJump demo timing
        }

        private void OnLand()
        {
            if (_animator == null) return;
            _animator.SetBool(IS_IN_AIR, false); // Copy FPSPlayer.OnLand
        }

        // -------------------------
        // Core loop (copy from FPSPlayer.Update, minus legacy input & controller movement)
        // -------------------------

        private float GetDesiredGait()
        {
            if (_bTacSprinting) return 3f;
            if (_bSprinting) return 2f;
            return _moveInput.magnitude;
        }

        private void Update()
        {
            if (_animator == null || playerSettings == null) return;
            if (weapon == null) return;

            _adsWeight = Mathf.Clamp01(
                _adsWeight + playerSettings.aimSpeed * Time.deltaTime * (_isAiming ? 1f : -1f)
            );

            _smoothGait = Mathf.Lerp(_smoothGait, GetDesiredGait(),
                KMath.ExpDecayAlpha(playerSettings.gaitSmoothing, Time.deltaTime));

            _animator.SetFloat(GAIT, _smoothGait);
            _animator.SetLayerWeight(_tacSprintLayerIndex, Mathf.Clamp01(_smoothGait - 2f));

            bool triggerAllowed = weapon.weaponSettings.useSprintTriggerDiscipline;

            _animator.SetLayerWeight(_triggerDisciplineLayerIndex,
                triggerAllowed ? _animator.GetFloat(TAC_SPRINT_WEIGHT) : 0f);

            _animator.SetLayerWeight(_rightHandLayerIndex, _animator.GetFloat(RIGHT_HAND_WEIGHT));

            // Copy FPSPlayer rig alignment to camera point (pitch only)
            Vector3 cameraPosition = -_localCameraPoint.position;

            transform.localRotation = Quaternion.Euler(_lookInput.y, 0f, 0f);
            transform.localPosition = transform.localRotation * cameraPosition - cameraPosition;

            // NOTE: FPSPlayer optionally rotates a CharacterController root by yaw delta and moves it.
            // Your project uses server snapshots for movement/rotation, so we do NOT do that here.
            // Keep yaw delta available for your own root rotation system if needed.
        }

        // -------------------------
        // IK pipeline (copy from FPSPlayer.LateUpdate and helpers)
        // -------------------------

        private void SetupIkData(ref KTwoBoneIkData ikData, in KTransform target, in IKTransforms transforms, float weight = 1f)
        {
            ikData.target = target;

            ikData.tip = new KTransform(transforms.tip);
            ikData.mid = ikData.hint = new KTransform(transforms.mid);
            ikData.root = new KTransform(transforms.root);

            ikData.hintWeight = weight;
            ikData.posWeight = weight;
            ikData.rotWeight = weight;
        }

        private void ApplyIkData(in KTwoBoneIkData ikData, in IKTransforms transforms)
        {
            transforms.root.rotation = ikData.root.rotation;
            transforms.mid.rotation = ikData.mid.rotation;
            transforms.tip.rotation = ikData.tip.rotation;
        }

        private void ProcessOffsets(ref KTransform weaponT)
        {
            var root = transform;
            KTransform rootT = new KTransform(root);
            var weaponOffset = weapon.weaponSettings.ikOffset;

            float mask = 1f - _animator.GetFloat(TAC_SPRINT_WEIGHT);
            weaponT.position = KAnimationMath.MoveInSpace(rootT, weaponT, weaponOffset, mask);

            var settings = weapon.weaponSettings;
            KAnimationMath.MoveInSpace(root, rightHand.root, settings.rightClavicleOffset, mask);
            KAnimationMath.MoveInSpace(root, leftHand.root, settings.leftClavicleOffset, mask);
        }

        private void ProcessAdditives(ref KTransform weaponT)
        {
            KTransform rootT = new KTransform(skeletonRoot);
            KTransform additive = rootT.GetRelativeTransform(new KTransform(weaponBoneAdditive), false);

            float weight = Mathf.Lerp(1f, 0.3f, _adsWeight) * (1f - _animator.GetFloat(GRENADE_WEIGHT));

            weaponT.position = KAnimationMath.MoveInSpace(rootT, weaponT, additive.position, weight);
            weaponT.rotation = KAnimationMath.RotateInSpace(rootT, weaponT, additive.rotation, weight);
        }

        private void ProcessRecoil(ref KTransform weaponT)
        {
            KTransform recoil = new KTransform()
            {
                rotation = recoilAnimation.OutRot,
                position = recoilAnimation.OutLoc,
            };

            KTransform root = new KTransform(transform);
            weaponT.position = KAnimationMath.MoveInSpace(root, weaponT, recoil.position, 1f);
            weaponT.rotation = KAnimationMath.RotateInSpace(root, weaponT, recoil.rotation, 1f);
        }

        private void ProcessAds(ref KTransform weaponT)
        {
            var weaponOffset = weapon.weaponSettings.ikOffset;
            var adsPose = weaponT;

            KTransform aimPoint = KTransform.Identity;

            aimPoint.position = -weaponBone.InverseTransformPoint(weapon.aimPoint.position);
            aimPoint.position -= weapon.weaponSettings.aimPointOffset;
            aimPoint.rotation = Quaternion.Inverse(weaponBone.rotation) * weapon.aimPoint.rotation;

            KTransform root = new KTransform(transform);
            adsPose.position = KAnimationMath.MoveInSpace(root, adsPose,
                weapon.adsPose.position - weaponOffset, 1f);
            adsPose.rotation =
                KAnimationMath.RotateInSpace(root, adsPose,
                    weapon.adsPose.rotation, 1f);

            KTransform cameraPose = root.GetWorldTransform(_localCameraPoint, false);

            float adsBlendWeight = weapon.weaponSettings.adsBlend;
            adsPose.position = Vector3.Lerp(cameraPose.position, adsPose.position, adsBlendWeight);
            adsPose.rotation = Quaternion.Slerp(cameraPose.rotation, adsPose.rotation, adsBlendWeight);

            adsPose.position = KAnimationMath.MoveInSpace(root, adsPose, aimPoint.rotation * aimPoint.position, 1f);
            adsPose.rotation = KAnimationMath.RotateInSpace(root, adsPose, aimPoint.rotation, 1f);

            float weight = KCurves.EaseSine(0f, 1f, _adsWeight);

            weaponT.position = Vector3.Lerp(weaponT.position, adsPose.position, weight);
            weaponT.rotation = Quaternion.Slerp(weaponT.rotation, adsPose.rotation, weight);
        }

        private KTransform GetWeaponPose()
        {
            KTransform defaultWorldPose =
                new KTransform(rightHand.tip).GetWorldTransform(weapon.rightHandPose, false);
            float weight = _animator.GetFloat(RIGHT_HAND_WEIGHT);

            return KTransform.Lerp(new KTransform(weaponBone), defaultWorldPose, weight);
        }

        // IK Motion (copy)
        private void PlayIkMotion(IKMotion newMotion)
        {
            _ikMotionPlayBack = 0f;
            _cachedIkMotion = _ikMotion;
            _activeMotion = newMotion;
        }

        private void ProcessIkMotion(ref KTransform weaponT)
        {
            if (_activeMotion == null) return;

            _ikMotionPlayBack = Mathf.Clamp(_ikMotionPlayBack + _activeMotion.playRate * Time.deltaTime, 0f,
                _activeMotion.GetLength());

            Vector3 positionTarget = _activeMotion.translationCurves.GetValue(_ikMotionPlayBack);
            positionTarget.x *= _activeMotion.translationScale.x;
            positionTarget.y *= _activeMotion.translationScale.y;
            positionTarget.z *= _activeMotion.translationScale.z;

            Vector3 rotationTarget = _activeMotion.rotationCurves.GetValue(_ikMotionPlayBack);
            rotationTarget.x *= _activeMotion.rotationScale.x;
            rotationTarget.y *= _activeMotion.rotationScale.y;
            rotationTarget.z *= _activeMotion.rotationScale.z;

            _ikMotion.position = positionTarget;
            _ikMotion.rotation = Quaternion.Euler(rotationTarget);

            if (!Mathf.Approximately(_activeMotion.blendTime, 0f))
            {
                _ikMotion = KTransform.Lerp(_cachedIkMotion, _ikMotion,
                    _ikMotionPlayBack / _activeMotion.blendTime);
            }

            var root = new KTransform(transform);
            weaponT.position = KAnimationMath.MoveInSpace(root, weaponT, _ikMotion.position, 1f);
            weaponT.rotation = KAnimationMath.RotateInSpace(root, weaponT, _ikMotion.rotation, 1f);
        }

        private void LateUpdate()
        {
            if (_animator == null || playerSettings == null) return;
            if (weapon == null || recoilAnimation == null) return;

            // Copy FPSPlayer.LateUpdate
            KAnimationMath.RotateInSpace(transform, rightHand.tip,
                weapon.weaponSettings.rightHandSprintOffset, _animator.GetFloat(TAC_SPRINT_WEIGHT));

            KTransform weaponTransform = GetWeaponPose();

            weaponTransform.rotation = KAnimationMath.RotateInSpace(weaponTransform, weaponTransform,
                weapon.weaponSettings.rotationOffset, 1f);

            KTransform rightHandTarget = weaponTransform.GetRelativeTransform(new KTransform(rightHand.tip), false);
            KTransform leftHandTarget = weaponTransform.GetRelativeTransform(new KTransform(leftHand.tip), false);

            ProcessOffsets(ref weaponTransform);
            ProcessAds(ref weaponTransform);
            ProcessAdditives(ref weaponTransform);
            ProcessIkMotion(ref weaponTransform);   // IK layer (your requirement)
            ProcessRecoil(ref weaponTransform);

            weaponBone.position = weaponTransform.position;
            weaponBone.rotation = weaponTransform.rotation;

            rightHandTarget = weaponTransform.GetWorldTransform(rightHandTarget, false);
            leftHandTarget = weaponTransform.GetWorldTransform(leftHandTarget, false);

            SetupIkData(ref _rightHandIk, rightHandTarget, rightHand, playerSettings.ikWeight);
            SetupIkData(ref _leftHandIk, leftHandTarget, leftHand, playerSettings.ikWeight);

            KTwoBoneIK.Solve(ref _rightHandIk);
            KTwoBoneIK.Solve(ref _leftHandIk);

            ApplyIkData(_rightHandIk, rightHand);
            ApplyIkData(_leftHandIk, leftHand);
        }

        // Weapon recoil hook (FPSPlayer has private void OnFire() { _recoilAnimation.Play(); })
        // Keep the same name/signature so third-party weapon can SendMessage("OnFire") if it does.
        private void OnFire()
        {
            recoilAnimation.Play();
        }
    }
}
