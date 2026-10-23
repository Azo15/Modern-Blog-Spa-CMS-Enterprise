using DataAccesLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccesLayer.Concrete
{
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
    {
        private readonly ICurrentUserService? _currentUser;

        public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUser = null) : base(options)
        {
            _currentUser = currentUser;
            // Soft-deletes are converted before EF processes required dependent relationships.
            ChangeTracker.CascadeDeleteTiming = Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming.OnSaveChanges;
        }

        public DbSet<About> Abouts { get; set; }
        public DbSet<AboutCard> AboutCards { get; set; }
        public DbSet<Slider> Sliders { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactForm> ContactForms { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ContactReply> ContactReplies { get; set; }
        public DbSet<DailyVisitorStat> DailyVisitorStats { get; set; }
        public DbSet<VisitorLog> VisitorLogs { get; set; }
        public DbSet<Galary> Galaries { get; set; }
        public DbSet<SiteTable> SiteTables { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Catagory> Catagories { get; set; }
        public DbSet<BlogFavorite> BlogFavorites { get; set; }
        public DbSet<BlogReaction> BlogReactions { get; set; }
        public DbSet<ContactEmailDelivery> ContactEmailDeliveries { get; set; }
        public DbSet<TrashArchiveEntry> TrashArchiveEntries { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentEmailDelivery> AppointmentEmailDeliveries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);


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

            modelBuilder.Entity<Comment>().HasOne(c => c.Blog).WithMany(b => b.Comments)
                .HasForeignKey(c => c.BlogId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Comment>().HasOne(c => c.AppUser).WithMany()
                .HasForeignKey(c => c.AppUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Comment>().HasOne(c => c.ParentComment).WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Comment>().Property(c => c.Content).HasMaxLength(1000).IsRequired();
            modelBuilder.Entity<Comment>().Property(c => c.IsApproved).HasDefaultValue(false);
            modelBuilder.Entity<Comment>().HasIndex(c => new { c.BlogId, c.IsApproved, c.IsDeleted });
            modelBuilder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted);

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
            => SaveChangesAsync(true, cancellationToken);

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override int SaveChanges() => SaveChanges(true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyAuditInformation();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        private void ApplyAuditInformation()
        {
            var now = DateTime.UtcNow;
            var userId = _currentUser?.UserId;
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedDate = now;
                    entry.Entity.DeletedBy = userId;
                }
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = now;
                        entry.Entity.CreatedBy = userId;
                        entry.Entity.IsDeleted = false;
                        entry.Entity.DeletedDate = null;
                        entry.Entity.DeletedBy = null;
                        break;
                    case EntityState.Modified:
                        entry.Property(e => e.CreatedDate).IsModified = false;
                        entry.Property(e => e.CreatedBy).IsModified = false;
                        entry.Entity.UpdatedDate = now;
                        entry.Entity.UpdatedBy = userId;
                        if (entry.Entity.IsDeleted && !entry.Property(e => e.IsDeleted).OriginalValue)
                        {
                            entry.Entity.DeletedDate = now;
                            entry.Entity.DeletedBy = userId;
                        }
                        break;
                }
            }
        }
    }
}
