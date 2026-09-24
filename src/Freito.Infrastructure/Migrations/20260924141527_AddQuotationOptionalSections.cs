using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Freito.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationOptionalSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarrierInfo",
                table: "quotations",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ClosingSchedule",
                table: "quotations",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DimensionsJson",
                table: "quotations",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                table: "quotations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "InsuranceStatus",
                table: "quotations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "quotations",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TermsAndConditions",
                table: "quotations",
                type: "varchar(4000)",
                maxLength: 4000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TransitTime",
                table: "quotations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarrierInfo",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "ClosingSchedule",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "DimensionsJson",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "InsuranceStatus",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "TermsAndConditions",
                table: "quotations");

            migrationBuilder.DropColumn(
                name: "TransitTime",
                table: "quotations");
        }
    }
}
