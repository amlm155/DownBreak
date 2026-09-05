#if UNITY_EDITOR
using MieMieFrameWork;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace DBWeaponSystem.Editor
{
    /// <summary>
    /// 武器刃尖刃根快速配置页
    /// </summary>
    public sealed class WeaponBladeSetupPage
    {
        private const string RootName = "BladeRoot";
        private const string TipName = "BladeTip";
        private const string PrefabPathKey = "DBWeapon.WeaponDataEditor.BladePrefabPath";

        [HideInInspector]
        public WeaponDataEditorWindow Window;

        public WeaponBladeSetupPage(WeaponDataEditorWindow window)
        {
            Window = window;
        }

        [Title("武器对象")]
        [OnValueChanged(nameof(OnWeaponRootChanged))]
        [LabelText("武器根")]
        public GameObject WeaponRoot;

        [ShowInInspector, ReadOnly, LabelText("BladeRoot")]
        private Transform bladeRoot;

        [ShowInInspector, ReadOnly, LabelText("BladeTip")]
        private Transform bladeTip;

        [Title("刃根 BladeRoot")]
        [ShowIf(nameof(HasBladeRoot))]
        [LabelText("本地位置")]
        [OnValueChanged(nameof(ApplyRootLocal))]
        public Vector3 RootLocalPosition;

        [ShowIf(nameof(HasBladeRoot))]
        [LabelText("本地欧拉角")]
        [OnValueChanged(nameof(ApplyRootLocal))]
        public Vector3 RootLocalEuler;

        [ShowIf(nameof(HasBladeRoot))]
        [LabelText("本地缩放")]
        [OnValueChanged(nameof(ApplyRootLocal))]
        public Vector3 RootLocalScale = Vector3.one;

        [Title("刃尖 BladeTip")]
        [ShowIf(nameof(HasBladeTip))]
        [LabelText("本地位置")]
        [OnValueChanged(nameof(ApplyTipLocal))]
        public Vector3 TipLocalPosition;

        [ShowIf(nameof(HasBladeTip))]
        [LabelText("本地欧拉角")]
        [OnValueChanged(nameof(ApplyTipLocal))]
        public Vector3 TipLocalEuler;

        [ShowIf(nameof(HasBladeTip))]
        [LabelText("本地缩放")]
        [OnValueChanged(nameof(ApplyTipLocal))]
        public Vector3 TipLocalScale = Vector3.one;

        [Title("采样")]
        [LabelText("采样 Transform")]
        public Transform SampleTransform;

        [HorizontalGroup("SampleOps")]
        [Button("采样→刃根", ButtonSizes.Medium)]
        private void SampleToRoot()
        {
            if (bladeRoot == null || SampleTransform == null)
            {
                Debug.LogWarning("需要 BladeRoot 与采样 Transform");
                return;
            }

            CopyWorldToLocal(SampleTransform, bladeRoot);
            PullFromTransforms();
            MarkModified();
        }

        [HorizontalGroup("SampleOps")]
        [Button("采样→刃尖", ButtonSizes.Medium)]
        private void SampleToTip()
        {
            if (bladeTip == null || SampleTransform == null)
            {
                Debug.LogWarning("需要 BladeTip 与采样 Transform");
                return;
            }

            CopyWorldToLocal(SampleTransform, bladeTip);
            PullFromTransforms();
            MarkModified();
        }

        [Title("操作")]
        [Button("查找或创建刃根刃尖", ButtonSizes.Large), GUIColor(0.35f, 0.65f, 0.95f)]
        private void EnsureBladePoints()
        {
            if (WeaponRoot == null)
            {
                Debug.LogWarning("请先指定武器根物体");
                return;
            }

            if (TryEditPrefabAsset(root =>
                {
                    EnsureChild(root.transform, RootName);
                    EnsureChild(root.transform, TipName);
                }))
            {
                RefreshBladeRefs();
                PullFromTransforms();
                MarkModified();
                Debug.Log($"已确保 {RootName}/{TipName} -> {WeaponRoot.name}", WeaponRoot);
                return;
            }

            EnsureChild(WeaponRoot.transform, RootName);
            EnsureChild(WeaponRoot.transform, TipName);
            RefreshBladeRefs();
            PullFromTransforms();
            MarkModified();
            Debug.Log($"已确保场景物体 {RootName}/{TipName}", WeaponRoot);
        }

        [HorizontalGroup("PingOps")]
        [Button("Ping 刃根", ButtonSizes.Medium)]
        private void PingRoot()
        {
            if (bladeRoot == null)
                return;
            Selection.activeTransform = bladeRoot;
            EditorGUIUtility.PingObject(bladeRoot.gameObject);
        }

        [HorizontalGroup("PingOps")]
        [Button("Ping 刃尖", ButtonSizes.Medium)]
        private void PingTip()
        {
            if (bladeTip == null)
                return;
            Selection.activeTransform = bladeTip;
            EditorGUIUtility.PingObject(bladeTip.gameObject);
        }

        [Button("保存预制体", ButtonSizes.Medium), GUIColor(0.35f, 0.78f, 0.45f)]
        private void SavePrefab()
        {
            if (WeaponRoot == null)
                return;

            if (PrefabUtility.IsPartOfPrefabInstance(WeaponRoot))
            {
                PrefabUtility.ApplyPrefabInstance(WeaponRoot, InteractionMode.UserAction);
                Window?.ClearDirty();
                Debug.Log("已 Apply 预制体实例", WeaponRoot);
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(WeaponRoot);
            if (!string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.SetDirty(WeaponRoot);
                AssetDatabase.SaveAssets();
                Window?.ClearDirty();
                Debug.Log("已保存预制体资源 " + assetPath);
                return;
            }

            EditorUtility.SetDirty(WeaponRoot);
            Window?.ClearDirty();
            Debug.Log("已标记场景物体 Dirty", WeaponRoot);
        }

        /// <summary>
        /// 从会话加载
        /// </summary>
        public void LoadFromSession()
        {
            string path = EditorPrefs.GetString(PrefabPathKey, string.Empty);
            if (string.IsNullOrEmpty(path))
                return;

            WeaponRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            RefreshBladeRefs();
            PullFromTransforms();
        }

        private bool HasBladeRoot => bladeRoot != null;

        private bool HasBladeTip => bladeTip != null;

        private void OnWeaponRootChanged()
        {
            string path = WeaponRoot != null
                ? AssetDatabase.GetAssetPath(WeaponRoot)
                : string.Empty;
            EditorPrefs.SetString(PrefabPathKey, path);
            RefreshBladeRefs();
            PullFromTransforms();
            Window?.MarkDirty();
        }

        private void RefreshBladeRefs()
        {
            bladeRoot = null;
            bladeTip = null;
            if (WeaponRoot == null)
                return;

            bladeRoot = FindBladePoint(WeaponRoot.transform, RootName);
            bladeTip = FindBladePoint(WeaponRoot.transform, TipName);
        }

        private void PullFromTransforms()
        {
            if (bladeRoot != null)
            {
                RootLocalPosition = bladeRoot.localPosition;
                RootLocalEuler = bladeRoot.localEulerAngles;
                RootLocalScale = bladeRoot.localScale;
            }

            if (bladeTip != null)
            {
                TipLocalPosition = bladeTip.localPosition;
                TipLocalEuler = bladeTip.localEulerAngles;
                TipLocalScale = bladeTip.localScale;
            }
        }

        private void ApplyRootLocal()
        {
            if (bladeRoot == null)
                return;

            if (TryEditPrefabAsset(root =>
                {
                    var point = FindBladePoint(root.transform, RootName);
                    if (point == null)
                        return;
                    point.localPosition = RootLocalPosition;
                    point.localEulerAngles = RootLocalEuler;
                    point.localScale = RootLocalScale;
                }))
            {
                RefreshBladeRefs();
                MarkModified();
                return;
            }

            bladeRoot.localPosition = RootLocalPosition;
            bladeRoot.localEulerAngles = RootLocalEuler;
            bladeRoot.localScale = RootLocalScale;
            MarkModified();
        }

        private void ApplyTipLocal()
        {
            if (bladeTip == null)
                return;

            if (TryEditPrefabAsset(root =>
                {
                    var point = FindBladePoint(root.transform, TipName);
                    if (point == null)
                        return;
                    point.localPosition = TipLocalPosition;
                    point.localEulerAngles = TipLocalEuler;
                    point.localScale = TipLocalScale;
                }))
            {
                RefreshBladeRefs();
                MarkModified();
                return;
            }

            bladeTip.localPosition = TipLocalPosition;
            bladeTip.localEulerAngles = TipLocalEuler;
            bladeTip.localScale = TipLocalScale;
            MarkModified();
        }

        private bool TryEditPrefabAsset(System.Action<GameObject> editAction)
        {
            if (WeaponRoot == null || editAction == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(WeaponRoot);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab"))
                return false;

            // 场景实例走直接改 不走 LoadPrefabContents
            if (PrefabUtility.IsPartOfPrefabInstance(WeaponRoot))
                return false;

            GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                editAction(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            WeaponRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return true;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            var found = FindBladePoint(parent, childName);
            if (found != null)
                return found;

            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static Transform FindBladePoint(Transform root, string pointName)
        {
            if (root == null || string.IsNullOrEmpty(pointName))
                return null;
            if (root.name == pointName)
                return root;
            return root.FindDeepChild(pointName);
        }

        private static void CopyWorldToLocal(Transform sample, Transform target)
        {
            target.position = sample.position;
            target.rotation = sample.rotation;
            target.localScale = sample.lossyScale;
        }

        private void MarkModified()
        {
            if (WeaponRoot != null)
                EditorUtility.SetDirty(WeaponRoot);
            Window?.MarkDirty();
        }
    }
}
#endif
