using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBitResourceScheduleRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitResourceScheduleRanges",
                columns: table => new
                {
                    BitResourceScheduleRangeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BitClientId = table.Column<int>(type: "int", nullable: false),
                    BitResourceId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    Payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                name: "IX_BitResourceScheduleRanges_BitClientId_BitResourceId_StartDate_EndDate",
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
                name: "BitResourceScheduleRanges");
        }
    }
}
