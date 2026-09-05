using System;
using MotionCharacterController;
using UnityEngine;

namespace PlayerControllerSpace.FpMotion
{
    /// <summary>
    /// HandsRoot 程序晃动
    /// 视角 + 移动 + 走路起伏 直接算偏移再平滑跟上 无弹簧
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class FpHandsMotion : MonoBehaviour
    {
        /// <summary> 基准本地坐标 </summary>
        private Vector3 baseLocalPosition;

        /// <summary> 基准本地欧拉 </summary>
        private Vector3 baseLocalEuler;

        /// <summary> 当前平滑后的位置偏移 </summary>
        private Vector3 currentPosOffset;

        /// <summary> 当前平滑后的旋转偏移 </summary>
        private Vector3 currentRotOffset;

        /// <summary> Bob 相位 </summary>
        private float bobPhase;

        /// <summary> 上一帧 Bob 正弦 </summary>
        private float prevBobSin;

        /// <summary> 下一步是否左脚 </summary>
        private bool nextFootIsLeft = true;

        /// <summary> 是否已缓存基准 </summary>
        private bool hasBasePose;

        /// <summary> 当前 Bob 相位 </summary>
        public float BobPhase => bobPhase;

        /// <summary>
        /// 落脚脉冲
        /// </summary>
        public event Action<FpFootSide, float> OnFootPlanted;

        private void Awake()
        {
            CacheBasePose();
        }

        private void OnEnable()
        {
            if (!hasBasePose)
                CacheBasePose();
            currentPosOffset = Vector3.zero;
            currentRotOffset = Vector3.zero;
            ResetBobClock();
        }

        /// <summary>
        /// 由 PlayerController 在视角之后调用
        /// </summary>
        public void Tick(
            Vector2 lookInput,
            MotionCC motion,
            bool isCrouching,
            float deltaTime,
            Vector3 breathPosOffset,
            Vector3 breathEulerOffset)
        {
            if (!isActiveAndEnabled)
                return;
            if (!hasBasePose)
                CacheBasePose();
            if (PlayerConfig.Instance == null)
                return;

            Vector3 posTarget = breathPosOffset;
            Vector3 rotTarget = breathEulerOffset;

            float planarSpeed = 0f;
            if (motion != null)
            {
                Vector3 planar = Vector3.ProjectOnPlane(motion.Velocity, motion.CharacterUp);
                planarSpeed = planar.magnitude;
            }

            // 视角甩手
            var lookSway = PlayerConfig.Instance.FpLookSway;
            if (lookSway.Enabled)
            {
                Vector2 look = Vector2.ClampMagnitude(lookInput, lookSway.InputLimit);
                posTarget += new Vector3(
                    look.y * lookSway.PositionStrength.x,
                    look.x * lookSway.PositionStrength.y,
                    0f);
                rotTarget += new Vector3(
                    look.x * lookSway.RotationStrength.x,
                    look.y * -lookSway.RotationStrength.y,
                    look.y * -lookSway.RotationStrength.z);
            }

            // 移动晃动 速度越大越猛
            var moveSway = PlayerConfig.Instance.FpMoveSway;
            if (moveSway.Enabled && motion != null)
            {
                Vector3 localVel = transform.root.InverseTransformDirection(motion.Velocity);
                localVel.y = 0f;
                float limited = Mathf.Min(localVel.magnitude, moveSway.InputLimit);
                Vector3 dir = localVel.sqrMagnitude > 0.0001f ? localVel.normalized : Vector3.zero;
                Vector3 move = dir * limited;
                posTarget += new Vector3(
                    move.x * moveSway.PositionStrength.x,
                    -Mathf.Abs(move.x * moveSway.PositionStrength.y),
                    -move.z * moveSway.PositionStrength.z);
                rotTarget += new Vector3(
                    move.z * moveSway.RotationStrength.x,
                    -move.x * moveSway.RotationStrength.y,
                    move.x * moveSway.RotationStrength.z);
            }

            // 走路起伏 频率和幅度都跟速度比
            var bob = PlayerConfig.Instance.FpBob;
            if (bob.Enabled && motion != null)
            {
                bool bobActive = motion.GroundingStatus.IsStableOnGround && planarSpeed >= bob.SpeedThreshold;
                if (bobActive)
                {
                    float weight = isCrouching ? bob.CrouchWeight : 1f;
                    float speedFactor = planarSpeed / Mathf.Max(0.01f, bob.ReferenceSpeed);
                    bobPhase += deltaTime * bob.Frequency * Mathf.PI * 2f * speedFactor;
                    float s = Mathf.Sin(bobPhase);
                    float c = Mathf.Cos(bobPhase * 0.5f);
                    float amp = weight * speedFactor;
                    posTarget += new Vector3(
                        c * bob.PositionAmplitude.x,
                        Mathf.Abs(s) * bob.PositionAmplitude.y,
                        s * bob.PositionAmplitude.z) * amp;
                    rotTarget += new Vector3(
                        s * bob.RotationAmplitude.x,
                        c * bob.RotationAmplitude.y,
                        -c * bob.RotationAmplitude.z) * amp;

                    if (bob.EmitFootstepEvents)
                        TryEmitFootPlant(s, planarSpeed);
                    prevBobSin = s;
                }
                else
                {
                    prevBobSin = 0f;
                }
            }

            // 单参数平滑跟上 无弹簧
            float follow = PlayerConfig.Instance.FpFollowSpeed;
            float t = 1f - Mathf.Exp(-follow * deltaTime);
            currentPosOffset = Vector3.Lerp(currentPosOffset, posTarget, t);
            currentRotOffset = Vector3.Lerp(currentRotOffset, rotTarget, t);

            transform.localPosition = baseLocalPosition + currentPosOffset;
            transform.localRotation = Quaternion.Euler(baseLocalEuler + currentRotOffset);
        }

        /// <summary>
        /// 重记持枪基准
        /// </summary>
        public void RecacheBasePose()
        {
            CacheBasePose();
        }

        /// <summary>
        /// 重置步态时钟
        /// </summary>
        public void ResetBobClock()
        {
            bobPhase = 0f;
            prevBobSin = 0f;
            nextFootIsLeft = true;
        }

        /// <summary>
        /// sin 过零发射落脚
        /// </summary>
        private void TryEmitFootPlant(float bobSin, float planarSpeed)
        {
            bool crossed =
                (prevBobSin >= 0f && bobSin < 0f) ||
                (prevBobSin < 0f && bobSin >= 0f);
            if (!crossed)
                return;
            if (Mathf.Abs(prevBobSin) < 0.0001f && Mathf.Abs(bobSin) < 0.05f)
                return;

            var eFootSide = nextFootIsLeft ? FpFootSide.Left : FpFootSide.Right;
            nextFootIsLeft = !nextFootIsLeft;
            OnFootPlanted?.Invoke(eFootSide, planarSpeed);
        }

        /// <summary>
        /// 缓存基准
        /// </summary>
        private void CacheBasePose()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalEuler = transform.localEulerAngles;
            hasBasePose = true;
        }
    }
}
