/// <summary>
/// PlayerPanel Logic层 - 生命周期编排
/// </summary>

using MmUIFrameWork.Core;
namespace MieMieUIFrameWork.Runtime
{
    
    public partial class PlayerPanel : UIWindowBase
    {
        public PlayerPanelGen View { get; private set; }

        #region 生命周期

        protected override void OnAwake()
        {
            base.OnAwake();
            View = UIContent.GetComponent<PlayerPanelGen>();
            BindGEUI();
            SubscribeStatEvents();
            BindCombatFeedbackEvents();
            SubscribeUiFlowEvents();
        }
    
        protected override void OnShow()
        {
            base.OnShow();
        }
    
        protected override void OnHide()
        {
            if (UIHub.Instance.HasWindow<UIItemWheel>())
                UIHub.Instance.HideWindow<UIItemWheel>();
            base.OnHide();
        }
    
        protected override void OnDestroy()
        {
            UnsubscribeUiFlowEvents();
            UnbindCombatFeedbackEvents();
            UnsubscribeStatEvents();
            UnbindGEUIEvents();
            ClearGEInfoList();
            base.OnDestroy();
        }

        #endregion
    }
    
}
