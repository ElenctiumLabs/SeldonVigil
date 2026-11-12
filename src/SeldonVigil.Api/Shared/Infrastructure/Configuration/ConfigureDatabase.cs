using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Shared.Infrastructure.Configuration;

public static class ConfigureDatabase
{
    /// <summary>
    /// Applies EF migrations and creates app-data tables on startup.
    /// Returns true when the app should exit (--migrate mode).
    /// </summary>
    public static async Task<bool> MigrateOnStartupAsync(
        this WebApplication app,
        string[] args)
    {
        var migrateOnly = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);
        var migrateDefault = app.Environment.IsDevelopment() || migrateOnly;

        if (migrateOnly || app.Configuration.GetValue("Database:MigrateOnStartup", migrateDefault))
        {
            using var migrateScope = app.Services.CreateScope();
            try
            {
                var db = (DbContext)migrateScope.ServiceProvider.GetRequiredService<SeldonVigilDbContext>();
                // SeldonVigilDbContext has all entity configurations inline but no
                // EF migrations (migrations exist only for the unused module contexts).
                // EnsureCreated() builds the schema directly from the model.
                await db.Database.EnsureCreatedAsync();
                app.Logger.LogInformation("Database schema ensured for SeldonVigilDbContext");
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Schema creation failed for SeldonVigilDbContext - see inner exception");
                throw;
            }
        }

