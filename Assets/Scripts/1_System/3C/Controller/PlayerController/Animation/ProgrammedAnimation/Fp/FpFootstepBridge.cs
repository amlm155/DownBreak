using UnityEngine;

namespace PlayerControllerSpace.FpMotion
{
    /// <summary>
    /// 落脚事件桥 挂在与 FpHandsMotion 同物体或父级
    /// 音频系统就绪后在 PlayFootstep 里播 clip 不要自己算间隔
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FpFootstepBridge : MonoBehaviour
    {
        /// <summary> 手部晃动源 </summary>
        [SerializeField]
        private FpHandsMotion handsMotion;

        /// <summary> 调试打印落脚 </summary>
        [SerializeField]
        private bool debugLog;

        private void Awake()
        {
            if (handsMotion == null)
                handsMotion = GetComponent<FpHandsMotion>();
            if (handsMotion == null)
                handsMotion = GetComponentInParent<FpHandsMotion>();
        }

        private void OnEnable()
        {
            if (handsMotion != null)
                handsMotion.OnFootPlanted += HandleFootPlanted;
        }

        private void OnDisable()
        {
            if (handsMotion != null)
                handsMotion.OnFootPlanted -= HandleFootPlanted;
        }

        /// <summary>
        /// 收到落脚脉冲
        /// </summary>
        private void HandleFootPlanted(FpFootSide eFootSide, float planarSpeed)
        {
            if (debugLog)
                Debug.Log($"[FpFootstep] {eFootSide} speed={planarSpeed:F2}", this);

            PlayFootstep(eFootSide, planarSpeed);
        }

        /// <summary>
        /// 音频接入点 后续在此播左右脚与材质变体
        /// </summary>
        private void PlayFootstep(FpFootSide eFootSide, float planarSpeed)
        {
            // 音频系统未就绪 故意留空
        }
    }
}
