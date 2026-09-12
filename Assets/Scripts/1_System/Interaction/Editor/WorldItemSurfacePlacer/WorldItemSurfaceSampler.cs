using System;
using System.Collections.Generic;
using MmInventory;
using MieMieFrameWork.Asset;
using UnityEditor;
using UnityEngine;

namespace Interaction.Editor
{
    /// <summary>
    /// 表面采样与世界物品实例化 仅编辑器工具使用
    /// 采样思路 在目标碰撞体包围盒内随机撒点 再向下打射线 只接受法线朝上的命中面
    /// </summary>
    public static class WorldItemSurfaceSampler
    {
        /// <summary> 球测缓冲 避开已有掉落物用 </summary>
        private static readonly Collider[] overlapBuffer = new Collider[32];

        /// <summary> 内部表面多命中缓冲 用于越过遮挡物找指定层板 </summary>
        private static readonly RaycastHit[] interiorHitBuffer = new RaycastHit[16];

        /// <summary> 净空盒测缓冲 防穿模用 </summary>
        private static readonly Collider[] clearanceBuffer = new Collider[32];

        /// <summary> 一次采样得到的表面命中 </summary>
        public struct SurfaceHit
        {
            /// <summary> 命中点世界坐标 </summary>
            public Vector3 Position;

            /// <summary> 命中面法线 已归一化 </summary>
            public Vector3 Normal;

            /// <summary> 命中的表面碰撞体 </summary>
            public Collider Surface;
        }

        /// <summary>
        /// 采样失败原因统计 用于回显为什么摆不上
        /// </summary>
        public struct SurfaceSampleReport
        {
            /// <summary> 射线尝试次数 </summary>
            public int AttemptCount;

            /// <summary> 射线没打到任何东西 </summary>
            public int RayMissCount;

            /// <summary> 打到了但不是目标自身的表面 </summary>
            public int ForeignHitCount;

            /// <summary> 命中面法线不够朝上 </summary>
            public int BadNormalCount;

            /// <summary> 与已放置点间距不足 </summary>
            public int TooCloseCount;

            /// <summary> 附近已经有掉落物 </summary>
            public int ItemNearbyCount;

            /// <summary> 内部采样时头顶没有目标几何 不算内部</summary>
            public int InteriorNoCoverCount;

            /// <summary> 净空不足 放上去会穿模</summary>
            public int ClearanceBlockedCount;

            /// <summary> 合并另一次统计 </summary>
            public void Accumulate(in SurfaceSampleReport other)
            {
                AttemptCount += other.AttemptCount;
                RayMissCount += other.RayMissCount;
                ForeignHitCount += other.ForeignHitCount;
                BadNormalCount += other.BadNormalCount;
                TooCloseCount += other.TooCloseCount;
                ItemNearbyCount += other.ItemNearbyCount;
                InteriorNoCoverCount += other.InteriorNoCoverCount;
                ClearanceBlockedCount += other.ClearanceBlockedCount;
            }

            /// <summary> 失败原因文案 </summary>
            public string BuildReasonText()
            {
                var builder = new System.Text.StringBuilder();
                if (RayMissCount > 0)
                    builder.Append($"射线未命中 {RayMissCount} 次（多为层掩码/目标未激活/不是场景实例）");
                AppendReason(builder, ForeignHitCount, "命中非目标表面");
                AppendReason(builder, BadNormalCount, "命中面不够朝上");
                AppendReason(builder, InteriorNoCoverCount, "内部空间头顶没有遮挡");
                AppendReason(builder, TooCloseCount, "与已放置点太近");
                AppendReason(builder, ItemNearbyCount, "附近已有掉落物");
                AppendReason(builder, ClearanceBlockedCount, "净空不足放上去会穿模");

                if (builder.Length == 0)
                    return "无";

                return builder.ToString();
            }

            /// <summary>
            /// 追加一条原因
            /// </summary>
            private static void AppendReason(System.Text.StringBuilder builder, int count, string label)
            {
                if (count <= 0)
                    return;

                if (builder.Length > 0)
                    builder.Append('，');
                builder.Append($"{label} {count} 次");
            }
        }

        #region 目标与包围盒

        /// <summary>
        /// 收集目标的碰撞体
        /// 优先只取实体碰撞体 目标只有触发器时退回触发器并把 collideTriggers 置真
        /// </summary>
        public static Collider[] CollectTargetColliders(GameObject target, out bool collideTriggers)
        {
            collideTriggers = false;
            if (target == null)
                return Array.Empty<Collider>();

            var colliderList = target.GetComponentsInChildren<Collider>(true);
            if (colliderList == null || colliderList.Length == 0)
                return Array.Empty<Collider>();

            var solidList = new List<Collider>(colliderList.Length);
            var triggerList = new List<Collider>(colliderList.Length);
            for (int i = 0; i < colliderList.Length; i++)
            {
                var collider = colliderList[i];
                if (collider == null)
                    continue;

                if (collider.isTrigger)
                    triggerList.Add(collider);
                else
                    solidList.Add(collider);
            }

            if (solidList.Count > 0)
                return solidList.ToArray();

            // 只有触发器时也能采样 但射线要允许命中触发器
            collideTriggers = triggerList.Count > 0;
            return triggerList.ToArray();
        }

