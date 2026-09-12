using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 交互球形投射检测 SphereCastNonAlloc 无堆分配
    /// </summary>
    [System.Serializable]
    public class InteractionDetector
    {
        [SerializeField, LabelText("射线起点")]
        private Transform rayOrigin;

        [SerializeField, LabelText("最大交互距离"), MinValue(0.1f)]
        private float maxDistance = 1.3f;

        [SerializeField, LabelText("投射半径"), MinValue(0.01f)]
        private float sphereCastRadius = 0.2f;

        [SerializeField, LabelText("检测 Layer")]
        private LayerMask interactLayer = ~0;

        [SerializeField, LabelText("Trigger 检测")]
        private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;

        [SerializeField, LabelText("调试投射")]
        private bool debugDraw;

        /// <summary>
        /// SphereCast 命中缓冲
        /// 一次投射会同时命中架子与架子上摆的物品 需要多命中再按优先级挑
        /// </summary>
        private readonly RaycastHit[] castHitBuffer = new RaycastHit[24];

        /// <summary>
        /// Collider 实体 ID 到 IInteractable 缓存
        /// </summary>
        private readonly Dictionary<EntityId, IInteractableInterface> interactableCacheDict = new();

        /// <summary>
        /// 射线起点 Transform
        /// </summary>
        public Transform RayOrigin => rayOrigin;

        /// <summary>
        /// 设置射线起点
        /// </summary>
        public void SetRayOrigin(Transform origin)
        {
            rayOrigin = origin;
        }

        /// <summary>
        /// 解析默认射线起点
        /// </summary>
        public void EnsureRayOrigin()
        {
            if (rayOrigin != null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                rayOrigin = mainCamera.transform;
        }   

        /// <summary>
        /// 设置射线检测最大距离
        /// </summary>
        public void SetMaxDistance(float distance)
        {
            maxDistance = distance;
        }

        /// <summary>
        /// 设置球形范围投射半径
        /// </summary>
        public void SetSphereCastRadius(float radius)
        {
            sphereCastRadius = radius;
        }

        /// <summary>
        /// 尝试检测当前聚焦的可交互物
        /// 收集全部命中后按 交互优先级 再按 距离 取最优
        /// 可拾取物品优先级最高 不会被架子/容器这类大体量交互物挡住
        /// </summary>
        public bool TryDetect(out RaycastHit hit, out IInteractableInterface target)
        {
            target = null;
            hit = default;

            EnsureRayOrigin();
            if (rayOrigin == null)
                return false;

            Vector3 origin = rayOrigin.position;
            Vector3 direction = rayOrigin.forward;

            // 球形范围投射 比单射线更易命中交互 Trigger
            int hitCount = PhysicRayCast.SphereCastNonAlloc(
                origin,
                sphereCastRadius,
                direction,
                castHitBuffer,
                maxDistance,
                interactLayer,
                queryTriggerInteraction,
                debugDraw);

            if (hitCount <= 0)
                return false;

            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hitCount && i < castHitBuffer.Length; i++)
            {
                var candidateHit = castHitBuffer[i];
                if (candidateHit.collider == null)
                    continue;

                var candidate = ResolveInteractable(candidateHit.collider);
                // 不可交互的碰撞体只跳过自己 不再否决整次检测
                if (candidate == null)
                    continue;

                int priority = InteractPriorityUtil.GetPriority(candidate);
                bool isBetter = priority > bestPriority
                    || (priority == bestPriority && candidateHit.distance < bestDistance);
                if (!isBetter)
                    continue;

                bestPriority = priority;
                bestDistance = candidateHit.distance;
                hit = candidateHit;
                target = candidate;
            }

            return target != null;
        }

        /// <summary>
        /// 从 Collider 解析 IInteractable 带缓存
        /// 缓存里的组件被销毁时(物品被拾取)会清掉重查 避免残留项挡交互
        /// </summary>
        private IInteractableInterface ResolveInteractable(Collider collider)
        {
            if (collider == null)
                return null;

            var entityId = collider.GetEntityId();
            if (interactableCacheDict.TryGetValue(entityId, out IInteractableInterface cached))
            {
                if (cached == null)
                    return null;

                // 接口引用不重载 Unity 的判空 需要落到 UnityEngine.Object 上判断存活
                if (cached is Object unityObject && unityObject == null)
                {
                    interactableCacheDict.Remove(entityId);
                }
                else
                {
                    return cached;
                }
            }

            var interactable = collider.GetComponentInParent<IInteractableInterface>();
            interactableCacheDict[entityId] = interactable;
            return interactable;
        }

        /// <summary>
        /// 清空 Collider 缓存
        /// </summary>
        public void ClearCache()
        {
            interactableCacheDict.Clear();
        }
    }
}
