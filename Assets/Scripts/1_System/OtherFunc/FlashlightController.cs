using DBGameSystem;
using DBWeaponSystem;
using Interaction.Player;
using UnityEngine;

namespace DBOtherFunc
{
    [DisallowMultipleComponent]
    public class FlashlightController : MonoBehaviour
    {
        /// <summary>
        /// 手电聚光灯
        /// </summary>
        [SerializeField]
        private Light flashlightLight;

        /// <summary>
        /// 当前开灯状态
        /// </summary>
        private bool isLightOn;

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Awake()
        {
            InitComponents();
        }

        /// <summary>
        /// 处理手电开关输入
        /// </summary>
        private void Update()
        {
            var weaponSystem = GameHub.Get<IWeaponSystem>();
            bool isHeld = weaponSystem != null && weaponSystem.IsEquippedWeapon(gameObject);
            if (!isHeld)
            {
                if (isLightOn)
                    SetLightEnabled(false);
                return;
            }

            var playerInput = GameHub.Get<IPlayerInput>();
            if (playerInput != null && playerInput.IsFlashlightPressed)
                SetLightEnabled(!isLightOn);
        }

        /// <summary>
        /// 初始化手电灯光组件
        /// </summary>
        private void InitComponents()
        {
            flashlightLight = GetComponentInChildren<Light>(true);
            SetLightEnabled(false);
        }

        /// <summary>
        /// 设置手电灯光状态
        /// </summary>
        private void SetLightEnabled(bool isEnabled)
        {
            isLightOn = isEnabled;
            flashlightLight.enabled = isEnabled;
        }
    }
}
