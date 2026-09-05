using System;
using cfg.item;
using DBGameSystem;
using DBWeaponSystem;
using Interaction.Player;
using MieMieFrameWork.Asset;
using MieMieFrameWork.MMAnimation;
using UnityEngine;

namespace PlayerControllerSpace
{
    /// <summary>
    /// 消耗品演出 食物 FP+本体+餐具 / 药品 FP+本体 离开 IsConsuming Tag 收尾
    /// </summary>
    public partial class PlayerAnimationController
    {
        #region 常量

        /// <summary> 餐具显隐名 </summary>
        private const string UtensilVisibleEventName = "UtensilVisible";

        /// <summary> 消耗态片段进度收尾阈值 </summary>
        private const float ConsumeFinishedNormalizedTime = 0.95f;

        /// <summary> 开演后迟迟未进入 IsConsuming 的等待秒 </summary>
        private const float ConsumeEnterTimeoutSeconds = 1.5f;

        /// <summary> 是否正在消耗品演出会话 </summary>
        private bool isEating;

        /// <summary> 本段演出是否已进入过 IsConsuming </summary>
        private bool hasEnteredConsumingState;

        /// <summary> 演出前模组 </summary>
        private EAnimationModelType eatPrevModule;

        /// <summary> 演出完成回调 </summary>
        private Action eatCompletedCallback;

        /// <summary> 等待进入消耗态已过秒数 </summary>
        private float waitEnterConsumingSeconds;

        /// <summary> 本体实例 </summary>
        private GameObject foodGo;

        /// <summary> 工具实例 </summary>
        private GameObject utensilGo;

        /// <summary> 本体 Animator </summary>
        private Animator foodAnimator;

        /// <summary> 工具 Animator </summary>
        private Animator utensilAnimator;

        /// <summary> 是否已绑演出事件 </summary>
        private bool eatEventsBound;

        #endregion

        #region 生命周期

        /// <summary> 正在消耗品演出 </summary>
        public bool IsConsuming => isEating;

        /// <summary>
        /// 绑消耗品动画事件
        /// </summary>
        private void InitEatPerformance()
        {
            BindEatAnimationEvents();
        }

        private void OnDestroy()
        {
            UnbindEatAnimationEvents();
        }

        /// <summary>
        /// 消耗态播完或离开 Tag 则收尾
        /// 超时仅兜未进入 IsConsuming 不掐正在播的长动画
        /// </summary>
        private void LateUpdateEatFallback()
        {
            if (!isEating || fpAnimator == null)
                return;

            if (IsFpInConsumingState())
            {
                hasEnteredConsumingState = true;
                waitEnterConsumingSeconds = 0f;

                if (!fpAnimator.IsInTransition(0))
                {
                    AnimatorStateInfo stateInfo = fpAnimator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.IsTag(ConsumingTag)
                        && stateInfo.normalizedTime >= ConsumeFinishedNormalizedTime)
                        FinishEat();
                }

                return;
            }

            if (hasEnteredConsumingState)
            {
                FinishEat();
                return;
            }

            waitEnterConsumingSeconds += Time.deltaTime;
            if (waitEnterConsumingSeconds >= ConsumeEnterTimeoutSeconds)
            {
                Debug.LogWarning("消耗品演出未进入 IsConsuming 强制收尾");
                FinishEat();
            }
        }

        #endregion

        #region 对外入口

        /// <summary>
        /// 播放进食 FP + 食物本体 FoodAm + 可选餐具
        /// </summary>
        public bool TryPlayEat(FoodOrWater foodTable, Action onCompleted)
        {
            if (foodTable == null)
                return false;

            return TryStartConsume(
                EAnimationModelType.FoodOrWater,
                foodTable.EatAnimName,
                foodTable.WorldPrefabPath,
                foodTable.UtensilPrefabPath,
                foodTable.Id,
                PlayerAmHashMap.FoodAm,
                onCompleted);
        }

