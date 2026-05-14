using GwaFind.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GwaFind.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Listing> Listings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }
        public DbSet<Inquiry> Inquiries { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Listing>()
                .Property(l => l.Price)
                .HasPrecision(18, 2);

            builder.Entity<Favorite>()
                .HasOne(f => f.Listing)
                .WithMany()
                .HasForeignKey(f => f.ListingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Favorite>()
                .HasOne(f => f.Seeker)
                .WithMany()
                .HasForeignKey(f => f.SeekerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Inquiry>()
                .HasOne(i => i.Listing)
                .WithMany(l => l.Inquiries)
                .HasForeignKey(i => i.ListingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Inquiry>()
                .HasOne(i => i.Seeker)
                .WithMany()
                .HasForeignKey(i => i.SeekerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Report>()
                .HasOne(r => r.Listing)
                .WithMany(l => l.Reports) 
                .HasForeignKey(r => r.ListingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Report>()
                .HasOne(r => r.ReportedBy)
                .WithMany()
                .HasForeignKey(r => r.ReportedById)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Message>()
                .HasOne(m => m.Inquiry)
                .WithMany()
                .HasForeignKey(m => m.InquiryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}