        /// <summary>
        /// 求碰撞体集合的世界包围盒
        /// </summary>
        public static bool TryGetTargetBounds(Collider[] colliderList, out Bounds bounds)
        {
            bounds = default;
            if (colliderList == null || colliderList.Length == 0)
                return false;

            bounds = colliderList[0].bounds;
            for (int i = 1; i < colliderList.Length; i++)
                bounds.Encapsulate(colliderList[i].bounds);

            return true;
        }

        /// <summary>
        /// 取目标碰撞体所在层的掩码 采样时会并入表面层 避免目标自己所在的层被掩码挡掉
        /// </summary>
        public static int ResolveTargetLayerMask(Collider[] colliderList)
        {
            int layerMask = 0;
            if (colliderList == null)
                return layerMask;

            for (int i = 0; i < colliderList.Length; i++)
            {
                if (colliderList[i] == null)
                    continue;

                layerMask |= 1 << colliderList[i].gameObject.layer;
            }

            return layerMask;
        }

        #endregion

        #region 表面采样

        /// <summary>
        /// 采样一个可放置点 失败返回 false 并把原因累加到 report
        /// </summary>
        /// <param name="targetColliderList">目标碰撞体 只接受自身命中时使用</param>
        /// <param name="bounds">目标碰撞体包围盒</param>
        /// <param name="settings">工具配置</param>
        /// <param name="usedPointList">已经用掉的点 用于最小间距</param>
        /// <param name="random">随机源</param>
        /// <param name="itemRadius">当前物品的水平半径 用于内缩与间距</param>
        /// <param name="collideTriggers">目标只有触发器时允许命中触发器</param>
        /// <param name="report">失败原因统计</param>
        /// <param name="hit">采样结果</param>
        public static bool TrySampleSurface(
            Collider[] targetColliderList,
            Bounds bounds,
            SurfacePlacerSettings settings,
            List<Vector3> usedPointList,
            System.Random random,
            float itemRadius,
            float itemHeight,
            bool collideTriggers,
            ref SurfaceSampleReport report,
            out SurfaceHit hit)
        {
            hit = default;
            if (settings == null || random == null)
                return false;

            // 包围盒水平内缩 让物品整体落在表面内 避免悬空后掉落
            float inset = Mathf.Max(0f, settings.horizontalMargin) + Mathf.Max(0f, itemRadius);
            float minX = bounds.min.x + inset;
            float maxX = bounds.max.x - inset;
            float minZ = bounds.min.z + inset;
            float maxZ = bounds.max.z - inset;

            // 目标太窄时退化到包围盒中心线 保证还能生成
            if (maxX < minX)
                minX = maxX = bounds.center.x;
            if (maxZ < minZ)
                minZ = maxZ = bounds.center.z;

            float originY = bounds.max.y + Mathf.Max(0.1f, settings.rayStartHeight);
            float distance = (originY - bounds.min.y) + Mathf.Max(1f, settings.rayMaxDistance);
            int attemptCount = Mathf.Max(1, settings.sampleAttemptsPerPoint);

            // 目标自身所在层始终并入表面层 免得冰箱/箱子这类交互物被默认掩码排掉
            int layerMask = ResolveEffectiveLayerMask(settings, targetColliderList);

            var queryTriggerInteraction = collideTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            for (int i = 0; i < attemptCount; i++)
            {
                report.AttemptCount++;

                float x = Mathf.Lerp(minX, maxX, (float)random.NextDouble());
                float z = Mathf.Lerp(minZ, maxZ, (float)random.NextDouble());
                var origin = new Vector3(x, originY, z);

                if (!Physics.Raycast(origin,
                                     Vector3.down,
                                     out var raycastHit,
                                     distance,
                                     layerMask,
                                     queryTriggerInteraction))
                {
                    report.RayMissCount++;
                    continue;
                }

                if (raycastHit.collider == null)
                {
                    report.RayMissCount++;
                    continue;
                }

                // 只接受目标自身的表面 目标旁边的东西不算
                if (settings.onlyTargetColliders && !IsTargetCollider(targetColliderList, raycastHit.collider))
                {
                    report.ForeignHitCount++;
                    continue;
                }

                // 法线朝上才算可摆放表面 侧面与底面跳过
                Vector3 normal = raycastHit.normal.sqrMagnitude > 0.0001f
                    ? raycastHit.normal.normalized
                    : Vector3.up;
                if (Vector3.Angle(normal, Vector3.up) > Mathf.Max(0f, settings.maxSlopeAngle))
                {
                    report.BadNormalCount++;
                    continue;
                }

                // 间距至少留出物品自身宽度 避免两件物品叠在一起
                float spacing = Mathf.Max(settings.minSpacing, itemRadius * 2f);
                if (!HasEnoughSpacing(usedPointList, raycastHit.point, spacing))
                {
                    report.TooCloseCount++;
                    continue;
                }

                if (settings.avoidExistingItems
                    && HasItemNearby(raycastHit.point, normal, spacing, targetColliderList))
                {
                    report.ItemNearbyCount++;
                    continue;
                }

                // 净空检查 避免和台面/侧壁/已摆物品穿模
                if (settings.avoidClipping
                    && !HasPlacementClearance(raycastHit.point, normal, itemRadius, itemHeight, null))
                {
                    report.ClearanceBlockedCount++;
                    continue;
                }

                hit = new SurfaceHit
                {
                    Position = raycastHit.point,
                    Normal = normal,
                    Surface = raycastHit.collider,
                };
                return true;
            }

            return false;
        }

