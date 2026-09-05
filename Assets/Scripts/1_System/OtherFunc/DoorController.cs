using System.Collections;
using Interaction;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DBOtherFunc
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class DoorController : InteractableBase, IPressInteractable
    {
        /// <summary>
        /// 门动画控制器
        /// </summary>
        [SerializeField]
        private Animator doorAnimator;

        /// <summary>
        /// 阻挡通行的碰撞体
        /// </summary>
        [SerializeField]
        private BoxCollider blockingCollider;

        /// <summary>
        /// 开门动画状态名
        /// </summary>
        private const string OpenStateName = "Open";

        /// <summary>
        /// 关门动画状态名
        /// </summary>
        private const string CloseStateName = "Close";

        /// <summary>
        /// 当前是否已经打开
        /// </summary>
        private bool isDoorOpen;

        /// <summary>
        /// 当前是否正在播放动画
        /// </summary>
        private bool isAnimating;

        /// <summary>
        /// 初始化门组件
        /// </summary>
        private void Awake()
        {
            InitComponents();
        }

        /// <summary>
        /// 动画期间拒绝新的交互请求
        /// </summary>
        public override bool CanInteract(InteractionContext ctx)
        {
            return !isAnimating;
        }

        /// <summary>
        /// 切换门的开关状态
        /// </summary>
        public override void Interact(InteractionContext ctx)
        {
            if (isAnimating)
                return;

            StartCoroutine(SwitchDoor());
        }

        /// <summary>
        /// 返回当前门状态对应的提示
        /// </summary>
        public override string GetPromptText()
        {
            return isDoorOpen ? "关闭" : "打开";
        }

        /// <summary>
        /// 在场景中预览开门状态
        /// </summary>
        [Button("预览开门")]
        private void PreviewOpenDoor()
        {
            SetPreviewDoorState(false);
        }

        /// <summary>
        /// 在场景中预览关门状态
        /// </summary>
        [Button("预览关门")]
        private void PreviewCloseDoor()
        {
            SetPreviewDoorState(true);
        }

        /// <summary>
        /// 缓存门动画和碰撞体
        /// </summary>
        private void InitComponents()
        {
            doorAnimator = GetComponent<Animator>();
            blockingCollider.enabled = !isDoorOpen;
        }

        /// <summary>
        /// 播放目标状态动画并同步碰撞
        /// </summary>
        private IEnumerator SwitchDoor()
        {
            isAnimating = true;
            bool isOpening = !isDoorOpen;
            blockingCollider.enabled = false;
            doorAnimator.Play(isOpening ? OpenStateName : CloseStateName, 0, 0f);

            yield return null;
            AnimatorStateInfo stateInfo = doorAnimator.GetCurrentAnimatorStateInfo(0);
            while (stateInfo.normalizedTime < 1f)
            {
                yield return null;
                stateInfo = doorAnimator.GetCurrentAnimatorStateInfo(0);
            }

            isDoorOpen = isOpening;
            blockingCollider.enabled = true;
            isAnimating = false;
        }

        /// <summary>
        /// 编辑器采样终点或运行时播放门动画
        /// </summary>
        private void SetPreviewDoorState(bool isOpening)
        {
            if (Application.isPlaying)
            {
                if (isAnimating || isDoorOpen == isOpening)
                    return;

                StartCoroutine(SwitchDoor());
                return;
            }

            doorAnimator.Play(isOpening ? OpenStateName : CloseStateName, 0, 1f);
            doorAnimator.Update(0f);
            isDoorOpen = isOpening;
            isAnimating = false;
            blockingCollider.enabled = true;
        }
    }
}
