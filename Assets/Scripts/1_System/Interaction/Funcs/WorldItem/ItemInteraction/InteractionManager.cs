using MieMieFrameWork;
using MieMieFrameWork.M_InputSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 交互管理器 聚焦检测与 Hold 触发交互
    /// </summary>
    public class InteractionManager : MonoBehaviour, IInteraction
    {
        [SerializeField, LabelText("射线检测")]
        private InteractionDetector detector = new();
        /// <summary>
        /// 当前聚焦目标
        /// </summary>
        public IInteractableInterface CurrentFocus { get; private set; }

        /// <summary>
        /// 当前命中点
        /// </summary>
        public Vector3? CurrentHitPoint => hasCurrentContext ? currentContext.HitPoint : null;

        /// <summary>
        /// 当前聚焦距离
        /// </summary>
        public float CurrentDistance => hasCurrentContext ? currentContext.Distance : 0f;

        /// <summary>
        /// 当前提示文案
        /// </summary>
        public string CurrentPrompt =>
            CurrentFocus != null ? CurrentFocus.GetPromptText() : string.Empty;

        /// <summary> 交互检测射线起点 </summary>
        public Transform RayOrigin
        {
            get
            {
                detector.EnsureRayOrigin();
                return detector.RayOrigin;
            }
        }

        /// <summary> 输入管理器 </summary>
        private InputManager inputManager;
        /// <summary> 当前交互上下文 </summary>
        private InteractionContext currentContext;
        /// <summary> 是否已有有效上下文 </summary>
        private bool hasCurrentContext;

        /// <summary> 聚焦检测累计时间 </summary>
        private float focusDetectTimer;

        /// <summary> 聚焦检测间隔 约 20Hz 降低转视角时 SphereCast 频率 </summary>
        private const float FocusDetectInterval = 0.05f;

        private void Awake()
        {
            detector.EnsureRayOrigin();
        }

        private void Update()
        {
            if (inputManager == null)
            {
                if (ModuleHub.Instance == null)
                    return;
                inputManager = ModuleHub.Instance.GetManager<InputManager>();
                if (inputManager == null)
                    return;
            }

            Tick();
        }

        private void OnDestroy()
        {
            ClearFocus();
        }

        /// <summary>
        /// 每帧读 Hold 输入 聚焦 SphereCast 降到约 20Hz
        /// </summary>
        private void Tick()
        {
            focusDetectTimer += Time.deltaTime;
            if (focusDetectTimer >= FocusDetectInterval)
            {
                focusDetectTimer -= FocusDetectInterval;
                UpdateFocus();
            }

            if (CurrentFocus == null || !hasCurrentContext)
                return;

            if (!CurrentFocus.CanInteract(currentContext))
                return;

            if (!inputManager.IsInteractHoldCompleted)
                return;

            CurrentFocus.Interact(currentContext);
        }

        /// <summary>
        /// 更新聚焦目标
        /// </summary>
        private void UpdateFocus()
        {
            // 通过射线检测获取聚焦目标
            if (!detector.TryDetect(out RaycastHit hit, out IInteractableInterface newFocus))
            {
                if (CurrentFocus != null)
                    ClearFocus();
                return;
            }

            // 计算交互器与聚焦目标的距离
            Transform interactorTransform = detector.RayOrigin;
            float distance = interactorTransform != null
                ? Vector3.Distance(interactorTransform.position, hit.point)
                : 0f;

            // 创建交互上下文
            var newContext = new InteractionContext(interactorTransform, hit.point, distance);

            // 如果当前聚焦目标与新聚焦目标相同 则更新交互上下文
            if (ReferenceEquals(CurrentFocus, newFocus))
            {
                currentContext = newContext;
                hasCurrentContext = true;
                return;
            }

            // 如果当前聚焦目标不为空 则退出当前聚焦目标
            if (CurrentFocus != null)
            {
                CurrentFocus.OnFocusExit(currentContext);
                CurrentFocus = null;
                hasCurrentContext = false;
            }

            // 设置新的聚焦目标
            CurrentFocus = newFocus;
            currentContext = newContext;
            hasCurrentContext = true;
            CurrentFocus.OnFocusEnter(currentContext);
        }

        /// <summary>
        /// 清除当前聚焦
        /// </summary>
        private void ClearFocus()
        {
            if (CurrentFocus == null)
                return;

            if (hasCurrentContext)
                CurrentFocus.OnFocusExit(currentContext);

            CurrentFocus = null;
            hasCurrentContext = false;
        }

        /// <summary>
        /// 设置交互聚焦最大距离 仅影响拾取交互 不影响武器攻击距离
        /// </summary>
        public void SetMaxDistance(float distance)
        {
            detector.SetMaxDistance(distance);
        }

        /// <summary>
        /// 设置球形范围投射半径
        /// </summary>
        public void SetSphereCastRadius(float radius)
        {
            detector.SetSphereCastRadius(radius);
        }
    }
}
