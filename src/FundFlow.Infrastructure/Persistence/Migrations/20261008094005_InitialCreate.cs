using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FundFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LastSeenAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    InstrumentId = table.Column<string>(type: "TEXT", nullable: false),
                    IsinLikeId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    AssetClass = table.Column<string>(type: "TEXT", nullable: false),
                    SavingsPlanEligible = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.InstrumentId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_DemoSessions_DemoSessionId",
                        column: x => x.DemoSessionId,
                        principalTable: "DemoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepotNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Portfolios_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Portfolios_DemoSessions_DemoSessionId",
                        column: x => x.DemoSessionId,
                        principalTable: "DemoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavingsPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PortfolioId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlanNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsPlans_DemoSessions_DemoSessionId",
                        column: x => x.DemoSessionId,
                        principalTable: "DemoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavingsPlans_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavingsPlanVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SavingsPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionNo = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    ExecutionDay = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsDiscarded = table.Column<bool>(type: "INTEGER", nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsPlanVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsPlanVersions_DemoSessions_DemoSessionId",
                        column: x => x.DemoSessionId,
                        principalTable: "DemoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavingsPlanVersions_SavingsPlans_SavingsPlanId",
                        column: x => x.SavingsPlanId,
                        principalTable: "SavingsPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    InstrumentId = table.Column<string>(type: "TEXT", nullable: false),
                    Percentage = table.Column<int>(type: "INTEGER", nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Allocations_DemoSessions_DemoSessionId",
                        column: x => x.DemoSessionId,
                        principalTable: "DemoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Allocations_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "InstrumentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Allocations_SavingsPlanVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "SavingsPlanVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestNumber = table.Column<string>(type: "TEXT", nullable: false),
                    SavingsPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RequestedFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EffectiveDateShifted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReplacesRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    ResultingVersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeRequests_ChangeRequests_ReplacesRequestId",
                        column: x => x.ReplacesRequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChangeRequests_DemoSessions_DemoSessionId",
                        column: x => x.DemoSessionId,
                        principalTable: "DemoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangeRequests_SavingsPlanVersions_ResultingVersionId",
                        column: x => x.ResultingVersionId,
                        principalTable: "SavingsPlanVersions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChangeRequests_SavingsPlans_SavingsPlanId",
                        column: x => x.SavingsPlanId,
                        principalTable: "SavingsPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChangeLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeLog_ChangeRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangeLog_DemoSessions_DemoSessionId",
                        column: x => x.DemoSessionId,
                        principalTable: "DemoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChangeRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ChangedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    DemoSessionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeRequestStatusHistory_ChangeRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChangeRequestStatusHistory_DemoSessions_DemoSessionId",
                        column: x => x.DemoSessionId,
                        principalTable: "DemoSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Instruments",
                columns: new[] { "InstrumentId", "AssetClass", "IsinLikeId", "Name", "SavingsPlanEligible" },
                values: new object[,]
                {
                    { "INS-01", "Aktien-ETF", "XXDEMO000011", "Demo Welt Aktien ETF", true },
                    { "INS-02", "Aktien-ETF", "XXDEMO000029", "Demo Europa Aktien ETF", true },
                    { "INS-03", "Aktien-ETF", "XXDEMO000037", "Demo Schwellenländer ETF", true },
                    { "INS-04", "Rentenfonds", "XXDEMO000045", "Demo Euro Staatsanleihen Fonds", true },
                    { "INS-05", "Renten-ETF", "XXDEMO000052", "Demo Unternehmensanleihen ETF", true },
                    { "INS-06", "Mischfonds", "XXDEMO000060", "Demo Mischfonds Ausgewogen", true },
                    { "INS-07", "Offener Immobilienfonds", "XXDEMO000078", "Demo Immobilienfonds", false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_DemoSessionId",
                table: "Allocations",
                column: "DemoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_InstrumentId",
                table: "Allocations",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_VersionId",
                table: "Allocations",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLog_DemoSessionId",
                table: "ChangeLog",
                column: "DemoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLog_RequestId",
                table: "ChangeLog",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_DemoSessionId",
                table: "ChangeRequests",
                column: "DemoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_ReplacesRequestId",
                table: "ChangeRequests",
                column: "ReplacesRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_RequestNumber",
                table: "ChangeRequests",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_ResultingVersionId",
                table: "ChangeRequests",
                column: "ResultingVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequests_SavingsPlanId",
                table: "ChangeRequests",
                column: "SavingsPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequestStatusHistory_DemoSessionId",
                table: "ChangeRequestStatusHistory",
                column: "DemoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeRequestStatusHistory_RequestId",
                table: "ChangeRequestStatusHistory",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DemoSessionId",
                table: "Customers",
                column: "DemoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_CustomerId",
                table: "Portfolios",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_DemoSessionId",
                table: "Portfolios",
                column: "DemoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsPlans_DemoSessionId",
                table: "SavingsPlans",
                column: "DemoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsPlans_PortfolioId",
                table: "SavingsPlans",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsPlanVersions_DemoSessionId",
                table: "SavingsPlanVersions",
                column: "DemoSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsPlanVersions_SavingsPlanId_VersionNo",
                table: "SavingsPlanVersions",
                columns: new[] { "SavingsPlanId", "VersionNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allocations");

            migrationBuilder.DropTable(
                name: "ChangeLog");

            migrationBuilder.DropTable(
                name: "ChangeRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropTable(
                name: "ChangeRequests");

            migrationBuilder.DropTable(
                name: "SavingsPlanVersions");

            migrationBuilder.DropTable(
                name: "SavingsPlans");

            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "DemoSessions");
        }
    }
}
