using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_Budier
{
    [CreateAssetMenu(fileName = "BuilderSystemSetting", menuName = "Mm_Builder/BuilderSystemSetting")]
    public class BuilderSystemSetting : SerializedScriptableObject
    {
        public const string AssetPath = "Assets/Scripts/1_System/Builder/_Scripts/Mm_Builder/Scripts/BuilderSystem/So/Config/BuiderRuntimeSettings.asset";

        private static BuilderSystemSetting instance;

        public static BuilderSystemSetting Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_EDITOR
                    instance = UnityEditor.AssetDatabase.LoadAssetAtPath<BuilderSystemSetting>(AssetPath);
#endif
                }
                return instance;
            }
        }


        [Header("射线检测设置")] //此处可替换为其他检测方式 只需要提供相同参数即可
        [TitleGroup("射线检测设置"),LabelText("射线检测方块层级"), SerializeField]
        public LayerMask cubeLayer;
        [TitleGroup("射线检测设置"),LabelText("射线检测最大距离"), SerializeField]
        public float raycastMaxDistance;
        [TitleGroup("射线检测设置"),LabelText("地面层级"), SerializeField]
        public LayerMask groundLayer;
        [TitleGroup("射线检测设置"), LabelText("最大命中缓存数量"), MinValue(1), SerializeField]
        public int maxRaycastHitCount = 16;

        [Header("放置碰撞检测")]
        [TitleGroup("放置碰撞检测"), LabelText("启用放置碰撞检测"), SerializeField]
        public bool enablePlacementCollisionCheck = true;
        [TitleGroup("放置碰撞检测"), LabelText("放置阻挡层级"), SerializeField]
        public LayerMask placementBlockerLayer = LayerMask.GetMask("Cube");
        [TitleGroup("放置碰撞检测"), LabelText("碰撞检测边缘余量"), MinValue(0f), SerializeField]
        public float placementCollisionPadding = 0.01f;
        [TitleGroup("放置碰撞检测"), LabelText("最大碰撞命中数量"), MinValue(1), SerializeField]
        public int maxPlacementColliderCount = 16;

        [TitleGroup("占格缓存"), LabelText("占格列表最大容量"), MinValue(1), SerializeField]
        public int maxOccupiedGridCount = 8;


        [Header("预览材质")]
        [TitleGroup("预览材质"),LabelText("可放置预览材质"), SerializeField]
        public Material preTrueMaterial;
        [TitleGroup("预览材质"),LabelText("不可放置预览材质"), SerializeField]
        public Material preFalseMaterial;

        [TitleGroup("数据保存设置")]
        [LabelText("存档子目录名"), SerializeField]
        public string saveFolderName = "BuilderSystemData";

        [TitleGroup("数据保存设置")]
        [LabelText("方块存档文件名"), SerializeField]
        public string buildFileName = "build.json";

        [TitleGroup("数据保存设置")]
        [LabelText("分区配置文件名"), SerializeField]
        public string gridGroupsFileName = "grid-groups.json";

        [TitleGroup("数据保存设置"), LabelText("所有方块数据"), SerializeField]
        public List<CubeData> allCubeDataList = new();

        /// <summary>
        /// persistentDataPath 下的存档根目录
        /// </summary>
        public string GetSaveDirectory()
        {
            var folder = string.IsNullOrWhiteSpace(saveFolderName) ? "BuilderSystemData" : saveFolderName;
            return Path.Combine(Application.persistentDataPath, folder);
        }

        /// <summary>
        /// 方块摆放 JSON 完整路径
        /// </summary>
        public string GetBuildFilePath()
        {
            var fileName = string.IsNullOrWhiteSpace(buildFileName) ? "build.json" : buildFileName;
            return Path.Combine(GetSaveDirectory(), fileName);
        }

        /// <summary>
        /// 分区 grid-groups JSON 完整路径
        /// </summary>
        public string GetGridGroupsFilePath()
        {
            var fileName = string.IsNullOrWhiteSpace(gridGroupsFileName) ? "grid-groups.json" : gridGroupsFileName;
            return Path.Combine(GetSaveDirectory(), fileName);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 扫描工程内所有 CubeData 资产 写入列表 供存读档反查预制体
        /// </summary>
        [TitleGroup("数据保存设置")]
        [Button("扫描并注册所有方块数据", ButtonSizes.Medium)]
        public void RegisterAllCubeData()
        {
            allCubeDataList.Clear();
            var seenTypes = new HashSet<ECubeType>();

            var guids = UnityEditor.AssetDatabase.FindAssets("t:CubeData");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<CubeData>(path);
                if (data == null)
                    continue;

                if (!seenTypes.Add(data.CubeType))
                {
                    Debug.LogWarning($"[注册] 方块类型 {data.CubeType} 重复，已跳过：{path}");
                    continue;
                }

                allCubeDataList.Add(data);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[注册] 完成，共注册 {allCubeDataList.Count} 种方块");
        }
#endif
    }
}
