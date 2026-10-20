using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnableRowLevelSecurity : Migration
    {
        // RLS with no policies closes Supabase's Data API (publishable key) to these tables.
        // The API connects as postgres, which bypasses RLS, so it is not affected.
        private static readonly string[] Tables = ["Expenses", "Categories", "AppSecrets", "__EFMigrationsHistory"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
        }
    }
}
