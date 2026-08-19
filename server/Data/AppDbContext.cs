using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<NoteFile> NoteFiles => Set<NoteFile>();
    public DbSet<PostComment> Comments => Set<PostComment>();
    public DbSet<NotePage> NotePages => Set<NotePage>();
    public DbSet<BrainMapKeyword> BrainMapKeywords => Set<BrainMapKeyword>();
    public DbSet<NoteHighlight> NoteHighlights => Set<NoteHighlight>();
    public DbSet<AiChatMessage> AiChatMessages => Set<AiChatMessage>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<UserRequest> UserRequests => Set<UserRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.Username).HasMaxLength(100);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();

            entity.HasOne(u => u.Settings)
                .WithOne(s => s.User)
                .HasForeignKey<UserSettings>(s => s.UserId);
        });

        modelBuilder.Entity<UserSettings>().HasKey(s => s.UserId);

        modelBuilder.Entity<Group>(entity =>
        {
            entity.Property(g => g.Name).HasMaxLength(200);
            entity.HasIndex(g => new { g.OwnerId, g.Name }).IsUnique();

            entity.HasOne(g => g.Owner)
                .WithMany(u => u.Groups)
                .HasForeignKey(g => g.OwnerId);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            // Most rows here are never shared (IsShared stays false) — they're just a user's
            // private notes. "Posts" as a table name only ever described the subset that got
            // shared publicly, so the physical table is named Notes instead; the C# type/DbSet
            // stay Post/Posts since that's still the app-wide vocabulary for "the shareable
            // thing a note becomes" (ShareSettingsModal, PostCard, etc.) and renaming those
            // would be a much larger, purely cosmetic change with no schema benefit.
            entity.ToTable("Notes");

            // MySql.EntityFrameworkCore's DateOnly read path throws InvalidCastException
            // (tries to read the `date` column straight into DateOnly); store/read as
            // DateTime instead and convert at the boundary.
            entity.Property(p => p.CreatedDate)
                .HasConversion(d => d.ToDateTime(TimeOnly.MinValue), dt => DateOnly.FromDateTime(dt))
                .HasColumnType("date");

            entity.Property(p => p.DocumentKnowledgeBase).HasColumnType("mediumtext");
            entity.Property(p => p.GraphJson).HasColumnType("mediumtext");

            entity.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId);

            entity.HasOne(p => p.Group)
                .WithMany(g => g.Posts)
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(p => p.LikedByUsers)
                .WithMany(u => u.LikedPosts)
                .UsingEntity(j => j.ToTable("PostLikes"));

            entity.HasMany(p => p.FavoritedByUsers)
                .WithMany(u => u.FavoritedPosts)
                .UsingEntity(j => j.ToTable("PostFavorites"));

            entity.HasMany(p => p.RepostedByUsers)
                .WithMany(u => u.RepostedPosts)
                .UsingEntity(j => j.ToTable("PostReposts"));
        });

        modelBuilder.Entity<NoteFile>()
            .HasOne(f => f.Post)
            .WithMany(p => p.Files)
            .HasForeignKey(f => f.PostId);

        modelBuilder.Entity<NotePage>(entity =>
        {
            entity.Property(p => p.Body).HasColumnType("mediumtext");

            entity.HasOne(p => p.Post)
                .WithMany(p => p.Pages)
                .HasForeignKey(p => p.PostId);
        });

        modelBuilder.Entity<BrainMapKeyword>()
            .HasOne(k => k.Post)
            .WithMany(p => p.Keywords)
            .HasForeignKey(k => k.PostId);

        modelBuilder.Entity<NoteHighlight>(entity =>
        {
            entity.HasOne(h => h.Post)
                .WithMany(p => p.Highlights)
                .HasForeignKey(h => h.PostId);

            // Restrict, not Cascade: same reasoning as PostComment.Author below — a highlight
            // already cascades from its Post, EF/MySQL can't cascade the same row twice.
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AiChatMessage>()
            .HasOne(m => m.Highlight)
            .WithMany(h => h.Messages)
            .HasForeignKey(m => m.HighlightId);

        modelBuilder.Entity<PostComment>(entity =>
        {
            entity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId);

            // Restrict, not Cascade: a comment also cascades from its Post, and MySQL/EF
            // can't cascade the same row from two paths (Post delete and Author delete) at once.
            entity.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();

            // Both FKs point at User, so both must be Restrict — MySQL rejects two cascade
            // paths into the same table from one delete. UserService.DeleteAsync clears
            // Follow rows explicitly before removing the User, same as Comments/Highlights.
            entity.HasOne(f => f.Follower)
                .WithMany(u => u.FollowingLinks)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(f => f.Following)
                .WithMany(u => u.FollowerLinks)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRequest>(entity =>
        {
            // Both FKs point at User — same reasoning as Follow above, both must be Restrict.
            // UserService.DeleteAsync clears these rows explicitly before removing a User.
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.HandledByUser)
                .WithMany()
                .HasForeignKey(r => r.HandledByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
