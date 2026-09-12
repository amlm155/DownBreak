using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Interaction
{
    /// <summary>
    /// 锚点散布方式
    /// </summary>
    public enum EPlacementAnchorSpread
    {
        /// <summary> 单点 物品就摆在锚点原点 每个锚点默认 1 件</summary>
        AnchorPoint = 0,

        /// <summary> 在锚点范围内随机散布 用于一个锚点摆多件</summary>
        AreaRandom = 1,
    }

    /// <summary>
    /// 物品放置锚点 编辑器里摆出来的"可摆放面"
    /// 局部 +Y 为承托面法线 XZ 为摆放范围 表面摆放工具会在锚点范围内撒点
    /// 可选一键生成承托碰撞体 让被放置物品有实体托住 不生成则配合"静态摆放"冻结使用
    /// </summary>
    [DisallowMultipleComponent]
    public class ItemPlacementAnchor : MonoBehaviour
    {
        /// <summary> 承托碰撞体子节点名 </summary>
        public const string SupportColliderName = "AnchorSupportCollider";

        [SerializeField]
        [Tooltip("锚点范围 局部空间 XZ=摆放平面范围 Y=承托台厚度")]
        private Vector3 areaSize = new Vector3(0.6f, 0.02f, 0.6f);

        [SerializeField]
        [Tooltip("散布方式 单点=物品就摆在锚点原点 范围=在范围内随机撒开")]
        private EPlacementAnchorSpread spreadMode = EPlacementAnchorSpread.AnchorPoint;

        [SerializeField]
        [Tooltip("单点模式下的小抖动半径 0=多件完全同位")]
        private float jitterRadius;

        [SerializeField]
        [Tooltip("该锚点生成件数 0=用工具的全局件数 单点模式默认 1 件")]
        private int placeCountOverride;

        [SerializeField]
        [Tooltip("生成物相对锚点面的抬升 避免穿模")]
        private float surfaceOffset = 0.02f;

        [SerializeField]
        [Tooltip("随机水平朝向")]
        private bool randomYaw = true;

        [SerializeField]
        [Tooltip("该锚点可作为承托面 允许一键生成承托碰撞体")]
        private bool useAsSupportSurface = true;

        [SerializeField]
        [Tooltip("场景视图画范围框")]
        private bool drawGizmo = true;

        /// <summary> 锚点范围 局部空间 </summary>
        public Vector3 AreaSize => areaSize;

        /// <summary> 散布方式 </summary>
        public EPlacementAnchorSpread SpreadMode => spreadMode;

        /// <summary> 单点模式的抖动半径 </summary>
        public float JitterRadius => Mathf.Max(0f, jitterRadius);

        /// <summary> 该锚点件数覆盖 0 表示用工具全局值 </summary>
        public int PlaceCountOverride => placeCountOverride;

        /// <summary> 离面抬升 </summary>
        public float SurfaceOffset => surfaceOffset;

        /// <summary> 随机水平朝向 </summary>
        public bool RandomYaw => randomYaw;

        /// <summary> 是否可作为承托面 </summary>
        public bool UseAsSupportSurface => useAsSupportSurface;

        /// <summary> 承托面法线 </summary>
        public Vector3 Up => transform.up;

        /// <summary> 承托面中心 物品底面就落在这个平面上 </summary>
        public Vector3 Center => transform.position;

        /// <summary> 锚点范围 水平尺寸 </summary>
        public Vector2 AreaSizeXZ => new Vector2(
            Mathf.Max(0.01f, areaSize.x),
            Mathf.Max(0.01f, areaSize.z));

        /// <summary> 承托台厚度 </summary>
        public float SupportThickness => Mathf.Max(0.005f, areaSize.y);

        /// <summary>
        /// 取一个摆放点
        /// 单点模式返回锚点原点(可加小抖动) 范围模式在 areaSize 内随机撒点
        /// </summary>
        public Vector3 SamplePoint(System.Random random)
        {
            if (random == null)
                return Center;

            if (spreadMode == EPlacementAnchorSpread.AnchorPoint)
            {
                if (jitterRadius <= 0.0001f)
                    return Center;

                // 单点模式下的小抖动 让多件不至于完全重叠
                float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
                float radius = JitterRadius * Mathf.Sqrt((float)random.NextDouble());
                return Center
                    + (transform.right * Mathf.Cos(angle) + transform.forward * Mathf.Sin(angle)) * radius;
            }

            float halfX = Mathf.Max(0f, areaSize.x) * 0.5f;
            float halfZ = Mathf.Max(0f, areaSize.z) * 0.5f;
            float offsetX = Mathf.Lerp(-halfX, halfX, (float)random.NextDouble());
            float offsetZ = Mathf.Lerp(-halfZ, halfZ, (float)random.NextDouble());
            return Center + transform.right * offsetX + transform.forward * offsetZ;
        }

        /// <summary>
        /// 解析该锚点应生成的件数
        /// 单点模式默认 1 件(一个锚点一个位置) 范围模式用全局件数
        /// </summary>
        public int ResolvePlaceCount(int fallbackCount)
        {
            if (spreadMode == EPlacementAnchorSpread.AnchorPoint)
                return Mathf.Max(1, placeCountOverride);

            return placeCountOverride > 0
                ? placeCountOverride
                : Mathf.Max(0, fallbackCount);
        }

        /// <summary>
        /// 生成物旋转 锚点法线朝上 再叠加随机偏航
        /// </summary>
        public Quaternion ResolveRotation(System.Random random)
        {
            Vector3 up = Up.sqrMagnitude > 0.0001f ? Up.normalized : Vector3.up;
            float yaw = randomYaw && random != null
                ? (float)(random.NextDouble() * 360.0)
                : 0f;
            return Quaternion.FromToRotation(Vector3.up, up) * Quaternion.AngleAxis(yaw, Vector3.up);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmo)
                return;

            float areaX = Mathf.Max(0.01f, areaSize.x);
            float areaZ = Mathf.Max(0.01f, areaSize.z);
            float thickness = Mathf.Max(0.005f, areaSize.y);

            var previousMatrix = Gizmos.matrix;
            // 承托台画在平面下方 顶面就是摆放平面 物品底面贴着它
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.35f);
            Gizmos.DrawCube(Vector3.down * (thickness * 0.5f), new Vector3(areaX, thickness, areaZ));

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.95f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaX, 0.004f, areaZ));

            // 法线箭头 表示物品会被摆到哪一侧
            Vector3 tip = Vector3.up * 0.16f;
            Gizmos.DrawLine(Vector3.zero, tip);
            Gizmos.DrawLine(tip, tip + new Vector3(0.045f, -0.05f, 0.045f));
            Gizmos.DrawLine(tip, tip + new Vector3(-0.045f, -0.05f, 0.045f));

            Gizmos.matrix = previousMatrix;

            Handles.color = new Color(0.2f, 0.85f, 1f);
            Handles.Label(
                transform.position + Vector3.up * 0.22f,
                $"放置锚点 {name}\n"
                + (spreadMode == EPlacementAnchorSpread.AnchorPoint
                    ? $"单点(原点即落点)  件数 {(placeCountOverride > 0 ? placeCountOverride : 1)}"
                    : $"范围 {areaX:F2} x {areaZ:F2}  件数 {(placeCountOverride > 0 ? placeCountOverride.ToString() : "全局")}"));
        }
#endif
    }
}
