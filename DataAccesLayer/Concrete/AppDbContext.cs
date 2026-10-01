using EntityLayer.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccesLayer.Concrete
{
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<About> Abouts { get; set; }
        public DbSet<AboutCard> AboutCards { get; set; }
        public DbSet<Slider> Sliders { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactForm> ContactForms { get; set; }
        public DbSet<Galary> Galaries { get; set; }
        public DbSet<SiteTable> SiteTables { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Catagory> Catagories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // ============================
            // 📌 INDEXLER (Performans için)
            // ============================

            // Blog Title index
            modelBuilder.Entity<Blog>()
                .HasIndex(b => b.Title);

            // Blog Slug veya Url yapısı varsa (önerilir)
            // modelBuilder.Entity<Blog>()
            //     .HasIndex(b => b.Slug)
            //     .IsUnique();

            // Blog Categories Index
            modelBuilder.Entity<Blog>()
                .HasIndex(b => b.CatagoryId);

            // Blog User Index
            modelBuilder.Entity<Blog>()
                .HasIndex(b => b.AppUserId);

            // Category Name index
            modelBuilder.Entity<Catagory>()
                .HasIndex(c => c.Title);

            // ContactForm Email index (sık sorgulanır)
            modelBuilder.Entity<ContactForm>()
                .HasIndex(c => c.Email);

            // Service Name index
            modelBuilder.Entity<Service>()
                .HasIndex(s => s.Title);

            // Galeri Photo name index
            modelBuilder.Entity<Galary>()
                .HasIndex(g => g.Title);

            // =============================
            // 📌 İLİŞKİLER
            // =============================

            // AppUser ↔ Blog
            modelBuilder.Entity<Blog>()
                .HasOne(b => b.AppUser)
                .WithMany(u => u.Blogs)
                .HasForeignKey(b => b.AppUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Catagory ↔ Blog
            modelBuilder.Entity<Blog>()
                .HasOne(b => b.Catagory)
                .WithMany(c => c.Blogs)
                .HasForeignKey(b => b.CatagoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // =============================
            // 📌 GLOBAL QUERY FILTERS (SOFT DELETE)
            // =============================
            modelBuilder.Entity<About>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<AboutCard>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Slider>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Service>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Contact>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ContactForm>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Galary>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<SiteTable>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Blog>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Catagory>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        private void ApplyAuditInformation()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.Now;
                        entry.Entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedDate = DateTime.Now;
                        break;

                    case EntityState.Deleted:
                        // Soft Delete dönüşümü
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedDate = DateTime.Now;
                        break;
                }
            }
        }
    }
}