        /// <summary>
        /// 播放用药 FP + 医疗品本体 MedicineAm 无餐具
        /// </summary>
        public bool TryPlayMedicine(Medicine medicineTable, Action onCompleted)
        {
            if (medicineTable == null)
                return false;

            return TryStartConsume(
                EAnimationModelType.Medicine,
                medicineTable.UseAnimName,
                medicineTable.WorldPrefabPath,
                null,
                medicineTable.Id,
                PlayerAmHashMap.MedicineAm,
                onCompleted);
        }

        #endregion

        #region 演出核心

        /// <summary>
        /// 共用消耗品演出入口
        /// </summary>
        /// <param name="moduleType">模组类型</param>
        /// <param name="animName">动画名称</param>
        /// <param name="worldPrefabPath">本体预制体路径</param>
        /// <param name="utensilPrefabPath">餐具预制体路径</param>
        /// <param name="itemTableId">物品表ID</param>
        /// <param name="bodyAnimHash">本体动画哈希</param>
        /// <param name="onCompleted">完成回调</param>
        private bool TryStartConsume(
            EAnimationModelType moduleType,
            string animName,
            string worldPrefabPath,
            string utensilPrefabPath,
            int itemTableId,
            int bodyAnimHash,
            Action onCompleted)
        {
            if (isEating || IsFpInConsumingState() || fpAnimator == null || animationAssets == null)
                return false;

            if (!animationAssets.AnimationControllerDict.ContainsKey(moduleType)
                || animationAssets.AnimationControllerDict[moduleType] == null)
            {
                Debug.LogWarning($"未配置 {moduleType} 动画控制器 直接完成回调");
                onCompleted?.Invoke();
                return true;
            }

            isEating = true;
            hasEnteredConsumingState = false;
            eatCompletedCallback = onCompleted;
            eatPrevModule = ResolveCurrentEatModule();
            waitEnterConsumingSeconds = 0f;

            // 切换 FP Controller的动画模组
            SetFpController(moduleType);
            // 临时显隐手持武器
            SetEatHeldWeaponVisible(false);

            // 解析消耗品手部挂点
            ResolveConsumeHandPos(
                moduleType,
                itemTableId,
                out WeaponHandPosConfig bodyConfig,
                out WeaponHandPosConfig utensilConfig,
                out EMedicineHandSide medicineHandSide);

            // 生成本体与可选餐具并套挂点偏移
            if (!SpawnEatProps(
                worldPrefabPath,
                utensilPrefabPath,
                bodyConfig,
                utensilConfig,
                medicineHandSide))
            {
                // 生成失败时回滚 不回调
                AbortEatWithoutConsume();
                return false;
            }

            // 开演播本体动画 FoodAm 或 MedicineAm
            PlayConsumeBodyAm(bodyAnimHash);

            // 播放消耗品动画
            if (string.IsNullOrEmpty(animName))
            {
                Debug.LogWarning($"物品 {itemTableId} 未配置演出动画名 直接收尾");
                FinishEat();
                return true;
            }

            // 播动画 表字段与 FP Animator 状态名一致 直接播放
            fpAnimator.Play(Animator.StringToHash(animName), 0, 0f);
            return true;
        }

        /// <summary>
        /// 按模组解析挂点 食物走 FoodOrWater 药品走 Medicine
        /// </summary>
        private static void ResolveConsumeHandPos(
            EAnimationModelType moduleType,
            int itemTableId,
            out WeaponHandPosConfig bodyConfig,
            out WeaponHandPosConfig utensilConfig,
            out EMedicineHandSide medicineHandSide)
        {
            bodyConfig = null;
            utensilConfig = null;
            medicineHandSide = EMedicineHandSide.Left;
            if (GameHub.Get<IWeaponSystem>() == null || GameHub.Get<IWeaponSystem>().WeaponConfig == null)
                return;

            var weaponConfig = GameHub.Get<IWeaponSystem>().WeaponConfig;
            if (moduleType == EAnimationModelType.Medicine)
            {
                if (weaponConfig.TryGetMedicinePosConfig(itemTableId, out var medicineEntry))
                {
                    bodyConfig = medicineEntry.BodyConfig;
                    medicineHandSide = medicineEntry.HandSide;
                }

                return;
            }

            if (weaponConfig.TryGetFoodOrWaterPosConfig(itemTableId, out var foodEntry))
            {
                bodyConfig = foodEntry.FoodConfig;
                utensilConfig = foodEntry.UtensilConfig;
            }
        }

