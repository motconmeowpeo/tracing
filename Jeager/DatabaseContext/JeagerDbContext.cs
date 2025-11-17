using Microsoft.EntityFrameworkCore;

namespace Jeager.DatabaseContext
{
    public partial class JeagerDbContext : DbContext
    {
        public static readonly string Schema = "public";

        public JeagerDbContext(DbContextOptions<JeagerDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Test> Tests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Test>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.ToTable("test", Schema);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100);
            });
            base.OnModelCreating(modelBuilder);
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}