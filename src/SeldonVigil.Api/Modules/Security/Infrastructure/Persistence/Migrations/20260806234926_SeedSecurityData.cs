using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeldonVigil.Api.Modules.Security.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedSecurityData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var defaultTenantId = "'00000000-0000-0000-0000-000000000001'";
            var adminUserId = "'00000000-0000-0000-0000-000000000001'";

            // Rate Limiting Policy
            migrationBuilder.Sql(@"
                INSERT INTO security_policies (""Id"", ""TenantId"", ""Name"", ""Description"", ""Category"", ""RuleDefinition"", ""Severity"", ""AutoResponse"", ""Status"", ""CreatedBy"", ""CreatedAt"", ""UpdatedAt"", ""Version"")
                VALUES (
                    '00000000-0000-0000-0000-000000000001',
                    " + defaultTenantId + @",
                    'Rate Limiting Policy',
                    'Limits the number of requests per minute to prevent abuse',
                    'RateLimiting',
                    '{""metric"": ""request_count"", ""operator"": ""greater_than"", ""value"": 100, ""timeWindow"": ""00:01:00""}'::jsonb,
                    'Medium',
                    1, -- Throttle
                    'Active',
                    " + adminUserId + @",
                    NOW(),
                    NOW(),
                    1
                )
                ON CONFLICT DO NOTHING;
            ");

            // PII Detection Policy
            migrationBuilder.Sql(@"
                INSERT INTO security_policies (""Id"", ""TenantId"", ""Name"", ""Description"", ""Category"", ""RuleDefinition"", ""Severity"", ""AutoResponse"", ""Status"", ""CreatedBy"", ""CreatedAt"", ""UpdatedAt"", ""Version"")
                VALUES (
                    '00000000-0000-0000-0000-000000000002',
                    " + defaultTenantId + @",
                    'PII Detection Policy',
                    'Detects personally identifiable information in agent outputs',
                    'DataProtection',
                    '{""metric"": ""output_text"", ""operator"": ""contains_pattern"", ""value"": ""ssn_pattern"", ""threshold"": 1}'::jsonb,
                    'High',
                    2, -- Block
                    'Active',
                    " + adminUserId + @",
                    NOW(),
                    NOW(),
                    1
                )
                ON CONFLICT DO NOTHING;
            ");

            // Cost Threshold Policy
            migrationBuilder.Sql(@"
                INSERT INTO security_policies (""Id"", ""TenantId"", ""Name"", ""Description"", ""Category"", ""RuleDefinition"", ""Severity"", ""AutoResponse"", ""Status"", ""CreatedBy"", ""CreatedAt"", ""UpdatedAt"", ""Version"")
                VALUES (
                    '00000000-0000-0000-0000-000000000003',
                    " + defaultTenantId + @",
                    'Cost Threshold Policy',
                    'Alerts when single operation cost exceeds threshold',
                    'CostControl',
                    '{""metric"": ""cost_usd"", ""operator"": ""greater_than"", ""value"": 1.0}'::jsonb,
                    'Medium',
                    3, -- Alert
                    'Active',
                    " + adminUserId + @",
                    NOW(),
                    NOW(),
                    1
                )
                ON CONFLICT DO NOTHING;
            ");

            // Malicious Input Detection Policy
            migrationBuilder.Sql(@"
                INSERT INTO security_policies (""Id"", ""TenantId"", ""Name"", ""Description"", ""Category"", ""RuleDefinition"", ""Severity"", ""AutoResponse"", ""Status"", ""CreatedBy"", ""CreatedAt"", ""UpdatedAt"", ""Version"")
                VALUES (
                    '00000000-0000-0000-0000-000000000004',
                    " + defaultTenantId + @",
                    'Malicious Input Detection',
                    'Detects potentially malicious input patterns',
                    'Security',
                    '{""metric"": ""input_text"", ""operator"": ""contains_pattern"", ""value"": ""injection_pattern"", ""threshold"": 1}'::jsonb,
                    'Critical',
                    2, -- Block
                    'Active',
                    " + adminUserId + @",
                    NOW(),
                    NOW(),
                    1
                )
                ON CONFLICT DO NOTHING;
            ");

            // Token Usage Policy
            migrationBuilder.Sql(@"
                INSERT INTO security_policies (""Id"", ""TenantId"", ""Name"", ""Description"", ""Category"", ""RuleDefinition"", ""Severity"", ""AutoResponse"", ""Status"", ""CreatedBy"", ""CreatedAt"", ""UpdatedAt"", ""Version"")
                VALUES (
                    '00000000-0000-0000-0000-000000000005',
                    " + defaultTenantId + @",
                    'Token Usage Policy',
                    'Alerts when token usage exceeds reasonable limits',
                    'ResourceControl',
                    '{""metric"": ""token_usage_total"", ""operator"": ""greater_than"", ""value"": 10000}'::jsonb,
                    'Low',
                    3, -- Alert
                    'Active',
                    " + adminUserId + @",
                    NOW(),
                    NOW(),
                    1
                )
                ON CONFLICT DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM security_policies WHERE \"Id\" IN ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000005')");
        }
    }
}
