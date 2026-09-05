using System.Collections.Generic;
using MmInventory;

namespace Interaction
{
    /// <summary>
    /// 世界容器自己持有的物品快照 
    /// </summary>
    public interface IWorldContainerContents
    {
        /// <summary>
        /// 用一份快照覆盖容器内存 
        /// </summary>
        void ReplaceStoredItems(List<ItemSaveData> itemSaveDataList);

        /// <summary>
        /// 取出并清空容器内存
        /// </summary>
        List<ItemSaveData> TakeStoredItems();

        /// <summary>
        /// 只读当前容器内存
        /// </summary>
        IReadOnlyList<ItemSaveData> PeekStoredItems();
    }
}
