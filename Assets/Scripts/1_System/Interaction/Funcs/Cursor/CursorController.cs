using MieMieFrameWork;
using MieMieFrameWork.M_InputSystem;
using UnityEngine;

/// <summary>
/// 游戏光标静态入口 锁定与解锁统一走这里
/// 解锁时叠加启用 UI Map 指针 锁定时关闭
/// </summary>
public static class CursorController
{
    /// <summary> 当前是否锁定为游戏视角模式 </summary>
    public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

    /// <summary>
    /// 锁定光标 隐藏鼠标 恢复视角控制
    /// </summary>
    public static void Lock()
    {
        bool wasUnlocked = !IsLocked;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (wasUnlocked)
            SetUiPointerOverlay(false);
    }

    /// <summary>
    /// 解锁光标 显示鼠标 供 UI 操作
    /// </summary>
    public static void Unlock()
    {
        bool wasLocked = IsLocked;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (wasLocked)
            SetUiPointerOverlay(true);
    }

    /// <summary>
    /// 叠加开闭 UI Map 指针 不影响 Player 移动
    /// </summary>
    private static void SetUiPointerOverlay(bool enabled)
    {
        if (ModuleHub.Instance == null)
            return;

        var inputManager = ModuleHub.Instance.GetManager<InputManager>();
        if (inputManager == null)
            return;

        if (enabled)
            inputManager.EnableUiPointerOverlay();
        else
            inputManager.DisableUiPointerOverlay();
    }
}
