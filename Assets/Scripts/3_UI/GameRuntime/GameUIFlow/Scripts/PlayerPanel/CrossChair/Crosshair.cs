using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    public enum ECrosshairType
    {
        // 正常点
        Point,
        // 十字有点
        Crosshair,
        // 十字无点
        CrosshairNoPoint,
        // X打击效果
        XCrosshair
    }

    public class Crosshair : MonoBehaviour
    {
        [SerializeField]
        private Texture2D PointCrosshair;
        [SerializeField]
        private Texture2D CrosshairNoPoint;

        [SerializeField]
        private Texture2D CrosshairTexture;

        [SerializeField]
        private Texture2D XCrosshair;

        [SerializeField]
        private RawImage CrosshairRawImage;

        /// <summary> 击中放大倍率 </summary>
        [SerializeField]
        private float hitScale = 1.35f;

        /// <summary> 击中放大时长 </summary>
        [SerializeField]
        private float punchInDuration = 0.05f;

        /// <summary> 击中缓动缩回时长 </summary>
        [SerializeField]
        private float easeOutDuration = 0.22f;

        /// <summary> 移动时微扩倍率 </summary>
        [SerializeField]
        private float moveScale = 1.12f;

        /// <summary> 移动缩放切换时长 </summary>
        [SerializeField]
        private float moveDuration = 0.12f;

        /// <summary> 初始缩放 </summary>
        private Vector3 baseScale = Vector3.one;

        /// <summary> 击中动效 tween </summary>
        private Tween hitTween;

        /// <summary> 移动微扩 tween </summary>
        private Tween moveTween;

        /// <summary> 当前是否移动展开态 </summary>
        private bool isMovingExpanded;

        /// <summary> 当前准心样式 不含临时 X 击中 </summary>
        private ECrosshairType currentType = ECrosshairType.Point;

        private void Awake()
        {
            CacheBaseScale();
        }

        private void Start(){
            SetCrosshairActive(true);
        }

        private void OnDestroy()
        {
            KillScaleTweens();
        }

        /// <summary>
        /// 切换准心样式 showAnimation 对 Crosshair/CrosshairNoPoint 表示移动微扩 X 表示击中
        /// </summary>
        public void SetCrosshair(ECrosshairType crosshairType, bool showAnimation)
        {
            // 先停掉缩放动效再改纹理 避免 tween 与基准缩放互相污染
            KillScaleTweens();
            isMovingExpanded = false;

            switch (crosshairType)
            {
                case ECrosshairType.Point:
                    if (CrosshairRawImage.texture != PointCrosshair)
                    {
                        CrosshairRawImage.texture = PointCrosshair;
                    }

                    break;
                case ECrosshairType.CrosshairNoPoint:
                    if (CrosshairRawImage.texture != CrosshairNoPoint)
                    {
                        CrosshairRawImage.texture = CrosshairNoPoint;
                    }

                    break;
                case ECrosshairType.Crosshair:
                    if (CrosshairRawImage.texture != CrosshairTexture)
                    {
                        CrosshairRawImage.texture = CrosshairTexture;
                    }

                    break;
                case ECrosshairType.XCrosshair:
                    if (CrosshairRawImage.texture != XCrosshair)
                    {
                        CrosshairRawImage.texture = XCrosshair;
                    }

                    break;
            }

            // 铺满后再锁定基准缩放 最后才播微扩/击中
            ApplyStretchFill();
            baseScale = Vector3.one;

            // X 是临时击中态 不覆盖常驻样式
            if (crosshairType != ECrosshairType.XCrosshair)
                currentType = crosshairType;

            switch (crosshairType)
            {
                case ECrosshairType.Point:
                    break;
                case ECrosshairType.CrosshairNoPoint:
                case ECrosshairType.Crosshair:
                    SetMoving(showAnimation);
                    break;
                case ECrosshairType.XCrosshair:
                    XCrosshairAnimation(showAnimation);
                    break;
            }
        }

        /// <summary>
        /// 攻击挥空 当前准心放大回弹
        /// </summary>
        public void PlayAttackPunch()
        {
            XCrosshairAnimation(true);
        }

        /// <summary>
        /// 攻击命中 切 X 播放大后还原常驻准心
        /// </summary>
        public void PlayHitMarker()
        {
            if (CrosshairRawImage == null)
                return;

            var restoreType = currentType;
            bool restoreMoving = isMovingExpanded;

            hitTween?.Kill();
            moveTween?.Kill();
            moveTween = null;

            if (CrosshairRawImage.texture != XCrosshair)
                CrosshairRawImage.texture = XCrosshair;

            ApplyStretchFill();
            baseScale = Vector3.one;
            isMovingExpanded = restoreMoving;

            Vector3 restScale = restoreMoving ? baseScale * moveScale : baseScale;
            Transform target = CrosshairRawImage.transform;
            target.localScale = restScale;

            hitTween = DOTween.Sequence()
                .Append(target.DOScale(baseScale * hitScale, punchInDuration).SetEase(Ease.OutQuad))
                .Append(target.DOScale(restScale, easeOutDuration).SetEase(Ease.OutBack))
                .SetUpdate(true)
                .OnComplete(() => SetCrosshair(restoreType, restoreMoving))
                .OnKill(() =>
                {
                    if (CrosshairRawImage != null)
                    {
                        CrosshairRawImage.transform.localScale =
                            isMovingExpanded ? baseScale * moveScale : baseScale;
                    }
                });
        }

        /// <summary>
        /// 全向拉伸铺满父节点 避免 SetNativeSize 把框撑变形
        /// </summary>
        private void ApplyStretchFill()
        {
            if (CrosshairRawImage == null)
                return;

            var rect = CrosshairRawImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        /// <summary>
        /// 终止准心缩放相关 tween
        /// </summary>
        private void KillScaleTweens()
        {
            hitTween?.Kill();
            hitTween = null;
            moveTween?.Kill();
            moveTween = null;
        }

        /// <summary>
        /// 显示隐藏准心
        /// </summary>
        public void SetCrosshairActive(bool isActive)
        {
            CrosshairRawImage.gameObject.SetActive(isActive);
        }

        /// <summary>
        /// FPS 移动微扩 true 微微放大 false 缓回原大小
        /// </summary>
        public void SetMoving(bool isMoving)
        {
            if (CrosshairRawImage == null) return;
            if (isMoving == isMovingExpanded && (moveTween == null || !moveTween.IsActive()))
            {
                // 已是目标态且无残留 tween 时直接钉死缩放
                CrosshairRawImage.transform.localScale =
                    isMoving ? baseScale * moveScale : baseScale;
                return;
            }

            if (isMoving == isMovingExpanded && moveTween != null && moveTween.IsActive())
                return;

            isMovingExpanded = isMoving;
            moveTween?.Kill();

            Transform target = CrosshairRawImage.transform;
            Vector3 toScale = isMoving ? baseScale * moveScale : baseScale;
            moveTween = target.DOScale(toScale, moveDuration)
                .SetEase(isMoving ? Ease.OutQuad : Ease.OutCubic)
                .SetUpdate(true);
        }

        /// <summary>
        /// X准心击中动效 放大后缓动缩回
        /// </summary>
        public void XCrosshairAnimation(bool showAnimation)
        {
            if (CrosshairRawImage == null) return;

            hitTween?.Kill();
            Transform target = CrosshairRawImage.transform;

            if (!showAnimation)
            {
                target.localScale = isMovingExpanded ? baseScale * moveScale : baseScale;
                return;
            }

            Vector3 restScale = isMovingExpanded ? baseScale * moveScale : baseScale;
            target.localScale = restScale;
            hitTween = DOTween.Sequence()
                .Append(target.DOScale(baseScale * hitScale, punchInDuration).SetEase(Ease.OutQuad))
                .Append(target.DOScale(restScale, easeOutDuration).SetEase(Ease.OutBack))
                .SetUpdate(true)
                .OnKill(() =>
                {
                    if (CrosshairRawImage != null)
                    {
                        CrosshairRawImage.transform.localScale =
                            isMovingExpanded ? baseScale * moveScale : baseScale;
                    }
                });
        }

        /// <summary>
        /// 缓存当前基准缩放 动效进行中不采样
        /// </summary>
        private void CacheBaseScale()
        {
            if (CrosshairRawImage == null) return;
            bool moveActive = moveTween != null && moveTween.IsActive();
            bool hitActive = hitTween != null && hitTween.IsActive();
            if (!isMovingExpanded && !moveActive && !hitActive)
            {
                baseScale = CrosshairRawImage.transform.localScale;
            }
        }
    }
}
