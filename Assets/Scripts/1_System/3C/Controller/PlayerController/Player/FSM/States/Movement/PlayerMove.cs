namespace PlayerControllerSpace
{
    /// <summary>
    /// 站立移动状态
    /// </summary>
    public class PlayerMove : PlayerStateBase
    {
        /// <summary>
        /// 进入状态
        /// </summary>
        public override void OnEnter()
        {
        }

        /// <summary>
        /// 退出状态
        /// </summary>
        public override void OnExit()
        {
            
        }

        /// <summary>
        /// 状态更新
        /// </summary>
        public override void OnUpdate()
        {
            if (Owner.IsJumpRequested)
            {
                ChangeState<PlayerJumpUp>();
                return;
            }

            if (!Owner.IsStableOnGround)
            {
                if (Owner.VerticalSpeed > 0f)
                    ChangeState<PlayerJumpUp>();
                else
                    ChangeState<PlayerJumpFall>();
                return;
            }

            if (Owner.IsCrouching)
            {
                ChangeState<PlayerCrouch>();
                return;
            }

            if (!Owner.IsMoving)
                ChangeState<PlayerIdle>();
        }
    }
}
