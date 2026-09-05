using MiMieEventBus;

namespace Mm_Budier
{
    /// <summary>
    /// 建造系统对外事件 Key System 发布 UI/玩法订阅
    /// </summary>
    public static class BuilderEvents
    {
        /// <summary>
        /// 方块放置成功 参数 方块运行时实例
        /// </summary>
        public static readonly EventKey<CubeInstance> CubePlaced =
            new EventKey<CubeInstance>("Builder.CubePlaced");

        /// <summary>
        /// 方块破坏成功 参数 方块运行时实例
        /// </summary>
        public static readonly EventKey<CubeInstance> CubeBroken =
            new EventKey<CubeInstance>("Builder.CubeBroken");

        /// <summary>
        /// 取消放置 退出建造预览
        /// </summary>
        public static readonly EventKey PlaceCancelled =
            new EventKey("Builder.PlaceCancelled");
    }
}
