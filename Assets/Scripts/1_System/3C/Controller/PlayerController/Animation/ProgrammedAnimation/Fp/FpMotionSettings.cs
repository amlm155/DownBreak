using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace PlayerControllerSpace.FpMotion
{
    /// <summary>
    /// 落脚左右
    /// </summary>
    public enum FpFootSide
    {
        Left = 0,
        Right = 1
    }

    /// <summary>
    /// 一层晃动 只有强度 无弹簧
    /// </summary>
    [Serializable]
    public sealed class FpSwaySettings
    {
        [LabelText("启用")]
        public bool Enabled = true;

        [LabelText("输入上限")]
        [Tooltip("视角用鼠标增量上限 移动用平面速度上限")]
        [Range(0.1f, 40f)]
        public float InputLimit = 8f;

        [LabelText("位置强度")]
        [Tooltip("XYZ 本地位移倍率 调大=晃更猛")]
        public Vector3 PositionStrength = new Vector3(0.01f, 0.01f, 0.01f);

        [LabelText("旋转强度")]
        [Tooltip("XYZ 欧拉角倍率 调大=甩更猛")]
        public Vector3 RotationStrength = new Vector3(1.2f, 1.2f, 0.8f);
    }

    /// <summary>
    /// 走路起伏 频率与幅度都跟速度走
    /// </summary>
    [Serializable]
    public sealed class FpBobSettings
    {
        [LabelText("启用")]
        public bool Enabled = true;

        [LabelText("起步速度")]
        [Range(0f, 20f)]
        public float SpeedThreshold = 0.35f;

        [LabelText("走路频率")]
        [Tooltip("平面速度=参照速度时的起伏频率")]
        [Range(0.1f, 12f)]
        public float Frequency = 6.5f;

        [LabelText("参照速度")]
        [Tooltip("对齐普通走路速度 跑步更快则频率和幅度一起加大")]
        [Range(0.1f, 40f)]
        public float ReferenceSpeed = 10f;

        [LabelText("位置幅度")]
        [Tooltip("参照速度下的本地位移幅度 跑步会按速度比再乘")]
        public Vector3 PositionAmplitude = new Vector3(0.012f, 0.018f, 0f);

        [LabelText("旋转幅度")]
        [Tooltip("参照速度下的欧拉幅度 跑步会按速度比再乘")]
        public Vector3 RotationAmplitude = new Vector3(0.35f, 0.2f, 0.45f);

        [LabelText("下蹲减弱")]
        [Range(0f, 1f)]
        public float CrouchWeight = 0.45f;

        [LabelText("发射落脚事件")]
        public bool EmitFootstepEvents = true;
    }
}
