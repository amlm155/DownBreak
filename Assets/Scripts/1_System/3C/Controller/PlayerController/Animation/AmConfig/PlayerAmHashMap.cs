using UnityEngine;

namespace PlayerControllerSpace
{
    /// <summary>
    /// 该类用于存储玩家动画 Hash 
    /// 其中FP和TP动画 Hash 字段名与状态名 Clip 名一致
    /// 这样方便FP 和 TP同时播放同一段动画
    /// </summary>
    public static class PlayerAmHashMap
    {

        #region FP

        #region 标准通用动作模组
        public static readonly int 取出 = Animator.StringToHash("取出");

        public static readonly int 待机 = Animator.StringToHash("待机");

        public static readonly int 拾取 = Animator.StringToHash("拾取");

        public static readonly int 收起 = Animator.StringToHash("收起");

        public static readonly int 格挡 = Animator.StringToHash("格挡");

        public static readonly int 检视 = Animator.StringToHash("检视");

        public static readonly int 轻攻击 = Animator.StringToHash("轻攻击");

        public static readonly int 重攻击 = Animator.StringToHash("重攻击");

        #endregion

        #endregion

        #region 吃饭喝水
        /// <summary> 食物本体状态机 开罐等 </summary>
        public static readonly int FoodAm = Animator.StringToHash("FoodAm");
        /// <summary> 餐具状态机 使用 </summary>
        public static readonly int Utensil = Animator.StringToHash("Utensil");
        #endregion

        #region 药品
        /// <summary> 医疗品本体状态机 注射等 </summary>
        public static readonly int MedicineAm = Animator.StringToHash("MedicineAm");
        #endregion


        #region TP
        /// <summary> 身体起跳 暂留 </summary>
        public static readonly int JumpUp = Animator.StringToHash("JumpUp");
        /// <summary> 身体下落 暂留 </summary>
        public static readonly int JumpFall = Animator.StringToHash("JumpFall");
        #endregion
    }
}
