using DBGameSystem;
using UnityEngine;

namespace Interaction.Player
{
    /// <summary>
    /// 闲置过久播放待机杂项 有动作则打断
    /// </summary>
    public class HandsNullInteractModule : IPlayerInteract
    {
        /// <summary> 上次有动作的时间 </summary>
        private float lastActionTime;

        /// <summary> 正在播放随机休息动画 </summary>
        private bool isPlayRandomIdelClip;

        /// <summary>
        /// 构造闲置杂项模块
        /// </summary>
        public HandsNullInteractModule()
        {
            lastActionTime = Time.time;
        }

        /// <summary>
        /// 每帧检测触发与打断
        /// </summary>
        public void Tick()
        {
            var body = GameHub.Get<IPlayerBody>();
            if (body?.Anim == null)
                return;

            if (TryInterrupt(body))
            {
                body.Anim.CrossFadeIdle(0.3f);
                return;
            }

            if (!CheckLongTimeToIdle(body))
                return;

            body.Anim.PlayViewAnimation();
            isPlayRandomIdelClip = true;
        }

        /// <summary>
        /// 检测玩家长时间不动作
        /// </summary>
        private bool CheckLongTimeToIdle(IPlayerBody body)
        {
            var input = GameHub.Get<IPlayerInput>();
            if (input != null && input.HasPlayerAction())
            {
                lastActionTime = Time.time;
                return false;
            }

            if (Time.time - lastActionTime > body.LongTimeToIdle)
            {
                lastActionTime = Time.time;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 有动作时打断随机待机杂项
        /// </summary>
        private bool TryInterrupt(IPlayerBody body)
        {
            var input = GameHub.Get<IPlayerInput>();
            if (!isPlayRandomIdelClip || input == null || !input.HasPlayerAction())
                return false;

            isPlayRandomIdelClip = false;
            lastActionTime = Time.time;
            return true;
        }
    }
}
