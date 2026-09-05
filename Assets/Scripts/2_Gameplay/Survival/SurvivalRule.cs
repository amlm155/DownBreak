using System;
using System.Collections.Generic;
using DBGameSystem;
using GAS.StateSystem;
using MieMieFrameWork;
using MiMieEventBus;
using UnityEngine;

namespace DBGameplay
{
    /// <summary>
    /// 生存规则 驱动数值自然衰减 饥饿联动与死亡判定
    /// 负责 自然衰减节奏 饥饿联动 死亡判定
    /// </summary>
    public class SurvivalRule : MonoBehaviour
    {
        /// <summary> 玩家数值接口 从 GameHub 获取 </summary>
        private IPlayerStatus playerStatus;

        /// <summary> 是否处于饥饿状态 饱食归零后为 true </summary>
        private bool isStarving;

        /// <summary> 饥饿状态额外扣血速度 点/秒 </summary>
        private const float StarveDamagePerSecond = 3f;

        /// <summary> 属性名 饱食度 </summary>
        private const string FoodStatName = "Food";

        /// <summary> 属性名 血量 </summary>
        private const string HealthStatName = "Health";

        /// <summary> 事件订阅容器 </summary>
        private readonly List<IDisposable> disposableList = new List<IDisposable>();

        /// <summary>
        /// 初始化 订阅数值事件
        /// </summary>
        private void Awake()
        { 
            if (playerStatus == null)
                playerStatus = GameHub.Get<IPlayerStatus>();

            disposableList.Add(
                MmGlobalEventBus.GlobalBus.Subscribe(
                    PlayerStatEvents.FoodChanged, OnFoodChanged));
            disposableList.Add(
                MmGlobalEventBus.GlobalBus.Subscribe(
                    PlayerStatEvents.HealthChanged, OnHealthChanged));
        }

        /// <summary>
        /// 每帧驱动 饥饿状态持续扣血
        /// </summary>
        private void Update()
        {

            if (!isStarving || playerStatus == null)
                return;

            playerStatus.ChangeValue(
                HealthStatName,
                -StarveDamagePerSecond * Time.deltaTime);
        }

        /// <summary>
        /// 饱食变化 归零进入饥饿 恢复退出饥饿
        /// </summary>
        private void OnFoodChanged(float curr, float max, bool anim)
        {
            bool emptyNow = curr <= 0f;
            if (emptyNow == isStarving)
                return;

            isStarving = emptyNow;
        }

        /// <summary>
        /// 血量归零 死亡判定 广播死亡
        /// </summary>
        private void OnHealthChanged(float curr, float max, bool anim)
        {
            if (curr > 0f)
                return;

            MmGlobalEventBus.GlobalBus.Publish(GameFlowEvents.PlayerDied);
        }

        /// <summary>
        /// 销毁时退订
        /// </summary>
        private void OnDestroy()
        {
            for (int i = 0; i < disposableList.Count; i++)
                disposableList[i].Dispose();
            disposableList.Clear();
        }
    }
}
