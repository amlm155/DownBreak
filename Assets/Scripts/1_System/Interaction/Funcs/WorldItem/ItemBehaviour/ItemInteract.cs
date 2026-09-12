using cfg.item;
using DBWeaponSystem;
using Interaction.Combat;
using MmInventory;
using UnityEngine;

namespace Interaction
{
/// <summary>
/// 世界拾取物 勾表 ID / 实例快照 描边色跟稀有度
/// </summary>
[RequireComponent(typeof(InteractOutline))]
public class ItemInteract : InteractableBase, IItemInterface, IItemSaveCarrier, global::Interaction.Combat.IDurabilityProvider
{
    /// <summary> 描边颜色 alpha Color32 30 </summary>
    private const float OutlineColorAlpha = 30f / 255f;

    [SerializeField]
    private int itemTableID;

    [SerializeField]
    /// <summary> 静态摆放物 编辑器摆好后固定在原地 不进入世界物理 </summary>
    private bool isStaticPlaced;

    [SerializeField]
    /// <summary> 世界掉落实例快照 </summary>
    private ItemSaveData saveData;

    /// <summary> 外圈高亮 </summary>
    private InteractOutline outline;

    public int ItemTableID => itemTableID;

    /// <summary> 是否为静态摆放物 </summary>
    public bool IsStaticPlaced => isStaticPlaced;

    /// <summary>
    /// 可拾取物品优先级最高 压在架子/容器上时也要先被聚焦
    /// </summary>
    public override int InteractPriority => InteractPriorityUtil.ItemPriority;

    public bool HasSaveData =>
        saveData != null && saveData.excelItemId > 0;

    public ItemSaveData SaveData => saveData;

    /// <summary>
    /// 运行时写入表 ID 丢弃实例化后调用
    /// </summary>
    public void SetItemTableID(int tableId)
    {
        itemTableID = tableId;
        ApplyOutlineByRarity();
    }

    public void BindItemTableID(int itemTableId)
    {
        saveData = null;
        SetItemTableID(itemTableId);
    }

    /// <summary>
    /// 绑定实例快照 同步表 ID 与描边
    /// </summary>
    public void BindSaveData(ItemSaveData data)
    {
        saveData = data;
        if (data == null)
        {
            itemTableID = 0;
            return;
        }

        SetItemTableID(data.excelItemId);
    }

    private void Awake()
    {
        InitComponents();
        outline.ApplyStandardStyle();
        ApplyOutlineByRarity();
    }

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitComponents()
    {
        outline = GetComponent<InteractOutline>();
    }

        private void Start()
        {
            // 手持表现
            if (IsHeldByHandSocket())
            {
                ItemPhysicsUtil.SetHeld(gameObject);
                return;
            }

            // 编辑器摆放物 原地固定 不随机姿态也不下落 不会被碰撞挤压顶飞
            if (isStaticPlaced)
            {
                ItemPhysicsUtil.PrepareStaticPlaced(gameObject);
                return;
            }

            // 世界掉落表现 随机姿态自由下落 落地后转 Trigger
            if (GetComponent<ItemWorldPhysics>() == null)
                gameObject.AddComponent<ItemWorldPhysics>();

        ItemPhysicsUtil.PrepareWorldDrop(gameObject);
    }

    /// <summary>
    /// 判断当前物品是否挂在玩家手部挂点
    /// </summary>
    private bool IsHeldByHandSocket()
    {
        Transform parent = transform.parent;
        if (parent == null)
            return false;

        var handPos = GetComponentInParent<WeaponAsyncHandPos>();
        if (handPos == null)
            return false;

        return parent == handPos.LeftHandWeaponPos
            || parent == handPos.RightHandWeaponPos;
    }

    public override void OnFocusEnter(InteractionContext ctx)
    {
        outline.Show();
    }

    public override void OnFocusExit(InteractionContext ctx)
    {
        outline.Hide();
    }

    public void OnPickup()
    {
        Destroy(gameObject);
    }

    public override string GetPromptText()
    {
        return "我是方块";
    }

    public override bool CanInteract(InteractionContext ctx)
    {
        return true;
    }

    /// <summary>
    /// 读取当前耐久与最大耐久
    /// </summary>
    /// <param name="currentDurability">当前耐久</param>
    /// <param name="maxDurability">最大耐久</param>
    /// <returns>是否成功读取</returns>
    public bool TryGetDurability(out int currentDurability, out int maxDurability)
    {
        currentDurability = 0;
        maxDurability = 0;

        if (HasSaveData)
        {
            currentDurability = saveData.currDurability;
            maxDurability = saveData.maxDurability;
            return maxDurability > 0;
        }

        if (!LubanTables.TryGetItem(itemTableID, out var itemTableData))
            return false;

        maxDurability = itemTableData.MaxDurability;
        currentDurability = maxDurability;
        return maxDurability > 0;
    }

    /// <summary>
    /// 按表或快照稀有度刷新描边色 a 固定 30
    /// </summary>
    private void ApplyOutlineByRarity()
    {
        if (itemTableID <= 0)
            return;

        EItemRarity eRarity = EItemRarity.White;
        if (HasSaveData)
        {
            eRarity = saveData.itemRarity;
        }
        else if (LubanTables.TryGetItem(itemTableID, out var itemTableData))
        {
            eRarity = itemTableData.ItemRarity;
        }
        else
        {
            return;
        }

        outline.SetOutlineColor(ItemRarityColors.GetColor(eRarity, OutlineColorAlpha));
    }
}
}
