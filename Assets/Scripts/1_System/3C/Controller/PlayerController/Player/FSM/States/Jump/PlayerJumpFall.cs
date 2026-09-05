namespace PlayerControllerSpace
{
    /// <summary>
    /// 跳跃下落状态
    /// </summary>
    public class PlayerJumpFall : PlayerStateBase
    {
        /// <summary>
        /// 进入状态
        /// </summary>  
        public override void OnEnter()
        {
            AmController.CrossFadeClip(PlayerAmHashMap.JumpFall, 0.1f);
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
            if (!Owner.IsStableOnGround)
                return;

            if (Owner.IsCrouching)
                ChangeState<PlayerCrouch>();
            else if (Owner.IsMoving)
                ChangeState<PlayerMove>();
            else
                ChangeState<PlayerIdle>();
        }
    }
}
