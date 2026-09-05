using System.ComponentModel.DataAnnotations;

namespace DeveloperWorkManager.Data;

public class ProjectUpdate
{
    public long Id { get; set; }
    public int ProjectId { get; set; }
    public string CreatedById { get; set; } = string.Empty;

    [Required, StringLength(240)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(8000)]
    public string Details { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? ReleaseUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public WorkProject Project { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
}
