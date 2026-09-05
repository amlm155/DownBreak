using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// 分区 grid-groups.json 读写 以及场景 BuilderVirtualGrid 与页签数据之间的拷贝
    /// </summary>
    internal static class GridGroupsConfigIO
    {
        [Serializable]
        private class GridGroupsFile
        {
            public List<BuilderVirtualGridGroup> groups = new();
        }

        public static string GetFilePath()
        {
            var setting = BuilderSystemSetting.Instance;
            return setting != null
                ? setting.GetGridGroupsFilePath()
                : Path.Combine(Application.persistentDataPath, "BuilderSystemData", "grid-groups.json");
        }

        public static List<BuilderVirtualGridGroup> Capture(BuilderVirtualGrid grid)
        {
            var groups = new List<BuilderVirtualGridGroup>();
            if (grid?.gridGroups == null)
                return groups;

            foreach (var group in grid.gridGroups)
            {
                if (group != null)
                    groups.Add(group.Clone());
            }

            return groups;
        }

        public static void Apply(BuilderVirtualGrid grid, List<BuilderVirtualGridGroup> groups)
        {
            if (grid == null)
                return;

            grid.gridGroups ??= new List<BuilderVirtualGridGroup>();
            grid.gridGroups.Clear();

            if (groups == null)
                return;

            foreach (var group in groups)
            {
                if (group != null)
                    grid.gridGroups.Add(group.Clone());
            }
        }

        public static BuilderVirtualGrid FindFirstVirtualGridInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var grid = root.GetComponentInChildren<BuilderVirtualGrid>(true);
                if (grid != null)
                    return grid;
            }

            return null;
        }

        public static bool TryLoad(out List<BuilderVirtualGridGroup> groups, out string error)
        {
            groups = new List<BuilderVirtualGridGroup>();
            error = null;
            var path = GetFilePath();

            if (!File.Exists(path))
            {
                error = $"文件不存在：{path}";
                return false;
            }

            try
            {
                var file = JsonConvert.DeserializeObject<GridGroupsFile>(File.ReadAllText(path));
                if (file == null)
                {
                    error = "JSON 解析结果为空";
                    return false;
                }

                groups = file.groups ?? new List<BuilderVirtualGridGroup>();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TrySave(List<BuilderVirtualGridGroup> groups, out string error)
        {
            error = null;
            var path = GetFilePath();
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var file = new GridGroupsFile
                {
                    groups = groups ?? new List<BuilderVirtualGridGroup>(),
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(file, Formatting.Indented));
                Debug.Log($"[GridGroups] 已保存 {file.groups.Count} 个分区 -> {path}");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void RevealInExplorer()
        {
            var path = GetFilePath();
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
                return;

            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(path);
        }
    }
}
