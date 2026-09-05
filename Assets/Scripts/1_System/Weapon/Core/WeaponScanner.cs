using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DBWeaponSystem
{
    /// <summary>
    /// 武器扫描命中结果
    /// </summary>
    public struct WeaponScanHit
    {
        /// <summary> 命中碰撞体 </summary>
        public Collider Collider;

        /// <summary> 命中点 </summary>
        public Vector3 Point;

        /// <summary> 命中法线 </summary>
        public Vector3 Normal;
    }

    /// <summary>
    /// 武器轨迹近战扫描 动画开窗期间按刃根+刃尖帧间 SphereCast
    /// 不结算伤害 只抛命中 由上层接 IDamageable
    /// </summary>
    public class WeaponScanner : MonoBehaviour
    {
        #region 配置

        /// <summary> 刃根挂点 </summary>
        [SerializeField]
        private Transform bladeRoot;

        /// <summary> 刃尖挂点 </summary>
        [SerializeField]
        private Transform bladeTip;

        /// <summary> 沿刃采样数 含根与尖 至少 2 </summary>
        [SerializeField,LabelText("沿刃采样数")]
        private int bladeSampleCount = 3;

        /// <summary> 扫描球半径 </summary>
        [SerializeField,LabelText("扫描球半径")]
        private float radius = 0.2f;

        /// <summary> 检测层 默认统一走 Interactable </summary>
        [SerializeField,LabelText("检测 Layer")]
        private LayerMask damageLayer;

        /// <summary> 位移距离额外余量 防高速穿帧 </summary>
        [SerializeField,LabelText("额外位移距离")]
        private float distancePadding = 0.02f;

        /// <summary> 本帧位移低于此值视为未移动 </summary>
        [SerializeField,LabelText("本帧位移低于此值视为未移动")]
        private float minSweepDistance = 0.001f;

        /// <summary> 命中缓冲长度 </summary>
        [SerializeField,LabelText("命中缓冲长度")]
        private int hitBufferSize = 8;

        /// <summary> 是否绘制调试 </summary>
        [SerializeField,LabelText("是否开启绘制调试")]
        private bool drawDebug;

        /// <summary> SphereCast 缓冲 </summary>
        private RaycastHit[] hitBuffer;

        /// <summary> 本段窗口已命中实例 </summary>
        private readonly HashSet<EntityId> hitInstanceHashList = new();

        /// <summary> 是否开窗中 </summary>
        private bool isWindowOpen;

        /// <summary> 上一帧刃根世界坐标 </summary>
        private Vector3 prevRootPos;

        /// <summary> 上一帧刃尖世界坐标 </summary>
        private Vector3 prevTipPos;

        /// <summary> 是否已有上一帧坐标 </summary>
        private bool hasPrevPos;

        /// <summary> 命中回调 同窗口同目标只进一次 </summary>
        public event Action<WeaponScanHit> OnHit;

        /// <summary> 窗口关闭回调 参数 本窗口是否命中过目标 </summary>
        public event Action<bool> OnWindowClosed;

        /// <summary> 当前是否在攻击窗口内 </summary>
        public bool IsWindowOpen => isWindowOpen;

        /// <summary> 刃根 </summary>
        public Transform BladeRoot => bladeRoot;

        /// <summary> 刃尖 </summary>
        public Transform BladeTip => bladeTip;

        /// <summary> 当前半径 </summary>
        public float Radius => radius;

        /// <summary> 当前额外位移余量 </summary>
        public float DistancePadding => distancePadding;

        /// <summary> 组件上配置的默认半径 用于空手回退 </summary>
        private float defaultRadius;

        /// <summary> 组件上配置的默认位移余量 用于空手回退 </summary>
        private float defaultDistancePadding;

        #endregion

        #region 生命周期与绑定
        private const string InteractableLayerName = "Interactable";

        private void Awake()
        {
            defaultRadius = radius;
            defaultDistancePadding = distancePadding;
            EnsureDefaultDamageLayer();
        }

        private void Start()
        {
            InitBuffers();
        }

        /// <summary>
        /// 绑定刃根与刃尖 装备武器时调用
        /// </summary>
        public void BindBlade(Transform root, Transform tip)
        {
            bladeRoot = root;
            bladeTip = tip;
            hasPrevPos = false;
        }

        /// <summary>
        /// 仅绑定刃尖 根为空时退化为单点扫描
        /// </summary>
        public void BindHitPoint(Transform tip)
        {
            bladeRoot = null;
            bladeTip = tip;
            hasPrevPos = false;
        }

        /// <summary>
        /// 按武器配置写入扫描半径与位移余量
        /// </summary>
        public void ApplyScanParams(float scanRadius, float padding)
        {
            SetRadius(scanRadius);
            SetDistancePadding(padding);
        }

        /// <summary>
        /// 恢复组件默认扫描参数 空手用
        /// </summary>
        public void RestoreDefaultScanParams()
        {
            radius = Mathf.Max(0.01f, defaultRadius);
            distancePadding = Mathf.Max(0f, defaultDistancePadding);
        }

        /// <summary>
        /// 设置扫描半径
        /// </summary>
        public void SetRadius(float value)
        {
            radius = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// 设置额外位移余量
        /// </summary>
        public void SetDistancePadding(float value)
        {
            distancePadding = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 设置检测层
        /// </summary>
        public void SetDamageLayer(LayerMask layer)
        {
            damageLayer = layer;
        }

        /// <summary>
        /// 默认统一扫 Interactable 能不能受伤交给 IDamageable
        /// </summary>
        private void EnsureDefaultDamageLayer()
        {
            if (damageLayer.value != 0)
                return;

            int layer = LayerMask.NameToLayer(InteractableLayerName);
            if (layer < 0)
                return;

            damageLayer = 1 << layer;
        }


        /// <summary>
        /// 打开攻击窗口 清空命中名单
        /// </summary>
        public void OpenWindow()
        {
            isWindowOpen = true;
            hitInstanceHashList.Clear();
            hasPrevPos = false;

            if (TryGetBladePose(out Vector3 rootPos, out Vector3 tipPos))
            {
                prevRootPos = rootPos;
                prevTipPos = tipPos;
                hasPrevPos = true;
            }
        }

        /// <summary>
        /// 关闭攻击窗口
        /// </summary>
        public void CloseWindow()
        {
            isWindowOpen = false;
            hasPrevPos = false;
            bool anyHit = hitInstanceHashList.Count > 0;
            hitInstanceHashList.Clear();
            OnWindowClosed?.Invoke(anyHit);
        }

        #endregion

        #region 扫描

        private void LateUpdate()
        {
            if (!isWindowOpen)
                return;

            if (!TryGetBladePose(out Vector3 currRootPos, out Vector3 currTipPos))
                return;

            if (!hasPrevPos)
            {
                prevRootPos = currRootPos;
                prevTipPos = currTipPos;
                hasPrevPos = true;
                return;
            }

            SweepBladeMotion(prevRootPos, prevTipPos, currRootPos, currTipPos);

            if (drawDebug)
            {
                Debug.DrawLine(prevRootPos, currRootPos, Color.yellow, 0f, false);
                Debug.DrawLine(prevTipPos, currTipPos, Color.yellow, 0f, false);
                Debug.DrawLine(currRootPos, currTipPos, Color.cyan, 0f, false);
            }

            prevRootPos = currRootPos;
            prevTipPos = currTipPos;
        }

        /// <summary>
        /// 沿上一帧到本帧的刃带采样并扫掠
        /// </summary>
        private void SweepBladeMotion(Vector3 fromRoot,
                                      Vector3 fromTip,
                                      Vector3 toRoot,
                                      Vector3 toTip)
        {
            int sampleCount = ResolveSampleCount();

            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount == 1 ? 1f : i / (float)(sampleCount - 1);
                Vector3 from = Vector3.Lerp(fromRoot, fromTip, t);
                Vector3 to = Vector3.Lerp(toRoot, toTip, t);
                SweepSegment(from, to);
            }
        }

        /// <summary>
        /// 单段扫掠 仅在有有效位移时检测
        /// </summary>
        private void SweepSegment(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float dist = delta.magnitude;
            if (dist < minSweepDistance)
                return;

            Vector3 direction = delta / dist;
            float castDist = dist + distancePadding;
            Sweep(from, direction, castDist);
        }

        /// <summary>
        /// 沿轨迹 SphereCast
        /// </summary>
        private void Sweep(Vector3 origin, Vector3 direction, float distance)
        {
            int hitCount = PhysicRayCast.SphereCastNonAlloc(
                origin,
                radius,
                direction,
                hitBuffer,
                distance,
                damageLayer,
                QueryTriggerInteraction.Collide,
                drawDebug);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitBuffer[i];
                if (hit.collider == null)
                    continue;

                TryReport(hit.collider, hit.point, hit.normal);
            }
        }

        /// <summary>
        /// 去重后抛出命中
        /// </summary>
        private void TryReport(Collider col, Vector3 point, Vector3 normal)
        {
            EntityId id = col.transform.root.GetEntityId();
            if (!hitInstanceHashList.Add(id))
                return;

            OnHit?.Invoke(new WeaponScanHit
            {
                Collider = col,
                Point = point,
                Normal = normal
            });
        }

        /// <summary>
        /// 读取当前刃根刃尖 无根时尖兼作根
        /// </summary>
        private bool TryGetBladePose(out Vector3 rootPos, out Vector3 tipPos)
        {
            rootPos = default;
            tipPos = default;

            if (bladeTip == null)
                return false;

            tipPos = bladeTip.position;
            rootPos = bladeRoot != null ? bladeRoot.position : tipPos;
            return true;
        }

        /// <summary>
        /// 解析采样数 单点时强制 1
        /// </summary>
        private int ResolveSampleCount()
        {
            if (bladeRoot == null || bladeRoot == bladeTip)
                return 1;

            return Mathf.Max(2, bladeSampleCount);
        }

        /// <summary>
        /// 初始化缓冲
        /// </summary>
        private void InitBuffers()
        {
            int size = Mathf.Max(1, hitBufferSize);
            hitBuffer = new RaycastHit[size];
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!TryGetBladePose(out Vector3 rootPos, out Vector3 tipPos))
                return;

            Gizmos.color = isWindowOpen ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(rootPos, radius);
            Gizmos.DrawWireSphere(tipPos, radius);
            Gizmos.DrawLine(rootPos, tipPos);

            int sampleCount = ResolveSampleCount();
            for (int i = 0; i < sampleCount; i++)
            {
                float t = sampleCount == 1 ? 1f : i / (float)(sampleCount - 1);
                Gizmos.DrawWireSphere(Vector3.Lerp(rootPos, tipPos, t), radius * 0.5f);
            }
        }
#endif
    }
}