        if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
        {
            using var appDataScope = app.Services.CreateScope();
            var appDataDb = appDataScope.ServiceProvider.GetRequiredService<SeldonVigilDbContext>();
            var conn = (Npgsql.NpgsqlConnection)appDataDb.Database.GetDbConnection();
            await conn.OpenAsync().ConfigureAwait(false);
            await using var ddlCommand = conn.CreateCommand();
            ddlCommand.CommandText = """

                CREATE TABLE IF NOT EXISTS app_governance_policies (
                    "Id" uuid PRIMARY KEY,
                    "TenantId" uuid NOT NULL,
                    "Name" character varying(255) NOT NULL,
                    "Description" character varying(2000),
                    "Category" character varying(50),
                    "DefinitionYaml" text NOT NULL,
                    "Status" character varying(30),
                    "PreviousVersionId" text,
                    "CreatedBy" uuid NOT NULL,
                    "CreatedAt" timestamp WITHOUT time zone NOT NULL,
                    "UpdatedAt" timestamp WITHOUT time zone NOT NULL,
                    "ActivatedAt" timestamp WITHOUT time zone,
                    "DeprecatedAt" timestamp WITHOUT time zone,
                    "Version" integer NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS ix_app_gp_tenant_status ON app_governance_policies ("TenantId", "Status");
                CREATE TABLE IF NOT EXISTS app_certification_pipelines (
                    "Id" uuid PRIMARY KEY,
                    "TenantId" uuid NOT NULL,
                    "AgentId" uuid NOT NULL,
                    "Status" character varying(30),
                    "Stages" jsonb NOT NULL DEFAULT '[]',
                    "CurrentStage" character varying(100),
                    "StartedAt" timestamp WITHOUT time zone NOT NULL,
                    "CompletedAt" timestamp WITHOUT time zone,
                    "ApprovedBy" text,
                    "ApprovalNotes" character varying(2000),
                    "CreatedAt" timestamp WITHOUT time zone NOT NULL,
                    "UpdatedAt" timestamp WITHOUT time zone NOT NULL,
                    "Version" integer NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS ix_app_cp_tenant_agent ON app_certification_pipelines ("TenantId", "AgentId");
                CREATE TABLE IF NOT EXISTS app_root_cause_analyses (
                    "Id" uuid PRIMARY KEY,
                    "TenantId" text NOT NULL,
                    "AgentId" text NOT NULL,
                    "TraceId" text,
                    "AnomalyType" text NOT NULL,
                    "AnomalyStartTime" timestamp WITHOUT time zone NOT NULL,
                    "AnomalyEndTime" timestamp WITHOUT time zone NOT NULL,
                    "ProbableRootCause" text NOT NULL,
                    "Confidence" double precision NOT NULL,
                    "ConfidenceLabel" text NOT NULL,
                    "EvidenceJson" text NOT NULL DEFAULT '[]',
                    "ContributingFactorsJson" text NOT NULL DEFAULT '[]',
                    "RecommendedActionsJson" text NOT NULL DEFAULT '[]',
                    "RelatedTraceIdsJson" text NOT NULL DEFAULT '[]',
                    "RawResponse" text,
                    "Status" text NOT NULL DEFAULT 'pending',
                    "CreatedAt" timestamp WITHOUT time zone NOT NULL,
                    "UpdatedAt" timestamp WITHOUT time zone,
                    "is_deleted" boolean NOT NULL DEFAULT false
                );
                CREATE INDEX IF NOT EXISTS ix_app_rca_tenant ON app_root_cause_analyses ("TenantId");
                CREATE TABLE IF NOT EXISTS app_notifications (
                    "Id" uuid PRIMARY KEY,
                    "Type" text NOT NULL,
                    "Title" text NOT NULL,
                    "Message" text NOT NULL,
                    "AgentId" text,
                    "Severity" text NOT NULL DEFAULT 'info',
                    "Read" boolean NOT NULL DEFAULT false,
                    "ReadAt" timestamp WITHOUT time zone,
                    "TenantId" text,
                    "CreatedAt" timestamp WITHOUT time zone NOT NULL,
                    "UpdatedAt" timestamp WITHOUT time zone,
                    "is_deleted" boolean NOT NULL DEFAULT false
                );
                CREATE TABLE IF NOT EXISTS app_integrations (
                    "Id" uuid PRIMARY KEY,
                    "TenantId" text NOT NULL,
                    "Name" text NOT NULL,
                    "Type" character varying(40) NOT NULL,
                    "Status" character varying(40) NOT NULL,
                    "ConfigurationJson" text NOT NULL DEFAULT '{}',
                    "WebhookUrl" text,
                    "ApiKey" text,
                    "ChannelId" text,
                    "LastUsedAt" timestamp WITHOUT time zone,
                    "ErrorCount" integer NOT NULL DEFAULT 0,
                    "CreatedAt" timestamp WITHOUT time zone NOT NULL,
                    "UpdatedAt" timestamp WITHOUT time zone,
                    "is_deleted" boolean NOT NULL DEFAULT false
                );
                CREATE TABLE IF NOT EXISTS app_prompt_versions (
                    "Id" uuid PRIMARY KEY,
                    "Name" character varying(255) NOT NULL,
                    "Version" integer NOT NULL,
                    "Template" text NOT NULL,
                    "SystemPrompt" text NOT NULL,
                    "ModelConfig" jsonb NOT NULL DEFAULT '{}',
                    "Status" character varying(30) NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "CreatedAt" timestamp WITHOUT time zone NOT NULL,
                    "UpdatedAt" timestamp WITHOUT time zone NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_app_prompt_versions_name_version ON app_prompt_versions ("Name", "Version");
                CREATE INDEX IF NOT EXISTS ix_app_prompt_versions_name_status ON app_prompt_versions ("Name", "Status");
                CREATE TABLE IF NOT EXISTS app_evaluation_results (
                    "Id" uuid PRIMARY KEY,
                    "PromptName" character varying(255) NOT NULL,
                    "OverallScore" double precision NOT NULL,
                    "Passed" boolean NOT NULL,
                    "Metrics" jsonb NOT NULL DEFAULT '[]',
                    "EvaluationDurationMs" bigint NOT NULL,
                    "EvaluatedAt" timestamp WITHOUT time zone NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_app_evaluation_results_prompt_evaluated ON app_evaluation_results ("PromptName", "EvaluatedAt");
                CREATE TABLE IF NOT EXISTS saga_instances (
                    "Id" uuid PRIMARY KEY,
                    "SagaType" character varying(255) NOT NULL,
                    "CorrelationId" character varying(255) NOT NULL,
                    "State" character varying(50) NOT NULL,
                    "CurrentStep" integer NOT NULL DEFAULT 0,
                    "TotalSteps" integer NOT NULL DEFAULT 0,
                    "SagaData" jsonb NOT NULL DEFAULT '{}',
                    "ErrorMessage" character varying(4000),
                    "CreatedAt" timestamp WITHOUT time zone NOT NULL,
                    "UpdatedAt" timestamp WITHOUT time zone NOT NULL,
                    "CompletedAt" timestamp WITHOUT time zone,
                    "RetryCount" integer NOT NULL DEFAULT 0,
                    "MaxRetries" integer NOT NULL DEFAULT 3
                );
                CREATE INDEX IF NOT EXISTS ix_saga_correlation ON saga_instances ("CorrelationId");
                CREATE INDEX IF NOT EXISTS ix_saga_state ON saga_instances ("State");
                CREATE INDEX IF NOT EXISTS ix_saga_state_retry ON saga_instances ("State", "RetryCount");
                DROP TABLE IF EXISTS outbox_messages;
                CREATE TABLE outbox_messages (
                    "Id" uuid PRIMARY KEY,
                    "EventType" character varying(500) NOT NULL,
                    "Payload" jsonb NOT NULL,
                    "CorrelationId" character varying(255),
                    "CreatedAt" timestamp WITHOUT time zone NOT NULL,
                    "ProcessedAt" timestamp WITHOUT time zone,
                    "Status" integer NOT NULL DEFAULT 0,
                    "ErrorMessage" character varying(4000),
                    "RetryCount" integer NOT NULL DEFAULT 0,
                    "MaxRetries" integer NOT NULL DEFAULT 3
                );
                CREATE INDEX IF NOT EXISTS ix_outbox_unprocessed ON outbox_messages ("ProcessedAt", "CreatedAt") WHERE "ProcessedAt" IS NULL;

                CREATE TABLE IF NOT EXISTS agents (
                    "Id" uuid PRIMARY KEY,
                    "TenantId" uuid NOT NULL,
                    "ExternalId" character varying(255) NOT NULL,
                    "Name" character varying(255) NOT NULL,
                    "Framework" character varying(100),
                    "ModelProvider" character varying(100),
                    "ModelName" character varying(100) NOT NULL,
                    "Status" character varying(50) NOT NULL,
                    "CapabilityManifest" jsonb NOT NULL DEFAULT '{}',
                    "CapabilityManifestHash" character varying(64) NOT NULL,
                    "IdentityTokenJti" uuid,
                    "IdentityTokenIssuedAt" timestamp with time zone,
                    "IdentityTokenExpiresAt" timestamp with time zone,
                    "CreatedBy" uuid NOT NULL,
                    "ActivatedAt" timestamp with time zone,
                    "SuspendedAt" timestamp with time zone,
                    "SuspendedReason" text,
                    "RevokedAt" timestamp with time zone,
                    "RevokedReason" text,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    "Version" integer NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS ix_agents_tenant_external ON agents ("TenantId", "ExternalId");
                CREATE UNIQUE INDEX IF NOT EXISTS ix_agents_tenant_external_unique ON agents ("TenantId", "ExternalId");
                CREATE INDEX IF NOT EXISTS ix_agents_tenant_status ON agents ("TenantId", "Status");
                CREATE INDEX IF NOT EXISTS ix_agents_tenant_framework ON agents ("TenantId", "Framework");
                CREATE INDEX IF NOT EXISTS ix_agents_identity_token_jti ON agents ("IdentityTokenJti");

                CREATE TABLE IF NOT EXISTS users (
                    "Id" uuid PRIMARY KEY,
                    "TenantId" uuid NOT NULL,
                    "Email" character varying(255) NOT NULL,
                    "Name" character varying(255) NOT NULL,
                    "PasswordHash" character varying(255),
                    "SsoSubject" text,
                    "SsoProvider" character varying(50),
                    "MfaEnabled" boolean NOT NULL DEFAULT false,
                    "MfaSecretEncrypted" text,
                    "Role" character varying(50) NOT NULL,
                    "LastLoginAt" timestamp with time zone,
                    "LoginAttempts" integer NOT NULL DEFAULT 0,
                    "LockedUntil" timestamp with time zone,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    "Version" integer NOT NULL DEFAULT 0
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_users_tenant_email ON users ("TenantId", "Email");
                CREATE INDEX IF NOT EXISTS ix_users_tenant_role ON users ("TenantId", "Role");

                CREATE TABLE IF NOT EXISTS user_sessions (
                    "Id" uuid PRIMARY KEY,
                    "UserId" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "TokenHash" character varying(255) NOT NULL,
                    "RefreshTokenHash" character varying(255),
                    "ExpiresAt" timestamp with time zone NOT NULL,
                    "IpAddress" text,
                    "UserAgent" text,
                    "DeviceFingerprint" character varying(255),
                    "RevokedAt" timestamp with time zone,
                    "RevokedReason" character varying(255),
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_user_sessions_refresh_token ON user_sessions ("RefreshTokenHash");
                CREATE INDEX IF NOT EXISTS ix_user_sessions_user_created ON user_sessions ("UserId", "CreatedAt");

                -- No administrator is seeded: create the first user through registration and assign the Administrator role.
                """;
            await ddlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            app.Logger.LogInformation("App data schema ensured");
        }

        return migrateOnly;
    }
}
