using DBGameSystem;
using Interaction;
using Interaction.Combat;
using MmInventory;
using TMPro;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 世界物品信息牌 聚焦可拾取物显示名称占格 聚焦搜刮容器显示名称 搜过之后带耐久
    /// </summary>
    public class WorldItemInfoView : MonoBehaviour
    {
        /// <summary> 聚焦类型 </summary>
        private enum FocusKind
        {
            None = 0,
            Item = 1,
            Scrap = 2,
            PlaceAndBreak = 3
        }

        public static WorldItemInfoView Instance { get; private set; }

        /// <summary> 名称占格文案 </summary>
        private TextMeshProUGUI nameInfoText;

        /// <summary> 显隐组 </summary>
        private CanvasGroup canvasGroup;

        /// <summary> 交互管理器 </summary>
        private IInteraction interactionManager;

        /// <summary> 观察相机 </summary>
        private Camera viewCamera;

        /// <summary> 跟随目标 </summary>
        private Transform followTarget;

        /// <summary> 当前显示来源组件 </summary>
        private Component currentSourceComponent;

        /// <summary> 当前聚焦类型 </summary>
        private FocusKind currentFocusKind;

        /// <summary> 当前显示的表 ID </summary>
        private int currentTableId;

        /// <summary> 是否正在显示 </summary>
        private bool isShowing;

        /// <summary> 移入时钉住的射线近点 </summary>
        private Vector3 anchorWorldPos;

        /// <summary> 是否已钉住近点 </summary>
        private bool hasAnchor;

        [SerializeField]
        private Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);

#region 生命周期
        /// <summary>
        /// 绑定节点并默认隐藏
        /// </summary>
        private void Awake()
        {
            Instance = this;
            nameInfoText = transform.Find("NameInfo").GetComponentInChildren<TextMeshProUGUI>(true);
            canvasGroup = GetComponent<CanvasGroup>();
            viewCamera = Camera.main;

            var canvas = GetComponent<Canvas>();
            canvas.worldCamera = viewCamera;
 
            Hide();
        }

        /// <summary>
        /// 销毁时清理单例
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 跟随聚焦目标并朝向相机
        /// </summary>
        private void LateUpdate()
        {
            RefreshFocus();
            if (!isShowing)
                return;

            RefreshVisibleText();
            UpdateFollowAndBillboard();
        }
#endregion