        /// <summary>
        /// 在目标内部空间采样一个可放置点(例如冰箱隔板 货架层板)
        /// 显式给了内部表面碰撞体就只在那些碰撞体上采 否则在目标包围盒内部随机高度向下打射线
        /// </summary>
        /// <param name="targetColliderList">目标碰撞体 自动模式下作为可接受面</param>
        /// <param name="interiorColliderList">显式指定的内部表面碰撞体 可空</param>
        public static bool TrySampleInteriorSurface(
            Collider[] targetColliderList,
            Collider[] interiorColliderList,
            SurfacePlacerSettings settings,
            List<Vector3> usedPointList,
            System.Random random,
            float itemRadius,
            float itemHeight,
            bool collideTriggers,
            ref SurfaceSampleReport report,
            out SurfaceHit hit)
        {
            hit = default;
            if (settings == null || random == null)
                return false;

            var queryTriggerInteraction = collideTriggers
                ? QueryTriggerInteraction.Collide
                : QueryTriggerInteraction.Ignore;

            // 显式指定内部表面 逐面在自己的包围盒里采样 不要求头顶有遮挡
            if (interiorColliderList != null && interiorColliderList.Length > 0)
            {
                int interiorLayerMask = ResolveEffectiveLayerMask(settings, interiorColliderList);
                int colliderCount = interiorColliderList.Length;
                int attemptCount = Mathf.Max(1, settings.sampleAttemptsPerPoint);
                for (int i = 0; i < attemptCount; i++)
                {
                    report.AttemptCount++;

                    var surfaceCollider = interiorColliderList[random.Next(colliderCount)];
                    if (surfaceCollider == null)
                    {
                        report.RayMissCount++;
                        continue;
                    }

                    var surfaceBounds = surfaceCollider.bounds;
                    if (!TryResolveSampleColumn(
                            surfaceBounds, settings, random, itemRadius,
                            out float x, out float z, out float originY, out float distance))
                    {
                        report.RayMissCount++;
                        continue;
                    }

                    // 层板上方可能还有箱体外壳等遮挡 多命中里挑属于指定内部表面的最近一个
                    if (!TryPickNearestInteriorHit(
                            new Vector3(x, originY, z),
                            distance,
                            interiorLayerMask,
                            queryTriggerInteraction,
                            interiorColliderList,
                            out var interiorHit))
                    {
                        report.RayMissCount++;
                        continue;
                    }

                    if (!TryAcceptInteriorHit(
                            in interiorHit, interiorColliderList, settings, usedPointList,
                            itemRadius, itemHeight, ref report, out hit))
                    {
                        continue;
                    }

                    // 净空检查 目标自身与指定内部面不算遮挡 其余几何与已摆物品都算
                    if (settings.avoidClipping
                        && !HasPlacementClearance(
                            hit.Position, hit.Normal, itemRadius, itemHeight,
                            interiorColliderList, targetColliderList))
                    {
                        report.ClearanceBlockedCount++;
                        continue;
                    }

                    return true;
                }

                return false;
            }
            // 自动模式 在目标包围盒内部任意高度向下打射线 打到哪层算哪层
            if (!TryGetTargetBounds(targetColliderList, out var bounds))
                return false;

            int layerMask = ResolveEffectiveLayerMask(settings, targetColliderList);
            float inset = Mathf.Max(0f, settings.horizontalMargin) + Mathf.Max(0f, itemRadius);
            float minX = bounds.min.x + inset;
            float maxX = bounds.max.x - inset;
            float minZ = bounds.min.z + inset;
            float maxZ = bounds.max.z - inset;
            if (maxX < minX)
                minX = maxX = bounds.center.x;
            if (maxZ < minZ)
                minZ = maxZ = bounds.center.z;

            float minY = bounds.min.y + Mathf.Max(0f, settings.interiorBottomInset);
            float maxY = bounds.max.y - Mathf.Max(0.01f, settings.interiorTopInset);
            if (maxY <= minY)
            {
                minY = maxY = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.5f);
            }

