using System.Collections.Generic;
using DBGameSystem;
using Interaction.Player;
using MieMieFrameWork.Asset;
using MmInventory;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_Budier
{

    /// <summary>
    /// 方块 Y 轴旋转 仅 0° 与 90° 两档
    /// </summary>
    public enum ECubeRotation
    {
        Deg0 = 0,
        Deg90 = 1,
    }


    [RequireComponent(typeof(BuilderVirtualGrid))]
    public partial class BuilderSystem : IBuilderSystem, IBuilder
    {
        [LabelText("游戏模式")]
        public ERayType gameRayType;

        [LabelText("配置文件")]
        public BuilderSystemSetting builderSetting;

        [LabelText("当前活跃方块类型")]
        public CubeData activeCubeData;

        public CubeData ActiveCubeData => activeCubeData;

        /// <summary>
        /// 相机组件
        /// </summary>
        private Camera mainCamera;

        /// <summary>
        /// 建筑方块父节点
        /// </summary>
        private Transform cubeRoot;
        /// <summary>
        /// 预览方块父节点
        /// </summary>
        private Transform preViewRoot;

        /// <summary>
        /// 外部开发者组件
        /// </summary>
        public IBuilderCustom imBuilder;

        /// <summary>
        /// 运行时状态：UI 打开时屏蔽射线检测与放置
        /// 由 UI 在开关时调用 SetUIOpen 维护
        /// </summary>

        /// <summary>
        /// 虚拟网格组件
        /// </summary>
        private BuilderVirtualGrid virtualGrid;

        /// <summary>
        /// 运行时总方块字典
        /// </summary>
        private Dictionary<Vector3Int, CubeInstance> runtimeCubeDataDict = new();

        /// <summary>
        /// 占格校验临时列表
        /// </summary>
        private List<Vector3Int> tempOccupiedGridList;

        /// <summary>
        /// 放置碰撞检测缓存
        /// </summary>
        private Collider[] placementColliderBuffer;

        /// <summary> 旋转枚举值 </summary>
        private ECubeRotation placementRotation;

        /// <summary> 物品表驱动的临时放置数据 不进建造注册表 </summary>
        private CubeData itemPlaceCubeData;

        private static BuilderSystem instance;
        public static BuilderSystem Instance { get => instance; private set => instance = value; }


        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitBuilderSystem();
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;

            if (itemPlaceCubeData != null)
            {
                Destroy(itemPlaceCubeData);
                itemPlaceCubeData = null;
            }

            if (ReferenceEquals(GameHub.Get<IBuilder>(), this))
                GameHub.Unregister<IBuilder>();
        }

        void Start()
        {
            InitBuilderSystem();
            InitPreViewCubeInfo();
        }

        void Update()
        {
            UpdateBuilderSystem();
        }


        /// <summary>
        /// 初始化建造系统
        /// </summary>
        private void InitBuilderSystem()
        {
            // 虚拟网格
            virtualGrid ??= GetComponent<BuilderVirtualGrid>();
            // 相机
            mainCamera ??= Camera.main;

            // Obj父节点
            cubeRoot ??= transform.Find("CubeRoot") ?? new GameObject("CubeRoot").transform;
            preViewRoot ??= transform.Find("PreViewRoot") ?? new GameObject("PreViewRoot").transform;
            cubeRoot.SetParent(this.transform);
            preViewRoot.SetParent(this.transform);

            // 运行时缓存 容量来自配置
            var maxHits = builderSetting.maxRaycastHitCount;
            var maxOccupied = builderSetting.maxOccupiedGridCount;

            // 射线命中缓存
            raycastHitBuffer = new RaycastHit[maxHits];
            // 放置碰撞命中缓存
            placementColliderBuffer = new Collider[Mathf.Max(1, builderSetting.maxPlacementColliderCount)];
            // 最大方块占格位置列表
            tempOccupiedGridList = new List<Vector3Int>(maxOccupied);

            builderSetting.RegisterAllCubeData();
        }

        /// <summary>
        /// 设置外部开发者组件
        /// </summary>
        /// <param name="builderCustom">外部开发者组件</param>
        public void OpenCustomMode(IBuilderCustom builderCustom) => this.imBuilder = builderCustom;


        /// <summary>
        /// 设置活跃方块
        /// </summary>
        /// <param name="cubeData">活跃方块</param>
        public void SetActiveCubeData(CubeData cubeData)
        {
            activeCubeData = cubeData;
            placementRotation = ECubeRotation.Deg0;
        }

        /// <summary>
        /// 按物品表进入放置预览 预制体走 Luban place_prefab_path
        /// </summary>
        public bool TryEnterPlaceFromItem(int itemTableId)
        {
            if (itemTableId <= 0)
                return false;
            if (!LubanTables.TryGetItem(itemTableId, out var tableData)
                || string.IsNullOrEmpty(tableData.PlacePrefabPath))
                return false;

            var prefab = MmAssetMgr.LoadAsset<GameObject>(tableData.PlacePrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("放置失败 加载不到放置预制体 " + tableData.PlacePrefabPath);
                return false;
            }

            if (itemPlaceCubeData == null)
            {
                itemPlaceCubeData = ScriptableObject.CreateInstance<CubeData>();
                itemPlaceCubeData.hideFlags = HideFlags.HideAndDontSave;
                itemPlaceCubeData.name = "ItemPlaceRuntime";
            }

            itemPlaceCubeData.CubePrefab = prefab;
            SetActiveCubeData(itemPlaceCubeData);
            SetStopRayCast(false);
            return true;
        }

        /// <summary>
        /// 更新建造系统
        /// </summary>
        public void UpdateBuilderSystem()
        {
            // 有 3C 输入时由 BuildInteractModule 注入 不再直接读鼠标
            if (GameHub.Get<IPlayerInput>() == null)
                PollInput();

            // 如果射线检测停止 则隐藏预览
            if (stopRayCast) { HidePreView(); return; }

            // 处理射线检测
            if (!HandleRaycast(out var hit)) { HidePreView(); return; }

            // 处理破坏
            if (breakButtonPressed)
            {
                HandleBreak(GetBreakTargetGridPos(hit), null);
                breakButtonPressed = false;
                return;
            }

            // 如果活跃方块为空 则隐藏预览
            if (activeCubeData == null) { HidePreView(); return; }


            // 处理旋转
            if (rotateButtonPressed)
            {
                placementRotation = placementRotation == ECubeRotation.Deg0
                    ? ECubeRotation.Deg90
                    : ECubeRotation.Deg0;
                ClearRotateButtonPressed();
            }

            // 创建放置报告
            BuilderPlacementReport.CreateReport(GetPlaceTargetGridPos(hit),
                                                activeCubeData,
                                                placementRotation,
                                                out var placement);


            // 处理校验
            var canPlace = HandlePlaceValid(placement, activeCubeData);
            HandlePreview(placement, activeCubeData, canPlace);

            // 处理放置
            if (placeButtonPressed)
            {
                if (canPlace)
                    HandlePlace(placement, activeCubeData);

                placeButtonPressed = false;
            }
        }


    }
}
