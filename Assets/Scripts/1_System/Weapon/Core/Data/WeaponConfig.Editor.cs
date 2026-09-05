#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using cfg.item;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace DBWeaponSystem
{
    
    /// <summary>
    /// 武器挂点配表的Inspector编辑器
    /// </summary>
    public partial class WeaponConfig
    {
        /// <summary>
        /// 小刀分页列表
        /// </summary>
        [TabGroup("右手武器", "小刀", order: 0)]
        [ShowInInspector, LabelText("挂点列表")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, DraggableItems = true)]
        [OnCollectionChanged(nameof(OnKnifeListChanged))]
        [OnValueChanged(nameof(OnKnifeListValueChanged), IncludeChildren = true)]
        [NonSerialized]
        private List<WeaponHandPosDictEntry> editorKnifeList;

        /// <summary>
        /// 单手武器分页列表
        /// </summary>
        [TabGroup("右手武器", "单手武器", order: 1)]
        [ShowInInspector, LabelText("挂点列表")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, DraggableItems = true)]
        [OnCollectionChanged(nameof(OnSingleHandListChanged))]
        [OnValueChanged(nameof(OnSingleHandListValueChanged), IncludeChildren = true)]
        [NonSerialized]
        private List<WeaponHandPosDictEntry> editorSingleHandList;

        /// <summary>
        /// 双持武器分页列表
        /// </summary>
        [TabGroup("右手武器", "双持武器", order: 2)]
        [ShowInInspector, LabelText("挂点列表")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, DraggableItems = true)]
        [OnCollectionChanged(nameof(OnDoubleHandListChanged))]
        [OnValueChanged(nameof(OnDoubleHandListValueChanged), IncludeChildren = true)]
        [NonSerialized]
        private List<WeaponHandPosDictEntry> editorDoubleHandList;

        /// <summary>
        /// 左手道具挂点列表
        /// </summary>
        [TabGroup("左手道具", "道具栏", order: 0)]
        [ShowInInspector, LabelText("挂点列表")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, DraggableItems = true)]
        [OnCollectionChanged(nameof(OnLeftHandPropListChanged))]
        [OnValueChanged(nameof(OnLeftHandPropListValueChanged), IncludeChildren = true)]
        [NonSerialized]
        private List<WeaponHandPosDictEntry> editorLeftHandPropList;

        /// <summary>
        /// 食物水分挂点列表
        /// </summary>
        [TabGroup("食物水分", "配置栏", order: 0)]
        [ShowInInspector, LabelText("挂点列表")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, DraggableItems = true)]
        [OnCollectionChanged(nameof(OnFoodOrWaterListChanged))]
        [OnValueChanged(nameof(OnFoodOrWaterListValueChanged), IncludeChildren = true)]
        [NonSerialized]
        private List<FoodOrWaterHandPosDictEntry> editorFoodOrWaterList;

        /// <summary>
        /// 药品挂点列表
        /// </summary>
        [TabGroup("药品", "配置栏", order: 0)]
        [ShowInInspector, LabelText("挂点列表")]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true, DraggableItems = true)]
        [OnCollectionChanged(nameof(OnMedicineListChanged))]
        [OnValueChanged(nameof(OnMedicineListValueChanged), IncludeChildren = true)]
        [NonSerialized]
        private List<MedicineHandPosDictEntry> editorMedicineList;

        /// <summary> 刷新分页时跳过 OnValueChanged </summary>
        private bool isRefreshingEditorLists;

        /// <summary>
        /// Inspector 打开时从主列表回填分页
        /// </summary>
        [OnInspectorInit]
        private void OnWeaponConfigInspectorInit()
        {
            PrepareEditorLists();
        }

        /// <summary>
        /// Inspector 关闭时强制写回并标脏
        /// </summary>
        [OnInspectorDispose]
        private void OnWeaponConfigInspectorDispose()
        {
            CommitEditorLists();
        }

        /// <summary>
        /// 供独立编辑器窗口显式初始化分页列表
        /// </summary>
        public void PrepareEditorLists()
        {
            MigrateLegacyListIfNeeded();
            RefreshAllEditorTypeLists();
        }

        /// <summary>
        /// 供独立编辑器窗口保存前写回主列表
        /// </summary>
        public void CommitEditorLists()
        {
            FlushEditorListsToMaster();
        }

        /// <summary>
        /// 小刀列表结构变更
        /// </summary>
        private void OnKnifeListChanged()
        {
            if (isRefreshingEditorLists)
                return;
            SyncTypeListToMaster(EAnimationModelType.Knife, editorKnifeList);
        }

        /// <summary>
        /// 单手武器列表结构变更
        /// </summary>
        private void OnSingleHandListChanged()
        {
            if (isRefreshingEditorLists)
                return;
            SyncTypeListToMaster(EAnimationModelType.SingleHandWeapon, editorSingleHandList);
        }

        /// <summary>
        /// 双持武器列表结构变更
        /// </summary>
        private void OnDoubleHandListChanged()
        {
            if (isRefreshingEditorLists)
                return;
            SyncTypeListToMaster(EAnimationModelType.DoubleHandWeapon, editorDoubleHandList);
        }

        /// <summary>
        /// 左手道具列表结构变更
        /// </summary>
        private void OnLeftHandPropListChanged()
        {
            if (isRefreshingEditorLists)
                return;
            SyncLeftHandPropListToMaster();
        }

        /// <summary>
        /// 食物水分列表结构变更
        /// </summary>
        private void OnFoodOrWaterListChanged()
        {
            if (isRefreshingEditorLists)
                return;
            SyncFoodOrWaterListToMaster();
        }

        /// <summary>
        /// 药品列表结构变更
        /// </summary>
        private void OnMedicineListChanged()
        {
            if (isRefreshingEditorLists)
                return;
            SyncMedicineListToMaster();
        }

        /// <summary>
        /// 小刀字段变更写回
        /// </summary>
        private void OnKnifeListValueChanged()
        {
            MarkEditorConfigDirty();
        }

        /// <summary>
        /// 单手字段变更写回
        /// </summary>
        private void OnSingleHandListValueChanged()
        {
            MarkEditorConfigDirty();
        }

        /// <summary>
        /// 双持字段变更写回
        /// </summary>
        private void OnDoubleHandListValueChanged()
        {
            MarkEditorConfigDirty();
        }

        /// <summary>
        /// 左手道具备字段变更写回
        /// </summary>
        private void OnLeftHandPropListValueChanged()
        {
            MarkEditorConfigDirty();
        }

        /// <summary>
        /// 食物水分字段变更写回
        /// </summary>
        private void OnFoodOrWaterListValueChanged()
        {
            MarkEditorConfigDirty();
        }

        /// <summary>
        /// 药品字段变更写回
        /// </summary>
        private void OnMedicineListValueChanged()
        {
            MarkEditorConfigDirty();
        }

        /// <summary>
        /// 分页字段改动后标脏并刷新运行时字典
        /// </summary>
        private void MarkEditorConfigDirty()
        {
            if (isRefreshingEditorLists)
                return;

            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 刷新全部分页列表
        /// </summary>
        private void RefreshAllEditorTypeLists()
        {
            if (editorKnifeList == null)
                editorKnifeList = new List<WeaponHandPosDictEntry>();
            if (editorSingleHandList == null)
                editorSingleHandList = new List<WeaponHandPosDictEntry>();
            if (editorDoubleHandList == null)
                editorDoubleHandList = new List<WeaponHandPosDictEntry>();
            if (editorLeftHandPropList == null)
                editorLeftHandPropList = new List<WeaponHandPosDictEntry>();
            if (editorFoodOrWaterList == null)
                editorFoodOrWaterList = new List<FoodOrWaterHandPosDictEntry>();
            if (editorMedicineList == null)
                editorMedicineList = new List<MedicineHandPosDictEntry>();

            isRefreshingEditorLists = true;
            try
            {
                RefreshEditorTypeList(EAnimationModelType.Knife, editorKnifeList);
                RefreshEditorTypeList(EAnimationModelType.SingleHandWeapon, editorSingleHandList);
                RefreshEditorTypeList(EAnimationModelType.DoubleHandWeapon, editorDoubleHandList);
                RefreshLeftHandPropEditorList();
                RefreshFoodOrWaterEditorList();
                RefreshMedicineEditorList();
            }
            finally
            {
                isRefreshingEditorLists = false;
            }
        }

        /// <summary>
        /// 刷新左手道具编辑列表
        /// </summary>
        private void RefreshLeftHandPropEditorList()
        {
            editorLeftHandPropList.Clear();
            if (leftHandPropPosConfigDict == null)
                return;

            int count = leftHandPropPosConfigDict.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = leftHandPropPosConfigDict[i];
                // 直接引用主列表条目 字段改动即改 SO
                if (entry != null)
                    editorLeftHandPropList.Add(entry);
            }
        }

        /// <summary>
        /// 左手道具列表写回主列表
        /// </summary>
        private void SyncLeftHandPropListToMaster()
        {
            leftHandPropPosConfigDict = new List<WeaponHandPosDictEntry>();
            if (editorLeftHandPropList == null)
            {
                RebuildRuntimeDict();
                EditorUtility.SetDirty(this);
                return;
            }

            int count = editorLeftHandPropList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = editorLeftHandPropList[i];
                if (entry == null)
                {
                    entry = new WeaponHandPosDictEntry();
                    editorLeftHandPropList[i] = entry;
                }

                if (entry.WeaponName == EWeaponName.None)
                    continue;

                if (entry.Config == null)
                    entry.Config = new WeaponHandPosConfig();

                leftHandPropPosConfigDict.Add(entry);
            }

            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 刷新食物水分编辑列表
        /// </summary>
        private void RefreshFoodOrWaterEditorList()
        {
            editorFoodOrWaterList.Clear();
            if (foodOrWaterPosConfigDict == null)
                return;

            int count = foodOrWaterPosConfigDict.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = foodOrWaterPosConfigDict[i];
                if (entry != null)
                    editorFoodOrWaterList.Add(entry);
            }
        }

        /// <summary>
        /// 食物水分列表写回主列表
        /// </summary>
        private void SyncFoodOrWaterListToMaster()
        {
            foodOrWaterPosConfigDict = new List<FoodOrWaterHandPosDictEntry>();
            if (editorFoodOrWaterList == null)
            {
                RebuildRuntimeDict();
                EditorUtility.SetDirty(this);
                return;
            }

            int count = editorFoodOrWaterList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = editorFoodOrWaterList[i];
                if (entry == null)
                {
                    entry = new FoodOrWaterHandPosDictEntry();
                    editorFoodOrWaterList[i] = entry;
                }

                if (entry.ItemTableId <= 0)
                    continue;

                if (entry.FoodConfig == null)
                    entry.FoodConfig = new WeaponHandPosConfig();
                if (entry.UtensilConfig == null)
                    entry.UtensilConfig = new WeaponHandPosConfig();

                foodOrWaterPosConfigDict.Add(entry);
            }

            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 刷新药品编辑列表
        /// </summary>
        private void RefreshMedicineEditorList()
        {
            editorMedicineList.Clear();
            if (medicinePosConfigDict == null)
                return;

            int count = medicinePosConfigDict.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = medicinePosConfigDict[i];
                if (entry != null)
                    editorMedicineList.Add(entry);
            }
        }

        /// <summary>
        /// 药品列表写回主列表
        /// </summary>
        private void SyncMedicineListToMaster()
        {
            medicinePosConfigDict = new List<MedicineHandPosDictEntry>();
            if (editorMedicineList == null)
            {
                RebuildRuntimeDict();
                EditorUtility.SetDirty(this);
                return;
            }

            int count = editorMedicineList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = editorMedicineList[i];
                if (entry == null)
                {
                    entry = new MedicineHandPosDictEntry();
                    editorMedicineList[i] = entry;
                }

                if (entry.ItemTableId <= 0)
                    continue;

                if (entry.BodyConfig == null)
                    entry.BodyConfig = new WeaponHandPosConfig();

                medicinePosConfigDict.Add(entry);
            }

            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 从主列表读取某一武器类型分页
        /// </summary>
        private void RefreshEditorTypeList(
            EAnimationModelType weaponType,
            List<WeaponHandPosDictEntry> typeList)
        {
            typeList.Clear();
            if (rightHandPosConfigDict == null)
                return;

            int count = rightHandPosConfigDict.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = rightHandPosConfigDict[i];
                // 直接引用主列表条目 字段改动即改 SO
                if (entry != null && entry.AnimationType == weaponType)
                    typeList.Add(entry);
            }
        }

        /// <summary>
        /// 将分页列表写回主列表
        /// </summary>
        private void SyncTypeListToMaster(
            EAnimationModelType weaponType,
            List<WeaponHandPosDictEntry> typeList)
        {
            if (rightHandPosConfigDict == null)
                rightHandPosConfigDict = new List<WeaponHandPosDictEntry>();

            for (int i = rightHandPosConfigDict.Count - 1; i >= 0; i--)
            {
                var entry = rightHandPosConfigDict[i];
                if (entry != null && entry.AnimationType == weaponType)
                    rightHandPosConfigDict.RemoveAt(i);
            }

            if (typeList == null)
            {
                RebuildRuntimeDict();
                EditorUtility.SetDirty(this);
                return;
            }

            int count = typeList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = typeList[i];
                if (entry == null)
                {
                    entry = new WeaponHandPosDictEntry();
                    typeList[i] = entry;
                }

                if (entry.WeaponName == EWeaponName.None)
                    continue;

                entry.AnimationType = weaponType;
                if (entry.Config == null)
                    entry.Config = new WeaponHandPosConfig();

                rightHandPosConfigDict.Add(entry);
            }

            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 备份当前主列表到 JSON
        /// </summary>
        [HorizontalGroup("备份操作", order: -100)]
        [Button("备份", ButtonSizes.Medium)]
        private void BackupToJson()
        {
            FlushEditorListsToMaster();

            string backupAssetPath = GetBackupAssetPath();
            if (string.IsNullOrEmpty(backupAssetPath))
            {
                Debug.LogError("无法获取当前 WeaponConfig 资源路径");
                return;
            }

            var backupData = BuildBackupData();
            string json = JsonUtility.ToJson(backupData, true);
            string fullPath = ToFullProjectPath(backupAssetPath);

            string folder = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            File.WriteAllText(fullPath, json, new UTF8Encoding(false));
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log($"武器挂点配置已备份 {backupData.entries.Count} 条 -> {backupAssetPath}");
        }

        /// <summary>
        /// 从备份 JSON 回填主列表
        /// </summary>
        [HorizontalGroup("备份操作")]
        [Button("备份回填", ButtonSizes.Medium)]
        private void RestoreFromBackupJson()
        {
            string backupAssetPath = GetBackupAssetPath();
            if (string.IsNullOrEmpty(backupAssetPath))
            {
                Debug.LogError("无法获取当前 WeaponConfig 资源路径");
                return;
            }

            string fullPath = ToFullProjectPath(backupAssetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError("未找到备份文件 " + backupAssetPath);
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "备份回填",
                $"将从备份覆盖当前配置\n{backupAssetPath}\n确认继续",
                "确认回填",
                "取消");
            if (!confirm)
                return;

            string json = File.ReadAllText(fullPath, Encoding.UTF8);
            var backup = JsonUtility.FromJson<WeaponConfigBackupData>(json);
            if (backup == null)
            {
                Debug.LogError("备份 JSON 解析失败");
                return;
            }

            if (backup.entries != null && backup.entries.Count > 0)
            {
                ApplyBackupEntries(backup.entries);
            }
            else if (backup.leftHandPosConfigList != null && backup.leftHandPosConfigList.Count > 0)
            {
                ApplyLegacyBackupEntries(backup.leftHandPosConfigList);
            }
            else
            {
                Debug.LogError("备份 JSON 无有效数据");
                return;
            }

            if (backup.leftHandPropEntries != null)
                ApplyLeftHandPropBackupEntries(backup.leftHandPropEntries);

            if (backup.foodOrWaterEntries != null)
                ApplyFoodOrWaterBackupEntries(backup.foodOrWaterEntries);

            if (backup.medicineEntries != null)
                ApplyMedicineBackupEntries(backup.medicineEntries);

            RefreshAllEditorTypeLists();
            RebuildRuntimeDict();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            Debug.Log("已从备份回填武器挂点配置");
        }

        /// <summary>
        /// 将分页列表全部写回主列表
        /// </summary>
        private void FlushEditorListsToMaster()
        {
            SyncTypeListToMaster(EAnimationModelType.Knife, editorKnifeList);
            SyncTypeListToMaster(EAnimationModelType.SingleHandWeapon, editorSingleHandList);
            SyncTypeListToMaster(EAnimationModelType.DoubleHandWeapon, editorDoubleHandList);
            SyncLeftHandPropListToMaster();
            SyncFoodOrWaterListToMaster();
            SyncMedicineListToMaster();
        }

        /// <summary>
        /// 构建备份数据
        /// </summary>
        private WeaponConfigBackupData BuildBackupData()
        {
            var backupData = new WeaponConfigBackupData
            {
                assetPath = AssetDatabase.GetAssetPath(this),
                backupTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                entries = new List<WeaponHandPosBackupEntry>(),
                leftHandPropEntries = new List<WeaponHandPosBackupEntry>(),
                foodOrWaterEntries = new List<FoodOrWaterHandPosBackupEntry>(),
                medicineEntries = new List<MedicineHandPosBackupEntry>()
            };

            AppendBackupEntries(rightHandPosConfigDict, backupData.entries);
            AppendBackupEntries(leftHandPropPosConfigDict, backupData.leftHandPropEntries);
            AppendFoodOrWaterBackupEntries(foodOrWaterPosConfigDict, backupData.foodOrWaterEntries);
            AppendMedicineBackupEntries(medicinePosConfigDict, backupData.medicineEntries);
            return backupData;
        }

        /// <summary>
        /// 追加备份条目
        /// </summary>
        private static void AppendBackupEntries(
            List<WeaponHandPosDictEntry> sourceList,
            List<WeaponHandPosBackupEntry> targetList)
        {
            if (sourceList == null || targetList == null)
                return;

            int count = sourceList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = sourceList[i];
                if (entry == null || entry.WeaponName == EWeaponName.None)
                    continue;

                targetList.Add(new WeaponHandPosBackupEntry
                {
                    eWeaponName = (int)entry.WeaponName,
                    eAnimationType = (int)entry.AnimationType,
                    position = entry.Config.Position,
                    eulerAngles = entry.Config.EulerAngles,
                    scale = entry.Config.Scale,
                    scanRadius = entry.Config.ScanRadius,
                    distancePadding = entry.Config.DistancePadding
                });
            }
        }

        /// <summary>
        /// 应用新格式备份
        /// </summary>
        private void ApplyBackupEntries(List<WeaponHandPosBackupEntry> entryList)
        {
            rightHandPosConfigDict = new List<WeaponHandPosDictEntry>();
            int count = entryList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = entryList[i];
                if (entry == null || entry.eWeaponName == 0)
                    continue;

                int typeInt = entry.eAnimationType != 0 ? entry.eAnimationType : entry.eWeaponType;

                rightHandPosConfigDict.Add(new WeaponHandPosDictEntry
                {
                    WeaponName = (EWeaponName)entry.eWeaponName,
                    AnimationType = (EAnimationModelType)typeInt,
                    Config = new WeaponHandPosConfig
                    {
                        Position = entry.position,
                        EulerAngles = entry.eulerAngles,
                        Scale = entry.scale,
                        ScanRadius = entry.scanRadius > 0f ? entry.scanRadius : 0.2f,
                        DistancePadding = entry.distancePadding
                    }
                });
            }
        }

        /// <summary>
        /// 应用旧格式备份
        /// </summary>
        private void ApplyLegacyBackupEntries(List<WeaponHandPosLegacyBackupEntry> entryList)
        {
            rightHandPosConfigDict = new List<WeaponHandPosDictEntry>();
            int count = entryList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = entryList[i];
                if (entry == null)
                    continue;

                if (!TryParseWeaponName(entry.name, out EWeaponName weaponName))
                {
                    Debug.LogWarning("备份项无法解析武器名 " + entry.name);
                    continue;
                }

                int typeInt = entry.eAnimationType != 0 ? entry.eAnimationType : entry.eWeaponType;

                rightHandPosConfigDict.Add(new WeaponHandPosDictEntry
                {
                    WeaponName = weaponName,
                    AnimationType = (EAnimationModelType)typeInt,
                    Config = new WeaponHandPosConfig
                    {
                        Position = entry.position,
                        EulerAngles = entry.eulerAngles,
                        Scale = entry.scale,
                        ScanRadius = 0.2f,
                        DistancePadding = 0.02f
                    }       
                });
            }
        }

        /// <summary>
        /// 应用左手道具备份
        /// </summary>
        private void ApplyLeftHandPropBackupEntries(List<WeaponHandPosBackupEntry> entryList)
        {
            leftHandPropPosConfigDict = new List<WeaponHandPosDictEntry>();
            int count = entryList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = entryList[i];
                if (entry == null || entry.eWeaponName == 0)
                    continue;

                int typeInt = entry.eAnimationType != 0 ? entry.eAnimationType : entry.eWeaponType;
                leftHandPropPosConfigDict.Add(new WeaponHandPosDictEntry
                {
                    WeaponName = (EWeaponName)entry.eWeaponName,
                    AnimationType = (EAnimationModelType)typeInt,
                    Config = new WeaponHandPosConfig
                    {
                        Position = entry.position,
                        EulerAngles = entry.eulerAngles,
                        Scale = entry.scale,
                        ScanRadius = entry.scanRadius > 0f ? entry.scanRadius : 0.2f,
                        DistancePadding = entry.distancePadding
                    }
                });
            }
        }

        /// <summary>
        /// 追加食物水分备份条目
        /// </summary>
        private static void AppendFoodOrWaterBackupEntries(
            List<FoodOrWaterHandPosDictEntry> sourceList,
            List<FoodOrWaterHandPosBackupEntry> targetList)
        {
            if (sourceList == null || targetList == null)
                return;

            int count = sourceList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = sourceList[i];
                if (entry == null || entry.ItemTableId <= 0)
                    continue;

                targetList.Add(new FoodOrWaterHandPosBackupEntry
                {
                    itemTableId = entry.ItemTableId,
                    displayName = entry.DisplayName,
                    foodPosition = entry.FoodConfig != null ? entry.FoodConfig.Position : Vector3.zero,
                    foodEulerAngles = entry.FoodConfig != null ? entry.FoodConfig.EulerAngles : Vector3.zero,
                    foodScale = entry.FoodConfig != null ? entry.FoodConfig.Scale : Vector3.one,
                    utensilPosition = entry.UtensilConfig != null ? entry.UtensilConfig.Position : Vector3.zero,
                    utensilEulerAngles = entry.UtensilConfig != null ? entry.UtensilConfig.EulerAngles : Vector3.zero,
                    utensilScale = entry.UtensilConfig != null ? entry.UtensilConfig.Scale : Vector3.one
                });
            }
        }

        /// <summary>
        /// 应用食物水分备份
        /// </summary>
        private void ApplyFoodOrWaterBackupEntries(List<FoodOrWaterHandPosBackupEntry> entryList)
        {
            foodOrWaterPosConfigDict = new List<FoodOrWaterHandPosDictEntry>();
            int count = entryList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = entryList[i];
                if (entry == null || entry.itemTableId <= 0)
                    continue;

                foodOrWaterPosConfigDict.Add(new FoodOrWaterHandPosDictEntry
                {
                    ItemTableId = entry.itemTableId,
                    DisplayName = entry.displayName,
                    FoodConfig = new WeaponHandPosConfig
                    {
                        Position = entry.foodPosition,
                        EulerAngles = entry.foodEulerAngles,
                        Scale = entry.foodScale == Vector3.zero ? Vector3.one : entry.foodScale
                    },
                    UtensilConfig = new WeaponHandPosConfig
                    {
                        Position = entry.utensilPosition,
                        EulerAngles = entry.utensilEulerAngles,
                        Scale = entry.utensilScale == Vector3.zero ? Vector3.one : entry.utensilScale
                    }
                });
            }
        }

        /// <summary>
        /// 追加药品备份条目
        /// </summary>
        private static void AppendMedicineBackupEntries(
            List<MedicineHandPosDictEntry> sourceList,
            List<MedicineHandPosBackupEntry> targetList)
        {
            if (sourceList == null || targetList == null)
                return;

            int count = sourceList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = sourceList[i];
                if (entry == null || entry.ItemTableId <= 0)
                    continue;

                targetList.Add(new MedicineHandPosBackupEntry
                {
                    itemTableId = entry.ItemTableId,
                    displayName = entry.DisplayName,
                    handSide = (int)entry.HandSide,
                    bodyPosition = entry.BodyConfig != null ? entry.BodyConfig.Position : Vector3.zero,
                    bodyEulerAngles = entry.BodyConfig != null ? entry.BodyConfig.EulerAngles : Vector3.zero,
                    bodyScale = entry.BodyConfig != null ? entry.BodyConfig.Scale : Vector3.one
                });
            }
        }

        /// <summary>
        /// 应用药品备份
        /// </summary>
        private void ApplyMedicineBackupEntries(List<MedicineHandPosBackupEntry> entryList)
        {
            medicinePosConfigDict = new List<MedicineHandPosDictEntry>();
            int count = entryList.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = entryList[i];
                if (entry == null || entry.itemTableId <= 0)
                    continue;

                medicinePosConfigDict.Add(new MedicineHandPosDictEntry
                {
                    ItemTableId = entry.itemTableId,
                    DisplayName = entry.displayName,
                    HandSide = (EMedicineHandSide)entry.handSide,
                    BodyConfig = new WeaponHandPosConfig
                    {
                        Position = entry.bodyPosition,
                        EulerAngles = entry.bodyEulerAngles,
                        Scale = entry.bodyScale == Vector3.zero ? Vector3.one : entry.bodyScale
                    }
                });
            }
        }

        /// <summary>
        /// 迁移旧 List 格式
        /// </summary>
        private void MigrateLegacyListIfNeeded()
        {
            // Unity 反序列化后旧字段已不存在 仅处理空条目时从 asset 文本无法自动迁移
            if (rightHandPosConfigDict != null && rightHandPosConfigDict.Count > 0)
                return;

            RebuildRuntimeDict();
        }

        /// <summary>
        /// 字符串转武器名枚举
        /// </summary>
        private static bool TryParseWeaponName(string rawName, out EWeaponName weaponName)
        {
            weaponName = EWeaponName.None;
            if (string.IsNullOrWhiteSpace(rawName))
                return false;

            if (Enum.TryParse(rawName, out weaponName))
                return weaponName != EWeaponName.None;

            return false;
        }

        /// <summary>
        /// 获取当前资源对应备份路径
        /// </summary>
        private string GetBackupAssetPath()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            return Path.ChangeExtension(assetPath, ".backup.json");
        }

        /// <summary>
        /// Assets 相对路径转磁盘绝对路径
        /// </summary>
        private static string ToFullProjectPath(string assetRelativePath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectRoot, assetRelativePath));
        }

        [Serializable]
        private class WeaponConfigBackupData
        {
            public string assetPath;
            public string backupTime;
            public List<WeaponHandPosBackupEntry> entries;
            public List<WeaponHandPosBackupEntry> leftHandPropEntries;
            public List<FoodOrWaterHandPosBackupEntry> foodOrWaterEntries;
            public List<MedicineHandPosBackupEntry> medicineEntries;
            public List<WeaponHandPosLegacyBackupEntry> leftHandPosConfigList;
        }

        [Serializable]
        private class FoodOrWaterHandPosBackupEntry
        {
            public int itemTableId;
            public string displayName;
            public Vector3 foodPosition;
            public Vector3 foodEulerAngles;
            public Vector3 foodScale = Vector3.one;
            public Vector3 utensilPosition;
            public Vector3 utensilEulerAngles;
            public Vector3 utensilScale = Vector3.one;
        }

        [Serializable]
        private class MedicineHandPosBackupEntry
        {
            public int itemTableId;
            public string displayName;
            public int handSide;
            public Vector3 bodyPosition;
            public Vector3 bodyEulerAngles;
            public Vector3 bodyScale = Vector3.one;
        }

        [Serializable]
        private class WeaponHandPosBackupEntry
        {
            public int eWeaponName;
            public int eAnimationType;
            public int eWeaponType;
            public Vector3 position;
            public Vector3 eulerAngles;
            public Vector3 scale = Vector3.one;
            public float scanRadius = 0.2f;
            public float distancePadding = 0.02f;
        }

        [Serializable]
        private class WeaponHandPosLegacyBackupEntry
        {
            public int eAnimationType;
            public int eWeaponType;
            public string name;
            public Vector3 position;
            public Vector3 eulerAngles;
            public Vector3 scale = Vector3.one;
        }
    }
}
#endif
