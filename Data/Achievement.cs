using System.ComponentModel.DataAnnotations;

namespace DeveloperWorkManager.Data;

public class Achievement
{
    public long Id { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public int? SourceWorkItemId { get; set; }

    [Required, StringLength(300)]
    public string Title { get; set; } = string.Empty;

    public DateOnly AchievementDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, StringLength(4000)]
    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser CreatedBy { get; set; } = null!;
    public WorkItem? SourceWorkItem { get; set; }
}
