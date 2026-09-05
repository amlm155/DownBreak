using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 预览模块 全局持有一个临时方块 动态切换网格材质位置旋转缩放
    /// </summary>
    public partial class BuilderSystem
    {
        //预览方块
        private GameObject preObj;
        //预览方块网格渲染器
        private MeshRenderer preMeshRenderer;
        //预览方块网格过滤器
        private MeshFilter preMeshFilter;
        //当前预览方块数据
        private CubeData currentPreCubeData;
        
        /// <summary>
        /// 初始化预览方块信息
        /// </summary>
        private void InitPreViewCubeInfo()
        {
            //创建预览方块
            preObj = new GameObject("PreViewObj");
            preObj.transform.SetParent(preViewRoot, false);
            //添加网格过滤器和渲染器
            preMeshFilter = preObj.AddComponent<MeshFilter>();
            preMeshRenderer = preObj.AddComponent<MeshRenderer>();
            //禁用阴影投射与接收
            preMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            preMeshRenderer.receiveShadows = false;

            if (builderSetting?.preTrueMaterial != null)
                preMeshRenderer.sharedMaterial = builderSetting.preTrueMaterial;

            preObj.SetActive(false);
        }

        /// <summary>
        /// 隐藏预览方块
        /// </summary>
        private void HidePreView()
        {
            if (preObj != null)
                preObj.SetActive(false);

            currentPreCubeData = null;
        }

        /// <summary>
        /// 显示预览方块
        /// </summary>
        private void ShowPreView()
        {
            if (preObj != null)
                preObj.SetActive(true);
        }

        /// <summary>
        /// 更新预览方块
        /// </summary>
        public override void HandlePreview(BuilderPlacementReport placement, CubeData cubeData, bool canPlace)
        {
            if (cubeData?.CubePrefab == null || builderSetting == null)
            {
                HidePreView();
                return;
            }

            //获取源网格 家具网格常在子节点
            var srcFilter = cubeData.CubePrefab.GetComponentInChildren<MeshFilter>(true);
            if (srcFilter == null)
            {
                HidePreView();
                return;
            }

            ShowPreView();

            placement.FillOccupiedInfoToList(tempOccupiedGridList);

            //方块类型变化时更新网格
            if (currentPreCubeData != cubeData)
            {
                currentPreCubeData = cubeData;
                preMeshFilter.sharedMesh = srcFilter.sharedMesh;
            }

            // 位置按旋转后的实际占格中心对齐，避免绕未旋转中心转导致歪出网格
            preObj.transform.position = GetPlacementWorldPosition(
                placement,
                cubeData,
                tempOccupiedGridList,
                virtualGrid.gridUnitSize);
                                                            
            preObj.transform.rotation = placement.CubeWorldRotation;
            preObj.transform.localScale = cubeData.CubePrefab.transform.localScale;

            // 按能否放置切换预览材质
            preMeshRenderer.sharedMaterial = canPlace ? builderSetting.preTrueMaterial : builderSetting.preFalseMaterial;
        }
    }
}
