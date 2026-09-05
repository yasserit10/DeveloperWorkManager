using System.ComponentModel.DataAnnotations;

namespace DeveloperWorkManager.Data;

public class WorkProject
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? ProjectUrl { get; set; }

    public string? LatestReleaseUrl { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public string AssignedDeveloperId { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkItem> WorkItems { get; set; } = [];
    public ICollection<ProjectUpdate> Updates { get; set; } = [];
    public ICollection<WorkStateEntry> WorkStateEntries { get; set; } = [];
    public ICollection<ProjectDeveloper> Developers { get; set; } = [];
    public ApplicationUser AssignedDeveloper { get; set; } = null!;
}
