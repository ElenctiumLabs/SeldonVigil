using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeldonVigil.Api.Modules.CostIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlertThresholdsToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE cost_budgets DROP COLUMN \"AlertThresholds\"");
            migrationBuilder.Sql("ALTER TABLE cost_budgets ADD \"AlertThresholds\" jsonb NOT NULL DEFAULT '[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE cost_budgets DROP COLUMN \"AlertThresholds\"");
            migrationBuilder.Sql("ALTER TABLE cost_budgets ADD \"AlertThresholds\" decimal(5,2)[] NOT NULL DEFAULT ARRAY[]::decimal(5,2)[]");
        }
    }
}
