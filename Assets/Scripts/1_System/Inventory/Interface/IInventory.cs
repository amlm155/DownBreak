using System.Collections.Generic;
using DBGameSystem;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品数据接口 用于沟通各层与物品表数据 方法实现在 ItemRtDataMgr 类里
    /// </summary>
    public interface IInventory : IGameService
    {
        /// <summary> 物品视图基础预制体 </summary>
        GameObject ItemViewPrefab { get; }

        /// <summary> 物品数据字典 key物品ExcelID value物品配置表数据 </summary>
        IReadOnlyDictionary<int, IItemTableData> ItemDataDict { get; }

        /// <summary>
        /// 注册所有物品数据
        /// </summary>
        void RegisterItemData();

        /// <summary>
        /// 根据id获取物品数据
        /// </summary>
        T GetItemData<T>(int id) where T : IItemTableData;
    }
}
