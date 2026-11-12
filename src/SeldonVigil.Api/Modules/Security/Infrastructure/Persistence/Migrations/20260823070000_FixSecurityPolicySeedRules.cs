using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeldonVigil.Api.Modules.Security.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SecurityDbContext))]
[Migration("20260823070000_FixSecurityPolicySeedRules")]
public sealed class FixSecurityPolicySeedRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The original seed rows stored flat rule JSON that RuleDefinitionConverter
        // cannot parse, category names missing from PolicyCategory, and auto-response
        // integers that do not line up with the AutoResponse enum. Rewrite them into
        // the canonical shape so policies load and evaluate correctly.
        migrationBuilder.Sql(@"
            UPDATE security_policies SET
                ""Category"" = 'RateLimiting',
                ""AutoResponse"" = 1,
                ""RuleDefinition"" = '{""type"": ""Threshold"", ""expression"": ""gt"", ""parameters"": {""metric"": ""requests_per_minute"", ""operator"": ""gt"", ""value"": 100}}'::jsonb
            WHERE ""Id"" = '00000000-0000-0000-0000-000000000001';

            UPDATE security_policies SET
                ""Category"" = 'DataProtection',
                ""AutoResponse"" = 1,
                ""RuleDefinition"" = '{""type"": ""RegexMatch"", ""expression"": ""contains"", ""parameters"": {""field"": ""output_text"", ""pattern"": ""[0-9]{3}-[0-9]{2}-[0-9]{4}""}}'::jsonb
            WHERE ""Id"" = '00000000-0000-0000-0000-000000000002';

            UPDATE security_policies SET
                ""Category"" = 'CostControl',
                ""AutoResponse"" = 2,
                ""RuleDefinition"" = '{""type"": ""Threshold"", ""expression"": ""gt"", ""parameters"": {""metric"": ""cost_usd"", ""operator"": ""gt"", ""value"": 1.0}}'::jsonb
            WHERE ""Id"" = '00000000-0000-0000-0000-000000000003';

            UPDATE security_policies SET
                ""Category"" = 'PromptInjection',
                ""AutoResponse"" = 1,
                ""RuleDefinition"" = '{""type"": ""RegexMatch"", ""expression"": ""contains"", ""parameters"": {""field"": ""input_text"", ""pattern"": ""(?i)(ignore (all )?(previous|prior) instructions|disregard your (system )?prompt|reveal your (system )?prompt|you are now|developer mode enabled)""}}'::jsonb
            WHERE ""Id"" = '00000000-0000-0000-0000-000000000004';
            UPDATE security_policies SET
                ""Category"" = 'ResourceControl',
                ""AutoResponse"" = 2,
                ""RuleDefinition"" = '{""type"": ""Threshold"", ""expression"": ""gt"", ""parameters"": {""metric"": ""token_usage_total"", ""operator"": ""gt"", ""value"": 10000}}'::jsonb
            WHERE ""Id"" = '00000000-0000-0000-0000-000000000005';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restores the original (broken) seed values; there is no reason to go back.
    }
}
