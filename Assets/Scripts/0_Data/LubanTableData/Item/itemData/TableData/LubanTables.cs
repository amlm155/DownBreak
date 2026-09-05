using System.Collections.Generic;
using System.IO;
using Luban.SimpleJSON;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// Luban 物品表加载与查询
    /// </summary>
    public static class LubanTables
    {
        /// <summary> StreamingAssets 下物品配置相对目录 </summary>
        public const string ConfigRelativeDir = "Config/Item";

        /// <summary> 已加载的 Tables </summary>
        private static cfg.Tables tables;

        /// <summary> 全部物品适配列表 </summary>
        private static List<IItemTableData> itemList;

        /// <summary> 全部物品适配字典 </summary>
        private static Dictionary<int, IItemTableData> itemDict;

        /// <summary> Luban Tables 实例 </summary>
        public static cfg.Tables Tables
        {
            get
            {
                EnsureLoaded();
                return tables;
            }
        }

        /// <summary> 全部物品模版 </summary>
        public static IReadOnlyList<IItemTableData> ItemList
        {
            get
            {
                EnsureLoaded();
                return itemList;
            }
        }

        /// <summary> 物品模版字典 </summary>
        public static IReadOnlyDictionary<int, IItemTableData> ItemDict
        {
            get
            {
                EnsureLoaded();
                return itemDict;
            }
        }

        /// <summary>
        /// 确保已加载
        /// </summary>
        public static void EnsureLoaded()
        {
            if (tables != null)
                return;

            tables = new cfg.Tables(LoadJson);
            BuildItemCache();
        }

        /// <summary>
        /// 强制重载
        /// </summary>
        public static void Reload()
        {
            tables = null;
            itemList = null;
            itemDict = null;
            EnsureLoaded();
        }

        /// <summary>
        /// 按 id 取物品模版
        /// </summary>
        public static bool TryGetItem(int excelItemId, out IItemTableData itemTableData)
        {
            EnsureLoaded();
            return itemDict.TryGetValue(excelItemId, out itemTableData);
        }

        /// <summary>
        /// 按 id 取具体类型行
        /// </summary>
        public static bool TryGetRaw<T>(int excelItemId, out T raw) where T : cfg.item.Item
        {
            raw = null;
            if (!TryGetItem(excelItemId, out var itemTableData))
                return false;

            if (itemTableData is not LubanItemTableData adapter)
                return false;

            if (adapter.Raw is not T typed)
                return false;

            raw = typed;
            return true;
        }

        /// <summary>
        /// 汇总各类型表
        /// </summary>
        private static void BuildItemCache()
        {
            itemList = new List<IItemTableData>(64);
            itemDict = new Dictionary<int, IItemTableData>(64);
            AddItems(tables.TbEquipment.DataList);
            AddItems(tables.TbWeapon.DataList);
            AddItems(tables.TbFoodOrWater.DataList);
            AddItems(tables.TbMedicine.DataList);
            AddItems(tables.TbMaterial.DataList);
            AddItems(tables.TbBlueprint.DataList);
            AddItems(tables.TbFurniture.DataList);
            AddItems(tables.TbStorageBox.DataList);
        }

        /// <summary>
        /// 写入缓存
        /// </summary>
        private static void AddItems<T>(IReadOnlyList<T> dataList) where T : cfg.item.Item
        {
            for (int i = 0; i < dataList.Count; i++)
            {
                var adapter = new LubanItemTableData(dataList[i]);
                itemList.Add(adapter);
                itemDict[adapter.ExcelItemId] = adapter;
            }
        }

        /// <summary>
        /// 从 StreamingAssets 读 json
        /// </summary>
        private static JSONNode LoadJson(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath,
                 ConfigRelativeDir,
                  fileName + ".json");
            if (!File.Exists(path))
                throw new FileNotFoundException("Luban 配置缺失", path);

            string text = File.ReadAllText(path);
            return JSON.Parse(text);
        }
    }
}
