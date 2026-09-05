using System;
using cfg.item;
using MieMieFrameWork;
using MieMieFrameWork.Asset;
using Interaction;
using MiMieEventBus;
using MmInventory;
using UnityEngine;

namespace DBWeaponSystem
{
    public partial class WeaponSystem : SingletonMono<WeaponSystem>, IWeaponSystem
    {
        /// <summary> 武器挂点同步组件 </summary>
        private WeaponAsyncHandPos weaponAsyncHandPos;

        /// <summary> 当前手上武器实例 </summary>
        private GameObject currentWeaponGo;

        /// <summary> 当前手持运行时物品 </summary>
        private ItemRtData equippedItemRtData;

        /// <summary> 当前手持表 ID </summary>
        private int equippedItemTableId;

        /// <summary> 当前装备的挂点枚举 </summary>
        private EWeaponName equippedWeaponName;

        /// <summary> 当前动画模组 </summary>
        private EAnimationModelType equippedAnimationType;

        [SerializeField]
        /// <summary> 武器挂点配置 </summary>
        private WeaponConfig weaponConfig;

        /// <summary> 装备成功后请求切动画模组 由 3C 宿主订阅 </summary>
        public Action<EAnimationModelType> OnEquippedAnimationRequest;

        public WeaponConfig WeaponConfig
        {
            get => weaponConfig;
            set => weaponConfig = value;
        }

        /// <summary> 当前手持物品表 ID 0 表示空手 </summary>
        public int EquippedItemTableId => equippedItemTableId;

        /// <summary> 当前手持运行时物品 </summary>
        public ItemRtData EquippedItemRtData => equippedItemRtData;

        Action<EAnimationModelType> IWeaponSystem.OnEquippedAnimationRequest { get => OnEquippedAnimationRequest; set => OnEquippedAnimationRequest = value; }

        #region 生命周期

        // 单例登记由 SingletonMono 基类 Awake 完成 组件引用统一在 Start 兜底获取

        private void Start()
        {
            if (weaponAsyncHandPos == null)
                weaponAsyncHandPos = GetComponent<WeaponAsyncHandPos>();
            if (weaponScanner == null)
                weaponScanner = GetComponent<WeaponScanner>();

            // 绑定空手刃点
            BindFistBlade();
        }

        #endregion

        #region 装备与卸下

        /// <summary>
        /// 按运行时物品装备武器 保留耐久与实例
        /// </summary>
        public bool TryEquipWeapon(ItemRtData itemRtData, out ItemRtData oldItemRtData)
        {
            oldItemRtData = equippedItemRtData;
            if (itemRtData == null)
                return false;

            if (!TryEquipWeaponVisual(itemRtData.ExcelItemId))
                return false;

            equippedItemRtData = itemRtData;
            PublishEquippedChanged();
            return true;
        }

        /// <summary>
        /// 仅挂载武器视觉与模组
        /// </summary>
        private bool TryEquipWeaponVisual(int itemTableId)
        {
            LubanTables.EnsureLoaded();
            var weaponRow = LubanTables.Tables.TbWeapon.GetOrDefault(itemTableId);
            if (weaponRow == null)
            {
                Debug.LogWarning($"TbWeapon 无 id={itemTableId}");
                return false;
            }

            if (weaponAsyncHandPos == null)
            {
                Debug.LogWarning("当前武器挂点不存在");
                return false;
            }

            if (weaponConfig == null)
            {
                Debug.LogWarning("武器配置未设置");
                return false;
            }

            if (string.IsNullOrEmpty(weaponRow.WorldPrefabPath))
            {
                Debug.LogWarning($"武器未配置 world_prefab_path id={itemTableId}");
                return false;
            }

            if (weaponRow.WeaponName == EWeaponName.None)
            {
                Debug.LogWarning($"weapon_name 为 None 无法装备 id={itemTableId}");
                return false;
            }

            var eWeaponName = weaponRow.WeaponName;
            if (!weaponConfig.TryGetHandPosConfig(eWeaponName, out var handPosConfig))
            {
                Debug.LogWarning($"未找到武器 {eWeaponName} 的挂点配置");
                return false;
            }

            var prefab = MmAssetMgr.LoadAsset<GameObject>(weaponRow.WorldPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"加载武器预制体失败 path={weaponRow.WorldPrefabPath}");
                return false;
            }

            ClearCurrentWeaponVisual();

            var weapon = Instantiate(prefab);
            // TODO 武器实例后续改对象池 现为直接 Instantiate/Destroy
            // 预制体自带动态刚体 先拆物理再挂点 否则不跟手
            ItemPhysicsUtil.SetHeld(weapon);

            // 获取右手武器挂点
            var socket = weaponAsyncHandPos.RightHandWeaponPos;
            if (socket == null)
            {
                Debug.LogError("右手武器挂点为空 无法装备");
                Destroy(weapon);
                return false;
            }

            // 同步武器本地变换到右手武器挂点
            var weaponTransform = weapon.transform;
            weaponTransform.SetParent(socket, false);
            weaponTransform.localPosition = handPosConfig.Position;
            weaponTransform.localRotation = handPosConfig.Rotation;
            weaponTransform.localScale = handPosConfig.Scale;

            if (weaponTransform.parent != socket)
            {
                Debug.LogError($"武器挂点失败 parent={weaponTransform.parent}", weapon);
                Destroy(weapon);
                return false;
            }

