using System;
using System.Collections.Generic;

namespace DBGameSystem
{
    /// <summary>
    /// 服务标记接口 无方法 仅用于泛型约束与类型识别
    /// 各服务接口继承它 实现类注册后可被 Get 取回
    /// </summary>
    public interface IGameService
    {
    }

    /// <summary>
    /// 游戏层服务注册表 存放 DownBreak 各子系统实现
    /// 场景服务由 GameBootstrap 注册 UI 面板在 Awake 自注册
    /// 跨层取服务统一走这里 避免直接引用具体实现类
    /// </summary>
    public static class GameHub
    {
        /// <summary> 服务字典 键为服务接口类型 </summary>
        private static readonly Dictionary<Type, IGameService> serviceDict = new Dictionary<Type, IGameService>();

        /// <summary> 注册服务 同名覆盖 </summary>
        public static void Register<T>(T service) where T : class, IGameService
        {
            serviceDict[typeof(T)] = service;
        }

        /// <summary> 取服务 未注册返回 null </summary>
        public static T Get<T>() where T : class, IGameService
        {
            if (serviceDict.TryGetValue(typeof(T), out var service))
                return service as T;
            return null;
        }

        /// <summary> 注销服务 </summary>
        public static void Unregister<T>() where T : class, IGameService
        {
            serviceDict.Remove(typeof(T));
        }

        /// <summary> 清空 场景切换或框架销毁时调用 </summary>
        public static void Clear()
        {
            serviceDict.Clear();
        }
    }
}
