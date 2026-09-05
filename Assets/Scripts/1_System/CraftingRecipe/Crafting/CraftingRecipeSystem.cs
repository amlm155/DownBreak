using System.Collections.Generic;
using cfg.item;
using DBGameSystem;
using Interaction.Player;
using MieMieFrameWork;
using MmInventory;
using UnityEngine;
namespace DownBreak.CraftingRecipeSystem
{
    public partial class CraftingRecipeSystem : MonoBehaviour, ICraftingRecipe
    {
        /// <summary> 已解锁的配方ID列表 </summary>
        private HashSet<int> unlockedRecipeList = new HashSet<int>();

        /// <summary> 当前制作台等级 0为空手 </summary>
        private int currentWorkbenchLevel;

        void Start()
        {
            // 解锁开局楼层的配方
            InitRecipe(10);
        }

        /// <summary>
        /// 初始化楼层
        /// </summary>
        public void InitRecipe(int floorLevel)
        {
            // 获取总表
            var recipeList = LubanTables.Tables.TbRecipe.DataList;

            foreach (var recipe in recipeList)
            {
                // 如果是那种不需要蓝图解锁的配方
                if (recipe.UnlockBlueprintId != 0)
                    continue;

                // 如果配方解锁楼层大于当前楼层
                if (recipe.UnlockFloor > floorLevel)
                    continue;

                // 尝试解锁配方
                TryUnlockRecipe(recipe.Id);
            }
        }
        #region 配方解锁
        /// <summary>
        /// 尝试直接使用配方id解锁配方
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        public bool TryUnlockRecipe(int recipeId)
        {
            // 校验配方是否存在
            var recipe = LubanTables.Tables.TbRecipe.GetOrDefault(recipeId);
            if (recipe is null)
            {
                Debug.LogError($"配方不存在: {recipeId}");
                return false;
            }

            // 解锁配方
            return unlockedRecipeList.Add(recipeId);
        }

        /// <summary>
        /// 使用蓝图解锁配方
        /// </summary>
        /// <param name="blueprintId">蓝图ID</param>
        /// <returns></returns>
        public bool TryUnlockByBlueprint(int blueprintId)
        {
            // 获取蓝图物品数据
            var blueprint = LubanTables.Tables.TbBlueprint.GetOrDefault(blueprintId);
            if (blueprint is null)
            {
                Debug.LogError($"蓝图不存在: {blueprintId}");
                return false;
            }
            // 如果蓝图物品有配方关联 则尝试解锁配方
            int recipeId = blueprint.LinkedRecipeId;
            if (recipeId == 0)
            {
                Debug.LogError($"蓝图物品没有配方关联: {blueprintId}");
                return false;
            }
            // 使用recipeId去配方表反查配方
            var recipe = LubanTables.Tables.TbRecipe.GetOrDefault(recipeId);
            if (recipe is null)
            {
                Debug.LogError($"配方不存在: {recipeId}");
                return false;
            }
            // 尝试解锁配方
            return TryUnlockRecipe(recipeId);
        }
        #endregion

        #region 制作分解

        /// <summary>
        /// 制作物品
        /// </summary>
        public bool CraftingItem(int recipeId, int workbenchLevel, int craftCount)
        {
            return TryStartCrafting(recipeId, workbenchLevel, craftCount);
        }

        /// <summary>
        /// 开始制作时扣除材料
        /// </summary>
        public bool TryConsumeCraftMaterials(int recipeId, int workbenchLevel, int craftCount)
        {
            var recipe = LubanTables.Tables.TbRecipe.GetOrDefault(recipeId);
            if (recipe is null)
            {
                Debug.LogError($"配方不存在: {recipeId}");
                return false;
            }
            if (!IsRecipeUnlocked(recipe.Id))
            {
                Debug.LogWarning($"配方未解锁: {recipe.Id}");
                return false;
            }
            if (recipe.WorkbenchLevel > workbenchLevel)
            {
                Debug.LogWarning($"工作台等级不足: {recipe.WorkbenchLevel}");
                return false;
            }
            return TryConsumeFromBodyContainer(recipe.Materials, craftCount);
        }

        /// <summary>
        /// 倒计时结束发放产物
        /// </summary>
        public bool TryGiveCraftOutput(int recipeId, int craftCount)
        {
            var recipe = LubanTables.Tables.TbRecipe.GetOrDefault(recipeId);
            var bagInteracr = GameHub.Get<IUIBagInteract>();
            if (bagInteracr is null)
                return false;
            return bagInteracr.TryGiveItem(recipe.OutputItemId, recipe.OutputCount * craftCount);
        }


        /// <summary>
        /// 从身上口袋扣材料 先数够不够再扣
        /// </summary>
        /// <param name="materialList">材料列表</param>
        /// <returns></returns>
        private bool TryConsumeFromBodyContainer(IReadOnlyList<cfg.craft.CraftMaterial> materialList, int craftCount)
        {
            foreach (var material in materialList)
            {
                int needCount = material.Count * craftCount;
                if (!HasEnoughFromBody(material.ItemId, needCount))
                    return false;
            }
            foreach (var material in materialList)
                ConsumeFromBody(material.ItemId, material.Count * craftCount);
            return true;
        }

        /// <summary>
        /// 身上口袋是否够扣指定数量
        /// </summary>
        private bool HasEnoughFromBody(int itemId, int needCount)
        {
            return GetItemCountFromBody(itemId) >= needCount;
        }

