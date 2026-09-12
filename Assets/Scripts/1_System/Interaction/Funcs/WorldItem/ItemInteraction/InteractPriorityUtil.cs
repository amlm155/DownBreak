namespace Interaction
{
    /// <summary>
    /// 交互优先级解析
    /// 排序规则 可拾取物品 &gt; 按压式交互 &gt; 容器/工作台/可拆物 &gt; 普通可交互
    /// 同一优先级再按命中距离由近到远
    /// </summary>
    public static class InteractPriorityUtil
    {
        /// <summary> 可拾取世界物品 </summary>
        public const int ItemPriority = 100;

        /// <summary> 按压式交互 开关 按钮 门 </summary>
        public const int PressPriority = 60;

        /// <summary> 容器 工作台 可拆物 </summary>
        public const int ContainerPriority = 20;

        /// <summary> 普通可交互 </summary>
        public const int DefaultPriority = 0;

        /// <summary>
        /// 取交互优先级
        /// </summary>
        public static int GetPriority(IInteractableInterface interactable)
        {
            if (interactable == null)
                return int.MinValue;

            // 显式声明优先级的以声明为准
            if (interactable is IInteractPriorityProvider provider)
                return provider.InteractPriority;

            if (interactable is IItemInterface)
                return ItemPriority;

            if (interactable is IPressInteractable)
                return PressPriority;

            if (interactable is IScrapInterface
                || interactable is IPlaceAndBreakInterface
                || interactable is IWorkbenchInterface)
            {
                return ContainerPriority;
            }

            return DefaultPriority;
        }
    }
}
