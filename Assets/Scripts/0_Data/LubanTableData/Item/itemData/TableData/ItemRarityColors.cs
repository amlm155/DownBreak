using cfg.item;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品稀有度展示色 世界描边与 UI 共用
    /// </summary>
    public static class ItemRarityColors
    {
        /// <summary>
        /// 稀有度对应 RGB 不含 alpha
        /// </summary>
        public static Color GetRgb(EItemRarity eItemRarity)
        {
            switch (eItemRarity)
            {
                case EItemRarity.White:
                    return Color.white;
                case EItemRarity.Green:
                    return Color.green;
                case EItemRarity.Blue:
                    return Color.blue;
                case EItemRarity.Purple:
                    return Color.purple;
                case EItemRarity.Gold:
                    return Color.orange;
                case EItemRarity.Red:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// 稀有度色并指定 alpha
        /// </summary>
        public static Color GetColor(EItemRarity eItemRarity, float alpha)
        {
            Color color = GetRgb(eItemRarity);
            color.a = alpha;
            return color;
        }
    }
}
