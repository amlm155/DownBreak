using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Interaction
{
    /// <summary>
    /// 可交互物体基类 关卡物体继承或挂脚本即可
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractableInterface
    {
        [SerializeField, LabelText("提示文案")]
        private string promptText = "交互";

        [SerializeField, LabelText("允许交互")]
        private bool allowInteract = true;

        [SerializeField, LabelText("交互事件")]
        public UnityEvent onInteractEvent;

        public virtual bool CanInteract(InteractionContext ctx)
        {
            return allowInteract;
        }

        public virtual void OnFocusEnter(InteractionContext ctx)
        {
        }

        public virtual void OnFocusExit(InteractionContext ctx)
        {
        }

        public virtual void Interact(InteractionContext ctx)
        {
        }

        public virtual string GetPromptText()
        {
            return promptText;
        }
    }
}
