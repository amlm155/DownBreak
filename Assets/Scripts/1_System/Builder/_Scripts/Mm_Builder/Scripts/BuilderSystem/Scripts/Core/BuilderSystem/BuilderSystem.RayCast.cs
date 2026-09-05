using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mm_Budier
{
    public enum ERayType
    {
        FirstPerson,
        ThirdPerson,
        RTS,
    }

    /// <summary>
    /// 处理射线检测命中方块的哪个部分并返回对应的hit信息
    /// </summary>
    public partial class BuilderSystem
    {

        // 放置偏移量 防止穿模
        private const float PLACEMENT_EPSILON = 0.01f;

        // 控制射线检测是否停止 默认停 由 3C 建造模式打开
        private bool stopRayCast = true;
        public bool IsRayCastStopped => stopRayCast;
        public void SetStopRayCast(bool stop) => stopRayCast = stop;
        private RaycastHit[] raycastHitBuffer;

        /// <summary>
        /// 处理射线
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override bool HandleRaycast(out RaycastHit hit)
        {
            hit = default;
            var found = GetRaycastHitInfo(out hit);
            return found;
        }

        /// <summary>
        /// 获取射线命中信息
        /// </summary>
        private bool GetRaycastHitInfo(out RaycastHit hit)
        {
            hit = default;
            if (mainCamera == null)
                return false;


            if (raycastHitBuffer == null || raycastHitBuffer.Length == 0)
                return false;

            var hits = raycastHitBuffer;
            var mask = builderSetting.groundLayer | builderSetting.cubeLayer;
            var ray = GetRayType(gameRayType);

            // 射线检测
            int hitCount = Physics.RaycastNonAlloc(ray,  // 射线
                                                   hits,  // 碰撞体数组
                                                   builderSetting.raycastMaxDistance,  // 最大距离
                                                   mask,  // 层掩码
                                                   QueryTriggerInteraction.Ignore);  // 触发器交互

            if (hitCount <= 0)
                return false;

            float closestDist = float.MaxValue;
            bool found = false;

            // 获取最近的碰撞体
            for (int i = 0; i < hitCount; i++)
            {
                var col = hits[i].collider;
                // 忽略玩家碰撞体 
                if (col == null || IsPlayerCollider(col))
                    continue;

                // 外部开发者自定义射线检测校验
                var candidateHit = hits[i];
                if (imBuilder != null)
                {
                    if (!imBuilder.CustomRaycastValid(out candidateHit))
                        continue;
                }

                if (candidateHit.distance < closestDist)
                {
                    closestDist = candidateHit.distance;
                    hit = candidateHit;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// 判断是否是玩家碰撞体
        /// </summary>
        private bool IsPlayerCollider(Collider collider)
        {
            // 判断Tag和Layer
            return collider.gameObject.layer == LayerMask.NameToLayer("Player") ||
                    collider.gameObject.tag == "Player";
        }

        /// <summary>
        /// 获取放置目标网格坐标
        /// 先向命中面的内侧偏移
        /// 再向命中面的法线方向偏移 方便在已有方块头顶放下新方块
        /// </summary>
        /// <param name="hit">射线命中信息</param>
        /// <returns>目标网格坐标</returns>
        private Vector3Int GetPlaceTargetGridPos(RaycastHit hit)
        {
            var eps = virtualGrid.gridUnitSize * PLACEMENT_EPSILON;
            // 向命中面的内侧偏移
            var hitPoint = hit.point - hit.normal * eps;
            var hitGridPos = virtualGrid.WorldToGrid(hitPoint);
            // 返回命中面的内侧偏移后的网格坐标
            return hitGridPos + Vector3Int.RoundToInt(hit.normal);
        }

        /// <summary>
        /// 获取破坏目标网格坐标
        /// 先向命中面的内侧偏移
        /// 然后直接返回网格坐标,返回精准命中方块的网格坐标
        /// </summary>
        /// <param name="hit">射线命中信息</param>
        private Vector3Int GetBreakTargetGridPos(RaycastHit hit)
        {
            var eps = virtualGrid.gridUnitSize * PLACEMENT_EPSILON;
            var insidePoint = hit.point - hit.normal * eps;
            var gridPos = virtualGrid.WorldToGrid(insidePoint);
            return gridPos;
        }


        /// <summary>
        /// 根据枚举获取射线类型
        /// </summary>
        /// <param name="rayType"></param>
        /// <returns></returns>
        private Ray GetRayType(ERayType rayType)
        {
            switch (rayType)
            {
                // 第一人称的射线通常是相机位置到前方
                case ERayType.FirstPerson:
                    return new Ray(mainCamera.transform.position, mainCamera.transform.forward);
                // TODO: 待定
                case ERayType.ThirdPerson:
                    return new Ray(mainCamera.transform.position, mainCamera.transform.forward);

                // TODO: 待定
                case ERayType.RTS:
                    return new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            }
            return default;
        }

    }
}