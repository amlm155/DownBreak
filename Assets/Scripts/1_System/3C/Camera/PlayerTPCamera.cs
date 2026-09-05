using UnityEngine;
using MmInventory;

/// <summary>
/// 背包 TP 全身预览相机 挂在 Player 下 默认关闭 只渲到 RT
/// 用固定本地 Offset 绕父节点 Y 环绕 不读碰撞 不改角色模型
/// </summary>
[RequireComponent(typeof(Camera))]
public class PlayerTPCamera : MonoBehaviour, IPlayerTpPreview
{
    /// <summary> 预览可见层 通常为 PlayerLocalHidden </summary>
    [SerializeField]
    private LayerMask renderLayer;

    /// <summary> 相对父节点的环绕偏移 开包默认机位 </summary>
    [SerializeField]
    private Vector3 orbitOffset = new Vector3(0f, 1.36f, 1.68f);

    /// <summary> 相对父节点的默认欧拉 与 Offset 配套 </summary>
    [SerializeField]
    private Vector3 orbitLocalEuler = new Vector3(12f, 180f, 0f);

    /// <summary> 预览相机 </summary>
    private Camera renderCamera;

    /// <summary> 当前绑定的 RT </summary>
    private RenderTexture boundTexture;

    /// <summary> 是否处于偏航会话 </summary>
    private bool hasYawSession;

    /// <summary> 会话内当前偏航角 度 </summary>
    private float currentYawDegrees;

    /// <summary> 当前绑定的 RT </summary>
    public RenderTexture BoundTexture => boundTexture;

    private void Awake()
    {
        InitComponents();
        ForceIdle();
    }

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitComponents()
    {
        renderCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if (boundTexture == null && renderCamera.targetTexture == null)
            ForceIdle();

        PlayerTpPreviewHub.Current = this;
    }

    private void OnDisable()
    {
        if (PlayerTpPreviewHub.Current == (IPlayerTpPreview)this)
            PlayerTpPreviewHub.Current = null;

        EndPreviewYaw();
        OpenTPCamera(false);
    }

    private void LateUpdate()
    {
        if (!hasYawSession)
            return;

        ApplyOrbitLocal(currentYawDegrees);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (renderCamera == null)
            renderCamera = GetComponent<Camera>();
        if (!Application.isPlaying)
            ForceIdle();
    }

    private void Reset()
    {
        renderCamera = GetComponent<Camera>();
        ForceIdle();
    }
#endif

    /// <summary>
    /// 绑定输出 RT 并同步剔除层
    /// </summary>
    public void BindTargetTexture(RenderTexture targetTexture)
    {
        boundTexture = targetTexture;
        renderCamera.targetTexture = targetTexture;
        renderCamera.cullingMask = renderLayer;
    }

    /// <summary>
    /// 开关预览渲染 无 RT 时不允许打开
    /// </summary>
    public void OpenTPCamera(bool isOpen)
    {
        if (isOpen)
        {
            if (boundTexture == null && renderCamera.targetTexture == null)
            {
                ForceIdle();
                return;
            }

            renderCamera.cullingMask = renderLayer;
            renderCamera.enabled = true;
            return;
        }

        renderCamera.enabled = false;
    }

    /// <summary>
    /// 开始预览偏航 复位到固定 Offset
    /// </summary>
    public void BeginPreviewYaw()
    {
        currentYawDegrees = 0f;
        hasYawSession = true;
        ApplyOrbitLocal(0f);
    }

    /// <summary>
    /// 绕父节点世界 Y 等效的本地偏航 只改相机本地变换
    /// </summary>
    public void SetPreviewYaw(float yawDegrees)
    {
        if (!hasYawSession)
            return;

        currentYawDegrees = yawDegrees;
        ApplyOrbitLocal(currentYawDegrees);
    }

    /// <summary>
    /// 结束偏航并回到固定 Offset
    /// </summary>
    public void EndPreviewYaw()
    {
        if (!hasYawSession)
            return;

        currentYawDegrees = 0f;
        hasYawSession = false;
        ApplyOrbitLocal(0f);
    }

    /// <summary>
    /// 用固定 Offset 写本地位姿 与墙体碰撞无关
    /// </summary>
    private void ApplyOrbitLocal(float yawDegrees)
    {
        Quaternion yawRotation = Quaternion.AngleAxis(yawDegrees, Vector3.up);
        transform.localPosition = yawRotation * orbitOffset;
        transform.localRotation = yawRotation * Quaternion.Euler(orbitLocalEuler);
    }

    /// <summary>
    /// 回到不输出屏幕的闲置态
    /// </summary>
    private void ForceIdle()
    {
        if (renderCamera == null)
            return;

        renderCamera.enabled = false;
        if (!Application.isPlaying)
            renderCamera.targetTexture = null;
    }
}
