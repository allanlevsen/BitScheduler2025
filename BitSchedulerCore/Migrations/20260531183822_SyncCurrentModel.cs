using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BitDayId",
                table: "BitReservations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitReservations_BitDayId",
                table: "BitReservations",
                column: "BitDayId");

            migrationBuilder.AddForeignKey(
                name: "FK_BitReservations_BitDays_BitDayId",
                table: "BitReservations",
                column: "BitDayId",
                principalTable: "BitDays",
                principalColumn: "BitDayId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BitReservations_BitDays_BitDayId",
                table: "BitReservations");

            migrationBuilder.DropIndex(
                name: "IX_BitReservations_BitDayId",
                table: "BitReservations");

            migrationBuilder.DropColumn(
                name: "BitDayId",
                table: "BitReservations");
        }
    }
}
