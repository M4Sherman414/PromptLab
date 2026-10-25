using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PromptLab.Models;

[Index(nameof(UserId), IsUnique = true)]
public class UserApiKey
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required]
    public string EncryptedKey { get; set; } = string.Empty;

    [Required, MaxLength(4)]
    public string LastFour { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}