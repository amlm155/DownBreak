/// <summary>
/// TipPanel Logic层 - 用户编写
/// </summary>

using System.Collections.Generic;
using DG.Tweening;
using MmUIFrameWork.Core;
using TMPro;
using UnityEngine;
namespace MieMieUIFrameWork.Runtime
{
    
    internal class TipPanel : UIWindowBase
    {
        internal TipPanelGen View { get; private set; }
    
        /// <summary> 活跃提示条 新在前 </summary>
        private readonly List<ActiveTip> activeTipList = new();
    
        /// <summary> 条目标板 隐藏不参与显示 </summary>
        private RectTransform tipTemplate;
    
        /// <summary> 堆叠根节点 UIContent </summary>
        private RectTransform stackRoot;
    
        private const float TipWidth = 500f;
        private const float TipHeight = 100f;
        private const float TipSpacing = 10f;
        private const int MaxVisibleTip = 5;
        private const float SlideInDuration = 0.35f;
        private const float HoldDuration = 1.2f;
        private const float FadeOutDuration = 0.45f;
        private const float ChaseDuration = 0.25f;
        private const float SlideFromX = 200f;
    
        /// <summary>
        /// 单条活跃提示 Slot 负责上推 Visual 负责进出场
        /// </summary>
        private sealed class ActiveTip
        {
            public RectTransform SlotRect;
            public RectTransform VisualRect;
            public CanvasGroup CanvasGroup;
            public Tween LifeTween;
        }
    
        protected override void OnAwake()
        {
            base.OnAwake();
            View = UIContent.GetComponent<TipPanelGen>();
            InitTipStack();
        }
    
        protected override void OnShow()
        {
            base.OnShow();
        }
    
        protected override void OnHide()
        {
            base.OnHide();
            ClearAllTips();
        }
    
        protected override void OnDestroy()
        {
            base.OnDestroy();
            ClearAllTips();
        }
    
        /// <summary>
        /// 入队一条提示并打开面板
        /// </summary>
        public static void Push(string tip)
        {
            var panel = UIHub.Instance.ShowWindow<TipPanel>();
            panel?.ShowTip(tip);
        }
    
        /// <summary>
        /// 追逐式弹出 多条可同时存在
        /// </summary>
        public void ShowTip(string tip)
        {
            if (activeTipList.Count >= MaxVisibleTip)
                RemoveTip(activeTipList[activeTipList.Count - 1], false);
    
            ChaseExistingUp();
            SpawnTip(tip);
        }
    
        /// <summary>
        /// 初始化模板与堆叠根
        /// </summary>
        private void InitTipStack()
        {
            tipTemplate = View.ImageRectRectTransform;
            stackRoot = View.transform as RectTransform;
    
            tipTemplate.gameObject.SetActive(false);
    
            float stackHeight = TipHeight * MaxVisibleTip + TipSpacing * (MaxVisibleTip - 1);
            stackRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, stackHeight);
        }
    
        /// <summary>
        /// 已有 Slot 上推 不打断 Visual 进出场
        /// </summary>
        private void ChaseExistingUp()
        {
            for (int i = 0; i < activeTipList.Count; i++)
            {
                var tip = activeTipList[i];
                float targetY = (i + 1) * (TipHeight + TipSpacing);
                tip.SlotRect.DOKill(false);
                tip.SlotRect.DOAnchorPosY(targetY, ChaseDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }
    
        /// <summary>
        /// 在底部生成新条并播生命周期
        /// </summary>
        private void SpawnTip(string tipText)
        {
            var slotGo = new GameObject("TipSlot", typeof(RectTransform));
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.SetParent(stackRoot, false);
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.sizeDelta = new Vector2(TipWidth, TipHeight);
            slotRect.anchoredPosition = Vector2.zero;
    
            var visualGo = Object.Instantiate(tipTemplate.gameObject, slotRect);
            visualGo.name = "TipVisual";
            visualGo.SetActive(true);
    
            var sequence = visualGo.GetComponent<DOTweenSequence>();
            if (sequence != null)
                sequence.enabled = false;
    
            var visualRect = visualGo.GetComponent<RectTransform>();
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.offsetMin = Vector2.zero;
            visualRect.offsetMax = Vector2.zero;
            visualRect.anchoredPosition = new Vector2(SlideFromX, 0f);
    
            var infoText = visualGo.GetComponentInChildren<TextMeshProUGUI>(true);
            infoText.text = tipText;
    
            var canvasGroup = visualGo.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = visualGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
    
            var activeTip = new ActiveTip
            {
                SlotRect = slotRect,
                VisualRect = visualRect,
                CanvasGroup = canvasGroup,
            };
            activeTipList.Insert(0, activeTip);
    
            var lifeSeq = DOTween.Sequence().SetUpdate(true);
            lifeSeq.Append(visualRect.DOAnchorPosX(0f, SlideInDuration).SetEase(Ease.OutCubic));
            lifeSeq.AppendInterval(HoldDuration);
            lifeSeq.Append(canvasGroup.DOFade(0f, FadeOutDuration).SetEase(Ease.InQuad));
            lifeSeq.Join(visualRect.DOAnchorPosX(SlideFromX, FadeOutDuration).SetEase(Ease.InCubic));
            lifeSeq.OnComplete(() => RemoveTip(activeTip, true));
            activeTip.LifeTween = lifeSeq;
        }
    
        /// <summary>
        /// 移除一条 可选紧凑重排
        /// </summary>
        private void RemoveTip(ActiveTip tip, bool compact)
        {
            if (tip == null || !activeTipList.Contains(tip))
                return;
    
            tip.LifeTween?.Kill();
            tip.SlotRect.DOKill(false);
            tip.VisualRect.DOKill(false);
            activeTipList.Remove(tip);
    
            if (tip.SlotRect != null)
                Object.Destroy(tip.SlotRect.gameObject);
    
            if (compact)
                CompactTips();
    
            if (activeTipList.Count == 0)
                UIHub.Instance.HideWindow<TipPanel>();
        }
    
        /// <summary>
        /// 按当前顺序重排 Slot Y
        /// </summary>
        private void CompactTips()
        {
            for (int i = 0; i < activeTipList.Count; i++)
            {
                var tip = activeTipList[i];
                float targetY = i * (TipHeight + TipSpacing);
                tip.SlotRect.DOKill(false);
                tip.SlotRect.DOAnchorPosY(targetY, ChaseDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }
    
        /// <summary>
        /// 清空全部提示条
        /// </summary>
        private void ClearAllTips()
        {
            for (int i = activeTipList.Count - 1; i >= 0; i--)
            {
                var tip = activeTipList[i];
                tip.LifeTween?.Kill();
                tip.SlotRect.DOKill(false);
                tip.VisualRect.DOKill(false);
                if (tip.SlotRect != null)
                    Object.Destroy(tip.SlotRect.gameObject);
            }
    
            activeTipList.Clear();
        }
    }
    
}