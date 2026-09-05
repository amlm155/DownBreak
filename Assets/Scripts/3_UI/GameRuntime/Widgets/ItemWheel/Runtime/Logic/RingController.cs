using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 单个扇环交互与选中动画 不含物品数据
    /// </summary>
    public class RingController : MonoBehaviour, IRingBehaviour
    {
        private const string IconPath = "Icon";
        private const string StackNumPath = "Icon/num";

        [SerializeField] private float scaleUpFactor = 1.08f;
        [SerializeField] private float tweenDuration = 0.22f;
        [SerializeField] private float backEaseAmplitude = 0.2f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.65f, 1f);

        /// <summary> 扇区物品图标 </summary>
        private Image itemIcon;

        /// <summary> 堆叠数量 </summary>
        private TextMeshProUGUI stackNum;

        /// <summary> 扇环绘制 </summary>
        private RingDraw ringDraw;

        /// <summary> 初始缩放 </summary>
        private Vector3 originalScale;

        /// <summary> 初始扇环色 </summary>
        private Color originalRingColor;

        /// <summary> 缩放补间 </summary>
        private Tween scaleTween;

        /// <summary> 颜色补间 </summary>
        private Tween colorTween;

        private void Awake()
        {
            InitComponents();
            originalScale = transform.localScale;

            if (ringDraw != null)
                originalRingColor = ringDraw.color;

            SetItemDisplay(null, false, 0);
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitComponents()
        {
            ringDraw = GetComponent<RingDraw>();

            Transform iconTf = transform.Find(IconPath);
            if (iconTf != null)
            {
                itemIcon = iconTf.GetComponent<Image>();
            }
            else
            {
                itemIcon = GetComponentInChildren<Image>(true);
                Debug.LogWarning($"{name} 缺少 {IconPath} 已回退 GetComponentInChildren", this);
            }

            Transform numTf = transform.Find(StackNumPath);
            if (numTf != null)
            {
                stackNum = numTf.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogWarning($"{name} 缺少 {StackNumPath} 无法显示堆叠数", this);
            }
        }

        private void OnDestroy()
        {
            scaleTween?.Kill();
            colorTween?.Kill();
        }

        /// <summary>
        /// 设置扇区图标与堆叠数 空图标则隐藏
        /// </summary>
        public void SetItemDisplay(Sprite icon, bool showStack, int stackCount)
        {
            if (itemIcon == null)
                return;

            itemIcon.sprite = icon;
            bool hasIcon = icon != null;
            itemIcon.preserveAspect = hasIcon;
            itemIcon.enabled = hasIcon;
            if (itemIcon.gameObject.activeSelf != hasIcon)
                itemIcon.gameObject.SetActive(hasIcon);

            RefreshStackNum(hasIcon && showStack, stackCount);
        }

        /// <summary>
        /// 兼容旧调用 无堆叠显示
        /// </summary>
        public void SetItemIcon(Sprite icon)
        {
            SetItemDisplay(icon, false, 0);
        }

        /// <summary>
        /// 刷新堆叠 TMP
        /// </summary>
        private void RefreshStackNum(bool show, int stackCount)
        {
            if (stackNum == null)
                return;

            if (stackNum.gameObject.activeSelf != show)
                stackNum.gameObject.SetActive(show);

            if (show)
                stackNum.text = stackCount.ToString();
        }

        public void OnEnter()
        {
            OnEnterAnimation();
        }

        public void OnExit()
        {
            OnExitAnimation();
        }

        /// <summary>
        /// 移入动画 外扩缩放与高亮
        /// </summary>
        private void OnEnterAnimation()
        {
            KillActiveTween();

            scaleTween = transform
                .DOScale(originalScale * scaleUpFactor, tweenDuration)
                .SetEase(Ease.OutBack, backEaseAmplitude)
                .SetUpdate(true);

            if (ringDraw != null)
            {
                colorTween = ringDraw
                    .DOColor(highlightColor, tweenDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// 移出动画 缩回与还原颜色
        /// </summary>
        private void OnExitAnimation()
        {
            KillActiveTween();

            scaleTween = transform
                .DOScale(originalScale, tweenDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);

            if (ringDraw != null)
            {
                colorTween = ringDraw
                    .DOColor(originalRingColor, tweenDuration)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true);
            }
        }

        /// <summary>
        /// 清理当前扇环上的补间
        /// </summary>
        private void KillActiveTween()
        {
            scaleTween?.Kill();
            colorTween?.Kill();
            scaleTween = null;
            colorTween = null;
        }
    }
}
