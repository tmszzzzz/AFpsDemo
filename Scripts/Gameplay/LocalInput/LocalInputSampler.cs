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
        public float pitchMin = -80f;
        public float pitchMax = 80f;

        [Header("Keys")]
        public KeyCode jumpKey = KeyCode.Space;
        public KeyCode useKey = KeyCode.E;
        public KeyCode sprintKey = KeyCode.LeftShift;
        public KeyCode reloadKey = KeyCode.R;

        [Header("Mouse")]
        public int fireMouseButton = 0;
        public int aimMouseButton = 1;

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

            LocalButtons b = LocalButtons.None;
            if (Input.GetKey(jumpKey)) b |= LocalButtons.Jump;
            if (Input.GetKey(useKey)) b |= LocalButtons.Use;
            if (Input.GetKey(sprintKey)) b |= LocalButtons.Sprint;
            if (Input.GetMouseButton(aimMouseButton)) b |= LocalButtons.Aim;
            if (Input.GetMouseButton(fireMouseButton)) b |= LocalButtons.Fire;
            if (Input.GetKeyDown(reloadKey)) b |= LocalButtons.Reload;

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