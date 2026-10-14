using AncientServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AncientServer.Data
{
    public class AncientGameContext : DbContext
    {
        public AncientGameContext(DbContextOptions<AncientGameContext> options)
            : base(options)
        {
        }

        public DbSet<Continent> Continents { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Player> Players { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Континент
            modelBuilder.Entity<Continent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Element).HasMaxLength(50);
                entity.Property(e => e.BonusType).HasMaxLength(50);
            });

            // Город
            modelBuilder.Entity<City>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Buff).HasMaxLength(255);

                // Вложенные списки храним как JSONB
                entity.Property(e => e.Settlements)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb");

                entity.Property(e => e.Mines)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb");

                entity.Property(e => e.MobCamps)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb");

                entity.Property(e => e.Dungeons)
                    .HasColumnType("jsonb")
                    .HasDefaultValueSql("'[]'::jsonb");

                entity.HasOne<Continent>()
                    .WithMany()
                    .HasForeignKey(e => e.ContinentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Игрок
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(32);
                entity.Property(e => e.Username).HasMaxLength(20).IsRequired();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.GuildId).HasMaxLength(32);
                entity.Property(e => e.PasswordHash).HasMaxLength(100);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Language).HasMaxLength(5);
            });
        }
    }
}