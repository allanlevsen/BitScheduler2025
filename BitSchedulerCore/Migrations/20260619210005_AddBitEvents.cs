using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBitEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitEvents",
                columns: table => new
                {
                    BitEventId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BitClientId = table.Column<int>(type: "integer", nullable: false),
                    BitResourceId = table.Column<int>(type: "integer", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartLatitude = table.Column<double>(type: "double precision", nullable: true),
                    StartLongitude = table.Column<double>(type: "double precision", nullable: true),
                    StartHexGridId = table.Column<int>(type: "integer", nullable: true),
                    EndAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EndLatitude = table.Column<double>(type: "double precision", nullable: true),
                    EndLongitude = table.Column<double>(type: "double precision", nullable: true),
                    EndHexGridId = table.Column<int>(type: "integer", nullable: true),
                    RequiresTransportation = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresReturnTransportation = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitEvents", x => x.BitEventId);
                    table.ForeignKey(
                        name: "FK_BitEvents_BitClients_BitClientId",
                        column: x => x.BitClientId,
                        principalTable: "BitClients",
                        principalColumn: "BitClientId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BitEvents_BitResources_BitResourceId",
                        column: x => x.BitResourceId,
                        principalTable: "BitResources",
                        principalColumn: "BitResourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BitEvents_BitClientId_BitResourceId_StartDateTime_EndDateTi~",
                table: "BitEvents",
                columns: new[] { "BitClientId", "BitResourceId", "StartDateTime", "EndDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BitEvents_BitResourceId_StartDateTime",
                table: "BitEvents",
                columns: new[] { "BitResourceId", "StartDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitEvents");
        }
    }
}
