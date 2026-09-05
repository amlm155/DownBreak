using DBGameSystem;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 世界物聚焦交互门面 用于沟通 3C/UI 与射线聚焦
    /// </summary>
    public interface IInteraction : IGameService
    {
        IInteractableInterface CurrentFocus { get; }

        Vector3? CurrentHitPoint { get; }

        float CurrentDistance { get; }

        string CurrentPrompt { get; }

        Transform RayOrigin { get; }

        /// <summary>
        /// 设置交互聚焦最大距离
        /// </summary>
        void SetMaxDistance(float distance);

        /// <summary>
        /// 设置球形范围投射半径
        /// </summary>
        void SetSphereCastRadius(float radius);
    }
}
