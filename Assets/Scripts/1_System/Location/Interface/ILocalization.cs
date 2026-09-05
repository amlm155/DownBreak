using DBGameSystem;

namespace DBLocation
{
    /// <summary>
    /// 本地化服务门面
    /// </summary>
    public interface ILocalization : IGameService
    {
        /// <summary> 当前语言 </summary>
        string LanguageCode { get; }

        /// <summary>
        /// 切换语言
        /// </summary>
        void SetLanguage(string languageCode);

        /// <summary>
        /// 查询本地化文本
        /// </summary>
        string Get(string key);

        /// <summary>
        /// 查询本地化文本 找不到时返回 false
        /// </summary>
        bool TryGet(string key, out string text);
    }
}
