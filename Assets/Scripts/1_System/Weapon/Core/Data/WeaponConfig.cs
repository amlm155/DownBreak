using System;
using System.Collections.Generic;
using cfg.item;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DBWeaponSystem
{
    /// <summary>
    /// 武器挂点配置表
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "DBProjectConfig/WeaponConfig", order = 1)]
    public partial class WeaponConfig : ScriptableObject
    {
        [HideInInspector]
        [SerializeField]
        /// <summary> 右手武器挂点列表 </summary>
        private List<WeaponHandPosDictEntry> rightHandPosConfigDict = new List<WeaponHandPosDictEntry>();

        [HideInInspector]
        [SerializeField]
        /// <summary> 左手道具挂点列表 </summary>
        private List<WeaponHandPosDictEntry> leftHandPropPosConfigDict = new List<WeaponHandPosDictEntry>();

        [HideInInspector]
        [SerializeField]
        /// <summary> 食物水分挂点列表 </summary>
        private List<FoodOrWaterHandPosDictEntry> foodOrWaterPosConfigDict = new List<FoodOrWaterHandPosDictEntry>();

        [HideInInspector]
        [SerializeField]
        /// <summary> 药品挂点列表 </summary>
        private List<MedicineHandPosDictEntry> medicinePosConfigDict = new List<MedicineHandPosDictEntry>();

        /// <summary>
        /// 运行时查询缓存 右手武器
        /// </summary>
        private Dictionary<EWeaponName, WeaponHandPosConfig> rightHandPosRuntimeDict;

        /// <summary>
        /// 运行时查询缓存 左手道具
        /// </summary>
        private Dictionary<EWeaponName, WeaponHandPosConfig> leftHandPropPosRuntimeDict;

        /// <summary>
        /// 运行时查询缓存 食物水分
        /// </summary>
        private Dictionary<int, FoodOrWaterHandPosDictEntry> foodOrWaterPosRuntimeDict;

        /// <summary>
        /// 运行时查询缓存 药品
        /// </summary>
        private Dictionary<int, MedicineHandPosDictEntry> medicinePosRuntimeDict;

        /// <summary>
        /// 序列化字典条目 右手武器挂点
        /// </summary>
        public List<WeaponHandPosDictEntry> RightHandPosConfigDict
        {
            get => rightHandPosConfigDict;
            set => rightHandPosConfigDict = value;
        }

        /// <summary>
        /// 序列化字典条目 左手道具挂点
        /// </summary>
        public List<WeaponHandPosDictEntry> LeftHandPropPosConfigDict
        {
            get => leftHandPropPosConfigDict;
            set => leftHandPropPosConfigDict = value;
        }

        /// <summary>
        /// 序列化字典条目 食物水分挂点
        /// </summary>
        public List<FoodOrWaterHandPosDictEntry> FoodOrWaterPosConfigDict
        {
            get => foodOrWaterPosConfigDict;
            set => foodOrWaterPosConfigDict = value;
        }

        /// <summary>
        /// 序列化字典条目 药品挂点
        /// </summary>
        public List<MedicineHandPosDictEntry> MedicinePosConfigDict
        {
            get => medicinePosConfigDict;
            set => medicinePosConfigDict = value;
        }

        private void OnEnable()
        {
            RebuildRuntimeDict();
        }

        /// <summary>
        /// 重建运行时字典
        /// </summary>
        public void RebuildRuntimeDict()
        {
            RebuildOneRuntimeDict(rightHandPosConfigDict, ref rightHandPosRuntimeDict);
            RebuildOneRuntimeDict(leftHandPropPosConfigDict, ref leftHandPropPosRuntimeDict);
            RebuildFoodOrWaterRuntimeDict();
            RebuildMedicineRuntimeDict();
        }

        /// <summary>
        /// 重建单个运行时字典
        /// </summary>
        private static void RebuildOneRuntimeDict(
            List<WeaponHandPosDictEntry> entryList,
            ref Dictionary<EWeaponName, WeaponHandPosConfig> runtimeDict)
        {
            if (runtimeDict == null)
                runtimeDict = new Dictionary<EWeaponName, WeaponHandPosConfig>();
            else
                runtimeDict.Clear();

            if (entryList == null)
                return;

            int count = entryList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = entryList[i];
                if (entry == null || entry.WeaponName == EWeaponName.None)
                    continue;

                runtimeDict[entry.WeaponName] = entry.Config;
            }
        }

        /// <summary>
        /// 按武器名匹配右手挂点配置
        /// </summary>
        public bool TryGetHandPosConfig(EWeaponName weaponName, out WeaponHandPosConfig config)
        {
            return TryGetPosConfig(weaponName, ref rightHandPosRuntimeDict, rightHandPosConfigDict, out config);
        }

        /// <summary>
        /// 按道具名匹配左手挂点配置
        /// </summary>
        public bool TryGetLeftHandPropPosConfig(EWeaponName weaponName, out WeaponHandPosConfig config)
        {
            return TryGetPosConfig(weaponName, ref leftHandPropPosRuntimeDict, leftHandPropPosConfigDict, out config);
        }

        /// <summary>
        /// 按物品表 ID 匹配食物水分挂点
        /// </summary>
        public bool TryGetFoodOrWaterPosConfig(int itemTableId, out FoodOrWaterHandPosDictEntry entry)
        {
            entry = null;
            if (itemTableId <= 0)
                return false;

            if (foodOrWaterPosRuntimeDict == null)
                RebuildFoodOrWaterRuntimeDict();

            return foodOrWaterPosRuntimeDict.TryGetValue(itemTableId, out entry);
        }

        /// <summary>
        /// 按物品表 ID 匹配药品挂点
        /// </summary>
        public bool TryGetMedicinePosConfig(int itemTableId, out MedicineHandPosDictEntry entry)
        {
            entry = null;
            if (itemTableId <= 0)
                return false;

            if (medicinePosRuntimeDict == null)
                RebuildMedicineRuntimeDict();

            return medicinePosRuntimeDict.TryGetValue(itemTableId, out entry);
        }

        /// <summary>
        /// 重建食物水分运行时字典
        /// </summary>
        private void RebuildFoodOrWaterRuntimeDict()
        {
            if (foodOrWaterPosRuntimeDict == null)
                foodOrWaterPosRuntimeDict = new Dictionary<int, FoodOrWaterHandPosDictEntry>();
            else
                foodOrWaterPosRuntimeDict.Clear();

            if (foodOrWaterPosConfigDict == null)
                return;

            int count = foodOrWaterPosConfigDict.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = foodOrWaterPosConfigDict[i];
                if (entry == null || entry.ItemTableId <= 0)
                    continue;

                foodOrWaterPosRuntimeDict[entry.ItemTableId] = entry;
            }
        }

        /// <summary>
        /// 重建药品运行时字典
        /// </summary>
        private void RebuildMedicineRuntimeDict()
        {
            if (medicinePosRuntimeDict == null)
                medicinePosRuntimeDict = new Dictionary<int, MedicineHandPosDictEntry>();
            else
                medicinePosRuntimeDict.Clear();

            if (medicinePosConfigDict == null)
                return;

            int count = medicinePosConfigDict.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = medicinePosConfigDict[i];
                if (entry == null || entry.ItemTableId <= 0)
                    continue;

                medicinePosRuntimeDict[entry.ItemTableId] = entry;
            }
        }

        /// <summary>
        /// 按名匹配挂点配置
        /// </summary>
        private bool TryGetPosConfig(
            EWeaponName weaponName,
            ref Dictionary<EWeaponName, WeaponHandPosConfig> runtimeDict,
            List<WeaponHandPosDictEntry> sourceList,
            out WeaponHandPosConfig config)
        {
            config = null;
            if (weaponName == EWeaponName.None)
                return false;

            if (runtimeDict == null)
                RebuildOneRuntimeDict(sourceList, ref runtimeDict);

            return runtimeDict.TryGetValue(weaponName, out config);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 写入或更新右手挂点配置
        /// </summary>
        public bool SetHandPosConfig(
            EWeaponName weaponName,
            EAnimationModelType animationType,
            WeaponHandPosConfig config)
        {
            return SetPosConfig(
                ref rightHandPosConfigDict,
                weaponName,
                animationType,
                config);
        }

        /// <summary>
        /// 写入或更新左手道具挂点配置
        /// </summary>
        public bool SetLeftHandPropPosConfig(
            EWeaponName weaponName,
            EAnimationModelType animationType,
            WeaponHandPosConfig config)
        {
            return SetPosConfig(
                ref leftHandPropPosConfigDict,
                weaponName,
                animationType,
                config);
        }

        /// <summary>
        /// 写入或更新挂点配置到指定列表
        /// </summary>
        private bool SetPosConfig(
            ref List<WeaponHandPosDictEntry> entryList,
            EWeaponName weaponName,
            EAnimationModelType animationType,
            WeaponHandPosConfig config)
        {
            if (weaponName == EWeaponName.None || config == null)
                return false;

            if (entryList == null)
                entryList = new List<WeaponHandPosDictEntry>();

            int count = entryList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = entryList[i];
                if (entry == null || entry.WeaponName != weaponName)
                    continue;

                entry.AnimationType = animationType;
                entry.Config = config;
                RebuildRuntimeDict();
                EditorUtility.SetDirty(this);
                return true;
            }

            entryList.Add(new WeaponHandPosDictEntry
            {
                WeaponName = weaponName,
                AnimationType = animationType,
                Config = config
            });
            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
            return true;
        }

        /// <summary>
        /// 写入或更新食物水分挂点
        /// </summary>
        public bool SetFoodOrWaterPosConfig(FoodOrWaterHandPosDictEntry entry)
        {
            if (entry == null || entry.ItemTableId <= 0)
                return false;

            if (foodOrWaterPosConfigDict == null)
                foodOrWaterPosConfigDict = new List<FoodOrWaterHandPosDictEntry>();

            int count = foodOrWaterPosConfigDict.Count;
            for (int i = 0; i < count; i++)
            {
                var old = foodOrWaterPosConfigDict[i];
                if (old == null || old.ItemTableId != entry.ItemTableId)
                    continue;

                foodOrWaterPosConfigDict[i] = entry;
                RebuildRuntimeDict();
                EditorUtility.SetDirty(this);
                return true;
            }

            foodOrWaterPosConfigDict.Add(entry);
            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
            return true;
        }

        /// <summary>
        /// 写入或更新药品挂点
        /// </summary>
        public bool SetMedicinePosConfig(MedicineHandPosDictEntry entry)
        {
            if (entry == null || entry.ItemTableId <= 0)
                return false;

            if (medicinePosConfigDict == null)
                medicinePosConfigDict = new List<MedicineHandPosDictEntry>();

            int count = medicinePosConfigDict.Count;
            for (int i = 0; i < count; i++)
            {
                var old = medicinePosConfigDict[i];
                if (old == null || old.ItemTableId != entry.ItemTableId)
                    continue;

                medicinePosConfigDict[i] = entry;
                RebuildRuntimeDict();
                EditorUtility.SetDirty(this);
                return true;
            }

            medicinePosConfigDict.Add(entry);
            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
            return true;
        }
