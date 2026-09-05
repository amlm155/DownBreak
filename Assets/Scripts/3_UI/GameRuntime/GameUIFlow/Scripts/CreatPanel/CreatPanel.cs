/// <summary>
/// CreatPanel Logic层 - 用户编写
/// </summary>

using DBGameSystem;
using DownBreak.CraftingRecipeSystem;
using MmUIFrameWork.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    internal class CreatPanel : UIWindowBase
    {
        internal CreatPanelGen View { get; private set; }

        /// <summary> 制作面板是否处于打开显示 </summary>
        public bool IsOpen => View != null && View.isOpen;

        protected override void OnAwake()
        {
            base.OnAwake();
            View = UIContent.GetComponent<CreatPanelGen>();
            View.MakrPanelMakrPanel.InitComponents();
            View.HandBookPanelHandBookPanel.InitComponents();
            View.MakeButton.onClick.AddListener(OpenMakePanel);
            View.HandBookButton.onClick.AddListener(OpenHandBookPanel);
            View.ShowHasToggleToggle.onValueChanged.AddListener(OnShowHasToggleChanged);
            View.MakrPanelMakrPanel.SetShowUnlockedOnly(View.ShowHasToggleToggle.isOn);
        }

        protected override void OnShow()
        {
            base.OnShow();
            View.isOpen = true;
            UIHub.Instance.HideWindow<PlayerPanel>();
            CursorController.Unlock();
        }

        protected override void OnHide()
        {
            base.OnHide();
            View.isOpen = false;
            GameHub.Get<ICraftingRecipe>().ExitWorkbench();
            CursorController.Lock();
        }

        /// <summary>
        /// 关闭制作并回到 PlayerPanel
        /// </summary>
        public void CloseCreatPanel()
        {
            UIHub.Instance.HideWindow<CreatPanel>();
            UIHub.Instance.ShowWindow<PlayerPanel>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            View.ShowHasToggleToggle.onValueChanged.RemoveListener(OnShowHasToggleChanged);
            View.MakrPanelMakrPanel.Release();
        }

        private void OpenMakePanel()
        {
            View.HandBookPanelHandBookPanel.Hide();
            View.MakrPanelMakrPanel.Show();
        }

        private void OpenHandBookPanel()
        {
            View.MakrPanelMakrPanel.Hide();
            View.HandBookPanelHandBookPanel.Show();
        }

        /// <summary>
        /// 已解锁开关刷制作列表
        /// </summary>
        private void OnShowHasToggleChanged(bool isOn)
        {
            View.MakrPanelMakrPanel.SetShowUnlockedOnly(isOn);
        }

    }
}
