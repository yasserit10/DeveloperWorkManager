using Microsoft.AspNetCore.Identity;

namespace DeveloperWorkManager.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkItem> AssignedWorkItems { get; set; } = [];

    public ICollection<WorkUpdate> WorkUpdates { get; set; } = [];
    public ICollection<WorkProject> AssignedProjects { get; set; } = [];
    public ICollection<ProjectDeveloper> ProjectAssignments { get; set; } = [];
    public ICollection<ProjectUpdate> ProjectUpdates { get; set; } = [];
    public ICollection<WorkStateEntry> WorkStateEntries { get; set; } = [];
    public ICollection<Achievement> Achievements { get; set; } = [];
    public ICollection<SystemNotification> Notifications { get; set; } = [];
    public ICollection<PersonalNote> Notes { get; set; } = [];
}

