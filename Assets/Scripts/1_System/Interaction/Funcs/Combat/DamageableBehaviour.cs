using System;
using UnityEngine;

namespace Interaction.Combat
{
    /// <summary>
    /// 简易可受伤体 先给场景物/测试怪挂用
    /// </summary>
    public class DamageableBehaviour : MonoBehaviour, IDamageable, IDurabilityProvider
    {
        public event Action<Vector3> Died;

        /// <summary> 最大生命 </summary>
        [SerializeField]
        private int maxHealth = 30;

        /// <summary> 当前生命 </summary>
        [SerializeField]
        private int currentHealth = 30;

        /// <summary> 生命归零时销毁 </summary>
        [SerializeField]
        private bool destroyOnDeath = true;

        /// <summary> 当前是否允许受伤 </summary>
        [SerializeField]
        private bool canTakeDamage = true;

        /// <summary> 是否还能继续受伤 </summary>
        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            if (currentHealth <= 0)
                currentHealth = maxHealth;
        }

        /// <summary>
        /// 施加伤害 返回实际扣减值
        /// </summary>
        public int ApplyDamage(int damage, Vector3 hitPoint)
        {
            if (!canTakeDamage || !IsAlive || damage <= 0)
                return 0;

            int appliedDamage = damage;
            if (appliedDamage > currentHealth)
                appliedDamage = currentHealth;

            currentHealth -= appliedDamage;
            if (currentHealth <= 0 && destroyOnDeath)
            {
                Died?.Invoke(hitPoint);
                Destroy(gameObject);
            }

            return appliedDamage;
        }

        /// <summary>
        /// 设置当前耐久与最大耐久
        /// </summary>
        public void SetDurability(int currentDurability, int maxDurability)
        {
            maxHealth = Mathf.Max(1, maxDurability);
            currentHealth = Mathf.Clamp(currentDurability, 0, maxHealth);
        }

        /// <summary>
        /// 设置当前是否允许受伤
        /// </summary>
        public void SetCanTakeDamage(bool canTake)
        {
            canTakeDamage = canTake;
        }

        /// <summary>
        /// 读取当前耐久与最大耐久 不管当前能不能挨打都返回真实值
        /// </summary>
        public bool TryGetDurability(out int currentDurability, out int maxDurability)
        {
            currentDurability = currentHealth;
            maxDurability = maxHealth;
            return maxHealth > 0;
        }
    }
}
