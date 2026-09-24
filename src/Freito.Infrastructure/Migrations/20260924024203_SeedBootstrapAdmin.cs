using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freito.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBootstrapAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "PasswordHash", "Role" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@freito.local", true, "100000.RnJlaXRvQm9vdHN0cmFwU2FsdA==.ARqOilZbMKbjVo4Xi+DXpTkl2AQYWlJcX+e3XKjy7VA=", "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
