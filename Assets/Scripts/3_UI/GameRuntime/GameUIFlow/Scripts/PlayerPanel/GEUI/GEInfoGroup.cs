using System;
using DG.Tweening;
using MieMieFrameWork.Asset;
using MmInventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    public class GEInfoGroup : MonoBehaviour
    {
        /// <summary> GE 图标 </summary>
        [SerializeField]
        private Image Icon;

        /// <summary> GE 描述文本 </summary>
        [SerializeField]
        private TextMeshProUGUI Info;

        /// <summary> 左右滑动距离 </summary>
        [SerializeField]
        private float slideDistance = 400f;

        /// <summary> 向右弹出时长 </summary>
        [SerializeField]
        private float showDuration = 0.25f;

        /// <summary> 向左隐藏时长 </summary>
        [SerializeField]
        private float hideDuration = 0.2f;

        /// <summary> 当前 GE Id </summary>
        private int geId;

        /// <summary> 是否已初始化组件 </summary>
        private bool isComponentsInitialized;

        /// <summary> 滑动视觉节点 </summary>
        private RectTransform visualRect;

        /// <summary> 显示态 X </summary>
        private float shownPosX;

        /// <summary> 隐藏态 X </summary>
        private float hiddenPosX;

        /// <summary> 进出场 tween </summary>
        private Tween slideTween;

        public int GeId => geId;

        /// <summary>
        /// 缓存节点并准备滑动层 只允许壳调用一次
        /// </summary>
        public void InitComponents()
        {
            if (isComponentsInitialized)
                return;
            isComponentsInitialized = true;

            if (Icon == null)
                Icon = transform.Find("Icon").GetComponent<Image>();
            if (Info == null)
                Info = transform.Find("Info").GetComponent<TextMeshProUGUI>();

            EnsureVisualRect();
            shownPosX = visualRect.anchoredPosition.x;
            hiddenPosX = shownPosX - slideDistance;
        }

        /// <summary>
        /// 写入当前 GE 展示数据
        /// </summary>
        public void SetInfo(int geId)
        {
            this.geId = geId;
            var geTableData = LubanTables.Tables.TbGameplayEffect.Get(geId);
            Info.text = geTableData.Name;

            var iconSprite = string.IsNullOrEmpty(geTableData.IconAddress)
                ? null
                : MmAssetMgr.LoadAsset<Sprite>(geTableData.IconAddress);
            Icon.sprite = iconSprite;
            Icon.enabled = iconSprite != null;
        }

        /// <summary>
        /// 从左侧向右弹出显示
        /// </summary>
        public void PlayShow()
        {
            KillSlideTween();
            visualRect.anchoredPosition = new Vector2(hiddenPosX, visualRect.anchoredPosition.y);
            slideTween = visualRect.DOAnchorPosX(shownPosX, showDuration)
                .SetEase(Ease.OutCubic);
        }

        /// <summary>
        /// 向左滑出隐藏
        /// </summary>
        public void PlayHide(Action onHidden)
        {
            KillSlideTween();
            slideTween = visualRect.DOAnchorPosX(hiddenPosX, hideDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() => onHidden?.Invoke());
        }

        private void OnDestroy()
        {
            KillSlideTween();
        }

        /// <summary>
        /// 根节点交给 VerticalLayoutGroup 占位 只滑动 Visual 避免布局把位移打回
        /// </summary>
        private void EnsureVisualRect()
        {
            Transform existVisual = transform.Find("Visual");
            if (existVisual != null)
            {
                visualRect = existVisual as RectTransform;
                return;
            }

            var visualGo = new GameObject("Visual", typeof(RectTransform));
            visualRect = visualGo.GetComponent<RectTransform>();
            visualRect.SetParent(transform, false);
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.offsetMin = Vector2.zero;
            visualRect.offsetMax = Vector2.zero;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == visualRect)
                    continue;
                child.SetParent(visualRect, false);
            }
        }

        /// <summary>
        /// 停掉进出场 tween
        /// </summary>
        private void KillSlideTween()
        {
            slideTween?.Kill();
            slideTween = null;
        }
    }
}
