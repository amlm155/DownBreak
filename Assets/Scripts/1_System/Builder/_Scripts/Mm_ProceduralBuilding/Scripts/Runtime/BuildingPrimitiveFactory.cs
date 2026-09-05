using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public static class BuildingPrimitiveFactory
    {
        /// <summary>
        /// 创建格子盒子
        /// </summary>
        public static GameObject CreateGridCube(
            string objectName,
            Transform parent,
            BuildingGridConvention convention,
            Vector3Int originGridPos,
            Vector3Int gridSize,
            Material material)
        {
            if (convention == null)
                return null;

            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = objectName;
            obj.transform.SetParent(parent, false);
            obj.transform.position = convention.GridBoxToWorldCenter(originGridPos, gridSize);
            obj.transform.localScale = convention.GridSizeToWorldSize(gridSize);

            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return obj;
        }

        /// <summary>
        /// 创建分组节点
        /// </summary>
        public static Transform CreateGroup(string groupName, Transform parent)
        {
            var obj = new GameObject(groupName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            return obj.transform;
        }
    }
}
