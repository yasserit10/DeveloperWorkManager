using System.ComponentModel.DataAnnotations;

namespace DeveloperWorkManager.Data;

public class PersonalNote
{
    public long Id { get; set; }

    [Required]
    public string CreatedById { get; set; } = string.Empty;

    [Required, StringLength(1200)]
    public string Text { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ApplicationUser CreatedBy { get; set; } = null!;
}