            float interiorRayLength = Mathf.Max(0.1f, settings.interiorRayLength);
            int autoAttemptCount = Mathf.Max(1, settings.sampleAttemptsPerPoint);

            for (int i = 0; i < autoAttemptCount; i++)
            {
                report.AttemptCount++;

                float x = Mathf.Lerp(minX, maxX, (float)random.NextDouble());
                float z = Mathf.Lerp(minZ, maxZ, (float)random.NextDouble());
                float startY = Mathf.Lerp(minY, maxY, (float)random.NextDouble());

                if (!Physics.Raycast(new Vector3(x, startY, z),
                                     Vector3.down,
                                     out var interiorHit,
                                     interiorRayLength,
                                     layerMask,
                                     queryTriggerInteraction))
                {
                    report.RayMissCount++;
                    continue;
                }

                if (!TryAcceptInteriorHit(
                        in interiorHit, targetColliderList, settings, usedPointList,
                        itemRadius, itemHeight, ref report, out hit))
                {
                    continue;
                }

                // 真"内部"要求头顶有目标自己的几何 开放朝天的面不算内部
                if (settings.requireOverheadCover
                    && !HasOverheadCover(
                        hit.Position, hit.Normal, targetColliderList,
                        layerMask, settings.overheadCheckDistance, queryTriggerInteraction))
                {
                    report.InteriorNoCoverCount++;
                    continue;
                }

                // 净空检查 目标自身不算遮挡 其余几何与已摆物品都算
                if (settings.avoidClipping
                    && !HasPlacementClearance(
                        hit.Position, hit.Normal, itemRadius, itemHeight, targetColliderList))
                {
                    report.ClearanceBlockedCount++;
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 向下多命中里挑属于指定内部表面的最近命中
        /// </summary>
        private static bool TryPickNearestInteriorHit(
            Vector3 origin,
            float distance,
            int layerMask,
            QueryTriggerInteraction queryTriggerInteraction,
            Collider[] interiorColliderList,
            out RaycastHit bestHit)
        {
            bestHit = default;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                interiorHitBuffer,
                distance,
                layerMask,
                queryTriggerInteraction);

            bool hasHit = false;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hitCount && i < interiorHitBuffer.Length; i++)
            {
                var candidate = interiorHitBuffer[i];
                if (candidate.collider == null)
                    continue;

                if (!IsTargetCollider(interiorColliderList, candidate.collider))
                    continue;

                if (candidate.distance >= bestDistance)
                    continue;

                bestDistance = candidate.distance;
                bestHit = candidate;
                hasHit = true;
            }

            return hasHit;
        }

        /// <summary>
        /// 生成一次采样用的 XZ 列 供显式内部表面复用
        /// </summary>
        private static bool TryResolveSampleColumn(
            Bounds bounds,
            SurfacePlacerSettings settings,
            System.Random random,
            float itemRadius,
            out float x,
            out float z,
            out float originY,
            out float distance)
        {
            x = bounds.center.x;
            z = bounds.center.z;
            originY = bounds.max.y + Mathf.Max(0.1f, settings.rayStartHeight);
            distance = (originY - bounds.min.y) + Mathf.Max(1f, settings.rayMaxDistance);

            float inset = Mathf.Max(0f, settings.horizontalMargin) + Mathf.Max(0f, itemRadius);
            float minX = bounds.min.x + inset;
            float maxX = bounds.max.x - inset;
            float minZ = bounds.min.z + inset;
            float maxZ = bounds.max.z - inset;

            if (maxX < minX && maxZ < minZ)
                return false;

            if (maxX < minX)
                minX = maxX = bounds.center.x;
            if (maxZ < minZ)
                minZ = maxZ = bounds.center.z;

            x = Mathf.Lerp(minX, maxX, (float)random.NextDouble());
            z = Mathf.Lerp(minZ, maxZ, (float)random.NextDouble());
            return true;
        }

