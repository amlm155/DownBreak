using MotionCharacterController;
using UnityEngine;

namespace PlayerControllerSpace
{
    public partial class PlayerController : IMcc
    {
        #region 模拟周期

        public void BeforeCharacterUpdate(float deltaTime)
        {
            // 模拟步开始前切入下蹲 保证本帧碰撞用矮胶囊
            if (CrouchHeld && !isCrouching)
                Crouch();
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // 模拟步结束后再尝试起身 用本帧最终位姿做重叠检测
            if (isCrouching && !CrouchHeld)
                Stand();
        }

        public void PostGroundingUpdate(float deltaTime)
        {
        }

        #endregion

        #region 输入

        public void InputVectorUpdate(ref Vector3 inputDirection, ref bool jumpRequested)
        {
            var input = inputManager;
            if (input == null)
            {
                inputDirection = Vector3.zero;
                return;
            }

            Vector2 raw = input.MoveInput;
            // 用身体前方/右方 已由鼠标 yaw 转过
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, PlayerUp).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, PlayerUp).normalized;
            // 将2维输入转为3维
            inputDirection = (forward * raw.y + right * raw.x);
            if (inputDirection.sqrMagnitude > 1f)
                inputDirection.Normalize();

            if (input.IsJumpPressed)
                jumpRequested = true;
        }

        #endregion

        #region 速度与旋转

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 如果有跳跃请求 则应用跳跃冲力
            if (motionController.JumpRequested && motionController.GroundingStatus.IsStableOnGround)
            {
                ApplyJumpImpulse(ref currentVelocity);
                return;
            }

            // 如果是未稳定接地 则添加重力
            if (!motionController.GroundingStatus.IsStableOnGround)
            {
                ApplyGravity(ref currentVelocity, deltaTime);
                return;
            }

            // 如果是稳定接地 则应用地面移动
            ApplyGroundMove(ref currentVelocity, deltaTime);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            ApplyRotation(ref currentRotation);
        }

        #endregion

        #region 碰撞回调

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        #endregion
    }
}
