namespace DeveloperWorkManager.Data;

public class WorkStateEntry
{
    public long Id { get; set; }
    public int ProjectId { get; set; }
    public int WorkItemId { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public WorkActivityStatus Status { get; set; } = WorkActivityStatus.WorkingNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public WorkProject Project { get; set; } = null!;
    public WorkItem WorkItem { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
}

public enum WorkActivityStatus
{
    WorkingNow = 0,
    Paused = 1,
    Finished = 2,
    NotStarted = 3
}
