using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

#nullable disable

namespace SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No administrator is seeded: create the first user through registration and assign the Administrator role.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: nothing was seeded.
        }
    }
}
