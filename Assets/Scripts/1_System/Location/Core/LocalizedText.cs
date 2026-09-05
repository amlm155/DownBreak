using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DBLocation
{
    /// <summary>
    /// TMP 本地化组件
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        /// <summary> 当前场景中已启用的本地化组件 </summary>
        private static readonly HashSet<LocalizedText> activeTexts = new();

        /// <summary> 文本Key 使用 模块.Key 格式 </summary>
        [SerializeField]
        private string key;

        /// <summary> TMP 文本组件 </summary>
        private TMP_Text textComponent;

        /// <summary> 文本Key </summary>
        public string Key
        {
            get => key;
            set
            {
                key = value;
                Refresh();
            }
        }

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            // 注册后语言切换可以一次刷新所有可见文本
            activeTexts.Add(this);
            Refresh();
        }

        private void Start()
        {
            // GameFlow 在 Awake 阶段注册服务 因此这里补一次启动后的刷新
            Refresh();
        }

        private void OnDisable()
        {
            // 静态集合不能保留已经禁用的 UI 对象
            activeTexts.Remove(this);
        }

        /// <summary>
        /// 刷新当前 TMP 文本
        /// </summary>
        public void Refresh()
        {
            if (textComponent == null)
                return;

            var localization = DBGameSystem.GameHub.Get<ILocalization>();
            if (localization != null)
                textComponent.text = localization.Get(key);
        }

        /// <summary>
        /// 刷新所有已启用的本地化组件
        /// </summary>
        public static void RefreshAll()
        {
            foreach (var localizedText in activeTexts)
                localizedText.Refresh();
        }
    }
}
