using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 存档结构体 
    /// </summary>
    [System.Serializable]
    public struct CubeSaveStruct
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ECubeType type;
        public int x;
        public int y;
        public int z;
        public int rotationSteps;
    }

    /// <summary>
    /// 此类用于存档处理与全局注册表管理
    /// 全局注册表用于反查方块数据以及实现GM功能
    /// </summary>
    public partial class BuilderSystem
    {
        /// <summary>
        /// 保存目录
        /// </summary>
        private string SaveDirectory => builderSetting.GetSaveDirectory();

        /// <summary>
        /// 保存文件路径
        /// </summary>
        private string SaveFilePath => builderSetting.GetBuildFilePath();

        /// <summary>
        /// 全局方块注册表
        /// </summary>
        private Dictionary<ECubeType, CubeData> cubeRegisteredDataDict = new();

        /// <summary>
        /// 注册所有方块数据
        /// </summary>
        public void RegisterAllCubeData()
        {
            cubeRegisteredDataDict.Clear();
            foreach (var cubeData in builderSetting.allCubeDataList)
            {
                if (cubeData == null) continue;
                cubeRegisteredDataDict[cubeData.CubeType] = cubeData;
            }
        }

        /// <summary>
        /// 注册方块数据
        /// 一般用于运行时动态注册方块数据
        /// </summary>
        /// <param name="cubeType"></param>
        /// <param name="cubeData"></param>
        public void RegisterCubeData(ECubeType cubeType, CubeData cubeData)
        {
            cubeRegisteredDataDict[cubeData.CubeType] = cubeData;
        }

        /// <summary>
        /// 移除方块数据
        /// 一般用于运行时动态移除方块数据
        /// </summary>
        /// <param name="cubeType"></param>
        public void RemoveCubeData(ECubeType cubeType)
        {
            cubeRegisteredDataDict.Remove(cubeType);
        }


        /// <summary>
        /// 尝试获取方块数据
        /// </summary>
        /// <param name="cubeType"></param>
        public CubeData TryGetCubeDataFormRegister(ECubeType cubeType)
        {
            if (cubeRegisteredDataDict.TryGetValue(cubeType, out var cubeData) && cubeData != null)
                return cubeData;
            Debug.LogWarning($"[TryGetCubeDataFormRegister] 找不到类型 {cubeType} 对应的 CubeData");
            return null;
        }

        /// <summary>
        /// 清空当前所有已放置方块 销毁物体并清空字典
        /// 一般用于加载存档时清空当前所有方块
        /// </summary>
        public void ClearAllCubeData()
        {
            var seen = new HashSet<CubeInstance>( runtimeCubeDataDict.Values);
            foreach (var cubeInstance in seen)
            {
                if (cubeInstance.instantiateCube != null)
                    Destroy(cubeInstance.instantiateCube);
            }
            runtimeCubeDataDict.Clear();  
        }


        /// <summary>
        /// 处理保存
        /// </summary>
        override public void HandleSave()
        {
            // 去重 因为一个方块可能占了多个格子 比如0,0,0和0,0,1都是同一个长条方块
            // 我们存储的时候 只需要记录这长条方块的起始位置即可 其世界位置是推导出来的
            var noReaptedList = new List<CubeInstance>(runtimeCubeDataDict.Values);

            var saveStructList = new List<CubeSaveStruct>();
            // 逐条保存数据
            foreach (var itme in noReaptedList)
            {
                // 物品表放置不进建造方块存档
                if (ReferenceEquals(itme.data, itemPlaceCubeData))
                    continue;

                saveStructList.Add(new CubeSaveStruct
                {
                    type = itme.data.CubeType,
                    x = itme.originGridPos.x,
                    y = itme.originGridPos.y,
                    z = itme.originGridPos.z,
                    rotationSteps = (int)itme.rotation,
                });
            }

            string json = JsonConvert.SerializeObject(saveStructList, Formatting.Indented);
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(SaveFilePath, json);
        }

        /// <summary>
        /// 处理加载
        /// </summary>
        override public void HandleLoad()
        {
            // 确认存档文件存在
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning($"[Load] 没有存档文件 {SaveFilePath}");
                return;
            }

            // 读取存档文件
            string json = File.ReadAllText(SaveFilePath);
            var saveStructList = JsonConvert.DeserializeObject<List<CubeSaveStruct>>(json);
            if (saveStructList == null) return;

            // 检查全局注册表 并 清空内存之中旧数据
            if (cubeRegisteredDataDict.Count == 0)
                RegisterAllCubeData();

            // 清空内存之中旧数据
            ClearAllCubeData();

            // 逐条加载数据
            foreach (var itme in saveStructList)
            {
                var cubeData = TryGetCubeDataFormRegister(itme.type);
                if (cubeData == null) continue;

                var originGridPos = new Vector3Int(itme.x, itme.y, itme.z);

                // 创建放置描述
                BuilderPlacementReport.CreateReport(originGridPos,
                                                    cubeData,
                                                    (ECubeRotation)itme.rotationSteps,
                                                    out var placement);
                CreatAndPlaceCube(placement, cubeData);
            }
        }
    }
}
