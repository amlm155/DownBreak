using DBGameSystem;
using DBWeaponSystem;
using Mm_Budier;

namespace Interaction.Player
{
    /// <summary>
    /// 此模块为玩家交互核心模块
    /// 负责多个交互子模块 以及基础的调度与状态切换
    /// </summary>
    public class PlayerInteractCore
    {
        /// <summary> 交互子模块列表 </summary>
        private IPlayerInteract[] moduleList;

        /// <summary> 建造输入桥 </summary>
        private BuildInteractModule buildModule;

        public bool IsBuildMode => BuildInteractModule.IsBuildModeActive();

        /// <summary>
        /// 构造函数
        /// </summary>
        public PlayerInteractCore()
        {
            buildModule = new BuildInteractModule();
            moduleList = new IPlayerInteract[]
            {
                new HandsNullInteractModule(),
                new PickupInteractModule(),
                new AttackInteractModule(),
                buildModule,
            };
        }

        /// <summary>
        /// 每帧调度交互子模块
        /// </summary>
        public void Tick()
        {
            int count = moduleList.Length;
            for (int i = 0; i < count; i++)
                moduleList[i].Tick();
        }

        /// <summary>
        /// 进入建造模式
        /// </summary>
        public void EnterBuildMode(CubeData cubeData)
        {
            buildModule.EnterBuildMode(cubeData);
        }

        /// <summary>
        /// 退出建造模式
        /// </summary>
        public void ExitBuildMode()
        {
            buildModule.ExitBuildMode();
        }

        #region 切换状态

        /// <summary>
        /// 切换出空手
        /// </summary>
        public void ChangeToNullHand()
        {
            SwitchWeaponModule(EAnimationModelType.None);
        }

        /// <summary>
        /// 切换手持模组
        /// </summary>
        public void SwitchWeaponModule(EAnimationModelType modelType)
        {
            SwitchAmModule(modelType);
            if (GameHub.Get<IWeaponSystem>() != null)
                GameHub.Get<IWeaponSystem>().RefreshCurrentWeaponPose();
        }

        /// <summary>
        /// 只切动画模组
        /// </summary>
        public void SwitchAmModule(EAnimationModelType modelType)
        {
            var body = GameHub.Get<IPlayerBody>();
            body?.Anim?.SwitchModule(modelType);
        }

        #endregion
    }
}
