using System.ComponentModel.DataAnnotations;

namespace DeveloperWorkManager.Data;

public class MemoRequest
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string MemoNumber { get; set; } = string.Empty;

    public DateOnly MemoDate { get; set; }

    [Required, StringLength(260)]
    public string ImageFileName { get; set; } = string.Empty;

    [Required]
    public string AssignedToId { get; set; } = string.Empty;

    [Required]
    public string CreatedById { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser AssignedTo { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
}
