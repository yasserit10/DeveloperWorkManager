using System.ComponentModel.DataAnnotations;

namespace DeveloperWorkManager.Data;

public class SystemNotification
{
    public long Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1200)]
    public string Message { get; set; } = string.Empty;

    [StringLength(500)]
    public string? TargetUrl { get; set; }

    [StringLength(40)]
    public string Type { get; set; } = "General";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
