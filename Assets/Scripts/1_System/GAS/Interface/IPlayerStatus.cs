using cfg.item;
using DBGameSystem;

namespace GAS.StateSystem
{
    /// <summary>
    /// 生存数值接口 用于沟通玩法层与 GAS 数值 方法实现在 StatController 类里
    /// </summary>
    public interface IPlayerStatus : IGameService
    {
        /// <summary>
        /// 获取属性当前值 即时取Current 被动取Final
        /// </summary>
        float GetCurrentValue(string statName);

        /// <summary>
        /// 获取属性最大值
        /// </summary>
        float GetMaxValue(string statName);

        /// <summary>
        /// 对属性做瞬时变化
        /// </summary>
        /// <param name="statName">属性名</param>
        /// <param name="delta">变化量</param>
        /// <param name="showAnimation">是否让 UI 播放缓动</param>
        void ChangeValue(string statName, float delta, bool showAnimation = false);

        /// <summary>
        /// 设置属性每秒自动变化量 正恢复负消耗 0停止
        /// </summary>
        void SetTickPerSecond(string statName, float perSecond);

        /// <summary>
        /// 按食物表写入饱食水分 San
        /// </summary>
        void ApplyFoodOrWaterEffects(FoodOrWater foodTable);

        /// <summary>
        /// 按药品表写入血量 San
        /// </summary>
        void ApplyMedicineEffects(Medicine medicineTable);
    }

    /// <summary>
    /// 玩家属性 ID 集中定义
    /// GE 资产仍使用同名字符串保持编辑器配置兼容
    /// </summary>
    public static class PlayerStatIds
    {
        /// <summary> 生命 </summary>
        public const string Health = "Health";

        /// <summary> 体力 </summary>
        public const string Power = "Power";

        /// <summary> 饥饿 </summary>
        public const string Food = "Food";

        /// <summary> 口渴 </summary>
        public const string Water = "Water";

        /// <summary> 理智 </summary>
        public const string San = "San";

        /// <summary> 精力 </summary>
        public const string Energy = "Energy";

        /// <summary> 疼痛 </summary>
        public const string Pain = "Pain";

        /// <summary> 攻击力 </summary>
        public const string Attack = "Attack";

        /// <summary> 防御力 </summary>
        public const string Defence = "Defence";

        /// <summary>
        /// 移动速度倍率
        /// </summary>
        public const string MoveSpeed = "MoveSpeed";

        /// <summary>
        /// 跳跃速度倍率
        /// </summary>
        public const string JumpSpeed = "JumpSpeed";

    }
}