            // 更新当前手持武器实例
            currentWeaponGo = weapon;
            equippedItemTableId = itemTableId;
            equippedWeaponName = eWeaponName;
            equippedAnimationType = (EAnimationModelType)weaponRow.AnimationType;

            BindWeaponBlade(weapon);
            OnEquippedAnimationRequest?.Invoke(equippedAnimationType);
            return true;
        }

        /// <summary>
        /// 读当前武器表攻击力 空手返回 false
        /// </summary>
        public bool TryGetAttackValue(out int attackValue)
        {
            attackValue = 0;
            if (equippedItemTableId <= 0)
                return false;

            LubanTables.EnsureLoaded();
            var weaponRow = LubanTables.Tables.TbWeapon.GetOrDefault(equippedItemTableId);
            if (weaponRow == null)
                return false;

            attackValue = weaponRow.AttackValue;
            return true;
        }

        /// <summary>
        /// 扣当前手持耐久 必须由调用方传入当前攻击损耗
        /// 扣完耐久<=0 时摧毁手持
        /// </summary>
        public bool TryApplyDurabilityLoss(int loss)
        {
            if (equippedItemRtData == null)
                return false;

            equippedItemRtData.ApplyDurabilityLoss(loss);
            PublishDurabilityChanged();

            if (equippedItemRtData.CurrDurability <= 0)
                BreakEquippedWeapon();

            return true;
        }

        /// <summary>
        /// 耐久耗尽摧毁手持 清视觉与轮盘槽
        /// </summary>
        private void BreakEquippedWeapon()
        {
            string instancedItemId = equippedItemRtData != null
                ? equippedItemRtData.InstancedItemId
                : null;

            ClearWeapon();

            if (string.IsNullOrEmpty(instancedItemId))
                return;

            ItemWheelSlotStore.ClearInstance(instancedItemId);
            MmGlobalEventBus.GlobalBus.Publish(WeaponHudEvents.Broken, instancedItemId);
        }

        /// <summary>
        /// 当前手持耐久
        /// </summary>
        public bool TryGetDurability(out int curr, out int max)
        {
            curr = 0;
            max = 0;
            if (equippedItemRtData == null)
                return false;

            curr = equippedItemRtData.CurrDurability;
            max = equippedItemRtData.MaxDurability;
            return true;
        }

        /// <summary>
        /// 清空手持武器视觉 返回原表 ID
        /// </summary>
        public int ClearWeapon()
        {
            int oldId = equippedItemTableId;
            ClearCurrentWeaponVisual();
            equippedItemTableId = 0;
            equippedWeaponName = EWeaponName.None;
            equippedAnimationType = EAnimationModelType.None;
            equippedItemRtData = null;

            BindFistBlade();
            OnEquippedAnimationRequest?.Invoke(EAnimationModelType.None);
            PublishEquippedChanged();
            return oldId;
        }

        /// <summary>
        /// 发布手持变化
        /// </summary>
        private void PublishEquippedChanged()
        {
            MmGlobalEventBus.GlobalBus.Publish(
                WeaponHudEvents.EquippedChanged,
                equippedItemRtData);
            PublishDurabilityChanged();
        }

        /// <summary>
        /// 发布手持耐久
        /// </summary>
        private void PublishDurabilityChanged()
        {
            int curr = 0;
            int max = 0;
            if (equippedItemRtData != null)
            {
                curr = equippedItemRtData.CurrDurability;
                max = equippedItemRtData.MaxDurability;
            }

            MmGlobalEventBus.GlobalBus.Publish(
                WeaponHudEvents.DurabilityChanged,
                curr,
                max);
        }

        /// <summary>
        /// 重挂当前武器本地变换 切模组后调用
        /// </summary>
        public void RefreshCurrentWeaponPose()
        {
            if (currentWeaponGo == null || equippedWeaponName == EWeaponName.None)
                return;

            if (weaponAsyncHandPos == null || weaponConfig == null)
                return;

            if (!weaponConfig.TryGetHandPosConfig(equippedWeaponName, out var handPosConfig))
                return;

            var socket = weaponAsyncHandPos.RightHandWeaponPos;
            var weaponTransform = currentWeaponGo.transform;
            ItemPhysicsUtil.SetHeld(currentWeaponGo);
            weaponTransform.SetParent(socket, false);
            weaponTransform.localPosition = handPosConfig.Position;
            weaponTransform.localRotation = handPosConfig.Rotation;
            weaponTransform.localScale = handPosConfig.Scale;
        }

        /// <summary>
        /// 临时显隐当前手持武器视觉 不卸装
        /// </summary>
        public void SetEquippedWeaponVisible(bool visible)
        {
            if (currentWeaponGo == null)
                return;

            currentWeaponGo.SetActive(visible);
        }

        /// <summary>
        /// 判断指定实例是否为当前手持物
        /// </summary>
        public bool IsEquippedWeapon(GameObject weaponObject)
        {
            return currentWeaponGo == weaponObject;
        }

        /// <summary>
        /// 销毁手上实例
        /// </summary>
        private void ClearCurrentWeaponVisual()
        {
            if (currentWeaponGo == null)
                return;

            // TODO 配合装备实例对象池化
            Destroy(currentWeaponGo);
            currentWeaponGo = null;
        }

        #endregion
    }
}
