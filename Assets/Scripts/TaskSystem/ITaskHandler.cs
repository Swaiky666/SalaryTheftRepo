using UnityEngine;

/// <summary>
/// Task Handler Interface
/// All task handlers must implement this interface
/// </summary>
public interface ITaskHandler
{
    /// <summary>
    /// Initializes the task handler
    /// </summary>
    /// <param name="taskManager">Reference to the Task Manager</param>
    void Initialize(TaskManager taskManager);

    /// <summary>
    /// Checks if the handler can process the specified task type
    /// </summary>
    /// <param name="taskType">Task Type</param>
    /// <returns>True if it can handle the task, otherwise false</returns>
    bool CanHandleTask(TaskType taskType);

    /// <summary>
    /// Starts the task execution
    /// </summary>
    /// <param name="taskData">Task Data</param>
    /// <param name="taskIndex">The index of the task in the active list</param>
    void StartTask(TaskData taskData, int taskIndex);

    /// <summary>
    /// Cleans up task-related objects
    /// </summary>
    void CleanupTasks();
}