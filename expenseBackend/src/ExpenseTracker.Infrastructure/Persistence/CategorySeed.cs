using ExpenseTracker.Domain.Entities;

namespace ExpenseTracker.Infrastructure.Persistence
{
    // The fixed categories. Changing this list requires a new migration.
    public static class CategorySeed
    {
        public static readonly Guid Groceries = Guid.Parse("00000000-0000-0000-0000-000000000101");
        public static readonly Guid Dining = Guid.Parse("00000000-0000-0000-0000-000000000102");
        public static readonly Guid Transport = Guid.Parse("00000000-0000-0000-0000-000000000103");
        public static readonly Guid Housing = Guid.Parse("00000000-0000-0000-0000-000000000104");
        public static readonly Guid Bills = Guid.Parse("00000000-0000-0000-0000-000000000105");
        public static readonly Guid Health = Guid.Parse("00000000-0000-0000-0000-000000000106");
        public static readonly Guid Shopping = Guid.Parse("00000000-0000-0000-0000-000000000107");
        public static readonly Guid Entertainment = Guid.Parse("00000000-0000-0000-0000-000000000108");
        public static readonly Guid Travel = Guid.Parse("00000000-0000-0000-0000-000000000109");
        public static readonly Guid Other = Guid.Parse("00000000-0000-0000-0000-000000000110");

        public static readonly Category[] All =
        [
            new(Groceries, "Groceries"),
            new(Dining, "Dining"),
            new(Transport, "Transport"),
            new(Housing, "Housing"),
            new(Bills, "Bills"),
            new(Health, "Health"),
            new(Shopping, "Shopping"),
            new(Entertainment, "Entertainment"),
            new(Travel, "Travel"),
            new(Other, "Other"),
        ];
    }
}