        #endregion

        #region 道具与挂点

        /// <summary>
        /// 生成本体与可选餐具并套挂点偏移
        /// </summary>
        private bool SpawnEatProps(
            string worldPrefabPath,
            string utensilPrefabPath,
            WeaponHandPosConfig bodyConfig,
            WeaponHandPosConfig utensilConfig,
            EMedicineHandSide medicineHandSide)
        {
            CleanupEatProps();

            var handPos = ResolveWeaponHandPos();
            bool needUtensil = !string.IsNullOrEmpty(utensilPrefabPath);
            bool bodyOnRight = !needUtensil && medicineHandSide == EMedicineHandSide.Right;

            if (handPos == null)
            {
                Debug.LogWarning("消耗品演出失败 缺少 WeaponAsyncHandPos");
                return false;
            }

            Transform bodyParent = bodyOnRight
                ? handPos.RightHandWeaponPos
                : handPos.LeftHandWeaponPos;
            if (bodyParent == null)
            {
                Debug.LogWarning(bodyOnRight
                    ? "消耗品演出失败 缺少右手武器挂点"
                    : "消耗品演出失败 缺少左手武器挂点");
                return false;
            }

            if (needUtensil && handPos.RightHandWeaponPos == null)
            {
                Debug.LogWarning("消耗品演出失败 缺少右手武器挂点");
                return false;
            }

            if (!string.IsNullOrEmpty(worldPrefabPath))
            {
                foodGo = MmAssetMgr.Instantiate(worldPrefabPath, bodyParent);
                ApplyPropLocalPose(foodGo, bodyConfig);
                if (foodGo != null)
                    foodAnimator = foodGo.GetComponentInChildren<Animator>(true);
            }

            if (needUtensil)
            {
                utensilGo = MmAssetMgr.Instantiate(
                    utensilPrefabPath, handPos.RightHandWeaponPos);
                ApplyPropLocalPose(utensilGo, utensilConfig);
                if (utensilGo != null)
                {
                    utensilAnimator = utensilGo.GetComponentInChildren<Animator>(true);
                    utensilGo.SetActive(false);
                }
            }

            return true;
        }

        /// <summary>
        /// 开演播本体动画 FoodAm 或 MedicineAm
        /// </summary>
        private void PlayConsumeBodyAm(int bodyAnimHash)
        {
            if (foodAnimator == null)
                return;

            foodAnimator.Play(bodyAnimHash, 0, 0f);
        }

        /// <summary>
        /// 工具显隐 true 时播 Utensil
        /// </summary>
        private void OnUtensilVisible(bool visible)
        {
            if (utensilGo == null)
                return;

            utensilGo.SetActive(visible);
            if (!visible || utensilAnimator == null)
                return;

            utensilAnimator.Play(PlayerAmHashMap.Utensil, 0, 0f);
        }

        /// <summary>
        /// 取武器左右手挂点组件
        /// </summary>
        private WeaponAsyncHandPos ResolveWeaponHandPos()
        {
            if (GameHub.Get<IWeaponSystem>() != null)
            {
                var handPos = (GameHub.Get<IWeaponSystem>() as UnityEngine.MonoBehaviour).GetComponent<WeaponAsyncHandPos>();
                if (handPos != null)
                    return handPos;
            }

            return GetComponentInParent<WeaponAsyncHandPos>();
        }

        /// <summary>
        /// 套本地偏移 无配置则归零
        /// </summary>
        private static void ApplyPropLocalPose(GameObject go, WeaponHandPosConfig config)
        {
            if (go == null)
                return;

            var t = go.transform;
            if (config == null)
            {
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                return;
            }

            t.localPosition = config.Position;
            t.localRotation = config.Rotation;
            t.localScale = config.Scale;
        }

