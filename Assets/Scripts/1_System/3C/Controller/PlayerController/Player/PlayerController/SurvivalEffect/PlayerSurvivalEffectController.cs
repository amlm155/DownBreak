using System;
using GAS.Core.GameplayEffect;
using GAS.StateSystem;
using MieMieFrameWork;
using MieMieFrameWork.Asset;
using MiMieEventBus;
using UnityEngine;

namespace GAS.Core
{
    /// <summary>
    /// 玩家生存属性阈值 GE 驱动
    /// </summary>
    public class PlayerSurvivalEffectController : MonoBehaviour
    {
        #region 配置

        /// <summary> 精力超过此值进入超饱和状态 </summary>
        private const float EnergySuperSaturatedThreshold = 100f;

        /// <summary> 精力低于此值降低最大体力 </summary>
        private const float EnergyLowThreshold = 40f;

        /// <summary> 精力低于此值降低最大生命和最大体力 </summary>
        private const float EnergyExhaustedThreshold = 10f;

        /// <summary> 疼痛超过此值降低最大体力 </summary>
        private const float PainHeavyThreshold = 60f;

        /// <summary> 疼痛超过此值降低最大生命和最大体力 </summary>
        private const float PainCriticalThreshold = 80f;

        /// <summary> 理智低于此值进入低理智状态 </summary>
        private const float SanLowThreshold = 40f;

        /// <summary> 理智低于此值进入危急理智状态 </summary>
        private const float SanCriticalThreshold = 20f;

        /// <summary> 饥饿和口渴都超过此值时开始回血 </summary>
        private const float WellFedAndHydratedThreshold = 80f;

        #endregion

        #region 运行时状态

        private StatController statController;
        private GEManager geManager;
        private bool isInitialized;
        private int energyState;
        private int painState;
        private int sanState;
        private bool isStarving;
        private bool isDehydrated;
        private bool isWellFedAndHydrated;

        #endregion

        #region GE 资产

        private GameplayEffectData energySuperSaturated;
        private GameplayEffectData energyLow;
        private GameplayEffectData energyExhausted;
        private GameplayEffectData painHeavy;
        private GameplayEffectData painCritical;
        private GameplayEffectData starving;
        private GameplayEffectData dehydrated;
        private GameplayEffectData wellFedHydratedRegen;
        private GameplayEffectData sanLow;
        private GameplayEffectData sanCritical;

        #endregion

        #region GE 运行时实例

        private GameplayEffectRuntime energyRuntime;
        private GameplayEffectRuntime painRuntime;
        private GameplayEffectRuntime sanRuntime;
        private GameplayEffectRuntime starvingRuntime;
        private GameplayEffectRuntime dehydratedRuntime;
        private GameplayEffectRuntime regenRuntime;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化生存属性 GE
        /// </summary>
        public void Init(StatController controller, GEManager manager)
        {
            if (isInitialized)
                return;

            statController = controller;
            geManager = manager;
            LoadEffectData();
            statController.OnImmediateStatChanged += OnStatChanged;
            isInitialized = true;
            RefreshAll();
        }

