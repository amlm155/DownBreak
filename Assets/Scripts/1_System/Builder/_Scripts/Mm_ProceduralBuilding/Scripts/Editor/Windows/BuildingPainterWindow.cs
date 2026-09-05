#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mm_ProceduralBuilding.Editor
{
    public class BuildingPainterWindow : EditorWindow
    {
        private enum EBrushMode
        {
            Building,
            Interior
        }

        /// <summary> 模块根路径缓存 </summary>
        private static string cachedModuleRootPath;

        private static string ModuleRootPath
        {
            get
            {
                if (!string.IsNullOrEmpty(cachedModuleRootPath))
                    return cachedModuleRootPath;

                string scriptPath = FindThisScriptPath();
                string windowsFolder = Path.GetDirectoryName(scriptPath);
                string editorFolder = Path.GetDirectoryName(windowsFolder);
                string scriptsFolder = Path.GetDirectoryName(editorFolder);
                cachedModuleRootPath = Path.GetDirectoryName(scriptsFolder).Replace('\\', '/');
                return cachedModuleRootPath;
            }
        }

        private static string SoFolderPath => $"{ModuleRootPath}/So";
        private static string PlanAssetPath => $"{SoFolderPath}/PaintedBuildingPlan.asset";
        private static string ConventionAssetPath => $"{SoFolderPath}/BuildingGridConvention.asset";
        private static string BrushPresetFolderPath => $"{SoFolderPath}/BrushPresets";
        private static string InteriorBrushPresetFolderPath => $"{SoFolderPath}/InteriorBrushes";
        private static string InteriorLayoutAssetPath => $"{SoFolderPath}/PaintedInteriorLayout.asset";
        private const string PlanPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.PlanPath";
        private const string GeneratorPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.GeneratorId";
        private const string InteriorLayoutPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.InteriorLayoutPath";
        private const string LeftWidthPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.LeftWidth";
        private const string RightWidthPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.RightWidth";
        private const string ToolPanelTabPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.ToolPanelTab";
        private const string PaintToolPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.PaintTool";
        private const string CurrentBrushPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.CurrentBrushPath";
        private const string LastWallBrushPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.LastWallBrushPath";
        private const string LastFloorBrushPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.LastFloorBrushPath";
        private const string WallFoldoutPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.WallFoldout";
        private const string FloorFoldoutPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.FloorFoldout";
        private const float MinCellPixelSize = 10f;
        private const float MaxCellPixelSize = 64f;
        private const float MinSideWidth = 130f;
        private const float MaxSideWidth = 360f;
        private const float SplitterWidth = 5f;
        private const float InteriorPreviewSize = 210f;
        private const float InteriorPreviewMinZoomDistance = 0.35f;
        private const float InteriorPreviewMaxZoomDistance = 6f;
        private const float InteriorPreviewBaseDistanceFactor = 2.6f;

        /// <summary>
        /// 绘制蓝图
        /// </summary>
        [SerializeField]
        private PaintedBuildingPlan paintedPlan;

        /// <summary>
        /// 绘制生成器
        /// </summary>
        [SerializeField]
        private PaintedBuildingGenerator generator;

        /// <summary>
        /// 格子公约
        /// </summary>
        [SerializeField]
        private BuildingGridConvention convention;

        /// <summary>
        /// 笔刷预设列表
        /// </summary>
        [SerializeField]
        private List<PaintedBuildingBrushPreset> brushPresetList = new();

        /// <summary>
        /// 当前笔刷模式
        /// </summary>
        [SerializeField]
        private EBrushMode brushMode;

        /// <summary>
        /// 内饰布局
        /// </summary>
        [SerializeField]
        private PaintedInteriorLayout interiorLayout;

        /// <summary>
        /// 内饰笔刷预设列表
        /// </summary>
        [SerializeField]
        private List<InteriorFurnitureBrushPreset> interiorFurnitureBrushPresetList = new();

        /// <summary>
        /// 当前内饰笔刷预设
        /// </summary>
        [SerializeField]
        private InteriorFurnitureBrushPreset currentInteriorFurnitureBrushPreset;

        /// <summary>
        /// 当前内饰朝向
        /// </summary>
        [SerializeField]
        private EInteriorFurnitureRotation currentInteriorRotation;

        /// <summary>
        /// 已观察的内饰笔刷
        /// </summary>
        private InteriorFurnitureBrushPreset observedInteriorFurnitureBrushPreset;

        /// <summary>
        /// 已观察的笔刷默认朝向
        /// </summary>
        private EInteriorFurnitureRotation observedInteriorDefaultRotation;

        /// <summary>
        /// 当前笔刷预设
        /// </summary>
        [SerializeField]
        private PaintedBuildingBrushPreset currentBrushPreset;

        /// <summary>
        /// 最近选用的墙体笔刷
        /// </summary>
        [SerializeField]
        private PaintedBuildingBrushPreset lastWallBrushPreset;

        /// <summary>
        /// 最近选用的地面笔刷
        /// </summary>
        [SerializeField]
        private PaintedBuildingBrushPreset lastFloorBrushPreset;

        /// <summary>
        /// 墙体子笔刷折叠
        /// </summary>
        [SerializeField]
        private bool wallBrushFoldoutExpanded = true;

        /// <summary>
        /// 地面子笔刷折叠
        /// </summary>
        [SerializeField]
        private bool floorBrushFoldoutExpanded = true;

        /// <summary>
        /// 当前楼层
        /// </summary>
        [SerializeField]
        private int currentFloorIndex;

        /// <summary>
        /// 当前格子类型
        /// </summary>
        [SerializeField]
        private EPaintedBuildingCellType currentCellType = EPaintedBuildingCellType.Wall;

        /// <summary>
        /// 墙体高度格数
        /// </summary>
        [SerializeField]
        private int wallHeightGridCount = 3;

        /// <summary>
        /// 挖空起点高度
        /// </summary>
        [SerializeField]
        private int cutoutStartHeightGridCount;

        /// <summary>
        /// 挖空终点高度
        /// </summary>
        [SerializeField]
        private int cutoutEndHeightGridCount = 2;

        /// <summary>
        /// 地面填充左下角
        /// </summary>
        [SerializeField]
        private Vector2Int floorFillBottomLeftGridPos;

        /// <summary>
        /// 地面填充右上角
        /// </summary>
        [SerializeField]
        private Vector2Int floorFillTopRightGridPos = new Vector2Int(5, 5);

        /// <summary>
        /// 墙体厚度格数
        /// </summary>
        [SerializeField]
        private int wallThicknessGridCount = 1;

        /// <summary>
        /// 墙体延伸方向
        /// </summary>
        [SerializeField]
        private EWallExtendDirection wallExtendDirection = EWallExtendDirection.Outward;

        /// <summary>
        /// 生成前清空楼层
        /// </summary>
        [SerializeField]
        private bool roomClearBeforeGenerate;

        /// <summary>
        /// 房间锚点格坐标
        /// </summary>
        [SerializeField]
        private Vector2Int roomAnchorGridPos;

        /// <summary>
        /// 房间宽度格数
        /// </summary>
        [SerializeField]
        private int roomWidthGridCount = 6;

        /// <summary>
        /// 房间深度格数
        /// </summary>
        [SerializeField]
        private int roomDepthGridCount = 6;

        /// <summary>
        /// 房间是否带门
        /// </summary>
        [SerializeField]
        private bool roomEnableDoor = true;

        /// <summary>
        /// 房间门所在墙面
        /// </summary>
        [SerializeField]
        private ERoomDoorWallSide roomDoorWallSide = ERoomDoorWallSide.Down;

        /// <summary>
        /// 房间门偏移格数
        /// </summary>
        [SerializeField]
        private int roomDoorOffsetGridCount = 2;

        /// <summary>
        /// 房间门宽格数
        /// </summary>
        [SerializeField]
        private int roomDoorWidthGridCount = 1;

        /// <summary>
        /// 阵列行数
        /// </summary>
        [SerializeField]
        private int roomGridRowCount = 2;

        /// <summary>
        /// 阵列列数
        /// </summary>
        [SerializeField]
        private int roomGridColumnCount = 2;

        /// <summary>
        /// 房间横向邻近间隔格数
        /// </summary>
        [SerializeField]
        private int roomAdjacentSpacingGridCount = 1;

        /// <summary>
        /// 走廊宽度格数
        /// </summary>
        [SerializeField]
        private int roomCorridorWidthGridCount = 1;

        /// <summary>
        /// 阵列门模式
        /// </summary>
        [SerializeField]
        private ERoomGridDoorMode roomGridDoorMode = ERoomGridDoorMode.Same;

        /// <summary>
        /// 阵列门随机种子
        /// </summary>
        [SerializeField]
        private int roomGridDoorRandomSeed = 12345;

        /// <summary>
        /// 单格像素大小
        /// </summary>
        [SerializeField]
        private float cellPixelSize = 24f;

        /// <summary>
        /// 左侧栏宽度
        /// </summary>
        [SerializeField]
        private float leftPanelWidth = 180f;

        /// <summary>
        /// 右侧栏宽度
        /// </summary>
        [SerializeField]
        private float rightPanelWidth = 260f;

        /// <summary>
        /// 网格平移
        /// </summary>
        [SerializeField]
        private Vector2 gridPanOffset;

        /// <summary>
        /// 鼠标悬停格子
        /// </summary>
        private Vector2Int hoverGridPos;

        /// <summary>
        /// 是否存在悬停格子
        /// </summary>
        private bool hasHoverGridPos;

        /// <summary>
        /// 是否正在拖拽平移
        /// </summary>
        private bool isPanning;

        /// <summary>
        /// 是否正在框选绘制
        /// </summary>
        private bool isSelectingCells;

        /// <summary>
        /// 框选起点格子
        /// </summary>
        private Vector2Int selectionStartGridPos;

        /// <summary>
        /// 框选终点格子
        /// </summary>
        private Vector2Int selectionEndGridPos;

        /// <summary>
        /// 是否正在拖拽左侧分隔条
        /// </summary>
        private bool isDraggingLeftSplitter;

        /// <summary>
        /// 是否正在拖拽右侧分隔条
        /// </summary>
        private bool isDraggingRightSplitter;

        /// <summary>
        /// 上次鼠标位置
        /// </summary>
        private Vector2 lastMousePos;

        /// <summary>
        /// 笔刷滚动位置
        /// </summary>
        private Vector2 brushScrollPos;

        /// <summary>
        /// 绘制页签滚动位置
        /// </summary>
        private Vector2 toolPaintScrollPos;

        /// <summary>
        /// 通用页签滚动位置
        /// </summary>
        private Vector2 toolGeneralScrollPos;

        /// <summary>
        /// 工具面板页签索引
        /// </summary>
        private int toolPanelTabIndex;

        /// <summary>
        /// 是否存在未保存绘制数据
        /// </summary>
        private bool hasDirtyPaintData;

        /// <summary>
        /// 内饰预览占用格列表
        /// </summary>
        private readonly List<Vector2Int> interiorOccupiedGridPosList = new();

        /// <summary>
        /// 是否正在刷入内饰
        /// </summary>
        private bool isPaintingInterior;

        /// <summary>
        /// 是否正在擦除内饰
        /// </summary>
        private bool isErasingInterior;

        /// <summary>
        /// 是否存在上一个内饰拖动格
        /// </summary>
        private bool hasLastInteriorPaintGridPos;

        /// <summary>
        /// 上一个内饰拖动格
        /// </summary>
        private Vector2Int lastInteriorPaintGridPos;

        /// <summary>
        /// 当前内饰拖动是否修改数据
        /// </summary>
        private bool hasInteriorPaintChanged;

        /// <summary>
        /// 当前内饰拖动是否记录撤销
        /// </summary>
        private bool hasInteriorUndoRecord;

        /// <summary>
        /// 内饰占用缓存是否需要重建
        /// </summary>
        private bool interiorOccupancyCacheDirty = true;

        /// <summary>
        /// 内饰占用格缓存
        /// </summary>
        private readonly HashSet<Vector2Int> interiorOccupiedGridHashList = new();

        /// <summary>
        /// 当前楼层地面格缓存
        /// </summary>
        private readonly HashSet<Vector2Int> interiorFloorGridHashList = new();

        /// <summary>
        /// 当前楼层结构格缓存
        /// </summary>
        private readonly HashSet<Vector2Int> interiorStructureGridHashList = new();

        /// <summary>
        /// 占用缓存关联布局
        /// </summary>
        private PaintedInteriorLayout cachedInteriorLayout;

        /// <summary>
        /// 占用缓存楼层
        /// </summary>
        private int cachedInteriorFloorIndex = -1;

        /// <summary>
        /// 占用缓存布局数量
        /// </summary>
        private int cachedInteriorPlacementCount = -1;

        /// <summary>
        /// 占用缓存地面数量
        /// </summary>
        private int cachedInteriorFloorCellCount = -1;

        /// <summary>
        /// 占用缓存结构数量
        /// </summary>
        private int cachedInteriorStructureCellCount = -1;

        /// <summary>
        /// 内饰预制体预览工具
        /// </summary>
        private static PreviewRenderUtility interiorPreviewUtility;

        /// <summary>
        /// 内饰预制体预览实例
        /// </summary>
        private static GameObject interiorPreviewInstance;

        /// <summary>
        /// 内饰预览源预制体路径
        /// </summary>
        private static string interiorPreviewPrefabPath;

        /// <summary>
        /// 内饰预览相机轨道角度
        /// </summary>
        private static Vector2 interiorPreviewOrbitAngles = new Vector2(35f, -30f);

        /// <summary>
        /// 内饰预览缩放距离
        /// </summary>
        private static float interiorPreviewZoomDistance = 1.6f;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/MmBuilderSystem/建筑画笔")]
        public static void OpenWindow()
        {
            var window = GetWindow<BuildingPainterWindow>();
            window.titleContent = new GUIContent("建筑画笔");
            window.minSize = new Vector2(760f, 420f);
            window.Show();
        }

        /// <summary>
        /// 启用窗口
        /// </summary>
        private void OnEnable()
        {
            wantsMouseMove = true;
            leftPanelWidth = EditorPrefs.GetFloat(LeftWidthPrefsKey, leftPanelWidth);
            rightPanelWidth = EditorPrefs.GetFloat(RightWidthPrefsKey, rightPanelWidth);
            toolPanelTabIndex = EditorPrefs.GetInt(ToolPanelTabPrefsKey, toolPanelTabIndex);
            EnsureDefaultAssets();
            EnsureInteriorAssets();
            LoadPersistedReferences();
            EnsureConventionReference();
            SyncWallThicknessFromConvention();
            SyncGlobalSettingsFromPlan();
            LoadPaintToolPrefs();
            RepairCutoutWallBrushPresets();
            SyncGeneratorReferences();
        }

        /// <summary>
        /// 禁用窗口
        /// </summary>
        private void OnDisable()
        {
            DisposeInteriorPrefabPreview();
            PersistReferences();
            SavePaintToolPrefs();
            EditorPrefs.SetFloat(LeftWidthPrefsKey, leftPanelWidth);
            EditorPrefs.SetFloat(RightWidthPrefsKey, rightPanelWidth);
            EditorPrefs.SetInt(ToolPanelTabPrefsKey, toolPanelTabIndex);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 绘制窗口
        /// </summary>
        private void OnGUI()
        {
            if (paintedPlan == null || brushPresetList.Count == 0)
                EnsureDefaultAssets();

            if (interiorLayout == null)
                EnsureInteriorAssets();

            SyncCurrentInteriorRotationFromBrush();

            Rect windowRect = new Rect(0f, 0f, position.width, position.height);
            ClampPanelWidths(windowRect.width);
            Rect brushRect = new Rect(0f, 0f, leftPanelWidth, windowRect.height);
            Rect leftSplitterRect = new Rect(brushRect.xMax, 0f, SplitterWidth, windowRect.height);
            Rect toolRect = new Rect(windowRect.width - rightPanelWidth, 0f, rightPanelWidth, windowRect.height);
            Rect rightSplitterRect = new Rect(toolRect.x - SplitterWidth, 0f, SplitterWidth, windowRect.height);
            Rect gridRect = new Rect(leftSplitterRect.xMax, 0f, rightSplitterRect.x - leftSplitterRect.xMax, windowRect.height);

            DrawPanelBackground(brushRect, new Color(0.18f, 0.18f, 0.18f));
            DrawPanelBackground(gridRect, new Color(0.12f, 0.12f, 0.12f));
            DrawPanelBackground(toolRect, new Color(0.18f, 0.18f, 0.18f));
            DrawPanelBackground(leftSplitterRect, new Color(0.06f, 0.06f, 0.06f));
            DrawPanelBackground(rightSplitterRect, new Color(0.06f, 0.06f, 0.06f));

            DrawBrushPanel(brushRect);
            DrawGridPanel(gridRect);
            DrawToolPanel(toolRect);
            HandleSplitterInput(windowRect, leftSplitterRect, rightSplitterRect);
            HandleKeyboardInput();
        }

        /// <summary>
        /// 绘制面板背景
        /// </summary>
        private void DrawPanelBackground(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);
        }

        /// <summary>
        /// 绘制笔刷面板
        /// </summary>
        private void DrawBrushPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            brushScrollPos = EditorGUILayout.BeginScrollView(brushScrollPos);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("笔刷", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            int newBrushModeIndex = GUILayout.Toolbar((int)brushMode, new[] { "建筑", "内饰" });
            if (newBrushModeIndex != (int)brushMode)
            {
                brushMode = (EBrushMode)newBrushModeIndex;
                InvalidateInteriorOccupancyCache();
                if (brushMode == EBrushMode.Interior && currentInteriorFurnitureBrushPreset != null)
                    currentInteriorRotation = currentInteriorFurnitureBrushPreset.defaultRotation;
                Repaint();
            }

            EditorGUILayout.Space(6f);
            if (brushMode == EBrushMode.Building)
                DrawBuildingBrushPanel();
            else
                DrawInteriorBrushPanel();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 绘制建筑笔刷面板
        /// </summary>
        private void DrawBuildingBrushPanel()
        {
            foreach (EPaintedBuildingCellType cellType in Enum.GetValues(typeof(EPaintedBuildingCellType)))
            {
                var cellBrushPresetList = CollectBrushPresetsByType(cellType);
                if (cellBrushPresetList.Count == 0)
                    continue;

                if (cellType == EPaintedBuildingCellType.CutoutFill)
                {
                    DrawCutoutFillBrushPanel(cellBrushPresetList);
                    continue;
                }

                if (SupportsSubBrush(cellType))
                    DrawSubBrushGroup(cellType, cellBrushPresetList);
                else
                    DrawBrushButton(GetPreferredBrushPreset(cellBrushPresetList));
            }

            GUILayout.FlexibleSpace();
            DrawBrushGlobalSettings();
            EditorGUILayout.Space(4f);
            if (GUILayout.Button("刷新默认笔刷", GUILayout.Height(28f)))
            {
                EnsureAllBrushPresets();
                LoadBrushPresetList();
            }
        }

        /// <summary>
        /// 是否支持子笔刷
        /// </summary>
        private static bool SupportsSubBrush(EPaintedBuildingCellType cellType) =>
            cellType == EPaintedBuildingCellType.Wall
            || cellType == EPaintedBuildingCellType.Floor;

        /// <summary>
        /// 绘制挖空填充物笔刷
        /// </summary>
        private void DrawCutoutFillBrushPanel(List<PaintedBuildingBrushPreset> fillBrushPresetList)
        {
            EditorGUILayout.LabelField("挖空填充物", EditorStyles.boldLabel);
            foreach (var brushPreset in fillBrushPresetList)
            {
                DrawBrushButton(brushPreset);
            }

            EditorGUILayout.Space(2f);
            if (GUILayout.Button("新建挖空填充物笔刷", GUILayout.Height(24f)))
                CreateSubBrushPreset(EPaintedBuildingCellType.CutoutFill);

            if (currentBrushPreset != null
                && currentBrushPreset.cellType == EPaintedBuildingCellType.CutoutFill
                && !currentBrushPreset.isPrimaryPreset
                && GUILayout.Button("删除当前挖空填充物笔刷", GUILayout.Height(22f)))
            {
                DeleteCurrentSubBrushPreset(EPaintedBuildingCellType.CutoutFill);
            }
        }

        /// <summary>
        /// 绘制可展开子笔刷分组
        /// </summary>
        private void DrawSubBrushGroup(
            EPaintedBuildingCellType cellType,
            List<PaintedBuildingBrushPreset> cellBrushPresetList)
        {
            string groupTitle = GetBrushGroupDisplayName(cellType);
            bool foldoutExpanded = GetSubBrushFoldout(cellType);
            foldoutExpanded = EditorGUILayout.Foldout(foldoutExpanded, groupTitle, true);
            SetSubBrushFoldout(cellType, foldoutExpanded);
            if (!foldoutExpanded)
                return;

            EditorGUI.indentLevel++;
            foreach (var brushPreset in cellBrushPresetList)
            {
                if (brushPreset == null)
                    continue;

                DrawBrushButton(brushPreset);
            }

            EditorGUILayout.Space(2f);
            if (GUILayout.Button(
                    $"新建{groupTitle}材质笔刷",
                    GUILayout.Height(24f)))
            CreateSubBrushPreset(cellType);

            if (currentBrushPreset != null
                && currentBrushPreset.cellType == cellType
                && !currentBrushPreset.isPrimaryPreset
                && GUILayout.Button($"删除当前{groupTitle}笔刷", GUILayout.Height(22f)))
            {
                DeleteCurrentSubBrushPreset(cellType);
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 读取子笔刷折叠
        /// </summary>
        private bool GetSubBrushFoldout(EPaintedBuildingCellType cellType)
        {
            switch (cellType)
            {
                case EPaintedBuildingCellType.Floor:
                    return floorBrushFoldoutExpanded;
                default:
                    return wallBrushFoldoutExpanded;
            }
        }

        /// <summary>
        /// 写入子笔刷折叠
        /// </summary>
        private void SetSubBrushFoldout(EPaintedBuildingCellType cellType, bool expanded)
        {
            if (cellType == EPaintedBuildingCellType.Floor)
            {
                floorBrushFoldoutExpanded = expanded;
                return;
            }

            if (cellType == EPaintedBuildingCellType.Wall)
                wallBrushFoldoutExpanded = expanded;
        }

        /// <summary>
        /// 按类型收集笔刷
        /// </summary>
        private List<PaintedBuildingBrushPreset> CollectBrushPresetsByType(EPaintedBuildingCellType cellType)
        {
            var resultList = new List<PaintedBuildingBrushPreset>();
            foreach (var brushPreset in brushPresetList)
            {
                if (brushPreset != null && brushPreset.cellType == cellType)
                    resultList.Add(brushPreset);
            }

            return resultList;
        }

        /// <summary>
        /// 获取优先笔刷 主笔刷优先
        /// </summary>
        private static PaintedBuildingBrushPreset GetPreferredBrushPreset(List<PaintedBuildingBrushPreset> cellBrushPresetList)
        {
            if (cellBrushPresetList == null || cellBrushPresetList.Count == 0)
                return null;

            foreach (var brushPreset in cellBrushPresetList)
            {
                if (brushPreset != null && brushPreset.isPrimaryPreset)
                    return brushPreset;
            }

            return cellBrushPresetList[0];
        }

        /// <summary>
        /// 绘制内饰笔刷面板
        /// </summary>
        private void DrawInteriorBrushPanel()
        {
            EditorGUILayout.HelpBox("内饰笔刷会记录预制体 占格和朝向 点击网格后再生成完整家具", MessageType.Info);
            currentFloorIndex = Mathf.Max(0, EditorGUILayout.IntField("当前楼层", currentFloorIndex));
            if (paintedPlan != null)
                EditorGUILayout.LabelField("当前地面高度", $"Y = {paintedPlan.GetFloorBaseY(currentFloorIndex)}");

            foreach (EInteriorFurnitureCategory category in Enum.GetValues(typeof(EInteriorFurnitureCategory)))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(GetInteriorCategoryDisplayName(category), EditorStyles.boldLabel);
                bool hasBrush = false;
                foreach (var brushPreset in interiorFurnitureBrushPresetList)
                {
                    if (brushPreset == null || brushPreset.category != category)
                        continue;

                    hasBrush = true;
                    DrawInteriorBrushButton(brushPreset);
                }

                if (!hasBrush)
                    EditorGUILayout.LabelField("暂无型号", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("新建内饰笔刷", GUILayout.Height(28f)))
                CreateInteriorBrushPreset();

            if (currentInteriorFurnitureBrushPreset != null
                && GUILayout.Button("删除当前内饰笔刷", GUILayout.Height(24f)))
            {
                DeleteCurrentInteriorBrushPreset();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新内饰笔刷", GUILayout.Height(28f)))
                LoadInteriorBrushPresetList();
        }

        /// <summary>
        /// 绘制笔刷全局设置
        /// </summary>
        private void DrawBrushGlobalSettings()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("笔刷设置", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            currentFloorIndex = Mathf.Max(0, EditorGUILayout.IntField("当前楼层", currentFloorIndex));
            if (paintedPlan != null)
                EditorGUILayout.LabelField("当前地面高度", $"Y = {paintedPlan.GetFloorBaseY(currentFloorIndex)}");

            wallHeightGridCount = Mathf.Max(1, EditorGUILayout.IntField("全局墙体高度", wallHeightGridCount));
            if (EditorGUI.EndChangeCheck())
            {
                cutoutStartHeightGridCount = Mathf.Clamp(cutoutStartHeightGridCount, 0, wallHeightGridCount - 1);
                cutoutEndHeightGridCount = Mathf.Clamp(
                    cutoutEndHeightGridCount,
                    cutoutStartHeightGridCount + 1,
                    wallHeightGridCount);

                if (paintedPlan != null && paintedPlan.globalWallHeightGridCount != wallHeightGridCount)
                {
                    Undo.RecordObject(paintedPlan, "修改全局墙体高度");
                    paintedPlan.globalWallHeightGridCount = wallHeightGridCount;
                    EditorUtility.SetDirty(paintedPlan);
                }
            }

            if (currentFloorIndex > 0)
            {
                if (GUILayout.Button("复制上一层布局", GUILayout.Height(22f)))
                    CopyPreviousFloorLayout();
            }
        }

        /// <summary>
        /// 绘制笔刷按钮
        /// </summary>
        private void DrawBrushButton(PaintedBuildingBrushPreset brushPreset)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 34f, GUILayout.ExpandWidth(true));
            bool isSelected = currentBrushPreset == brushPreset;
            Color color = brushPreset.previewColor;
            Color backgroundColor = isSelected
                ? new Color(color.r, color.g, color.b, 0.75f)
                : new Color(color.r, color.g, color.b, 0.35f);
            EditorGUI.DrawRect(rect, backgroundColor);

            Rect labelRect = new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 20f);
            GUI.Label(labelRect, GetBrushLabel(brushPreset), EditorStyles.boldLabel);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectBrush(brushPreset);
                Event.current.Use();
            }
        }

        /// <summary>
        /// 绘制内饰笔刷按钮
        /// </summary>
        private void DrawInteriorBrushButton(InteriorFurnitureBrushPreset brushPreset)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 38f, GUILayout.ExpandWidth(true));
            bool isSelected = currentInteriorFurnitureBrushPreset == brushPreset;
            Color color = brushPreset.previewColor;
            Color backgroundColor = isSelected
                ? new Color(color.r, color.g, color.b, 0.75f)
                : new Color(color.r, color.g, color.b, 0.35f);
            EditorGUI.DrawRect(rect, backgroundColor);

            Rect labelRect = new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 18f);
            GUI.Label(labelRect, brushPreset.DisplayName, EditorStyles.boldLabel);
            Rect sizeRect = new Rect(rect.x + 10f, rect.y + 21f, rect.width - 20f, 14f);
            GUI.Label(sizeRect, $"占格 {brushPreset.FootprintGridSize.x} x {brushPreset.FootprintGridSize.y}", EditorStyles.miniLabel);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectInteriorBrush(brushPreset);
                Event.current.Use();
            }
        }

        /// <summary>
        /// 绘制网格面板
        /// </summary>
        private void DrawGridPanel(Rect rect)
        {
            DrawGridHeader(rect);
            Rect gridRect = new Rect(rect.x + 8f, rect.y + 42f, rect.width - 16f, rect.height - 50f);
            EditorGUI.DrawRect(gridRect, new Color(0.1f, 0.1f, 0.1f));

            GUI.BeginGroup(gridRect);
            Rect localGridRect = new Rect(0f, 0f, gridRect.width, gridRect.height);
            Event e = Event.current;
            UpdateHoverGridPos(localGridRect, e.mousePosition);
            DrawGridLines(localGridRect);
            DrawPaintedCells(localGridRect);
            DrawSelectionRect(localGridRect);
            DrawInteriorHoverPreview(localGridRect);
            DrawHoverCell(localGridRect);
            DrawRoomHoverPreview(localGridRect);
            HandleGridInput(localGridRect, e);
            GUI.EndGroup();
        }

        /// <summary>
        /// 绘制网格标题
        /// </summary>
        private void DrawGridHeader(Rect rect)
        {
            Rect titleRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 24f);
            string hoverText = hasHoverGridPos ? $"  格子 {hoverGridPos.x} {hoverGridPos.y}" : string.Empty;
            string modeText = brushMode == EBrushMode.Building
                ? "建筑"
                : $"内饰  {currentInteriorFurnitureBrushPreset?.DisplayName ?? "未选择"}  {GetInteriorRotationDisplayName(currentInteriorRotation)}";
            GUI.Label(titleRect, $"{modeText}  楼层 {currentFloorIndex}{hoverText}", EditorStyles.boldLabel);
        }

        /// <summary>
        /// 绘制工具面板
        /// </summary>
        private void DrawToolPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.Space(6f);
            int newToolPanelTabIndex = GUILayout.Toolbar(toolPanelTabIndex, new[] { "绘制", "通用" });
            if (newToolPanelTabIndex != toolPanelTabIndex)
            {
                toolPanelTabIndex = newToolPanelTabIndex;
                EditorPrefs.SetInt(ToolPanelTabPrefsKey, toolPanelTabIndex);
            }

            EditorGUILayout.Space(4f);
            if (toolPanelTabIndex == 0)
                DrawToolPaintTab();
            else
                DrawToolGeneralTab();

            GUILayout.EndArea();
        }

        /// <summary>
        /// 绘制工具页签
        /// </summary>
        private void DrawToolPaintTab()
        {
            toolPaintScrollPos = EditorGUILayout.BeginScrollView(toolPaintScrollPos);
            EditorGUILayout.Space(4f);
            if (brushMode == EBrushMode.Building)
                DrawBrushSettings();
            else
                DrawInteriorBrushSettings();
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(GetGridHelpMessage(), MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制通用页签
        /// </summary>
        private void DrawToolGeneralTab()
        {
            toolGeneralScrollPos = EditorGUILayout.BeginScrollView(toolGeneralScrollPos);
            EditorGUILayout.Space(4f);
            DrawAssetFields();
            EditorGUILayout.Space(8f);
            if (brushMode == EBrushMode.Building)
                DrawActionButtons();
            else
                DrawInteriorActionButtons();
            EditorGUILayout.Space(8f);
            DrawViewSettings();
            if (brushMode == EBrushMode.Building)
            {
                EditorGUILayout.Space(8f);
                DrawOptimizationPanel();
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制资产字段
        /// </summary>
        private void DrawAssetFields()
        {
            EditorGUILayout.LabelField("持久化资产", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            paintedPlan = (PaintedBuildingPlan)EditorGUILayout.ObjectField("绘制蓝图", paintedPlan, typeof(PaintedBuildingPlan), false);
            interiorLayout = (PaintedInteriorLayout)EditorGUILayout.ObjectField("内饰布局", interiorLayout, typeof(PaintedInteriorLayout), false);
            generator = (PaintedBuildingGenerator)EditorGUILayout.ObjectField("生成器", generator, typeof(PaintedBuildingGenerator), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (interiorLayout != null)
                {
                    interiorLayout.paintedBuildingPlan = paintedPlan;
                    EditorUtility.SetDirty(interiorLayout);
                }

                EnsureConventionReference();
                SyncWallThicknessFromConvention();
                SyncGlobalSettingsFromPlan();
                RepairCutoutWallBrushPresets();
                SyncGeneratorReferences();
                PersistReferences();
            }
        }

        /// <summary>
        /// 绘制笔刷设置
        /// </summary>
        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("当前笔刷", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            currentBrushPreset = (PaintedBuildingBrushPreset)EditorGUILayout.ObjectField("笔刷预设", currentBrushPreset, typeof(PaintedBuildingBrushPreset), false);
            if (EditorGUI.EndChangeCheck() && currentBrushPreset != null)
                ApplyBrushPreset(currentBrushPreset);

            currentCellType = (EPaintedBuildingCellType)EditorGUILayout.EnumPopup("绘制类型", currentCellType);

            if (currentBrushPreset != null
                && (SupportsSubBrush(currentBrushPreset.cellType)
                    || currentBrushPreset.cellType == EPaintedBuildingCellType.CutoutFill))
                DrawBuildingBrushAssetSettings(currentBrushPreset);

            if (currentCellType == EPaintedBuildingCellType.Floor)
                DrawFloorBrushTools();

            if (currentCellType == EPaintedBuildingCellType.Wall)
                DrawWallBrushTools();

            if (currentCellType == EPaintedBuildingCellType.Cutout)
            {
                cutoutStartHeightGridCount = Mathf.Clamp(EditorGUILayout.IntField("挖空起点高度", cutoutStartHeightGridCount), 0, wallHeightGridCount - 1);
                cutoutEndHeightGridCount = Mathf.Clamp(EditorGUILayout.IntField("挖空终点高度", cutoutEndHeightGridCount), cutoutStartHeightGridCount + 1, wallHeightGridCount);
                EditorGUILayout.HelpBox("挖空从地面上方的墙体开始计算 地面层不会被墙体或挖空覆盖", MessageType.Info);
            }

            if (currentCellType == EPaintedBuildingCellType.CutoutFill)
                EditorGUILayout.HelpBox("左键点击已有挖空格设置填充物 不会修改洞口高度或墙体材质", MessageType.Info);

            if (currentCellType == EPaintedBuildingCellType.Room)
                DrawRoomBrushTools();

            if (currentCellType == EPaintedBuildingCellType.Erase)
                DrawEraseBrushTools();
        }

        /// <summary>
        /// 绘制建筑笔刷资产设置
        /// </summary>
        private void DrawBuildingBrushAssetSettings(PaintedBuildingBrushPreset brushPreset)
        {
            EditorGUILayout.Space(4f);
            string title = brushPreset.cellType == EPaintedBuildingCellType.CutoutFill
                ? "挖空填充物笔刷"
                : $"{GetCellTypeDisplayName(brushPreset.cellType)}材质笔刷";
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string displayName = EditorGUILayout.TextField("显示名称", brushPreset.displayName);
            Color previewColor = EditorGUILayout.ColorField("预览颜色", brushPreset.previewColor);
            var material = brushPreset.material;
            var cutoutFillPrefab = brushPreset.cutoutFillPrefab;
            float cutoutFillYRotation = brushPreset.cutoutFillYRotation;
            if (brushPreset.cellType == EPaintedBuildingCellType.CutoutFill)
            {
                cutoutFillPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "填充预制体",
                    cutoutFillPrefab,
                    typeof(GameObject),
                    false);
                cutoutFillYRotation = EditorGUILayout.FloatField("填充Y轴角度", cutoutFillYRotation);
                EditorGUILayout.HelpBox("未设置填充预制体时 会保持普通挖空", MessageType.None);
            }
            else
            {
                material = (Material)EditorGUILayout.ObjectField("生成材质", material, typeof(Material), false);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(brushPreset, $"修改{GetCellTypeDisplayName(brushPreset.cellType)}笔刷");
                brushPreset.displayName = displayName;
                brushPreset.previewColor = previewColor;
                brushPreset.material = material;
                brushPreset.cutoutFillPrefab = cutoutFillPrefab;
                brushPreset.cutoutFillYRotation = cutoutFillYRotation;
                EditorUtility.SetDirty(brushPreset);
            }
        }

        /// <summary>
        /// 绘制内饰笔刷设置
        /// </summary>
        private void DrawInteriorBrushSettings()
        {
            EditorGUILayout.LabelField("当前内饰笔刷", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            currentInteriorFurnitureBrushPreset = (InteriorFurnitureBrushPreset)EditorGUILayout.ObjectField(
                "笔刷预设",
                currentInteriorFurnitureBrushPreset,
                typeof(InteriorFurnitureBrushPreset),
                false);
            if (EditorGUI.EndChangeCheck())
                SelectInteriorBrush(currentInteriorFurnitureBrushPreset);

            if (currentInteriorFurnitureBrushPreset == null)
            {
                EditorGUILayout.HelpBox("请先在左侧新建或选择内饰笔刷", MessageType.Info);
                return;
            }

            DrawInteriorBrushAssetSettings(currentInteriorFurnitureBrushPreset);
            EditorGUILayout.HelpBox(
                "刷入初始朝向只影响之后新刷入的家具\n模型正面校正只调整预制体模型与网格箭头的对应关系\n已经刷入的家具会保存自己的朝向",
                MessageType.Info);
            DrawInteriorPrefabPreview(currentInteriorFurnitureBrushPreset);
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("当前放置朝向", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("旋转左", GUILayout.Height(24f)))
                RotateInteriorBrush(-1);
            EditorGUILayout.LabelField(GetInteriorRotationDisplayName(currentInteriorRotation), EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button("旋转右", GUILayout.Height(24f)))
                RotateInteriorBrush(1);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("网格中会显示占用区域和正面箭头 R 键可顺时针旋转", MessageType.None);
        }

        /// <summary>
        /// 绘制内饰笔刷资产设置
        /// </summary>
        private void DrawInteriorBrushAssetSettings(InteriorFurnitureBrushPreset brushPreset)
        {
            EditorGUI.BeginChangeCheck();
            EInteriorFurnitureRotation oldDefaultRotation = brushPreset.defaultRotation;
            string displayName = EditorGUILayout.TextField("显示名称", brushPreset.displayName);
            var category = (EInteriorFurnitureCategory)EditorGUILayout.EnumPopup("内饰大类", brushPreset.category);
            var furniturePrefab = (GameObject)EditorGUILayout.ObjectField("家具预制体", brushPreset.furniturePrefab, typeof(GameObject), false);
            int footprintWidthGridCount = Mathf.Max(1, EditorGUILayout.IntField("占用宽度", brushPreset.footprintWidthGridCount));
            int footprintDepthGridCount = Mathf.Max(1, EditorGUILayout.IntField("占用深度", brushPreset.footprintDepthGridCount));
            int heightGridCount = Mathf.Max(1, EditorGUILayout.IntField("占用高度", brushPreset.heightGridCount));
            Color previewColor = EditorGUILayout.ColorField("预览颜色", brushPreset.previewColor);
            var defaultRotation = (EInteriorFurnitureRotation)EditorGUILayout.EnumPopup("刷入初始朝向", brushPreset.defaultRotation);
            var prefabRotationOffset = (EInteriorFurnitureRotation)EditorGUILayout.EnumPopup("模型正面校正", brushPreset.prefabRotationOffset);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(brushPreset, "修改内饰笔刷");
                brushPreset.displayName = displayName;
                brushPreset.category = category;
                brushPreset.furniturePrefab = furniturePrefab;
                brushPreset.footprintWidthGridCount = footprintWidthGridCount;
                brushPreset.footprintDepthGridCount = footprintDepthGridCount;
                brushPreset.heightGridCount = heightGridCount;
                brushPreset.previewColor = previewColor;
                brushPreset.defaultRotation = defaultRotation;
                brushPreset.prefabRotationOffset = prefabRotationOffset;
                EditorUtility.SetDirty(brushPreset);
                InvalidateInteriorOccupancyCache();
                if (brushPreset == currentInteriorFurnitureBrushPreset
                    && oldDefaultRotation != defaultRotation)
                {
                    currentInteriorRotation = defaultRotation;
                    observedInteriorFurnitureBrushPreset = brushPreset;
                    observedInteriorDefaultRotation = defaultRotation;
                }
                LoadInteriorBrushPresetList();
            }
        }

        /// <summary>
        /// 绘制内饰预制体预览
        /// </summary>
        private void DrawInteriorPrefabPreview(InteriorFurnitureBrushPreset brushPreset)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("预制体预览", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("右键拖动旋转视角 滚轮缩放模型", EditorStyles.centeredGreyMiniLabel);
            if (brushPreset == null || brushPreset.furniturePrefab == null)
            {
                EditorGUILayout.HelpBox("指定家具预制体后显示预览", MessageType.None);
                return;
            }

            float previewSize = Mathf.Min(InteriorPreviewSize, Mathf.Max(160f, EditorGUIUtility.currentViewWidth - 48f));
            Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize);
            EditorGUI.DrawRect(previewRect, new Color(0.14f, 0.14f, 0.14f, 1f));
            EnsureInteriorPrefabPreview(brushPreset.furniturePrefab);
            HandleInteriorPrefabPreviewInput(previewRect);
            if (interiorPreviewInstance != null)
            {
                interiorPreviewInstance.transform.SetPositionAndRotation(
                    Vector3.zero,
                    InteriorFurniturePlacementUtility.GetWorldRotation(
                        brushPreset,
                        EInteriorFurnitureRotation.Deg0));
            }

            if (interiorPreviewInstance != null && Event.current.type == EventType.Repaint)
                DrawInteriorPrefabPreviewTexture(previewRect);

            EditorGUILayout.LabelField(brushPreset.furniturePrefab.name, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("模型左转", GUILayout.Height(24f)))
                RotatePrefabModel(brushPreset, -1);
            EditorGUILayout.LabelField(
                $"模型校正 {GetInteriorRotationDisplayName(brushPreset.prefabRotationOffset)}",
                EditorStyles.centeredGreyMiniLabel);
            if (GUILayout.Button("模型右转", GUILayout.Height(24f)))
                RotatePrefabModel(brushPreset, 1);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 确保内饰预制体预览实例
        /// </summary>
        private void EnsureInteriorPrefabPreview(GameObject prefab)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (interiorPreviewInstance != null && interiorPreviewPrefabPath == prefabPath)
                return;

            DisposeInteriorPrefabPreview();
            interiorPreviewUtility = new PreviewRenderUtility();
            interiorPreviewUtility.cameraFieldOfView = 28f;
            interiorPreviewUtility.camera.nearClipPlane = 0.01f;
            interiorPreviewUtility.camera.farClipPlane = 50f;
            interiorPreviewUtility.lights[0].intensity = 1.15f;
            interiorPreviewUtility.lights[0].transform.rotation = Quaternion.Euler(48f, 48f, 0f);
            interiorPreviewUtility.lights[1].intensity = 0.55f;
            interiorPreviewUtility.ambientColor = new Color(0.22f, 0.22f, 0.22f, 1f);
            interiorPreviewInstance = interiorPreviewUtility.InstantiatePrefabInScene(prefab);
            interiorPreviewInstance.transform.position = Vector3.zero;
            interiorPreviewPrefabPath = prefabPath;
            interiorPreviewZoomDistance = 1.6f;
        }

        /// <summary>
        /// 处理内饰预制体预览输入
        /// </summary>
        private void HandleInteriorPrefabPreviewInput(Rect previewRect)
        {
            Event e = Event.current;
            if (!previewRect.Contains(e.mousePosition))
                return;

            if (e.type == EventType.MouseDrag && e.button == 1)
            {
                interiorPreviewOrbitAngles.x += e.delta.x;
                interiorPreviewOrbitAngles.y -= e.delta.y;
                interiorPreviewOrbitAngles.y = Mathf.Clamp(interiorPreviewOrbitAngles.y, -85f, 85f);
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.ScrollWheel)
            {
                interiorPreviewZoomDistance *= 1f - e.delta.y * 0.08f;
                interiorPreviewZoomDistance = Mathf.Clamp(
                    interiorPreviewZoomDistance,
                    InteriorPreviewMinZoomDistance,
                    InteriorPreviewMaxZoomDistance);
                e.Use();
                Repaint();
            }
        }

        /// <summary>
        /// 绘制内饰预制体预览纹理
        /// </summary>
        private void DrawInteriorPrefabPreviewTexture(Rect previewRect)
        {
            Bounds bounds = CalculateInteriorPreviewBounds(interiorPreviewInstance);
            Vector3 center = bounds.center;
            float radius = Mathf.Max(bounds.extents.magnitude, 0.25f);
            float distance = radius * InteriorPreviewBaseDistanceFactor * interiorPreviewZoomDistance;
            Quaternion orbitRotation = Quaternion.Euler(
                interiorPreviewOrbitAngles.y,
                interiorPreviewOrbitAngles.x,
                0f);
            Vector3 cameraPosition = center + orbitRotation * (Vector3.back * distance);
            interiorPreviewUtility.camera.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(center - cameraPosition, Vector3.up));
            interiorPreviewUtility.camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 3f);
            interiorPreviewUtility.camera.farClipPlane = distance + radius * 4f;

            interiorPreviewUtility.BeginPreview(previewRect, GUIStyle.none);
            interiorPreviewUtility.Render(allowScriptableRenderPipeline: true);
            var previewTexture = interiorPreviewUtility.EndPreview();
            if (previewTexture != null)
                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.StretchToFill, false);
        }

        /// <summary>
        /// 获取内饰预览包围盒
        /// </summary>
        private Bounds CalculateInteriorPreviewBounds(GameObject previewObject)
        {
            var rendererList = previewObject.GetComponentsInChildren<Renderer>(true);
            if (rendererList.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            Bounds bounds = rendererList[0].bounds;
            for (int i = 1; i < rendererList.Length; i++)
                bounds.Encapsulate(rendererList[i].bounds);

            return bounds;
        }

        /// <summary>
        /// 旋转预制体模型校正
        /// </summary>
        private void RotatePrefabModel(InteriorFurnitureBrushPreset brushPreset, int step)
        {
            if (brushPreset == null)
                return;

            int rotationStep = ((int)brushPreset.prefabRotationOffset + step) % 4;
            if (rotationStep < 0)
                rotationStep += 4;

            Undo.RecordObject(brushPreset, "旋转预制体模型校正");
            brushPreset.prefabRotationOffset = (EInteriorFurnitureRotation)rotationStep;
            EditorUtility.SetDirty(brushPreset);
            Repaint();
        }

        /// <summary>
        /// 清理内饰预制体预览
        /// </summary>
        private static void DisposeInteriorPrefabPreview()
        {
            if (interiorPreviewInstance != null)
            {
                UnityEngine.Object.DestroyImmediate(interiorPreviewInstance);
                interiorPreviewInstance = null;
            }

            interiorPreviewPrefabPath = null;
            if (interiorPreviewUtility == null)
                return;

            interiorPreviewUtility.Cleanup();
            interiorPreviewUtility = null;
        }

        /// <summary>
        /// 绘制擦除笔刷工具
        /// </summary>
        private void DrawEraseBrushTools()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("右键可逐格擦除 下方按钮可一键清空当前楼层全部地面和结构", MessageType.None);
            if (GUILayout.Button("一键清空当前楼层网格", GUILayout.Height(28f)))
                ClearCurrentFloorGrid();
        }

        /// <summary>
        /// 绘制地面笔刷工具
        /// </summary>
        private void DrawFloorBrushTools()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("范围填充", EditorStyles.boldLabel);
            floorFillBottomLeftGridPos = EditorGUILayout.Vector2IntField("左下角坐标", floorFillBottomLeftGridPos);
            floorFillTopRightGridPos = EditorGUILayout.Vector2IntField("右上角坐标", floorFillTopRightGridPos);
            if (GUILayout.Button("填充地面范围", GUILayout.Height(28f)))
                FillFloorRange();
        }

        /// <summary>
        /// 绘制墙体笔刷工具
        /// </summary>
        private void DrawWallBrushTools()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("一键圈墙", EditorStyles.boldLabel);
            wallThicknessGridCount = Mathf.Max(1, EditorGUILayout.IntField("墙体厚度", wallThicknessGridCount));
            wallExtendDirection = (EWallExtendDirection)EditorGUILayout.EnumPopup("延伸方向", wallExtendDirection);
            EditorGUILayout.HelpBox("基于当前楼层地面最外围圈墙 厚度包含最外圈本身", MessageType.None);
            if (GUILayout.Button("一键绘制墙体", GUILayout.Height(28f)))
                PaintPerimeterWalls();
        }

        /// <summary>
        /// 绘制房间笔刷工具
        /// </summary>
        private void DrawRoomBrushTools()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "房间笔刷 = 矩形地面 + 四周一圈墙 + 可选门洞\n" +
                "左键点击网格放置单个房间 或使用下方按钮生成阵列",
                MessageType.Info);

            roomClearBeforeGenerate = EditorGUILayout.Toggle("生成前清空当前楼层", roomClearBeforeGenerate);
            roomAnchorGridPos = EditorGUILayout.Vector2IntField("房间左下角", roomAnchorGridPos);
            roomWidthGridCount = Mathf.Max(2, EditorGUILayout.IntField("房间宽度", roomWidthGridCount));
            roomDepthGridCount = Mathf.Max(2, EditorGUILayout.IntField("房间深度", roomDepthGridCount));
            wallThicknessGridCount = Mathf.Max(1, EditorGUILayout.IntField("墙体厚度", wallThicknessGridCount));
            wallExtendDirection = (EWallExtendDirection)EditorGUILayout.EnumPopup("延伸方向", wallExtendDirection);
            roomEnableDoor = EditorGUILayout.Toggle("生成门洞", roomEnableDoor);

            if (roomEnableDoor)
            {
                roomDoorWallSide = (ERoomDoorWallSide)EditorGUILayout.EnumPopup("门所在方向", roomDoorWallSide);
                roomDoorOffsetGridCount = Mathf.Max(0, EditorGUILayout.IntField("门沿墙偏移", roomDoorOffsetGridCount));
                roomDoorWidthGridCount = Mathf.Max(1, EditorGUILayout.IntField("房间门宽", roomDoorWidthGridCount));
                cutoutStartHeightGridCount = Mathf.Clamp(EditorGUILayout.IntField("挖空起点高度", cutoutStartHeightGridCount), 0, wallHeightGridCount - 1);
                cutoutEndHeightGridCount = Mathf.Clamp(EditorGUILayout.IntField("挖空终点高度", cutoutEndHeightGridCount), cutoutStartHeightGridCount + 1, wallHeightGridCount);
            }

            if (GUILayout.Button("在左下角生成单个房间", GUILayout.Height(28f)))
                GenerateSingleRoom();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("房间阵列", EditorStyles.boldLabel);
            roomGridRowCount = Mathf.Max(1, EditorGUILayout.IntField("行数", roomGridRowCount));
            roomGridColumnCount = Mathf.Max(1, EditorGUILayout.IntField("列数", roomGridColumnCount));
            EditorGUILayout.HelpBox("房间邻近间隔控制列方向横向距离 走廊宽度控制行方向对向距离", MessageType.None);
            roomAdjacentSpacingGridCount = Mathf.Max(0, EditorGUILayout.IntField("房间邻近间隔", roomAdjacentSpacingGridCount));
            roomCorridorWidthGridCount = Mathf.Max(1, EditorGUILayout.IntField("走廊宽度", roomCorridorWidthGridCount));
            roomGridDoorMode = (ERoomGridDoorMode)EditorGUILayout.EnumPopup("阵列门模式", roomGridDoorMode);

            if (roomGridDoorMode == ERoomGridDoorMode.Symmetric)
                EditorGUILayout.HelpBox("对称模式会按阵列中心镜像门方向和偏移", MessageType.None);

            if (roomGridDoorMode == ERoomGridDoorMode.Random)
                roomGridDoorRandomSeed = EditorGUILayout.IntField("门随机种子", roomGridDoorRandomSeed);

            if (GUILayout.Button("生成房间阵列", GUILayout.Height(28f)))
                GenerateRoomGrid();
        }

        /// <summary>
        /// 绘制优化面板
        /// </summary>
        private void DrawOptimizationPanel()
        {
            EditorGUILayout.LabelField("优化", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("请先生成到场景 再执行优化操作", MessageType.None);

            if (GUILayout.Button("全部合并网格", GUILayout.Height(28f)))
                RunMeshMerge(EBuildingMergeTarget.All);

            if (GUILayout.Button("合并地面网格", GUILayout.Height(28f)))
                RunMeshMerge(EBuildingMergeTarget.Floor);

            if (GUILayout.Button("合并墙体网格", GUILayout.Height(28f)))
                RunMeshMerge(EBuildingMergeTarget.Structure);

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("合并碰撞体", GUILayout.Height(28f)))
                RunCollisionMerge();

            if (GUILayout.Button("开启 GPU Instancing", GUILayout.Height(28f)))
                RunGpuInstancing();
        }

        /// <summary>
        /// 绘制视图设置
        /// </summary>
        private void DrawViewSettings()
        {
            EditorGUILayout.LabelField("视图", EditorStyles.boldLabel);
            cellPixelSize = EditorGUILayout.Slider("缩放", cellPixelSize, MinCellPixelSize, MaxCellPixelSize);
            if (GUILayout.Button("视图居中", GUILayout.Height(24f)))
                gridPanOffset = Vector2.zero;
        }

        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("生成", EditorStyles.boldLabel);
            if (GUILayout.Button("一键生成到场景", GUILayout.Height(34f)))
                GenerateBuilding();

            if (GUILayout.Button("清理场景生成物", GUILayout.Height(28f)))
                ClearGenerated();

            if (GUILayout.Button("保存蓝图资产", GUILayout.Height(24f)))
                SaveAssets();
        }

        /// <summary>
        /// 绘制内饰操作按钮
        /// </summary>
        private void DrawInteriorActionButtons()
        {
            EditorGUILayout.LabelField("内饰生成", EditorStyles.boldLabel);
            if (GUILayout.Button("生成内饰到场景", GUILayout.Height(34f)))
                GenerateInterior();

            if (GUILayout.Button("清理内饰场景生成物", GUILayout.Height(28f)))
                ClearInteriorGenerated();

            if (GUILayout.Button("清空当前楼层内饰", GUILayout.Height(28f)))
                ClearCurrentFloorInterior();

            if (GUILayout.Button("保存内饰布局", GUILayout.Height(24f)))
                SaveAssets();
        }

        /// <summary>
        /// 更新悬停格子
        /// </summary>
        private void UpdateHoverGridPos(Rect gridRect, Vector2 localMousePos)
        {
            hasHoverGridPos = gridRect.Contains(localMousePos);
            if (!hasHoverGridPos)
                return;

            hoverGridPos = WindowToGrid(localMousePos, gridRect);
            if (brushMode == EBrushMode.Interior)
                Repaint();
        }

        /// <summary>
        /// 绘制网格线
        /// </summary>
        private void DrawGridLines(Rect gridRect)
        {
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = new Color(1f, 1f, 1f, 0.13f);

            Vector2 center = GetGridCenter(gridRect);
            int minX = Mathf.FloorToInt((-center.x) / cellPixelSize) - 1;
            int maxX = Mathf.CeilToInt((gridRect.width - center.x) / cellPixelSize) + 1;
            int minZ = Mathf.FloorToInt((center.y - gridRect.height) / cellPixelSize) - 1;
            int maxZ = Mathf.CeilToInt(center.y / cellPixelSize) + 1;

            for (int x = minX; x <= maxX; x++)
            {
                float pixelX = center.x + x * cellPixelSize;
                Handles.DrawLine(new Vector3(pixelX, 0f), new Vector3(pixelX, gridRect.height));
            }

            for (int z = minZ; z <= maxZ; z++)
            {
                float pixelY = center.y - z * cellPixelSize;
                Handles.DrawLine(new Vector3(0f, pixelY), new Vector3(gridRect.width, pixelY));
            }

            Handles.color = new Color(1f, 0.2f, 0.2f, 0.55f);
            Handles.DrawLine(new Vector3(center.x, 0f), new Vector3(center.x, gridRect.height));
            Handles.DrawLine(new Vector3(0f, center.y), new Vector3(gridRect.width, center.y));
            Handles.color = oldColor;
            Handles.EndGUI();
        }

        /// <summary>
        /// 绘制已有格子
        /// </summary>
        private void DrawPaintedCells(Rect gridRect)
        {
            if (paintedPlan == null)
                return;

            var floorData = paintedPlan.FindFloor(currentFloorIndex);
            if (floorData == null)
                return;

            DrawCellList(floorData.floorCellDataList, gridRect, 0.62f);
            DrawCellList(floorData.structureCellDataList, gridRect, 0.82f);
        }

        /// <summary>
        /// 绘制内饰布局和悬停预览
        /// </summary>
        private void DrawInteriorHoverPreview(Rect gridRect)
        {
            if (brushMode != EBrushMode.Interior || interiorLayout == null)
                return;

            foreach (var placementData in interiorLayout.furniturePlacementDataList)
            {
                if (placementData == null
                    || placementData.floorIndex != currentFloorIndex
                    || placementData.brushPreset == null)
                    continue;

                DrawInteriorPlacementOverlay(
                    placementData.brushPreset,
                    placementData.anchorGridPos,
                    placementData.rotation,
                    gridRect,
                    placementData.locked ? 0.5f : 0.32f);
            }

            if (!hasHoverGridPos || currentInteriorFurnitureBrushPreset == null)
                return;

            DrawInteriorPlacementOverlay(
                currentInteriorFurnitureBrushPreset,
                hoverGridPos,
                currentInteriorRotation,
                gridRect,
                0.42f,
                true,
                false);
        }

        /// <summary>
        /// 绘制单个内饰占格预览
        /// </summary>
        private void DrawInteriorPlacementOverlay(
            InteriorFurnitureBrushPreset brushPreset,
            Vector2Int anchorGridPos,
            EInteriorFurnitureRotation rotation,
            Rect gridRect,
            float alpha,
            bool canPlace = true,
            bool validationReady = true)
        {
            if (!IsInteriorPlacementOverlayVisible(brushPreset, anchorGridPos, rotation, gridRect))
                return;

            Vector2Int footprintGridSize = InteriorFurniturePlacementUtility.GetRotatedFootprintGridSize(brushPreset, rotation);
            Color color = !validationReady
                ? brushPreset.previewColor
                : canPlace
                    ? brushPreset.previewColor
                    : new Color(1f, 0.15f, 0.15f, 1f);
            InteriorFurniturePlacementUtility.FillOccupiedGridPosList(
                brushPreset,
                anchorGridPos,
                rotation,
                interiorOccupiedGridPosList);

            foreach (var gridPos in interiorOccupiedGridPosList)
            {
                Rect cellRect = GridToWindowCellRect(gridPos, gridRect);
                EditorGUI.DrawRect(cellRect, new Color(color.r, color.g, color.b, alpha));
            }

            Rect minRect = GridToWindowCellRect(anchorGridPos, gridRect);
            Rect maxRect = GridToWindowCellRect(
                anchorGridPos + footprintGridSize - Vector2Int.one,
                gridRect);
            Rect footprintRect = Rect.MinMaxRect(
                minRect.xMin,
                maxRect.yMin,
                maxRect.xMax,
                minRect.yMax);

            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = new Color(color.r, color.g, color.b, 0.95f);
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(footprintRect.xMin, footprintRect.yMin),
                new Vector3(footprintRect.xMax, footprintRect.yMin),
                new Vector3(footprintRect.xMax, footprintRect.yMax),
                new Vector3(footprintRect.xMin, footprintRect.yMax),
                new Vector3(footprintRect.xMin, footprintRect.yMin));

            Vector2 forwardDirection = InteriorFurniturePlacementUtility.GetForwardGridDirection(rotation);
            Vector2 screenDirection = new Vector2(forwardDirection.x, -forwardDirection.y).normalized;
            Vector2 arrowCenter = footprintRect.center;
            float arrowLength = Mathf.Min(footprintRect.width, footprintRect.height) * 0.36f;
            Vector2 arrowTip = arrowCenter + screenDirection * arrowLength;
            Vector2 arrowBase = arrowCenter - screenDirection * arrowLength * 0.35f;
            Vector2 perpendicular = new Vector2(-screenDirection.y, screenDirection.x);
            Handles.DrawAAPolyLine(3f, arrowBase, arrowTip);
            Handles.DrawAAPolyLine(
                3f,
                arrowTip - screenDirection * arrowLength * 0.28f + perpendicular * arrowLength * 0.18f,
                arrowTip,
                arrowTip - screenDirection * arrowLength * 0.28f - perpendicular * arrowLength * 0.18f);
            Handles.color = oldColor;
            Handles.EndGUI();

        }

        /// <summary>
        /// 判断内饰预览是否在网格视口内
        /// </summary>
        private bool IsInteriorPlacementOverlayVisible(
            InteriorFurnitureBrushPreset brushPreset,
            Vector2Int anchorGridPos,
            EInteriorFurnitureRotation rotation,
            Rect gridRect)
        {
            Vector2Int footprintGridSize = InteriorFurniturePlacementUtility.GetRotatedFootprintGridSize(brushPreset, rotation);
            Rect minRect = GridToWindowCellRect(anchorGridPos, gridRect);
            Rect maxRect = GridToWindowCellRect(
                anchorGridPos + footprintGridSize - Vector2Int.one,
                gridRect);
            Rect footprintRect = Rect.MinMaxRect(
                minRect.xMin,
                maxRect.yMin,
                maxRect.xMax,
                minRect.yMax);
            return footprintRect.Overlaps(gridRect);
        }

        /// <summary>
        /// 校验内饰放置
        /// </summary>
        private bool IsInteriorPlacementValid(
            InteriorFurnitureBrushPreset brushPreset,
            int floorIndex,
            Vector2Int anchorGridPos,
            EInteriorFurnitureRotation rotation)
        {
            if (brushPreset == null
                || brushPreset.furniturePrefab == null
                || paintedPlan == null
                || interiorLayout == null)
                return false;

            var floorData = paintedPlan.FindFloor(floorIndex);
            if (floorData == null)
                return false;

            EnsureInteriorOccupancyCache();
            InteriorFurniturePlacementUtility.FillOccupiedGridPosList(
                brushPreset,
                anchorGridPos,
                rotation,
                interiorOccupiedGridPosList);
            foreach (var gridPos in interiorOccupiedGridPosList)
            {
                if (!interiorFloorGridHashList.Contains(gridPos)
                    || interiorStructureGridHashList.Contains(gridPos))
                    return false;

                if (interiorOccupiedGridHashList.Contains(gridPos))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 确保当前楼层内饰占用缓存
        /// </summary>
        private void EnsureInteriorOccupancyCache()
        {
            var floorData = paintedPlan == null
                ? null
                : paintedPlan.FindFloor(currentFloorIndex);
            int floorCellCount = floorData == null || floorData.floorCellDataList == null
                ? 0
                : floorData.floorCellDataList.Count;
            int structureCellCount = floorData == null || floorData.structureCellDataList == null
                ? 0
                : floorData.structureCellDataList.Count;
            int placementCount = interiorLayout == null || interiorLayout.furniturePlacementDataList == null
                ? 0
                : interiorLayout.furniturePlacementDataList.Count;
            if (!interiorOccupancyCacheDirty
                && cachedInteriorLayout == interiorLayout
                && cachedInteriorFloorIndex == currentFloorIndex
                && cachedInteriorPlacementCount == placementCount
                && cachedInteriorFloorCellCount == floorCellCount
                && cachedInteriorStructureCellCount == structureCellCount)
            {
                return;
            }

            interiorOccupiedGridHashList.Clear();
            interiorFloorGridHashList.Clear();
            interiorStructureGridHashList.Clear();
            if (floorData != null && floorData.floorCellDataList != null)
            {
                foreach (var cellData in floorData.floorCellDataList)
                {
                    if (cellData != null)
                        interiorFloorGridHashList.Add(cellData.gridPos);
                }

                if (floorData.structureCellDataList != null)
                {
                    foreach (var cellData in floorData.structureCellDataList)
                    {
                        if (cellData != null)
                            interiorStructureGridHashList.Add(cellData.gridPos);
                    }
                }
            }

            if (interiorLayout != null && interiorLayout.furniturePlacementDataList != null)
            {
                foreach (var placementData in interiorLayout.furniturePlacementDataList)
                {
                    if (placementData == null
                        || placementData.floorIndex != currentFloorIndex
                        || placementData.brushPreset == null)
                    {
                        continue;
                    }

                    InteriorFurniturePlacementUtility.FillOccupiedGridPosList(
                        placementData.brushPreset,
                        placementData.anchorGridPos,
                        placementData.rotation,
                        interiorOccupiedGridPosList);
                    foreach (var gridPos in interiorOccupiedGridPosList)
                        interiorOccupiedGridHashList.Add(gridPos);
                }
            }

            cachedInteriorLayout = interiorLayout;
            cachedInteriorFloorIndex = currentFloorIndex;
            cachedInteriorPlacementCount = placementCount;
            cachedInteriorFloorCellCount = floorCellCount;
            cachedInteriorStructureCellCount = structureCellCount;
            interiorOccupancyCacheDirty = false;
        }

        /// <summary>
        /// 标记内饰占用缓存失效
        /// </summary>
        private void InvalidateInteriorOccupancyCache()
        {
            interiorOccupancyCacheDirty = true;
        }

        /// <summary>
        /// 绘制格子列表
        /// </summary>
        private void DrawCellList(List<PaintedBuildingCellData> cellDataList, Rect gridRect, float alpha)
        {
            foreach (var cellData in cellDataList)
            {
                if (cellData == null)
                    continue;

                Rect cellRect = GridToWindowCellRect(cellData.gridPos, gridRect);
                if (!cellRect.Overlaps(gridRect))
                    continue;

                Color color = GetCellColor(cellData);
                EditorGUI.DrawRect(cellRect, new Color(color.r, color.g, color.b, alpha));
            }
        }

        /// <summary>
        /// 绘制房间悬停预览
        /// </summary>
        private void DrawRoomHoverPreview(Rect gridRect)
        {
            if (brushMode != EBrushMode.Building || currentCellType != EPaintedBuildingCellType.Room || !hasHoverGridPos)
                return;

            int safeWidth = Mathf.Max(2, roomWidthGridCount);
            int safeDepth = Mathf.Max(2, roomDepthGridCount);
            Vector2Int topRightGridPos = new Vector2Int(
                hoverGridPos.x + safeWidth - 1,
                hoverGridPos.y + safeDepth - 1);

            GetGridRectBounds(
                hoverGridPos,
                topRightGridPos,
                out int minX,
                out int maxX,
                out int minZ,
                out int maxZ);
            Rect minRect = GridToWindowCellRect(new Vector2Int(minX, maxZ), gridRect);
            Rect maxRect = GridToWindowCellRect(new Vector2Int(maxX, minZ), gridRect);
            Rect previewRect = Rect.MinMaxRect(minRect.xMin, minRect.yMin, maxRect.xMax, maxRect.yMax);
            Color roomColor = BuildingPainterColorUtility.GetCellColor(EPaintedBuildingCellType.Room);
            EditorGUI.DrawRect(previewRect, new Color(roomColor.r, roomColor.g, roomColor.b, 0.18f));

            var floorGridPosHashList = new HashSet<Vector2Int>();
            for (int x = hoverGridPos.x; x <= topRightGridPos.x; x++)
            {
                for (int z = hoverGridPos.y; z <= topRightGridPos.y; z++)
                    floorGridPosHashList.Add(new Vector2Int(x, z));
            }

            var wallGridPosHashList = BuildingPerimeterWallUtility.CalculateWallGridPosHashList(
                floorGridPosHashList,
                wallThicknessGridCount,
                wallExtendDirection);
            Color wallColor = BuildingPainterColorUtility.GetCellColor(EPaintedBuildingCellType.Wall);
            foreach (var wallGridPos in wallGridPosHashList)
            {
                Rect cellRect = GridToWindowCellRect(wallGridPos, gridRect);
                if (!cellRect.Overlaps(gridRect))
                    continue;

                EditorGUI.DrawRect(cellRect, new Color(wallColor.r, wallColor.g, wallColor.b, 0.55f));
            }

            if (roomEnableDoor)
            {
                BuildingRoomDoorUtility.TryCollectRoomDoorGridPosList(
                    wallGridPosHashList,
                    hoverGridPos,
                    safeWidth,
                    safeDepth,
                    roomDoorWallSide,
                    roomDoorOffsetGridCount,
                    roomDoorWidthGridCount,
                    out _,
                    out List<Vector2Int> doorGridPosList);
                Color doorColor = BuildingPainterColorUtility.GetCellColor(EPaintedBuildingCellType.Cutout);
                foreach (var doorGridPos in doorGridPosList)
                {
                    Rect cellRect = GridToWindowCellRect(doorGridPos, gridRect);
                    if (!cellRect.Overlaps(gridRect))
                        continue;

                    EditorGUI.DrawRect(cellRect, new Color(doorColor.r, doorColor.g, doorColor.b, 0.9f));
                }
            }

            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = new Color(roomColor.r, roomColor.g, roomColor.b, 0.95f);
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(previewRect.xMin, previewRect.yMin),
                new Vector3(previewRect.xMax, previewRect.yMin),
                new Vector3(previewRect.xMax, previewRect.yMax),
                new Vector3(previewRect.xMin, previewRect.yMax),
                new Vector3(previewRect.xMin, previewRect.yMin));
            Handles.color = oldColor;
            Handles.EndGUI();
        }

        /// <summary>
        /// 获取网格帮助信息
        /// </summary>
        private string GetGridHelpMessage()
        {
            if (brushMode == EBrushMode.Interior)
                return "左键刷入内饰 右键擦除 中键拖拽平移 滚轮缩放 R 键旋转";

            if (currentCellType == EPaintedBuildingCellType.CutoutFill)
                return "左键点击已有挖空格设置填充物 右键擦除 中键拖拽平移 滚轮缩放";

            if (currentCellType == EPaintedBuildingCellType.Room)
                return "左键点击网格放置房间 右键擦除 中键拖拽平移 滚轮缩放";

            return "在中间网格左键绘制 右键擦除 中键拖拽平移 滚轮缩放";
        }

        /// <summary>
        /// 绘制悬停格子
        /// </summary>
        private void DrawHoverCell(Rect gridRect)
        {
            if (brushMode == EBrushMode.Interior || !hasHoverGridPos)
                return;

            if (currentCellType == EPaintedBuildingCellType.Room)
                return;

            Rect cellRect = GridToWindowCellRect(hoverGridPos, gridRect);
            Color color = GetCurrentBrushColor();
            EditorGUI.DrawRect(cellRect, new Color(color.r, color.g, color.b, 0.35f));
        }

        /// <summary>
        /// 绘制框选区域
        /// </summary>
        private void DrawSelectionRect(Rect gridRect)
        {
            if (!isSelectingCells)
                return;

            GetGridRectBounds(selectionStartGridPos, selectionEndGridPos, out int minX, out int maxX, out int minZ, out int maxZ);
            Rect minRect = GridToWindowCellRect(new Vector2Int(minX, maxZ), gridRect);
            Rect maxRect = GridToWindowCellRect(new Vector2Int(maxX, minZ), gridRect);
            Rect selectionRect = Rect.MinMaxRect(minRect.xMin, minRect.yMin, maxRect.xMax, maxRect.yMax);
            Color color = GetCurrentBrushColor();
            EditorGUI.DrawRect(selectionRect, new Color(color.r, color.g, color.b, 0.18f));
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = new Color(color.r, color.g, color.b, 1f);
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(selectionRect.xMin, selectionRect.yMin),
                new Vector3(selectionRect.xMax, selectionRect.yMin),
                new Vector3(selectionRect.xMax, selectionRect.yMax),
                new Vector3(selectionRect.xMin, selectionRect.yMax),
                new Vector3(selectionRect.xMin, selectionRect.yMin));
            Handles.color = oldColor;
            Handles.EndGUI();
        }

        /// <summary>
        /// 处理网格输入
        /// </summary>
        private void HandleGridInput(Rect gridRect, Event e)
        {
            if (!gridRect.Contains(e.mousePosition)
                && !isSelectingCells
                && !isPanning
                && !isPaintingInterior
                && !isErasingInterior)
                return;

            if (e.type == EventType.ScrollWheel)
            {
                float oldCellSize = cellPixelSize;
                cellPixelSize = Mathf.Clamp(cellPixelSize - e.delta.y, MinCellPixelSize, MaxCellPixelSize);
                Vector2 pivot = e.mousePosition - GetGridCenter(gridRect);
                if (!Mathf.Approximately(oldCellSize, cellPixelSize))
                    gridPanOffset -= pivot * (cellPixelSize / oldCellSize - 1f);
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 2)
            {
                isPanning = true;
                lastMousePos = e.mousePosition;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDrag && isPanning)
            {
                gridPanOffset += e.mousePosition - lastMousePos;
                lastMousePos = e.mousePosition;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 2)
            {
                isPanning = false;
                e.Use();
                return;
            }

            if (brushMode == EBrushMode.Interior)
            {
                HandleInteriorGridInput(e);
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (currentCellType == EPaintedBuildingCellType.Room)
                {
                    if (hasHoverGridPos)
                    {
                        roomAnchorGridPos = hoverGridPos;
                        GenerateSingleRoom();
                    }

                    e.Use();
                    return;
                }

                isSelectingCells = true;
                selectionStartGridPos = hoverGridPos;
                selectionEndGridPos = hoverGridPos;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && isSelectingCells)
            {
                selectionEndGridPos = hoverGridPos;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 0 && isSelectingCells)
            {
                selectionEndGridPos = hoverGridPos;
                PaintRect(selectionStartGridPos, selectionEndGridPos);
                isSelectingCells = false;
                hasDirtyPaintData = false;
                SaveAssets();
                e.Use();
                return;
            }

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1)
            {
                EraseCell();
                e.Use();
            }

            if (e.type == EventType.MouseUp && e.button == 1 && hasDirtyPaintData)
            {
                hasDirtyPaintData = false;
                SaveAssets();
                e.Use();
            }
        }

        /// <summary>
        /// 处理内饰网格输入
        /// </summary>
        private void HandleInteriorGridInput(Event e)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                isPaintingInterior = true;
                isErasingInterior = false;
                hasLastInteriorPaintGridPos = false;
                hasInteriorPaintChanged = false;
                hasInteriorUndoRecord = false;
                PaintInteriorAtHover();
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && isPaintingInterior)
            {
                PaintInteriorAtHoverLine();
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 0 && isPaintingInterior)
            {
                FinishInteriorStroke();
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 1)
            {
                isPaintingInterior = false;
                isErasingInterior = true;
                hasLastInteriorPaintGridPos = false;
                hasInteriorPaintChanged = false;
                hasInteriorUndoRecord = false;
                EraseInteriorAtHover();
                if (hasHoverGridPos)
                {
                    lastInteriorPaintGridPos = hoverGridPos;
                    hasLastInteriorPaintGridPos = true;
                }
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDrag && e.button == 1 && isErasingInterior)
            {
                EraseInteriorAtHoverLine();
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 1 && isErasingInterior)
            {
                FinishInteriorStroke();
                e.Use();
            }
        }

        /// <summary>
        /// 在悬停格刷入内饰
        /// </summary>
        private void PaintInteriorAtHover()
        {
            if (!hasHoverGridPos)
                return;

            PaintInteriorAtGridPos(hoverGridPos);
            lastInteriorPaintGridPos = hoverGridPos;
            hasLastInteriorPaintGridPos = true;
        }

        /// <summary>
        /// 沿悬停格路径刷入内饰
        /// </summary>
        private void PaintInteriorAtHoverLine()
        {
            if (!hasHoverGridPos)
                return;

            if (!hasLastInteriorPaintGridPos)
            {
                PaintInteriorAtHover();
                return;
            }

            PaintInteriorAlongGridLine(lastInteriorPaintGridPos, hoverGridPos);
            lastInteriorPaintGridPos = hoverGridPos;
        }

        /// <summary>
        /// 沿网格线刷入内饰
        /// </summary>
        private void PaintInteriorAlongGridLine(Vector2Int startGridPos, Vector2Int endGridPos)
        {
            int stepCount = Mathf.Max(
                Mathf.Abs(endGridPos.x - startGridPos.x),
                Mathf.Abs(endGridPos.y - startGridPos.y));
            if (stepCount <= 0)
                return;

            for (int i = 1; i <= stepCount; i++)
            {
                float progress = i / (float)stepCount;
                var gridPos = new Vector2Int(
                    Mathf.RoundToInt(Mathf.Lerp(startGridPos.x, endGridPos.x, progress)),
                    Mathf.RoundToInt(Mathf.Lerp(startGridPos.y, endGridPos.y, progress)));
                PaintInteriorAtGridPos(gridPos);
            }
        }

        /// <summary>
        /// 在目标格刷入一个内饰
        /// </summary>
        private void PaintInteriorAtGridPos(Vector2Int gridPos)
        {
            if (currentInteriorFurnitureBrushPreset == null
                || interiorLayout == null
                || !IsInteriorPlacementValid(
                    currentInteriorFurnitureBrushPreset,
                    currentFloorIndex,
                    gridPos,
                    currentInteriorRotation))
            {
                return;
            }

            if (!hasInteriorUndoRecord)
            {
                Undo.RecordObject(interiorLayout, "刷入内饰");
                hasInteriorUndoRecord = true;
            }

            interiorLayout.AddPlacement(new InteriorFurniturePlacementData
            {
                brushPreset = currentInteriorFurnitureBrushPreset,
                floorIndex = currentFloorIndex,
                anchorGridPos = gridPos,
                rotation = currentInteriorRotation,
                prefabRotationOffset = currentInteriorFurnitureBrushPreset.prefabRotationOffset
            });
            EditorUtility.SetDirty(interiorLayout);
            foreach (var occupiedGridPos in interiorOccupiedGridPosList)
                interiorOccupiedGridHashList.Add(occupiedGridPos);
            cachedInteriorPlacementCount = interiorLayout.furniturePlacementDataList.Count;
            hasInteriorPaintChanged = true;
        }

        /// <summary>
        /// 沿悬停格路径擦除内饰
        /// </summary>
        private void EraseInteriorAtHoverLine()
        {
            if (!hasHoverGridPos)
                return;

            if (!hasLastInteriorPaintGridPos)
            {
                EraseInteriorAtHover();
                lastInteriorPaintGridPos = hoverGridPos;
                hasLastInteriorPaintGridPos = true;
                return;
            }

            EraseInteriorAlongGridLine(lastInteriorPaintGridPos, hoverGridPos);
            lastInteriorPaintGridPos = hoverGridPos;
        }

        /// <summary>
        /// 沿网格线擦除内饰
        /// </summary>
        private void EraseInteriorAlongGridLine(Vector2Int startGridPos, Vector2Int endGridPos)
        {
            int stepCount = Mathf.Max(
                Mathf.Abs(endGridPos.x - startGridPos.x),
                Mathf.Abs(endGridPos.y - startGridPos.y));
            if (stepCount <= 0)
                return;

            for (int i = 1; i <= stepCount; i++)
            {
                float progress = i / (float)stepCount;
                var gridPos = new Vector2Int(
                    Mathf.RoundToInt(Mathf.Lerp(startGridPos.x, endGridPos.x, progress)),
                    Mathf.RoundToInt(Mathf.Lerp(startGridPos.y, endGridPos.y, progress)));
                EraseInteriorAtGridPos(gridPos);
            }
        }

        /// <summary>
        /// 结束内饰拖动
        /// </summary>
        private void FinishInteriorStroke()
        {
            isPaintingInterior = false;
            isErasingInterior = false;
            hasLastInteriorPaintGridPos = false;
            hasInteriorUndoRecord = false;
            if (hasInteriorPaintChanged)
                SaveAssets();

            hasInteriorPaintChanged = false;
            Repaint();
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown)
                return;

            if (brushMode == EBrushMode.Interior && e.keyCode == KeyCode.R)
            {
                RotateInteriorBrush(e.shift ? -1 : 1);
                e.Use();
                Repaint();
                return;
            }

            if (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.KeypadPlus)
            {
                cellPixelSize = Mathf.Clamp(cellPixelSize + 2f, MinCellPixelSize, MaxCellPixelSize);
                e.Use();
                Repaint();
            }

            if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus)
            {
                cellPixelSize = Mathf.Clamp(cellPixelSize - 2f, MinCellPixelSize, MaxCellPixelSize);
                e.Use();
                Repaint();
            }
        }

        /// <summary>
        /// 处理分隔条输入
        /// </summary>
        private void HandleSplitterInput(Rect windowRect, Rect leftSplitterRect, Rect rightSplitterRect)
        {
            Event e = Event.current;
            EditorGUIUtility.AddCursorRect(leftSplitterRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightSplitterRect, MouseCursor.ResizeHorizontal);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (leftSplitterRect.Contains(e.mousePosition))
                {
                    isDraggingLeftSplitter = true;
                    e.Use();
                }

                if (rightSplitterRect.Contains(e.mousePosition))
                {
                    isDraggingRightSplitter = true;
                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag && isDraggingLeftSplitter)
            {
                leftPanelWidth = Mathf.Clamp(e.mousePosition.x, MinSideWidth, GetMaxPanelWidth(windowRect.width));
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseDrag && isDraggingRightSplitter)
            {
                rightPanelWidth = Mathf.Clamp(windowRect.width - e.mousePosition.x, MinSideWidth, GetMaxPanelWidth(windowRect.width));
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp)
            {
                isDraggingLeftSplitter = false;
                isDraggingRightSplitter = false;
                EditorPrefs.SetFloat(LeftWidthPrefsKey, leftPanelWidth);
                EditorPrefs.SetFloat(RightWidthPrefsKey, rightPanelWidth);
            }
        }

        /// <summary>
        /// 限制面板宽度
        /// </summary>
        private void ClampPanelWidths(float windowWidth)
        {
            float maxPanelWidth = GetMaxPanelWidth(windowWidth);
            leftPanelWidth = Mathf.Clamp(leftPanelWidth, MinSideWidth, maxPanelWidth);
            rightPanelWidth = Mathf.Clamp(rightPanelWidth, MinSideWidth, maxPanelWidth);
        }

        /// <summary>
        /// 获取最大面板宽度
        /// </summary>
        private float GetMaxPanelWidth(float windowWidth)
        {
            return Mathf.Min(MaxSideWidth, Mathf.Max(MinSideWidth, (windowWidth - 280f) * 0.5f));
        }

        /// <summary>
        /// 绘制格子
        /// </summary>
        private void PaintCell()
        {
            if (paintedPlan == null)
                return;

            PaintCell(hoverGridPos);
        }

        /// <summary>
        /// 绘制格子
        /// </summary>
        private void PaintCell(Vector2Int gridPos)
        {
            if (paintedPlan == null || currentCellType == EPaintedBuildingCellType.Room)
                return;

            PaintedBuildingBrushPreset paintBrushPreset = ResolveBrushPreset(currentCellType);
            if (currentCellType == EPaintedBuildingCellType.CutoutFill)
            {
                var cutoutFloorData = paintedPlan.FindFloor(currentFloorIndex);
                if (cutoutFloorData == null)
                    return;

                PaintCutoutFill(cutoutFloorData, gridPos, paintBrushPreset);
                return;
            }

            PaintedBuildingFloorData floorData = paintedPlan.GetOrCreateFloor(currentFloorIndex);
            PaintedBuildingCellData oldCellData = currentCellType == EPaintedBuildingCellType.Floor
                ? floorData.FindFloorCell(gridPos)
                : floorData.FindStructureCell(gridPos);
            if (oldCellData != null
                && oldCellData.cellType == currentCellType
                && oldCellData.brushPreset == paintBrushPreset
                && oldCellData.heightGridCount == wallHeightGridCount
                && oldCellData.cutoutStartHeightGridCount == cutoutStartHeightGridCount
                && oldCellData.cutoutEndHeightGridCount == cutoutEndHeightGridCount)
                return;

            paintedPlan.SetCell(
                currentFloorIndex,
                gridPos,
                currentCellType,
                wallHeightGridCount,
                cutoutStartHeightGridCount,
                cutoutEndHeightGridCount,
                paintBrushPreset);
            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            hasDirtyPaintData = true;
            Repaint();
        }

        /// <summary>
        /// 设置挖空填充物
        /// </summary>
        private void PaintCutoutFill(
            PaintedBuildingFloorData floorData,
            Vector2Int gridPos,
            PaintedBuildingBrushPreset fillBrushPreset)
        {
            var cellData = floorData.FindStructureCell(gridPos);
            if (cellData == null
                || cellData.cellType != EPaintedBuildingCellType.Cutout
                || cellData.brushPreset == fillBrushPreset)
            {
                return;
            }

            Undo.RecordObject(paintedPlan, "设置挖空填充物");
            cellData.brushPreset = fillBrushPreset;
            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            hasDirtyPaintData = true;
            Repaint();
        }

        /// <summary>
        /// 框选绘制
        /// </summary>
        private void PaintRect(Vector2Int startGridPos, Vector2Int endGridPos)
        {
            if (paintedPlan == null)
                return;

            Undo.RecordObject(paintedPlan, "框选绘制建筑格子");
            GetGridRectBounds(startGridPos, endGridPos, out int minX, out int maxX, out int minZ, out int maxZ);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    PaintCell(new Vector2Int(x, z));
                }
            }
        }

        /// <summary>
        /// 擦除格子
        /// </summary>
        private void EraseCell()
        {
            if (paintedPlan == null)
                return;

            PaintedBuildingFloorData floorData = paintedPlan.FindFloor(currentFloorIndex);
            if (floorData == null
                || (floorData.FindStructureCell(hoverGridPos) == null && floorData.FindFloorCell(hoverGridPos) == null))
                return;

            Undo.RecordObject(paintedPlan, "擦除建筑格子");
            paintedPlan.RemoveTopCell(currentFloorIndex, hoverGridPos);
            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            hasDirtyPaintData = true;
            Repaint();
        }

        /// <summary>
        /// 清空当前楼层网格
        /// </summary>
        private void ClearCurrentFloorGrid()
        {
            if (paintedPlan == null)
                return;

            if (!EditorUtility.DisplayDialog(
                    "清空网格",
                    $"确定清空楼层 {currentFloorIndex} 的全部地面和结构格子吗",
                    "清空",
                    "取消"))
                return;

            Undo.RecordObject(paintedPlan, "清空当前楼层网格");
            paintedPlan.ClearFloor(currentFloorIndex);
            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            if (interiorLayout != null)
            {
                Undo.RecordObject(interiorLayout, "清空当前楼层内饰");
                interiorLayout.ClearFloor(currentFloorIndex);
                EditorUtility.SetDirty(interiorLayout);
                InvalidateInteriorOccupancyCache();
            }
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 复制上一层布局
        /// </summary>
        private void CopyPreviousFloorLayout()
        {
            if (paintedPlan == null || currentFloorIndex <= 0)
                return;

            int sourceFloorIndex = currentFloorIndex - 1;
            var sourceFloorData = paintedPlan.FindFloor(sourceFloorIndex);
            if (sourceFloorData == null
                || (sourceFloorData.floorCellDataList.Count == 0 && sourceFloorData.structureCellDataList.Count == 0))
            {
                EditorUtility.DisplayDialog("复制布局", $"楼层 {sourceFloorIndex} 没有可复制的布局", "确定");
                return;
            }

            Undo.RecordObject(paintedPlan, "复制上一层布局");
            bool copied = paintedPlan.CopyFloorLayout(sourceFloorIndex, currentFloorIndex);
            if (!copied)
            {
                EditorUtility.DisplayDialog("复制布局", "复制失败", "确定");
                return;
            }

            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 填充地面范围
        /// </summary>
        private void FillFloorRange()
        {
            if (paintedPlan == null)
                return;

            Undo.RecordObject(paintedPlan, "填充地面范围");
            paintedPlan.FillFloorRect(
                currentFloorIndex,
                floorFillBottomLeftGridPos,
                floorFillTopRightGridPos,
                ResolveBrushPreset(EPaintedBuildingCellType.Floor));
            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 一键绘制圈墙
        /// </summary>
        private void PaintPerimeterWalls()
        {
            if (paintedPlan == null)
                return;

            var floorData = paintedPlan.FindFloor(currentFloorIndex);
            if (floorData == null || floorData.floorCellDataList.Count == 0)
            {
                EditorUtility.DisplayDialog("一键圈墙", "当前楼层没有地面 请先绘制地面", "确定");
                return;
            }

            var floorGridPosHashList = new HashSet<Vector2Int>();
            foreach (var cellData in floorData.floorCellDataList)
            {
                if (cellData == null)
                    continue;

                floorGridPosHashList.Add(cellData.gridPos);
            }

            var wallGridPosHashList = BuildingPerimeterWallUtility.CalculateWallGridPosHashList(
                floorGridPosHashList,
                wallThicknessGridCount,
                wallExtendDirection);
            if (wallGridPosHashList.Count == 0)
            {
                EditorUtility.DisplayDialog("一键圈墙", "未能计算出墙体范围", "确定");
                return;
            }

            Undo.RecordObject(paintedPlan, "一键绘制圈墙");
            paintedPlan.SetWallCells(
                currentFloorIndex,
                wallGridPosHashList,
                wallHeightGridCount,
                ResolveBrushPreset(EPaintedBuildingCellType.Wall));
            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 确保格子公约引用
        /// </summary>
        private void EnsureConventionReference()
        {
            if (generator != null && generator.convention != null)
            {
                convention = generator.convention;
                return;
            }

            if (convention == null)
                convention = LoadOrCreateAsset<BuildingGridConvention>(ConventionAssetPath);

            if (generator != null && generator.convention == null)
            {
                generator.convention = convention;
                EditorUtility.SetDirty(generator);
            }
        }

        /// <summary>
        /// 同步墙体厚度
        /// </summary>
        private void SyncWallThicknessFromConvention()
        {
            if (convention == null)
                return;

            wallThicknessGridCount = Mathf.Max(1, convention.WallThicknessGridCount);
        }

        /// <summary>
        /// 生成单个程序化房间
        /// </summary>
        private void GenerateSingleRoom()
        {
            if (paintedPlan == null)
                return;

            Undo.RecordObject(paintedPlan, "生成单个房间");
            if (roomClearBeforeGenerate)
            {
                paintedPlan.ClearFloor(currentFloorIndex);
                ClearCurrentFloorInteriorData();
            }

            var config = BuildSingleRoomConfig();
            int paintedCellCount = BuildingRoomGenerator.GenerateSingleRoom(paintedPlan, currentFloorIndex, config);
            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            SaveAssets();
            Repaint();

            if (paintedCellCount <= 0)
                EditorUtility.DisplayDialog("房间笔刷", "生成失败 请检查参数", "确定");
        }

        /// <summary>
        /// 生成房间阵列
        /// </summary>
        private void GenerateRoomGrid()
        {
            if (paintedPlan == null)
                return;

            Undo.RecordObject(paintedPlan, "生成房间阵列");
            if (roomClearBeforeGenerate)
            {
                paintedPlan.ClearFloor(currentFloorIndex);
                ClearCurrentFloorInteriorData();
            }

            var config = BuildRoomGridConfig();
            int paintedCellCount = BuildingRoomGenerator.GenerateRoomGrid(paintedPlan, currentFloorIndex, config);
            EditorUtility.SetDirty(paintedPlan);
            InvalidateInteriorOccupancyCache();
            SaveAssets();
            Repaint();

            if (paintedCellCount <= 0)
                EditorUtility.DisplayDialog("房间笔刷", "生成失败 请检查参数", "确定");
        }

        /// <summary>
        /// 构建单个房间配置
        /// </summary>
        private BuildingSingleRoomConfig BuildSingleRoomConfig()
        {
            return new BuildingSingleRoomConfig
            {
                anchorGridPos = roomAnchorGridPos,
                widthGridCount = roomWidthGridCount,
                depthGridCount = roomDepthGridCount,
                wallThicknessGridCount = wallThicknessGridCount,
                wallHeightGridCount = wallHeightGridCount,
                wallExtendDirection = wallExtendDirection,
                enableDoor = roomEnableDoor,
                doorWallSide = roomDoorWallSide,
                doorOffsetGridCount = roomDoorOffsetGridCount,
                roomDoorWidthGridCount = roomDoorWidthGridCount,
                cutoutStartHeightGridCount = cutoutStartHeightGridCount,
                cutoutEndHeightGridCount = cutoutEndHeightGridCount,
                floorBrushPreset = ResolveRoomFloorBrushPreset(),
                wallBrushPreset = ResolveRoomWallBrushPreset()
            };
        }

        /// <summary>
        /// 构建房间阵列配置
        /// </summary>
        private BuildingRoomGridConfig BuildRoomGridConfig()
        {
            return new BuildingRoomGridConfig
            {
                anchorGridPos = roomAnchorGridPos,
                roomWidthGridCount = roomWidthGridCount,
                roomDepthGridCount = roomDepthGridCount,
                rowCount = roomGridRowCount,
                columnCount = roomGridColumnCount,
                roomAdjacentSpacingGridCount = roomAdjacentSpacingGridCount,
                corridorWidthGridCount = roomCorridorWidthGridCount,
                wallThicknessGridCount = wallThicknessGridCount,
                wallHeightGridCount = wallHeightGridCount,
                wallExtendDirection = wallExtendDirection,
                enableDoorPerRoom = roomEnableDoor,
                doorWallSide = roomDoorWallSide,
                doorOffsetGridCount = roomDoorOffsetGridCount,
                roomDoorWidthGridCount = roomDoorWidthGridCount,
                cutoutStartHeightGridCount = cutoutStartHeightGridCount,
                cutoutEndHeightGridCount = cutoutEndHeightGridCount,
                gridDoorMode = roomGridDoorMode,
                gridDoorRandomSeed = roomGridDoorRandomSeed,
                floorBrushPreset = ResolveRoomFloorBrushPreset(),
                wallBrushPreset = ResolveRoomWallBrushPreset()
            };
        }

        /// <summary>
        /// 房间墙体笔刷 有指定材质才用子笔刷 否则默认主墙体
        /// </summary>
        private PaintedBuildingBrushPreset ResolveRoomWallBrushPreset()
        {
            if (lastWallBrushPreset != null && lastWallBrushPreset.material != null)
                return lastWallBrushPreset;

            return FindBrushPreset(EPaintedBuildingCellType.Wall);
        }

        /// <summary>
        /// 房间地面笔刷
        /// </summary>
        private PaintedBuildingBrushPreset ResolveRoomFloorBrushPreset()
        {
            if (lastFloorBrushPreset != null && lastFloorBrushPreset.material != null)
                return lastFloorBrushPreset;

            return FindBrushPreset(EPaintedBuildingCellType.Floor);
        }

        /// <summary>
        /// 生成建筑
        /// </summary>
        private void GenerateBuilding()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "生成绘制建筑");
            generator.GenerateBuilding();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();
            SaveAssets();
        }

        /// <summary>
        /// 清理生成物
        /// </summary>
        private void ClearGenerated()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "清理绘制建筑");
            generator.ClearGenerated();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();
        }

        /// <summary>
        /// 生成内饰
        /// </summary>
        private void GenerateInterior()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "生成绘制内饰");
            generator.GenerateInterior();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();
            SaveAssets();
        }

        /// <summary>
        /// 清理内饰生成物
        /// </summary>
        private void ClearInteriorGenerated()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "清理绘制内饰");
            generator.ClearInteriorGenerated();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();
        }

        /// <summary>
        /// 清空当前楼层内饰
        /// </summary>
        private void ClearCurrentFloorInterior()
        {
            if (interiorLayout == null)
                return;

            ClearCurrentFloorInteriorData();
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 清理当前楼层内饰数据
        /// </summary>
        private void ClearCurrentFloorInteriorData()
        {
            if (interiorLayout == null)
                return;

            Undo.RecordObject(interiorLayout, "清空当前楼层内饰");
            interiorLayout.ClearFloor(currentFloorIndex);
            EditorUtility.SetDirty(interiorLayout);
            InvalidateInteriorOccupancyCache();
        }

        /// <summary>
        /// 执行网格合并
        /// </summary>
        private void RunMeshMerge(EBuildingMergeTarget mergeTarget)
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            if (generator.transform.Find("__PaintedBuildingGenerated") == null)
            {
                EditorUtility.DisplayDialog("合并网格", "请先生成到场景", "确定");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "合并渲染网格");
            int mergedLayerCount = generator.MergeRenderMeshes(mergeTarget);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();

            if (mergedLayerCount > 0)
                Debug.Log($"[BuildingPainterWindow] 已合并 {mergedLayerCount} 个图层网格");
            else
                EditorUtility.DisplayDialog("合并网格", "没有可合并的渲染网格", "确定");
        }

        /// <summary>
        /// 执行碰撞合并
        /// </summary>
        private void RunCollisionMerge()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            if (generator.transform.Find("__PaintedBuildingGenerated") == null)
            {
                EditorUtility.DisplayDialog("合并碰撞体", "请先生成到场景", "确定");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "合并碰撞体");
            int colliderCount = generator.MergeCollisionBoxes();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();

            if (colliderCount > 0)
                Debug.Log($"[BuildingPainterWindow] 已生成 {colliderCount} 个合并碰撞盒");
            else
                EditorUtility.DisplayDialog("合并碰撞体", "没有可合并的碰撞体", "确定");
        }

        /// <summary>
        /// 执行 GPU Instancing
        /// </summary>
        private void RunGpuInstancing()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            var generatedRoot = generator.transform.Find("__PaintedBuildingGenerated");
            if (generatedRoot == null)
            {
                EditorUtility.DisplayDialog("GPU Instancing", "请先生成到场景", "确定");
                return;
            }

            var materialHashList = new System.Collections.Generic.HashSet<Material>();
            foreach (var meshRenderer in generatedRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    if (material != null)
                        materialHashList.Add(material);
                }
            }

            foreach (var material in materialHashList)
                Undo.RecordObject(material, "开启 GPU Instancing");

            BuildingGpuInstancingEditorUtility.EnableWithDialog(generatedRoot);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();
        }

        /// <summary>
        /// 保存资产
        /// </summary>
        private void SaveAssets()
        {
            if (paintedPlan != null)
                EditorUtility.SetDirty(paintedPlan);

            if (interiorLayout != null)
                EditorUtility.SetDirty(interiorLayout);

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 选择笔刷
        /// </summary>
        private void SelectBrush(PaintedBuildingBrushPreset brushPreset)
        {
            currentBrushPreset = brushPreset;
            if (brushPreset != null && brushPreset.cellType == EPaintedBuildingCellType.Wall)
                lastWallBrushPreset = brushPreset;
            if (brushPreset != null && brushPreset.cellType == EPaintedBuildingCellType.Floor)
                lastFloorBrushPreset = brushPreset;

            ApplyBrushPreset(brushPreset);
            Repaint();
        }

        /// <summary>
        /// 解析绘制用笔刷
        /// </summary>
        private PaintedBuildingBrushPreset ResolveBrushPreset(EPaintedBuildingCellType cellType)
        {
            if (currentBrushPreset != null && currentBrushPreset.cellType == cellType)
                return currentBrushPreset;

            if (cellType == EPaintedBuildingCellType.Wall && lastWallBrushPreset != null)
                return lastWallBrushPreset;

            if (cellType == EPaintedBuildingCellType.Floor && lastFloorBrushPreset != null)
                return lastFloorBrushPreset;

            return FindBrushPreset(cellType);
        }

        /// <summary>
        /// 选择内饰笔刷
        /// </summary>
        private void SelectInteriorBrush(InteriorFurnitureBrushPreset brushPreset)
        {
            currentInteriorFurnitureBrushPreset = brushPreset;
            if (brushPreset != null)
            {
                currentInteriorRotation = brushPreset.defaultRotation;
                observedInteriorFurnitureBrushPreset = brushPreset;
                observedInteriorDefaultRotation = brushPreset.defaultRotation;
            }
            else
            {
                observedInteriorFurnitureBrushPreset = null;
            }

            Repaint();
        }

        /// <summary>
        /// 同步内饰笔刷默认朝向
        /// </summary>
        private void SyncCurrentInteriorRotationFromBrush()
        {
            if (currentInteriorFurnitureBrushPreset == null)
            {
                observedInteriorFurnitureBrushPreset = null;
                return;
            }

            if (observedInteriorFurnitureBrushPreset == currentInteriorFurnitureBrushPreset
                && observedInteriorDefaultRotation == currentInteriorFurnitureBrushPreset.defaultRotation)
            {
                return;
            }

            currentInteriorRotation = currentInteriorFurnitureBrushPreset.defaultRotation;
            observedInteriorFurnitureBrushPreset = currentInteriorFurnitureBrushPreset;
            observedInteriorDefaultRotation = currentInteriorFurnitureBrushPreset.defaultRotation;
        }

        /// <summary>
        /// 旋转内饰笔刷
        /// </summary>
        private void RotateInteriorBrush(int step)
        {
            int rotationStep = ((int)currentInteriorRotation + step) % 4;
            if (rotationStep < 0)
                rotationStep += 4;

            currentInteriorRotation = (EInteriorFurnitureRotation)rotationStep;
            Repaint();
        }

        /// <summary>
        /// 擦除悬停位置内饰
        /// </summary>
        private void EraseInteriorAtHover()
        {
            if (!hasHoverGridPos)
                return;

            EraseInteriorAtGridPos(hoverGridPos);
        }

        /// <summary>
        /// 擦除目标格内饰
        /// </summary>
        private void EraseInteriorAtGridPos(Vector2Int gridPos)
        {
            if (interiorLayout == null)
                return;

            EnsureInteriorOccupancyCache();
            int placementIndex = interiorLayout.FindPlacementIndexAt(currentFloorIndex, gridPos);
            if (placementIndex < 0)
                return;

            var placementData = interiorLayout.furniturePlacementDataList[placementIndex];
            if (placementData != null && placementData.locked)
                return;

            if (placementData != null)
            {
                InteriorFurniturePlacementUtility.FillOccupiedGridPosList(
                    placementData.brushPreset,
                    placementData.anchorGridPos,
                    placementData.rotation,
                    interiorOccupiedGridPosList);
                foreach (var occupiedGridPos in interiorOccupiedGridPosList)
                    interiorOccupiedGridHashList.Remove(occupiedGridPos);
            }

            if (!hasInteriorUndoRecord)
            {
                Undo.RecordObject(interiorLayout, "擦除内饰");
                hasInteriorUndoRecord = true;
            }

            interiorLayout.RemovePlacementAt(placementIndex);
            EditorUtility.SetDirty(interiorLayout);
            cachedInteriorPlacementCount = interiorLayout.furniturePlacementDataList.Count;
            hasInteriorPaintChanged = true;
        }

        /// <summary>
        /// 应用笔刷预设
        /// </summary>
        private void ApplyBrushPreset(PaintedBuildingBrushPreset brushPreset)
        {
            if (brushPreset == null)
                return;

            currentCellType = brushPreset.cellType;
            cutoutStartHeightGridCount = Mathf.Clamp(cutoutStartHeightGridCount, 0, wallHeightGridCount - 1);
            cutoutEndHeightGridCount = Mathf.Clamp(cutoutEndHeightGridCount, cutoutStartHeightGridCount + 1, wallHeightGridCount);
        }

        /// <summary>
        /// 同步蓝图全局设置
        /// </summary>
        private void SyncGlobalSettingsFromPlan()
        {
            if (paintedPlan == null)
                return;

            wallHeightGridCount = paintedPlan.GlobalWallHeightGridCount;
            cutoutStartHeightGridCount = Mathf.Clamp(cutoutStartHeightGridCount, 0, wallHeightGridCount - 1);
            cutoutEndHeightGridCount = Mathf.Clamp(cutoutEndHeightGridCount, cutoutStartHeightGridCount + 1, wallHeightGridCount);
        }

        /// <summary>
        /// 同步生成器引用
        /// </summary>
        private void SyncGeneratorReferences()
        {
            if (generator == null)
                return;

            if (interiorLayout != null && interiorLayout.paintedBuildingPlan != paintedPlan)
            {
                interiorLayout.paintedBuildingPlan = paintedPlan;
                EditorUtility.SetDirty(interiorLayout);
            }

            generator.paintedPlan = paintedPlan;
            generator.interiorLayout = interiorLayout;
            generator.convention = convention;
            generator.brushPresetList = brushPresetList;
            EditorUtility.SetDirty(generator);
        }

        /// <summary>
        /// 修复旧挖空墙体笔刷
        /// </summary>
        private void RepairCutoutWallBrushPresets()
        {
            if (paintedPlan == null || !paintedPlan.RepairCutoutWallBrushPresets())
                return;

            EditorUtility.SetDirty(paintedPlan);
        }

        /// <summary>
        /// 确保默认资产
        /// </summary>
        private void EnsureDefaultAssets()
        {
            EnsureFolderExists(BrushPresetFolderPath);

            if (paintedPlan == null)
                paintedPlan = LoadOrCreateAsset<PaintedBuildingPlan>(PlanAssetPath);

            if (convention == null)
                convention = LoadOrCreateAsset<BuildingGridConvention>(ConventionAssetPath);

            EnsureAllBrushPresets();
            LoadBrushPresetList();

            if (currentBrushPreset == null)
            {
                currentBrushPreset = FindBrushPreset(EPaintedBuildingCellType.Wall);
                if (currentBrushPreset != null)
                    ApplyBrushPreset(currentBrushPreset);
            }
        }

        /// <summary>
        /// 确保内饰资产
        /// </summary>
        private void EnsureInteriorAssets()
        {
            EnsureFolderExists(InteriorBrushPresetFolderPath);
            if (interiorLayout == null)
                interiorLayout = LoadOrCreateAsset<PaintedInteriorLayout>(InteriorLayoutAssetPath);

            if (interiorLayout != null && interiorLayout.paintedBuildingPlan != paintedPlan)
            {
                interiorLayout.paintedBuildingPlan = paintedPlan;
                EditorUtility.SetDirty(interiorLayout);
            }

            LoadInteriorBrushPresetList();
            if (currentInteriorFurnitureBrushPreset == null
                || !interiorFurnitureBrushPresetList.Contains(currentInteriorFurnitureBrushPreset))
            {
                currentInteriorFurnitureBrushPreset = interiorFurnitureBrushPresetList.Count > 0
                    ? interiorFurnitureBrushPresetList[0]
                    : null;
                if (currentInteriorFurnitureBrushPreset != null)
                    currentInteriorRotation = currentInteriorFurnitureBrushPreset.defaultRotation;
            }
        }

        /// <summary>
        /// 确保所有笔刷预设
        /// </summary>
        private void EnsureAllBrushPresets()
        {
            foreach (EPaintedBuildingCellType cellType in Enum.GetValues(typeof(EPaintedBuildingCellType)))
            {
                if (cellType == EPaintedBuildingCellType.CutoutFill)
                    continue;

                string assetPath = $"{BrushPresetFolderPath}/{cellType}.asset";
                var brushPreset = AssetDatabase.LoadAssetAtPath<PaintedBuildingBrushPreset>(assetPath);
                if (brushPreset != null)
                {
                    if (!brushPreset.isPrimaryPreset)
                    {
                        brushPreset.isPrimaryPreset = true;
                        EditorUtility.SetDirty(brushPreset);
                    }

                    UpdateBrushPresetDisplayName(brushPreset);
                    continue;
                }

                brushPreset = CreateInstance<PaintedBuildingBrushPreset>();
                brushPreset.name = GetCellTypeDisplayName(cellType);
                brushPreset.cellType = cellType;
                brushPreset.displayName = GetCellTypeDisplayName(cellType);
                brushPreset.isPrimaryPreset = true;
                brushPreset.previewColor = BuildingPainterColorUtility.GetCellColor(cellType);
                brushPreset.defaultHeightGridCount = GetDefaultHeight(cellType);
                AssetDatabase.CreateAsset(brushPreset, assetPath);
                EditorUtility.SetDirty(brushPreset);
            }
        }

        /// <summary>
        /// 更新笔刷预设显示名
        /// </summary>
        private void UpdateBrushPresetDisplayName(PaintedBuildingBrushPreset brushPreset)
        {
            if (brushPreset == null || !brushPreset.isPrimaryPreset)
                return;

            string displayName = GetCellTypeDisplayName(brushPreset.cellType);
            if (brushPreset.name == displayName)
                return;

            brushPreset.name = displayName;
            if (string.IsNullOrWhiteSpace(brushPreset.displayName))
                brushPreset.displayName = displayName;
            EditorUtility.SetDirty(brushPreset);
        }

        /// <summary>
        /// 加载笔刷预设列表
        /// </summary>
        private void LoadBrushPresetList()
        {
            brushPresetList.Clear();
            string[] guidList = AssetDatabase.FindAssets("t:PaintedBuildingBrushPreset", new[] { BrushPresetFolderPath });
            foreach (string guid in guidList)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var brushPreset = AssetDatabase.LoadAssetAtPath<PaintedBuildingBrushPreset>(assetPath);
                if (brushPreset != null)
                    brushPresetList.Add(brushPreset);
            }

            brushPresetList.Sort((a, b) =>
            {
                int cellTypeCompare = a.cellType.CompareTo(b.cellType);
                if (cellTypeCompare != 0)
                    return cellTypeCompare;

                if (a.isPrimaryPreset != b.isPrimaryPreset)
                    return a.isPrimaryPreset ? -1 : 1;

                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// 新建材质子笔刷
        /// </summary>
        private void CreateSubBrushPreset(EPaintedBuildingCellType cellType)
        {
            if (!SupportsSubBrush(cellType) && cellType != EPaintedBuildingCellType.CutoutFill)
                return;

            EnsureFolderExists(BrushPresetFolderPath);
            string typeName = cellType.ToString();
            string displayName = cellType == EPaintedBuildingCellType.CutoutFill
                ? "新挖空填充物"
                : $"{GetCellTypeDisplayName(cellType)}材质";
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{BrushPresetFolderPath}/{typeName}_Material.asset");
            var brushPreset = CreateInstance<PaintedBuildingBrushPreset>();
            brushPreset.name = displayName;
            brushPreset.displayName = displayName;
            brushPreset.cellType = cellType;
            brushPreset.isPrimaryPreset = false;
            brushPreset.previewColor = BuildingPainterColorUtility.GetCellColor(cellType);
            brushPreset.defaultHeightGridCount = GetDefaultHeight(cellType);
            AssetDatabase.CreateAsset(brushPreset, assetPath);
            EditorUtility.SetDirty(brushPreset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LoadBrushPresetList();
            SelectBrush(brushPreset);
            Selection.activeObject = brushPreset;
            Repaint();
        }

        /// <summary>
        /// 删除当前材质子笔刷
        /// </summary>
        private void DeleteCurrentSubBrushPreset(EPaintedBuildingCellType cellType)
        {
            if (currentBrushPreset == null
                || currentBrushPreset.cellType != cellType
                || currentBrushPreset.isPrimaryPreset
                || (!SupportsSubBrush(cellType) && cellType != EPaintedBuildingCellType.CutoutFill))
            {
                return;
            }

            string typeDisplayName = GetBrushGroupDisplayName(cellType);
            if (paintedPlan != null && paintedPlan.ContainsBrush(currentBrushPreset))
            {
                EditorUtility.DisplayDialog(
                    $"删除{typeDisplayName}笔刷",
                    $"当前笔刷仍被绘制蓝图使用 请先擦除对应{typeDisplayName}",
                    "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    $"删除{typeDisplayName}笔刷",
                    $"确定删除 {currentBrushPreset.DisplayName} 吗",
                    "删除",
                    "取消"))
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(currentBrushPreset);
            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.DeleteAsset(assetPath);

            if (lastWallBrushPreset == currentBrushPreset)
                lastWallBrushPreset = null;
            if (lastFloorBrushPreset == currentBrushPreset)
                lastFloorBrushPreset = null;

            currentBrushPreset = null;
            LoadBrushPresetList();
            currentBrushPreset = FindBrushPreset(cellType);
            if (currentBrushPreset != null)
            {
                if (cellType == EPaintedBuildingCellType.Wall)
                    lastWallBrushPreset = currentBrushPreset;
                if (cellType == EPaintedBuildingCellType.Floor)
                    lastFloorBrushPreset = currentBrushPreset;
                ApplyBrushPreset(currentBrushPreset);
            }

            AssetDatabase.SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 加载内饰笔刷列表
        /// </summary>
        private void LoadInteriorBrushPresetList()
        {
            interiorFurnitureBrushPresetList.Clear();
            if (!AssetDatabase.IsValidFolder(InteriorBrushPresetFolderPath))
                return;

            string[] guidList = AssetDatabase.FindAssets(
                "t:InteriorFurnitureBrushPreset",
                new[] { InteriorBrushPresetFolderPath });
            foreach (string guid in guidList)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var brushPreset = AssetDatabase.LoadAssetAtPath<InteriorFurnitureBrushPreset>(assetPath);
                if (brushPreset != null)
                    interiorFurnitureBrushPresetList.Add(brushPreset);
            }

            interiorFurnitureBrushPresetList.Sort((a, b) =>
            {
                int categoryCompare = a.category.CompareTo(b.category);
                return categoryCompare != 0
                    ? categoryCompare
                    : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// 创建内饰笔刷
        /// </summary>
        private void CreateInteriorBrushPreset()
        {
            EnsureFolderExists(InteriorBrushPresetFolderPath);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{InteriorBrushPresetFolderPath}/NewInteriorFurnitureBrushPreset.asset");
            var brushPreset = CreateInstance<InteriorFurnitureBrushPreset>();
            brushPreset.name = "新内饰笔刷";
            brushPreset.displayName = "新内饰";
            AssetDatabase.CreateAsset(brushPreset, assetPath);
            EditorUtility.SetDirty(brushPreset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LoadInteriorBrushPresetList();
            currentInteriorFurnitureBrushPreset = brushPreset;
            currentInteriorRotation = brushPreset.defaultRotation;
            Selection.activeObject = brushPreset;
            Repaint();
        }

        /// <summary>
        /// 删除当前内饰笔刷
        /// </summary>
        private void DeleteCurrentInteriorBrushPreset()
        {
            if (currentInteriorFurnitureBrushPreset == null)
                return;

            if (interiorLayout != null && interiorLayout.ContainsBrush(currentInteriorFurnitureBrushPreset))
            {
                EditorUtility.DisplayDialog(
                    "删除内饰笔刷",
                    "当前笔刷仍被内饰布局使用 请先擦除对应家具",
                    "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "删除内饰笔刷",
                    $"确定删除 {currentInteriorFurnitureBrushPreset.DisplayName} 吗",
                    "删除",
                    "取消"))
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(currentInteriorFurnitureBrushPreset);
            if (!string.IsNullOrEmpty(assetPath))
                AssetDatabase.DeleteAsset(assetPath);

            currentInteriorFurnitureBrushPreset = null;
            LoadInteriorBrushPresetList();
            if (interiorFurnitureBrushPresetList.Count > 0)
            {
                currentInteriorFurnitureBrushPreset = interiorFurnitureBrushPresetList[0];
                currentInteriorRotation = currentInteriorFurnitureBrushPreset.defaultRotation;
            }

            AssetDatabase.SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 加载持久引用
        /// </summary>
        private void LoadPersistedReferences()
        {
            string planPath = EditorPrefs.GetString(PlanPrefsKey, PlanAssetPath);
            paintedPlan = AssetDatabase.LoadAssetAtPath<PaintedBuildingPlan>(planPath) ?? paintedPlan;
            string interiorLayoutPath = EditorPrefs.GetString(InteriorLayoutPrefsKey, InteriorLayoutAssetPath);
            interiorLayout = AssetDatabase.LoadAssetAtPath<PaintedInteriorLayout>(interiorLayoutPath) ?? interiorLayout;
            generator = LoadPersistedGenerator() ?? UnityEngine.Object.FindObjectOfType<PaintedBuildingGenerator>();
        }

        /// <summary>
        /// 持久化引用
        /// </summary>
        private void PersistReferences()
        {
            if (paintedPlan != null)
                EditorPrefs.SetString(PlanPrefsKey, AssetDatabase.GetAssetPath(paintedPlan));

            if (interiorLayout != null)
                EditorPrefs.SetString(InteriorLayoutPrefsKey, AssetDatabase.GetAssetPath(interiorLayout));

            if (generator != null)
            {
                GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(generator);
                EditorPrefs.SetString(GeneratorPrefsKey, globalObjectId.ToString());
            }
        }

        /// <summary>
        /// 保存右侧绘制临时参数
        /// </summary>
        private void SavePaintToolPrefs()
        {
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.BrushMode", (int)brushMode);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.CellType", (int)currentCellType);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.FloorIndex", currentFloorIndex);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.CutoutStart", cutoutStartHeightGridCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.CutoutEnd", cutoutEndHeightGridCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.FloorMinX", floorFillBottomLeftGridPos.x);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.FloorMinY", floorFillBottomLeftGridPos.y);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.FloorMaxX", floorFillTopRightGridPos.x);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.FloorMaxY", floorFillTopRightGridPos.y);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.WallThickness", wallThicknessGridCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.WallExtend", (int)wallExtendDirection);
            EditorPrefs.SetBool($"{PaintToolPrefsKey}.RoomClear", roomClearBeforeGenerate);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomAnchorX", roomAnchorGridPos.x);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomAnchorY", roomAnchorGridPos.y);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomWidth", roomWidthGridCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomDepth", roomDepthGridCount);
            EditorPrefs.SetBool($"{PaintToolPrefsKey}.RoomDoor", roomEnableDoor);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomDoorSide", (int)roomDoorWallSide);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomDoorOffset", roomDoorOffsetGridCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomDoorWidth", roomDoorWidthGridCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomRow", roomGridRowCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomCol", roomGridColumnCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomAdjacent", roomAdjacentSpacingGridCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomCorridor", roomCorridorWidthGridCount);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomDoorMode", (int)roomGridDoorMode);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.RoomDoorSeed", roomGridDoorRandomSeed);
            EditorPrefs.SetFloat($"{PaintToolPrefsKey}.CellPixel", cellPixelSize);
            EditorPrefs.SetFloat($"{PaintToolPrefsKey}.PanX", gridPanOffset.x);
            EditorPrefs.SetFloat($"{PaintToolPrefsKey}.PanY", gridPanOffset.y);
            EditorPrefs.SetBool(WallFoldoutPrefsKey, wallBrushFoldoutExpanded);
            EditorPrefs.SetBool(FloorFoldoutPrefsKey, floorBrushFoldoutExpanded);
            EditorPrefs.SetInt($"{PaintToolPrefsKey}.InteriorRotation", (int)currentInteriorRotation);

            if (currentBrushPreset != null)
                EditorPrefs.SetString(CurrentBrushPrefsKey, AssetDatabase.GetAssetPath(currentBrushPreset));

            if (lastWallBrushPreset != null)
                EditorPrefs.SetString(LastWallBrushPrefsKey, AssetDatabase.GetAssetPath(lastWallBrushPreset));

            if (lastFloorBrushPreset != null)
                EditorPrefs.SetString(LastFloorBrushPrefsKey, AssetDatabase.GetAssetPath(lastFloorBrushPreset));

            if (currentInteriorFurnitureBrushPreset != null)
            {
                EditorPrefs.SetString(
                    $"{PaintToolPrefsKey}.InteriorBrushPath",
                    AssetDatabase.GetAssetPath(currentInteriorFurnitureBrushPreset));
            }
        }

        /// <summary>
        /// 加载右侧绘制临时参数
        /// </summary>
        private void LoadPaintToolPrefs()
        {
            if (!EditorPrefs.HasKey($"{PaintToolPrefsKey}.WallThickness"))
                return;

            brushMode = (EBrushMode)EditorPrefs.GetInt($"{PaintToolPrefsKey}.BrushMode", (int)brushMode);
            currentCellType = (EPaintedBuildingCellType)EditorPrefs.GetInt($"{PaintToolPrefsKey}.CellType", (int)currentCellType);
            currentFloorIndex = Mathf.Max(0, EditorPrefs.GetInt($"{PaintToolPrefsKey}.FloorIndex", currentFloorIndex));
            cutoutStartHeightGridCount = EditorPrefs.GetInt($"{PaintToolPrefsKey}.CutoutStart", cutoutStartHeightGridCount);
            cutoutEndHeightGridCount = EditorPrefs.GetInt($"{PaintToolPrefsKey}.CutoutEnd", cutoutEndHeightGridCount);
            floorFillBottomLeftGridPos = new Vector2Int(
                EditorPrefs.GetInt($"{PaintToolPrefsKey}.FloorMinX", floorFillBottomLeftGridPos.x),
                EditorPrefs.GetInt($"{PaintToolPrefsKey}.FloorMinY", floorFillBottomLeftGridPos.y));
            floorFillTopRightGridPos = new Vector2Int(
                EditorPrefs.GetInt($"{PaintToolPrefsKey}.FloorMaxX", floorFillTopRightGridPos.x),
                EditorPrefs.GetInt($"{PaintToolPrefsKey}.FloorMaxY", floorFillTopRightGridPos.y));
            wallThicknessGridCount = Mathf.Max(1, EditorPrefs.GetInt($"{PaintToolPrefsKey}.WallThickness", wallThicknessGridCount));
            wallExtendDirection = (EWallExtendDirection)EditorPrefs.GetInt($"{PaintToolPrefsKey}.WallExtend", (int)wallExtendDirection);
            roomClearBeforeGenerate = EditorPrefs.GetBool($"{PaintToolPrefsKey}.RoomClear", roomClearBeforeGenerate);
            roomAnchorGridPos = new Vector2Int(
                EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomAnchorX", roomAnchorGridPos.x),
                EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomAnchorY", roomAnchorGridPos.y));
            roomWidthGridCount = Mathf.Max(2, EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomWidth", roomWidthGridCount));
            roomDepthGridCount = Mathf.Max(2, EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomDepth", roomDepthGridCount));
            roomEnableDoor = EditorPrefs.GetBool($"{PaintToolPrefsKey}.RoomDoor", roomEnableDoor);
            roomDoorWallSide = (ERoomDoorWallSide)EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomDoorSide", (int)roomDoorWallSide);
            roomDoorOffsetGridCount = Mathf.Max(0, EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomDoorOffset", roomDoorOffsetGridCount));
            roomDoorWidthGridCount = Mathf.Max(1, EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomDoorWidth", roomDoorWidthGridCount));
            roomGridRowCount = Mathf.Max(1, EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomRow", roomGridRowCount));
            roomGridColumnCount = Mathf.Max(1, EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomCol", roomGridColumnCount));
            roomAdjacentSpacingGridCount = Mathf.Max(0, EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomAdjacent", roomAdjacentSpacingGridCount));
            roomCorridorWidthGridCount = Mathf.Max(1, EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomCorridor", roomCorridorWidthGridCount));
            roomGridDoorMode = (ERoomGridDoorMode)EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomDoorMode", (int)roomGridDoorMode);
            roomGridDoorRandomSeed = EditorPrefs.GetInt($"{PaintToolPrefsKey}.RoomDoorSeed", roomGridDoorRandomSeed);
            cellPixelSize = Mathf.Clamp(
                EditorPrefs.GetFloat($"{PaintToolPrefsKey}.CellPixel", cellPixelSize),
                MinCellPixelSize,
                MaxCellPixelSize);
            gridPanOffset = new Vector2(
                EditorPrefs.GetFloat($"{PaintToolPrefsKey}.PanX", gridPanOffset.x),
                EditorPrefs.GetFloat($"{PaintToolPrefsKey}.PanY", gridPanOffset.y));
            wallBrushFoldoutExpanded = EditorPrefs.GetBool(WallFoldoutPrefsKey, wallBrushFoldoutExpanded);
            floorBrushFoldoutExpanded = EditorPrefs.GetBool(FloorFoldoutPrefsKey, floorBrushFoldoutExpanded);
            currentInteriorRotation = (EInteriorFurnitureRotation)EditorPrefs.GetInt(
                $"{PaintToolPrefsKey}.InteriorRotation",
                (int)currentInteriorRotation);

            string currentBrushPath = EditorPrefs.GetString(CurrentBrushPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(currentBrushPath))
            {
                var loadedBrushPreset = AssetDatabase.LoadAssetAtPath<PaintedBuildingBrushPreset>(currentBrushPath);
                if (loadedBrushPreset != null)
                {
                    currentBrushPreset = loadedBrushPreset;
                    currentCellType = loadedBrushPreset.cellType;
                }
            }

            string lastWallBrushPath = EditorPrefs.GetString(LastWallBrushPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(lastWallBrushPath))
            {
                var loadedWallBrushPreset = AssetDatabase.LoadAssetAtPath<PaintedBuildingBrushPreset>(lastWallBrushPath);
                if (loadedWallBrushPreset != null)
                    lastWallBrushPreset = loadedWallBrushPreset;
            }

            string lastFloorBrushPath = EditorPrefs.GetString(LastFloorBrushPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(lastFloorBrushPath))
            {
                var loadedFloorBrushPreset = AssetDatabase.LoadAssetAtPath<PaintedBuildingBrushPreset>(lastFloorBrushPath);
                if (loadedFloorBrushPreset != null)
                    lastFloorBrushPreset = loadedFloorBrushPreset;
            }

            string interiorBrushPath = EditorPrefs.GetString($"{PaintToolPrefsKey}.InteriorBrushPath", string.Empty);
            if (!string.IsNullOrEmpty(interiorBrushPath))
            {
                var loadedInteriorBrushPreset = AssetDatabase.LoadAssetAtPath<InteriorFurnitureBrushPreset>(interiorBrushPath);
                if (loadedInteriorBrushPreset != null)
                    currentInteriorFurnitureBrushPreset = loadedInteriorBrushPreset;
            }

            cutoutStartHeightGridCount = Mathf.Clamp(cutoutStartHeightGridCount, 0, wallHeightGridCount - 1);
            cutoutEndHeightGridCount = Mathf.Clamp(
                cutoutEndHeightGridCount,
                cutoutStartHeightGridCount + 1,
                wallHeightGridCount);
        }

        /// <summary>
        /// 加载持久生成器
        /// </summary>
        private PaintedBuildingGenerator LoadPersistedGenerator()
        {
            string generatorId = EditorPrefs.GetString(GeneratorPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(generatorId))
                return null;

            if (!GlobalObjectId.TryParse(generatorId, out GlobalObjectId globalObjectId))
                return null;

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId) as PaintedBuildingGenerator;
        }

        /// <summary>
        /// 获取或创建生成器
        /// </summary>
        private PaintedBuildingGenerator GetOrCreateGenerator()
        {
            if (generator != null)
                return generator;

            generator = UnityEngine.Object.FindObjectOfType<PaintedBuildingGenerator>();
            if (generator != null)
                return generator;

            var obj = new GameObject("PaintedBuildingGenerator");
            Undo.RegisterCreatedObjectUndo(obj, "创建绘制建筑生成器");
            generator = obj.AddComponent<PaintedBuildingGenerator>();
            return generator;
        }

        /// <summary>
        /// 查找笔刷预设
        /// </summary>
        private PaintedBuildingBrushPreset FindBrushPreset(EPaintedBuildingCellType cellType)
        {
            PaintedBuildingBrushPreset fallbackBrushPreset = null;
            foreach (var brushPreset in brushPresetList)
            {
                if (brushPreset == null || brushPreset.cellType != cellType)
                    continue;

                if (brushPreset.isPrimaryPreset)
                    return brushPreset;

                if (fallbackBrushPreset == null)
                    fallbackBrushPreset = brushPreset;
            }

            return fallbackBrushPreset;
        }

        /// <summary>
        /// 获取格子颜色
        /// </summary>
        private Color GetCellColor(PaintedBuildingCellData cellData)
        {
            if (cellData == null)
                return BuildingPainterColorUtility.GetCellColor(EPaintedBuildingCellType.Floor);

            // 普通挖空保留专用色 填充物笔刷使用自身颜色
            if (cellData.cellType == EPaintedBuildingCellType.Cutout
                && (cellData.brushPreset == null
                    || cellData.brushPreset.cellType == EPaintedBuildingCellType.Cutout))
                return BuildingPainterColorUtility.GetCellColor(EPaintedBuildingCellType.Cutout);

            if (cellData.brushPreset != null
                && (cellData.brushPreset.cellType == cellData.cellType
                    || (cellData.cellType == EPaintedBuildingCellType.Cutout
                        && cellData.brushPreset.cellType == EPaintedBuildingCellType.CutoutFill)))
            {
                return cellData.brushPreset.previewColor;
            }

            foreach (var brushPreset in brushPresetList)
            {
                if (brushPreset == null || brushPreset.cellType != cellData.cellType)
                    continue;

                return brushPreset.previewColor;
            }

            return BuildingPainterColorUtility.GetCellColor(cellData.cellType);
        }

        /// <summary>
        /// 获取当前笔刷颜色
        /// </summary>
        private Color GetCurrentBrushColor()
        {
            if (brushMode == EBrushMode.Interior)
            {
                return currentInteriorFurnitureBrushPreset != null
                    ? currentInteriorFurnitureBrushPreset.previewColor
                    : new Color(0.2f, 0.75f, 1f, 1f);
            }

            if (currentBrushPreset != null)
                return currentBrushPreset.previewColor;

            return BuildingPainterColorUtility.GetCellColor(currentCellType);
        }

        /// <summary>
        /// 获取内饰大类显示名
        /// </summary>
        private string GetInteriorCategoryDisplayName(EInteriorFurnitureCategory category)
        {
            switch (category)
            {
                case EInteriorFurnitureCategory.Bed:
                    return "床";
                case EInteriorFurnitureCategory.Table:
                    return "桌子";
                case EInteriorFurnitureCategory.Chair:
                    return "椅子";
                case EInteriorFurnitureCategory.Cabinet:
                    return "柜子";
                case EInteriorFurnitureCategory.Chest:
                    return "箱子";
                default:
                    return "内饰";
            }
        }

        /// <summary>
        /// 获取内饰旋转显示名
        /// </summary>
        private string GetInteriorRotationDisplayName(EInteriorFurnitureRotation rotation)
        {
            switch (rotation)
            {
                case EInteriorFurnitureRotation.Deg90:
                    return "90 度";
                case EInteriorFurnitureRotation.Deg180:
                    return "180 度";
                case EInteriorFurnitureRotation.Deg270:
                    return "270 度";
                default:
                    return "0 度";
            }
        }

        /// <summary>
        /// 获取默认高度
        /// </summary>
        private int GetDefaultHeight(EPaintedBuildingCellType cellType)
        {
            switch (cellType)
            {
                case EPaintedBuildingCellType.Floor:
                    return 1;
                case EPaintedBuildingCellType.Room:
                    return 3;
                default:
                    return 3;
            }
        }

        /// <summary>
        /// 获取笔刷显示名
        /// </summary>
        private string GetBrushLabel(PaintedBuildingBrushPreset brushPreset)
        {
            if (brushPreset.cellType == EPaintedBuildingCellType.Erase)
                return "擦除";

            if (brushPreset.cellType == EPaintedBuildingCellType.Room)
                return "房间";

            if (brushPreset.cellType == EPaintedBuildingCellType.Cutout)
                return "挖空";

            if (brushPreset.cellType == EPaintedBuildingCellType.CutoutFill)
                return brushPreset.DisplayName;

            if (brushPreset.cellType == EPaintedBuildingCellType.Wall
                || brushPreset.cellType == EPaintedBuildingCellType.Floor)
                return brushPreset.DisplayName;

            return GetCellTypeDisplayName(brushPreset.cellType);
        }

        /// <summary>
        /// 获取笔刷分组显示名
        /// </summary>
        private static string GetBrushGroupDisplayName(EPaintedBuildingCellType cellType)
        {
            return GetCellTypeDisplayName(cellType);
        }

        /// <summary>
        /// 获取格子类型显示名
        /// </summary>
        private static string GetCellTypeDisplayName(EPaintedBuildingCellType cellType)
        {
            switch (cellType)
            {
                case EPaintedBuildingCellType.Floor:
                    return "地面";
                case EPaintedBuildingCellType.Wall:
                    return "墙体";
                case EPaintedBuildingCellType.Cutout:
                    return "挖空";
                case EPaintedBuildingCellType.CutoutFill:
                    return "挖空填充物";
                case EPaintedBuildingCellType.Erase:
                    return "擦除";
                case EPaintedBuildingCellType.Room:
                    return "房间";
                default:
                    return "无";
            }
        }

        /// <summary>
        /// 获取网格中心
        /// </summary>
        private Vector2 GetGridCenter(Rect gridRect)
        {
            return new Vector2(gridRect.width * 0.5f, gridRect.height * 0.5f) + gridPanOffset;
        }

        /// <summary>
        /// 窗口坐标转格子
        /// </summary>
        private Vector2Int WindowToGrid(Vector2 localPos, Rect gridRect)
        {
            Vector2 center = GetGridCenter(gridRect);
            Vector2 offset = localPos - center;
            int x = Mathf.FloorToInt(offset.x / cellPixelSize);
            int z = Mathf.FloorToInt(-offset.y / cellPixelSize);
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// 格子转窗口矩形
        /// </summary>
        private Rect GridToWindowCellRect(Vector2Int gridPos, Rect gridRect)
        {
            Vector2 center = GetGridCenter(gridRect);
            float x = center.x + gridPos.x * cellPixelSize;
            float y = center.y - (gridPos.y + 1) * cellPixelSize;
            return new Rect(x + 1f, y + 1f, cellPixelSize - 2f, cellPixelSize - 2f);
        }

        /// <summary>
        /// 获取框选边界
        /// </summary>
        private void GetGridRectBounds(
            Vector2Int startGridPos,
            Vector2Int endGridPos,
            out int minX,
            out int maxX,
            out int minZ,
            out int maxZ)
        {
            minX = Mathf.Min(startGridPos.x, endGridPos.x);
            maxX = Mathf.Max(startGridPos.x, endGridPos.x);
            minZ = Mathf.Min(startGridPos.y, endGridPos.y);
            maxZ = Mathf.Max(startGridPos.y, endGridPos.y);
        }

        /// <summary>
        /// 加载或创建资产
        /// </summary>
        private T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                return asset;

            asset = CreateInstance<T>();
            EnsureFolderExists(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>
        /// 确保文件夹链路存在
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            string normalized = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
                return;

            string[] partList = normalized.Split('/');
            string current = partList[0];
            for (int i = 1; i < partList.Length; i++)
            {
                string next = $"{current}/{partList[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, partList[i]);
                current = next;
            }
        }

        /// <summary>
        /// 定位本窗口脚本路径
        /// </summary>
        private static string FindThisScriptPath()
        {
            const string fileName = "BuildingPainterWindow.cs";
            string[] guidList = AssetDatabase.FindAssets("BuildingPainterWindow");
            for (int i = 0; i < guidList.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guidList[i]);
                if (path.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return string.Empty;
        }
    }
}
#endif
