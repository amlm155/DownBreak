using cfg.item;
using UnityEngine;
using UnityEngine.EventSystems;
namespace MieMieUIFrameWork.Runtime
{
    
    /// <summary>
    /// 热区点击转发 挂在各热区节点上
    /// </summary>
    public class ModelHotspotClick : MonoBehaviour, IPointerClickHandler
    {
        /// <summary> 所属热区根 </summary>
        private ModelHotspot owner;
    
        /// <summary> 对应装备槽 </summary>
        private EEquipSlot slot;
    
        /// <summary>
        /// 绑定所属与槽位
        /// </summary>
        public void Bind(ModelHotspot hotspot, EEquipSlot eSlot)
        {
            owner = hotspot;
            slot = eSlot;
        }
    
        /// <summary>
        /// 转发点击给 ModelHotspot
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.HandleHotspotClick(slot, eventData);
        }
    }
    
}