using UnityEngine.Assemblies;

namespace PlayerControllerSpace
{
    /// <summary>
    /// 站立待机状态
    /// </summary>
    public class PlayerIdle : PlayerStateBase
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
                // 冲量已消费时用竖直速度区分上升 下落
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

            if (Owner.IsMoving)
                ChangeState<PlayerMove>();
        }
    }
}