        /// <summary>
        /// 物品摆放位置的净空检查 避免和场景几何/已摆物品穿模
        /// 盒子坐在承托面上并向上留出物品高度 所以不会和承托面自己相交
        /// </summary>
        /// <param name="ignoreColliderList">不算遮挡的碰撞体 例如目标自身与锚点承托体</param>
        /// <param name="ignoreColliderList2">第二组不算遮挡的碰撞体</param>
        public static bool HasPlacementClearance(
            Vector3 bottomPoint,
            Vector3 up,
            float radius,
            float height,
            Collider[] ignoreColliderList,
            Collider[] ignoreColliderList2 = null)
        {
            if (radius <= 0.001f || height <= 0.001f)
                return true;

            Vector3 normal = up.sqrMagnitude > 0.0001f ? up.normalized : Vector3.up;

            // 略微收一点尺寸 贴边摆放时不至于被整面墙否掉
            const float shrink = 0.85f;
            float halfHeight = Mathf.Max(0.005f, height * 0.5f * shrink);
            float halfWidth = Mathf.Max(0.005f, radius * shrink);
            Vector3 halfExtents = new Vector3(halfWidth, halfHeight, halfWidth);
            Vector3 center = bottomPoint + normal * (halfHeight + 0.005f);

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                clearanceBuffer,
                rotation,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount && i < clearanceBuffer.Length; i++)
            {
                var collider = clearanceBuffer[i];
                if (collider == null)
                    continue;

                // 世界掉落物落地后是触发器 不算遮挡
                if (collider.isTrigger)
                    continue;

                if (IsInColliderList(ignoreColliderList, collider)
                    || IsInColliderList(ignoreColliderList2, collider))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 碰撞体是否在忽略列表里
        /// </summary>
        private static bool IsInColliderList(Collider[] colliderList, Collider collider)
        {
            if (colliderList == null || collider == null)
                return false;

            for (int i = 0; i < colliderList.Length; i++)
            {
                if (ReferenceEquals(colliderList[i], collider))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 按包围盒把物品最低点抬到承托面上
        /// 预制体轴心常常在模型中间 只对齐轴心会半个物品埋进台面
        /// 同时把碰撞体算进去 避免碰撞体比模型低时进 Play 被去穿透顶出去
        /// </summary>
        private static void AlignBottomToSurface(
            GameObject instance,
            Vector3 surfacePoint,
            Vector3 normal,
            float surfaceOffset)
        {
            if (!TryGetLowestProjection(instance, normal, out float minProjection))
                return;

            float targetProjection = Vector3.Dot(surfacePoint, normal) + surfaceOffset;
            instance.transform.position += normal * (targetProjection - minProjection);
        }

        /// <summary>
        /// 求实例(渲染器与碰撞体一起)在法线方向上的最低投影
        /// </summary>
        private static bool TryGetLowestProjection(GameObject instance, Vector3 normal, out float minProjection)
        {
            minProjection = float.MaxValue;
            bool hasAny = false;

            if (TryGetVisualBounds(instance, out var visualBounds))
            {
                minProjection = Mathf.Min(minProjection, ProjectBoundsMin(visualBounds, normal));
                hasAny = true;
            }

            if (TryGetPhysicsBounds(instance, out var physicsBounds))
            {
                minProjection = Mathf.Min(minProjection, ProjectBoundsMin(physicsBounds, normal));
                hasAny = true;
            }

            return hasAny;
        }

        /// <summary>
        /// 包围盒八个角在法线方向上的最小投影
        /// </summary>
        private static float ProjectBoundsMin(Bounds bounds, Vector3 normal)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float minProjection = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                minProjection = Mathf.Min(minProjection, Vector3.Dot(corner, normal));
            }

            return minProjection;
        }

        /// <summary>
        /// 求碰撞体包围盒 这是物理真正参与计算的范围
        /// </summary>
        public static bool TryGetPhysicsBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
                return false;

            var colliderList = root.GetComponentsInChildren<Collider>(true);
            bool hasBounds = false;
            for (int i = 0; i < colliderList.Length; i++)
            {
                var collider = colliderList[i];
                if (collider == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(collider.bounds);
            }

            return hasBounds;
        }

        /// <summary>
        /// 内部命中校验 通过则回填 hit 并把不通过原因计入 report
        /// </summary>
        private static bool TryAcceptInteriorHit(
            in RaycastHit raycastHit,
            Collider[] acceptedColliderList,
            SurfacePlacerSettings settings,
            List<Vector3> usedPointList,
            float itemRadius,
            float itemHeight,
            ref SurfaceSampleReport report,
            out SurfaceHit hit)
        {
            hit = default;
            _ = itemHeight;

            if (raycastHit.collider == null)
            {
                report.RayMissCount++;
                return false;
            }

            if (settings.onlyTargetColliders && !IsTargetCollider(acceptedColliderList, raycastHit.collider))
            {
                report.ForeignHitCount++;
                return false;
            }

            Vector3 normal = raycastHit.normal.sqrMagnitude > 0.0001f
                ? raycastHit.normal.normalized
                : Vector3.up;
            if (Vector3.Angle(normal, Vector3.up) > Mathf.Max(0f, settings.maxSlopeAngle))
            {
                report.BadNormalCount++;
                return false;
            }

            // 间距至少留出物品自身宽度
            float spacing = Mathf.Max(settings.minSpacing, itemRadius * 2f);
            if (!HasEnoughSpacing(usedPointList, raycastHit.point, spacing))
            {
                report.TooCloseCount++;
                return false;
            }

            if (settings.avoidExistingItems
                && HasItemNearby(raycastHit.point, normal, spacing, acceptedColliderList))
            {
                report.ItemNearbyCount++;
                return false;
            }

            hit = new SurfaceHit
            {
                Position = raycastHit.point,
                Normal = normal,
                Surface = raycastHit.collider,
            };
            return true;
        }

        /// <summary>
        /// 采样点头顶是否压着目标自己的几何
        /// </summary>
        private static bool HasOverheadCover(
            Vector3 point,
            Vector3 normal,
            Collider[] targetColliderList,
            int layerMask,
            float maxDistance,
            QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 up = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Vector3 origin = point + up * 0.02f;
            if (!Physics.Raycast(origin,
                                 up,
                                 out var roofHit,
                                 Mathf.Max(0.05f, maxDistance),
                                 layerMask,
                                 queryTriggerInteraction))
            {
                return false;
            }

            return IsTargetCollider(targetColliderList, roofHit.collider);
        }

        /// <summary>
        /// 合成实际使用的层掩码 目标自身所在层始终并入 掩码为 0 时退回默认可射线层
        /// </summary>
        private static int ResolveEffectiveLayerMask(SurfacePlacerSettings settings, Collider[] colliderList)
        {
            int layerMask = settings.surfaceLayerMask;
            if (settings.onlyTargetColliders)
                layerMask |= ResolveTargetLayerMask(colliderList);

            return layerMask == 0 ? Physics.DefaultRaycastLayers : layerMask;
        }

        /// <summary>
        /// 命中体是否属于目标自己
        /// </summary>
        private static bool IsTargetCollider(Collider[] targetColliderList, Collider hitCollider)
        {
            if (targetColliderList == null || hitCollider == null)
                return false;

            for (int i = 0; i < targetColliderList.Length; i++)
            {
                if (ReferenceEquals(targetColliderList[i], hitCollider))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 与已用点是否拉开足够间距
        /// </summary>
        public static bool HasEnoughSpacing(List<Vector3> usedPointList, Vector3 point, float minSpacing)
        {
            if (usedPointList == null || usedPointList.Count == 0)
                return true;

            float safeSpacing = Mathf.Max(0f, minSpacing);
            float sqrSpacing = safeSpacing * safeSpacing;
            for (int i = 0; i < usedPointList.Count; i++)
            {
                if ((usedPointList[i] - point).sqrMagnitude < sqrSpacing)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 表面点上是否已经有掉落物
        /// 只认带 ItemInteract 的碰撞体 目标自身的碰撞体不算
        /// </summary>
        private static bool HasItemNearby(
            Vector3 point,
            Vector3 normal,
            float minSpacing,
            Collider[] targetColliderList)
        {
            float radius = Mathf.Max(0.02f, minSpacing * 0.5f);
            Vector3 center = point + normal * 0.05f;
            int hitCount = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                overlapBuffer,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                var collider = overlapBuffer[i];
                if (collider == null)
                    continue;

                if (IsTargetCollider(targetColliderList, collider))
                    continue;

                if (collider.GetComponentInParent<ItemInteract>() == null)
                    continue;

                return true;
            }

            return false;
        }

        #endregion

        #region 预制体解析

        /// <summary>
        /// 解析物品的世界掉落预制体 结果按物品 ID 缓存
        /// </summary>
        public static bool TryResolveItemPrefab(
            IItemTableData item,
            Dictionary<int, GameObject> prefabCache,
            out GameObject prefab,
            out string error)
        {
            prefab = null;
            error = string.Empty;
            if (item == null)
            {
                error = "物品为空";
                return false;
            }

            if (prefabCache != null && prefabCache.TryGetValue(item.ExcelItemId, out prefab) && prefab != null)
                return true;

            string address = item.WorldPrefabPath;
            if (string.IsNullOrWhiteSpace(address))
            {
                error = $"id={item.ExcelItemId} 未配置 world_prefab_path";
                return false;
            }

            prefab = ResolvePrefabByAddress(address);
            if (prefab == null)
            {
                error = $"id={item.ExcelItemId} 短名 {address} 未在 assetAliasList 注册 也找不到同名预制体";
                return false;
            }

            prefabCache?.Add(item.ExcelItemId, prefab);
            return true;
        }

        /// <summary>
        /// 短名或完整路径转预制体资源
        /// </summary>
        public static GameObject ResolvePrefabByAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            // 完整路径直接取
            if (address.StartsWith("Assets/", StringComparison.Ordinal))
                return AssetDatabase.LoadAssetAtPath<GameObject>(address);

            // 短名走打包配置里的资源别名
            var config = BuildBundleConfigura.Instance;
            var moduleList = config != null ? config.bundleModuleDataList : null;
            if (moduleList != null)
            {
                for (int i = 0; i < moduleList.Count; i++)
                {
                    var aliasList = moduleList[i] != null ? moduleList[i].assetAliasList : null;
                    if (aliasList == null)
                        continue;

                    for (int j = 0; j < aliasList.Length; j++)
                    {
                        var aliasInfo = aliasList[j];
                        if (aliasInfo == null || aliasInfo.alias != address)
                            continue;

                        if (aliasInfo.asset is GameObject prefabAsset)
                            return prefabAsset;
                    }
                }
            }

            // 兜底 工程内唯一同名预制体
            string[] guidList = AssetDatabase.FindAssets(address + " t:Prefab");
            GameObject found = null;
            for (int i = 0; i < guidList.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guidList[i]);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != address)
                    continue;

                // 有重名就直接放弃 交给上层报错 避免摆错资源
                if (found != null)
                    return null;

                found = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return found;
        }

        /// <summary>
        /// 取预制体的水平半径 结果按预制体缓存
        /// </summary>
        public static float ResolvePlaceRadius(GameObject prefab, Dictionary<GameObject, float> radiusCache)
        {
            if (prefab == null)
                return 0.05f;

            if (radiusCache != null && radiusCache.TryGetValue(prefab, out float cachedRadius))
                return cachedRadius;

            TryGetVisualBounds(prefab, out var bounds);
            float radius = Mathf.Clamp(
                Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f,
                0.02f,
                1f);

            radiusCache?.Add(prefab, radius);
            return radius;
        }

        /// <summary>
        /// 取预制体的摆放占地 半径与高度 用于间距与净空判断
        /// 渲染器与碰撞体一起算 取较大者 避免碰撞体比模型大时穿模
        /// </summary>
        public static void ResolvePlaceFootprint(GameObject prefab, out float radius, out float height)
        {
            radius = 0.05f;
            height = 0.05f;
            if (prefab == null)
                return;

            if (!TryCombineBounds(prefab, out var bounds))
                return;

            radius = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f, 0.02f, 1f);
            height = Mathf.Clamp(bounds.size.y, 0.02f, 2f);
        }

        /// <summary>
        /// 合并渲染器与碰撞体包围盒
        /// </summary>
        private static bool TryCombineBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            if (TryGetVisualBounds(root, out var visualBounds))
            {
                bounds = visualBounds;
                hasBounds = true;
            }

            if (TryGetPhysicsBounds(root, out var physicsBounds))
            {
                if (hasBounds)
                    bounds.Encapsulate(physicsBounds);
                else
                {
                    bounds = physicsBounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        /// <summary>
        /// 求预制体或实例的可视包围盒 跳过粒子/拖尾这类边界不可信的渲染器
        /// </summary>
        public static bool TryGetVisualBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
                return false;

            bool hasBounds = false;
            var rendererList = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rendererList.Length; i++)
            {
                var renderer = rendererList[i];
                if (!IsUsableRenderer(renderer))
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            if (hasBounds)
                return true;

            // 没有可用渲染器时退回碰撞体
            var colliderList = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliderList.Length; i++)
            {
                var collider = colliderList[i];
                if (collider == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(collider.bounds);
            }

            return hasBounds;
        }

        /// <summary>
        /// 渲染器边界是否可信
        /// </summary>
        private static bool IsUsableRenderer(Renderer renderer)
        {
            if (renderer == null || !renderer.enabled)
                return false;

            return renderer is not ParticleSystemRenderer
                && renderer is not TrailRenderer
                && renderer is not LineRenderer;
        }

        #endregion

        #region 落位实例化

        /// <summary>
        /// 按采样点在场景里实例化一个物品 保持预制体关联并登记撤销
        /// </summary>
        public static GameObject InstantiatePlacedItem(
            GameObject prefab,
            IItemTableData item,
            SurfaceHit hit,
            int stackCount,
            Transform parent,
            SurfacePlacerSettings settings,
            System.Random random,
            string objectName,
            bool keepPlacedInPlace = false,
            Quaternion? overrideRotation = null,
            float? overrideSurfaceOffset = null)
        {
            if (prefab == null || item == null || settings == null || random == null)
                return null;

            GameObject instance = parent != null
                ? PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject
                : PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return null;

            instance.name = string.IsNullOrEmpty(objectName) ? prefab.name : objectName;

            float surfaceOffset = Mathf.Max(0f, overrideSurfaceOffset ?? settings.surfaceOffset);
            Vector3 normal = hit.Normal.sqrMagnitude > 0.0001f ? hit.Normal.normalized : Vector3.up;
            float yaw = settings.randomYaw ? (float)(random.NextDouble() * 360.0) : 0f;
            // 先绕自身 up 转朝向 再整体贴合表面法线 这样斜面上也站得住
            Quaternion alignRotation = settings.alignToSurfaceNormal
                ? Quaternion.FromToRotation(Vector3.up, normal)
                : Quaternion.identity;
            Quaternion rotation = overrideRotation
                ?? alignRotation * Quaternion.AngleAxis(yaw, Vector3.up);
            Vector3 position = hit.Position + normal * surfaceOffset;

            instance.transform.SetPositionAndRotation(position, rotation);
            // 底面贴面时把最低点(渲染器与碰撞体取更低者)对齐承托面
            // 轴心对齐模式保留轴心落在锚点上的结果 便于和锚点位置完全一致
            if (settings.alignMode == EPlacementAlignMode.BottomToSurface)
                AlignBottomToSurface(instance, hit.Position, normal, surfaceOffset);

            ApplyItemBinding(instance, item, stackCount, settings);
            if (keepPlacedInPlace)
                ApplyStaticPlacement(instance);

            Undo.RegisterCreatedObjectUndo(instance, "表面摆放物品");
            return instance;
        }

        /// <summary>
        /// 摆成静态物 运行时不进世界物理 也就不用承托碰撞体
        /// </summary>
        private static void ApplyStaticPlacement(GameObject instance)
        {
            var itemInteract = instance.GetComponentInChildren<ItemInteract>(true);
            if (itemInteract != null)
            {
                var serializedObject = new SerializedObject(itemInteract);
                var staticProperty = serializedObject.FindProperty("isStaticPlaced");
                if (staticProperty != null)
                    staticProperty.boolValue = true;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            // 刚体一并冻结 否则运行时会自己掉下去
            var bodyList = instance.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodyList.Length; i++)
            {
                var body = bodyList[i];
                if (body == null)
                    continue;

                body.useGravity = false;
                body.isKinematic = true;
            }
        }

        /// <summary>
        /// 写入物品表 ID 与实例快照 让场景物品能被正常拾取
        /// </summary>
        private static void ApplyItemBinding(
            GameObject instance,
            IItemTableData item,
            int stackCount,
            SurfacePlacerSettings settings)
        {
            var itemInteract = instance.GetComponentInChildren<ItemInteract>(true);
            if (itemInteract == null)
                return;

            var serializedObject = new SerializedObject(itemInteract);

            var idProperty = serializedObject.FindProperty("itemTableID");
            if (idProperty != null)
                idProperty.intValue = item.ExcelItemId;

            bool writeSaveData = settings.writeSaveDataWhenStacked && stackCount > 1;
            int safeStack = Mathf.Max(1, stackCount);

            SetString(serializedObject, "saveData.instancedItemId",
                writeSaveData ? Guid.NewGuid().ToString() : string.Empty);
            SetInt(serializedObject, "saveData.excelItemId", writeSaveData ? item.ExcelItemId : 0);
            SetInt(serializedObject, "saveData.hasStackCount", writeSaveData ? safeStack : 0);
            SetInt(serializedObject, "saveData.maxStackCount", writeSaveData ? item.MaxStackCount : 0);
            SetInt(serializedObject, "saveData.itemStackType",
                writeSaveData ? (int)item.ItemStackType : 0);
            SetInt(serializedObject, "saveData.itemRarity", writeSaveData ? (int)item.ItemRarity : 0);
            SetInt(serializedObject, "saveData.currDurability", writeSaveData ? item.MaxDurability : 0);
            SetInt(serializedObject, "saveData.maxDurability", writeSaveData ? item.MaxDurability : 0);
            SetVector2Int(serializedObject, "saveData.dataSize",
                writeSaveData ? item.DataSize : Vector2Int.zero);
            SetVector2Int(serializedObject, "saveData.anchorPos", Vector2Int.zero);
            SetBool(serializedObject, "saveData.rotated", false);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 写 int 字段 找不到就跳过
        /// </summary>
        private static void SetInt(SerializedObject serializedObject, string propertyPath, int value)
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property != null)
                property.intValue = value;
        }

        /// <summary>
        /// 写 bool 字段 找不到就跳过
        /// </summary>
        private static void SetBool(SerializedObject serializedObject, string propertyPath, bool value)
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property != null)
                property.boolValue = value;
        }

        /// <summary>
        /// 写 string 字段 找不到就跳过
        /// </summary>
        private static void SetString(SerializedObject serializedObject, string propertyPath, string value)
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property != null)
                property.stringValue = value;
        }

        /// <summary>
        /// 写 Vector2Int 字段 找不到就跳过
        /// </summary>
        private static void SetVector2Int(
            SerializedObject serializedObject,
            string propertyPath,
            Vector2Int value)
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property != null)
                property.vector2IntValue = value;
        }

        #endregion
    }
}
