using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddHexGridTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HexGridVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AreaName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OriginLatitude = table.Column<double>(type: "double precision", nullable: false),
                    OriginLongitude = table.Column<double>(type: "double precision", nullable: false),
                    HexRadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    MinLatitude = table.Column<double>(type: "double precision", nullable: false),
                    MaxLatitude = table.Column<double>(type: "double precision", nullable: false),
                    MinLongitude = table.Column<double>(type: "double precision", nullable: false),
                    MaxLongitude = table.Column<double>(type: "double precision", nullable: false),
                    MaxPrecomputedRingDistance = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HexGridVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HexGridCells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HexGridVersionId = table.Column<int>(type: "integer", nullable: false),
                    Q = table.Column<int>(type: "integer", nullable: false),
                    R = table.Column<int>(type: "integer", nullable: false),
                    CenterLatitude = table.Column<double>(type: "double precision", nullable: false),
                    CenterLongitude = table.Column<double>(type: "double precision", nullable: false),
                    HexRadiusMeters = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AreaName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HexGridCells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HexGridCells_HexGridVersions_HexGridVersionId",
                        column: x => x.HexGridVersionId,
                        principalTable: "HexGridVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HexGridCellVertices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HexGridCellId = table.Column<int>(type: "integer", nullable: false),
                    VertexOrder = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HexGridCellVertices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HexGridCellVertices_HexGridCells_HexGridCellId",
                        column: x => x.HexGridCellId,
                        principalTable: "HexGridCells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HexGridNeighbors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HexGridCellId = table.Column<int>(type: "integer", nullable: false),
                    NeighborHexGridCellId = table.Column<int>(type: "integer", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HexGridNeighbors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HexGridNeighbors_HexGridCells_HexGridCellId",
                        column: x => x.HexGridCellId,
                        principalTable: "HexGridCells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HexGridNeighbors_HexGridCells_NeighborHexGridCellId",
                        column: x => x.NeighborHexGridCellId,
                        principalTable: "HexGridCells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HexGridSearchRings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HexGridCellId = table.Column<int>(type: "integer", nullable: false),
                    NearbyHexGridCellId = table.Column<int>(type: "integer", nullable: false),
                    RingDistance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HexGridSearchRings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HexGridSearchRings_HexGridCells_HexGridCellId",
                        column: x => x.HexGridCellId,
                        principalTable: "HexGridCells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HexGridSearchRings_HexGridCells_NearbyHexGridCellId",
                        column: x => x.NearbyHexGridCellId,
                        principalTable: "HexGridCells",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HexGridCells_AreaName_IsActive",
                table: "HexGridCells",
                columns: new[] { "AreaName", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_HexGridCells_HexGridVersionId_Q_R",
                table: "HexGridCells",
                columns: new[] { "HexGridVersionId", "Q", "R" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HexGridCells_Id",
                table: "HexGridCells",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_HexGridCells_IsActive",
                table: "HexGridCells",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_HexGridCellVertices_HexGridCellId_VertexOrder",
                table: "HexGridCellVertices",
                columns: new[] { "HexGridCellId", "VertexOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HexGridNeighbors_HexGridCellId_Direction",
                table: "HexGridNeighbors",
                columns: new[] { "HexGridCellId", "Direction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HexGridNeighbors_NeighborHexGridCellId",
                table: "HexGridNeighbors",
                column: "NeighborHexGridCellId");

            migrationBuilder.CreateIndex(
                name: "IX_HexGridSearchRings_HexGridCellId_NearbyHexGridCellId",
                table: "HexGridSearchRings",
                columns: new[] { "HexGridCellId", "NearbyHexGridCellId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HexGridSearchRings_HexGridCellId_RingDistance",
                table: "HexGridSearchRings",
                columns: new[] { "HexGridCellId", "RingDistance" });

            migrationBuilder.CreateIndex(
                name: "IX_HexGridSearchRings_NearbyHexGridCellId",
                table: "HexGridSearchRings",
                column: "NearbyHexGridCellId");

            migrationBuilder.CreateIndex(
                name: "IX_HexGridVersions_AreaName_IsActive",
                table: "HexGridVersions",
                columns: new[] { "AreaName", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HexGridCellVertices");

            migrationBuilder.DropTable(
                name: "HexGridNeighbors");

            migrationBuilder.DropTable(
                name: "HexGridSearchRings");

            migrationBuilder.DropTable(
                name: "HexGridCells");

            migrationBuilder.DropTable(
                name: "HexGridVersions");
        }
    }
}
