using DBGameSystem;
using Mm_Budier;

namespace Interaction.Player
{
    /// <summary>
    /// 建造输入桥 手持可放置物时把 3C 按键转成 IBuilder 注入
    /// </summary>
    public class BuildInteractModule : IPlayerInteract
    {
        /// <summary>
        /// 当前是否处于放置预览
        /// </summary>
        public static bool IsBuildModeActive()
        {
            var builder = GameHub.Get<IBuilder>();
            return builder != null && !builder.IsRayCastStopped;
        }

        /// <summary>
        /// 开始放置指定方块 打开射线预览
        /// </summary>
        public void EnterBuildMode(CubeData cubeData)
        {
            var builder = GameHub.Get<IBuilder>();
            if (builder == null)
                return;

            if (cubeData != null)
                builder.SetActiveCubeData(cubeData);
            builder.SetStopRayCast(false);
        }

        /// <summary>
        /// 结束放置 藏预览 把键还给攻击拾取
        /// </summary>
        public void ExitBuildMode()
        {
            GameHub.Get<IBuilder>()?.CancelPlace();
        }

        /// <summary>
        /// 每帧把放置与旋转转给建造系统
        /// </summary>
        public void Tick()
        {
            var builder = GameHub.Get<IBuilder>();
            if (builder == null || builder.IsRayCastStopped)
                return;

            var input = GameHub.Get<IPlayerInput>();
            if (input == null)
                return;

            if (input.IsLeftAttackPressed)
                builder.SetPlaceButtonPressed();

            if (input.IsRotatePressed)
                builder.SetRotateButtonPressed();

            if (input.IsRightAttackPressed)
                builder.CancelPlace();
        }
    }
}
