using MieMieFrameWork;
using MieMieFrameWork.M_InputSystem;
using MiMieEventBus;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// UI 流程输入路由 InputManager 命令直发 UIFlowEvents
    /// 由 GameUIWarmUpFuck 确保存在
    /// </summary>
    public class UIInputEventFuck : MonoBehaviour
    {
        /// <summary> 输入管理器缓存 </summary>
        private InputManager inputManager;

        /// <summary>
        /// 绑定输入命令
        /// </summary>
        private void Start()
        {
            if (ModuleHub.Instance == null)
                return;

            inputManager = ModuleHub.Instance.GetManager<InputManager>();
            if (inputManager == null)
                return;

            inputManager.OnOpenBagStarted += OnOpenBagStarted;
            inputManager.OnOpenSettingStarted += OnOpenSettingStarted;
            inputManager.OnItemWheelStarted += OnItemWheelStarted;
            inputManager.OnItemWheelCanceled += OnItemWheelCanceled;
            inputManager.OnOpenCraftStarted += OnOpenCraftStarted;
        }

        /// <summary>
        /// 退订
        /// </summary>
        private void OnDestroy()
        {
            if (inputManager == null)
                return;

            inputManager.OnOpenBagStarted -= OnOpenBagStarted;
            inputManager.OnOpenSettingStarted -= OnOpenSettingStarted;
            inputManager.OnItemWheelStarted -= OnItemWheelStarted;
            inputManager.OnItemWheelCanceled -= OnItemWheelCanceled;
            inputManager.OnOpenCraftStarted -= OnOpenCraftStarted;
            inputManager = null;
        }

        /// <summary>
        /// 转发打开背包
        /// </summary>
        private void OnOpenBagStarted()
        {
            MmGlobalEventBus.GlobalBus.Publish(UIFlowEvents.OnOpenBagStarted);
        }

        /// <summary>
        /// 转发打开设置
        /// </summary>
        private void OnOpenSettingStarted()
        {
            MmGlobalEventBus.GlobalBus.Publish(UIFlowEvents.OnOpenSettingStarted);
        }

        /// <summary>
        /// 转发打开物品轮盘
        /// </summary>
        private void OnItemWheelStarted()
        {
            MmGlobalEventBus.GlobalBus.Publish(UIFlowEvents.OnItemWheelStarted);
        }

        /// <summary>
        /// 转发关闭物品轮盘
        /// </summary>
        private void OnItemWheelCanceled()
        {
            MmGlobalEventBus.GlobalBus.Publish(UIFlowEvents.OnItemWheelCanceled);
        }

        /// <summary>
        /// 转发打开制作
        /// </summary>
        private void OnOpenCraftStarted()
        {
            MmGlobalEventBus.GlobalBus.Publish(UIFlowEvents.OnOpenCraftStarted);
        }
    }
}
