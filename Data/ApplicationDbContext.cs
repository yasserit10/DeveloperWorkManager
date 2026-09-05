using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeveloperWorkManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<WorkProject> Projects => Set<WorkProject>();
    public DbSet<ProjectDeveloper> ProjectDevelopers => Set<ProjectDeveloper>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkUpdate> WorkUpdates => Set<WorkUpdate>();
    public DbSet<ProjectUpdate> ProjectUpdates => Set<ProjectUpdate>();
    public DbSet<MemoRequest> MemoRequests => Set<MemoRequest>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<SystemNotification> Notifications => Set<SystemNotification>();
    public DbSet<PersonalNote> Notes => Set<PersonalNote>();
    public DbSet<WorkStateEntry> WorkStateEntries => Set<WorkStateEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.CreatedAt);
        });

        builder.Entity<WorkProject>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProjectUrl).HasColumnType("nvarchar(max)");
            entity.Property(x => x.LatestReleaseUrl).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => x.AssignedDeveloperId);
            entity.HasOne(x => x.AssignedDeveloper).WithMany(x => x.AssignedProjects)
                .HasForeignKey(x => x.AssignedDeveloperId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectDeveloper>(entity =>
        {
            entity.HasKey(x => new { x.ProjectId, x.DeveloperId });
            entity.HasIndex(x => x.DeveloperId);
            entity.HasOne(x => x.Project).WithMany(x => x.Developers)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Developer).WithMany(x => x.ProjectAssignments)
                .HasForeignKey(x => x.DeveloperId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkItem>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.BlockerReason).HasMaxLength(1000);
            entity.HasIndex(x => new { x.AssignedToId, x.Status, x.DueDate });
            entity.HasOne(x => x.Project).WithMany(x => x.WorkItems)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedTo).WithMany(x => x.AssignedWorkItems)
                .HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkUpdate>(entity =>
        {
            entity.Property(x => x.Summary).HasMaxLength(3000).IsRequired();
            entity.Property(x => x.BlockerReason).HasMaxLength(1000);
            entity.Property(x => x.ReferenceUrl).HasMaxLength(1000);
            entity.Property(x => x.HoursSpent).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.WorkItemId, x.CreatedAt });
            entity.HasOne(x => x.WorkItem).WithMany(x => x.Updates)
                .HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CreatedBy).WithMany(x => x.WorkUpdates)
                .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectUpdate>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.ReleaseUrl).HasMaxLength(1000);
            entity.HasIndex(x => new { x.ProjectId, x.CreatedAt });
            entity.HasOne(x => x.Project).WithMany(x => x.Updates)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CreatedBy).WithMany(x => x.ProjectUpdates)
                .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MemoRequest>(entity =>
        {
            entity.Property(x => x.MemoNumber).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ImageFileName).HasMaxLength(260).IsRequired();
            entity.HasIndex(x => new { x.AssignedToId, x.MemoDate });
            entity.HasIndex(x => x.CreatedAt);
            entity.HasOne(x => x.AssignedTo).WithMany()
                .HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedBy).WithMany()
                .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Achievement>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.CreatedById, x.AchievementDate });
            entity.HasIndex(x => x.CreatedAt);
            entity.HasOne(x => x.CreatedBy).WithMany(x => x.Achievements)
                .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.SourceWorkItemId).IsUnique().HasFilter("[SourceWorkItemId] IS NOT NULL");
            entity.HasOne(x => x.SourceWorkItem).WithMany()
                .HasForeignKey(x => x.SourceWorkItemId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<WorkStateEntry>(entity =>
        {
            entity.HasIndex(x => new { x.CreatedById, x.CreatedAt });
            entity.HasIndex(x => new { x.ProjectId, x.CreatedAt });
            entity.HasOne(x => x.Project).WithMany(x => x.WorkStateEntries)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.WorkItem).WithMany(x => x.WorkStateEntries)
                .HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CreatedBy).WithMany(x => x.WorkStateEntries)
                .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SystemNotification>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1200).IsRequired();
            entity.Property(x => x.TargetUrl).HasMaxLength(500);
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.ReadAt, x.CreatedAt });
            entity.HasOne(x => x.User).WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PersonalNote>(entity =>
        {
            entity.Property(x => x.Text).HasMaxLength(1200).IsRequired();
            entity.HasIndex(x => new { x.CreatedById, x.IsCompleted, x.CreatedAt });
            entity.HasOne(x => x.CreatedBy).WithMany(x => x.Notes)
                .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
