using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// HUD 后处理单路贡献 由血量或理智上报 混音器负责合成
    /// </summary>
    public struct HudPostContribution
    {
        /// <summary> 饱和度覆盖值 负数为去饱和 </summary>
        public float saturation;

        /// <summary> 曝光 </summary>
        public float exposure;

        /// <summary> 对比度 </summary>
        public float contrast;

        /// <summary> 色相偏移 </summary>
        public float hueShift;

        /// <summary> 色彩滤镜 </summary>
        public Color colorFilter;

        /// <summary> 暗角强度 </summary>
        public float vignetteIntensity;

        /// <summary> 暗角色 </summary>
        public Color vignetteColor;

        /// <summary> 暗角平滑 </summary>
        public float vignetteSmoothness;

        /// <summary> 胶片颗粒强度 </summary>
        public float filmGrain;

        /// <summary> 胶片颗粒响应 </summary>
        public float filmGrainResponse;

        /// <summary> 胶片颗粒类型 </summary>
        public FilmGrainLookup grainType;

        /// <summary> 色散强度 </summary>
        public float chromatic;

        /// <summary> 镜头畸变 </summary>
        public float distort;

        /// <summary> 高斯模糊强度 0到1 </summary>
        public float blur;

        /// <summary>
        /// 无效果中性贡献
        /// </summary>
        public static HudPostContribution Neutral => new HudPostContribution
        {
            saturation = 0f,
            exposure = 0f,
            contrast = 0f,
            hueShift = 0f,
            colorFilter = Color.white,
            vignetteIntensity = 0f,
            vignetteColor = Color.black,
            vignetteSmoothness = 0.45f,
            filmGrain = 0f,
            filmGrainResponse = 0.8f,
            grainType = FilmGrainLookup.Thin1,
            chromatic = 0f,
            distort = 0f,
            blur = 0f
        };
    }
}
