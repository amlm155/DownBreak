using MiMieEventBus;

namespace DownBreak.CraftingRecipeSystem
{
    /// <summary>
    /// 配方系统对外事件 Key
    /// 由 CraftingRecipeSystem 发布 UI 订阅
    /// </summary>
    public static class CraftingRecipeEvents
    {
        /// <summary>
        /// 工作台请求打开制作页
        /// </summary>
        public static readonly EventKey OnWorkbenchCraftRequested =
            new EventKey("CraftingRecipe.OnWorkbenchCraftRequested");
    }
}
