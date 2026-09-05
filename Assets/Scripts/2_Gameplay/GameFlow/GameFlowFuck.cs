using DBGameSystem;
using DBLocation;
using DBWeaponSystem;
using DownBreak.CraftingRecipeSystem;
using GAS.StateSystem;
using Interaction;
using Mm_Budier;
using MmInventory;
using UnityEngine;

namespace DBGameplay
{
    /// <summary>
    /// 游戏启动引导 把场景子系统注册到 GameHub
    /// 挂在场景 GameRoot 或任意常驻物体 生命周期只做注册
    /// 按接口类型注册 调用方按接口获取
    /// 执行序设为最后 保证场景各组件 Awake 已完成 能 FindFirstObjectByType 到
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class GameFlowFuck : MonoBehaviour
    {
        /// <summary>
        /// 注册全部游戏服务 场景物体已激活即能找到
        /// </summary>
        private void Awake()
        {
            RegisterWeapon();
            RegisterInventory();
            RegisterStatus();
            RegisterCrafting();
            RegisterLocalization();
            RegisterBuilder();
            RegisterInteraction();
        }

        /// <summary>
        /// 注册武器系统
        /// </summary>
        private void RegisterWeapon()
        {
            var weaponSystem = FindAnyObjectByType<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogWarning("[GameBootstrap] 场景未找到 WeaponSystem 跳过注册");
                return;
            }

            GameHub.Register<IWeaponSystem>(weaponSystem);
        }

        /// <summary>
        /// 注册库存数据服务
        /// </summary>
        private void RegisterInventory()
        {
            var itemRtDataMgr = FindAnyObjectByType<ItemRtDataMgr>();
            if (itemRtDataMgr == null)
            {
                Debug.LogWarning("[GameBootstrap] 场景未找到 ItemRtDataMgr 跳过注册");
                return;
            }

            GameHub.Register<IInventory>(itemRtDataMgr);
        }

        /// <summary>
        /// 注册生存数值服务
        /// </summary>
        private void RegisterStatus()
        {
            var statController = FindAnyObjectByType<StatController>();
            if (statController == null)
            {
                Debug.LogWarning("[GameBootstrap] 场景未找到 StatController 跳过注册");
                return;
            }

            GameHub.Register<IPlayerStatus>(statController);
        }

        /// <summary>
        /// 注册合成配方系统
        /// </summary>
        private void RegisterCrafting()
        {
            var craftingRecipeSystem = FindAnyObjectByType<CraftingRecipeSystem>();
            if (craftingRecipeSystem == null)
            {
                Debug.LogWarning("[GameBootstrap] 场景未找到 CraftingRecipeSystem 跳过注册");
                return;
            }

            GameHub.Register<ICraftingRecipe>(craftingRecipeSystem);
        }

        /// <summary>
        /// 注册本地化服务
        /// </summary>
        private void RegisterLocalization()
        {
            GameHub.Register<ILocalization>(new LocalizationSystem());
        }

        /// <summary>
        /// 注册建造系统
        /// </summary>
        private void RegisterBuilder()
        {
            var builderSystem = FindAnyObjectByType<BuilderSystem>();
            if (builderSystem == null)
            {
                Debug.LogWarning("[GameBootstrap] 场景未找到 BuilderSystem 跳过注册");
                return;
            }

            GameHub.Register<IBuilder>(builderSystem);
        }

        /// <summary>
        /// 注册世界物聚焦交互
        /// </summary>
        private void RegisterInteraction()
        {
            var interactionManager = FindAnyObjectByType<InteractionManager>();
            if (interactionManager == null)
            {
                Debug.LogWarning("[GameBootstrap] 场景未找到 InteractionManager 跳过注册");
                return;
            }

            GameHub.Register<IInteraction>(interactionManager);
        }
    }
}
