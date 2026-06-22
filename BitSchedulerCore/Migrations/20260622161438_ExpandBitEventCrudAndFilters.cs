using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class ExpandBitEventCrudAndFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "BitEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduleBitsReserved",
                table: "BitEvents",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitEvents_BitClientId_EventType_StartDateTime",
                table: "BitEvents",
                columns: new[] { "BitClientId", "EventType", "StartDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BitEvents_BitClientId_EventType_StartDateTime",
                table: "BitEvents");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "BitEvents");

            migrationBuilder.DropColumn(
                name: "ScheduleBitsReserved",
                table: "BitEvents");
        }
    }
}
