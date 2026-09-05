/// <summary>
/// PlayerPanel GE 条目增删
/// </summary>

using System;
using System.Collections.Generic;
using GAS.StateSystem;
using MieMieFrameWork;
using MiMieEventBus;
using UnityEngine;
namespace MieMieUIFrameWork.Runtime
{
    public partial class PlayerPanel
    {
        /// <summary> GE 条目模板 </summary>
        private GEInfoGroup geInfoGroupTemplate;

        /// <summary> 已生成的 GE 条目 </summary>
        private readonly List<GEInfoGroup> geInfoGroupList = new List<GEInfoGroup>();

        /// <summary> GE 应用事件订阅 </summary>
        private IDisposable geAppliedDisposable;

        /// <summary> GE 移除事件订阅 </summary>
        private IDisposable geRemovedDisposable;

        /// <summary>
        /// 取 GEUI 下模板并订阅生存 GE 事件 只允许壳调用一次
        /// </summary>
        private void BindGEUI()
        {
            var geRoot = View.GEUIVerticalLayoutGroup.transform;
            geInfoGroupTemplate = geRoot.GetComponentInChildren<GEInfoGroup>(true);
            geInfoGroupTemplate.gameObject.SetActive(false);

            geAppliedDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                PlayerStatEvents.SurvivalEffectApplied,
                AddGEInfo);
            geRemovedDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                PlayerStatEvents.SurvivalEffectRemoved,
                RemoveGEInfo);
        }

        /// <summary>
        /// 取消生存 GE 事件订阅
        /// </summary>
        private void UnbindGEUIEvents()
        {
            geAppliedDisposable?.Dispose();
            geAppliedDisposable = null;
            geRemovedDisposable?.Dispose();
            geRemovedDisposable = null;
        }

        /// <summary>
        /// 新增一条 GE 展示
        /// </summary>
        public void AddGEInfo(int geId)
        {
            var geInfoGroup = UnityEngine.Object.Instantiate(geInfoGroupTemplate, View.GEUIVerticalLayoutGroup.transform);
            geInfoGroup.gameObject.SetActive(true);
            geInfoGroup.InitComponents();
            geInfoGroup.SetInfo(geId);
            geInfoGroup.PlayShow();
            geInfoGroupList.Add(geInfoGroup);
        }

        /// <summary>
        /// 移除一条 GE 展示
        /// </summary>
        public void RemoveGEInfo(int geId)
        {
            for (int i = 0; i < geInfoGroupList.Count; i++)
            {
                var geInfoGroup = geInfoGroupList[i];
                if (geInfoGroup.GeId != geId)
                    continue;

                geInfoGroupList.RemoveAt(i);
                geInfoGroup.PlayHide(() => UnityEngine.Object.Destroy(geInfoGroup.gameObject));
                return;
            }
        }

        /// <summary>
        /// 立刻清掉全部已生成 GE 条目
        /// </summary>
        private void ClearGEInfoList()
        {
            for (int i = 0; i < geInfoGroupList.Count; i++)
                UnityEngine.Object.Destroy(geInfoGroupList[i].gameObject);
            geInfoGroupList.Clear();
        }
    }
}
