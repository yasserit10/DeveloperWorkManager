using System.ComponentModel.DataAnnotations;

namespace DeveloperWorkManager.Data;

public class WorkUpdate
{
    public long Id { get; set; }
    public int WorkItemId { get; set; }
    public string CreatedById { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Summary { get; set; } = string.Empty;

    [Range(0, 24)]
    public decimal? HoursSpent { get; set; }
    [StringLength(1000)]
    public string? BlockerReason { get; set; }
    [Url, StringLength(1000)]
    public string? ReferenceUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public WorkItem WorkItem { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
}
