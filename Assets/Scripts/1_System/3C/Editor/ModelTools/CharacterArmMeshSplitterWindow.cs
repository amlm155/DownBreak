#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TestScene.EditorTools
{
    /// <summary>
    /// 手臂拆分工具
    /// 仅拆皮 同骨 FP
    /// 或拆皮拆骨 生成独立 FP Prefab 挂 HandsRoot
    /// </summary>
    public sealed class CharacterArmMeshSplitterWindow : EditorWindow
    {
        private enum ESplitMode
        {
            MeshOnly = 0,
            MeshAndBonesFpPrefab = 1,
        }

        /// <summary> 目标蒙皮 </summary>
        private SkinnedMeshRenderer targetRenderer;

        /// <summary> HandsRoot 可选 拆完自动挂上 </summary>
        private Transform handsRoot;

        /// <summary> 拆分模式 </summary>
        private ESplitMode splitMode = ESplitMode.MeshAndBonesFpPrefab;

        /// <summary> 手臂权重阈值 </summary>
        private float armWeightThreshold = 0.35f;

        /// <summary> 拆完关掉原全身 Mesh </summary>
        private bool disableSourceRenderer = true;

        /// <summary> TP 身体设为 PlayerLocalHidden </summary>
        private bool applyTpHiddenLayer = true;

        /// <summary> Mesh 输出目录 </summary>
        private string meshOutputFolder = "Assets/测试场景/GeneratedMeshes";

        /// <summary> Prefab 输出目录 </summary>
        private string prefabOutputFolder = "Assets/测试场景/GeneratedFpArms";

        /// <summary> 手臂骨骼关键词 </summary>
        private static readonly string[] ArmBoneKeywordList =
        {
            "Clavicle",
            "Shoulder",
            "Elbow",
            "Hand",
            "Finger",
            "Thumb",
            "IndexFinger",
            "UpperArm",
            "LowerArm",
            "ForeArm",
        };

        [MenuItem("Tools/DownBreak/角色手臂网格拆分")]
        private static void Open()
        {
            var window = GetWindow<CharacterArmMeshSplitterWindow>("手臂拆分 FP");
            window.minSize = new Vector2(460f, 360f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "推荐 拆皮+拆骨\n" +
                "生成独立 FP 手臂 Prefab 挂到 HandsRoot 自己看\n" +
                "PlayerModel 上 TP 身体用 Layer 剔除 别人看\n" +
                "Humanoid 手部动画可直接打到 FP Prefab 的 Avatar 上",
                MessageType.Info);

            targetRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                "目标 SkinnedMesh",
                targetRenderer,
                typeof(SkinnedMeshRenderer),
                true);

            handsRoot = (Transform)EditorGUILayout.ObjectField(
                "HandsRoot 可选",
                handsRoot,
                typeof(Transform),
                true);

            splitMode = (ESplitMode)EditorGUILayout.EnumPopup("拆分模式", splitMode);
            armWeightThreshold = EditorGUILayout.Slider("手臂权重阈值", armWeightThreshold, 0.05f, 0.95f);
            disableSourceRenderer = EditorGUILayout.Toggle("拆完关闭原 Mesh", disableSourceRenderer);
            applyTpHiddenLayer = EditorGUILayout.Toggle("TP 设 PlayerLocalHidden", applyTpHiddenLayer);
            meshOutputFolder = EditorGUILayout.TextField("Mesh 输出目录", meshOutputFolder);
            prefabOutputFolder = EditorGUILayout.TextField("Prefab 输出目录", prefabOutputFolder);

            EditorGUILayout.Space(10f);
            using (new EditorGUI.DisabledScope(targetRenderer == null))
            {
                string buttonLabel = splitMode == ESplitMode.MeshAndBonesFpPrefab
                    ? "拆皮拆骨 生成 FP Prefab"
                    : "仅拆皮 挂角色下";
                if (GUILayout.Button(buttonLabel, GUILayout.Height(36f)))
                    SplitSelected();
            }
        }

        /// <summary>
        /// 执行拆分
        /// </summary>
        private void SplitSelected()
        {
            if (targetRenderer == null || targetRenderer.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("拆分失败", "请指定带 Mesh 的 SkinnedMeshRenderer", "确定");
                return;
            }

            Transform[] sourceBoneList = targetRenderer.bones;
            if (sourceBoneList == null || sourceBoneList.Length == 0)
            {
                EditorUtility.DisplayDialog("拆分失败", "目标没有 bones", "确定");
                return;
            }

            EnsureFolder(meshOutputFolder);
            if (splitMode == ESplitMode.MeshAndBonesFpPrefab)
                EnsureFolder(prefabOutputFolder);

            var armBoneHashList = BuildArmBoneIndexHashList(sourceBoneList);
            if (armBoneHashList.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "拆分失败",
                    "没匹配到手臂骨骼 请确认骨骼名含 Clavicle Shoulder Elbow Hand Finger",
                    "确定");
                return;
            }

            ExtractMeshes(
                targetRenderer.sharedMesh,
                armBoneHashList,
                armWeightThreshold,
                out Mesh armMesh,
                out Mesh bodyMesh,
                out int armTriCount,
                out int bodyTriCount);

            if (armTriCount == 0 || bodyTriCount == 0)
            {
                DestroyImmediate(armMesh);
                DestroyImmediate(bodyMesh);
                EditorUtility.DisplayDialog(
                    "拆分失败",
                    $"结果无效 手臂三角={armTriCount} 身体三角={bodyTriCount} 试试调阈值",
                    "确定");
                return;
            }

            string baseName = targetRenderer.sharedMesh.name;
            string armPath = AssetDatabase.GenerateUniqueAssetPath($"{meshOutputFolder}/{baseName}_Arms.asset");
            string bodyPath = AssetDatabase.GenerateUniqueAssetPath($"{meshOutputFolder}/{baseName}_Body.asset");
            AssetDatabase.CreateAsset(armMesh, armPath);
            AssetDatabase.CreateAsset(bodyMesh, bodyPath);
            AssetDatabase.SaveAssets();

            // TP 身体仍用原角色骨骼
            Transform tpParent = targetRenderer.transform.parent != null
                ? targetRenderer.transform.parent
                : targetRenderer.transform;
            var bodyRenderer = CreateChildRenderer(tpParent, $"{targetRenderer.name}_TP_Body", targetRenderer, bodyMesh);
            if (applyTpHiddenLayer)
                ApplyLayerRecursive(bodyRenderer.gameObject, "PlayerLocalHidden");

            GameObject focusGo = bodyRenderer.gameObject;
            string extraMsg = "";

            if (splitMode == ESplitMode.MeshAndBonesFpPrefab)
            {
                GameObject fpPrefab = BuildFpArmsPrefabAsset(targetRenderer, armMesh, baseName, out string prefabPath);
                if (fpPrefab == null)
                {
                    EditorUtility.DisplayDialog("拆分失败", "FP Prefab 生成失败", "确定");
                    return;
                }

                if (handsRoot != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(fpPrefab, handsRoot);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                    focusGo = instance;
                    extraMsg = $"\n已挂到 HandsRoot {handsRoot.name}";
                }
                else
                {
                    focusGo = fpPrefab;
                    extraMsg = "\n未指定 HandsRoot 请手动把 Prefab 拖到 CameraPos/HandsRoot 下";
                }

                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
                extraMsg = $"\nFP Prefab\n{prefabPath}{extraMsg}";
            }
            else
            {
                var armsRenderer = CreateChildRenderer(
                    tpParent,
                    $"{targetRenderer.name}_FP_Arms",
                    targetRenderer,
                    armMesh);
                focusGo = armsRenderer.gameObject;
                extraMsg = "\n仅拆皮 手臂仍绑原骨 俯仰不跟相机 请改用拆骨 FP Prefab";
            }

            if (disableSourceRenderer)
                targetRenderer.enabled = false;

            Selection.activeGameObject = focusGo;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "拆分完成",
                $"手臂三角 {armTriCount}\n身体三角 {bodyTriCount}\n\nArms {armPath}\nBody {bodyPath}{extraMsg}",
                "好的");
        }

        /// <summary>
        /// 复制骨骼树 绑手臂皮 存 Prefab
        /// </summary>
        private GameObject BuildFpArmsPrefabAsset(
            SkinnedMeshRenderer source,
            Mesh armMesh,
            string baseName,
            out string prefabPath)
        {
            prefabPath = "";
            Transform skeletonRoot = FindSkeletonRoot(source);
            if (skeletonRoot == null)
            {
                Debug.LogError("找不到骨骼根");
                return null;
            }

            string rootName = $"FP_Arms_{baseName}";
            var fpRoot = new GameObject(rootName);

            // 复制整棵骨架 保证 Humanoid Avatar 骨名对齐
            var oldToNewDict = new Dictionary<Transform, Transform>();
            Transform clonedRoot = DuplicateHierarchy(skeletonRoot, fpRoot.transform, oldToNewDict);
            clonedRoot.name = skeletonRoot.name;

            Transform[] sourceBoneList = source.bones;
            var newBoneList = new Transform[sourceBoneList.Length];
            for (int i = 0; i < sourceBoneList.Length; i++)
            {
                Transform oldBone = sourceBoneList[i];
                if (oldBone == null)
                    continue;
                if (!oldToNewDict.TryGetValue(oldBone, out Transform newBone))
                {
                    Debug.LogWarning($"骨骼未复制到 {oldBone.name}");
                    continue;
                }

                newBoneList[i] = newBone;
            }

            Transform newRootBone = source.rootBone != null && oldToNewDict.TryGetValue(source.rootBone, out Transform mappedRoot)
                ? mappedRoot
                : clonedRoot;

            var meshGo = new GameObject($"{baseName}_FP_Arms");
            meshGo.transform.SetParent(fpRoot.transform, false);
            var fpRenderer = meshGo.AddComponent<SkinnedMeshRenderer>();
            CopyRendererSettings(fpRenderer, source, armMesh, newBoneList, newRootBone);

            var sourceAnimator = source.GetComponentInParent<Animator>();
            var fpAnimator = fpRoot.AddComponent<Animator>();
            if (sourceAnimator != null)
            {
                fpAnimator.avatar = sourceAnimator.avatar;
                fpAnimator.applyRootMotion = false;
                fpAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{prefabOutputFolder}/{rootName}.prefab");
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(fpRoot, prefabPath);
            DestroyImmediate(fpRoot);
            return prefabAsset;
        }

        /// <summary>
        /// 找骨架根 从 rootBone 向上停在 Animator 子级 避免把 Mesh 节点一起拷走
        /// </summary>
        private static Transform FindSkeletonRoot(SkinnedMeshRenderer source)
        {
            if (source.rootBone == null)
                return source.transform;

            var animator = source.GetComponentInParent<Animator>();
            Transform t = source.rootBone;
            while (t.parent != null)
            {
                Transform parent = t.parent;
                if (animator != null && parent == animator.transform)
                    break;
                if (parent == source.transform)
                    break;
                if (parent.GetComponent<SkinnedMeshRenderer>() != null)
                    break;
                t = parent;
            }

            return t;
        }

        /// <summary>
        /// 递归复制层级
        /// </summary>
        private static Transform DuplicateHierarchy(
            Transform source,
            Transform parent,
            Dictionary<Transform, Transform> oldToNewDict)
        {
            var go = new GameObject(source.name);
            Transform clone = go.transform;
            clone.SetParent(parent, false);
            clone.localPosition = source.localPosition;
            clone.localRotation = source.localRotation;
            clone.localScale = source.localScale;
            oldToNewDict[source] = clone;

            for (int i = 0; i < source.childCount; i++)
                DuplicateHierarchy(source.GetChild(i), clone, oldToNewDict);

            return clone;
        }

        /// <summary>
        /// 收集手臂骨骼下标
        /// </summary>
        private static HashSet<int> BuildArmBoneIndexHashList(Transform[] boneList)
        {
            var armBoneHashList = new HashSet<int>();
            for (int i = 0; i < boneList.Length; i++)
            {
                Transform bone = boneList[i];
                if (bone == null)
                    continue;

                string boneName = bone.name;
                for (int k = 0; k < ArmBoneKeywordList.Length; k++)
                {
                    if (boneName.IndexOf(ArmBoneKeywordList[k], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        armBoneHashList.Add(i);
                        break;
                    }
                }
            }

            return armBoneHashList;
        }

        /// <summary>
        /// 按权重拆 Mesh
        /// </summary>
        private static void ExtractMeshes(
            Mesh sourceMesh,
            HashSet<int> armBoneHashList,
            float threshold,
            out Mesh armMesh,
            out Mesh bodyMesh,
            out int armTriCount,
            out int bodyTriCount)
        {
            var boneWeightList = sourceMesh.boneWeights;
            var triangleList = sourceMesh.triangles;
            int vertexCount = sourceMesh.vertexCount;

            var vertexArmScoreList = new float[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                vertexArmScoreList[i] = GetArmWeightScore(boneWeightList[i], armBoneHashList);

            var armTriangleList = new List<int>(triangleList.Length / 2);
            var bodyTriangleList = new List<int>(triangleList.Length / 2);

            for (int i = 0; i < triangleList.Length; i += 3)
            {
                int i0 = triangleList[i];
                int i1 = triangleList[i + 1];
                int i2 = triangleList[i + 2];
                float score = (vertexArmScoreList[i0] + vertexArmScoreList[i1] + vertexArmScoreList[i2]) / 3f;

                if (score >= threshold)
                {
                    armTriangleList.Add(i0);
                    armTriangleList.Add(i1);
                    armTriangleList.Add(i2);
                }
                else
                {
                    bodyTriangleList.Add(i0);
                    bodyTriangleList.Add(i1);
                    bodyTriangleList.Add(i2);
                }
            }

            armTriCount = armTriangleList.Count / 3;
            bodyTriCount = bodyTriangleList.Count / 3;
            armMesh = BuildSubMesh(sourceMesh, armTriangleList, sourceMesh.name + "_Arms");
            bodyMesh = BuildSubMesh(sourceMesh, bodyTriangleList, sourceMesh.name + "_Body");
        }

        /// <summary>
        /// 手臂权重分
        /// </summary>
        private static float GetArmWeightScore(BoneWeight boneWeight, HashSet<int> armBoneHashList)
        {
            float score = 0f;
            if (armBoneHashList.Contains(boneWeight.boneIndex0))
                score += boneWeight.weight0;
            if (armBoneHashList.Contains(boneWeight.boneIndex1))
                score += boneWeight.weight1;
            if (armBoneHashList.Contains(boneWeight.boneIndex2))
                score += boneWeight.weight2;
            if (armBoneHashList.Contains(boneWeight.boneIndex3))
                score += boneWeight.weight3;
            return score;
        }

        /// <summary>
        /// 紧凑子 Mesh
        /// </summary>
        private static Mesh BuildSubMesh(Mesh sourceMesh, List<int> sourceTriangleList, string meshName)
        {
            var oldToNewDict = new Dictionary<int, int>(sourceTriangleList.Count);
            var newTriangleList = new List<int>(sourceTriangleList.Count);
            var usedVertexList = new List<int>(sourceTriangleList.Count / 2);

            for (int i = 0; i < sourceTriangleList.Count; i++)
            {
                int oldIndex = sourceTriangleList[i];
                if (!oldToNewDict.TryGetValue(oldIndex, out int newIndex))
                {
                    newIndex = usedVertexList.Count;
                    oldToNewDict.Add(oldIndex, newIndex);
                    usedVertexList.Add(oldIndex);
                }

                newTriangleList.Add(newIndex);
            }

            int newVertexCount = usedVertexList.Count;
            var positions = sourceMesh.vertices;
            var normals = sourceMesh.normals;
            var tangents = sourceMesh.tangents;
            var uv0 = sourceMesh.uv;
            var uv1 = sourceMesh.uv2;
            var colors = sourceMesh.colors;
            var boneWeights = sourceMesh.boneWeights;
            var bindposes = sourceMesh.bindposes;

            var newPositions = new Vector3[newVertexCount];
            var newNormals = normals != null && normals.Length == positions.Length ? new Vector3[newVertexCount] : null;
            var newTangents = tangents != null && tangents.Length == positions.Length ? new Vector4[newVertexCount] : null;
            var newUv0 = uv0 != null && uv0.Length == positions.Length ? new Vector2[newVertexCount] : null;
            var newUv1 = uv1 != null && uv1.Length == positions.Length ? new Vector2[newVertexCount] : null;
            var newColors = colors != null && colors.Length == positions.Length ? new Color[newVertexCount] : null;
            var newBoneWeights = boneWeights != null && boneWeights.Length == positions.Length
                ? new BoneWeight[newVertexCount]
                : null;

            for (int i = 0; i < newVertexCount; i++)
            {
                int oldIndex = usedVertexList[i];
                newPositions[i] = positions[oldIndex];
                if (newNormals != null)
                    newNormals[i] = normals[oldIndex];
                if (newTangents != null)
                    newTangents[i] = tangents[oldIndex];
                if (newUv0 != null)
                    newUv0[i] = uv0[oldIndex];
                if (newUv1 != null)
                    newUv1[i] = uv1[oldIndex];
                if (newColors != null)
                    newColors[i] = colors[oldIndex];
                if (newBoneWeights != null)
                    newBoneWeights[i] = boneWeights[oldIndex];
            }

            var mesh = new Mesh
            {
                name = meshName,
                indexFormat = newVertexCount > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            mesh.SetVertices(newPositions);
            if (newNormals != null)
                mesh.SetNormals(newNormals);
            if (newTangents != null)
                mesh.SetTangents(newTangents);
            if (newUv0 != null)
                mesh.SetUVs(0, newUv0);
            if (newUv1 != null)
                mesh.SetUVs(1, newUv1);
            if (newColors != null)
                mesh.SetColors(newColors);
            mesh.triangles = newTriangleList.ToArray();
            if (newBoneWeights != null)
                mesh.boneWeights = newBoneWeights;
            if (bindposes != null && bindposes.Length > 0)
                mesh.bindposes = bindposes;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 同骨子 Renderer
        /// </summary>
        private static SkinnedMeshRenderer CreateChildRenderer(
            Transform parent,
            string objectName,
            SkinnedMeshRenderer source,
            Mesh mesh)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var renderer = go.AddComponent<SkinnedMeshRenderer>();
            CopyRendererSettings(renderer, source, mesh, source.bones, source.rootBone);
            return renderer;
        }

        /// <summary>
        /// 拷贝蒙皮设置
        /// </summary>
        private static void CopyRendererSettings(
            SkinnedMeshRenderer target,
            SkinnedMeshRenderer source,
            Mesh mesh,
            Transform[] bones,
            Transform rootBone)
        {
            target.sharedMesh = mesh;
            target.bones = bones;
            target.rootBone = rootBone;
            target.sharedMaterials = source.sharedMaterials;
            target.updateWhenOffscreen = true;
            target.quality = source.quality;
            target.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            target.receiveShadows = false;
            target.skinnedMotionVectors = source.skinnedMotionVectors;
            target.localBounds = source.localBounds;
        }

        /// <summary>
        /// 设 Layer
        /// </summary>
        private static void ApplyLayerRecursive(GameObject root, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                return;

            var transformList = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transformList.Length; i++)
                transformList[i].gameObject.layer = layer;
        }

        /// <summary>
        /// 确保文件夹
        /// </summary>
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] partList = folderPath.Split('/');
            string current = partList[0];
            for (int i = 1; i < partList.Length; i++)
            {
                string next = current + "/" + partList[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, partList[i]);
                current = next;
            }
        }
    }
}
#endif
