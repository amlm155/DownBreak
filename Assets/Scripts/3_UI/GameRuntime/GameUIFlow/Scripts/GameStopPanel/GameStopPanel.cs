/// <summary>
/// GameStopPanel Logic层 - 用户编写
/// </summary>

using MieMieUIFrameWork;
using MmUIFrameWork.Core;
using UnityEngine;
using UnityEngine.UI;
namespace MieMieUIFrameWork.Runtime
{
    
    internal class GameStopPanel : UIWindowBase
    {
        internal GameStopPanelGen View { get; private set; }
    
        protected override void OnAwake()
        {
            base.OnAwake();
            View = UIContent.GetComponent<GameStopPanelGen>();
            BindButtonEvents();
        }
    
        protected override void OnShow()
        {
            Time.timeScale = 0;
            CursorController.Unlock();  
            base.OnShow();
        }
    
        protected override void OnHide()
        {
            Time.timeScale = 1;
            CursorController.Lock();
            base.OnHide();
        }
    
        protected override void OnDestroy()
        {
            Time.timeScale = 1;
            CursorController.Lock();
            base.OnDestroy();
        }
    
        private void BindButtonEvents(){
            View.ContinueButton.onClick.AddListener(OnContinueButtonClick);
            View.SettingButton.onClick.AddListener(OnSettingButtonClick);
            View.SaveGameButton.onClick.AddListener(OnSaveGameButtonClick);
            View.LoadGameButton.onClick.AddListener(OnLoadGameButtonClick);
            View.ExitToMenuButton.onClick.AddListener(OnExitToMenuButtonClick);
            View.ExitToDeskTopButton.onClick.AddListener(OnExitToDeskTopButtonClick);
        }
        
        private void OnContinueButtonClick(){
            // 关闭此窗口
            UIHub.Instance.CloseWindow<GameStopPanel>();
        }
        private void OnSettingButtonClick(){
            // 打开设置窗口
            UIHub.Instance.ShowWindow<SettingPanel>();
        }
        private void OnSaveGameButtonClick(){
            // 保存游戏
            // GameManager.Instance.SaveGame();
        }
        private void OnLoadGameButtonClick(){
            // 加载游戏
            // GameManager.Instance.LoadGame();
        }
        private void OnExitToMenuButtonClick(){
            // 退出到菜单
            UIHub.Instance.ShowWindow<GameStartPanel>(() => {
                UIHub.Instance.CloseWindow<GameStopPanel>();
            });
        }
        private void OnExitToDeskTopButtonClick(){
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    
    }
    
}