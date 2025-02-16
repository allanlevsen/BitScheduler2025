using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitDays",
                columns: table => new
                {
                    BitDayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    BitsLow = table.Column<long>(type: "bigint", nullable: false),
                    BitsHigh = table.Column<long>(type: "bigint", nullable: false),
                    IsFree = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitDays", x => x.BitDayId);
                });

            migrationBuilder.CreateTable(
                name: "BitReservations",
                columns: table => new
                {
                    BitReservationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartBlock = table.Column<int>(type: "int", nullable: false),
                    SlotLength = table.Column<int>(type: "int", nullable: false),
                    BitDayId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitReservations", x => x.BitReservationId);
                    table.ForeignKey(
                        name: "FK_BitReservations_BitDays_BitDayId",
                        column: x => x.BitDayId,
                        principalTable: "BitDays",
                        principalColumn: "BitDayId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BitDays_ClientId_Date",
                table: "BitDays",
                columns: new[] { "ClientId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitReservations_BitDayId",
                table: "BitReservations",
                column: "BitDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitReservations");

            migrationBuilder.DropTable(
                name: "BitDays");
        }
    }
}
