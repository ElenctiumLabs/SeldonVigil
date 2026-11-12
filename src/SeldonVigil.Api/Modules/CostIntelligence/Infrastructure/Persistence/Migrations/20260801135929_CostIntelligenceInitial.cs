using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeldonVigil.Api.Modules.CostIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CostIntelligenceInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cost_budgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_amount_usd = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Period = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AlertThresholds = table.Column<decimal[]>(type: "decimal(5, 2)[]", nullable: false),
                    EnforcementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    current_spend_usd = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    LastResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cost_budgets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cost_budgets_TenantId_ScopeType",
                table: "cost_budgets",
                columns: new[] { "TenantId", "ScopeType" });

            migrationBuilder.CreateIndex(
                name: "IX_cost_budgets_TenantId_ScopeType_ScopeId_Period",
                table: "cost_budgets",
                columns: new[] { "TenantId", "ScopeType", "ScopeId", "Period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cost_budgets");
        }
    }
}
