namespace Interaction
{
    /// <summary>
    /// 可拾取物品源 场景掉落物实现
    /// </summary>
    public interface IItemInterface
    {
        /// <summary> 物品表 ID </summary>
        int ItemTableID { get; }

        /// <summary>
        /// 运行时绑定表 ID 丢弃实例化后调用
        /// </summary>
        void BindItemTableID(int itemTableId);

        /// <summary>
        /// 拾取成功后回调 如销毁场景物
        /// </summary>
        void OnPickup();
    }
}
