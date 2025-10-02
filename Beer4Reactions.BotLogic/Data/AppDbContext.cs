using Microsoft.EntityFrameworkCore;
using Beer4Reactions.BotLogic.Models;

namespace Beer4Reactions.BotLogic.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<MediaGroup> MediaGroups { get; set; }
    public DbSet<Reaction> Reactions { get; set; }
    public DbSet<MonthlyStatistic> MonthlyStatistics { get; set; }
    public DbSet<TopMessage> TopMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TelegramUserId, e.ChatId }).IsUnique();
            entity.HasIndex(e => e.ChatId);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
        });

        // MediaGroup configuration
        modelBuilder.Entity<MediaGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MediaGroupId, e.ChatId }).IsUnique();
            entity.Property(e => e.MediaGroupId).HasMaxLength(100);
        });

        // Photo configuration
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => new { e.ChatId, e.MessageId });
            entity.Property(e => e.FileId).HasMaxLength(200);
            entity.Property(e => e.FileUniqueId).HasMaxLength(200);
            entity.Property(e => e.Caption).HasMaxLength(1024);

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.Photos)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MediaGroup)
                .WithMany(mg => mg.Photos)
                .HasForeignKey(e => e.MediaGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Reaction configuration
        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.PhotoId, e.MediaGroupId, e.Type }).IsUnique();
            entity.Property(e => e.Type).HasMaxLength(50);

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.Reactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Photo)
                .WithMany(p => p.Reactions)
                .HasForeignKey(e => e.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MediaGroup)
                .WithMany(mg => mg.GroupReactions)
                .HasForeignKey(e => e.MediaGroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.ToTable(t => t.HasCheckConstraint("CK_Reaction_Target", 
                "(\"PhotoId\" IS NOT NULL AND \"MediaGroupId\" IS NULL) OR (\"PhotoId\" IS NULL AND \"MediaGroupId\" IS NOT NULL)"));
        });

        // MonthlyStatistic configuration
        modelBuilder.Entity<MonthlyStatistic>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChatId, e.Year, e.Month }).IsUnique();
            entity.Property(e => e.TopReactionType).HasMaxLength(50);

            // Relationships
            entity.HasOne(e => e.TopPhoto)
                .WithMany()
                .HasForeignKey(e => e.TopPhotoId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TopMediaGroup)
                .WithMany()
                .HasForeignKey(e => e.TopMediaGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TopUser)
                .WithMany()
                .HasForeignKey(e => e.TopUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // TopMessage configuration
        modelBuilder.Entity<TopMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChatId, e.IsActive });
            entity.Property(e => e.LastMessageContent).HasMaxLength(4096);
        });
    }
}
