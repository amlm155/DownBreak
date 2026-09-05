using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 编辑器下方块配置
    /// </summary>
    [Serializable]
    public class CubeTypeEntry
    {
        public string Name;
        public int Value;
        public string Comment;
        public string Group;
        public bool CreateCubeData = true;
        public string CubeAssetName;
        public GameObject CubePrefab;
    }

    /// <summary>
    /// 建造系统 编辑器窗口设置
    /// </summary> 
    [CreateAssetMenu(fileName = "BuilderEditorSettings", menuName = "Mm_Builder/BuilderEditorSettings")]
    public class BuilderEditorSettings : SerializedScriptableObject
    {
        public const string AssetPath = "Assets/Scripts/1_System/Builder/_Scripts/Mm_Builder/Scripts/BuilderSystem/So/Config/BuilderEditorSettings.asset";
        public const string DefaultEnumPath = "Assets/Scripts/1_System/Builder/_Scripts/Mm_Builder/Scripts/BuilderSystem/Scripts/Data/ECubeType.cs";
        public const string DefaultCubeDataFolder = "Assets/Scripts/1_System/Builder/_Scripts/Mm_Builder/Scripts/BuilderSystem/So/CubeConfig";

        [HideInInspector]
        public List<CubeTypeEntry> Entries = new();

        [HideInInspector]
        public List<string> CustomGroups = new();

        [HideInInspector]
        public List<string> EnumNamePresets = new();

        public string EnumOutputFolder = "Assets/Scripts/1_System/Builder/_Scripts/Mm_Builder/Scripts/BuilderSystem/Scripts/Data";

        public string EnumFileName = "ECubeType.cs";

        public string CubeDataOutputFolder = DefaultCubeDataFolder;

        [LabelText("命名空间")]
        public string Namespace = "Mm_Budier";

        [LabelText("枚举类名")]
        public string EnumClassName = "ECubeType";

        public string EnumOutputPath => $"{EnumOutputFolder}/{EnumFileName}";
    }
}