        /// <summary>
        /// 从身上口袋扣除某材料 堆叠扣到 0 则销毁格子
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="consumeCount">消耗数量</param>
        private void ConsumeFromBody(int itemId, int consumeCount)
        {
            // 剩余需要消耗的物品数量
            int remainCount = consumeCount;
            // 遍历所有容器
            var playerContainerList = GridMainContainerManager.PlayerContainerList;
            foreach (var container in playerContainerList)
            {
                var itemViewList = container.GetItemViewList();
                foreach (var itemView in itemViewList)
                {
                    var itemRtData = itemView.ItemData;
                    if (itemRtData is null || itemRtData.ExcelItemId != itemId)
                        continue;

                    // 计算可消耗数量 剩余数量和物品当前数量取最小值
                    int takeCount = Mathf.Min(remainCount, itemRtData.CurrStackCount);
                    // 计算剩余数量
                    int nextCount = itemRtData.CurrStackCount - takeCount;
                    if (nextCount <= 0)
                        container.DestroyItemUI(itemView);
                    else
                    {
                        itemRtData.CurrStackCount = nextCount;
                        itemView.RefreshStackView();
                    }

                    // 还要扣多少数量
                    remainCount -= takeCount;
                    if (remainCount <= 0)
                        return;
                }
            }
        }


        public void DismantleItem(int itemId)
        {
            // TODO:目前没有分解物品表
        }

        #endregion

        #region 工作台上下文

        public int CurrentWorkbenchLevel => currentWorkbenchLevel;

        /// <summary>
        /// 进入指定等级工作台并请求打开制作页
        /// </summary>
        public void EnterWorkbench(int workbenchLevel)
        {
            currentWorkbenchLevel = workbenchLevel;
            MmGlobalEventBus.GlobalBus.Publish(CraftingRecipeEvents.OnWorkbenchCraftRequested);
        }

        /// <summary>
        /// 离开工作台 回到空手
        /// </summary>
        public void ExitWorkbench()
        {
            currentWorkbenchLevel = 0;
        }

        #endregion

        #region 查询
        /// <summary>
        /// 获取所有配方
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<cfg.craft.Recipe> GetAllRecipes()
        {
            return LubanTables.Tables.TbRecipe.DataList;
        }

        /// <summary>
        /// 按照分类获取可用配方
        /// </summary>
        /// <param name="eItemType">配方分类</param>
        /// <returns></returns>
        public List<cfg.craft.Recipe> GetAvaliableRecipesByType(EItemType eItemType)
        {
            var recipeList = GetAllRecipes();
            var typedRecipeList = new List<cfg.craft.Recipe>();
            foreach (var recipe in recipeList)
            {
                if (recipe.ItemType != eItemType)
                    continue;
                typedRecipeList.Add(recipe);
            }
            return typedRecipeList;
        }

        /// <summary>
        /// 查询配方是否已经解锁
        /// </summary>
        /// <param name="recipeId"></param>
        /// <returns></returns>
        public bool IsRecipeUnlocked(int recipeId)
        {
            return unlockedRecipeList.Contains(recipeId);
        }

        /// <summary>
        /// 身上口袋持有某物品的数量
        /// </summary>
        public int GetItemCountFromBody(int itemId)
        {
            int haveCount = 0;
            var playerContainerList = GridMainContainerManager.PlayerContainerList;
            foreach (var container in playerContainerList)
            {
                var itemViewList = container.GetItemViewList();
                foreach (var itemView in itemViewList)
                {
                    var itemRtData = itemView.ItemData;
                    if (itemRtData is null || itemRtData.ExcelItemId != itemId)
                        continue;
                    haveCount += itemRtData.CurrStackCount;
                }
            }
            return haveCount;
        }

        /// <summary>
        /// 根据工作台等级获取可用的配方
        /// 自动向下覆盖
        /// </summary>
        /// <param name="workbenchLevel"></param>
        /// <returns></returns>
        public List<cfg.craft.Recipe> GetAvaliableRecipes(int workbenchLevel)
        {
            var recipeList = GetAllRecipes();
            var avaliableRecipeList = new List<cfg.craft.Recipe>();
            foreach (var recipe in recipeList)
            {
                // 如果是未解锁的   
                if (!IsRecipeUnlocked(recipe.Id))
                    continue;
                // 如果工作台等级小于配方所需工作台等级
                if (recipe.WorkbenchLevel > workbenchLevel)
                    continue;
                // 添加到可用配方列表
                avaliableRecipeList.Add(recipe);
            }
            return avaliableRecipeList;
        }

        /// <summary>
        /// 按工作台等级与配方分类获取可用配方
        /// 自动向下覆盖
        /// </summary>
        /// <param name="workbenchLevel">工作台等级</param>
        /// <param name="eItemType">配方分类</param>
        /// <returns></returns>
        public List<cfg.craft.Recipe> GetAvaliableRecipes(int workbenchLevel, EItemType eItemType)
        {
            var recipeList = GetAvaliableRecipes(workbenchLevel);
            var typedRecipeList = new List<cfg.craft.Recipe>();
            foreach (var recipe in recipeList)
            {
                if (recipe.ItemType != eItemType)
                    continue;
                typedRecipeList.Add(recipe);
            }
            return typedRecipeList;
        }

        #endregion
    }
}
