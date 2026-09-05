using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 生存向受伤屏幕反馈 贡献上报给 HudPostProcessMixer
    /// 外部传入 0到1 强度 组件不负责算血量
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/MmUI/DamageIndicator")]
    public class DamageIndicator : MonoBehaviour
    {
        /// <summary> 残血最大去饱和 0为不变 100为全灰 </summary>
        [FoldoutGroup("残血 Post")]
        [LabelText("最大去饱和")]
        [PropertyRange(0f, 100f)]
        [SerializeField]
        private float m_MaxDesaturate = 55f;

        /// <summary> 残血冷色滤镜 </summary>
        [FoldoutGroup("残血 Post")]
        [LabelText("冷色滤镜")]
        [SerializeField]
        private Color m_ColdFilter = new Color(0.72f, 0.82f, 0.95f, 1f);

        /// <summary> 残血额外压暗 </summary>
        [FoldoutGroup("残血 Post")]
        [LabelText("残血压暗")]
        [PropertyRange(-2f, 0f)]
        [SerializeField]
        private float m_GrayExposure = -0.25f;

        /// <summary> 残血最大模糊 0到1 </summary>
        [FoldoutGroup("残血 Post")]
        [LabelText("最大模糊")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float m_MaxBlur = 0.35f;

        /// <summary> 濒死 vignette 最大强度 </summary>
        [FoldoutGroup("濒死 Vignette")]
        [LabelText("最大压暗强度")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float m_MaxVignette = 0.5f;

        /// <summary> 濒死 vignette 颜色 </summary>
        [FoldoutGroup("濒死 Vignette")]
        [LabelText("边缘颜色")]
        [SerializeField]
        private Color m_VignetteColor = Color.black;

        /// <summary> 濒死最大模糊 0到1 </summary>
        [FoldoutGroup("濒死 Vignette")]
        [LabelText("最大模糊")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float m_CriticalBlur = 0.7f;

        /// <summary> 挨打边缘红色 </summary>
        [FoldoutGroup("挨打闪红")]
        [LabelText("边缘红色")]
        [SerializeField]
        private Color m_HitVignetteColor = Color.red;

        /// <summary> 挨打边缘闪红强度 </summary>
        [FoldoutGroup("挨打闪红")]
        [LabelText("边缘强度")]
        [PropertyRange(0f, 1f)]
        [SerializeField]
        private float m_HitVignette = 0.3f;

        /// <summary> 挨打 vignette 平滑 越大越靠四角 </summary>
        [FoldoutGroup("挨打闪红")]
        [LabelText("边缘收敛")]
        [PropertyRange(0.01f, 1f)]
        [SerializeField]
        private float m_HitVignetteSmoothness = 0.1f;

        /// <summary> 闪红淡入时长 </summary>
        [FoldoutGroup("挨打闪红")]
        [LabelText("淡入秒")]
        [SerializeField]
        private float m_HitFadeIn = 0.04f;

        /// <summary> 闪红淡出时长 </summary>
        [FoldoutGroup("挨打闪红")]
        [LabelText("淡出秒")]
        [SerializeField]
        private float m_HitFadeOut = 3f;

        /// <summary> 混音器 可空则向上查找或在父节点自建 </summary>
        [FoldoutGroup("引用")]
        [LabelText("混音器 可空")]
        [SerializeField]
        private HudPostProcessMixer m_Mixer;

        /// <summary> 是否已有参数备份 </summary>
        [FoldoutGroup("备份")]
        [LabelText("已备份")]
        [ReadOnly]
        [SerializeField]
        private bool m_HasParamBackup;

        /// <summary> 参数备份快照 </summary>
        [SerializeField]
        [HideInInspector]
        private ParamBackup m_ParamBackup;

        /// <summary> 残血强度 0到1 </summary>
        private float m_LowHp;

        /// <summary> 濒死强度 0到1 </summary>
        private float m_Critical;

        /// <summary> 当前闪红权重 0到1 </summary>
        private float m_HitWeight;

        /// <summary> 闪红 Tween </summary>
        private Tween m_HitTween;

        public float LowHp => m_LowHp;

        public float Critical => m_Critical;

        private void Awake()
        {
            InitComponents();
            ApplyVisual();
        }

        private void OnDestroy()
        {
            m_HitTween?.Kill();
            if (m_Mixer != null)
                m_Mixer.ClearDamageContribution();
        }

        /// <summary>
        /// 设置残血灰冷强度 0无效果 1满强度
        /// </summary>
        public void SetLowHp(float amount01)
        {
            m_LowHp = Mathf.Clamp01(amount01);
            ApplyVisual();
        }

        /// <summary>
        /// 设置濒死边缘压暗 0无效果 1满强度
        /// </summary>
        public void SetCritical(float amount01)
        {
            m_Critical = Mathf.Clamp01(amount01);
            ApplyVisual();
        }

        /// <summary>
        /// 同时设置残血与濒死强度
        /// </summary>
        public void SetState(float lowHp01, float critical01)
        {
            m_LowHp = Mathf.Clamp01(lowHp01);
            m_Critical = Mathf.Clamp01(critical01);
            ApplyVisual();
        }

        /// <summary>
        /// 播放挨打边缘闪红
        /// </summary>
        public void PlayHit()
        {
            PlayHit(1f);
        }

        /// <summary>
        /// 播放挨打边缘闪红 可调强度
        /// </summary>
        public void PlayHit(float intensity)
        {
            float peak = Mathf.Clamp01(intensity);

            m_HitTween?.Kill();
            m_HitWeight = 0f;
            m_HitTween = DOTween.Sequence()
                .Append(DOTween.To(() => m_HitWeight, w =>
                {
                    m_HitWeight = w;
                    ApplyVisual();
                }, peak, Mathf.Max(0.01f, m_HitFadeIn)))
                .Append(DOTween.To(() => m_HitWeight, w =>
                {
                    m_HitWeight = w;
                    ApplyVisual();
                }, 0f, Mathf.Max(0.01f, m_HitFadeOut)))
                .SetUpdate(true)
                .SetTarget(this);
        }

        /// <summary>
        /// 清除残血与濒死效果
        /// </summary>
        public void ClearState()
        {
            SetState(0f, 0f);
        }

#if UNITY_EDITOR
        [FoldoutGroup("备份")]
        [HorizontalGroup("备份/操作")]
        [Button("备份当前参数", ButtonSizes.Medium)]
        private void EditorBackupParams()
        {
            UnityEditor.Undo.RecordObject(this, "Backup DamageIndicator Params");
            m_ParamBackup = CaptureParamBackup();
            m_HasParamBackup = true;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("DamageIndicator 参数已备份", this);
        }

        [FoldoutGroup("备份")]
        [HorizontalGroup("备份/操作")]
        [Button("回填备份参数", ButtonSizes.Medium)]
        private void EditorRestoreParams()
        {
            if (!m_HasParamBackup || m_ParamBackup == null)
            {
                Debug.LogWarning("DamageIndicator 尚无备份可回填", this);
                return;
            }

            UnityEditor.Undo.RecordObject(this, "Restore DamageIndicator Params");
            ApplyParamBackup(m_ParamBackup);
            UnityEditor.EditorUtility.SetDirty(this);
            if (Application.isPlaying)
                ApplyVisual();
            Debug.Log("DamageIndicator 已从备份回填参数", this);
        }

        [FoldoutGroup("调试")]
        [Button("测试挨打闪红")]
        private void EditorTestHit()
        {
            if (!Application.isPlaying) return;
            PlayHit();
        }

        [FoldoutGroup("调试")]
        [Button("测试残血")]
        private void EditorTestLowHp()
        {
            if (!Application.isPlaying) return;
            SetLowHp(0.7f);
        }

        [FoldoutGroup("调试")]
        [Button("测试濒死")]
        private void EditorTestCritical()
        {
            if (!Application.isPlaying) return;
            SetState(1f, 0.8f);
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
                maxDesaturate = m_MaxDesaturate,
                coldFilter = m_ColdFilter,
                grayExposure = m_GrayExposure,
                maxBlur = m_MaxBlur,
                maxVignette = m_MaxVignette,
                vignetteColor = m_VignetteColor,
                criticalBlur = m_CriticalBlur,
                hitVignetteColor = m_HitVignetteColor,
                hitVignette = m_HitVignette,
                hitVignetteSmoothness = m_HitVignetteSmoothness,
                hitFadeIn = m_HitFadeIn,
                hitFadeOut = m_HitFadeOut
            };
        }

        /// <summary>
        /// 用快照回填可调参数
        /// </summary>
        private void ApplyParamBackup(ParamBackup backup)
        {
            m_MaxDesaturate = backup.maxDesaturate;
            m_ColdFilter = backup.coldFilter;
            m_GrayExposure = backup.grayExposure;
            m_MaxBlur = backup.maxBlur;
            m_MaxVignette = backup.maxVignette;
            m_VignetteColor = backup.vignetteColor;
            m_CriticalBlur = backup.criticalBlur;
            m_HitVignetteColor = backup.hitVignetteColor;
            m_HitVignette = backup.hitVignette;
            m_HitVignetteSmoothness = backup.hitVignetteSmoothness;
            m_HitFadeIn = backup.hitFadeIn;
            m_HitFadeOut = backup.hitFadeOut;
        }

        /// <summary>
        /// 伤害后处理参数备份
        /// </summary>
        [Serializable]
        private class ParamBackup
        {
            /// <summary> 最大去饱和 </summary>
            public float maxDesaturate;

            /// <summary> 冷色滤镜 </summary>
            public Color coldFilter;

            /// <summary> 残血压暗 </summary>
            public float grayExposure;

            /// <summary> 残血最大模糊 </summary>
            public float maxBlur;

            /// <summary> 最大压暗强度 </summary>
            public float maxVignette;

            /// <summary> 边缘颜色 </summary>
            public Color vignetteColor;

            /// <summary> 濒死最大模糊 </summary>
            public float criticalBlur;

            /// <summary> 边缘红色 </summary>
            public Color hitVignetteColor;

            /// <summary> 边缘强度 </summary>
            public float hitVignette;

            /// <summary> 边缘收敛 </summary>
            public float hitVignetteSmoothness;

            /// <summary> 淡入秒 </summary>
            public float hitFadeIn;

            /// <summary> 淡出秒 </summary>
            public float hitFadeOut;
        }

        /// <summary>
        /// 计算血量贡献并上报混音器
        /// </summary>
        private void ApplyVisual()
        {
            if (m_Mixer == null) return;

            float grayT = Mathf.Clamp01(m_LowHp);
            float sat = Mathf.Lerp(0f, -m_MaxDesaturate, grayT);
            float exposure = Mathf.Lerp(0f, m_GrayExposure, grayT);
            Color filter = Color.Lerp(Color.white, m_ColdFilter, grayT);

            float hitT = Mathf.Clamp01(m_HitWeight);
            float criticalT = Mathf.Clamp01(m_Critical);
            float vigFromCritical = m_MaxVignette * criticalT;
            float vigFromHit = m_HitVignette * hitT;
            float vigIntensity = Mathf.Max(vigFromCritical, vigFromHit);

            // 残血与濒死模糊取更强一路
            float blur = Mathf.Max(grayT * m_MaxBlur, criticalT * m_CriticalBlur);

            Color vigColor = m_VignetteColor;
            float vigSmooth = 0.45f;
            if (hitT > 0.001f)
            {
                float hitBlend = vigFromHit >= vigFromCritical
                    ? 1f
                    : Mathf.Clamp01(vigFromHit / Mathf.Max(0.001f, vigFromCritical));
                vigColor = Color.Lerp(m_VignetteColor, m_HitVignetteColor, Mathf.Max(hitT, hitBlend));
                vigSmooth = Mathf.Lerp(0.45f, m_HitVignetteSmoothness, hitT);
            }

            var contribution = HudPostContribution.Neutral;
            contribution.saturation = sat;
            contribution.exposure = exposure;
            contribution.colorFilter = filter;
            contribution.vignetteIntensity = vigIntensity;
            contribution.vignetteColor = vigColor;
            contribution.vignetteSmoothness = vigSmooth;
            contribution.blur = Mathf.Clamp01(blur);
            m_Mixer.SetDamageContribution(contribution);
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitComponents()
        {
            if (m_Mixer != null) return;

            m_Mixer = GetComponentInParent<HudPostProcessMixer>();
            if (m_Mixer != null) return;

            var host = transform.parent != null ? transform.parent.gameObject : gameObject;
            m_Mixer = host.GetComponent<HudPostProcessMixer>();
            if (m_Mixer == null)
                m_Mixer = host.AddComponent<HudPostProcessMixer>();
        }
    }
}
