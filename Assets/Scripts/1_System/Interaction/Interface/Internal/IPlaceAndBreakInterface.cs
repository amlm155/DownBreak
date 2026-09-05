namespace Interaction
{
    /// <summary>
    /// 可放置可破坏物体 手持时进入放置 落地后左键攻击扣耐久 F 键交互
    /// </summary>
    public interface IPlaceAndBreakInterface
    {
        /// <summary> 物品表 ID </summary>
        int ItemTableId { get; }
    }
}
