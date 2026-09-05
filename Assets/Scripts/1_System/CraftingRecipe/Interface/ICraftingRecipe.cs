using System.Collections.Generic;
using cfg.craft;
using cfg.item;
using DBGameSystem;

namespace DownBreak.CraftingRecipeSystem
{
    /// <summary>
    /// 合成配方接口 用于沟通制作界面/工作台与配方系统 方法实现在 CraftingRecipeSystem 类里
    /// </summary>
    public interface ICraftingRecipe : IGameService
    {
        /// <summary>
        /// 获取所有配方
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<Recipe> GetAllRecipes();

        /// <summary>
        /// 按照分类获取可用配方
        /// </summary>
        /// <param name="eItemType">配方分类</param>
        /// <returns></returns>
        List<Recipe> GetAvaliableRecipesByType(EItemType eItemType);
        
        /// <summary>
        /// 按工作台等级与配方分类获取可用配方
        /// </summary>
        /// <param name="workbenchLevel">工作台等级</param>
        /// <param name="eItemType">配方分类</param>
        /// <returns></returns>
        List<Recipe> GetAvaliableRecipes(int workbenchLevel, EItemType eItemType);
        /// <summary>
        /// 配方是否解锁
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <returns></returns>
        bool IsRecipeUnlocked(int recipeId);
        /// <summary>
        /// 身上口袋持有某物品的数量
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns></returns>
        int GetItemCountFromBody(int itemId);
        /// <summary>
        /// 初始化配方
        /// </summary>
        /// <param name="floorLevel">楼层等级</param>
        void InitRecipe(int floorLevel);
        /// <summary>
        /// 使用蓝图解锁配方
        /// </summary>
        /// <param name="blueprintId">蓝图ID</param>
        /// <returns></returns>
        bool TryUnlockByBlueprint(int blueprintId);
        /// <summary>
        /// 制作物品
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <param name="workbenchLevel">工作台等级</param>
        /// <param name="craftCount">制作数量</param>
        /// <returns></returns>
        bool CraftingItem(int recipeId, int workbenchLevel, int craftCount);
        /// <summary>
        /// 开始制作时扣除材料
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <param name="workbenchLevel">工作台等级</param>
        /// <param name="craftCount">制作数量</param>
        /// <returns></returns>
        bool TryConsumeCraftMaterials(int recipeId, int workbenchLevel, int craftCount);
        /// <summary>
        /// 倒计时结束发放产物
        /// </summary>
        /// <param name="recipeId">配方ID</param>
        /// <param name="craftCount">制作数量</param>
        /// <returns></returns>
        bool TryGiveCraftOutput(int recipeId, int craftCount);

        /// <summary>
        /// 加入制作队列
        /// </summary>
        bool TryStartCrafting(int recipeId, int workbenchLevel, int craftCount);

        /// <summary> 当前是否存在制作任务 </summary>
        bool IsCrafting { get; }

        /// <summary> 当前制作配方ID </summary>
        int CurrentCraftingRecipeId { get; }

        /// <summary> 当前制作数量 </summary>
        int CurrentCraftingCount { get; }

        /// <summary> 当前制作剩余秒数 </summary>
        int CurrentCraftingRemainSeconds { get; }

        /// <summary> 当前制作总秒数 </summary>
        int CurrentCraftingTotalSeconds { get; }

        /// <summary> 后续排队任务数量 </summary>
        int CraftingQueueCount { get; }

        /// <summary>
        /// 取消当前制作并返还材料
        /// </summary>
        bool TryCancelCurrentCrafting();
        /// <summary>
        /// 分解物品
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns></returns>
        void DismantleItem(int itemId);

        /// <summary> 当前制作台等级 0为空手 </summary>
        int CurrentWorkbenchLevel { get; }

        /// <summary>
        /// 进入指定等级工作台并请求打开制作页
        /// </summary>
        /// <param name="workbenchLevel">工作台等级</param>
        void EnterWorkbench(int workbenchLevel);

        /// <summary>
        /// 离开工作台 回到空手
        /// </summary>
        void ExitWorkbench();
    }
}
