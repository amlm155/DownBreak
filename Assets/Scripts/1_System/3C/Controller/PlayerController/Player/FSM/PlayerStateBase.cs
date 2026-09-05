using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiMieFSM.UpdateFsm;

namespace PlayerControllerSpace
{
    public abstract class PlayerStateBase :StateBase
    {
        protected PlayerController Owner=>
                GetBlackboardValue<PlayerController>(EBlockBoardParme.PlayerController);
        
        protected PlayerAnimationController AmController=>
                GetBlackboardValue<PlayerAnimationController>(EBlockBoardParme.PlayerAnimator);

        protected StateMachine Machine=>Owner?.Machine;

        protected bool ChangeState<T>(bool force = false) where T : PlayerStateBase, new()
        {
            return Machine.ChangeState<T>(force);
        }

    }
}