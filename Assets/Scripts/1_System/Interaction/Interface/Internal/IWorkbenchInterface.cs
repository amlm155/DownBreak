namespace Interaction
{
    /// <summary>
    /// 工作台接口 场景可交互物体实现
    /// </summary>
    public interface IWorkbenchInterface : IPlaceAndBreakInterface
    {
        /// <summary> 工作台等级 </summary>
        int WorkbenchLevel { get; }
    }
}
