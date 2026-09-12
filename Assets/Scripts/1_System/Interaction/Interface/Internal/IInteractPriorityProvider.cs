namespace Interaction
{
    /// <summary>
    /// 交互优先级契约 数值越大越优先
    /// 多个可交互物重叠时(例如架子上摆着的可拾取物品) 用它压过架子/容器这类大体量交互物
    /// </summary>
    public interface IInteractPriorityProvider
    {
        /// <summary>
        /// 交互优先级 越大越优先
        /// </summary>
        int InteractPriority { get; }
    }
}
