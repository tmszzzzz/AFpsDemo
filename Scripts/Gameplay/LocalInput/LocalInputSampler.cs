using UnityEngine;

namespace Gameplay.LocalInput
{
    /// <summary>
    /// 纯本地采样器：WASD + 鼠标视角 + 常用按键。
    /// 后续切换新 Input System / Cinemachine，只改这里。
    /// </summary>
    public sealed class LocalInputSampler : MonoBehaviour
    {
        [Header("Look")]
        public bool lockCursor = true;
        public float mouseSensitivity = 2.0f;
        public float pitchMin = -90f;
        public float pitchMax = 90f;

        [Header("Keys")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode ultraKey = KeyCode.Q;
        public KeyCode skillEKey = KeyCode.E;
        public KeyCode skillShiftKey = KeyCode.LeftShift;
        public KeyCode skillCtrlKey = KeyCode.LeftControl;
        public KeyCode hitVKey = KeyCode.V;
        public KeyCode reloadKey = KeyCode.R;

        [Header("Mouse")]
        public int firePriMouseButton = 0;
        public int fireSecMouseButton = 1;

        private float _yaw;
        private float _pitch;

        public void SetAngles(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        public LocalInputFrame Sample()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            _yaw += mx * mouseSensitivity;
            _pitch -= my * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);

            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            LocalButtons b = LocalButtons.NONE;
            if (Input.GetKey(jumpKey)) b |= LocalButtons.BUTTON_JUMP;
            if (Input.GetKey(skillEKey)) b |= LocalButtons.BUTTON_SKILL_E;
            if (Input.GetKey(skillShiftKey)) b |= LocalButtons.BUTTON_SKILL_SHIFT;
            if (Input.GetKey(ultraKey)) b |= LocalButtons.BUTTON_ULTRA;
            if (Input.GetKey(skillCtrlKey)) b |= LocalButtons.BUTTON_SKILL_CTRL;
            if (Input.GetKey(hitVKey)) b |= LocalButtons.BUTTON_HIT_V;
            if (Input.GetMouseButton(fireSecMouseButton)) b |= LocalButtons.MOUSE_FIRE_SEC;
            if (Input.GetMouseButton(firePriMouseButton)) b |= LocalButtons.MOUSE_FIRE_PRI;
            if (Input.GetKeyDown(reloadKey)) b |= LocalButtons.BUTTON_RELOAD;

            return new LocalInputFrame
            {
                moveX = Mathf.Clamp(moveX, -1f, 1f),
                moveY = Mathf.Clamp(moveY, -1f, 1f),
                yaw = _yaw,
                pitch = _pitch,
                buttons = b,
            };
        }
    }
}