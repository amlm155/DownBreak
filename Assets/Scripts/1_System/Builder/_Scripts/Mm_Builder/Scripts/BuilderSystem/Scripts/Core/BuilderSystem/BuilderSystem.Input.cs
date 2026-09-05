using UnityEngine;
using UnityEngine.InputSystem;

namespace Mm_Budier
{
    public partial class BuilderSystem
    {
        public bool placeButtonPressed;
        public bool breakButtonPressed;
        public bool rotateButtonPressed;

        /// <summary>
        /// 轮询默认输入 外部也可用 SetXxxButtonPressed 注入
        /// </summary>
        private void PollInput()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            if (mouse.leftButton.wasPressedThisFrame)
                SetPlaceButtonPressed();

            if (mouse.rightButton.wasPressedThisFrame)
                CancelPlace();

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SetRotateButtonPressed();
        }

        public void SetPlaceButtonPressed(System.Action action = null)
        {
            placeButtonPressed = true;
            action?.Invoke();
        }

        public void SetBreakButtonPressed(System.Action action = null)
        {
            breakButtonPressed = true;
            action?.Invoke();
        }

        public void SetRotateButtonPressed(System.Action action = null)
        {
            rotateButtonPressed = true;
            action?.Invoke();
        }

        public void ClearPlaceButtonPressed()
        {
            placeButtonPressed = false;
        }

        public void ClearBreakButtonPressed()
        {
            breakButtonPressed = false;
        }

        public void ClearRotateButtonPressed()
        {
            rotateButtonPressed = false;
        }
    }
}