namespace PlayerControllerSpace
{
    /// <summary>
    /// 跳跃上升状态
    /// </summary>
    public class PlayerJumpUp : PlayerStateBase
    {
        /// <summary>
        /// 进入状态
        /// </summary>
        public override void OnEnter()
        {
            AmController.CrossFadeClip(PlayerAmHashMap.JumpUp, 0.1f);
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
            if (Owner.IsStableOnGround && Owner.VerticalSpeed <= 0f)
            {
                ChangeToGroundState();
                return;
            }

            // 过顶点开始下落
            if (!Owner.IsStableOnGround && Owner.VerticalSpeed <= 0f)
                ChangeState<PlayerJumpFall>();
        }

        /// <summary>
        /// 落地切回地面态
        /// </summary>
        private void ChangeToGroundState()
        {
            if (Owner.IsCrouching)
                ChangeState<PlayerCrouch>();
            else if (Owner.IsMoving)
                ChangeState<PlayerMove>();
            else
                ChangeState<PlayerIdle>();
        }
    }
}
