using Evidence_MasterDetails_SinglePage.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Evidence_MasterDetails_SinglePage.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public virtual DbSet<Applicant> Applicant { get; set; }
        public virtual DbSet<Experience> Experience { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Applicant>(entity =>
            {
                entity.Property(e => e.Gender)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");

                entity.Property(e => e.PhotoUrl)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Qualification)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('')");
            });

            modelBuilder.Entity<Experience>(entity =>
            {
                entity.Property(e => e.CompanyName)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Designation)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.HasOne(d => d.Applicant)
                    .WithMany(p => p.Experience)
                    .HasForeignKey(d => d.ApplicantId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__Experienc__Appli__29572725");
            });

        }
    }
}
