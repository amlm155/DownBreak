using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 世界里某一个方块实例
    /// 这个类运行时,实例化出来的方块对象,既可以用于运行时管理,也可以用于保存和加载
    /// </summary>
    public class CubeInstance
    {
        // 配置数据
        public CubeData data;
        // 实例化出来的的对象
        public GameObject instantiateCube;
        // 放置起始网格坐标
        public Vector3Int originGridPos;
        // 放置旋转角度
        public ECubeRotation rotation;

        public CubeInstance(CubeData data, GameObject instantiateCube, BuilderPlacementReport placement)
        {
            this.data = data;
            this.instantiateCube = instantiateCube;
            originGridPos = placement.OriginGridPos;
            rotation = placement.ERotation;
        }

        
    }
}
