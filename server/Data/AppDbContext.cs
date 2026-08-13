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
            // MySql.EntityFrameworkCore's DateOnly read path throws InvalidCastException
            // (tries to read the `date` column straight into DateOnly); store/read as
            // DateTime instead and convert at the boundary.
            entity.Property(p => p.CreatedDate)
                .HasConversion(d => d.ToDateTime(TimeOnly.MinValue), dt => DateOnly.FromDateTime(dt))
                .HasColumnType("date");

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
    }
}
