using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 背包 TP 预览相机接口 由 3C PlayerTPCamera 实现 BagPanel 经 Hub 调用
    /// </summary>
    public interface IPlayerTpPreview
    {
        /// <summary> 当前绑定的 RT </summary>
        RenderTexture BoundTexture { get; }

        /// <summary>
        /// 绑定输出 RT
        /// </summary>
        void BindTargetTexture(RenderTexture targetTexture);

        /// <summary>
        /// 开关预览渲染
        /// </summary>
        void OpenTPCamera(bool isOpen);

        /// <summary>
        /// 开始预览偏航会话
        /// </summary>
        void BeginPreviewYaw();

        /// <summary>
        /// 相机绕角色枢轴 Y 环绕 不改角色模型
        /// </summary>
        void SetPreviewYaw(float yawDegrees);

        /// <summary>
        /// 结束偏航会话并还原预览相机
        /// </summary>
        void EndPreviewYaw();
    }

    /// <summary>
    /// TP 预览相机注册点 Outer 契约
    /// </summary>
    public static class PlayerTpPreviewHub
    {
        /// <summary> 当前场景中的预览相机 </summary>
        public static IPlayerTpPreview Current { get; set; }
    }
}
