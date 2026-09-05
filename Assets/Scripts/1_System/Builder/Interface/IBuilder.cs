using System;
using DBGameSystem;

namespace Mm_Budier
{
    /// <summary>
    /// 建造系统门面 用于沟通 3C/UI 与建造能力 方法实现在 BuilderSystem 类里
    /// </summary>
    public interface IBuilder : IGameService
    {
        CubeData ActiveCubeData { get; }

        bool IsRayCastStopped { get; }

        /// <summary>
        /// 设置当前要放置的方块
        /// </summary>
        void SetActiveCubeData(CubeData cubeData);

        /// <summary>
        /// 按物品表进入放置预览 预制体来自 Luban place_prefab_path
        /// </summary>
        bool TryEnterPlaceFromItem(int itemTableId);

        /// <summary>
        /// 停止或恢复建造射线与预览
        /// </summary>
        void SetStopRayCast(bool stop);

        /// <summary>
        /// 取消当前放置 藏预览并退出建造射线
        /// </summary>
        void CancelPlace();

        /// <summary>
        /// 注入放置输入
        /// </summary>
        void SetPlaceButtonPressed(Action action = null);

        /// <summary>
        /// 注入破坏输入
        /// </summary>
        void SetBreakButtonPressed(Action action = null);

        /// <summary>
        /// 注入旋转输入
        /// </summary>
        void SetRotateButtonPressed(Action action = null);

        /// <summary>
        /// 挂接外部自定义建造逻辑
        /// </summary>
        void OpenCustomMode(IBuilderCustom builderCustom);
    }
}
