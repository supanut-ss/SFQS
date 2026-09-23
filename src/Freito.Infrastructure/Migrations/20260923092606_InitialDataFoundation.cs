using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Freito.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDataFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cargo_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDangerous = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsProhibited = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cargo_types", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "carriers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carriers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DecimalDigits = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.Code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incoterms",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RiskTransferPoint = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SellerPaysFreight = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incoterms", x => x.Code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ports", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RateToBase = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exchange_rates_currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "incoterm_charge_rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IncotermCode = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChargeSide = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Payer = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incoterm_charge_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_incoterm_charge_rules_incoterms_IncotermCode",
                        column: x => x.IncotermCode,
                        principalTable: "incoterms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "freight_rates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OriginPortId = table.Column<int>(type: "int", nullable: false),
                    DestinationPortId = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Direction = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CarrierId = table.Column<int>(type: "int", nullable: false),
                    ContainerSize = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WeightBreakMin = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    WeightBreakMax = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    PriceMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PriceMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_freight_rates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_freight_rates_carriers_CarrierId",
                        column: x => x.CarrierId,
                        principalTable: "carriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_freight_rates_currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_freight_rates_ports_DestinationPortId",
                        column: x => x.DestinationPortId,
                        principalTable: "ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_freight_rates_ports_OriginPortId",
                        column: x => x.OriginPortId,
                        principalTable: "ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "local_charges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PortId = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mode = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChargeType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CalcBasis = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AmountMin = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AmountMax = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MinimumCharge = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChargeSide = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_local_charges_currencies_CurrencyCode",
                        column: x => x.CurrencyCode,
                        principalTable: "currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_local_charges_ports_PortId",
                        column: x => x.PortId,
                        principalTable: "ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Entity = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    BeforeJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AfterJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quotations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuoteNo = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerCompany = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerEmail = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerPhone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginPortId = table.Column<int>(type: "int", nullable: false),
                    DestinationPortId = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mode = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CargoTypeId = table.Column<int>(type: "int", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    ContainerSize = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cbm = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    IncotermCode = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReadyDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    QuoteCurrency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FxRateUsed = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    RateSource = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FreightCost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LocalChargeTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FinalPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotations_cargo_types_CargoTypeId",
                        column: x => x.CargoTypeId,
                        principalTable: "cargo_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotations_currencies_QuoteCurrency",
                        column: x => x.QuoteCurrency,
                        principalTable: "currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotations_incoterms_IncotermCode",
                        column: x => x.IncotermCode,
                        principalTable: "incoterms",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotations_ports_DestinationPortId",
                        column: x => x.DestinationPortId,
                        principalTable: "ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotations_ports_OriginPortId",
                        column: x => x.OriginPortId,
                        principalTable: "ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotations_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_quotations_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quotation_lines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Basis = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceRateId = table.Column<int>(type: "int", nullable: true),
                    SourceLocalChargeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotation_lines_freight_rates_SourceRateId",
                        column: x => x.SourceRateId,
                        principalTable: "freight_rates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_quotation_lines_local_charges_SourceLocalChargeId",
                        column: x => x.SourceLocalChargeId,
                        principalTable: "local_charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_quotation_lines_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quotation_status_history",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuotationId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToStatus = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    At = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quotation_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quotation_status_history_quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_quotation_status_history_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "currencies",
                columns: new[] { "Code", "DecimalDigits", "Name" },
                values: new object[,]
                {
                    { "EUR", 2, "Euro" },
                    { "THB", 2, "Thai Baht" },
                    { "USD", 2, "US Dollar" }
                });

            migrationBuilder.InsertData(
                table: "incoterms",
                columns: new[] { "Code", "Name", "RiskTransferPoint", "SellerPaysFreight" },
                values: new object[,]
                {
                    { "CFR", "Cost and Freight", "On board the vessel at the port of shipment.", true },
                    { "CIF", "Cost, Insurance and Freight", "On board the vessel at the port of shipment.", true },
                    { "CIP", "Carriage and Insurance Paid To", "When the goods are handed over to the seller-contracted carrier.", true },
                    { "CPT", "Carriage Paid To", "When the goods are handed over to the seller-contracted carrier.", true },
                    { "DAP", "Delivered at Place", "At the named destination, ready for unloading.", true },
                    { "DDP", "Delivered Duty Paid", "At the named destination, cleared for import and ready for unloading.", true },
                    { "DPU", "Delivered at Place Unloaded", "At the named destination after the goods have been unloaded.", true },
                    { "EXW", "Ex Works", "Seller's named premises; goods placed at the buyer's disposal, not loaded.", false },
                    { "FAS", "Free Alongside Ship", "Alongside the vessel at the named port of shipment.", false },
                    { "FCA", "Free Carrier", "Named place after delivery to the buyer-nominated carrier.", false },
                    { "FOB", "Free on Board", "On board the vessel at the named port of shipment.", false }
                });

            migrationBuilder.InsertData(
                table: "exchange_rates",
                columns: new[] { "Id", "CurrencyCode", "EffectiveDate", "RateToBase" },
                values: new object[] { 1, "USD", new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1m });

            migrationBuilder.InsertData(
                table: "incoterm_charge_rules",
                columns: new[] { "Id", "ChargeSide", "IncotermCode", "Payer" },
                values: new object[,]
                {
                    { 1, "Origin", "EXW", "Buyer" },
                    { 2, "Destination", "EXW", "Buyer" },
                    { 3, "Origin", "FCA", "Seller" },
                    { 4, "Destination", "FCA", "Buyer" },
                    { 5, "Origin", "FAS", "Seller" },
                    { 6, "Destination", "FAS", "Buyer" },
                    { 7, "Origin", "FOB", "Seller" },
                    { 8, "Destination", "FOB", "Buyer" },
                    { 9, "Origin", "CFR", "Seller" },
                    { 10, "Destination", "CFR", "Buyer" },
                    { 11, "Origin", "CIF", "Seller" },
                    { 12, "Destination", "CIF", "Buyer" },
                    { 13, "Origin", "CPT", "Seller" },
                    { 14, "Destination", "CPT", "Buyer" },
                    { 15, "Origin", "CIP", "Seller" },
                    { 16, "Destination", "CIP", "Buyer" },
                    { 17, "Origin", "DPU", "Seller" },
                    { 18, "Destination", "DPU", "Seller" },
                    { 19, "Origin", "DAP", "Seller" },
                    { 20, "Destination", "DAP", "Seller" },
                    { 21, "Origin", "DDP", "Seller" },
                    { 22, "Destination", "DDP", "Seller" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_ChangedByUserId_ChangedAt",
                table: "audit_logs",
                columns: new[] { "ChangedByUserId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Entity_EntityId_ChangedAt",
                table: "audit_logs",
                columns: new[] { "Entity", "EntityId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_cargo_types_Name",
                table: "cargo_types",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carriers_Code",
                table: "carriers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_CurrencyCode_EffectiveDate",
                table: "exchange_rates",
                columns: new[] { "CurrencyCode", "EffectiveDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_freight_rates_CarrierId",
                table: "freight_rates",
                column: "CarrierId");

            migrationBuilder.CreateIndex(
                name: "IX_freight_rates_CurrencyCode",
                table: "freight_rates",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_freight_rates_DestinationPortId",
                table: "freight_rates",
                column: "DestinationPortId");

            migrationBuilder.CreateIndex(
                name: "IX_freight_rates_OriginPortId_DestinationPortId_Mode_Direction_~",
                table: "freight_rates",
                columns: new[] { "OriginPortId", "DestinationPortId", "Mode", "Direction", "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_incoterm_charge_rules_IncotermCode_ChargeSide",
                table: "incoterm_charge_rules",
                columns: new[] { "IncotermCode", "ChargeSide" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_local_charges_ChargeType",
                table: "local_charges",
                column: "ChargeType");

            migrationBuilder.CreateIndex(
                name: "IX_local_charges_CurrencyCode",
                table: "local_charges",
                column: "CurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_local_charges_PortId_Direction_Mode_ChargeSide_CalcBasis_Cha~",
                table: "local_charges",
                columns: new[] { "PortId", "Direction", "Mode", "ChargeSide", "CalcBasis", "ChargeType", "CurrencyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ports_Code",
                table: "ports",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ports_Type_Country_City",
                table: "ports",
                columns: new[] { "Type", "Country", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_quotation_lines_QuotationId",
                table: "quotation_lines",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_lines_SourceLocalChargeId",
                table: "quotation_lines",
                column: "SourceLocalChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_lines_SourceRateId",
                table: "quotation_lines",
                column: "SourceRateId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_status_history_ActorUserId",
                table: "quotation_status_history",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_quotation_status_history_QuotationId_At",
                table: "quotation_status_history",
                columns: new[] { "QuotationId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_quotations_ApprovedByUserId",
                table: "quotations",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_CargoTypeId",
                table: "quotations",
                column: "CargoTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_CreatedByUserId",
                table: "quotations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_DestinationPortId",
                table: "quotations",
                column: "DestinationPortId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_IncotermCode",
                table: "quotations",
                column: "IncotermCode");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_OriginPortId",
                table: "quotations",
                column: "OriginPortId");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_QuoteCurrency",
                table: "quotations",
                column: "QuoteCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_QuoteNo",
                table: "quotations",
                column: "QuoteNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quotations_ReadyDate",
                table: "quotations",
                column: "ReadyDate");

            migrationBuilder.CreateIndex(
                name: "IX_quotations_Status_CreatedByUserId",
                table: "quotations",
                columns: new[] { "Status", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "exchange_rates");

            migrationBuilder.DropTable(
                name: "incoterm_charge_rules");

            migrationBuilder.DropTable(
                name: "quotation_lines");

            migrationBuilder.DropTable(
                name: "quotation_status_history");

            migrationBuilder.DropTable(
                name: "freight_rates");

            migrationBuilder.DropTable(
                name: "local_charges");

            migrationBuilder.DropTable(
                name: "quotations");

            migrationBuilder.DropTable(
                name: "carriers");

            migrationBuilder.DropTable(
                name: "cargo_types");

            migrationBuilder.DropTable(
                name: "currencies");

            migrationBuilder.DropTable(
                name: "incoterms");

            migrationBuilder.DropTable(
                name: "ports");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
