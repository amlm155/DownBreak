using System.Collections.Generic;
using cfg.craft;
using DBGameSystem;
using DownBreak.CraftingRecipeSystem;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    public class HandBookPanel : MonoBehaviour
    {
        /// <summary> 自身显隐 </summary>
        private CanvasGroup canvasGroup;

        /// <summary> 滚动内容根 </summary>
        private Transform contentRoot;

        /// <summary> 楼层行模板 </summary>
        private FoorLevel foorLevelTemplate;

        /// <summary> 悬停信息菜单 </summary>
        private HandBookItemInfoMenu itemInfoMenu;

        /// <summary> 已生成的楼层行 </summary>
        private List<FoorLevel> spawnedFoorLevelList;

        /// <summary> 是否已初始化组件 </summary>
        private bool isComponentsInitialized;

        /// <summary>
        /// 查找节点 只允许壳调用一次
        /// </summary>
        public void InitComponents()
        {
            if (isComponentsInitialized)
                return;

            canvasGroup = GetComponent<CanvasGroup>();
            contentRoot = transform.Find("Scroll View/Viewport/Content");
            foorLevelTemplate = contentRoot.Find("FoorLevel").GetComponent<FoorLevel>();
            foorLevelTemplate.InitComponents();
            foorLevelTemplate.gameObject.SetActive(false);
            itemInfoMenu = GetComponentInChildren<HandBookItemInfoMenu>(true);
            itemInfoMenu.InitComponents();
            foorLevelTemplate.BindInfoMenu(itemInfoMenu);
            spawnedFoorLevelList = new List<FoorLevel>();
            isComponentsInitialized = true;
        }

        /// <summary>
        /// 显示图鉴页
        /// </summary>
        public void Show()
        {
            RefreshFloorList();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        /// <summary>
        /// 隐藏图鉴页
        /// </summary>
        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        /// <summary>
        /// 按楼层刷新列表
        /// </summary>
        public void RefreshFloorList()
        {
            var craftingRecipe = GameHub.Get<ICraftingRecipe>();
            if (craftingRecipe is null)
                return;

            for (int i = 0; i < spawnedFoorLevelList.Count; i++)
                Destroy(spawnedFoorLevelList[i].gameObject);
            spawnedFoorLevelList.Clear();

            var recipeFloorDict = new SortedDictionary<int, List<Recipe>>();
            IReadOnlyList<Recipe> recipeList = craftingRecipe.GetAllRecipes();
            for (int i = 0; i < recipeList.Count; i++)
            {
                Recipe recipe = recipeList[i];
                if (!recipeFloorDict.TryGetValue(recipe.UnlockFloor, out var floorRecipeList))
                {
                    floorRecipeList = new List<Recipe>();
                    recipeFloorDict.Add(recipe.UnlockFloor, floorRecipeList);
                }

                floorRecipeList.Add(recipe);
            }

            foreach (var floorRecipes in recipeFloorDict)
            {
                FoorLevel foorLevel = CreateFoorLevel(floorRecipes.Key);
                foorLevel.RefreshRecipeList(floorRecipes.Value, craftingRecipe);
                spawnedFoorLevelList.Add(foorLevel);
            }
        }

        /// <summary>
        /// 克隆一条楼层行
        /// </summary>
        private FoorLevel CreateFoorLevel(int floor)
        {
            var foorLevel = Instantiate(foorLevelTemplate, contentRoot);
            foorLevel.gameObject.SetActive(true);
            foorLevel.InitComponents();
            foorLevel.BindInfoMenu(itemInfoMenu);
            foorLevel.SetFloor(floor);
            return foorLevel;
        }
    }
}
