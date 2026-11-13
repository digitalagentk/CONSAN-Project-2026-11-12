using ConsanWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsanWebApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<SiteContentTranslation> SiteContentTranslations { get; set; }
        public DbSet<HeroImage> HeroImages { get; set; }
        public DbSet<IndustrialContentTranslation> IndustrialContentTranslations { get; set; }
        public DbSet<IndustrialSetting> IndustrialSettings { get; set; }
        public DbSet<MapPin> MapPins { get; set; }
        public DbSet<ArchivalItem> ArchivalItems { get; set; }
        public DbSet<ExplorerCard> ExplorerCards { get; set; }
        public DbSet<GuideTopic> GuideTopics { get; set; }
        public DbSet<GuideEquipment> GuideEquipment { get; set; }
        public DbSet<AdminKey> AdminKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== User =====
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Username).IsUnique();
                e.HasIndex(u => u.Email).IsUnique();

                // Constraint على الدور
                e.ToTable(tb => tb.HasCheckConstraint(
                    "CK_Users_Role",
                    "[Role] IN ('Admin', 'Manager')"));
            });

            // ===== IndustrialSettings: Single-row constraint =====
            modelBuilder.Entity<IndustrialSetting>()
                .ToTable(tb => tb.HasCheckConstraint(
                    "CK_IndustrialSettings_SingleRow",
                    "[Id] = 1"));

            // ===== SiteContentTranslation =====
            modelBuilder.Entity<SiteContentTranslation>(e =>
            {
                e.HasIndex(t => new { t.Lang, t.Key }).IsUnique();
            });

            // ===== IndustrialContentTranslation =====
            modelBuilder.Entity<IndustrialContentTranslation>(e =>
            {
                e.HasIndex(t => new { t.Lang, t.Key }).IsUnique();
            });

            // ===== MapPin =====
            modelBuilder.Entity<MapPin>(e =>
            {
                e.HasIndex(p => p.IsApproved);
            });

            // ===== ExplorerCard =====
            modelBuilder.Entity<ExplorerCard>(e =>
            {
                e.HasIndex(c => c.SortOrder);
            });

            // ===== GuideTopic =====
            modelBuilder.Entity<GuideTopic>(e =>
            {
                e.HasIndex(t => t.SortOrder);
            });

            // ===== GuideEquipment =====
            modelBuilder.Entity<GuideEquipment>(e =>
            {
                e.HasIndex(g => g.SortOrder);
            });

            // ===== AdminKey =====
            modelBuilder.Entity<AdminKey>(e =>
            {
                e.HasIndex(k => k.KeyHash).IsUnique();
            });
        }
    }
}