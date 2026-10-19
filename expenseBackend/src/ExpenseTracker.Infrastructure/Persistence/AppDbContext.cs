using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
        {
        }

        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(category =>
            {
                category.Property(c => c.Name).HasMaxLength(50);
                category.HasIndex(c => c.Name).IsUnique();

                category.HasData(CategorySeed.All);
            });

            modelBuilder.Entity<Expense>(expense =>
            {
                expense.Property(e => e.Amount).HasPrecision(18, 2);
                expense.Property(e => e.Currency).HasMaxLength(3);
                expense.Property(e => e.Merchant).HasMaxLength(150);
                expense.Property(e => e.Description).HasMaxLength(500);
                expense.Property(e => e.ExpenseDate).HasColumnType("date");
                expense.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
                expense.Property(e => e.Source).HasConversion<string>().HasMaxLength(20);

                expense.ToTable(t => t.HasCheckConstraint("CK_Expenses_Amount_Positive", "\"Amount\" > 0"));

                expense.HasOne<Category>()
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                expense.HasIndex(e => new { e.UserId, e.ExpenseDate });
                expense.HasIndex(e => new { e.UserId, e.CategoryId });
            });
        }
    }
}