#endif

    }

    /// <summary>
    /// 食物水分挂点字典条目 按物品表 ID
    /// </summary>
    [Serializable]
    public class FoodOrWaterHandPosDictEntry
    {
        [SerializeField, LabelText("物品表ID")]
        private int itemTableId;

        [SerializeField, LabelText("备注名")]
        private string displayName;

        [SerializeField, LabelText("食物左手")]
        private WeaponHandPosConfig foodConfig = new WeaponHandPosConfig();

        [SerializeField, LabelText("餐具右手")]
        private WeaponHandPosConfig utensilConfig = new WeaponHandPosConfig();

        public int ItemTableId
        {
            get => itemTableId;
            set => itemTableId = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public WeaponHandPosConfig FoodConfig
        {
            get => foodConfig;
            set => foodConfig = value;
        }

        public WeaponHandPosConfig UtensilConfig
        {
            get => utensilConfig;
            set => utensilConfig = value;
        }
    }

    /// <summary>
    /// 药品挂点字典条目 按物品表 ID 可选左右手
    /// </summary>
    [Serializable]
    public class MedicineHandPosDictEntry
    {
        [SerializeField, LabelText("物品表ID")]
        private int itemTableId;

        [SerializeField, LabelText("备注名")]
        private string displayName;

        [SerializeField, LabelText("持握手")]
        [EnumToggleButtons]
        private EMedicineHandSide handSide = EMedicineHandSide.Left;

        [SerializeField, LabelText("本体偏移")]
        private WeaponHandPosConfig bodyConfig = new WeaponHandPosConfig();

        public int ItemTableId
        {
            get => itemTableId;
            set => itemTableId = value;
        }

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public EMedicineHandSide HandSide
        {
            get => handSide;
            set => handSide = value;
        }

        public WeaponHandPosConfig BodyConfig
        {
            get => bodyConfig;
            set => bodyConfig = value;
        }
    }

    /// <summary>
    /// 药品持握手
    /// </summary>
    public enum EMedicineHandSide
    {
        [LabelText("左手")]
        Left = 0,
        [LabelText("右手")]
        Right = 1,
    }

    /// <summary>
    /// 武器挂点字典条目
    /// </summary>
    [Serializable]
    public class WeaponHandPosDictEntry
    {
        [SerializeField, LabelText("名称")]
        private EWeaponName weaponName;

        [FormerlySerializedAs("weaponType")]
        [SerializeField, LabelText("动画模组")]
        private EAnimationModelType animationType;

        [SerializeField, LabelText("挂点数据")]
        private WeaponHandPosConfig config = new WeaponHandPosConfig();

        public EWeaponName WeaponName
        {
            get => weaponName;
            set => weaponName = value;
        }

        public EAnimationModelType AnimationType
        {
            get => animationType;
            set => animationType = value;
        }

        public WeaponHandPosConfig Config
        {
            get => config;
            set => config = value;
        }
    }

    /// <summary>
    /// 武器挂点变换数据
    /// </summary>
    [Serializable]
    public class WeaponHandPosConfig
    {
        [SerializeField, LabelText("本地位置")]
        private Vector3 position;

        [SerializeField, LabelText("本地欧拉角")]
        private Vector3 eulerAngles;

        [SerializeField, LabelText("本地缩放")]
        private Vector3 scale = Vector3.one;

        [SerializeField, LabelText("扫描球半径")]
        private float scanRadius = 0.2f;

        [SerializeField, LabelText("额外位移距离")]
        private float distancePadding = 0.02f;

#if UNITY_EDITOR
        [SerializeField, LabelText("采样Transform 运行时拖武器")]
        private Transform sampleSource;
#endif

        public Vector3 Position
        {
            get => position;
            set => position = value;
        }

        public Vector3 EulerAngles
        {
            get => eulerAngles;
            set => eulerAngles = value;
        }

        public Quaternion Rotation => Quaternion.Euler(eulerAngles);

        public Vector3 Scale
        {
            get => scale;
            set => scale = value;
        }

        public float ScanRadius
        {
            get => scanRadius;
            set => scanRadius = Mathf.Max(0.01f, value);
        }

        public float DistancePadding
        {
            get => distancePadding;
            set => distancePadding = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 从Transform本地变换写入配置
        /// </summary>
        public void CopyFromTransform(Transform source)
        {
            if (source == null)
                return;

            position = source.localPosition;
            eulerAngles = source.localEulerAngles;
            scale = source.localScale;
        }

#if UNITY_EDITOR
        [Button("从采样Transform复制本地变换"), PropertyOrder(100)]
        private void EditorCopyFromSampleSource()
        {
            if (sampleSource == null)
            {
                Debug.LogWarning("请先拖入采样Transform");
                return;
            }

            CopyFromTransform(sampleSource);
            if (Selection.activeObject is WeaponConfig weaponConfig)
            {
                weaponConfig.RebuildRuntimeDict();
                EditorUtility.SetDirty(weaponConfig);
            }
        }
#endif
    }
}