#region 聚焦刷新
        /// <summary>
        /// 根据交互射线聚焦刷新显隐与文案
        /// </summary>
        private void RefreshFocus()
        {
            if (interactionManager is null)
            {
                interactionManager = GameHub.Get<IInteraction>();
                if (interactionManager is null)
                    return;
            }

            var focus = interactionManager.CurrentFocus;
            // Unity 已销毁对象 C# 引用仍在 需用 == null 判断
            if (focus is Object focusObject && focusObject == null)
            {
                if (isShowing)
                    Hide();
                return;
            }

            if (focus is IItemInterface itemSource && focus is Component itemComponent)
            {
                if (itemComponent == null)
                {
                    if (isShowing)
                        Hide();
                    return;
                }

                int itemTableId = itemSource.ItemTableID;
                Transform target = itemComponent.transform;
                if (!isShowing
                    || currentFocusKind != FocusKind.Item
                    || currentTableId != itemTableId
                    || followTarget != target)
                {
                    ShowItem(itemTableId, target, itemComponent);
                }

                return;
            }

            if (focus is IScrapInterface scrapSource && focus is Component scrapComponent)
            {
                if (scrapComponent == null)
                {
                    if (isShowing)
                        Hide();
                    return;
                }

                int scrapContainerId = scrapSource.ScrapContainerId;
                Transform target = scrapComponent.transform;
                if (!isShowing
                    || currentFocusKind != FocusKind.Scrap
                    || currentTableId != scrapContainerId
                    || followTarget != target)
                {
                    ShowScrap(scrapContainerId, target, scrapComponent);
                }

                return;
            }

            if (focus is IPlaceAndBreakInterface placeSource && focus is Component placeComponent)
            {
                if (placeComponent == null)
                {
                    if (isShowing)
                        Hide();
                    return;
                }

                int placeTableId = placeSource.ItemTableId;
                Transform target = placeComponent.transform;
                if (!isShowing
                    || currentFocusKind != FocusKind.PlaceAndBreak
                    || currentTableId != placeTableId
                    || followTarget != target)
                {
                    ShowPlaceAndBreak(placeTableId, target, placeComponent);
                }

                return;
            }

            if (isShowing)
                Hide();
        }

        /// <summary>
        /// 显示物品名称与占格
        /// </summary>
        private void ShowItem(int itemTableId, Transform target, Component sourceComponent)
        {
            currentFocusKind = FocusKind.Item;
            currentTableId = itemTableId;
            followTarget = target;
            currentSourceComponent = sourceComponent;
            nameInfoText.text = BuildItemText(itemTableId, sourceComponent);
            CaptureAnchorFromHit();
            ApplyVisible();
        }

        /// <summary>
        /// 显示搜刮容器名称
        /// </summary>
        private void ShowScrap(int scrapContainerId, Transform target, Component sourceComponent)
        {
            currentFocusKind = FocusKind.Scrap;
            currentTableId = scrapContainerId;
            followTarget = target;
            currentSourceComponent = sourceComponent;
            nameInfoText.text = BuildScrapText(scrapContainerId, sourceComponent);
            CaptureAnchorFromHit();
            ApplyVisible();
        }

        /// <summary>
        /// 显示可放置物名称 储物箱带容量
        /// </summary>
        private void ShowPlaceAndBreak(int itemTableId, Transform target, Component sourceComponent)
        {
            currentFocusKind = FocusKind.PlaceAndBreak;
            currentTableId = itemTableId;
            followTarget = target;
            currentSourceComponent = sourceComponent;
            nameInfoText.text = BuildPlaceAndBreakText(itemTableId, sourceComponent);
            CaptureAnchorFromHit();
            ApplyVisible();
        }

        /// <summary>
        /// 打开信息牌可见状态
        /// </summary>
        private void ApplyVisible()
        {
            isShowing = true;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            UpdateFollowAndBillboard();
        }

        /// <summary>
        /// 显示期间只改文案 不重钉近点
        /// </summary>
        private void RefreshVisibleText()
        {
            if (currentSourceComponent == null)
            {
                Hide();
                return;
            }

            switch (currentFocusKind)
            {
                case FocusKind.Item:
                    nameInfoText.text = BuildItemText(currentTableId, currentSourceComponent);
                    break;
                case FocusKind.Scrap:
                    nameInfoText.text = BuildScrapText(currentTableId, currentSourceComponent);
                    break;
                case FocusKind.PlaceAndBreak:
                    nameInfoText.text = BuildPlaceAndBreakText(currentTableId, currentSourceComponent);
                    break;
            }
        }

        /// <summary>
        /// 物品名称占格文案
        /// </summary>
        private static string BuildItemText(int itemTableId, Component sourceComponent)
        {
            if (!LubanTables.TryGetItem(itemTableId, out var tableData))
                return string.Empty;

            Vector2Int dataSize = tableData.DataSize;
            return $"{tableData.Name} {dataSize.x}x{dataSize.y}{BuildDurabilitySuffix(sourceComponent)}";
        }

        /// <summary>
        /// 搜刮容器名称文案
        /// </summary>
        private static string BuildScrapText(int scrapContainerId, Component sourceComponent)
        {
            LubanTables.EnsureLoaded();
            var scrapContainer = LubanTables.Tables.TbScrapContainer.GetOrDefault(scrapContainerId);
            return scrapContainer is null
                ? string.Empty
                : $"{scrapContainer.Name}{BuildDurabilitySuffix(sourceComponent)}";
        }

        /// <summary>
        /// 家具只显示名称和耐久 储物箱额外显示容量
        /// </summary>
        private static string BuildPlaceAndBreakText(int itemTableId, Component sourceComponent)
        {
            LubanTables.EnsureLoaded();
            var storageBox = LubanTables.Tables.TbStorageBox.GetOrDefault(itemTableId);
            if (storageBox is not null)
                return $"{storageBox.Name} {storageBox.Capacity.X}x{storageBox.Capacity.Y}{BuildDurabilitySuffix(sourceComponent)}";

            if (LubanTables.TryGetItem(itemTableId, out var tableData))
                return $"{tableData.Name}{BuildDurabilitySuffix(sourceComponent)}";

            return string.Empty;
        }

        /// <summary>
        /// 构建耐久后缀 搜刮箱未搜过不显示 搜过之后读 DamageableBehaviour
        /// </summary>
        private static string BuildDurabilitySuffix(Component sourceComponent)
        {
            if (sourceComponent == null)
                return string.Empty;

            if (sourceComponent is IScrapInterface scrapSource && !scrapSource.IsAlreadyLooted)
                return string.Empty;

            int currentDurability = 0;
            int maxDurability = 0;
            if (!TryGetLiveDurability(sourceComponent, out currentDurability, out maxDurability)
                || maxDurability <= 0)
            {
                return string.Empty;
            }

            int percent = Mathf.RoundToInt((float)currentDurability / maxDurability * 100f);
            percent = Mathf.Clamp(percent, 0, 100);
            return $"  {percent}%";
        }

        /// <summary>
        /// 优先读 DamageableBehaviour 当前血量 没有再退回 IDurabilityProvider
        /// </summary>
        private static bool TryGetLiveDurability(Component sourceComponent,
                                                 out int currentDurability,
                                                 out int maxDurability)
        {
            currentDurability = 0;
            maxDurability = 0;

            var damageableBehaviour = sourceComponent.GetComponent<DamageableBehaviour>();
            if (damageableBehaviour == null)
                damageableBehaviour = sourceComponent.GetComponentInChildren<DamageableBehaviour>(true);
            if (damageableBehaviour == null)
                damageableBehaviour = sourceComponent.GetComponentInParent<DamageableBehaviour>();

            if (damageableBehaviour != null)
                return damageableBehaviour.TryGetDurability(out currentDurability, out maxDurability);

            global::Interaction.Combat.IDurabilityProvider durabilityProvider =
                sourceComponent.GetComponentInParent<global::Interaction.Combat.IDurabilityProvider>();
            if (durabilityProvider == null)
                return false;

            return durabilityProvider.TryGetDurability(out currentDurability, out maxDurability);
        }

        /// <summary>
        /// 隐藏信息牌
        /// </summary>
        private void Hide()
        {
            isShowing = false;
            followTarget = null;
            currentSourceComponent = null;
            currentFocusKind = FocusKind.None;
            currentTableId = 0;
            hasAnchor = false;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
#endregion

#region 位姿
        /// <summary>
        /// 移入时钉在当前射线近点 同一物体持续看着不再取样
        /// </summary>
        private void CaptureAnchorFromHit()
        {
            if (interactionManager != null && interactionManager.CurrentHitPoint.HasValue)
            {
                anchorWorldPos = interactionManager.CurrentHitPoint.Value;
                hasAnchor = true;
                return;
            }

            if (followTarget != null)
            {
                anchorWorldPos = followTarget.position;
                hasAnchor = true;
            }
        }

        /// <summary>
        /// 钉在移入近点 只转朝向贴相机 不每帧重打射线
        /// </summary>
        private void UpdateFollowAndBillboard()
        {
            if (!hasAnchor)
            {
                if (isShowing)
                    Hide();
                return;
            }

            if (viewCamera is null)
                viewCamera = Camera.main;

            if (viewCamera is null)
                return;

            transform.rotation = viewCamera.transform.rotation;
            transform.position = anchorWorldPos + worldOffset
                - viewCamera.transform.forward * 0.05f;
        }
#endregion
    }
}
