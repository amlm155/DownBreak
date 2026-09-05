using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 掉落物与手持物物理切换
    /// </summary>
    public static class ItemPhysicsUtil
    {
        /// <summary>
        /// 拿到手里 拆掉世界物理与刚体并关掉碰撞
        /// 必须在 SetParent 之前调用 否则动态刚体会盖掉挂点变换
        /// </summary>
        public static void SetHeld(GameObject root)
        {
            if (root == null)
                return;

            // 先拆依赖刚体的组件 再拆刚体
            var worldPhysicsList = root.GetComponentsInChildren<ItemWorldPhysics>(true);
            for (int i = 0; i < worldPhysicsList.Length; i++)
            {
                if (worldPhysicsList[i] != null)
                    Object.DestroyImmediate(worldPhysicsList[i]);
            }

            var interactableList = root.GetComponentsInChildren<InteractableBase>(true);
            for (int i = 0; i < interactableList.Length; i++)
            {
                if (interactableList[i] != null)
                    Object.DestroyImmediate(interactableList[i]);
            }

            var rigidbodyList = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodyList.Length; i++)
            {
                if (rigidbodyList[i] != null)
                    Object.DestroyImmediate(rigidbodyList[i]);
            }

            var colliderList = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliderList.Length; i++)
            {
                if (colliderList[i] != null)
                    colliderList[i].enabled = false;
            }

            // 关闭手持时不应保留的描边等组件
            var behaviourList = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviourList.Length; i++)
            {
                var behaviour = behaviourList[i];
                if (behaviour is IDisableWhenHeld)
                    behaviour.enabled = false;
            }
        }

        /// <summary>
        /// 扔到地上 随机朝向并自由下落 落地后变 Trigger
        /// </summary>
        public static void PrepareWorldDrop(GameObject root, bool randomizeRotation = true)
        {
            if (root == null)
                return;

            // 避免 identity 竖插地面 给随机姿态再进物理
            if (randomizeRotation)
                root.transform.rotation = Random.rotationUniform;

            if (root.GetComponent<Rigidbody>() == null)
                root.AddComponent<Rigidbody>();

            var worldPhysics = root.GetComponent<ItemWorldPhysics>();
            if (worldPhysics == null)
                worldPhysics = root.AddComponent<ItemWorldPhysics>();

            worldPhysics.enabled = true;
            worldPhysics.BeginWorldMode();
        }
    }
}
