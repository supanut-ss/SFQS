using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Freito.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSampleMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "cargo_types",
                columns: new[] { "Id", "IsDangerous", "IsProhibited", "Name" },
                values: new object[,]
                {
                    { 1, false, false, "General Cargo" },
                    { 2, false, false, "Perishable Goods" },
                    { 3, true, false, "Dangerous Goods" },
                    { 4, false, true, "Prohibited Goods" }
                });

            migrationBuilder.InsertData(
                table: "carriers",
                columns: new[] { "Id", "Code", "Name", "Type" },
                values: new object[,]
                {
                    { 1, "MAERSK", "Maersk", "ShippingLine" },
                    { 2, "COSCO", "COSCO Shipping Lines", "ShippingLine" },
                    { 3, "CMA CGM", "CMA CGM", "ShippingLine" },
                    { 4, "TG", "Thai Airways", "Airline" },
                    { 5, "SQ", "Singapore Airlines", "Airline" },
                    { 6, "CX", "Cathay Pacific Airways", "Airline" }
                });

            migrationBuilder.InsertData(
                table: "ports",
                columns: new[] { "Id", "City", "Code", "Country", "Name", "Type" },
                values: new object[,]
                {
                    { 1, "Laem Chabang", "THLCH", "Thailand", "Laem Chabang Port", "Sea" },
                    { 2, "Bangkok", "THBKK", "Thailand", "Bangkok Port", "Sea" },
                    { 3, "Shanghai", "CNSHA", "China", "Port of Shanghai", "Sea" },
                    { 4, "Shenzhen", "CNSZX", "China", "Port of Shenzhen", "Sea" },
                    { 5, "Singapore", "SGSIN", "Singapore", "Port of Singapore", "Sea" },
                    { 6, "Bangkok", "BKK", "Thailand", "Suvarnabhumi Airport", "Air" },
                    { 7, "Shanghai", "PVG", "China", "Shanghai Pudong International Airport", "Air" },
                    { 8, "Hong Kong", "HKG", "Hong Kong", "Hong Kong International Airport", "Air" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "cargo_types",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "cargo_types",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "cargo_types",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "cargo_types",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "carriers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "carriers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "carriers",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "carriers",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "carriers",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "carriers",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ports",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ports",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ports",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ports",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ports",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ports",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ports",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ports",
                keyColumn: "Id",
                keyValue: 8);
        }
    }
}
