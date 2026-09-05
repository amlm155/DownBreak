using MiMieEventBus;

namespace DBGameplay
{
    /// <summary>
    /// 游戏流程对外事件 由 Gameplay 玩法层发布 3C/UI 各自订阅
    /// </summary>
    public static class GameFlowEvents
    {
        /// <summary>
        /// 玩家死亡事件 无参数
        /// </summary>
        public static readonly EventKey PlayerDied =
            new EventKey("GameFlow.PlayerDied");

        /// <summary>
        /// 玩家复活事件 无参数
        /// </summary>
        public static readonly EventKey PlayerRevived =
            new EventKey("GameFlow.PlayerRevived");
    }
}
