using System.Collections.Generic;
using DBGameSystem;
using Interaction.Player;
using MmInventory;
using UnityEngine;

namespace DownBreak.CraftingRecipeSystem
{
    public partial class CraftingRecipeSystem
    {
        /// <summary> 排队制作任务 </summary>
        private Queue<CraftingTask> craftingQueue = new Queue<CraftingTask>();

        /// <summary> 当前制作任务 </summary>
        private CraftingTask currentCraftingTask;

        /// <summary> 当前制作剩余秒数 </summary>
        private float currentCraftingRemainTime;

        private sealed class CraftingTask
        {
            /// <summary> 配方ID </summary>
            public int RecipeId;
            /// <summary> 制作数量 </summary>
            public int CraftCount;
            /// <summary> 总时间 </summary>
            public float TotalTime;
        }

        private void Update()
        {
            if (currentCraftingTask is null)
                return;

            // 如果当前任务不为空，则减少剩余时间
            currentCraftingRemainTime -= Time.deltaTime;
            // 如果剩余时间小于等于0，则完成当前任务
            while (currentCraftingTask is not null && currentCraftingRemainTime <= 0f)
                FinishCurrentCrafting();
        }

        #region 制作队列

        /// <summary>
        /// 扣除材料并加入制作队列
        /// </summary>
        public bool TryStartCrafting(int recipeId, int workbenchLevel, int craftCount)
        {
            if (craftCount <= 0)
                return false;
            if (!TryConsumeCraftMaterials(recipeId, workbenchLevel, craftCount))
                return false;

            /// 获取配方
            var recipe = LubanTables.Tables.TbRecipe.GetOrDefault(recipeId);
            // 创建任务
            var task = new CraftingTask
            {
                RecipeId = recipeId,
                CraftCount = craftCount,
                TotalTime = recipe.CraftTime * craftCount,
            };

            // 如果当前没有制作任务，则直接开始制作
            if (currentCraftingTask is null)
                BeginCraftingTask(task);
            else
                // 如果当前有制作任务，则加入队列
                craftingQueue.Enqueue(task);
            return true;
        }

        /// <summary>
        /// 开始执行队首制作任务
        /// </summary>
        private void BeginCraftingTask(CraftingTask task)
        {
            currentCraftingTask = task;
            currentCraftingRemainTime = task.TotalTime;
        }

        /// <summary>
        /// 完成当前任务并切换下一任务
        /// </summary>
        private void FinishCurrentCrafting()
        {
            // 发放产物
            TryGiveCraftOutput(currentCraftingTask.RecipeId, currentCraftingTask.CraftCount);
            // 清空当前任务
            currentCraftingTask = null;
            currentCraftingRemainTime = 0f;
            if (craftingQueue.Count > 0)
                BeginCraftingTask(craftingQueue.Dequeue());
        }

        /// <summary>
        /// 取消当前制作并返还当前任务材料
        /// </summary>
        public bool TryCancelCurrentCrafting()
        {
            if (currentCraftingTask is null)
                return false;

            var recipe = LubanTables.Tables.TbRecipe.GetOrDefault(currentCraftingTask.RecipeId);
            if (recipe is null)
                return false;
            var bagInteract = GameHub.Get<IUIBagInteract>();
            if (bagInteract is null)
                return false;

            foreach (var material in recipe.Materials)
                bagInteract.TryGiveItem(material.ItemId, material.Count * currentCraftingTask.CraftCount);

            currentCraftingTask = null;
            currentCraftingRemainTime = 0f;
            if (craftingQueue.Count > 0)
                BeginCraftingTask(craftingQueue.Dequeue());
            return true;
        }

        public bool IsCrafting => currentCraftingTask is not null;

        public int CurrentCraftingRecipeId => currentCraftingTask?.RecipeId ?? 0;

        public int CurrentCraftingCount => currentCraftingTask?.CraftCount ?? 0;

        public int CurrentCraftingRemainSeconds => Mathf.Max(0, Mathf.CeilToInt(currentCraftingRemainTime));

        public int CurrentCraftingTotalSeconds => currentCraftingTask is null ? 0 : Mathf.CeilToInt(currentCraftingTask.TotalTime);

        public int CraftingQueueCount => craftingQueue.Count;

        #endregion
    }
}
