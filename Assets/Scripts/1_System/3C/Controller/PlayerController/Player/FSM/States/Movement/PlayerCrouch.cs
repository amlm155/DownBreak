namespace PlayerControllerSpace
{
    /// <summary>
    /// 下蹲状态 含待机与移动
    /// </summary>
    public class PlayerCrouch : PlayerStateBase
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

            // 起身成功后离开下蹲态
            if (!Owner.IsCrouching)
            {
                if (Owner.IsMoving)
                    ChangeState<PlayerMove>();
                else
                    ChangeState<PlayerIdle>();
            }
        }
    }
}
