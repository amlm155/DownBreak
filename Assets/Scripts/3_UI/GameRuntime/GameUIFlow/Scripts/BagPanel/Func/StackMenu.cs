using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace MieMieUIFrameWork.Runtime
{
    
    /// <summary>
    /// 堆叠数量菜单 丢弃与拆分共用 Ensure 确认 Cancel/CloseRect 取消
    /// </summary>
    public class StackMenu : MonoBehaviour
    {
        /// <summary> 用途 </summary>
        public enum EStackMenuMode
        {
            /// <summary> 丢弃 </summary>
            Throw = 0,
            /// <summary> 拆分 </summary>
            Split = 1,
        }
    
        /// <summary> 关闭区按钮 </summary>
        [SerializeField]
        private Button closeButton;

        /// <summary> 数量文案 </summary>
        [SerializeField]
        private TextMeshProUGUI showNumsTmp;
    
        /// <summary> 数量滑条 </summary>
        [SerializeField]
        private Slider amountSlider;
    
        /// <summary> 确认按钮 </summary>
        [SerializeField]
        private Button ensureButton;
    
        /// <summary> 取消按钮 </summary>
        [SerializeField]
        private Button cancelButton;
    
        /// <summary> 确认回调 参数为选中数量 </summary>
        private Action<int> confirmHandler;
    
        /// <summary> 取消回调 </summary>
        private Action cancelHandler;

        /// <summary> 当前模式 </summary>
        public EStackMenuMode Mode { get; private set; }

        /// <summary> 当前选中数量 </summary>
        public int CurrentAmount { get; private set; }

        /// <summary> 是否正在显示 </summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary> 是否已绑定按钮 </summary>
        private bool isInited;

        /// <summary>
        /// 绑定按钮并默认隐藏 由背包壳调用一次
        /// </summary>
        public void InitComponents()
        {
            if (isInited)
                return;

            BindButtonEvents();
            gameObject.SetActive(false);
            isInited = true;
        }
    
        private void OnDestroy()
        {
            if (amountSlider != null)
                amountSlider.onValueChanged.RemoveListener(OnSliderChanged);
    
            if (ensureButton != null)
                ensureButton.onClick.RemoveListener(Confirm);
    
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(Cancel);
    
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Cancel);
        }
    
        /// <summary>
        /// 打开菜单 min~max 闭区间
        /// </summary>
        public void Show(EStackMenuMode eMode,
                         int minAmount,
                         int maxAmount,
                         int defaultAmount,
                         Action<int> onConfirm,
                         Action onCancel)
        {
            Mode = eMode;
            confirmHandler = onConfirm;
            cancelHandler = onCancel;
    
            int minValue = Mathf.Max(1, minAmount);
            int maxValue = Mathf.Max(minValue, maxAmount);
            int startValue = Mathf.Clamp(defaultAmount, minValue, maxValue);
    
            amountSlider.wholeNumbers = true;
            amountSlider.minValue = minValue;
            amountSlider.maxValue = maxValue;
            amountSlider.SetValueWithoutNotify(startValue);
            CurrentAmount = startValue;
            RefreshNumsText();
    
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }
    
        /// <summary>
        /// 隐藏并清空回调
        /// </summary>
        public void Hide()
        {
            confirmHandler = null;
            cancelHandler = null;
            gameObject.SetActive(false);
        }
    
        /// <summary>
        /// 确认当前数量
        /// </summary>
        private void Confirm()
        {
            var handler = confirmHandler;
            int amount = CurrentAmount;
            Hide();
            handler?.Invoke(amount);
        }
    
        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            var handler = cancelHandler;
            Hide();
            handler?.Invoke();
        }
    
        /// <summary>
        /// 滑条变化刷新数量
        /// </summary>
        private void OnSliderChanged(float value)
        {
            CurrentAmount = Mathf.RoundToInt(value);
            RefreshNumsText();
        }
    
        /// <summary>
        /// 刷新数量 TMP
        /// </summary>
        private void RefreshNumsText()
        {
            showNumsTmp.text = CurrentAmount.ToString();
        }
    
        /// <summary>
        /// 绑定 Ensure Cancel CloseRect
        /// </summary>
        private void BindButtonEvents()
        {
            amountSlider.onValueChanged.AddListener(OnSliderChanged);
            ensureButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Cancel);
            closeButton.onClick.AddListener(Cancel);
        }
    }
    
}