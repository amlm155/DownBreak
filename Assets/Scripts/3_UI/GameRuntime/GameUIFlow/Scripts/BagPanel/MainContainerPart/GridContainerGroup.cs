using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MmInventory;
using MieMieFrameWork.Asset;
using MieMieFrameTools.Archive;
using cfg.item;
namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 容器组 相当于给GridContainerView套了一层壳 
    /// 其负责容器格子 信息 自适应
    /// </summary>
    
    public class GridContainerGroup : MonoBehaviour
    {
        /// <summary> 背包容器格子边长 </summary>
        private const int gridCellSize = 50;
    
        /// <summary>容器类型 </summary>
        [SerializeField]
        public EEquipSlot EquipSlot;
    
        [SerializeField]
        private Image Icon;
        [SerializeField]
        private TextMeshProUGUI Name;
        [SerializeField]
        private GridContainerView gridContainerView;
    
        /// <summary> 是否已初始化组件引用 </summary>
        private bool isComponentsInitialized;
    
        /// <summary> 网格视图 </summary>
        public GridContainerView GridView
        {
            get => gridContainerView;
        }
    
        // 视图高度 
        [SerializeField]
        private RectTransform InfoPosRectTransform;
        [SerializeField]
        private RectTransform gridContainerRectTransform;
    
        /// <summary> 编辑器测试用装备表 ID </summary>
        [SerializeField]
        [LabelText("测试装备ID")]
        private int editorEquipmentId;
    
        /// <summary> 编辑器测试用搜刮容器表 ID </summary>
        [SerializeField]
        [LabelText("测试搜刮容器ID")]
        private int editorScrapContainerId = 7001;
    
        /// <summary> 当前异步加载的图标路径 </summary>
        private string loadingIconPath;
    
        /// <summary> 当前穿戴的运行时物品 </summary>
        public ItemRtData EquippedItemData { get; private set; }
    
        /// <summary> 当前穿戴的装备表数据 </summary>
        public Equipment EquippedEquipment { get; private set; }
    
        /// <summary> 当前容器格数 </summary>
        public int CapacityCellCount
        {
            get
            {
                if (EquippedEquipment == null)
                    return 0;
                return EquippedEquipment.Capacity.X * EquippedEquipment.Capacity.Y;
            }
        }
    
        private void Awake()
        {
            InitComponents();
        }
    
        /// <summary>
        /// 初始化子节点引用
        /// </summary>
        private void InitComponents()
        {
            if (isComponentsInitialized)
                return;
    
            if (gridContainerView == null || gridContainerRectTransform == null)
            {
                // 搜刮根用 NatureMask 装备根用 Origin 都兼容
                var gridTf =  transform.Find("GridContainerOrigin");
                if (gridTf == null)
                {
                    gridContainerView = GetComponentInChildren<GridContainerView>(true);
                    if (gridContainerView != null)
                        gridTf = gridContainerView.transform;
                }
    
                if (gridContainerView == null && gridTf != null)
                    gridContainerView = gridTf.GetComponent<GridContainerView>();
    
                if (gridContainerRectTransform == null && gridTf != null)
                    gridContainerRectTransform = gridTf as RectTransform;
            }
    
            if (InfoPosRectTransform == null)
                InfoPosRectTransform = transform.Find("InfoPos") as RectTransform;
    
            if (Icon == null)
            {
                var iconTf = transform.Find("InfoPos/Icon") ?? transform.Find("Icon");
                if (iconTf != null)
                    Icon = iconTf.GetComponent<Image>();
            }
    
            if (Name == null)
            {
                var nameTf = transform.Find("InfoPos/Name") ?? transform.Find("Name");
                if (nameTf != null)
                    Name = nameTf.GetComponent<TextMeshProUGUI>();
            }
    
            isComponentsInitialized = true;
        }
    
        /// <summary>
        /// 改变容器信息
        /// </summary>
        private void ChangeUIInfo(cfg.item.Equipment equipment)
        {
            if (Icon != null)
                LoadIconAsync(equipment.IconPath).Forget();
            if (Name != null)
            {
                Name.text = equipment.Name + $" {equipment.Capacity.X}x{equipment.Capacity.Y}";
                Color rarityColor = ItemRarityColors.GetRgb(equipment.ItemRarity);
                MieMieFrameTools.Archive.ColorTools.TmpToColor(Name, rarityColor, 1);
            }
        }
    
        /// <summary>
        /// 异步加载并应用容器图标
        /// </summary>
        private async UniTask LoadIconAsync(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
                return;
    
            loadingIconPath = iconPath;
            Sprite iconSprite = await MmAssetMgr.LoadAssetAsync<Sprite>(iconPath);
            if (this == null || loadingIconPath != iconPath || Icon == null)
                return;
    
            Icon.sprite = iconSprite;
        }
    
        #region 自适应

        /// <summary>
        /// Inspector 按测试装备 ID 适配
        /// </summary>
        [Button("自适应普通容器")]
        private void AdaptEquipmentByEditorId()
        {
            AdaptContainerGroupByEquipmentId(editorEquipmentId);
        }
    
        /// <summary>
        /// Inspector 按测试搜刮 ID 适配
        /// </summary>
        [Button("自适应搜刮容器")]
        private void AdaptScrapByEditorId()
        {
            AdaptContainerGroup(editorScrapContainerId);
        }
    
        /// <summary>
        /// 按装备表 ID 自适应容器
        /// </summary>
        public void AdaptContainerGroupByEquipmentId(int equipmentId)
        {
            LubanTables.EnsureLoaded();
            var equipment = LubanTables.Tables.TbEquipment.GetOrDefault(equipmentId);
            if (equipment == null)
            {
                Debug.LogWarning($"TbEquipment 无 id={equipmentId}", this);
                return;
            }
    
            AdaptContainerGroup(equipment);
        }
    
        /// <summary>
        /// 按搜刮容器表 ID 自适应
        /// </summary>
        public void AdaptContainerGroup(int scrapContainerId)
        {
            LubanTables.EnsureLoaded();
            var scrapContainer = LubanTables.Tables.TbScrapContainer.GetOrDefault(scrapContainerId);
            if (scrapContainer == null)
            {
                Debug.LogWarning($"TbScrapContainer 无 id={scrapContainerId}", this);
                return;
            }
    
            AdaptContainerGroup(scrapContainer);
        }
    
        /// <summary>
        /// 按装备容量自适应容器
        /// </summary>
        public void AdaptContainerGroup(cfg.item.Equipment equipment)
        {
            if (gridContainerView == null || InfoPosRectTransform == null || gridContainerRectTransform == null)
            {
                Debug.LogWarning($"[{name}] Adapt 失败 容器引用未就绪", this);
                return;
            }
    
            // 按 Capacity 与格子边长重建
            gridContainerView.RebuildFromCapacity(
                new Vector2Int(equipment.Capacity.X, equipment.Capacity.Y),
                gridCellSize);
    
            // 自身高度 = 信息高度 + 容器视窗高度
            var rectTransform = transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x,
                InfoPosRectTransform.sizeDelta.y + gridContainerRectTransform.sizeDelta.y + 10);
    
            ChangeUIInfo(equipment);
        }
    
        /// <summary>
        /// 穿上装备并刷新栏位表现
        /// </summary>
        public void WearEquipment(ItemRtData itemRtData, Equipment equipment, bool rebuildCapacity = true)
        {
            EquippedItemData = itemRtData;
            EquippedEquipment = equipment;
            if (rebuildCapacity)
                AdaptContainerGroup(equipment);
            else
                ChangeUIInfo(equipment);
        }
    
        /// <summary>
        /// 脱下装备记录 不改格子
        /// </summary>
        public void ClearEquippedRecord()
        {
            EquippedItemData = null;
            EquippedEquipment = null;
        }
    
        /// <summary>
        /// 自适应搜刮容器模板
        /// </summary>
        public void AdaptContainerGroup(cfg.loot.ScrapContainer scrapContainer)
        {
            if (gridContainerView == null || InfoPosRectTransform == null || gridContainerRectTransform == null)
            {
                Debug.LogWarning($"[{name}] Adapt 失败 容器引用未就绪", this);
                return;
            }
    
            gridContainerView.RebuildFromCapacity(
                new Vector2Int(scrapContainer.Capacity.X, scrapContainer.Capacity.Y),
                gridCellSize);
    
            var rectTransform = transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x,
                InfoPosRectTransform.sizeDelta.y + gridContainerRectTransform.sizeDelta.y + 10);
    
            ChangeUIInfo(scrapContainer);
        }
    
        /// <summary>
        /// 改变搜刮容器信息
        /// </summary>
        private void ChangeUIInfo(cfg.loot.ScrapContainer scrapContainer)
        {
            if (Icon != null && !string.IsNullOrEmpty(scrapContainer.IconPath))
                LoadIconAsync(scrapContainer.IconPath).Forget();
            if (Name != null)
                Name.text = scrapContainer.Name + $" {scrapContainer.Capacity.X}x{scrapContainer.Capacity.Y}";
        }

        /// <summary>
        /// 按储物箱表 ID 自适应
        /// </summary>
        public void AdaptStorageBoxByItemId(int storageBoxItemId)
        {
            LubanTables.EnsureLoaded();
            var storageBox = LubanTables.Tables.TbStorageBox.GetOrDefault(storageBoxItemId);
            if (storageBox == null)
            {
                Debug.LogWarning($"TbStorageBox 无 id={storageBoxItemId}", this);
                return;
            }

            AdaptContainerGroup(storageBox);
        }

        /// <summary>
        /// 按储物箱容量自适应容器
        /// </summary>
        public void AdaptContainerGroup(StorageBox storageBox)
        {
            if (gridContainerView == null || InfoPosRectTransform == null || gridContainerRectTransform == null)
            {
                Debug.LogWarning($"[{name}] Adapt 失败 容器引用未就绪", this);
                return;
            }

            gridContainerView.RebuildFromCapacity(
                new Vector2Int(storageBox.Capacity.X, storageBox.Capacity.Y),
                gridCellSize);

            var rectTransform = transform as RectTransform;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x,
                InfoPosRectTransform.sizeDelta.y + gridContainerRectTransform.sizeDelta.y + 10);

            ChangeUIInfo(storageBox);
        }

        /// <summary>
        /// 改变储物箱信息
        /// </summary>
        private void ChangeUIInfo(StorageBox storageBox)
        {
            if (Icon != null && !string.IsNullOrEmpty(storageBox.IconPath))
                LoadIconAsync(storageBox.IconPath).Forget();
            if (Name != null)
                Name.text = storageBox.Name + $" {storageBox.Capacity.X}x{storageBox.Capacity.Y}";
        }

        #endregion
    }
    
}