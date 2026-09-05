using System;
using System.Collections.Generic;
using System.IO;
using cfg.location;
using Luban.SimpleJSON;
using Newtonsoft.Json;
using UnityEngine;

namespace DBLocation
{
    /// <summary>
    /// 本地化服务 负责语言选择 配置缓存与文本查询
    /// </summary>
    public sealed class LocalizationSystem : ILocalization
    {
        /// <summary> 语言缓存文件名 </summary>
        private const string LanguageCacheFileName = "localization.json";

        /// <summary> Luban 配置相对目录 </summary>
        private const string ConfigRelativeDir = "Config/Item";

        /// <summary> 默认语言代码 </summary>
        private const string DefaultLanguageCode = "zh_CN";

        /// <summary> 语言代码到回退语言代码的映射 </summary>
        private readonly Dictionary<string, string> fallbackDict = new(StringComparer.Ordinal);

        /// <summary> 语言代码到文本字典的映射 </summary>
        private readonly Dictionary<string, Dictionary<string, string>> textDict = new(StringComparer.Ordinal);

        /// <summary> 是否已经完成表数据缓存 </summary>
        private bool loaded;

        /// <summary> 当前语言代码 </summary>
        public string LanguageCode { get; private set; }

        /// <summary>
        /// 创建本地化服务并读取缓存语言
        /// </summary>
        public LocalizationSystem()
        {
            LanguageCode = LoadLanguage();
        }

        /// <summary>
        /// 切换语言并通知本地化组件
        /// </summary>
        public void SetLanguage(string languageCode)
        {
            if (LanguageCode == languageCode)
                return;

            LanguageCode = languageCode;
            SaveLanguage(languageCode);
            LocalizedText.RefreshAll();
        }

        /// <summary>
        /// 查询本地化文本 找不到时返回 Key
        /// </summary>
        public string Get(string key)
        {
            return TryGet(key, out string text) ? text : key;
        }

        /// <summary>
        /// 查询本地化文本
        /// </summary>
        public bool TryGet(string key, out string text)
        {
            text = null;
            return !string.IsNullOrEmpty(key) && TryGetText(LanguageCode, key, out text);
        }

        /// <summary>
        /// 读取本地缓存语言
        /// </summary>
        private static string LoadLanguage()
        {
            string path = Path.Combine(Application.persistentDataPath, LanguageCacheFileName);
            if (!File.Exists(path))
                return DefaultLanguageCode;

            var cache = JsonConvert.DeserializeObject<LanguageCache>(File.ReadAllText(path));
            return cache == null || string.IsNullOrEmpty(cache.LanguageCode)
                ? DefaultLanguageCode
                : cache.LanguageCode;
        }

        /// <summary>
        /// 保存本地缓存语言
        /// </summary>
        private static void SaveLanguage(string languageCode)
        {
            string path = Path.Combine(Application.persistentDataPath, LanguageCacheFileName);
            var cache = new LanguageCache { LanguageCode = languageCode };
            File.WriteAllText(path, JsonConvert.SerializeObject(cache));
        }

        /// <summary>
        /// 确保语言表和文本表只加载一次
        /// </summary>
        private void EnsureLoaded()
        {
            if (loaded)
                return;

            loaded = true;

            // 语言表负责语言代码和回退关系 文本表负责每个 Key 的多语言列
            var languageTable = new TbLanguage(LoadJson("location_tblanguage"));
            foreach (var row in languageTable.DataList)
                fallbackDict[row.Code] = row.FallbackCode;

            var localizationTable = new TbLocalization(LoadJson("location_tblocalization"));
            foreach (var row in localizationTable.DataList)
            {
                // 表格是宽结构 这里把每个语言列拆成运行时查询字典
                string fullKey = row.Module + "." + row.Key;
                AddText(fullKey, "zh_CN", row.ZhCn);
                AddText(fullKey, "en_US", row.EnUs);
                AddText(fullKey, "ja_JP", row.JaJp);
                AddText(fullKey, "ko_KR", row.KoKr);
                AddText(fullKey, "fr_FR", row.FrFr);
            }
        }

        /// <summary>
        /// 写入指定语言的文本缓存
        /// </summary>
        private void AddText(string key, string languageCode, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (!textDict.TryGetValue(languageCode, out var languageDict))
            {
                // 语言列第一次出现时创建对应的 Key 文本缓存
                languageDict = new Dictionary<string, string>(StringComparer.Ordinal);
                textDict.Add(languageCode, languageDict);
            }

            languageDict[key] = text;
        }

        /// <summary>
        /// 查询文本 当前语言没有时使用回退语言
        /// </summary>
        private bool TryGetText(string languageCode, string key, out string text)
        {
            if (textDict.TryGetValue(languageCode, out var languageDict)
                && languageDict.TryGetValue(key, out text))
                return true;

            // 当前语言缺少翻译时使用 Language 表中配置的回退语言
            if (fallbackDict.TryGetValue(languageCode, out var fallbackCode)
                && fallbackCode != languageCode
                && textDict.TryGetValue(fallbackCode, out languageDict)
                && languageDict.TryGetValue(key, out text))
                return true;

            text = null;
            return false;
        }

        /// <summary>
        /// 读取 StreamingAssets 下的 Luban JSON
        /// </summary>
        private static JSONNode LoadJson(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, ConfigRelativeDir, fileName + ".json");
            if (!File.Exists(path))
                throw new FileNotFoundException("Luban 本地化配置缺失", path);

            return JSON.Parse(File.ReadAllText(path));
        }

        private sealed class LanguageCache
        {
            public string LanguageCode;
        }
    }
}
