using System;
using System.Collections.Generic;
using System.Text;
using cfg.item;
using MmInventory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Interaction.Editor
{
    /// <summary>
    /// 物品表面摆放工具
    /// 在选中物体的上表面随机撒布物品表里的世界掉落物
    /// 纯编辑器工具 只在场景里摆预制体实例 不参与运行时逻辑
    /// </summary>
    public sealed class WorldItemSurfacePlacerWindow : EditorWindow
    {
        /// <summary> 窗口标题 </summary>
        private const string WindowTitle = "物品表面摆放";

        /// <summary> 菜单路径 </summary>
        private const string MenuPath = "Tools/DownBreak/" + WindowTitle;

        /// <summary> 配置存 EditorPrefs 的键 </summary>
        private const string SettingsPrefsKey = "DownBreak.WorldItemSurfacePlacer.Settings";

        /// <summary> 分组节点名前缀 </summary>
        private const string GeneratedGroupPrefix = "ItemScatter_";

        /// <summary> 生成物名前缀 </summary>
        private const string GeneratedObjectPrefix = "ItemScatter_";

        /// <summary> 目标物体 </summary>
        [SerializeField]
        private GameObject targetObject;

        /// <summary> 工具配置 </summary>
        [SerializeField]
        private SurfacePlacerSettings settings = new SurfacePlacerSettings();

        /// <summary> 显式指定的内部表面 留空则在目标内部空间自动采样 </summary>
        [SerializeField]
        private List<GameObject> interiorSurfaceRoots = new List<GameObject>();

        /// <summary> 滚动位置 </summary>
        [SerializeField]
        private Vector2 scrollPosition;

        /// <summary> 结果提示 </summary>
        [SerializeField]
        private string resultMessage = string.Empty;

        /// <summary> 结果提示类型 </summary>
        [SerializeField]
        private MessageType resultMessageType = MessageType.None;

        /// <summary> 物品预制体缓存 </summary>
        private readonly Dictionary<int, GameObject> prefabCache = new Dictionary<int, GameObject>();

        /// <summary> 物品表是否已就绪 </summary>
        private bool isTableReady;

        /// <summary> 物品表加载失败信息 </summary>
        private string tableErrorMessage = string.Empty;

        /// <summary> 类型枚举缓存 </summary>
        private static EItemType[] itemTypeValues;

        /// <summary> 稀有度枚举缓存 </summary>
        private static EItemRarity[] itemRarityValues;

        #region 生命周期

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem(MenuPath)]
        public static void Open()
        {
            OpenWithTarget(Selection.activeGameObject);
        }

        /// <summary>
        /// 右键菜单入口 用当前选中物体打开
        /// </summary>
        [MenuItem("GameObject/DownBreak/在表面随机摆放物品", false, 30)]
        private static void OpenFromHierarchy()
        {
            OpenWithTarget(Selection.activeGameObject);
        }

        /// <summary>
        /// 打开并指定目标
        /// </summary>
        private static void OpenWithTarget(GameObject target)
        {
            var window = GetWindow<WorldItemSurfacePlacerWindow>(false, WindowTitle, true);
            window.minSize = new Vector2(440f, 560f);
            if (target != null)
                window.targetObject = target;
            window.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
            if (targetObject == null)
                PickSelection();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        #endregion

        #region 界面

        private void OnGUI()
        {
            EnsureTable();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.HelpBox(
                "在目标物体的上表面随机摆放物品表里的世界掉落物。\n" +
                "采样方式：界面包围盒内随机撒点 → 向下射线 → 只取法线朝上的面。\n" +
                "生成的是预制体实例（保持预制体关联），可整体撤销，也可一键清空。",
                MessageType.None);

            DrawTargetSection();
            DrawModeSection();
            if (settings.placementMode == EPlacementMode.SurfaceSample)
                DrawSurfaceSection();
            else
                DrawAnchorSection();
            DrawSourceSection();
            DrawPlaceSection();
            DrawActionSection();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 摆放方式区
        /// </summary>
        private void DrawModeSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("摆放方式", EditorStyles.boldLabel);
            settings.placementMode = (EPlacementMode)EditorGUILayout.EnumPopup("方式", settings.placementMode);
        }

        /// <summary>
        /// 锚点模式区
        /// </summary>
        private void DrawAnchorSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("放置锚点", EditorStyles.boldLabel);

            if (targetObject == null)
            {
                EditorGUILayout.HelpBox("先指定目标物体", MessageType.Info);
                return;
            }

            var anchorList = ItemPlacementAnchorEditorUtility.CollectAnchors(targetObject);
            EditorGUILayout.LabelField($"目标下激活的锚点：{anchorList.Count} 个");

            if (anchorList.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "没有锚点：用菜单 GameObject/DownBreak/添加物品放置锚点 在目标下添加，\n" +
                    "然后在场景里拖动锚点决定物品摆在哪（局部 +Y 是承托面法线）。",
                    MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("为所有锚点生成承托碰撞体"))
                {
                    int count = ItemPlacementAnchorEditorUtility.EnsureAllSupportColliders(targetObject);
                    SetResult($"已生成/更新 {count} 个承托碰撞体", MessageType.Info);
                }

                if (GUILayout.Button("移除所有承托碰撞体"))
                {
                    for (int i = 0; i < anchorList.Count; i++)
                        ItemPlacementAnchorEditorUtility.RemoveSupportCollider(anchorList[i]);
                    SetResult("已移除承托碰撞体", MessageType.Info);
                }
            }

            if (!settings.autoCreateSupportCollider && settings.runtimeMode == EPlacementRuntimeMode.NormalDrop)
            {
                EditorGUILayout.HelpBox(
                    "运行时行为是「正常世界掉落物」且没补承托碰撞体：进 Play 物品会自由落体甚至穿出台面。\n" +
                    "要么生成承托碰撞体让物品落在锚点面上，要么把运行时行为改成「保持编辑位置」。",
                    MessageType.Warning);
            }
        }

        /// <summary>
        /// 目标物体区
        /// </summary>
        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("目标物体", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                targetObject = (GameObject)EditorGUILayout.ObjectField(
                    targetObject,
                    typeof(GameObject),
                    true);

                if (GUILayout.Button("取当前选中", GUILayout.Width(84f)))
                    PickSelection();
            }

            if (targetObject == null)
            {
                EditorGUILayout.HelpBox("请指定要摆放表面的场景物体（桌子 / 货架 / 冰箱等）", MessageType.Info);
                return;
            }

            // 这几项是射线采样打不到的最常见原因 先拦住并说清楚
            if (!targetObject.scene.IsValid())
            {
                EditorGUILayout.HelpBox(
                    "选中的是 Project 里的资源而不是场景实例，射线检测不到它，请从 Hierarchy 里选目标",
                    MessageType.Error);
                return;
            }

            if (!targetObject.activeInHierarchy)
            {
                EditorGUILayout.HelpBox(
                    "目标或其父节点未激活，未激活的碰撞体不参与射线检测",
                    MessageType.Error);
                return;
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene == targetObject.scene)
            {
                EditorGUILayout.HelpBox(
                    "当前在预制体编辑模式(Prefab Mode)下：射线检测查的是主场景物理场景，采不到预制体里的碰撞体，请先回到场景里操作",
                    MessageType.Error);
                return;
            }

            var colliderList = WorldItemSurfaceSampler.CollectTargetColliders(targetObject, out bool collideTriggers);
            if (colliderList.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    $"「{targetObject.name}」自身与子物体下没有碰撞体，无法采样表面",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(
                $"层 {LayerMask.LayerToName(targetObject.layer)}，碰撞体 {colliderList.Length} 个"
                + (collideTriggers ? "（只有触发器 已按触发器采样）" : string.Empty));

            if (WorldItemSurfaceSampler.TryGetTargetBounds(colliderList, out var bounds))
            {
                EditorGUILayout.LabelField(
                    $"包围盒 顶面 y={bounds.max.y:F2}，水平 {bounds.size.x:F2} x {bounds.size.z:F2}");
            }
        }

        /// <summary>
        /// 表面采样区
        /// </summary>
        private void DrawSurfaceSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("表面采样", EditorStyles.boldLabel);

            settings.sampleMode = (ESurfacePlacerSampleMode)EditorGUILayout.EnumPopup(
                "采样范围", settings.sampleMode);
            settings.surfaceLayerMask = DrawLayerMaskField("表面层", settings.surfaceLayerMask);
            settings.onlyTargetColliders = EditorGUILayout.Toggle("只取目标自身碰撞体", settings.onlyTargetColliders);
            settings.horizontalMargin = EditorGUILayout.FloatField("水平内缩(米)", settings.horizontalMargin);
            settings.minSpacing = EditorGUILayout.FloatField("最小间距(米)", settings.minSpacing);
            settings.maxSlopeAngle = EditorGUILayout.Slider("最大表面倾角", settings.maxSlopeAngle, 0f, 89f);
            settings.rayStartHeight = EditorGUILayout.FloatField("射线上抬(米)", settings.rayStartHeight);
            settings.rayMaxDistance = EditorGUILayout.FloatField("射线额外长度(米)", settings.rayMaxDistance);
            settings.sampleAttemptsPerPoint = EditorGUILayout.IntSlider(
                "单点采样次数", settings.sampleAttemptsPerPoint, 1, 200);
            settings.avoidExistingItems = EditorGUILayout.Toggle("避开已有掉落物", settings.avoidExistingItems);
            settings.avoidClipping = EditorGUILayout.Toggle("避免穿模(净空检查)", settings.avoidClipping);

            if (settings.sampleMode == ESurfacePlacerSampleMode.TopOnly)
                return;

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("内部采样", EditorStyles.boldLabel);
            DrawInteriorSurfaceList();

            settings.interiorTopInset = EditorGUILayout.FloatField("内部上界内缩(米)", settings.interiorTopInset);
            settings.interiorBottomInset = EditorGUILayout.FloatField("内部下界内缩(米)", settings.interiorBottomInset);
            settings.interiorRayLength = EditorGUILayout.FloatField("内部射线长度(米)", settings.interiorRayLength);
            settings.requireOverheadCover = EditorGUILayout.Toggle("要求头顶有遮挡", settings.requireOverheadCover);
            if (settings.requireOverheadCover)
                settings.overheadCheckDistance = EditorGUILayout.FloatField("遮挡检查距离(米)", settings.overheadCheckDistance);
        }

        /// <summary>
        /// 内部表面列表 留空则按目标包围盒内部自动采样
        /// </summary>
        private void DrawInteriorSurfaceList()
        {
            if (interiorSurfaceRoots == null)
                interiorSurfaceRoots = new List<GameObject>();

            EditorGUILayout.LabelField("内部表面（留空=自动在目标内部空间采样）");

            int removeIndex = -1;
            for (int i = 0; i < interiorSurfaceRoots.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    interiorSurfaceRoots[i] = (GameObject)EditorGUILayout.ObjectField(
                        interiorSurfaceRoots[i],
                        typeof(GameObject),
                        true);

                    if (GUILayout.Button("×", GUILayout.Width(22f)))
                        removeIndex = i;
                }
            }

            if (removeIndex >= 0)
                interiorSurfaceRoots.RemoveAt(removeIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加内部表面"))
                    interiorSurfaceRoots.Add(null);

                if (GUILayout.Button("用当前选中"))
                {
                    if (Selection.gameObjects != null)
                    {
                        for (int i = 0; i < Selection.gameObjects.Length; i++)
                        {
                            var selected = Selection.gameObjects[i];
                            if (selected != null && !interiorSurfaceRoots.Contains(selected))
                                interiorSurfaceRoots.Add(selected);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 物品来源区
        /// </summary>
        private void DrawSourceSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("物品来源", EditorStyles.boldLabel);

            settings.sourceMode = (ESurfacePlacerSourceMode)EditorGUILayout.EnumPopup(
                "来源模式", settings.sourceMode);

            if (settings.sourceMode == ESurfacePlacerSourceMode.ItemIdList)
                DrawItemIdListSection();
            else
                DrawRandomFilterSection();

            DrawPoolSummary();
        }

        /// <summary>
        /// ID 列表模式
        /// </summary>
        private void DrawItemIdListSection()
        {
            if (settings.itemEntryList == null)
                settings.itemEntryList = new List<SurfacePlacerItemEntry>();

            int removeIndex = -1;
            for (int i = 0; i < settings.itemEntryList.Count; i++)
            {
                var entry = settings.itemEntryList[i];
                if (entry == null)
                {
                    entry = new SurfacePlacerItemEntry();
                    settings.itemEntryList[i] = entry;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(20f));
                    entry.itemId = EditorGUILayout.IntField(entry.itemId, GUILayout.Width(70f));
                    EditorGUILayout.LabelField(ResolveItemLabel(entry.itemId));

                    EditorGUILayout.LabelField("权重", GUILayout.Width(30f));
                    entry.weight = EditorGUILayout.IntField(entry.weight, GUILayout.Width(38f));
                    EditorGUILayout.LabelField("堆叠", GUILayout.Width(30f));
                    entry.stackCount = EditorGUILayout.IntField(entry.stackCount, GUILayout.Width(38f));

                    if (GUILayout.Button("×", GUILayout.Width(22f)))
                        removeIndex = i;
                }
            }

            if (removeIndex >= 0)
                settings.itemEntryList.RemoveAt(removeIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加条目"))
                    settings.itemEntryList.Add(new SurfacePlacerItemEntry());

                if (GUILayout.Button("清空列表"))
                    settings.itemEntryList.Clear();
            }
        }

        /// <summary>
        /// 类型随机模式
        /// </summary>
        private void DrawRandomFilterSection()
        {
            if (itemTypeValues == null)
                itemTypeValues = (EItemType[])Enum.GetValues(typeof(EItemType));
            if (itemRarityValues == null)
                itemRarityValues = (EItemRarity[])Enum.GetValues(typeof(EItemRarity));

            if (settings.randomTypeList == null)
                settings.randomTypeList = new List<EItemType>();

            EditorGUILayout.LabelField("物品类型（全不勾=不限）");
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < itemTypeValues.Length; i++)
                {
                    var itemType = itemTypeValues[i];
                    bool isOn = settings.randomTypeList.Contains(itemType);
                    bool newIsOn = EditorGUILayout.ToggleLeft(itemType.ToString(), isOn, GUILayout.Width(74f));
                    if (newIsOn == isOn)
                        continue;

                    if (newIsOn)
                        settings.randomTypeList.Add(itemType);
                    else
                        settings.randomTypeList.Remove(itemType);
                }
            }

            settings.filterByRarity = EditorGUILayout.Toggle("按稀有度过滤", settings.filterByRarity);
            if (!settings.filterByRarity)
                return;

            if (settings.randomRarityList == null)
                settings.randomRarityList = new List<EItemRarity>();

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < itemRarityValues.Length; i++)
                {
                    var rarity = itemRarityValues[i];
                    bool isOn = settings.randomRarityList.Contains(rarity);
                    bool newIsOn = EditorGUILayout.ToggleLeft(rarity.ToString(), isOn, GUILayout.Width(74f));
                    if (newIsOn == isOn)
                        continue;

                    if (newIsOn)
                        settings.randomRarityList.Add(rarity);
                    else
                        settings.randomRarityList.Remove(rarity);
                }
            }
        }

        /// <summary>
        /// 候选池统计
        /// </summary>
        private void DrawPoolSummary()
        {
            if (!isTableReady)
            {
                EditorGUILayout.HelpBox($"物品表未就绪：{tableErrorMessage}", MessageType.Error);
                if (GUILayout.Button("重试加载物品表"))
                {
                    tableErrorMessage = string.Empty;
                    EnsureTable();
                }
                return;
            }

            SurfacePlacerItemPool pool;
            try
            {
                pool = SurfacePlacerItemPool.Build(settings);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox($"构建候选池失败：{exception.Message}", MessageType.Error);
                return;
            }

            var builder = new StringBuilder();
            builder.Append($"候选 {pool.Count} 件");
            if (pool.NoPrefabCount > 0)
                builder.Append($"，跳过无 world_prefab_path {pool.NoPrefabCount} 件");
            if (pool.MissingItemCount > 0)
                builder.Append($"，ID 不存在 {pool.MissingItemCount} 件");

            EditorGUILayout.HelpBox(builder.ToString(),
                pool.Count > 0 ? MessageType.Info : MessageType.Warning);
        }

        /// <summary>
        /// 放置区
        /// </summary>
        private void DrawPlaceSection()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("放置", EditorStyles.boldLabel);

            settings.placeCount = EditorGUILayout.IntSlider("目标件数", settings.placeCount, 1, 200);
            settings.groupMode = (ESurfacePlacerGroupMode)EditorGUILayout.EnumPopup(
                "挂载方式", settings.groupMode);
            settings.surfaceOffset = EditorGUILayout.FloatField("离面抬升(米)", settings.surfaceOffset);
            settings.alignMode = (EPlacementAlignMode)EditorGUILayout.EnumPopup("落位对齐", settings.alignMode);
            if (settings.alignMode == EPlacementAlignMode.BottomToSurface)
            {
                EditorGUILayout.HelpBox(
                    "底面贴承托面：物品视觉/物理正确，但 Transform 的 y 会比锚点高出一个「轴心→模型底部」的距离。\n" +
                    "想让 Transform 数值与锚点完全一致 就选「轴心对齐锚点」。",
                    MessageType.None);
            }
            settings.alignToSurfaceNormal = EditorGUILayout.Toggle("贴合表面法线", settings.alignToSurfaceNormal);
            settings.randomYaw = EditorGUILayout.Toggle("随机水平朝向", settings.randomYaw);
            settings.writeSaveDataWhenStacked = EditorGUILayout.Toggle(
                "堆叠>1 写实例快照", settings.writeSaveDataWhenStacked);

            settings.runtimeMode = (EPlacementRuntimeMode)EditorGUILayout.EnumPopup("运行时行为", settings.runtimeMode);
            if (settings.runtimeMode == EPlacementRuntimeMode.KeepPlaced)
            {
                EditorGUILayout.HelpBox(
                    "保持编辑位置：进 Play 后物品不随机翻姿态、不下落、刚体为运动学，不会被碰撞顶飞。\n" +
                    "仍是正常可拾取物品（对准按交互键即可拾取）。",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "正常世界掉落物：进 Play 会随机姿态自由下落，必须下方有实体碰撞体托住，\n" +
                    "否则会穿过台面/被去穿透顶出去。",
                    MessageType.Warning);
            }

            if (settings.placementMode == EPlacementMode.PlacementAnchor)
            {
                settings.autoCreateSupportCollider = EditorGUILayout.Toggle(
                    "生成前补承托碰撞体", settings.autoCreateSupportCollider);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                settings.randomSeed = EditorGUILayout.IntField("随机种子(0=每次不同)", settings.randomSeed);
                if (GUILayout.Button("随机", GUILayout.Width(50f)))
                    settings.randomSeed = UnityEngine.Random.Range(1, int.MaxValue);
            }
        }

        /// <summary>
        /// 操作区
        /// </summary>
        private void DrawActionSection()
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("干跑统计", GUILayout.Height(28f)))
                    RunPlacer(true);

                using (new EditorGUI.DisabledScope(targetObject == null))
                {
                    if (GUILayout.Button("生成", GUILayout.Height(28f)))
                        RunPlacer(false);
                }

                using (new EditorGUI.DisabledScope(targetObject == null))
                {
                    if (GUILayout.Button("清空生成物", GUILayout.Height(28f)))
                        ClearGenerated();
                }
            }

            if (string.IsNullOrEmpty(resultMessage))
                return;

            EditorGUILayout.Space(2f);
            EditorGUILayout.HelpBox(resultMessage, resultMessageType);
        }

        #endregion

        #region 执行

        /// <summary>
        /// 采样并摆放 dryRun 只统计不生成
        /// </summary>
        private void RunPlacer(bool dryRun)
        {
            resultMessage = string.Empty;
            resultMessageType = MessageType.None;

            if (targetObject == null)
            {
                SetResult("请先指定目标物体", MessageType.Warning);
                return;
            }

            if (!targetObject.scene.IsValid())
            {
                SetResult("目标不是场景实例（选的是 Project 里的资源）射线检测不到，请从 Hierarchy 里选目标", MessageType.Error);
                return;
            }

            if (!targetObject.activeInHierarchy)
            {
                SetResult("目标或其父节点未激活，未激活的碰撞体不参与射线检测", MessageType.Error);
                return;
            }

            var openedPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (openedPrefabStage != null && openedPrefabStage.scene == targetObject.scene)
            {
                SetResult("当前在预制体编辑模式(Prefab Mode)下，射线检测采不到预制体里的碰撞体，请回到场景里操作", MessageType.Error);
                return;
            }

            EnsureTable();
            if (!isTableReady)
            {
                SetResult($"物品表未就绪：{tableErrorMessage}", MessageType.Error);
                return;
            }

            // 锚点模式不需要目标表面 直接走锚点流程
            if (settings.placementMode == EPlacementMode.PlacementAnchor)
            {
                RunAnchorPlacer(dryRun);
                return;
            }

            var colliderList = WorldItemSurfaceSampler.CollectTargetColliders(targetObject, out bool collideTriggers);
            if (colliderList.Length == 0)
            {
                SetResult($"「{targetObject.name}」自身与子物体下没有碰撞体，无法采样表面", MessageType.Warning);
                return;
            }

            if (!WorldItemSurfaceSampler.TryGetTargetBounds(colliderList, out var bounds))
            {
                SetResult("无法计算目标包围盒", MessageType.Warning);
                return;
            }

            SurfacePlacerItemPool pool;
            try
            {
                pool = SurfacePlacerItemPool.Build(settings);
            }
            catch (Exception exception)
            {
                SetResult($"构建候选池失败：{exception.Message}", MessageType.Error);
                return;
            }

            if (pool.Count == 0)
            {
                SetResult("候选物品为空，请检查物品 ID / 类型过滤，或该物品没有 world_prefab_path", MessageType.Warning);
                return;
            }

            int placeTargetCount = Mathf.Max(1, settings.placeCount);
            var random = settings.randomSeed == 0
                ? new System.Random()
                : new System.Random(settings.randomSeed);
            var usedPointList = new List<Vector3>();

            Transform generatedRoot = null;
            if (!dryRun)
                generatedRoot = ResolveGeneratedRoot();

            int indexOffset = generatedRoot != null ? generatedRoot.childCount : 0;
            int placedCount = 0;
            int noSurfaceCount = 0;
            int prefabFailCount = 0;
            var failMessageList = new List<string>();
            var sampleReport = new WorldItemSurfaceSampler.SurfaceSampleReport();

            // 采样范围 顶面 / 内部 / 两者
            var interiorColliderList = CollectInteriorColliders(out bool interiorCollideTriggers);
            var interiorColliderArray = interiorColliderList.Count > 0
                ? interiorColliderList.ToArray()
                : System.Array.Empty<Collider>();
            bool queryCollideTriggers = collideTriggers || interiorCollideTriggers;
            bool wantTop = settings.sampleMode != ESurfacePlacerSampleMode.InteriorOnly;
            bool wantInterior = settings.sampleMode != ESurfacePlacerSampleMode.TopOnly;

            int undoGroup = -1;
            if (!dryRun)
            {
                Undo.SetCurrentGroupName("表面摆放物品");
                undoGroup = Undo.GetCurrentGroup();
            }

            for (int i = 0; i < placeTargetCount; i++)
            {
                if (!pool.TryPick(random, out var item, out int stackCount))
                    break;

                if (!WorldItemSurfaceSampler.TryResolveItemPrefab(
                        item, prefabCache, out var prefab, out string error))
                {
                    prefabFailCount++;
                    if (failMessageList.Count < 5 && !failMessageList.Contains(error))
                        failMessageList.Add(error);
                    continue;
                }

                WorldItemSurfaceSampler.ResolvePlaceFootprint(prefab, out float radius, out float itemHeight);

                // 顶面与内部都采时 每件随机决定先试哪一种 避免全挤在同一个面上
                bool tryInteriorFirst = wantInterior && (!wantTop || random.Next(2) == 0);
                var sampledHit = new WorldItemSurfaceSampler.SurfaceHit();
                bool hasSample = false;
                for (int attempt = 0; attempt < 2 && !hasSample; attempt++)
                {
                    bool useInterior = attempt == 0 ? tryInteriorFirst : !tryInteriorFirst;
                    if (useInterior && !wantInterior)
                        continue;
                    if (!useInterior && !wantTop)
                        continue;

                    hasSample = useInterior
                        ? WorldItemSurfaceSampler.TrySampleInteriorSurface(
                            colliderList, interiorColliderArray, settings, usedPointList,
                            random, radius, itemHeight, queryCollideTriggers, ref sampleReport, out sampledHit)
                        : WorldItemSurfaceSampler.TrySampleSurface(
                            colliderList, bounds, settings, usedPointList,
                            random, radius, itemHeight, queryCollideTriggers, ref sampleReport, out sampledHit);
                }

                if (!hasSample)
                {
                    noSurfaceCount++;
                    continue;
                }

                var hit = sampledHit;
                usedPointList.Add(hit.Position);

                if (dryRun)
                {
                    placedCount++;
                    continue;
                }

                string objectName =
                    $"{GeneratedObjectPrefix}{item.ExcelItemId}_{item.Name}_{indexOffset + placedCount}";
                var instance = WorldItemSurfaceSampler.InstantiatePlacedItem(
                    prefab, item, hit, stackCount, generatedRoot, settings, random, objectName,
                    settings.runtimeMode == EPlacementRuntimeMode.KeepPlaced);
                if (instance == null)
                {
                    prefabFailCount++;
                    continue;
                }

                placedCount++;
            }

            if (!dryRun && undoGroup >= 0)
                Undo.CollapseUndoOperations(undoGroup);

            if (!dryRun)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                SceneView.RepaintAll();
            }

            SetResult(BuildRunMessage(
                dryRun, pool, placeTargetCount, placedCount,
                noSurfaceCount, prefabFailCount, failMessageList, sampleReport), dryRun || placedCount > 0
                ? MessageType.Info
                : MessageType.Warning);
        }

        /// <summary>
        /// 按放置锚点摆放 锚点由美术在场景里手工摆好
        /// </summary>
        private void RunAnchorPlacer(bool dryRun)
        {
            var anchorList = ItemPlacementAnchorEditorUtility.CollectAnchors(targetObject);
            if (anchorList.Count == 0)
            {
                SetResult("目标下没有放置锚点：用 GameObject/DownBreak/添加物品放置锚点 添加，或把摆放方式改回表面采样", MessageType.Warning);
                return;
            }

            SurfacePlacerItemPool pool;
            try
            {
                pool = SurfacePlacerItemPool.Build(settings);
            }
            catch (Exception exception)
            {
                SetResult($"构建候选池失败：{exception.Message}", MessageType.Error);
                return;
            }

            if (pool.Count == 0)
            {
                SetResult("候选物品为空，请检查物品 ID / 类型过滤，或该物品没有 world_prefab_path", MessageType.Warning);
                return;
            }

            // 需要物理承托时 先给锚点补承托碰撞体
            int supportColliderCount = 0;
            if (!dryRun && settings.autoCreateSupportCollider)
                supportColliderCount = ItemPlacementAnchorEditorUtility.EnsureAllSupportColliders(targetObject);

            var random = settings.randomSeed == 0
                ? new System.Random()
                : new System.Random(settings.randomSeed);

            Transform generatedRoot = null;
            if (!dryRun)
                generatedRoot = ResolveGeneratedRoot();

            int indexOffset = generatedRoot != null ? generatedRoot.childCount : 0;
            int placedCount = 0;
            int noPointCount = 0;
            int prefabFailCount = 0;
            int clearanceBlockedCount = 0;
            float maxAnchorDeviation = 0f;
            var failMessageList = new List<string>();

            // 目标自身碰撞体在净空检查里不算遮挡 否则冰箱外壳会把整条内部空间否掉
            var anchorTargetColliders = WorldItemSurfaceSampler.CollectTargetColliders(targetObject, out _);
            bool hasAreaAnchor = false;

            int undoGroup = -1;
            if (!dryRun)
            {
                Undo.SetCurrentGroupName("按锚点摆放物品");
                undoGroup = Undo.GetCurrentGroup();
            }

            for (int a = 0; a < anchorList.Count; a++)
            {
                var anchor = anchorList[a];
                int anchorPlaceCount = anchor.ResolvePlaceCount(settings.placeCount);
                if (anchorPlaceCount <= 0)
                    continue;

                var usedPointList = new List<Vector3>();
                var anchorIgnoreColliders = BuildAnchorIgnoreColliders(anchor, anchorTargetColliders);
                Vector3 anchorUp = anchor.Up.sqrMagnitude > 0.0001f ? anchor.Up.normalized : Vector3.up;
                int attemptCount = Mathf.Max(1, settings.sampleAttemptsPerPoint);
                bool isSinglePointAnchor = anchor.SpreadMode == EPlacementAnchorSpread.AnchorPoint;
                if (!isSinglePointAnchor)
                    hasAreaAnchor = true;

                for (int i = 0; i < anchorPlaceCount; i++)
                {
                    if (!pool.TryPick(random, out var item, out int stackCount))
                        break;

                    if (!WorldItemSurfaceSampler.TryResolveItemPrefab(
                            item, prefabCache, out var prefab, out string error))
                    {
                        prefabFailCount++;
                        if (failMessageList.Count < 5 && !failMessageList.Contains(error))
                            failMessageList.Add(error);
                        continue;
                    }

                    WorldItemSurfaceSampler.ResolvePlaceFootprint(prefab, out float radius, out float itemHeight);
                    float spacing = Mathf.Max(settings.minSpacing, radius * 2f);

                    // 锚点范围内找一个不与已摆点打架、且净空足够的位置
                    // 单点锚点只落一件(或按配置件数同位) 不做件间间距检查
                    bool hasPoint = false;
                    Vector3 placePoint = default;
                    for (int attempt = 0; attempt < attemptCount; attempt++)
                    {
                        var candidate = anchor.SamplePoint(random);
                        if (!isSinglePointAnchor
                            && !WorldItemSurfaceSampler.HasEnoughSpacing(usedPointList, candidate, spacing))
                        {
                            continue;
                        }

                        if (settings.avoidClipping
                            && !WorldItemSurfaceSampler.HasPlacementClearance(
                                candidate, anchorUp, radius, itemHeight, anchorIgnoreColliders))
                        {
                            clearanceBlockedCount++;
                            continue;
                        }

                        placePoint = candidate;
                        hasPoint = true;
                        break;
                    }

                    if (!hasPoint)
                    {
                        noPointCount++;
                        continue;
                    }

                    usedPointList.Add(placePoint);

                    // 记录与锚点原点的水平偏差 单点模式应≈0
                    float deviation = Vector3.ProjectOnPlane(placePoint - anchor.Center, anchorUp).magnitude;
                    if (deviation > maxAnchorDeviation)
                        maxAnchorDeviation = deviation;

                    if (dryRun)
                    {
                        placedCount++;
                        continue;
                    }

                    var anchorHit = new WorldItemSurfaceSampler.SurfaceHit
                    {
                        Position = placePoint,
                        Normal = anchorUp,
                        Surface = null,
                    };
                    string objectName =
                        $"{GeneratedObjectPrefix}{item.ExcelItemId}_{item.Name}_{indexOffset + placedCount}";
                    var instance = WorldItemSurfaceSampler.InstantiatePlacedItem(
                        prefab, item, anchorHit, stackCount, generatedRoot, settings, random, objectName,
                        settings.runtimeMode == EPlacementRuntimeMode.KeepPlaced, anchor.ResolveRotation(random), anchor.SurfaceOffset);
                    if (instance == null)
                    {
                        prefabFailCount++;
                        continue;
                    }

                    placedCount++;
                }
            }

            if (!dryRun && undoGroup >= 0)
                Undo.CollapseUndoOperations(undoGroup);

            if (!dryRun)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                SceneView.RepaintAll();
            }

            var builder = new StringBuilder();
            builder.Append(dryRun ? "干跑：" : "生成：");
            builder.Append($"锚点 {anchorList.Count} 个，候选 {pool.Count} 件，");
            builder.Append(dryRun ? $"可摆放 {placedCount} 件" : $"已摆放 {placedCount} 件");

            if (noPointCount > 0)
                builder.Append($"，锚点内无空位 {noPointCount} 次");
            if (clearanceBlockedCount > 0)
                builder.Append($"，净空不足(会穿模) {clearanceBlockedCount} 次");
            if (prefabFailCount > 0)
                builder.Append($"，预制体解析失败 {prefabFailCount} 次");
            if (supportColliderCount > 0)
                builder.Append($"，补承托碰撞体 {supportColliderCount} 个");
            if (pool.NoPrefabCount > 0)
                builder.Append($"，跳过无掉落预制体 {pool.NoPrefabCount} 件");

            // 对位自检：单点锚点的水平偏差应≈0 便于确认是不是锚点散布导致的错位
            builder.AppendLine();
            builder.Append(
                $"对位：散布={settings.placementMode} 对齐={settings.alignMode} "
                + $"锚点最大水平偏差 {maxAnchorDeviation:F3} 米");
            if (hasAreaAnchor)
            {
                builder.Append("（有范围锚点在范围内随机撒点 偏差属预期）");
            }
            else if (maxAnchorDeviation > 0.001f)
            {
                builder.Append("（单点锚点偏差应≈0 若明显偏大请把情况告诉我）");
            }

            if (settings.runtimeMode == EPlacementRuntimeMode.NormalDrop && !settings.autoCreateSupportCollider)
            {
                builder.AppendLine();
                builder.Append("提示：既没冻结也没补承托碰撞体，运行中被摆物品会掉下去。");
            }

            if (failMessageList.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("失败明细：");
                for (int i = 0; i < failMessageList.Count; i++)
                    builder.AppendLine("· " + failMessageList[i]);
            }

            SetResult(builder.ToString(),
                dryRun || placedCount > 0 ? MessageType.Info : MessageType.Warning);
        }

        /// <summary>
        /// 组织执行结果文案
        /// </summary>
        private static string BuildRunMessage(
            bool dryRun,
            SurfacePlacerItemPool pool,
            int placeTargetCount,
            int placedCount,
            int noSurfaceCount,
            int prefabFailCount,
            List<string> failMessageList,
            WorldItemSurfaceSampler.SurfaceSampleReport sampleReport)
        {
            var builder = new StringBuilder();
            builder.Append(dryRun ? "干跑：" : "生成：");
            builder.Append($"候选 {pool.Count} 件，目标 {placeTargetCount} 件，");
            builder.Append(dryRun ? $"可摆放 {placedCount} 件" : $"已摆放 {placedCount} 件");

            if (noSurfaceCount > 0)
                builder.Append($"，无可用表面 {noSurfaceCount} 次");
            if (prefabFailCount > 0)
                builder.Append($"，预制体解析失败 {prefabFailCount} 次");
            if (pool.NoPrefabCount > 0)
                builder.Append($"，跳过无掉落预制体 {pool.NoPrefabCount} 件");
            if (pool.MissingItemCount > 0)
                builder.Append($"，ID 不存在 {pool.MissingItemCount} 件");

            // 摆放不成功时把采样失败原因摊开 方便定位是掩码 层 激活 还是表面朝向问题
            if (noSurfaceCount > 0)
            {
                builder.AppendLine();
                builder.Append($"采样 {sampleReport.AttemptCount} 次射线，失败原因：");
                builder.Append(sampleReport.BuildReasonText());
            }

            if (failMessageList != null && failMessageList.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("失败明细：");
                for (int i = 0; i < failMessageList.Count; i++)
                    builder.AppendLine("· " + failMessageList[i]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 清除本工具生成物
        /// </summary>
        private void ClearGenerated()
        {
            resultMessage = string.Empty;
            resultMessageType = MessageType.None;

            if (targetObject == null)
            {
                SetResult("请先指定目标物体", MessageType.Warning);
                return;
            }

            var deleteList = new List<GameObject>();
            if (settings.groupMode == ESurfacePlacerGroupMode.NoGroupUnderTarget)
            {
                for (int i = targetObject.transform.childCount - 1; i >= 0; i--)
                {
                    var child = targetObject.transform.GetChild(i);
                    if (child.name.StartsWith(GeneratedObjectPrefix, StringComparison.Ordinal))
                        deleteList.Add(child.gameObject);
                }
            }
            else
            {
                Transform parent = settings.groupMode == ESurfacePlacerGroupMode.GroupUnderSceneRoot
                    ? null
                    : targetObject.transform;
                var group = FindDirectChild(parent, GeneratedGroupPrefix + targetObject.name);
                if (group != null)
                    deleteList.Add(group.gameObject);
            }

            for (int i = 0; i < deleteList.Count; i++)
                Undo.DestroyObjectImmediate(deleteList[i]);

            if (deleteList.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                SceneView.RepaintAll();
                SetResult($"已清除 {deleteList.Count} 个生成节点", MessageType.Info);
                return;
            }

            SetResult("没有找到本工具生成的节点", MessageType.Info);
        }

        /// <summary>
        /// 取或新建生成分组节点
        /// </summary>
        private Transform ResolveGeneratedRoot()
        {
            if (settings.groupMode == ESurfacePlacerGroupMode.NoGroupUnderTarget)
                return targetObject.transform;

            Transform parent = settings.groupMode == ESurfacePlacerGroupMode.GroupUnderSceneRoot
                ? null
                : targetObject.transform;
            string groupName = GeneratedGroupPrefix + targetObject.name;

            var existingGroup = FindDirectChild(parent, groupName);
            if (existingGroup != null)
                return existingGroup;

            var groupObject = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(groupObject, "创建物品分组");
            groupObject.transform.SetParent(parent, true);
            return groupObject.transform;
        }

        /// <summary>
        /// 找直接子节点 父为空则在场景根里找
        /// </summary>
        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
            {
                var rootArray = SceneManager.GetActiveScene().GetRootGameObjects();
                for (int i = 0; i < rootArray.Length; i++)
                {
                    if (rootArray[i] != null && rootArray[i].name == name)
                        return rootArray[i].transform;
                }

                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == name)
                    return child;
            }

            return null;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 层掩码字段
        /// Unity 6 没有公开的 EditorGUILayout.LayerMaskField 这里用层名拼接换算
        /// </summary>
        private static int DrawLayerMaskField(string label, int layerMask)
        {
            string[] layerNames = InternalEditorUtility.layers;
            int concatenatedMask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask);
            int newConcatenatedMask = EditorGUILayout.MaskField(label, concatenatedMask, layerNames);
            if (newConcatenatedMask == concatenatedMask)
                return layerMask;

            return InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newConcatenatedMask);
        }

        /// <summary>
        /// 锚点净空检查里不算遮挡的碰撞体 锚点承托体 + 目标自身
        /// </summary>
        private static Collider[] BuildAnchorIgnoreColliders(
            ItemPlacementAnchor anchor,
            Collider[] targetColliderList)
        {
            var resultList = new List<Collider>();
            if (anchor != null)
            {
                var supportTransform = anchor.transform.Find(ItemPlacementAnchor.SupportColliderName);
                var supportCollider = supportTransform != null ? supportTransform.GetComponent<Collider>() : null;
                if (supportCollider != null)
                    resultList.Add(supportCollider);
            }

            if (targetColliderList != null)
            {
                for (int i = 0; i < targetColliderList.Length; i++)
                {
                    if (targetColliderList[i] != null)
                        resultList.Add(targetColliderList[i]);
                }
            }

            return resultList.ToArray();
        }

        /// <summary>
        /// 收集显式指定的内部表面碰撞体 列表为空表示按目标内部空间自动采样
        /// </summary>
        private List<Collider> CollectInteriorColliders(out bool collideTriggers)
        {
            collideTriggers = false;
            var resultList = new List<Collider>();
            if (interiorSurfaceRoots == null)
                return resultList;

            for (int i = 0; i < interiorSurfaceRoots.Count; i++)
            {
                var root = interiorSurfaceRoots[i];
                if (root == null)
                    continue;

                var colliderList = WorldItemSurfaceSampler.CollectTargetColliders(root, out bool rootCollideTriggers);
                collideTriggers |= rootCollideTriggers;
                for (int j = 0; j < colliderList.Length; j++)
                    resultList.Add(colliderList[j]);
            }

            return resultList;
        }

        /// <summary>
        /// 取当前选中物体为目标
        /// </summary>
        private void PickSelection()
        {
            if (Selection.activeGameObject == null)
                return;

            targetObject = Selection.activeGameObject;
        }

        /// <summary>
        /// 写好结果提示
        /// </summary>
        private void SetResult(string message, MessageType messageType)
        {
            resultMessage = message;
            resultMessageType = messageType;
            Repaint();
        }

        /// <summary>
        /// 确保物品表已加载
        /// </summary>
        private void EnsureTable()
        {
            if (isTableReady || !string.IsNullOrEmpty(tableErrorMessage))
                return;

            try
            {
                LubanTables.EnsureLoaded();
                isTableReady = true;
            }
            catch (Exception exception)
            {
                tableErrorMessage = exception.Message;
            }
        }

        /// <summary>
        /// 物品 ID 名称回显
        /// </summary>
        private string ResolveItemLabel(int itemId)
        {
            if (!isTableReady)
                return string.Empty;

            if (!LubanTables.TryGetItem(itemId, out var item))
                return "ID 不存在";

            string prefabState = string.IsNullOrWhiteSpace(item.WorldPrefabPath)
                ? "（无 world_prefab_path）"
                : string.Empty;
            return $"{item.Name} [{item.ItemType}]{prefabState}";
        }

        /// <summary>
        /// 读取持久化配置
        /// </summary>
        private void LoadSettings()
        {
            if (!EditorPrefs.HasKey(SettingsPrefsKey))
                return;

            string json = EditorPrefs.GetString(SettingsPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                var loaded = JsonUtility.FromJson<SurfacePlacerSettings>(json);
                if (loaded != null)
                    settings = loaded;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{WindowTitle}] 配置读取失败 使用默认值：{exception.Message}");
            }
        }

        /// <summary>
        /// 保存持久化配置
        /// </summary>
        private void SaveSettings()
        {
            if (settings == null)
                return;

            try
            {
                EditorPrefs.SetString(SettingsPrefsKey, JsonUtility.ToJson(settings));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{WindowTitle}] 配置保存失败：{exception.Message}");
            }
        }

        #endregion
    }
}
