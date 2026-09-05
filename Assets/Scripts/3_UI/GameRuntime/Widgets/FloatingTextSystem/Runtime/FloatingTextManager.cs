using System;
using Sirenix.OdinInspector;
using Interaction.Combat;
using MiMieEventBus;
using UnityEngine;
using MieMieFrameWork;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 跳字管理器 场景只挂这一份 订阅 CombatFeedbackEvents
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        /// <summary> 单例缓存 </summary>
        private static FloatingTextManager instance;

        /// <summary> 跳字世界预制体 </summary>
        [FoldoutGroup("资源")]
        [LabelText("跳字世界预制体")]
        [SerializeField]
        private FloatingTextWorld worldPrefab;

        /// <summary> 运行时世界实例 </summary>
        [FoldoutGroup("资源")]
        [LabelText("已有世界实例 可空")]
        [SerializeField]
        private FloatingTextWorld world;

        /// <summary> 切换场景不销毁 </summary>
        [FoldoutGroup("生命周期")]
        [LabelText("切换场景不销毁")]
        [SerializeField]
        private bool dontDestroyOnLoad;

        /// <summary> 预览相对自身的高度偏移 </summary>
        [FoldoutGroup("测试")]
        [LabelText("预览高度")]
        [SerializeField]
        private float previewHeight = 1.2f;

        /// <summary> 战斗跳字订阅 </summary>
        private IDisposable damageFloatingTextDisposable;

        public static FloatingTextManager Instance => instance;

        public FloatingTextWorld World => world;

        public int ActiveCount => world != null ? world.ActiveCount : 0;

        public int ActiveGlyphCount => world != null ? world.ActiveGlyphCount : 0;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureWorld();
            BindCombatFeedbackEvents();
        }

        private void OnDestroy()
        {
            UnbindCombatFeedbackEvents();
            if (instance == this) instance = null;
        }

        /// <summary>
        /// 订阅伤害跳字事件
        /// </summary>
        private void BindCombatFeedbackEvents()
        {
            damageFloatingTextDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                CombatFeedbackEvents.DamageFloatingText,
                OnDamageFloatingText);
        }

        /// <summary>
        /// 取消跳字事件订阅
        /// </summary>
        private void UnbindCombatFeedbackEvents()
        {
            damageFloatingTextDisposable?.Dispose();
            damageFloatingTextDisposable = null;
        }

        /// <summary>
        /// 事件驱动播放跳字
        /// </summary>
        private void OnDamageFloatingText(Vector3 worldPosition, long damage, bool isCrit)
        {
            Play(worldPosition, damage, isCrit);
        }

        /// <summary>
        /// 播放数字跳字
        /// </summary>
        public void Play(Vector3 worldPosition, int value, bool isCrit = false)
        {
            EnsureWorld();
            if (world == null) return;
            world.Play(worldPosition, value, isCrit);
        }

        /// <summary>
        /// 播放数字跳字
        /// </summary>
        public void Play(Vector3 worldPosition, long value, bool isCrit = false)
        {
            EnsureWorld();
            if (world == null) return;
            world.Play(worldPosition, value, isCrit);
        }

        /// <summary>
        /// 播放短文本跳字
        /// </summary>
        public void Play(Vector3 worldPosition, string text, bool isCrit = false)
        {
            EnsureWorld();
            if (world == null) return;
            world.Play(worldPosition, text, isCrit);
        }

        /// <summary>
        /// 普通伤害数字
        /// </summary>
        public void PlayDamage(Vector3 worldPosition, long value)
        {
            Play(worldPosition, value, false);
        }

        /// <summary>
        /// 暴击伤害数字
        /// </summary>
        public void PlayCrit(Vector3 worldPosition, long value)
        {
            Play(worldPosition, value, true);
        }

        /// <summary>
        /// 静态入口 数字
        /// </summary>
        public static void Show(Vector3 worldPosition, long value, bool isCrit = false)
        {
            if (instance == null) return;
            instance.Play(worldPosition, value, isCrit);
        }

        /// <summary>
        /// 静态入口 短文本
        /// </summary>
        public static void Show(Vector3 worldPosition, string text, bool isCrit = false)
        {
            if (instance == null) return;
            instance.Play(worldPosition, text, isCrit);
        }

        /// <summary>
        /// 测试预览一批数字与短词
        /// </summary>
        [FoldoutGroup("测试")]
        [Button("预览跳字 数字+短词", ButtonSizes.Medium)]
        [EnableIf("@UnityEngine.Application.isPlaying")]
        public void PreviewBurst()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[FloatingTextManager] 请先进入 Play 再预览", this);
                return;
            }

            EnsureWorld();
            if (world == null)
            {
                Debug.LogError("[FloatingTextManager] 无跳字世界 无法预览", this);
                return;
            }

            Vector3 origin = ResolvePreviewOrigin();
            Play(origin, 1234, false);
            Play(origin + Vector3.right * 0.55f, 8888, true);
            Play(origin + Vector3.left * 0.55f, "MISS", false);
            Play(origin + Vector3.right * 1.0f + Vector3.up * 0.35f, "CRIT", true);
            Play(origin + Vector3.up * 0.65f, "HEAL", false);
        }

        /// <summary>
        /// 测试预览普通伤害
        /// </summary>
        [FoldoutGroup("测试")]
        [Button("预览普通伤害")]
        [EnableIf("@UnityEngine.Application.isPlaying")]
        public void PreviewDamage()
        {
            if (!Application.isPlaying) return;
            PlayDamage(ResolvePreviewOrigin(), 42);
        }

        /// <summary>
        /// 测试预览暴击伤害
        /// </summary>
        [FoldoutGroup("测试")]
        [Button("预览暴击伤害")]
        [EnableIf("@UnityEngine.Application.isPlaying")]
        public void PreviewCrit()
        {
            if (!Application.isPlaying) return;
            PlayCrit(ResolvePreviewOrigin(), 999);
        }

        /// <summary>
        /// 预览点取主相机前方 每次加轻微抖动方便连点观察
        /// </summary>
        private Vector3 ResolvePreviewOrigin()
        {
            float jitterX = UnityEngine.Random.Range(-0.25f, 0.25f);
            float jitterY = UnityEngine.Random.Range(-0.1f, 0.1f);
            Camera cam = Camera.main;
            if (cam != null)
            {
                return cam.transform.position
                    + cam.transform.forward * 4f
                    + cam.transform.up * (0.4f + previewHeight * 0.15f)
                    + cam.transform.right * jitterX
                    + cam.transform.up * jitterY;
            }

            return transform.position + Vector3.up * previewHeight + new Vector3(jitterX, jitterY, 0f);
        }

        private void EnsureWorld()
        {
            // 序列化若误指到工程里的预制体资源 运行时会播一次后异常
            if (world != null && world.gameObject.scene.IsValid())
                return;

            world = null;
            world = GetComponentInChildren<FloatingTextWorld>(true);
            if (world != null && world.gameObject.scene.IsValid())
                return;

            if (worldPrefab != null)
            {
                world = Instantiate(worldPrefab, transform);
                world.name = "FloatingTextWorld";
                return;
            }

            Debug.LogError("[FloatingTextManager] 未绑定跳字世界预制体 无法播放", this);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器烘焙绑定世界预制体
        /// </summary>
        public void EditorBindWorldPrefab(FloatingTextWorld prefab)
        {
            worldPrefab = prefab;
            world = null;
        }
#endif
    }
}
