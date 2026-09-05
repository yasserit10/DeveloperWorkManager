using System.ComponentModel.DataAnnotations;

namespace DeveloperWorkManager.Data;

public class WorkItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string AssignedToId { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public WorkPriority Priority { get; set; } = WorkPriority.Normal;
    public WorkStatus Status { get; set; } = WorkStatus.New;
    [Range(0, 100)]
    public int CompletionPercent { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    [StringLength(1000)]
    public string? BlockerReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public WorkProject Project { get; set; } = null!;
    public ApplicationUser AssignedTo { get; set; } = null!;
    public ICollection<WorkUpdate> Updates { get; set; } = [];
    public ICollection<WorkStateEntry> WorkStateEntries { get; set; } = [];
}

public enum WorkStatus { New, InProgress, WaitingForReview, Completed, Blocked }
public enum WorkPriority { Low, Normal, High, Urgent }
