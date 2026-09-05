namespace MmInventory
{
    /// <summary>
    /// 世界掉落物上的实例快照载体
    /// </summary>
    public interface IItemSaveCarrier
    {
        /// <summary> 是否已绑定有效快照 </summary>
        bool HasSaveData { get; }

        /// <summary> 当前绑定的存档快照 </summary>
        ItemSaveData SaveData { get; }

        /// <summary>
        /// 绑定实例快照 同步表 ID
        /// </summary>
        void BindSaveData(ItemSaveData saveData);
    }
}
