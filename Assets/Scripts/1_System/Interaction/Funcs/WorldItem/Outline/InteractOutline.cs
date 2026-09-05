using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Interaction
{
/// <summary>
/// 交互物整体着色与边缘发光高亮
/// </summary>
[DisallowMultipleComponent]
public class InteractOutline : MonoBehaviour, IDisableWhenHeld
{
    private const string OutlineShaderName = "DownBreak/Interact/InvertedHullOutline";

    /// <summary> 高亮颜色 </summary>
    [SerializeField]
    [ColorUsage(true, true)]
    private Color outlineColor = new Color(1f, 1f, 1f, 30f / 255f);

    /// <summary> 边缘发光范围 </summary>
    [SerializeField]
    [Range(0.5f, 8f)]
    private float outlineWidth = 1f;

    /// <summary> HDR 发光强度 </summary>
    [SerializeField]
    [Range(0.5f, 3f)]
    private float glowIntensity = 1.5f;

    /// <summary> 呼吸速度 </summary>
    [SerializeField]
    [Range(0f, 8f)]
    private float pulseSpeed = 2f;

    /// <summary> 呼吸幅度 </summary>
    [SerializeField]
    [Range(0f, 0.2f)]
    private float pulseAmount = 0.2f;

    /// <summary> 是否包含子物体 </summary>
    [SerializeField]
    private bool includeChildren = true;

    /// <summary> 描边 Renderer 列表 </summary>
    private Renderer[] outlineRendererList;

    /// <summary> 材质属性块 </summary>
    private MaterialPropertyBlock propertyBlock;

    /// <summary> 是否显示中 </summary>
    private bool isShowing;

    /// <summary> 是否已持有共享材质 </summary>
    private bool hasMaterialReference;

    /// <summary> 全部交互物共享的描边材质 </summary>
    private static Material sharedOutlineMaterial;

    /// <summary> 共享材质使用数量 </summary>
    private static int sharedMaterialUserCount;

    private void Reset()
    {
        outlineColor = new Color(1f, 1f, 1f, 30f / 255f);
        outlineWidth = 1f;
        glowIntensity = 1.5f;
        pulseSpeed = 2f;
        pulseAmount = 0.2f;
        includeChildren = true;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        ApplyMaterialParams();
    }

    private void Awake()
    {
        // 迁移旧版世界空间宽度数据
        if (outlineWidth < 0.5f)
            outlineWidth = 1f;

        AcquireSharedMaterial();
        BuildOutlineRenderers();
        ApplyMaterialParams();
        SetOutlineEnabled(false);
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        DestroyOutlineRenderers();
        ReleaseSharedMaterial();
    }

    #region 公开控制

    /// <summary>
    /// 显示外圈高亮
    /// </summary>
    public void Show()
    {
        if (isShowing)
            return;

        SetOutlineEnabled(true);
        isShowing = true;
    }

    /// <summary>
    /// 隐藏外圈高亮
    /// </summary>
    public void Hide()
    {
        if (!isShowing)
            return;

        SetOutlineEnabled(false);
        isShowing = false;
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        ApplyMaterialParams();
    }

    /// <summary>
    /// 套用标准描边参数
    /// </summary>
    public void ApplyStandardStyle()
    {
        outlineWidth = 1f;
        glowIntensity = 1.5f;
        pulseSpeed = 2f;
        pulseAmount = 0.2f;
        ApplyMaterialParams();
    }

    /// <summary>
    /// 设置边缘发光范围
    /// </summary>
    public void SetOutlineWidth(float width)
    {
        outlineWidth = width;
        ApplyMaterialParams();
    }

    #endregion

    #region 材质

    /// <summary>
    /// 获取共享描边材质
    /// </summary>
    private void AcquireSharedMaterial()
    {
        if (sharedOutlineMaterial == null)
        {
            Shader outlineShader = Shader.Find(OutlineShaderName);
            sharedOutlineMaterial = new Material(outlineShader);
            sharedOutlineMaterial.name = "InteractOutlineShared";
            sharedOutlineMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        sharedMaterialUserCount++;
        hasMaterialReference = true;
    }

    /// <summary>
    /// 释放共享描边材质
    /// </summary>
    private void ReleaseSharedMaterial()
    {
        if (!hasMaterialReference)
            return;

        hasMaterialReference = false;
        sharedMaterialUserCount--;
        if (sharedMaterialUserCount > 0)
            return;

        Destroy(sharedOutlineMaterial);
        sharedOutlineMaterial = null;
        sharedMaterialUserCount = 0;
    }

    /// <summary>
    /// 同步当前物品的材质参数
    /// </summary>
    private void ApplyMaterialParams()
    {
        if (outlineRendererList == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.Clear();
        propertyBlock.SetColor("_OutlineColor", outlineColor);
        propertyBlock.SetFloat("_OutlineWidth", outlineWidth);
        propertyBlock.SetFloat("_GlowIntensity", glowIntensity);
        propertyBlock.SetFloat("_PulseSpeed", pulseSpeed);
        propertyBlock.SetFloat("_PulseAmount", pulseAmount);

        int count = outlineRendererList.Length;
        for (int i = 0; i < count; i++)
            outlineRendererList[i].SetPropertyBlock(propertyBlock);
    }

    #endregion

    #region 描边网格

    /// <summary>
    /// 预建描边网格
    /// </summary>
    private void BuildOutlineRenderers()
    {
        List<Renderer> outlineList = new List<Renderer>(4);
        MeshRenderer[] meshRendererList = includeChildren
            ? GetComponentsInChildren<MeshRenderer>(true)
            : GetComponents<MeshRenderer>();

        int meshCount = meshRendererList.Length;
        for (int i = 0; i < meshCount; i++)
        {
            MeshRenderer sourceRenderer = meshRendererList[i];
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                continue;

            outlineList.Add(CreateMeshOutline(sourceRenderer, sourceFilter));
        }

        SkinnedMeshRenderer[] skinnedRendererList = includeChildren
            ? GetComponentsInChildren<SkinnedMeshRenderer>(true)
            : GetComponents<SkinnedMeshRenderer>();

        int skinnedCount = skinnedRendererList.Length;
        for (int i = 0; i < skinnedCount; i++)
        {
            SkinnedMeshRenderer sourceRenderer = skinnedRendererList[i];
            if (sourceRenderer.sharedMesh == null)
                continue;

            outlineList.Add(CreateSkinnedOutline(sourceRenderer));
        }

        outlineRendererList = outlineList.ToArray();
    }

    /// <summary>
    /// 创建静态网格描边
    /// </summary>
    private Renderer CreateMeshOutline(MeshRenderer sourceRenderer, MeshFilter sourceFilter)
    {
        GameObject outlineObject = CreateOutlineObject(sourceRenderer);
        MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
        outlineFilter.sharedMesh = sourceFilter.sharedMesh;
        MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
        ConfigureOutlineRenderer(sourceRenderer, outlineRenderer);
        return outlineRenderer;
    }

    /// <summary>
    /// 创建蒙皮网格描边
    /// </summary>
    private Renderer CreateSkinnedOutline(SkinnedMeshRenderer sourceRenderer)
    {
        GameObject outlineObject = CreateOutlineObject(sourceRenderer);
        SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
        outlineRenderer.sharedMesh = sourceRenderer.sharedMesh;
        outlineRenderer.bones = sourceRenderer.bones;
        outlineRenderer.rootBone = sourceRenderer.rootBone;
        outlineRenderer.localBounds = sourceRenderer.localBounds;
        outlineRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;
        ConfigureOutlineRenderer(sourceRenderer, outlineRenderer);
        return outlineRenderer;
    }

    /// <summary>
    /// 创建描边节点
    /// </summary>
    private GameObject CreateOutlineObject(Renderer sourceRenderer)
    {
        GameObject outlineObject = new GameObject(sourceRenderer.name + "_InteractOutline");
        outlineObject.layer = sourceRenderer.gameObject.layer;
        outlineObject.hideFlags = HideFlags.DontSave;
        Transform outlineTransform = outlineObject.transform;
        outlineTransform.SetParent(sourceRenderer.transform, false);
        return outlineObject;
    }

    /// <summary>
    /// 配置描边 Renderer
    /// </summary>
    private void ConfigureOutlineRenderer(Renderer sourceRenderer, Renderer outlineRenderer)
    {
        int materialCount = Mathf.Max(1, sourceRenderer.sharedMaterials.Length);
        Material[] outlineMaterialList = new Material[materialCount];
        for (int i = 0; i < materialCount; i++)
            outlineMaterialList[i] = sharedOutlineMaterial;

        outlineRenderer.sharedMaterials = outlineMaterialList;
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
        outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        outlineRenderer.allowOcclusionWhenDynamic = true;
        outlineRenderer.enabled = false;
    }

    /// <summary>
    /// 开关描边
    /// </summary>
    private void SetOutlineEnabled(bool isEnabled)
    {
        if (outlineRendererList == null)
            return;

        int count = outlineRendererList.Length;
        for (int i = 0; i < count; i++)
            outlineRendererList[i].enabled = isEnabled;
    }

    /// <summary>
    /// 销毁描边节点
    /// </summary>
    private void DestroyOutlineRenderers()
    {
        if (outlineRendererList == null)
            return;

        int count = outlineRendererList.Length;
        for (int i = 0; i < count; i++)
        {
            if (outlineRendererList[i] != null)
                Destroy(outlineRendererList[i].gameObject);
        }

        outlineRendererList = null;
    }

    #endregion
}
}
