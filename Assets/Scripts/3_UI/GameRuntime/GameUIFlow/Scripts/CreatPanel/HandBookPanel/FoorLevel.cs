using System.Collections.Generic;
using cfg.craft;
using DBGameSystem;
using DownBreak.CraftingRecipeSystem;
using MieMieFrameWork.Asset;
using MmInventory;
using TMPro;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    public class FoorLevel : MonoBehaviour
    {
        /// <summary> 楼层文本 </summary>
        private TextMeshProUGUI levelText;

        /// <summary> 配方列表根 </summary>
        private Transform itemListRoot;

        /// <summary> 配方项模板 </summary>
        private RecipeItem recipeItemTemplate;

        /// <summary> 悬停信息菜单 </summary>
        private HandBookItemInfoMenu infoMenu;

        /// <summary> 已生成的配方项 </summary>
        private List<RecipeItem> spawnedRecipeItemList;

        /// <summary> 是否已初始化组件 </summary>
        private bool isComponentsInitialized;

        /// <summary>
        /// 查找楼层字和配方列表
        /// </summary>
        public void InitComponents()
        {
            if (isComponentsInitialized)
                return;

            levelText = transform.Find("Level").GetComponent<TextMeshProUGUI>();
            itemListRoot = transform.Find("ItemList");
            recipeItemTemplate = itemListRoot.GetComponentInChildren<RecipeItem>(true);
            recipeItemTemplate.InitComponents();
            recipeItemTemplate.gameObject.SetActive(false);
            spawnedRecipeItemList = new List<RecipeItem>();
            isComponentsInitialized = true;
        }

        /// <summary>
        /// 挂信息菜单 克隆项时下发
        /// </summary>
        public void BindInfoMenu(HandBookItemInfoMenu menu)
        {
            infoMenu = menu;
        }

        /// <summary>
        /// 填写楼层
        /// </summary>
        public void SetFloor(int floor)
        {
            levelText.text = $"Level {floor}";
        }

        /// <summary>
        /// 刷新本层配方项
        /// </summary>
        public void RefreshRecipeList(IReadOnlyList<Recipe> recipeList, ICraftingRecipe craftingRecipe)
        {
            for (int i = 0; i < spawnedRecipeItemList.Count; i++)
                Destroy(spawnedRecipeItemList[i].gameObject);
            spawnedRecipeItemList.Clear();

            for (int i = 0; i < recipeList.Count; i++)
            {
                Recipe recipe = recipeList[i];
                Sprite iconSprite = null;
                string itemName = recipe.Name;
                if (LubanTables.TryGetItem(recipe.OutputItemId, out var itemTableData))
                {
                    iconSprite = MmAssetMgr.LoadAsset<Sprite>(itemTableData.IconPath);
                    if (string.IsNullOrEmpty(itemName))
                        itemName = itemTableData.Name;
                }

                RecipeItem recipeItem = CreateRecipeItem(
                    iconSprite,
                    itemName,
                    craftingRecipe.IsRecipeUnlocked(recipe.Id));
                spawnedRecipeItemList.Add(recipeItem);
            }
        }

        /// <summary>
        /// 克隆一个配方项
        /// </summary>
        private RecipeItem CreateRecipeItem(Sprite iconSprite, string itemName, bool isUnlocked)
        {
            var recipeItem = Instantiate(recipeItemTemplate, itemListRoot);
            recipeItem.gameObject.SetActive(true);
            recipeItem.InitComponents();
            recipeItem.BindInfoMenu(infoMenu);
            recipeItem.Set(iconSprite, itemName, isUnlocked);
            return recipeItem;
        }
    }
}
