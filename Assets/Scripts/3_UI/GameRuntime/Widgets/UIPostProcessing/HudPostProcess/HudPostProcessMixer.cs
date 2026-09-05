using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// HUD 后处理混音器 唯一持有 Volume
    /// 血量与理智只上报贡献 在此按语义叠加后写入
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/MmUI/HudPostProcessMixer")]
    public class HudPostProcessMixer : MonoBehaviour
    {
        /// <summary> Volume 优先级 </summary>
        [FoldoutGroup("引用")]
        [LabelText("优先级")]
        [SerializeField]
        private float volumePriority = 60f;

        /// <summary> 预创建 Profile 资源 运行时克隆使用 </summary>
        [FoldoutGroup("引用")]
        [LabelText("Profile 模板")]
        [SerializeField]
        private VolumeProfile profileTemplate;

        /// <summary> 可选外部 Volume 为空则运行时自建 </summary>
        [FoldoutGroup("引用")]
        [LabelText("Volume 可空")]
        [SerializeField]
        private Volume volume;

        /// <summary> 血量贡献 </summary>
        private HudPostContribution damageContribution = HudPostContribution.Neutral;

        /// <summary> 理智贡献 </summary>
        private HudPostContribution sanContribution = HudPostContribution.Neutral;

        /// <summary> 是否由本组件创建了 VolumeProfile 克隆 </summary>
        private bool ownsProfile;

        /// <summary> 是否由本组件创建了 Volume 节点 </summary>
        private bool ownsVolumeGo;

        /// <summary> 运行时 Profile 克隆 </summary>
        private VolumeProfile runtimeProfile;

        /// <summary> 色彩调整 </summary>
        private ColorAdjustments colorAdjustments;

        /// <summary> 边缘压暗 </summary>
        private Vignette vignette;

        /// <summary> 胶片颗粒 </summary>
        private FilmGrain filmGrain;

        /// <summary> 色散 </summary>
        private ChromaticAberration chromaticAberration;

        /// <summary> 镜头畸变 </summary>
        private LensDistortion lensDistortion;

        /// <summary> 景深模糊 </summary>
        private DepthOfField depthOfField;

        private void Awake()
        {
            EnsureVolume();
            ComposeAndApply();
        }

        private void OnDestroy()
        {
            if (ownsProfile && runtimeProfile != null)
            {
                Destroy(runtimeProfile);
                runtimeProfile = null;
            }

            if (ownsVolumeGo && volume != null)
            {
                Destroy(volume.gameObject);
                volume = null;
            }
        }

        /// <summary>
        /// 上报血量后处理贡献并合成
        /// </summary>
        public void SetDamageContribution(HudPostContribution contribution)
        {
            damageContribution = contribution;
            ComposeAndApply();
        }

        /// <summary>
        /// 上报理智后处理贡献并合成
        /// </summary>
        public void SetSanContribution(HudPostContribution contribution)
        {
            sanContribution = contribution;
            ComposeAndApply();
        }

        /// <summary>
        /// 清除血量贡献
        /// </summary>
        public void ClearDamageContribution()
        {
            SetDamageContribution(HudPostContribution.Neutral);
        }

        /// <summary>
        /// 清除理智贡献
        /// </summary>
        public void ClearSanContribution()
        {
            SetSanContribution(HudPostContribution.Neutral);
        }

        /// <summary>
        /// 合成两路贡献并写入 Volume
        /// </summary>
        private void ComposeAndApply()
        {
            EnsureVolume();
            if (colorAdjustments == null || vignette == null || filmGrain == null
                || chromaticAberration == null || lensDistortion == null || depthOfField == null)
                return;

            var damage = damageContribution;
            var san = sanContribution;

            // 饱和度为负值 更惨取更小 曝光同理
            float saturation = Mathf.Min(damage.saturation, san.saturation);
            float exposure = Mathf.Min(damage.exposure, san.exposure);
            float contrast = Mathf.Max(damage.contrast, san.contrast);
            float hueShift = san.hueShift;
            Color filter = MultiplyFilter(damage.colorFilter, san.colorFilter);

            float vigIntensity = Mathf.Max(damage.vignetteIntensity, san.vignetteIntensity);
            Color vigColor = BlendVignetteColor(damage, san);
            float vigSmooth = damage.vignetteIntensity >= san.vignetteIntensity
                ? damage.vignetteSmoothness
                : san.vignetteSmoothness;

            float grain = Mathf.Max(damage.filmGrain, san.filmGrain);
            float grainResponse = san.filmGrain >= damage.filmGrain
                ? san.filmGrainResponse
                : damage.filmGrainResponse;
            FilmGrainLookup grainType = san.filmGrain > 0.001f
                ? san.grainType
                : damage.grainType;

            float chroma = Mathf.Max(damage.chromatic, san.chromatic);
            float distort = Mathf.Max(damage.distort, san.distort);
            float blur = Mathf.Clamp01(Mathf.Max(damage.blur, san.blur));

            colorAdjustments.active = true;
            colorAdjustments.saturation.Override(saturation);
            colorAdjustments.postExposure.Override(exposure);
            colorAdjustments.contrast.Override(contrast);
            colorAdjustments.colorFilter.Override(filter);
            colorAdjustments.hueShift.Override(hueShift);

            vignette.active = true;
            vignette.color.Override(vigColor);
            vignette.intensity.Override(Mathf.Clamp01(vigIntensity));
            vignette.smoothness.Override(vigSmooth);
            vignette.rounded.Override(true);

            filmGrain.active = true;
            filmGrain.type.Override(grainType);
            filmGrain.intensity.Override(Mathf.Clamp01(grain));
            filmGrain.response.Override(Mathf.Clamp01(grainResponse));

            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(Mathf.Clamp01(chroma));

            lensDistortion.active = true;
            lensDistortion.intensity.Override(Mathf.Clamp(distort, -1f, 1f));
            lensDistortion.xMultiplier.Override(1f);
            lensDistortion.yMultiplier.Override(0.85f);
            lensDistortion.scale.Override(1f);

            // 近距起糊 强度抬高时模糊半径与覆盖范围同步加大
            depthOfField.active = true;
            if (blur <= 0.001f)
            {
                depthOfField.mode.Override(DepthOfFieldMode.Off);
            }
            else
            {
                depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
                depthOfField.gaussianStart.Override(0f);
                depthOfField.gaussianEnd.Override(Mathf.Lerp(40f, 3f, blur));
                depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(0.5f, 1.5f, blur));
                depthOfField.highQualitySampling.Override(false);
            }
        }

        /// <summary>
        /// 滤镜通道相乘实现冷色与病色叠加
        /// </summary>
        private static Color MultiplyFilter(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, 1f);
        }

        /// <summary>
        /// 按暗角强度加权混合暗角色
        /// </summary>
        private static Color BlendVignetteColor(HudPostContribution damage, HudPostContribution san)
        {
            float damageVig = Mathf.Max(0f, damage.vignetteIntensity);
            float sanVig = Mathf.Max(0f, san.vignetteIntensity);
            float total = damageVig + sanVig;
            if (total <= 0.001f)
                return Color.black;

            return (damage.vignetteColor * damageVig + san.vignetteColor * sanVig) / total;
        }

        /// <summary>
        /// 确保唯一 Volume 与覆盖项存在
        /// </summary>
        private void EnsureVolume()
        {
            if (colorAdjustments != null && vignette != null && filmGrain != null
                && chromaticAberration != null && lensDistortion != null && depthOfField != null)
            {
                if (volume != null)
                {
                    volume.enabled = true;
                    volume.weight = 1f;
                    volume.isGlobal = true;
                }

                return;
            }

            if (volume == null || (!ownsVolumeGo && volume.gameObject == gameObject))
                CreateOwnedVolume(volumePriority);

            volume.enabled = true;
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.priority = Mathf.Max(volume.priority, volumePriority);

            if (runtimeProfile == null)
            {
                // 克隆预创建资源 避免运行时 Add 触发编辑器 VolumeComponentEditor
                if (profileTemplate != null)
                {
                    runtimeProfile = Instantiate(profileTemplate);
                    runtimeProfile.name = "HudPostProcessMixer_RuntimeClone";
                    runtimeProfile.hideFlags = HideFlags.DontSave;
                    volume.sharedProfile = runtimeProfile;
                    ownsProfile = true;
                }
                else if (volume.sharedProfile == null)
                {
                    runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                    runtimeProfile.name = "HudPostProcessMixer_RuntimeProfile";
                    runtimeProfile.hideFlags = HideFlags.DontSave;
                    volume.sharedProfile = runtimeProfile;
                    ownsProfile = true;
                }
                else
                {
                    runtimeProfile = volume.sharedProfile;
                }
            }

            if (runtimeProfile == null) return;

            // 模板已含覆盖项时只 TryGet 不 Add
            runtimeProfile.TryGet(out colorAdjustments);
            runtimeProfile.TryGet(out vignette);
            runtimeProfile.TryGet(out filmGrain);
            runtimeProfile.TryGet(out chromaticAberration);
            runtimeProfile.TryGet(out lensDistortion);
            runtimeProfile.TryGet(out depthOfField);

            if (colorAdjustments == null)
                colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);
            if (vignette == null)
                vignette = runtimeProfile.Add<Vignette>(true);
            if (filmGrain == null)
                filmGrain = runtimeProfile.Add<FilmGrain>(true);
            if (chromaticAberration == null)
                chromaticAberration = runtimeProfile.Add<ChromaticAberration>(true);
            if (lensDistortion == null)
                lensDistortion = runtimeProfile.Add<LensDistortion>(true);
            if (depthOfField == null)
                depthOfField = runtimeProfile.Add<DepthOfField>(true);

            colorAdjustments.active = true;
            vignette.active = true;
            filmGrain.active = true;
            chromaticAberration.active = true;
            lensDistortion.active = true;
            depthOfField.active = true;
        }

        /// <summary>
        /// 在场景根创建隐藏 Volume 不挂 UI 下
        /// </summary>
        /// <param name="priority">Volume 优先级</param>
        private void CreateOwnedVolume(float priority)
        {
            var volumeGo = new GameObject("HudPostProcessMixer_Volume");
            volumeGo.hideFlags = HideFlags.HideInHierarchy;

            volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.weight = 1f;
            volume.priority = priority;
            ownsVolumeGo = true;
        }
    }
}
