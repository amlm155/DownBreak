using UnityEngine;

namespace PlayerControllerSpace
{
    public partial class PlayerAnimationController
    {
        /// <summary>
        /// 切换 TP Controller
        /// </summary>
        public void SetTpController(RuntimeAnimatorController controller)
        {
            if (bodyAnimator == null || controller == null)
                return;
            bodyAnimator.runtimeAnimatorController = controller;
        }
    }
}
