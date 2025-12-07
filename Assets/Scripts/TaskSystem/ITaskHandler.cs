using UnityEngine;

/// <summary>
/// 任务处理器接口
/// 所有任务处理器都需要实现此接口
/// </summary>
public interface ITaskHandler
{
    /// <summary>
    /// 初始化任务处理器
    /// </summary>
    /// <param name="taskManager">任务管理器引用</param>
    void Initialize(TaskManager taskManager);

    /// <summary>
    /// 检查是否可以处理指定类型的任务
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <returns>是否可以处理</returns>
    bool CanHandleTask(TaskType taskType);

    /// <summary>
    /// 开始执行任务
    /// </summary>
    /// <param name="taskData">任务数据</param>
    /// <param name="taskIndex">任务在活跃列表中的索引</param>
    void StartTask(TaskData taskData, int taskIndex);

    /// <summary>
    /// 清理任务相关对象
    /// </summary>
    void CleanupTasks();
}