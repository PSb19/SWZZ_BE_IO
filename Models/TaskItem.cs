using SWZZ_Backend.Enums;

namespace SWZZ_Backend.Models{
    public class TaskItem{
        public int Id {get; set;}
        public int GroupId {get;set;}
        public string Title{get; set;}
        public string Description{get;set;}
        public string CommissionerName{get; set;}
        public string CommissioneeName{get; set;}
        public System.DateTime Deadline{get; set;}
        public System.DateTime StartTime{get; set;}
        public int EstimatedExecutionTime{get; set;}
        public TaskItemStatus Status {get; set;}
        public bool TaskFailed {get; set;}
    }
}