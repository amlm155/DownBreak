using System.Collections.Generic;
using DBWeaponSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace PlayerControllerSpace
{
    /// <summary>
    /// 该类用于存储玩家动态动画资源
    /// 创建此Data 然后加载对应动画模组 给动画控制器使用
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationAssets", menuName = "DBProjectConfig/PlayerController/AnimationAssets")]
    public class AnimationModelSoData : ScriptableObject
    {
        [SerializeField, LabelText("空手")]
        private RuntimeAnimatorController nullHandAnimator;
        [SerializeField, LabelText("小刀")]
        private RuntimeAnimatorController knifeAnimator;
        [SerializeField, LabelText("单手长武器")]
        private RuntimeAnimatorController singleHandAnimator;
        [SerializeField, LabelText("双持大武器类")]
        private RuntimeAnimatorController doubleHandAnimator;
        [SerializeField, LabelText("望远镜")]
        private RuntimeAnimatorController telescopeAnimator;
        [SerializeField, LabelText("盾牌与棍棒")]
        private RuntimeAnimatorController shieldAndStickAnimator;
        [SerializeField, LabelText("防身喷雾")]
        private RuntimeAnimatorController sprayAnimator;
        [SerializeField, LabelText("手电筒加单武器")]
        private RuntimeAnimatorController lanternAnimator;
        [SerializeField, LabelText("食物水分")]
        private RuntimeAnimatorController foodOrWaterAnimator;
        [SerializeField, LabelText("药品")]
        private RuntimeAnimatorController medicineAnimator;

        /// <summary>
        /// 运行时动画控制器字典
        /// </summary>
        private Dictionary<EAnimationModelType, RuntimeAnimatorController> animationControllerDict;

        public Dictionary<EAnimationModelType, RuntimeAnimatorController> AnimationControllerDict
        {
            get
            {
                if (animationControllerDict == null)
                    InitAnimationControllerDict();
                return animationControllerDict;
            }
        }

        /// <summary>
        /// 初始化模组控制器字典
        /// </summary>
        public void InitAnimationControllerDict()
        {
            animationControllerDict = new Dictionary<EAnimationModelType, RuntimeAnimatorController>();
            animationControllerDict[EAnimationModelType.None] = nullHandAnimator;
            animationControllerDict[EAnimationModelType.Knife] = knifeAnimator;
            animationControllerDict[EAnimationModelType.SingleHandWeapon] = singleHandAnimator;
            animationControllerDict[EAnimationModelType.DoubleHandWeapon] = doubleHandAnimator;
            animationControllerDict[EAnimationModelType.Telescope] = telescopeAnimator;
            animationControllerDict[EAnimationModelType.ShieldAndStick] = shieldAndStickAnimator;
            animationControllerDict[EAnimationModelType.Spray] = sprayAnimator;
            animationControllerDict[EAnimationModelType.Lantern] = lanternAnimator;
            animationControllerDict[EAnimationModelType.FoodOrWater] = foodOrWaterAnimator;
            animationControllerDict[EAnimationModelType.Medicine] = medicineAnimator;
        }
    }
}
