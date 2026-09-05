namespace Interaction.Player
{
    /// <summary>
    /// 玩家交互模块约定 每帧 Tick
    /// </summary>
    public interface IPlayerInteract
    {
        /// <summary>
        /// 每帧逻辑
        /// </summary>
        void Tick();
    }
}
