using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 这个抽象类只用于建造系统内部的过程约束 外部开发者不需要实现这个抽象类
    /// </summary>
    public abstract partial class IBuilderSystem : MonoBehaviour
    {
        /// <summary>
        /// 处理预览
        /// </summary>
        public abstract void HandlePreview(BuilderPlacementReport placement, CubeData cubeData, bool canPlace);

        /// <summary>
        /// 处理保存
        /// </summary>
        public abstract void HandleSave();

        /// <summary>
        /// 处理加载
        /// </summary>

        public abstract void HandleLoad();

        /// <summary>
        /// 处理射线
        /// </summary>
        public abstract bool HandleRaycast(out RaycastHit hit);

        /// <summary>
        /// 处理放置
        /// </summary>
        public abstract void HandlePlace(BuilderPlacementReport placement, CubeData cubeData);

        /// <summary>
        /// 处理破坏
        /// </summary>
        public abstract void HandleBreak(Vector3Int gridPos, CubeData cubeData);

        /// <summary>
        /// 处理放置校验
        /// </summary>
 
        public abstract bool HandlePlaceValid(BuilderPlacementReport placement,CubeData cubeData);

        /// <summary>
        /// 处理破坏校验
        /// </summary>
        public abstract bool HandleBreakValid(Vector3Int gridPos,CubeData cubeData);
    }
}