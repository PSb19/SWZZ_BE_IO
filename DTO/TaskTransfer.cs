using SWZZ_Backend.Models;
using SWZZ_Backend.Enums;
using System;

namespace SWZZ_Backend.DTO
{
    public class TaskItemDTO
    {
        public int TaskId {get; set;}
        public int GroupId {get;set;}
        public string Title {get; set;}
        public string Description {get;set;}
        public string CommissionerId {get; set;}
        public string CommissioneeId {get; set;}
        public long Deadline {get; set;}
        public long StartTime {get; set;}
        public int EstimatedExecutionTime {get; set;}
        public string Status {get; set;}
        public bool TaskFailed {get; set;}

        public TaskItemDTO() {}
        public TaskItemDTO(TaskItem task)
        {
            TaskId = task.Id;
            GroupId = task.GroupId;
            Title = task.Title;
            Description = task.Description;
            CommissionerId = task.CommissionerName;
            CommissioneeId = task.CommissioneeName;
            Deadline = new DateTimeOffset(task.Deadline).ToUnixTimeSeconds();
            StartTime = new DateTimeOffset(task.StartTime).ToUnixTimeSeconds();
            EstimatedExecutionTime = task.EstimatedExecutionTime;
            Status = task.Status.ToString();
            TaskFailed = task.TaskFailed;
        }
        
        public void CopyToTaskItem(TaskItem task)
        {
            TaskItemStatus status = ParseTaskStatus(Status);
            
            task.Id = TaskId;
            task.GroupId = GroupId;
            task.Title = Title;
            task.Description = Description;
            task.CommissionerName = CommissionerId;
            task.CommissioneeName = CommissioneeId;
            task.Deadline = DateTimeOffset.FromUnixTimeSeconds(Deadline).DateTime;
            task.StartTime = DateTimeOffset.FromUnixTimeSeconds(StartTime).DateTime;
            task.EstimatedExecutionTime = EstimatedExecutionTime;
            task.Status = status;
            task.TaskFailed = TaskFailed;
        }

        public static TaskItemStatus ParseTaskStatus(string inputStatus)
        {
            TaskItemStatus status;
            try
            {
                status = Enum.Parse<TaskItemStatus>(inputStatus);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is OverflowException)
                {
                    throw new ArgumentException(String.Format("'{0}' is not TaskItemStatus enum member", inputStatus), ex);
                }
                else throw;
            }
            return status;
        }
    }

    public class TaskAndPermissionsDTO
    {
        public TaskItemDTO TaskItemDTO {get; set;}
        public TaskPermissions TaskPermissions {get; set;}

        public TaskAndPermissionsDTO() {}
        public TaskAndPermissionsDTO(TaskItemDTO taskDTO, TaskPermissions permissions) => (TaskItemDTO, TaskPermissions) = (taskDTO, permissions);
    }

}