using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ObservabilityInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "traces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_trace_id = table.Column<Guid>(type: "uuid", nullable: true),
                    framework = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model_provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    operation_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    operation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<double>(type: "double precision", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status_message = table.Column<string>(type: "text", nullable: true),
                    attributes = table.Column<string>(type: "jsonb", nullable: false),
                    events = table.Column<string>(type: "jsonb", nullable: false),
                    links = table.Column<string>(type: "jsonb", nullable: false),
                    token_usage_input = table.Column<int>(type: "integer", nullable: false),
                    token_usage_output = table.Column<int>(type: "integer", nullable: false),
                    token_usage_total = table.Column<int>(type: "integer", nullable: false),
                    token_usage_cached = table.Column<int>(type: "integer", nullable: false),
                    cost_usd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    cost_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    latency_ms = table.Column<double>(type: "double precision", nullable: false),
                    queue_time_ms = table.Column<double>(type: "double precision", nullable: false),
                    is_root_span = table.Column<bool>(type: "boolean", nullable: false),
                    is_async = table.Column<bool>(type: "boolean", nullable: false),
                    trace_flags = table.Column<byte>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traces", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_traces_agent_id_start_time",
                table: "traces",
                columns: new[] { "agent_id", "start_time" });

            migrationBuilder.CreateIndex(
                name: "IX_traces_id",
                table: "traces",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_traces_tenant_id_start_time",
                table: "traces",
                columns: new[] { "tenant_id", "start_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "traces");
        }
    }
}
