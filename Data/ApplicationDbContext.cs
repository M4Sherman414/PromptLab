using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PromptLab.Models;

namespace PromptLab.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ModelCatalog> ModelCatalogs => Set<ModelCatalog>();
    public DbSet<UserApiKey> UserApiKeys => Set<UserApiKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.ApiKey)
            .WithOne(k => k.User)
            .HasForeignKey<UserApiKey>(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}