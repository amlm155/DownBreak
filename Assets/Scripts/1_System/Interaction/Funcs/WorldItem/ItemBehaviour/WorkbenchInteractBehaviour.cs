using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 场景工作台交互 挂在世界工作台上 当前制作上下文在 CraftingRecipeSystem
    /// </summary>
    [RequireComponent(typeof(InteractOutline))]
    public class WorkbenchInteractBehaviour : PlaceAndBreakInteractBehaviour, IWorkbenchInterface
    {
        [SerializeField]
        private int workbenchLevel = 1;

        [SerializeField]
        private int itemTableId;

        public int WorkbenchLevel => workbenchLevel;
        public override int ItemTableId => itemTableId;

        public override string GetPromptText()
        {
            return "制作";
        }
    }
}
