using Cysharp.Threading.Tasks;
using MieMieFrameWork.Asset;
using MmUIFrameWork.Core;
using MmInventory;
using UnityEngine;
namespace MieMieUIFrameWork.Runtime
{
    
    public class GameUIWarmUpFuck : MonoBehaviour
    {
        /// <summary>
        /// 初始化游戏界面
        /// </summary>
        private void Start()
        {
            EnsureUiFlowInput();
            UIHub.Instance.Init();
            // 加载PlayerUI
            UIHub.Instance.ShowWindow<PlayerPanel>();
            WarmUpWindowsAsync().Forget();
        }
    
        /// <summary>
        /// 确保 UI 流程输入路由存在
        /// </summary>
        private void EnsureUiFlowInput()
        {
            if (GetComponent<UIInputEventFuck>() != null)
                return;

            gameObject.AddComponent<UIInputEventFuck>();
        }

        /// <summary>
        /// 分帧预热窗口 错开实例化帧
        /// </summary>
        private async UniTask WarmUpWindowsAsync()
        {
            await UniTask.Yield();
            await UIHub.Instance.WarmUpWindowAsync<BagPanel>();
            await UniTask.Yield();
            await UIHub.Instance.WarmUpWindowAsync<CreatPanel>();
            await UniTask.Yield();
            await UIHub.Instance.WarmUpWindowAsync<UIItemWheel>();
            await UniTask.Yield();
            await WarmUpEquipmentIconsAsync();
            await UniTask.Yield();
            await UIHub.Instance.WarmUpWindowAsync<TipPanel>();
        }
    
        /// <summary>
        /// 分帧预加载装备容器图标
        /// </summary>
        private async UniTask WarmUpEquipmentIconsAsync()
        {
            var equipmentList = LubanTables.Tables.TbEquipment.DataList;
            for (int i = 0; i < equipmentList.Count; i++)
            {
                var equipment = equipmentList[i];
                if (string.IsNullOrEmpty(equipment.IconPath))
                    continue;
    
                await MmAssetMgr.LoadAssetAsync<Sprite>(equipment.IconPath);
                await UniTask.Yield();
            }
        }
    }
    
}