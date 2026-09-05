using System.Collections.Generic;
using cfg.craft;
using cfg.item;
using DBGameSystem;
using DownBreak.CraftingRecipeSystem;
using MieMieFrameWork.Asset;
using MmInventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    public class MakePanel : MonoBehaviour
    {
        /// <summary> 材料行 A到E </summary>
        private MakeItemNeedGroup[] needGroupList;

        /// <summary> 自身显隐 </summary>
        private CanvasGroup canvasGroup;

        /// <summary> 制作按钮 </summary>
        private Button makeButton;

        /// <summary> 取消制作按钮 </summary>
        private Button cancelButton;

        /// <summary> 剩余时间文本 </summary>
        private TextMeshProUGUI remainTimeText;

        /// <summary> 制作时间文本 </summary>
        private TextMeshProUGUI makeTimeText;

        /// <summary> 数量减少 </summary>
        private Button reduceButton;

        /// <summary> 数量增加 </summary>
        private Button addButton;

        /// <summary> 制作数量文本 </summary>
        private TextMeshProUGUI makeNumText;

        /// <summary> 列表内容根 </summary>
        private RectTransform contentRoot;

        /// <summary> 列表项模板 </summary>
        private MakeItemShowGroup showGroupTemplate;

        /// <summary> 已生成的列表项 </summary>
        private List<MakeItemShowGroup> spawnedShowGroupList;

        /// <summary> 当前分类 null为全部 </summary>
        private EItemType? currentItemType;

        /// <summary> 是否只显示已解锁配方 </summary>
        private bool showUnlockedOnly;

        /// <summary> 当前制作数量 </summary>
        private int makeCount;

        /// <summary> 当前选中的配方 </summary>
        private Recipe selectedRecipe;

        /// <summary> 是否已初始化组件 </summary>
        private bool isComponentsInitialized;

        /// <summary>
        /// 查找节点并绑按钮 只允许壳调用一次
        /// </summary>
        public void InitComponents()
        {
            if (isComponentsInitialized)
                return;

            canvasGroup = GetComponent<CanvasGroup>();
            InitNeedGroupList();
            InitOperate();
            InitShowGroupTemplate();
            InitTypeButtons();
            makeCount = 1;
            RefreshMakeNum();
            SetMakeTimeText(0);
            SetRemainTimeText(0);
            isComponentsInitialized = true;
            RefreshCraftingState();
        }

        /// <summary>
        /// 只显示已解锁配方 由壳转发 Toggle
        /// </summary>
        public void SetShowUnlockedOnly(bool onlyUnlocked)
        {
            showUnlockedOnly = onlyUnlocked;
            if (!isComponentsInitialized)
                return;
            if (canvasGroup.alpha <= 0f)
                return;
            RefreshContent(currentItemType);
        }

        /// <summary>
        /// 显示制作页
        /// </summary>
        public void Show()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            RefreshContent(currentItemType);
            RefreshCraftingState();
        }

        /// <summary>
        /// 隐藏制作页
        /// </summary>
        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        /// <summary>
        /// 按材料数量从 A 到 E 显示 其余隐藏
        /// </summary>
        public void SetNeedGroupVisibleCount(int visibleCount)
        {
            for (int i = 0; i < needGroupList.Length; i++)
                needGroupList[i].SetVisible(i < visibleCount);
        }

        /// <summary>
        /// 写入指定材料行
        /// </summary>
        public void SetNeedGroup(int index, Sprite iconSprite, string itemName, int haveCount, int needCount)
        {
            needGroupList[index].Set(iconSprite, itemName, haveCount, needCount);
        }

        /// <summary>
        /// 列表项点中 刷右侧材料行
        /// </summary>
        public void OnShowGroupClicked(MakeItemShowGroup showGroup)
        {
            selectedRecipe = showGroup.Recipe;
            makeCount = 1;
            RefreshMakeNum();
            RefreshNeedGroups();
            RefreshMakeTime();
            RefreshCraftingState();
        }

        /// <summary>
        /// 释放按钮
        /// </summary>
        public void Release()
        {
            makeButton.onClick.RemoveListener(OnMakeClicked);
            reduceButton.onClick.RemoveListener(OnReduceClicked);
            addButton.onClick.RemoveListener(OnAddClicked);
            cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        #region 材料行

        /// <summary>
        /// 按 A到E 取材料行
        /// </summary>
        private void InitNeedGroupList()
        {
            needGroupList = new MakeItemNeedGroup[5];
            needGroupList[0] = transform.Find("Right/MaekItemNeedGroupA").GetComponent<MakeItemNeedGroup>();
            needGroupList[1] = transform.Find("Right/MaekItemNeedGroupB").GetComponent<MakeItemNeedGroup>();
            needGroupList[2] = transform.Find("Right/MaekItemNeedGroupC").GetComponent<MakeItemNeedGroup>();
            needGroupList[3] = transform.Find("Right/MaekItemNeedGroupD").GetComponent<MakeItemNeedGroup>();
            needGroupList[4] = transform.Find("Right/MaekItemNeedGroupE").GetComponent<MakeItemNeedGroup>();
            SetNeedGroupVisibleCount(0);
        }

        /// <summary>
        /// 按选中配方刷材料行 A到E
        /// </summary>
        private void RefreshNeedGroups()
        {
            var materialList = selectedRecipe.Materials;
            int visibleCount = materialList.Count;
            if (visibleCount > needGroupList.Length)
                visibleCount = needGroupList.Length;

            SetNeedGroupVisibleCount(visibleCount);
            var craftingRecipe = GameHub.Get<ICraftingRecipe>();
            for (int i = 0; i < visibleCount; i++)
            {
                var material = materialList[i];
                Sprite iconSprite = null;
                string materialName = string.Empty;
                if (LubanTables.TryGetItem(material.ItemId, out var itemTableData))
                {
                    iconSprite = MmAssetMgr.LoadAsset<Sprite>(itemTableData.IconPath);
                    materialName = itemTableData.Name;
                }

                int haveCount = craftingRecipe.GetItemCountFromBody(material.ItemId);
                int needCount = material.Count * makeCount;
                SetNeedGroup(i, iconSprite, materialName, haveCount, needCount);
            }
        }

        /// <summary>
        /// 清空选中与材料行
        /// </summary>
        private void ClearSelectedRecipe()
        {
            selectedRecipe = null;
            SetNeedGroupVisibleCount(0);
            SetMakeTimeText(0);
            RefreshCraftingState();
        }

        #endregion

        #region 操作区

        /// <summary>
        /// 取操作区与数量加减
        /// </summary>
        private void InitOperate()
        {
            makeButton = transform.Find("Right/Operate/Op/MakeButton").GetComponent<Button>();
            cancelButton = transform.Find("Right/Operate/Op/CancelButton").GetComponent<Button>();
            remainTimeText = transform.Find("Right/Operate/Op/ReamTime").GetComponent<TextMeshProUGUI>();
            makeTimeText = transform.Find("Right/Operate/Op/MakeTime").GetComponent<TextMeshProUGUI>();
            reduceButton = transform.Find("Right/Operate/MkeNum/Reduce").GetComponent<Button>();
            addButton = transform.Find("Right/Operate/MkeNum/Add").GetComponent<Button>();
            makeNumText = transform.Find("Right/Operate/MkeNum/number").GetComponent<TextMeshProUGUI>();

            makeButton.onClick.AddListener(OnMakeClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
            reduceButton.onClick.AddListener(OnReduceClicked);
            addButton.onClick.AddListener(OnAddClicked);
        }

        /// <summary>
        /// 点击制作 等级不足则提示 先扣料再倒计时
        /// </summary>
        private void OnMakeClicked()
        {
            if (selectedRecipe is null)
                return;
            var craftingRecipeSystem = GameHub.Get<ICraftingRecipe>();
            int workbenchLevel = craftingRecipeSystem.CurrentWorkbenchLevel;
            if (selectedRecipe.WorkbenchLevel > workbenchLevel)
            {
                TipPanel.Push("制作等级不足");
                return;
            }
            if (!craftingRecipeSystem.TryStartCrafting(selectedRecipe.Id, workbenchLevel, makeCount))
                return;

            RefreshNeedGroups();
            RefreshCraftingState();
        }

        /// <summary>
        /// 刷新系统制作状态
        /// </summary>
        private void Update()
        {
            RefreshCraftingState();
        }

        /// <summary>
        /// 取消当前制作并返还材料
        /// </summary>
        private void OnCancelClicked()
        {
            var craftingRecipeSystem = GameHub.Get<ICraftingRecipe>();
            if (craftingRecipeSystem.TryCancelCurrentCrafting() && selectedRecipe != null)
                RefreshNeedGroups();
            RefreshCraftingState();
        }

        /// <summary>
        /// 刷新制作队列显示和操作按钮
        /// </summary>
        private void RefreshCraftingState()
        {
            var craftingRecipeSystem = GameHub.Get<ICraftingRecipe>();
            if (craftingRecipeSystem is null)
                return;

            SetRemainTimeText(craftingRecipeSystem.IsCrafting
                ? craftingRecipeSystem.CurrentCraftingRemainSeconds
                : 0);
            cancelButton.interactable = craftingRecipeSystem.IsCrafting;
            makeButton.interactable = selectedRecipe != null;
            reduceButton.interactable = selectedRecipe != null;
            addButton.interactable = selectedRecipe != null;
        }

        /// <summary>
        /// 刷新制作总时间
        /// </summary>
        private void RefreshMakeTime()
        {
            int totalTime = selectedRecipe.CraftTime * makeCount;
            SetMakeTimeText(totalTime);
        }

        /// <summary>
        /// 写入制作时间文本
        /// </summary>
        private void SetMakeTimeText(int seconds)
        {
            makeTimeText.text = $"制作时间: {seconds} s";
        }

        /// <summary>
        /// 写入剩余时间文本
        /// </summary>
        private void SetRemainTimeText(int seconds)
        {
            remainTimeText.text = $"剩余时间: {seconds} s";
        }

        /// <summary>
        /// 制作数量减一
        /// </summary>
        private void OnReduceClicked()
        {
            if (makeCount <= 1)
                return;
            makeCount--;
            RefreshMakeNum();
            if (selectedRecipe != null)
            {
                RefreshNeedGroups();
                RefreshMakeTime();
            }
        }

        /// <summary>
        /// 制作数量加一
        /// </summary>
        private void OnAddClicked()
        {
            makeCount++;
            RefreshMakeNum();
            if (selectedRecipe != null)
            {
                RefreshNeedGroups();
                RefreshMakeTime();
            }
        }

        /// <summary>
        /// 刷新制作数量文本
        /// </summary>
        private void RefreshMakeNum()
        {
            makeNumText.text = makeCount.ToString();
        }

        #endregion

        #region 分类列表

        /// <summary>
        /// 取列表模板并隐藏
        /// </summary>
        private void InitShowGroupTemplate()
        {
            contentRoot = transform.Find("Left/ItemScroview/Scroll View/Viewport/Content").GetComponent<RectTransform>();
            showGroupTemplate = contentRoot.GetComponentInChildren<MakeItemShowGroup>(true);
            showGroupTemplate.gameObject.SetActive(false);
            spawnedShowGroupList = new List<MakeItemShowGroup>();
        }

        /// <summary>
        /// 分类按钮刷列表
        /// </summary>
        private void InitTypeButtons()
        {
            var typeRoot = transform.Find("Left/Type");
            BindTypeButton(typeRoot.Find("All"));
            BindTypeButton(typeRoot.Find("Weapon"), EItemType.Weapon);
            BindTypeButton(typeRoot.Find("Equipment"), EItemType.Equipment);
            BindTypeButton(typeRoot.Find("FoodAndWater"), EItemType.FoodOrWater);
            BindTypeButton(typeRoot.Find("Medical"), EItemType.Medicine);
            BindTypeButton(typeRoot.Find("Materials"), EItemType.Material);
        }

        /// <summary>
        /// 绑定一个分类按钮 null为全部
        /// </summary>
        private void BindTypeButton(Transform buttonTf, EItemType? eItemType = null)
        {
            var eBindType = eItemType;
            buttonTf.GetComponent<Button>().onClick.AddListener(() => RefreshContent(eBindType));
        }

        /// <summary>
        /// 按分类刷新 Content null为全部
        /// </summary>
        private void RefreshContent(EItemType? eItemType)
        {
            currentItemType = eItemType;
            var craftingRecipe = GameHub.Get<ICraftingRecipe>();
            IReadOnlyList<Recipe> recipeList = eItemType.HasValue
                ? craftingRecipe.GetAvaliableRecipesByType(eItemType.Value)
                : craftingRecipe.GetAllRecipes();

            ClearSpawnedShowGroups();
            ClearSelectedRecipe();
            for (int i = 0; i < recipeList.Count; i++)
            {
                var recipe = recipeList[i];
                if (showUnlockedOnly && !craftingRecipe.IsRecipeUnlocked(recipe.Id))
                    continue;
                CreateShowGroup(recipe);
            }
            RefreshContentLayout();
        }

        /// <summary>
        /// 清掉已生成的列表项 保留模板
        /// </summary>
        private void ClearSpawnedShowGroups()
        {
            for (int i = 0; i < spawnedShowGroupList.Count; i++)
            {
                spawnedShowGroupList[i].gameObject.SetActive(false);
                Destroy(spawnedShowGroupList[i].gameObject);
            }
            spawnedShowGroupList.Clear();
        }

        /// <summary>
        /// 刷新配方列表布局
        /// </summary>
        private void RefreshContentLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        /// <summary>
        /// 克隆一个配方列表项
        /// </summary>
        private void CreateShowGroup(Recipe recipe)
        {
            Sprite iconSprite = null;
            if (LubanTables.TryGetItem(recipe.OutputItemId, out var itemTableData))
                iconSprite = MmAssetMgr.LoadAsset<Sprite>(itemTableData.IconPath);

            var showGroup = Instantiate(showGroupTemplate, contentRoot);
            showGroup.gameObject.SetActive(true);
            showGroup.Bind(this);
            showGroup.Set(iconSprite, recipe.Name, recipe);
            spawnedShowGroupList.Add(showGroup);
        }

        #endregion
    }
}