        /// <summary>
        /// 销毁临时道具
        /// </summary>
        private void CleanupEatProps()
        {
            foodAnimator = null;
            utensilAnimator = null;

            if (foodGo != null)
            {
                MmAssetMgr.DestroyObject(foodGo);
                foodGo = null;
            }

            if (utensilGo != null)
            {
                MmAssetMgr.DestroyObject(utensilGo);
                utensilGo = null;
            }
        }

        #endregion

        #region 收尾与回滚

        /// <summary>
        /// 演出结束 清道具切回模组 再回调
        /// </summary>
        private void FinishEat()
        {
            if (!isEating)
                return;

            var callback = eatCompletedCallback;
            eatCompletedCallback = null;
            waitEnterConsumingSeconds = 0f;
            hasEnteredConsumingState = false;

            CleanupEatProps();
            RestoreEatModule();
            isEating = false;

            callback?.Invoke();
        }

        /// <summary>
        /// 生成失败时回滚 不回调
        /// </summary>
        private void AbortEatWithoutConsume()
        {
            eatCompletedCallback = null;
            waitEnterConsumingSeconds = 0f;
            hasEnteredConsumingState = false;
            CleanupEatProps();
            RestoreEatModule();
            isEating = false;
        }

        /// <summary>
        /// 临时显隐手持武器
        /// </summary>
        private static void SetEatHeldWeaponVisible(bool visible)
        {
            if (GameHub.Get<IWeaponSystem>() == null)
                return;

            GameHub.Get<IWeaponSystem>().SetEquippedWeaponVisible(visible);
        }

        /// <summary>
        /// 切回演出前模组 先挂武器再取出
        /// 同帧 Update 压闪帧
        /// </summary>
        private void RestoreEatModule()
        {
            SetFpController(eatPrevModule);
            SetEatHeldWeaponVisible(true);
            if (GameHub.Get<IWeaponSystem>() != null)
                GameHub.Get<IWeaponSystem>().RefreshCurrentWeaponPose();

            if (fpAnimator == null)
                return;

            fpAnimator.Play(PlayerAmHashMap.取出, 0, 0f);
            fpAnimator.Update(0f);
        }

        /// <summary>
        /// 解析当前模组
        /// </summary>
        private EAnimationModelType ResolveCurrentEatModule()
        {
            if (IsFpController(EAnimationModelType.Knife))
                return EAnimationModelType.Knife;
            if (IsFpController(EAnimationModelType.SingleHandWeapon))
                return EAnimationModelType.SingleHandWeapon;
            if (IsFpController(EAnimationModelType.DoubleHandWeapon))
                return EAnimationModelType.DoubleHandWeapon;
            if (IsFpController(EAnimationModelType.Lantern))
                return EAnimationModelType.Lantern;
            if (IsFpController(EAnimationModelType.FoodOrWater))
                return EAnimationModelType.None;
            if (IsFpController(EAnimationModelType.Medicine))
                return EAnimationModelType.None;
            return EAnimationModelType.None;
        }

        #endregion

        #region 动画事件

        /// <summary>
        /// 绑定消耗品动画事件 仅餐具显隐
        /// </summary>
        private void BindEatAnimationEvents()
        {
            if (eatEventsBound || fpAnimator == null)
                return;

            var receiver = fpAnimator.GetComponent<AnimationReceiver>();
            if (receiver == null)
            {
                Debug.LogWarning("FP Animator 缺少 AnimationReceiver 无法绑消耗品事件");
                return;
            }

            if (!receiver.AnimationEventList.Contains(UtensilVisibleEventName))
                receiver.AddAnimationEvent<bool>(UtensilVisibleEventName, OnUtensilVisible);

            eatEventsBound = true;
        }

        /// <summary>
        /// 解绑消耗品动画事件
        /// </summary>
        private void UnbindEatAnimationEvents()
        {
            if (!eatEventsBound || fpAnimator == null)
                return;

            var receiver = fpAnimator.GetComponent<AnimationReceiver>();
            if (receiver == null)
                return;

            if (receiver.AnimationEventList.Contains(UtensilVisibleEventName))
                receiver.RemoveAnimationEvent<bool>(UtensilVisibleEventName, OnUtensilVisible);

            eatEventsBound = false;
        }

        #endregion
    }
}
