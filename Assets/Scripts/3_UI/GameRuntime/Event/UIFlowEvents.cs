using MiMieEventBus;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// UI 流程命令事件 由 UIInputEventFuck 发布 PlayerPanel 订阅
    /// 命名 On/操作/对象/手势 Started按下瞬间 Canceled松开瞬间
    /// </summary>
    public static class UIFlowEvents
    {
        /// <summary>
        /// 请求打开或关闭背包
        /// </summary>
        public static readonly EventKey OnOpenBagStarted =
            new EventKey("UiFlow.OnOpenBagStarted");

        /// <summary>
        /// 请求打开设置
        /// </summary>
        public static readonly EventKey OnOpenSettingStarted =
            new EventKey("UiFlow.OnOpenSettingStarted");

        /// <summary>
        /// 请求打开物品轮盘
        /// </summary>
        public static readonly EventKey OnItemWheelStarted =
            new EventKey("UiFlow.OnItemWheelStarted");

        /// <summary>
        /// 请求关闭物品轮盘
        /// </summary>
        public static readonly EventKey OnItemWheelCanceled =
            new EventKey("UiFlow.OnItemWheelCanceled");

        /// <summary>
        /// 请求打开或关闭制作
        /// </summary>
        public static readonly EventKey OnOpenCraftStarted =
            new EventKey("UiFlow.OnOpenCraftStarted");
    }
}
