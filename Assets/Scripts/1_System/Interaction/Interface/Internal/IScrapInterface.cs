namespace Interaction
{
    /// <summary>
    /// 搜刮容器接口 场景可交互物体实现
    /// </summary>
    public interface IScrapInterface
    {
        /// <summary> 搜刮容器模板 ID </summary>
        int ScrapContainerId { get; }

        /// <summary> 是否已搜过 </summary>
        bool IsAlreadyLooted { get; }

        /// <summary>
        /// 打开搜刮成功后回调
        /// </summary>
        void OnScrapOpened();
    }
}
