using System.Collections.Generic;
using MieMieFrameWork;
using UnityEngine;

namespace MmInventory
{
    public class ItemRtDataMgr : SingletonMono<ItemRtDataMgr>, IInventory
    {

        /// <summary> 物品视图基础预制体 </summary>
        [SerializeField]
        private GameObject itemViewPrefab;

        /// <summary>
        /// 物品视图基础预制体
        /// </summary>
        public GameObject ItemViewPrefab => itemViewPrefab;

        /// <summary> 物品数据字典 key物品ExcelID value物品配置表数据 </summary>
        private Dictionary<int, IItemTableData> itemDataDict = new();
        public IReadOnlyDictionary<int, IItemTableData> ItemDataDict => itemDataDict;

        protected override bool DontDestroyOnLoadEnabled => true;
        
        protected override void Awake()
        {
            base.Awake();
            RegisterItemData();
        }

        /// <summary>
        /// 注册所有物品数据
        /// </summary>
        public void RegisterItemData()
        {
            itemDataDict.Clear();
            LubanTables.EnsureLoaded();
            var itemList = LubanTables.ItemList;
            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                itemDataDict[item.ExcelItemId] = item;
            }

            Debug.Log($"物品数据注册完成 总计加载 {itemDataDict.Count} 条物品");
        }

        /// <summary>
        /// 根据id获取物品数据
        /// </summary>
        public T GetItemData<T>(int id) where T : IItemTableData
        {
            if (itemDataDict.Count == 0)
            {
                Debug.LogError("物品数据未加载完成");
                return default;
            }

            if (itemDataDict.TryGetValue(id, out var data))
                return (T)data;

            Debug.LogWarning($"未找到ID:{id} 的物品");
            return default;
        }
    }
}
