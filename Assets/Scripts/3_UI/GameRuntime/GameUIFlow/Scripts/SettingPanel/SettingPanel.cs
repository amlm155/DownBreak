/// <summary>
/// SettingPanel Logic层 - 用户编写
/// </summary>

using MieMieUIFrameWork;
using MmUIFrameWork.Core;
using UnityEngine;
using UnityEngine.UI;
namespace MieMieUIFrameWork.Runtime
{
    
    internal class SettingPanel : UIWindowBase
    {
        internal SettingPanelGen View { get; private set; }
    
        protected override void OnAwake()
        {
            base.OnAwake();
            View = UIContent.GetComponent<SettingPanelGen>();
        }
    
        protected override void OnShow()
        {
            base.OnShow();
            CursorController.Unlock();
        }
    
        protected override void OnHide()
        {
            base.OnHide();
            TryRestoreGameCursor();
        }
    
        protected override void OnDestroy()
        {
            base.OnDestroy();
            TryRestoreGameCursor();
        }
    
        /// <summary>
        /// 暂停菜单还开着就保持解锁 否则锁回光标
        /// </summary>
        private void TryRestoreGameCursor()
        {
            if (UIHub.Instance.HasWindow<GameStopPanel>())
                return;
    
            CursorController.Lock();
        }
    
    }
    
}