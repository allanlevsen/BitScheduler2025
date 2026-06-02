using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitClients",
                columns: table => new
                {
                    BitClientId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitClients", x => x.BitClientId);
                });

            migrationBuilder.CreateTable(
                name: "BitDays",
                columns: table => new
                {
                    BitDayId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    DayData = table.Column<byte[]>(type: "bytea", nullable: false),
                    Metadata = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitDays", x => x.BitDayId);
                });

            migrationBuilder.CreateTable(
                name: "BitResourceTypes",
                columns: table => new
                {
                    BitResourceTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitResourceTypes", x => x.BitResourceTypeId);
                });

            migrationBuilder.CreateTable(
                name: "BitResources",
                columns: table => new
                {
                    BitResourceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BitResourceTypeId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BitClientId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitResources", x => x.BitResourceId);
                    table.ForeignKey(
                        name: "FK_BitResources_BitClients_BitClientId",
                        column: x => x.BitClientId,
                        principalTable: "BitClients",
                        principalColumn: "BitClientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BitResources_BitResourceTypes_BitResourceTypeId",
                        column: x => x.BitResourceTypeId,
                        principalTable: "BitResourceTypes",
                        principalColumn: "BitResourceTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BitReservations",
                columns: table => new
                {
                    BitReservationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BitClientId = table.Column<int>(type: "integer", nullable: false),
                    BitResourceId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    StartBlock = table.Column<int>(type: "integer", nullable: false),
                    SlotLength = table.Column<int>(type: "integer", nullable: false),
                    BitDayId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitReservations", x => x.BitReservationId);
                    table.ForeignKey(
                        name: "FK_BitReservations_BitClients_BitClientId",
                        column: x => x.BitClientId,
                        principalTable: "BitClients",
                        principalColumn: "BitClientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BitReservations_BitDays_BitDayId",
                        column: x => x.BitDayId,
                        principalTable: "BitDays",
                        principalColumn: "BitDayId");
                    table.ForeignKey(
                        name: "FK_BitReservations_BitResources_BitResourceId",
                        column: x => x.BitResourceId,
                        principalTable: "BitResources",
                        principalColumn: "BitResourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BitResourceScheduleRanges",
                columns: table => new
                {
                    BitResourceScheduleRangeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BitClientId = table.Column<int>(type: "integer", nullable: false),
                    BitResourceId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    Payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitResourceScheduleRanges", x => x.BitResourceScheduleRangeId);
                    table.ForeignKey(
                        name: "FK_BitResourceScheduleRanges_BitClients_BitClientId",
                        column: x => x.BitClientId,
                        principalTable: "BitClients",
                        principalColumn: "BitClientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BitResourceScheduleRanges_BitResources_BitResourceId",
                        column: x => x.BitResourceId,
                        principalTable: "BitResources",
                        principalColumn: "BitResourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BitDays_ClientId_Date",
                table: "BitDays",
                columns: new[] { "ClientId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitReservations_BitClientId",
                table: "BitReservations",
                column: "BitClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BitReservations_BitDayId",
                table: "BitReservations",
                column: "BitDayId");

            migrationBuilder.CreateIndex(
                name: "IX_BitReservations_BitResourceId",
                table: "BitReservations",
                column: "BitResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_BitResources_BitClientId",
                table: "BitResources",
                column: "BitClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BitResources_BitResourceTypeId",
                table: "BitResources",
                column: "BitResourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BitResourceScheduleRanges_BitClientId_BitResourceId_StartDa~",
                table: "BitResourceScheduleRanges",
                columns: new[] { "BitClientId", "BitResourceId", "StartDate", "EndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitResourceScheduleRanges_BitResourceId_StartDate_EndDate",
                table: "BitResourceScheduleRanges",
                columns: new[] { "BitResourceId", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitReservations");

            migrationBuilder.DropTable(
                name: "BitResourceScheduleRanges");

            migrationBuilder.DropTable(
                name: "BitDays");

            migrationBuilder.DropTable(
                name: "BitResources");

            migrationBuilder.DropTable(
                name: "BitClients");

            migrationBuilder.DropTable(
                name: "BitResourceTypes");
        }
    }
}
