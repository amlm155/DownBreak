using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 世界掉落物 下落有物理 落地后关刚体模拟并把碰撞改成 Trigger
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class ItemWorldPhysics : MonoBehaviour
    {
        /// <summary> 静止多久算落地 </summary>
        private const float SettleDuration = 0.35f;

        /// <summary> 视为静止的速度 </summary>
        private const float SleepSpeed = 0.12f;

        /// <summary> 虚空销毁高度 </summary>
        private const float VoidDestroyHeight = -100f;

        /// <summary> 最大穿透修正速度 </summary>
        private const float MaxDepenetrationVelocity = 0.35f;

        /// <summary> 掉落最大线速度 </summary>
        private const float MaxLinearSpeed = 1f;

        /// <summary> 掉落最大角速度 </summary>
        private const float MaxAngularSpeed = 1f;

        /// <summary> 掉落线性阻尼 </summary>
        private const float WorldLinearDamping = 3f;

        /// <summary> 掉落角阻尼 </summary>
        private const float WorldAngularDamping = 3f;

        /// <summary> 根刚体 </summary>
        private Rigidbody body;

        /// <summary> 碰撞体缓存 </summary>
        private Collider[] colliderList;

        /// <summary> 累计静止时间 </summary>
        private float settledTime;

        /// <summary> 是否在下落监听 </summary>
        private bool isFalling;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliderList = GetComponentsInChildren<Collider>(true);
        }

        private void FixedUpdate()
        {
            if (!isFalling || body == null || body.isKinematic)
                return;

            float speed = body.linearVelocity.sqrMagnitude + body.angularVelocity.sqrMagnitude;
            if (speed > SleepSpeed * SleepSpeed && !body.IsSleeping())
            {
                settledTime = 0f;
                return;
            }

            settledTime += Time.fixedDeltaTime;
            if (settledTime >= SettleDuration)
                SettleOnGround();
        }

        private void Update()
        {
            if (transform.position.y < VoidDestroyHeight)
                Destroy(gameObject);
        }

        /// <summary>
        /// 扔到地上 自由下落
        /// </summary>
        public void BeginWorldMode()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();

            colliderList = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliderList.Length; i++)
            {
                if (colliderList[i] == null)
                    continue;
                colliderList[i].enabled = true;
                colliderList[i].isTrigger = false;
            }

            body.constraints = RigidbodyConstraints.None;
            body.useGravity = true;
            body.isKinematic = false;
            body.detectCollisions = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxDepenetrationVelocity = MaxDepenetrationVelocity;
            body.maxLinearVelocity = MaxLinearSpeed;
            body.maxAngularVelocity = MaxAngularSpeed;
            body.linearDamping = WorldLinearDamping;
            body.angularDamping = WorldAngularDamping;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();

            settledTime = 0f;
            isFalling = true;
        }

        /// <summary>
        /// 停止下落监听 由外部把手持物理关掉
        /// </summary>
        public void StopWorldMode()
        {
            isFalling = false;
            settledTime = 0f;
        }

        /// <summary>
        /// 落地 无物理 + Collider 变 Trigger
        /// </summary>
        private void SettleOnGround()
        {
            isFalling = false;
            settledTime = 0f;

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.isKinematic = true;
                body.detectCollisions = true;
            }

            if (colliderList == null)
                return;

            for (int i = 0; i < colliderList.Length; i++)
            {
                if (colliderList[i] == null)
                    continue;
                colliderList[i].enabled = true;
                colliderList[i].isTrigger = true;
            }
        }
    }
}
