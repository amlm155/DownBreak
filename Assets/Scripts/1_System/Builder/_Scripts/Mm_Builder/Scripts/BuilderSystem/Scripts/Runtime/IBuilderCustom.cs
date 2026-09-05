using UnityEngine;

namespace  Mm_Budier
{
    /// <summary>
    /// 这个接口完全用于外部开发者实现 
    /// 实现其中的方法可以在建造系统的过程中插入自己的逻辑
    /// </summary>
    public interface IBuilderCustom
    {
        /// <summary>
        /// 自定义放置校验
        /// </summary>
        bool CustomPlaceValid(out BuilderPlacementReport placement,CubeData cubeData);

        /// <summary>
        /// 自定义破坏校验
        /// </summary>
        bool CustomBreakValid(out Vector3Int gridPos,CubeData cubeData);
        
        /// <summary>
        /// 自定义射线检测校验
        /// </summary>
        bool CustomRaycastValid(out RaycastHit hit);

        /// <summary>
        /// 自定义放置成功回调
        /// </summary>
        void CustorOnPlaceSucceeded(CubeInstance cubeInstance);

        /// <summary>
        /// 自定义破坏成功回调
        /// </summary>
        void CustorOnBreakSucceeded(CubeInstance cubeInstance);

    }
}