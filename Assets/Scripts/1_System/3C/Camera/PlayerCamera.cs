using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class PlayerCamera : MonoBehaviour
{
    /// <summary> FP Cinemachine 相机 </summary>
    private CinemachineCamera playerfpCamera;

    /// <summary> 相机冲击源 </summary>
    private CinemachineImpulseSource impulseSource;

    [SerializeField]
    /// <summary> 轻攻击震动力度 </summary>
    private float lightAttackShakeForce = 1f;

    [SerializeField]
    /// <summary> 重攻击震动力度 </summary>
    private float heavyAttackShakeForce = 1.5f;

    public CinemachineCamera PlayerFpCamera => playerfpCamera;

    private void Awake()
    {
        InitComponents();
    }

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitComponents()
    {
        playerfpCamera = GetComponent<CinemachineCamera>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null)
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
    }

    /// <summary>
    /// 播放攻击相机震动
    /// </summary>
    public void ShakeForAttack(bool isHeavyAttack)
    {
        float force = isHeavyAttack ? heavyAttackShakeForce : lightAttackShakeForce;
        impulseSource.GenerateImpulseWithForce(force);
    }

    /// <summary>
    /// 按速度插值到目标 FOV
    /// </summary>
    public void ChangeFov(float targetFov, float deltaTime, float lerpSpeed)
    {
        if (playerfpCamera == null)
            return;

        var lens = playerfpCamera.Lens;
        float t = 1f - Mathf.Exp(-lerpSpeed * deltaTime);
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, t);
        playerfpCamera.Lens = lens;
    }
}
