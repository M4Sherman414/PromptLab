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
    public DbSet<Comparison> Comparisons => Set<Comparison>();
    public DbSet<ComparisonResult> ComparisonResults => Set<ComparisonResult>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<ModelStat> ModelStats => Set<ModelStat>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Kullanıcının en fazla bir API anahtarı olur.
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.ApiKey)
            .WithOne(k => k.User)
            .HasForeignKey<UserApiKey>(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Bir kullanıcının birden çok karşılaştırması olur.
        builder.Entity<Comparison>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Her karşılaştırmanın sonuçları olur. Aynı karşılaştırmada iki tane "A" olamaz.
        builder.Entity<ComparisonResult>()
            .HasOne(r => r.Comparison)
            .WithMany(c => c.ComparisonResults)
            .HasForeignKey(r => r.ComparisonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ComparisonResult>()
            .HasIndex(r => new { r.ComparisonId, r.Slot })
            .IsUnique();

        // Karşılaştırmada kullanılmış bir model silinemez.
        builder.Entity<ComparisonResult>()
            .HasOne(r => r.ModelCatalog)
            .WithMany()
            .HasForeignKey(r => r.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bir karşılaştırmaya en fazla bir oy verilir.
        builder.Entity<Vote>()
            .HasOne(v => v.Comparison)
            .WithOne(c => c.Vote)
            .HasForeignKey<Vote>(v => v.ComparisonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Her modelin en fazla bir istatistik satırı olur.
        builder.Entity<ModelStat>()
            .HasOne(s => s.ModelCatalog)
            .WithOne()
            .HasForeignKey<ModelStat>(s => s.ModelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Bir kullanıcının birden çok sohbet oturumu olur.
        builder.Entity<ChatSession>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sohbette kullanılmış bir model silinemez.
        builder.Entity<ChatSession>()
            .HasOne(s => s.ModelCatalog)
            .WithMany()
            .HasForeignKey(s => s.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ChatSession>()
            .HasIndex(s => new { s.UserId, s.UpdatedAt });

        // Bir sohbet silinince mesajları da silinir.
        builder.Entity<ChatMessage>()
            .HasOne(m => m.ChatSession)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ChatMessage>()
            .HasIndex(m => new { m.ChatSessionId, m.CreatedAt });
    }
}