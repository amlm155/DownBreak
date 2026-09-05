using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 理智下降后处理 参考 Outlast Amnesia 等恐怖游
    /// 贡献上报给 HudPostProcessMixer 与血量效果可叠加
    /// 外部传入 0到1 强度 组件不负责算数值
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/MmUI/SanIndicator")]
    public class SanIndicator : MonoBehaviour
    {
        #region 轻度

        /// <summary> 轻度胶片颗粒 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("颗粒")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float mildGrain = 0.14f;

        /// <summary> 轻度色散 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("色散")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float mildChromatic = 0.08f;

        /// <summary> 轻度暗角强度 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("暗角")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float mildVignette = 0.14f;

        /// <summary> 轻度去饱和 正数表示减饱和量 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("去饱和")]
        [PropertyRange(0f, 100f)]
        [SerializeField]
        private float mildDesaturate = 8f;

        /// <summary> 轻度压暗曝光 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("压暗")]
        [PropertyRange(-2f, 0f)]
        [SerializeField]
        private float mildExposure = -0.06f;

        /// <summary> 轻度对比度增量 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("对比度")]
        [PropertyRange(0f, 50f)]
        [SerializeField]
        private float mildContrast = 2.5f;

        /// <summary> 轻度镜头畸变 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("畸变")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float mildDistort = 0.02f;

        /// <summary> 轻度病色滤镜 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("病色滤镜")]
        [SerializeField]
        private Color mildFilter = new Color(0.9f, 0.92f, 0.88f, 1f);

        /// <summary> 轻度暗角色 偏紫灰 </summary>
        [FoldoutGroup("轻度")]
        [LabelText("暗角色")]
        [SerializeField]
        private Color mildVignetteColor = new Color(0.22f, 0.16f, 0.28f, 1f);

        #endregion

        #region 中度

        /// <summary> 中度胶片颗粒 </summary>
        [FoldoutGroup("中度")]
        [LabelText("颗粒")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float mediumGrain = 0.42f;

        /// <summary> 中度色散 </summary>
        [FoldoutGroup("中度")]
        [LabelText("色散")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float mediumChromatic = 0.28f;

        /// <summary> 中度暗角强度 </summary>
        [FoldoutGroup("中度")]
        [LabelText("暗角")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float mediumVignette = 0.38f;

        /// <summary> 中度去饱和 </summary>
        [FoldoutGroup("中度")]
        [LabelText("去饱和")]
        [PropertyRange(0f, 100f)]
        [SerializeField]
        private float mediumDesaturate = 28f;

        /// <summary> 中度压暗曝光 </summary>
        [FoldoutGroup("中度")]
        [LabelText("压暗")]
        [PropertyRange(-2f, 0f)]
        [SerializeField]
        private float mediumExposure = -0.18f;

        /// <summary> 中度对比度增量 </summary>
        [FoldoutGroup("中度")]
        [LabelText("对比度")]
        [PropertyRange(0f, 50f)]
        [SerializeField]
        private float mediumContrast = 8f;

        /// <summary> 中度镜头畸变 </summary>
        [FoldoutGroup("中度")]
        [LabelText("畸变")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float mediumDistort = 0.08f;

        /// <summary> 中度病色滤镜 绿紫 </summary>
        [FoldoutGroup("中度")]
        [LabelText("病色滤镜")]
        [SerializeField]
        private Color mediumFilter = new Color(0.78f, 0.86f, 0.72f, 1f);

        /// <summary> 中度暗角色 </summary>
        [FoldoutGroup("中度")]
        [LabelText("暗角色")]
        [SerializeField]
        private Color mediumVignetteColor = new Color(0.28f, 0.12f, 0.32f, 1f);

        #endregion

        #region 重度

        /// <summary> 重度胶片颗粒 </summary>
        [FoldoutGroup("重度")]
        [LabelText("颗粒")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float severeGrain = 0.72f;

        /// <summary> 重度色散 </summary>
        [FoldoutGroup("重度")]
        [LabelText("色散")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float severeChromatic = 0.55f;

        /// <summary> 重度暗角强度 </summary>
        [FoldoutGroup("重度")]
        [LabelText("暗角")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float severeVignette = 0.58f;

        /// <summary> 重度去饱和 </summary>
        [FoldoutGroup("重度")]
        [LabelText("去饱和")]
        [PropertyRange(0f, 100f)]
        [SerializeField]
        private float severeDesaturate = 48f;

        /// <summary> 重度压暗曝光 </summary>
        [FoldoutGroup("重度")]
        [LabelText("压暗")]
        [PropertyRange(-2f, 0f)]
        [SerializeField]
        private float severeExposure = -0.38f;

        /// <summary> 重度对比度增量 </summary>
        [FoldoutGroup("重度")]
        [LabelText("对比度")]
        [PropertyRange(0f, 50f)]
        [SerializeField]
        private float severeContrast = 16f;

        /// <summary> 重度镜头畸变 </summary>
        [FoldoutGroup("重度")]
        [LabelText("畸变")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float severeDistort = 0.22f;

        /// <summary> 重度色相偏移 </summary>
        [FoldoutGroup("重度")]
        [LabelText("色相偏移")]
        [PropertyRange(-30f, 30f)]
        [SerializeField]
        private float severeHueShift = -8f;

        /// <summary> 重度病色滤镜 </summary>
        [FoldoutGroup("重度")]
        [LabelText("病色滤镜")]
        [SerializeField]
        private Color severeFilter = new Color(0.7f, 0.78f, 0.68f, 1f);

        /// <summary> 重度暗角色 </summary>
        [FoldoutGroup("重度")]
        [LabelText("暗角色")]
        [SerializeField]
        private Color severeVignetteColor = new Color(0.18f, 0.05f, 0.22f, 1f);

        #endregion

        #region 呼吸与引用

        /// <summary> 中重度暗角呼吸频率 Hz </summary>
        [FoldoutGroup("呼吸")]
        [LabelText("暗角呼吸频率")]
        [SerializeField]
        private float vignettePulseHz = 0.9f;

        /// <summary> 重度畸变呼吸频率 Hz </summary>
        [FoldoutGroup("呼吸")]
        [LabelText("畸变呼吸频率")]
        [SerializeField]
        private float distortPulseHz = 1.35f;

        /// <summary> 重度色散闪烁频率 Hz </summary>
        [FoldoutGroup("呼吸")]
        [LabelText("色散闪烁频率")]
        [SerializeField]
        private float chromaticFlickerHz = 2.4f;

        /// <summary> 暗角呼吸振幅 0到1 </summary>
        [FoldoutGroup("呼吸")]
        [LabelText("暗角呼吸振幅")]
        [PropertyRange(0f, 0.5f)]
        [SerializeField]
        private float vignettePulseAmp = 0.12f;

        /// <summary> 混音器 可空则向上查找或在父节点自建 </summary>
        [FoldoutGroup("引用")]
        [LabelText("混音器 可空")]
        [SerializeField]
        private HudPostProcessMixer mixer;

        /// <summary> 是否已有参数备份 </summary>
        [FoldoutGroup("备份")]
        [LabelText("已备份")]
        [ReadOnly]
        [SerializeField]
        private bool hasParamBackup;

        /// <summary> 参数备份快照 </summary>
        [SerializeField]
        [HideInInspector]
        private ParamBackup paramBackup;

        #endregion

        #region 运行时

        /// <summary> 轻度强度 0到1 </summary>
        private float mild;

        /// <summary> 中度强度 0到1 </summary>
        private float medium;

        /// <summary> 重度强度 0到1 </summary>
        private float severe;

        #endregion

        public float Mild => mild;
        public float Medium => medium;
        public float Severe => severe;

        private void Awake()
        {
            InitComponents();
            ApplyVisual();
        }

        private void Update()
        {
            // 中重度需要呼吸脉冲持续刷新
            if (medium > 0.001f || severe > 0.001f)
                ApplyVisual();
        }

        private void OnDestroy()
        {
            if (mixer != null)
                mixer.ClearSanContribution();
        }

        /// <summary>
        /// 设置轻度强度 0无效果 1满强度
        /// </summary>
        public void SetMild(float amount01)
        {
            mild = Mathf.Clamp01(amount01);
            ApplyVisual();
        }

        /// <summary>
        /// 设置中度强度 0无效果 1满强度
        /// </summary>
        public void SetMedium(float amount01)
        {
            medium = Mathf.Clamp01(amount01);
            ApplyVisual();
        }

        /// <summary>
        /// 设置重度强度 0无效果 1满强度
        /// </summary>
        public void SetSevere(float amount01)
        {
            severe = Mathf.Clamp01(amount01);
            ApplyVisual();
        }

        /// <summary>
        /// 同时设置三阶段强度
        /// </summary>
        public void SetState(float mild01, float medium01, float severe01)
        {
            mild = Mathf.Clamp01(mild01);
            medium = Mathf.Clamp01(medium01);
            severe = Mathf.Clamp01(severe01);
            ApplyVisual();
        }

        /// <summary>
        /// 清除全部理智后处理
        /// </summary>
        public void ClearState()
        {
            SetState(0f, 0f, 0f);
        }

#if UNITY_EDITOR
        [FoldoutGroup("备份")]
        [HorizontalGroup("备份/操作")]
        [Button("备份当前参数", ButtonSizes.Medium)]
        private void EditorBackupParams()
        {
            UnityEditor.Undo.RecordObject(this, "Backup SanIndicator Params");
            paramBackup = CaptureParamBackup();
            hasParamBackup = true;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("SanIndicator 参数已备份", this);
        }

        [FoldoutGroup("备份")]
        [HorizontalGroup("备份/操作")]
        [Button("回填备份参数", ButtonSizes.Medium)]
        private void EditorRestoreParams()
        {
            if (!hasParamBackup || paramBackup == null)
            {
                Debug.LogWarning("SanIndicator 尚无备份可回填", this);
                return;
            }

            UnityEditor.Undo.RecordObject(this, "Restore SanIndicator Params");
            ApplyParamBackup(paramBackup);
            UnityEditor.EditorUtility.SetDirty(this);
            if (Application.isPlaying)
                ApplyVisual();
            Debug.Log("SanIndicator 已从备份回填参数", this);
        }

        [FoldoutGroup("调试")]
        [Button("测试轻度")]
        private void EditorTestMild()
        {
            if (!Application.isPlaying) return;
            SetState(0.85f, 0f, 0f);
        }

        [FoldoutGroup("调试")]
        [Button("测试中度")]
        private void EditorTestMedium()
        {
            if (!Application.isPlaying) return;
            SetState(0f, 0.85f, 0f);
        }

        [FoldoutGroup("调试")]
        [Button("测试重度")]
        private void EditorTestSevere()
        {
            if (!Application.isPlaying) return;
            SetState(0f, 0f, 0.9f);
        }

        [FoldoutGroup("调试")]
        [Button("清除状态")]
        private void EditorTestClear()
        {
            if (!Application.isPlaying) return;
            ClearState();
        }
#endif

        /// <summary>
        /// 捕获当前可调参数快照
        /// </summary>
        private ParamBackup CaptureParamBackup()
        {
            return new ParamBackup
            {
                mildGrain = mildGrain,
                mildChromatic = mildChromatic,
                mildVignette = mildVignette,
                mildDesaturate = mildDesaturate,
                mildExposure = mildExposure,
                mildContrast = mildContrast,
                mildDistort = mildDistort,
                mildFilter = mildFilter,
                mildVignetteColor = mildVignetteColor,
                mediumGrain = mediumGrain,
                mediumChromatic = mediumChromatic,
                mediumVignette = mediumVignette,
                mediumDesaturate = mediumDesaturate,
                mediumExposure = mediumExposure,
                mediumContrast = mediumContrast,
                mediumDistort = mediumDistort,
                mediumFilter = mediumFilter,
                mediumVignetteColor = mediumVignetteColor,
                severeGrain = severeGrain,
                severeChromatic = severeChromatic,
                severeVignette = severeVignette,
                severeDesaturate = severeDesaturate,
                severeExposure = severeExposure,
                severeContrast = severeContrast,
                severeDistort = severeDistort,
                severeHueShift = severeHueShift,
                severeFilter = severeFilter,
                severeVignetteColor = severeVignetteColor,
                vignettePulseHz = vignettePulseHz,
                distortPulseHz = distortPulseHz,
                chromaticFlickerHz = chromaticFlickerHz,
                vignettePulseAmp = vignettePulseAmp
            };
        }

        /// <summary>
        /// 用快照回填可调参数
        /// </summary>
        private void ApplyParamBackup(ParamBackup backup)
        {
            mildGrain = backup.mildGrain;
            mildChromatic = backup.mildChromatic;
            mildVignette = backup.mildVignette;
            mildDesaturate = backup.mildDesaturate;
            mildExposure = backup.mildExposure;
            mildContrast = backup.mildContrast;
            mildDistort = backup.mildDistort;
            mildFilter = backup.mildFilter;
            mildVignetteColor = backup.mildVignetteColor;
            mediumGrain = backup.mediumGrain;
            mediumChromatic = backup.mediumChromatic;
            mediumVignette = backup.mediumVignette;
            mediumDesaturate = backup.mediumDesaturate;
            mediumExposure = backup.mediumExposure;
            mediumContrast = backup.mediumContrast;
            mediumDistort = backup.mediumDistort;
            mediumFilter = backup.mediumFilter;
            mediumVignetteColor = backup.mediumVignetteColor;
            severeGrain = backup.severeGrain;
            severeChromatic = backup.severeChromatic;
            severeVignette = backup.severeVignette;
            severeDesaturate = backup.severeDesaturate;
            severeExposure = backup.severeExposure;
            severeContrast = backup.severeContrast;
            severeDistort = backup.severeDistort;
            severeHueShift = backup.severeHueShift;
            severeFilter = backup.severeFilter;
            severeVignetteColor = backup.severeVignetteColor;
            vignettePulseHz = backup.vignettePulseHz;
            distortPulseHz = backup.distortPulseHz;
            chromaticFlickerHz = backup.chromaticFlickerHz;
            vignettePulseAmp = backup.vignettePulseAmp;
        }

        /// <summary>
        /// 理智后处理参数备份
        /// </summary>
        [Serializable]
        private class ParamBackup
        {
            /// <summary> 轻度颗粒 </summary>
            public float mildGrain;

            /// <summary> 轻度色散 </summary>
            public float mildChromatic;

            /// <summary> 轻度暗角 </summary>
            public float mildVignette;

            /// <summary> 轻度去饱和 </summary>
            public float mildDesaturate;

            /// <summary> 轻度压暗 </summary>
            public float mildExposure;

            /// <summary> 轻度对比度 </summary>
            public float mildContrast;

            /// <summary> 轻度畸变 </summary>
            public float mildDistort;

            /// <summary> 轻度病色滤镜 </summary>
            public Color mildFilter;

            /// <summary> 轻度暗角色 </summary>
            public Color mildVignetteColor;

            /// <summary> 中度颗粒 </summary>
            public float mediumGrain;

            /// <summary> 中度色散 </summary>
            public float mediumChromatic;

            /// <summary> 中度暗角 </summary>
            public float mediumVignette;

            /// <summary> 中度去饱和 </summary>
            public float mediumDesaturate;

            /// <summary> 中度压暗 </summary>
            public float mediumExposure;

            /// <summary> 中度对比度 </summary>
            public float mediumContrast;

            /// <summary> 中度畸变 </summary>
            public float mediumDistort;

            /// <summary> 中度病色滤镜 </summary>
            public Color mediumFilter;

            /// <summary> 中度暗角色 </summary>
            public Color mediumVignetteColor;

            /// <summary> 重度颗粒 </summary>
            public float severeGrain;

            /// <summary> 重度色散 </summary>
            public float severeChromatic;

            /// <summary> 重度暗角 </summary>
            public float severeVignette;

            /// <summary> 重度去饱和 </summary>
            public float severeDesaturate;

            /// <summary> 重度压暗 </summary>
            public float severeExposure;

            /// <summary> 重度对比度 </summary>
            public float severeContrast;

            /// <summary> 重度畸变 </summary>
            public float severeDistort;

            /// <summary> 重度色相偏移 </summary>
            public float severeHueShift;

            /// <summary> 重度病色滤镜 </summary>
            public Color severeFilter;

            /// <summary> 重度暗角色 </summary>
            public Color severeVignetteColor;

            /// <summary> 暗角呼吸频率 </summary>
            public float vignettePulseHz;

            /// <summary> 畸变呼吸频率 </summary>
            public float distortPulseHz;

            /// <summary> 色散闪烁频率 </summary>
            public float chromaticFlickerHz;

            /// <summary> 暗角呼吸振幅 </summary>
            public float vignettePulseAmp;
        }

        /// <summary>
        /// 计算理智贡献并上报混音器
        /// </summary>
        private void ApplyVisual()
        {
            if (mixer == null) return;

            float mildT = mild;
            float mediumT = medium;
            float severeT = severe;
            float anyT = Mathf.Max(mildT, mediumT, severeT);

            if (anyT <= 0.001f)
            {
                mixer.SetSanContribution(HudPostContribution.Neutral);
                return;
            }

            // 呼吸与闪烁 中度弱重度强
            float vigPulse = SamplePulse(vignettePulseHz);
            float distortPulse = SamplePulse(distortPulseHz);
            float chromaFlicker = SamplePulse(chromaticFlickerHz);
            float pulseScale = Mathf.Max(mediumT * 0.45f, severeT);

            float grain = Mathf.Max(mildT * mildGrain, mediumT * mediumGrain, severeT * severeGrain);
            float chroma = Mathf.Max(mildT * mildChromatic, mediumT * mediumChromatic, severeT * severeChromatic);
            chroma *= Mathf.Lerp(1f, 0.55f + 0.45f * chromaFlicker, pulseScale);

            float vigBase = Mathf.Max(mildT * mildVignette, mediumT * mediumVignette, severeT * severeVignette);
            float vig = vigBase * (1f + vignettePulseAmp * vigPulse * pulseScale);

            float desat = Mathf.Max(mildT * mildDesaturate, mediumT * mediumDesaturate, severeT * severeDesaturate);
            float exposure = Mathf.Min(mildT * mildExposure, mediumT * mediumExposure, severeT * severeExposure);
            float contrast = Mathf.Max(mildT * mildContrast, mediumT * mediumContrast, severeT * severeContrast);
            float hue = severeT * severeHueShift;
            float distort = Mathf.Max(mildT * mildDistort, mediumT * mediumDistort, severeT * severeDistort)
                * (0.7f + 0.3f * distortPulse);

            Color filter = Color.white;
            if (severeT > 0.001f)
                filter = Color.Lerp(Color.white, severeFilter, severeT);
            else if (mediumT > 0.001f)
                filter = Color.Lerp(Color.white, mediumFilter, mediumT);
            else if (mildT > 0.001f)
                filter = Color.Lerp(Color.white, mildFilter, mildT);

            Color vigColor = mildVignetteColor;
            if (severeT >= mediumT && severeT >= mildT)
                vigColor = Color.Lerp(mildVignetteColor, severeVignetteColor, severeT);
            else if (mediumT >= mildT)
                vigColor = Color.Lerp(mildVignetteColor, mediumVignetteColor, mediumT);

            FilmGrainLookup grainType = FilmGrainLookup.Thin1;
            if (severeT > 0.35f)
                grainType = FilmGrainLookup.Medium5;
            else if (mediumT > 0.35f)
                grainType = FilmGrainLookup.Medium2;

            var contribution = HudPostContribution.Neutral;
            contribution.saturation = -desat;
            contribution.exposure = exposure;
            contribution.contrast = contrast;
            contribution.hueShift = hue;
            contribution.colorFilter = filter;
            contribution.vignetteIntensity = Mathf.Clamp01(vig);
            contribution.vignetteColor = vigColor;
            contribution.vignetteSmoothness = Mathf.Lerp(0.35f, 0.55f, severeT);
            contribution.filmGrain = Mathf.Clamp01(grain);
            contribution.filmGrainResponse = Mathf.Lerp(0.85f, 0.55f, severeT);
            contribution.grainType = grainType;
            contribution.chromatic = Mathf.Clamp01(chroma);
            contribution.distort = Mathf.Clamp(distort, -1f, 1f);
            mixer.SetSanContribution(contribution);
        }

        /// <summary>
        /// 0到1 正弦脉冲
        /// </summary>
        private static float SamplePulse(float hz)
        {
            return 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hz * Mathf.PI * 2f);
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitComponents()
        {
            if (mixer != null) return;

            mixer = GetComponentInParent<HudPostProcessMixer>();
            if (mixer != null) return;

            var host = transform.parent != null ? transform.parent.gameObject : gameObject;
            mixer = host.GetComponent<HudPostProcessMixer>();
            if (mixer == null)
                mixer = host.AddComponent<HudPostProcessMixer>();
        }
    }
}

