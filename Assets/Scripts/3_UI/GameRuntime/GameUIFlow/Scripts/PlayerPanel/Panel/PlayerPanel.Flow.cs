/// <summary>
/// PlayerPanel UI 流程 背包 制作 暂停 轮盘
/// </summary>

using System;
using System.Collections.Generic;
using DownBreak.CraftingRecipeSystem;
using MmUIFrameWork.Core;
using MiMieEventBus;
using DBGameSystem;
using MieMieFrameWork;
namespace MieMieUIFrameWork.Runtime
{
    public partial class PlayerPanel
    {
        /// <summary> UI 流程命令订阅令牌集合 </summary>
        private readonly List<IDisposable> uiFlowDisposableList = new List<IDisposable>();

        /// <summary>
        /// 订阅 UI 流程命令事件
        /// </summary>
        private void SubscribeUiFlowEvents()
        {
            uiFlowDisposableList.Add(MmGlobalEventBus.GlobalBus.Subscribe(
                UIFlowEvents.OnOpenBagStarted, ToggleBagPanel));
            uiFlowDisposableList.Add(MmGlobalEventBus.GlobalBus.Subscribe(
                UIFlowEvents.OnOpenSettingStarted, OpenSettingPanel));
            uiFlowDisposableList.Add(MmGlobalEventBus.GlobalBus.Subscribe(
                UIFlowEvents.OnItemWheelStarted, TryOpenItemWheel));
            uiFlowDisposableList.Add(MmGlobalEventBus.GlobalBus.Subscribe(
                UIFlowEvents.OnItemWheelCanceled, CloseItemWheel));
            uiFlowDisposableList.Add(MmGlobalEventBus.GlobalBus.Subscribe(
                UIFlowEvents.OnOpenCraftStarted, ToggleCreatPanel));
            uiFlowDisposableList.Add(MmGlobalEventBus.GlobalBus.Subscribe(
                CraftingRecipeEvents.OnWorkbenchCraftRequested, OpenCreatFromWorkbench));
        }

        /// <summary>
        /// 取消 UI 流程命令订阅
        /// </summary>
        private void UnsubscribeUiFlowEvents()
        {
            for (int i = 0; i < uiFlowDisposableList.Count; i++)
                uiFlowDisposableList[i].Dispose();
            uiFlowDisposableList.Clear();
        }

        /// <summary>
        /// TAB 开关背包 背包开着时优先关闭
        /// </summary>
        private void ToggleBagPanel()
        {
            CloseItemWheel();
            CloseCreatIfOpen();

            var bagPanel = UIHub.Instance.GetWindow<BagPanel>();
            if (bagPanel != null && bagPanel.IsOpen)
            {
                bagPanel.CloseBagPanel();
                return;
            }

            UIHub.Instance.ShowWindow<BagPanel>();
        }

        /// <summary>
        /// N 开关制作 开着时优先关闭 同时关掉武器轮盘
        /// </summary>
        private void ToggleCreatPanel()
        {
            CloseItemWheel();
            CloseBagIfOpen();

            var creatPanel = UIHub.Instance.GetWindow<CreatPanel>();
            if (creatPanel != null && creatPanel.IsOpen)
            {
                creatPanel.CloseCreatPanel();
                return;
            }

            GameHub.Get<ICraftingRecipe>().ExitWorkbench();
            UIHub.Instance.ShowWindow<CreatPanel>();
        }

        /// <summary>
        /// 工作台交互打开制作 保留当前工作台等级
        /// </summary>
        private void OpenCreatFromWorkbench()
        {
            CloseItemWheel();
            CloseBagIfOpen();
            UIHub.Instance.ShowWindow<CreatPanel>();
        }

        /// <summary>
        /// ESC 开关暂停菜单 背包制作开着时优先关对应面板 Setting 开着时优先关 Setting
        /// </summary>
        private void OpenSettingPanel()
        {
            CloseItemWheel();

            var settingPanel = UIHub.Instance.GetWindow<SettingPanel>();
            if (settingPanel != null && settingPanel.UIIsShow)
            {
                UIHub.Instance.CloseWindow<SettingPanel>();
                return;
            }

            var stopPanel = UIHub.Instance.GetWindow<GameStopPanel>();
            if (stopPanel != null && stopPanel.UIIsShow)
            {
                UIHub.Instance.CloseWindow<GameStopPanel>();
                return;
            }

            var bagPanel = UIHub.Instance.GetWindow<BagPanel>();
            if (bagPanel != null && bagPanel.IsOpen)
            {
                bagPanel.CloseBagPanel();
                return;
            }

            var creatPanel = UIHub.Instance.GetWindow<CreatPanel>();
            if (creatPanel != null && creatPanel.IsOpen)
            {
                creatPanel.CloseCreatPanel();
                return;
            }

            UIHub.Instance.ShowWindow<GameStopPanel>();
        }

        /// <summary>
        /// 长按 T 按下 满足条件时打开轮盘
        /// </summary>
        private void TryOpenItemWheel()
        {
            if (!CanHoldItemWheel())
                return;

            UIHub.Instance.ShowWindow<UIItemWheel>();
        }

        /// <summary>
        /// 关闭物品轮盘
        /// </summary>
        private void CloseItemWheel()
        {
            var wheel = UIHub.Instance.GetWindow<UIItemWheel>();
            if (wheel == null || !wheel.UIIsShow)
                return;

            UIHub.Instance.HideWindow<UIItemWheel>();
        }

        /// <summary>
        /// 制作开着则关掉
        /// </summary>
        private void CloseCreatIfOpen()
        {
            var creatPanel = UIHub.Instance.GetWindow<CreatPanel>();
            if (creatPanel == null || !creatPanel.IsOpen)
                return;

            creatPanel.CloseCreatPanel();
        }

        /// <summary>
        /// 背包开着则关掉
        /// </summary>
        private void CloseBagIfOpen()
        {
            var bagPanel = UIHub.Instance.GetWindow<BagPanel>();
            if (bagPanel == null || !bagPanel.IsOpen)
                return;

            bagPanel.CloseBagPanel();
        }

        /// <summary>
        /// PlayerPanel 在场且背包制作设置未开
        /// </summary>
        private bool CanHoldItemWheel()
        {
            var playerPanel = UIHub.Instance.GetWindow<PlayerPanel>();
            if (playerPanel == null || !playerPanel.UIIsShow)
                return false;

            var bagPanel = UIHub.Instance.GetWindow<BagPanel>();
            if (bagPanel != null && bagPanel.IsOpen)
                return false;

            var creatPanel = UIHub.Instance.GetWindow<CreatPanel>();
            if (creatPanel != null && creatPanel.IsOpen)
                return false;

            var stopPanel = UIHub.Instance.GetWindow<GameStopPanel>();
            if (stopPanel != null && stopPanel.UIIsShow)
                return false;

            return true;
        }
    }
}
