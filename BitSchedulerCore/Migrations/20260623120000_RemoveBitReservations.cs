using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBitReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitReservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitReservations",
                columns: table => new
                {
                    BitReservationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BitClientId = table.Column<int>(type: "integer", nullable: false),
                    BitDayId = table.Column<int>(type: "integer", nullable: true),
                    BitResourceId = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    SlotLength = table.Column<int>(type: "integer", nullable: false),
                    StartBlock = table.Column<int>(type: "integer", nullable: false),
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
        }
    }
}
