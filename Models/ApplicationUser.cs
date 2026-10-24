using Microsoft.AspNetCore.Identity;

namespace PromptLab.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;

    public UserApiKey? ApiKey {get; set;}
}