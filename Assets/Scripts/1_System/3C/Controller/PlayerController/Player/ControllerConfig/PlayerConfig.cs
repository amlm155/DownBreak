using GAS.StateSystem;
using MieMieFrameWork.Asset;
using PlayerControllerSpace.FpMotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace PlayerControllerSpace
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "DBProjectConfig/Player/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        /// <summary> 单例缓存 </summary>
        private static PlayerConfig instance;

        public static PlayerConfig Instance
        {
            get
            {
                if (instance == null)
                    instance = MmAssetMgr.LoadAsset<PlayerConfig>("PlayerConfig");
                return instance;
            }
        }
        [BoxGroup("合成设置"), SerializeField, LabelText("玩家初始合成等级")]
        private int playerCraftingLevel = 0;
        [BoxGroup("游戏阶段"), SerializeField, LabelText("当前所在楼层")]
        private int currentFloorLevel = 10;
        [BoxGroup("属性设置"), SerializeField, LabelText("自动回复体力 点/秒")]
        private float autoRecoveryPowerSpeed = 10f;
        [BoxGroup("属性设置"), SerializeField, LabelText("减少体力 点/秒")]
        private float reducePowerSpeed = 8f;
        [BoxGroup("属性设置"), SerializeField, LabelText("回复体力等待时间 秒")]
        private float recoveryPowerWaitTime = 1f;
        [BoxGroup("属性设置"), SerializeField, LabelText("自动降低水分 点/分钟")]
        private float autoReduceWaterSpeed = 60f;
        [BoxGroup("属性设置"), SerializeField, LabelText("自动降低饱食度 点/分钟")]
        private float autoReduceFoodSpeed = 60f;

        [BoxGroup("运动表现设置"), SerializeField, LabelText("下蹲高度"), Range(0.1f, 1.5f)]
        private float crouchHeight = 1f;

        [BoxGroup("运动表现设置"), SerializeField, LabelText("下蹲过程时间 秒"), Range(0.05f, 1f)]
        private float crouchDuration = 0.2f;
        [BoxGroup("运动表现设置"), SerializeField, LabelText("下蹲时移动速度倍率"), Range(0.1f, 2f)]
        private float crouchMoveSpeedRate = 0.5f;

        [BoxGroup("运动表现设置"), SerializeField, LabelText("普通移动速度倍率"), Range(0.1f, 2f)]
        private float normalMoveSpeedRate = 1f;

        [BoxGroup("运动表现设置"), SerializeField, LabelText("跑步时移动速度倍率"), Range(0.1f, 2f)]
        private float sprintMoveSpeedRate = 1.5f;

        [BoxGroup("运动表现设置"), SerializeField, LabelText("普通FOV")]
        private float normalFov = 60f;

        [BoxGroup("运动表现设置"), SerializeField, LabelText("跑步FOV增量")]
        private float sprintFovIncrement = 20f;
        [BoxGroup("运动表现设置"), SerializeField, LabelText("左斜角度")]
        private float leftSlantAngle = -30f;
        [BoxGroup("运动表现设置"), SerializeField, LabelText("右斜角度")]
        private float rightSlantAngle = 30f;

        [BoxGroup("运动表现设置"), SerializeField, LabelText("冲刺FOV插值速度"), Range(0.1f, 30f)]
        private float sprintFovLerpSpeed = 10f;

        [BoxGroup("视觉表现设置"), SerializeField, LabelText("长时间不动作阈值")]
        private float longTimeToIdle = 10f;

        [BoxGroup("视觉表现设置"), SerializeField, LabelText("视角灵敏度"), Range(0.01f, 1f)]
        private float lookSensitivity = 0.8f;

        [BoxGroup("视觉表现设置"), SerializeField, LabelText("垂直视角钳制")]
        private Vector2 verticalLookLimit = new Vector2(-90f, 90f);

        [BoxGroup("视觉表现设置"), SerializeField, LabelText("残血冷色滤镜阈值")]
        private Vector2Int criticalThreshold = new Vector2Int(10, 20);
        [BoxGroup("视觉表现设置"), SerializeField, LabelText("濒死黑边阈值")]
        private Vector2Int highHpThreshold = new Vector2Int(1, 9);

        [BoxGroup("视觉表现设置"), SerializeField, LabelText("轻度理智阈值")]
        private Vector2Int sanMildThreshold = new Vector2Int(40, 60);
        [BoxGroup("视觉表现设置"), SerializeField, LabelText("中度理智阈值")]
        private Vector2Int sanMediumThreshold = new Vector2Int(20, 39);
        [BoxGroup("视觉表现设置"), SerializeField, LabelText("重度理智阈值")]
        private Vector2Int sanSevereThreshold = new Vector2Int(1, 19);

        [BoxGroup("攻击动画速度"), SerializeField, LabelText("轻攻击动画速度"), MinValue(0.01f)]
        private float lightAttackAnimSpeed = 1f;

        [BoxGroup("攻击动画速度"), SerializeField, LabelText("重攻击动画速度"), MinValue(0.01f)]
        private float heavyAttackAnimSpeed = 1f;

        [BoxGroup("程序化呼吸"), SerializeField, LabelText("呼吸幅度")]
        private float proceduralBreathAmplitude = 0.01f;

        [BoxGroup("程序化呼吸"), SerializeField, LabelText("呼吸频率")]
        private float proceduralBreathFrequency = 1f;
        [BoxGroup("程序化手臂动作"), SerializeField, LabelText("启用FP手臂晃动")]
        private bool enableFpHandsMotion = true;

        [BoxGroup("程序化手臂动作"), SerializeField, LabelText("跟随平滑"), Range(1f, 40f)]
        [Tooltip("越大跟得越紧 1很肉 20很跟手")]
        private float fpFollowSpeed = 14f;

        [BoxGroup("程序化手臂动作"), SerializeField, LabelText("视角晃动")]
        private FpSwaySettings fpLookSway = new FpSwaySettings
        {
            Enabled = true,
            InputLimit = 8f,
            PositionStrength = new Vector3(0.008f, 0.006f, 0f),
            RotationStrength = new Vector3(1.4f, 1.6f, 0.9f)
        };

        [BoxGroup("程序化手臂动作"), SerializeField, LabelText("移动晃动")]
        [Tooltip("跑得越快晃越大 调位置强度/旋转强度")]
        private FpSwaySettings fpMoveSway = new FpSwaySettings
        {
            Enabled = true,
            InputLimit = 15f,
            PositionStrength = new Vector3(0.004f, 0.002f, 0.003f),
            RotationStrength = new Vector3(0.12f, 0.16f, 0.2f)
        };

        [BoxGroup("程序化手臂动作"), SerializeField, LabelText("脚步起伏")]
        private FpBobSettings fpBob = new FpBobSettings();

        public float CrouchHeight => crouchHeight;
        public float CrouchDuration => crouchDuration;
        public float CrouchMoveSpeedRate => crouchMoveSpeedRate;
        public float NormalMoveSpeedRate => normalMoveSpeedRate;
        public float SprintMoveSpeedRate => sprintMoveSpeedRate;
        public float LookSensitivity => lookSensitivity;
        public Vector2 VerticalLookLimit => verticalLookLimit;
        public float ProceduralBreathAmplitude => proceduralBreathAmplitude;
        public float ProceduralBreathFrequency => proceduralBreathFrequency;
        public bool EnableFpHandsMotion => enableFpHandsMotion;
        public float FpFollowSpeed => fpFollowSpeed;
        public FpSwaySettings FpLookSway => fpLookSway;
        public FpSwaySettings FpMoveSway => fpMoveSway;
        public FpBobSettings FpBob => fpBob;
        public float LongTimeToIdle => longTimeToIdle;

        public float AutoRecoveryPowerSpeed => autoRecoveryPowerSpeed;
        public float ReducePowerSpeed => reducePowerSpeed;
        public float RecoveryPowerWaitTime => recoveryPowerWaitTime;
        /// <summary> 水分下降 点/秒 由配置的点/分钟换算 </summary>
        public float AutoReduceWaterSpeed => autoReduceWaterSpeed / 60f;
        /// <summary> 饱食下降 点/秒 由配置的点/分钟换算 </summary>
        public float AutoReduceFoodSpeed => autoReduceFoodSpeed / 60f;
        /// <summary> 残血冷色滤镜血量区间 x最小 y最大 </summary>
        public Vector2Int CriticalThreshold => criticalThreshold;
        /// <summary> 濒死黑边血量区间 x最小 y最大 </summary>
        public Vector2Int HighHpThreshold => highHpThreshold;
        /// <summary> 轻度理智区间 x最小 y最大 </summary>
        public Vector2Int SanMildThreshold => sanMildThreshold;
        /// <summary> 中度理智区间 x最小 y最大 </summary>
        public Vector2Int SanMediumThreshold => sanMediumThreshold;
        /// <summary> 重度理智区间 x最小 y最大 </summary>
        public Vector2Int SanSevereThreshold => sanSevereThreshold;

        public float NormalFov => normalFov;
        public float SprintFovIncrement => sprintFovIncrement;
        public float SprintFovLerpSpeed => sprintFovLerpSpeed;
        public float LeftSlantAngle => leftSlantAngle;
        public float RightSlantAngle => rightSlantAngle;

        /// <summary> 轻攻击动画播放速度 </summary>
        public float LightAttackAnimSpeed => lightAttackAnimSpeed;

        /// <summary> 重攻击动画播放速度 </summary>
        public float HeavyAttackAnimSpeed => heavyAttackAnimSpeed;

        /// <summary> 玩家初始合成等级 </summary>
        public int PlayerCraftingLevel => playerCraftingLevel;
        /// <summary> 当前所在楼层 </summary>
        public int CurrentFloorLevel => currentFloorLevel;
    }
}
