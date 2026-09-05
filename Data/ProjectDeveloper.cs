namespace DeveloperWorkManager.Data;

public class ProjectDeveloper
{
    public int ProjectId { get; set; }
    public string DeveloperId { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public WorkProject Project { get; set; } = null!;
    public ApplicationUser Developer { get; set; } = null!;
}
