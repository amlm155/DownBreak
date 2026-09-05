using System;
using cfg.item;
using UnityEngine;
namespace MieMieUIFrameWork.Runtime
{
    
    /// <summary>
    /// 轮盘扇区通用展示数据 由 ItemWheelController 动态加入
    /// </summary>
    [Serializable]
    public class ItemWheelData
    {
        /// <summary> 扇区图标 </summary>
        public Sprite Icon;
    
        /// <summary> 中心说明文案 </summary>
        public string Info;
    
        /// <summary> 表 ID </summary>
        public int ExcelItemId;
    
        /// <summary> 实例 ID </summary>
        public string InstancedItemId;
    
        /// <summary> 物品大类 </summary>
        public EItemType ItemType;
    
        /// <summary> 运行时物品引用 </summary>
        public MmInventory.ItemRtData ItemRtData;
    }
    
}