        /// <summary>
        /// 加载生存 GE 资产
        /// </summary>
        private void LoadEffectData()
        {
            const string root = "Assets/Arts/InteranlArts/Configs/GASConfig/GE/";
            energySuperSaturated = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "Energy/SuperSaturated.asset");
            energyLow = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "Energy/Low.asset");
            energyExhausted = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "Energy/Exhausted.asset");
            painHeavy = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "Pain/Heavy.asset");
            painCritical = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "Pain/Critical.asset");
            starving = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "FoodAndWater/Starving.asset");
            dehydrated = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "FoodAndWater/Dehydrated.asset");
            wellFedHydratedRegen = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "FoodAndWater/WellFedHydratedRegen.asset");
            sanLow = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "San/Low.asset");
            sanCritical = MmAssetMgr.LoadAsset<GameplayEffectData>(root + "San/Critical.asset");
        }

        #endregion

        #region 属性变化驱动

        /// <summary>
        /// 属性变化时只刷新对应阈值状态
        /// </summary>
        private void OnStatChanged(string statName)
        {
            if (!isInitialized)
                return;

            if (string.Equals(statName, PlayerStatIds.Energy, StringComparison.OrdinalIgnoreCase))
                RefreshEnergy();
            else if (string.Equals(statName, PlayerStatIds.Pain, StringComparison.OrdinalIgnoreCase))
                RefreshPain();
            else if (string.Equals(statName, PlayerStatIds.San, StringComparison.OrdinalIgnoreCase))
                RefreshSan();
            else if (string.Equals(statName, PlayerStatIds.Food, StringComparison.OrdinalIgnoreCase)
                || string.Equals(statName, PlayerStatIds.Water, StringComparison.OrdinalIgnoreCase))
                RefreshFoodAndWater();
        }

        private void OnDestroy()
        {
            if (statController != null)
                statController.OnImmediateStatChanged -= OnStatChanged;
        }

        #endregion

        #region GE

        /// <summary>
        /// 初始化时刷新所有生存状态
        /// </summary>
        private void RefreshAll()
        {
            RefreshEnergy();
            RefreshPain();
            RefreshSan();
            RefreshFoodAndWater();
        }

        /// <summary>
        /// 应用一个生存状态 GE
        /// </summary>
        /// <param name="effectData">要应用的 GE 配置</param>
        /// <returns>应用后的运行时 GE 实例</returns>
        private GameplayEffectRuntime ApplyRuntime(GameplayEffectData effectData)
        {
            if (effectData == null)
                return null;

            var runtime = geManager.ApplyGE(effectData, this);
            if (runtime != null)
                MmGlobalEventBus.GlobalBus.Publish(
                    PlayerStatEvents.SurvivalEffectApplied,
                    effectData.TableId);
            return runtime;
        }

        /// <summary>
        /// 移除当前状态对应的 GE
        /// 状态切换时先移除旧效果再应用新效果
        /// </summary>
        /// <param name="runtime">要移除的运行时 GE</param>
        private void RemoveRuntime(ref GameplayEffectRuntime runtime)
        {
            if (runtime == null)
                return;
            // 刷UI
            MmGlobalEventBus.GlobalBus.Publish(
                PlayerStatEvents.SurvivalEffectRemoved,
                runtime.GEData.TableId);
            // 卸载掉旧的效果
            geManager.RemoveGE(runtime);
            runtime = null;
        }

        #endregion

        #region 精力

        /// <summary>
        /// 根据精力值切换精力状态 GE
        /// 1为超饱和 负1为低精力 负2为精力耗尽 0为正常
        /// </summary>
        private void RefreshEnergy()
        {
            float value = statController.GetCurrentValue(PlayerStatIds.Energy);
            int nextState = value > EnergySuperSaturatedThreshold
                ? 1
                : value < EnergyExhaustedThreshold
                    ? -2
                    : value < EnergyLowThreshold ? -1 : 0;
            if (nextState == energyState)
                return;

            // 状态变化时先清理旧 GE 避免多个阶段同时生效
            energyState = nextState;
            RemoveRuntime(ref energyRuntime);
            GameplayEffectData nextData = nextState switch
            {
                1 => energySuperSaturated,
                -1 => energyLow,
                -2 => energyExhausted,
                _ => null,
            };
            energyRuntime = ApplyRuntime(nextData);
        }

        #endregion

        #region 疼痛

        /// <summary>
        /// 根据疼痛值切换疼痛状态 GE
        /// 1为重度疼痛 2为严重疼痛 0为无疼痛影响
        /// </summary>
        private void RefreshPain()
        {
            float value = statController.GetCurrentValue(PlayerStatIds.Pain);
            int nextState = value > PainCriticalThreshold
                ? 2
                : value > PainHeavyThreshold ? 1 : 0;
            if (nextState == painState)
                return;

            painState = nextState;
            RemoveRuntime(ref painRuntime);
            painRuntime = ApplyRuntime(nextState == 2 ? painCritical : nextState == 1 ? painHeavy : null);
        }

        #endregion

        #region 理智

        /// <summary>
        /// 根据理智值切换理智状态 GE
        /// 1为低理智 2为危急理智 0为正常理智
        /// </summary>
        private void RefreshSan()
        {
            float value = statController.GetCurrentValue(PlayerStatIds.San);
            int nextState = value < SanCriticalThreshold
                ? 2
                : value < SanLowThreshold ? 1 : 0;
            if (nextState == sanState)
                return;

            sanState = nextState;
            RemoveRuntime(ref sanRuntime);
            sanRuntime = ApplyRuntime(nextState == 2 ? sanCritical : nextState == 1 ? sanLow : null);
        }

        #endregion

        #region 饥饿与口渴

        /// <summary>
        /// 检查饥饿 口渴和良好饮食状态
        /// 饥饿或脱水触发持续伤害 两项都高于阈值时触发回血
        /// </summary>
        private void RefreshFoodAndWater()
        {
            bool nextStarving = statController.GetCurrentValue(PlayerStatIds.Food) <= 0f;
            bool nextDehydrated = statController.GetCurrentValue(PlayerStatIds.Water) <= 0f;
            bool nextWellFedAndHydrated = statController.GetCurrentValue(PlayerStatIds.Food) > WellFedAndHydratedThreshold
                && statController.GetCurrentValue(PlayerStatIds.Water) > WellFedAndHydratedThreshold;

            if (nextStarving != isStarving)
            {
                // 只有跨过零点时切换饥饿 GE 避免每帧重复应用
                isStarving = nextStarving;
                RemoveRuntime(ref starvingRuntime);
                if (isStarving)
                    starvingRuntime = ApplyRuntime(starving);
            }

            if (nextDehydrated != isDehydrated)
            {
                // 只有跨过零点时切换脱水 GE
                isDehydrated = nextDehydrated;
                RemoveRuntime(ref dehydratedRuntime);
                if (isDehydrated)
                    dehydratedRuntime = ApplyRuntime(dehydrated);
            }

            if (nextWellFedAndHydrated != isWellFedAndHydrated)
            {
                // 两项资源必须同时高于阈值才能获得回血
                isWellFedAndHydrated = nextWellFedAndHydrated;
                RemoveRuntime(ref regenRuntime);
                if (isWellFedAndHydrated)
                    regenRuntime = ApplyRuntime(wellFedHydratedRegen);
            }
        }

        #endregion
    }
}
