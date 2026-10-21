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
        public DbSet<AppSecret> AppSecrets => Set<AppSecret>();
        public DbSet<Budget> Budgets => Set<Budget>();
        public DbSet<FixedExpense> FixedExpenses => Set<FixedExpense>();

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

                expense.ToTable(t => t.HasCheckConstraint("CK_Expenses_Amount_Positive", "\"Amount\" > 0"));

                expense.HasOne<Category>()
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                expense.HasIndex(e => new { e.UserId, e.ExpenseDate });
                expense.HasIndex(e => new { e.UserId, e.CategoryId });
            });

            modelBuilder.Entity<Budget>(budget =>
            {
                budget.HasKey(b => b.UserId);
                budget.Property(b => b.WeeklyAmount).HasPrecision(18, 2);
                budget.Property(b => b.UpdatedAt).HasColumnType("timestamp with time zone");

                budget.ToTable(t => t.HasCheckConstraint("CK_Budgets_WeeklyAmount_Positive", "\"WeeklyAmount\" > 0"));
            });

            modelBuilder.Entity<FixedExpense>(fixedExpense =>
            {
                fixedExpense.Property(f => f.Name).HasMaxLength(FixedExpense.MaxNameLength);
                fixedExpense.Property(f => f.Amount).HasPrecision(18, 2);
                fixedExpense.Property(f => f.Currency).HasMaxLength(3);
                fixedExpense.Property(f => f.StartDate).HasColumnType("date");
                fixedExpense.Property(f => f.Frequency).HasConversion<string>().HasMaxLength(20);
                fixedExpense.Property(f => f.CreatedAt).HasColumnType("timestamp with time zone");

                fixedExpense.ToTable(t => t.HasCheckConstraint("CK_FixedExpenses_Amount_Positive", "\"Amount\" > 0"));

                fixedExpense.HasOne<Category>()
                    .WithMany()
                    .HasForeignKey(f => f.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                fixedExpense.HasIndex(f => f.UserId);
            });

            modelBuilder.Entity<AppSecret>(secret =>
            {
                secret.HasKey(s => s.Name);
                secret.Property(s => s.Name).HasMaxLength(100);
                secret.Property(s => s.UpdatedAt).HasColumnType("timestamp with time zone");
            });
        }
    }
}
