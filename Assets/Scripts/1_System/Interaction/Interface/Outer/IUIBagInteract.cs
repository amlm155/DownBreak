using DBGameSystem;
using Cysharp.Threading.Tasks;
using cfg.item;
using MmInventory;
using UnityEngine;

namespace Interaction.Player
{
    /// <summary>
    /// 背包操作接口 用于沟通交互拾取与背包 UI 方法实现在 BagPanel 类里
    /// 由于物理程序集限制 无法直接在系统层调用任何 UI 方法 需要通过此接口进行中转
    /// 通常来说 接口是跟实现模块的 但是这类跨程序集的接口比较特殊 统一放到交互模块层
    /// </summary>
    public interface IUIBagInteract : IGameService
    {
        /// <summary>
        /// 拾取世界物 有快照则还原实例 否则按表 ID 新建
        /// </summary>
        bool TryPickupWorldItem(IItemInterface itemSource);

        /// <summary>
        /// 按已有实例入包 可开装备槽 满包提示
        /// </summary>
        bool TryPickupExistingItem(ItemRtData itemRtData);

        /// <summary>
        /// 按表 ID 新建实例入包 搜刮/调试用
        /// </summary>
        bool TryPickupItem(int itemTableId);

        /// <summary>
        /// 判断指定装备槽容器是否已激活
        /// </summary>
        bool HasContainer(EEquipSlot eSlot);

        /// <summary>
        /// 打开搜刮容器栏
        /// </summary>
        bool TryOpenScrapContainer(int scrapContainerId, bool alreadyLooted, Object owner = null);

        /// <summary>
        /// 打开玩家储物箱栏
        /// </summary>
        bool TryOpenStorageBox(int storageBoxItemId, Object owner = null);

        /// <summary>
        /// 容器被打碎时吐出该容器自己的物品 开着从搜刮栏取 关着从容器快照取
        /// </summary>
        bool TryDropOpenedContainerItems(Object owner, Vector3 worldPosition);

        /// <summary>
        /// 打开物品右键菜单
        /// </summary>
        void ShowItemMenu(ItemView itemView);

        /// <summary>
        /// 关闭物品右键菜单
        /// </summary>
        void HideItemMenu();

        /// <summary>
        /// 按表 ID 给予物品 先入包 满了丢到脚下
        /// </summary>
        bool TryGiveItem(int itemTableId, int stackCount);

        /// <summary>
        /// 在世界坐标生成掉落物 破坏掉落用
        /// </summary>
        bool TrySpawnWorldItem(int itemTableId, int stackCount, Vector3 worldPosition);

        /// <summary>
        /// 异步在世界坐标生成掉落物 破坏掉落用
        /// </summary>
        UniTask<bool> TrySpawnWorldItemAsync(int itemTableId, int stackCount, Vector3 worldPosition);
    }
}
