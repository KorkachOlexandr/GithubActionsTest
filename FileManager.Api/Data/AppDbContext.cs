using FileManager.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<FileMetadata> FileMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<FileMetadata>(entity =>
            {
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.ModifiedDate)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                // Foreign key to uploader
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UploaderId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Foreign key to editor
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.EditorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}