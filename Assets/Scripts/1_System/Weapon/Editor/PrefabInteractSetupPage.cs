#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace DBWeaponSystem.Editor
{
    /// <summary>
    /// 交互预制体快速配置页
    /// </summary>
    public sealed class PrefabInteractSetupPage
    {
        private const string LayerPrefKey = "DBWeapon.PrefabSetup.TargetLayer";
        private const string DisableMeshPrefKey = "DBWeapon.PrefabSetup.DisableNonConvexMesh";

        /// <summary> 配置目标类型 </summary>
        public enum ESetupKind
        {
            [LabelText("可交互物品")]
            InteractItem = 0,
            [LabelText("搜刮容器")]
            ScrapContainer = 1,
            [LabelText("可拆容器")]
            BreakableContainer = 2,
            [LabelText("家具")]
            Furniture = 3,
        }

        [HideInInspector]
        public WeaponDataEditorWindow Window;

        public PrefabInteractSetupPage(WeaponDataEditorWindow window)
        {
            Window = window;
        }

        [Title("目标预制体")]
        [AssetsOnly]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = false)]
        [LabelText("预制体列表")]
        public List<GameObject> PrefabList = new();

        [Title("配置类型")]
        [EnumToggleButtons]
        [OnValueChanged(nameof(MarkDirty))]
        [LabelText("类型")]
        public ESetupKind SetupKind = ESetupKind.InteractItem;

        [Title("搜刮容器组件")]
        [ShowIf(nameof(IsScrapContainer))]
        [InfoBox("应用后添加 InteractOutline / ScrapContainerInteractBehaviour / DamageableBehaviour / BoxCollider")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("配置内容")]
        private string ScrapContainerComponentInfo => "InteractOutline / ScrapContainerInteractBehaviour / DamageableBehaviour / BoxCollider";

        [Title("可拆容器组件")]
        [ShowIf(nameof(IsBreakableContainer))]
        [InfoBox("应用后添加 InteractOutline / StorageBoxInteractBehaviour / BoxCollider / DamageableBehaviour 并移除预制体上的 MeshCollider")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("配置内容")]
        private string BreakableContainerComponentInfo => "InteractOutline / StorageBoxInteractBehaviour / BoxCollider / DamageableBehaviour";

        [Title("家具组件")]
        [ShowIf(nameof(IsFurniture))]
        [InfoBox("应用后添加 InteractOutline / PlaceAndBreakInteractBehaviour / BoxCollider / DamageableBehaviour 不能 F 打开")]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("配置内容")]
        private string FurnitureComponentInfo => "InteractOutline / PlaceAndBreakInteractBehaviour / BoxCollider / DamageableBehaviour";

        [Title("层级")]
        [ValueDropdown(nameof(GetLayerNameList))]
        [OnValueChanged(nameof(OnLayerChanged))]
        [LabelText("目标 Layer")]
        public string TargetLayerName = "Interactable";

        [ShowIf(nameof(IsInteractItem))]
        [LabelText("禁用非凸 MeshCollider")]
        [OnValueChanged(nameof(OnDisableMeshChanged))]
        public bool DisableNonConvexMeshCollider = true;

        [Title("额外组件")]
        [InfoBox("仅添加覆盖下列组件 不删模型上其它无关组件")]
        [ListDrawerSettings(ShowFoldout = true)]
        [LabelText("额外 MonoScript")]
        public List<MonoScript> ExtraScriptList = new();

        [Title("操作")]
        [Button("应用到全部预制体", ButtonSizes.Large), GUIColor(0.35f, 0.78f, 0.45f)]
        private void ApplyToAllPrefabs()
        {
            if (PrefabList == null || PrefabList.Count == 0)
            {
                Debug.LogWarning("请先拖入预制体");
                return;
            }

            int successCount = 0;
            for (int i = 0; i < PrefabList.Count; i++)
            {
                if (ApplyOnePrefab(PrefabList[i]))
                    successCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Window?.ClearDirty();
            Debug.Log($"预制体配置完成 {successCount}/{PrefabList.Count}");
            RefreshInspectTargets();
        }

        [Button("刷新下方参数面板", ButtonSizes.Medium)]
        private void RefreshInspectTargets()
        {
            inspectRootList.Clear();
            if (PrefabList == null)
                return;

            for (int i = 0; i < PrefabList.Count; i++)
            {
                var prefab = PrefabList[i];
                if (prefab == null)
                    continue;
                inspectRootList.Add(prefab);
            }
        }

        [Button("Inspector 选中首个预制体", ButtonSizes.Medium)]
        private void SelectFirstPrefab()
        {
            if (PrefabList == null || PrefabList.Count == 0 || PrefabList[0] == null)
            {
                Debug.LogWarning("列表为空");
                return;
            }

            Selection.activeObject = PrefabList[0];
            EditorGUIUtility.PingObject(PrefabList[0]);
        }

        [Title("编辑组件参数")]
        [InfoBox("展开后可改 ItemInteract / ScrapContainerInteractBehaviour / PlaceAndBreakInteractBehaviour / InteractOutline 等字段")]
        [ShowInInspector]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
        [LabelText("预制体")]
        private List<GameObject> inspectRootList = new();

        [ShowInInspector]
        [ShowIf(nameof(HasInspectRoot))]
        [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
        [LabelText("选中首个预制体内联")]
        private GameObject InlineFirstPrefab
        {
            get
            {
                if (inspectRootList == null || inspectRootList.Count == 0)
                    return null;
                return inspectRootList[0];
            }
        }

        /// <summary>
        /// 从会话加载
        /// </summary>
        public void LoadFromSession()
        {
            TargetLayerName = EditorPrefs.GetString(LayerPrefKey, "Interactable");
            DisableNonConvexMeshCollider = EditorPrefs.GetBool(DisableMeshPrefKey, true);
            if (LayerMask.NameToLayer(TargetLayerName) < 0)
                TargetLayerName = "Interactable";
        }

        private bool IsInteractItem => SetupKind == ESetupKind.InteractItem;
        private bool IsScrapContainer => SetupKind == ESetupKind.ScrapContainer;
        private bool IsBreakableContainer => SetupKind == ESetupKind.BreakableContainer;
        private bool IsFurniture => SetupKind == ESetupKind.Furniture;
        private bool HasInspectRoot => inspectRootList != null && inspectRootList.Count > 0;

        /// <summary>
        /// 应用单个预制体
        /// </summary>
        private bool ApplyOnePrefab(GameObject prefabAsset)
        {
            if (prefabAsset == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"不是预制体资源 {prefabAsset.name}");
                return false;
            }

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                int layer = ResolveLayer();
                SetLayerRecursively(root, layer);

                if (SetupKind == ESetupKind.InteractItem)
                    ApplyInteractItem(root);
                else if (SetupKind == ESetupKind.ScrapContainer)
                    ApplyScrapContainer(root);
                else if (SetupKind == ESetupKind.BreakableContainer)
                    ApplyBreakableContainer(root);
                else
                    ApplyFurniture(root);

                ApplyExtraScripts(root);

                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                Debug.Log($"已配置 {assetPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// 可交互物品组件
        /// </summary>
        private void ApplyInteractItem(GameObject root)
        {
            if (DisableNonConvexMeshCollider)
                DisableNonConvexMeshColliders(root);

            EnsureBoxCollider(root);
            EnsureRigidbody(root);
            EnsureComponent(root, "InteractOutline");
            EnsureComponent(root, "ItemInteract");
            EnsureComponent(root, "Interaction.ItemWorldPhysics");

            // 去掉搜刮与放置破坏避免冲突
            RemoveComponentByName(root, "ScrapContainerInteractBehaviour");
            RemoveComponentByName(root, "StorageBoxInteractBehaviour");
            RemoveComponentByName(root, "PlaceAndBreakInteractBehaviour");
        }

        /// <summary>
        /// 搜刮容器组件
        /// </summary>
        private void ApplyScrapContainer(GameObject root)
        {
            // 静态容器不要动态刚体
            RemoveComponentByName(root, "Rigidbody");
            RemoveComponentByName(root, "Interaction.ItemWorldPhysics");
            RemoveComponentByName(root, "ItemInteract");
            RemoveComponentByName(root, "SimpleDamageable");
            RemoveComponentByName(root, "StorageBoxInteractBehaviour");
            RemoveComponentByName(root, "PlaceAndBreakInteractBehaviour");

            EnsureBoxCollider(root);
            EnsureComponent(root, "InteractOutline");
            EnsureComponent(root, "DamageableBehaviour");
            EnsureComponent(root, "ScrapContainerInteractBehaviour");

            root.isStatic = true;
            GameObjectUtility.SetStaticEditorFlags(
                root,
                StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.ContributeGI
                | StaticEditorFlags.OccluderStatic
                | StaticEditorFlags.OccludeeStatic);
        }

        /// <summary>
        /// 可拆容器组件
        /// </summary>
        private void ApplyBreakableContainer(GameObject root)
        {
            RemoveComponentByName(root, "Rigidbody");
            RemoveComponentByName(root, "Interaction.ItemWorldPhysics");
            RemoveComponentByName(root, "ItemInteract");
            RemoveComponentByName(root, "ScrapContainerInteractBehaviour");
            RemoveMeshColliders(root);

            EnsureComponent(root, "InteractOutline");
            root.isStatic = true;
            GameObjectUtility.SetStaticEditorFlags(
                root,
                StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.ContributeGI
                | StaticEditorFlags.OccluderStatic
                | StaticEditorFlags.OccludeeStatic);
        }

        /// <summary>
        /// 家具组件 不能 F 打开
        /// </summary>
        private void ApplyFurniture(GameObject root)
        {
            RemoveComponentByName(root, "Rigidbody");
            RemoveComponentByName(root, "Interaction.ItemWorldPhysics");
            RemoveComponentByName(root, "ItemInteract");
            RemoveComponentByName(root, "SimpleDamageable");
            RemoveComponentByName(root, "ScrapContainerInteractBehaviour");
            RemoveComponentByName(root, "StorageBoxInteractBehaviour");
            RemoveMeshColliders(root);

            EnsureBoxCollider(root);
            EnsureComponent(root, "InteractOutline");
            EnsureComponent(root, "DamageableBehaviour");
            EnsureComponent(root, "PlaceAndBreakInteractBehaviour");

            root.isStatic = true;
            GameObjectUtility.SetStaticEditorFlags(
                root,
                StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.ContributeGI
                | StaticEditorFlags.OccluderStatic
                | StaticEditorFlags.OccludeeStatic);
        }

        /// <summary>
        /// 追加额外脚本
        /// </summary>
        private void ApplyExtraScripts(GameObject root)
        {
            if (ExtraScriptList == null)
                return;

            for (int i = 0; i < ExtraScriptList.Count; i++)
            {
                var script = ExtraScriptList[i];
                if (script == null)
                    continue;

                var type = script.GetClass();
                if (type == null || !typeof(Component).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"额外脚本无效 {script.name}");
                    continue;
                }

                if (root.GetComponent(type) == null)
                    root.AddComponent(type);
            }
        }

        /// <summary>
        /// 确保 BoxCollider 并按 Renderer 包围盒拟合
        /// </summary>
        private static void EnsureBoxCollider(GameObject root)
        {
            var box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();

            FitBoxColliderToRenderers(root, box);
            box.isTrigger = false;
            box.enabled = true;
        }

        /// <summary>
        /// 确保 Rigidbody 掉落默认参数
        /// </summary>
        private static void EnsureRigidbody(GameObject root)
        {
            var body = root.GetComponent<Rigidbody>();
            if (body == null)
                body = root.AddComponent<Rigidbody>();

            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>
        /// 按类型名确保组件存在
        /// </summary>
        private static Component EnsureComponent(GameObject root, string typeName)
        {
            var type = FindType(typeName);
            if (type == null)
            {
                Debug.LogError($"找不到类型 {typeName}");
                return null;
            }

            var existing = root.GetComponent(type);
            if (existing != null)
                return existing;

            return root.AddComponent(type);
        }

        /// <summary>
        /// 按类型名移除组件
        /// </summary>
        private static void RemoveComponentByName(GameObject root, string typeName)
        {
            var type = FindType(typeName);
            if (type == null)
                return;

            var componentList = root.GetComponents(type);
            for (int i = 0; i < componentList.Length; i++)
            {
                if (componentList[i] != null)
                    UnityEngine.Object.DestroyImmediate(componentList[i], true);
            }
        }

        /// <summary>
        /// 禁用非凸 MeshCollider 避免动态刚体报错
        /// </summary>
        private static void DisableNonConvexMeshColliders(GameObject root)
        {
            var meshColliderList = root.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshColliderList.Length; i++)
            {
                var meshCollider = meshColliderList[i];
                if (meshCollider == null)
                    continue;
                if (!meshCollider.convex)
                    meshCollider.enabled = false;
            }
        }

        /// <summary>
        /// 移除预制体及子物体上的 MeshCollider
        /// </summary>
        private static void RemoveMeshColliders(GameObject root)
        {
            var meshColliderList = root.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshColliderList.Length; i++)
            {
                var meshCollider = meshColliderList[i];
                if (meshCollider != null)
                    UnityEngine.Object.DestroyImmediate(meshCollider, true);
            }
        }

        /// <summary>
        /// 用子 Renderer 世界包围盒拟合本地 BoxCollider
        /// </summary>
        private static void FitBoxColliderToRenderers(GameObject root, BoxCollider box)
        {
            var rendererList = root.GetComponentsInChildren<Renderer>(true);
            if (rendererList == null || rendererList.Length == 0)
                return;

            Bounds worldBounds = rendererList[0].bounds;
            for (int i = 1; i < rendererList.Length; i++)
                worldBounds.Encapsulate(rendererList[i].bounds);

            var transform = root.transform;
            Vector3 worldMin = worldBounds.min;
            Vector3 worldMax = worldBounds.max;
            Vector3 localMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 localMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? worldMin.x : worldMax.x,
                            y == 0 ? worldMin.y : worldMax.y,
                            z == 0 ? worldMin.z : worldMax.z);
                        Vector3 local = transform.InverseTransformPoint(corner);
                        localMin = Vector3.Min(localMin, local);
                        localMax = Vector3.Max(localMax, local);
                    }
                }
            }

            Vector3 localSize = localMax - localMin;
            if (localSize.sqrMagnitude < 0.0001f)
                localSize = Vector3.one * 0.1f;

            box.center = (localMin + localMax) * 0.5f;
            box.size = localSize;
        }

        /// <summary>
        /// 递归设层
        /// </summary>
        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (layer < 0)
                return;

            root.layer = layer;
            var transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        /// <summary>
        /// 解析目标层
        /// </summary>
        private int ResolveLayer()
        {
            int layer = LayerMask.NameToLayer(TargetLayerName);
            if (layer < 0)
            {
                Debug.LogWarning($"Layer 不存在 {TargetLayerName} 回退 Default");
                return 0;
            }

            return layer;
        }

        /// <summary>
        /// 程序集中查找类型
        /// </summary>
        private static Type FindType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            var assemblyList = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblyList.Length; i++)
            {
                var assembly = assemblyList[i];
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            // 短名兜底 跳过动态程序集
            for (int i = 0; i < assemblyList.Length; i++)
            {
                var assembly = assemblyList[i];
                if (assembly.IsDynamic)
                    continue;

                Type[] typeList;
                try
                {
                    typeList = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    typeList = e.Types;
                }

                if (typeList == null)
                    continue;

                for (int j = 0; j < typeList.Length; j++)
                {
                    var candidate = typeList[j];
                    if (candidate == null)
                        continue;
                    if (candidate.Name == typeName || candidate.FullName == typeName)
                        return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// 层名下拉
        /// </summary>
        private static IEnumerable<string> GetLayerNameList()
        {
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                    yield return name;
            }
        }

        private void OnLayerChanged()
        {
            EditorPrefs.SetString(LayerPrefKey, TargetLayerName);
            MarkDirty();
        }

        private void OnDisableMeshChanged()
        {
            EditorPrefs.SetBool(DisableMeshPrefKey, DisableNonConvexMeshCollider);
            MarkDirty();
        }

        private void MarkDirty()
        {
            Window?.MarkDirty();
        }
    }
}
#endif
