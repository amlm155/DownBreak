using MieMieFrameWork;
using UnityEngine;

namespace DBWeaponSystem
{
    /// <summary>
    /// 将武器挂点的信息与手部骨骼世界坐标进行同步
    /// 注意:武器挂点和手部骨骼是分开的 需要分别同步
    /// 如果直接将武器挂到手部骨骼上也不是不行 但是Debug的时候会非常不方便
    /// </summary>
    public class WeaponAsyncHandPos : MonoBehaviour
    {
        [SerializeField]
        /// <summary> 手臂骨骼根 用于查找 Hand_L Hand_R </summary>
        private Transform rootTransform;

        [SerializeField]
        /// <summary> 左手骨骼 </summary>
        private Transform leftHandPos;

        [SerializeField]
        /// <summary> 右手骨骼 </summary>
        private Transform rightHandPos;

        [SerializeField]
        /// <summary> 左手武器挂点 与手臂根平级 </summary>
        private Transform leftHandWeaponPos;

        [SerializeField]
        /// <summary> 右手武器挂点 与手臂根平级 </summary>
        private Transform rightHandWeaponPos;

        public Transform LeftHandWeaponPos => leftHandWeaponPos;
        public Transform RightHandWeaponPos => rightHandWeaponPos;

        void Start()
        {
            InitComponent();
        }

        private void LateUpdate()
        {
            if (leftHandPos == null || rightHandPos == null)
                InitComponent();

            // 动画算完后再同步 避免抖一帧
            if (leftHandPos != null && leftHandWeaponPos != null)
                leftHandWeaponPos.SetPositionAndRotation(leftHandPos.position, leftHandPos.rotation);

            if (rightHandPos != null && rightHandWeaponPos != null)
                rightHandWeaponPos.SetPositionAndRotation(rightHandPos.position, rightHandPos.rotation);
        }

        /// <summary>
        /// 解析手骨与挂点引用
        /// </summary>
        private void InitComponent()
        {
            if (rootTransform == null)
                rootTransform = transform;

            if (leftHandPos == null)
                leftHandPos = rootTransform.FindDeepChild("Hand_L");

            if (rightHandPos == null)
                rightHandPos = rootTransform.FindDeepChild("Hand_R");

            if (leftHandWeaponPos == null)
                leftHandWeaponPos = transform.Find("LeftHandWeaponPos");

            if (rightHandWeaponPos == null)
                rightHandWeaponPos = transform.Find("RightHandWeaponPos");

            if (rightHandPos == null)
                Debug.LogWarning("WeaponAsyncHandPos 未找到 Hand_R 武器将无法跟手", this);

            if (rightHandWeaponPos == null)
                Debug.LogWarning("WeaponAsyncHandPos 未找到 RightHandWeaponPos", this);
        }
    }
}
