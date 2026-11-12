using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeldonVigil.Api.Modules.CostIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCostData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var defaultTenantId = "'00000000-0000-0000-0000-000000000001'";
            var adminUserId = "'00000000-0000-0000-0000-000000000001'";

            // Monthly tenant-wide budget
            migrationBuilder.Sql($@"
                INSERT INTO cost_budgets (""Id"", ""TenantId"", ""Name"", ""ScopeType"", ""ScopeId"", ""budget_amount_usd"", ""Period"", ""AlertThresholds"", ""EnforcementType"", ""current_spend_usd"", ""LastResetAt"", ""CreatedBy"", ""CreatedAt"", ""UpdatedAt"", ""Version"")
                VALUES (
                    '00000000-0000-0000-0000-000000000001',
                    {defaultTenantId},
                    'Monthly Production Budget',
                    'Tenant',
                    {defaultTenantId},
                    1000.00,
                    'Monthly',
                    ARRAY[0.50, 0.75, 0.90]::decimal(5,2)[],
                    'Soft',
                    0.00,
                    NOW(),
                    {adminUserId},
                    NOW(),
                    NOW(),
                    1
                )
                ON CONFLICT (""Id"") DO NOTHING;
            ");

            // Daily development budget
            migrationBuilder.Sql($@"
                INSERT INTO cost_budgets (""Id"", ""TenantId"", ""Name"", ""ScopeType"", ""ScopeId"", ""budget_amount_usd"", ""Period"", ""AlertThresholds"", ""EnforcementType"", ""current_spend_usd"", ""LastResetAt"", ""CreatedBy"", ""CreatedAt"", ""UpdatedAt"", ""Version"")
                VALUES (
                    '00000000-0000-0000-0000-000000000002',
                    {defaultTenantId},
                    'Daily Development Budget',
                    'Tenant',
                    {defaultTenantId},
                    50.00,
                    'Daily',
                    ARRAY[0.75, 0.90]::decimal(5,2)[],
                    'Soft',
                    0.00,
                    NOW(),
                    {adminUserId},
                    NOW(),
                    NOW(),
                    1
                )
                ON CONFLICT (""Id"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM cost_budgets WHERE \"Id\" IN ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002')");
        }
    }
}
