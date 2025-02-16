using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdatesForResourceAndClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BitClients",
                columns: table => new
                {
                    BitClientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitClients", x => x.BitClientId);
                });

            migrationBuilder.CreateTable(
                name: "BitResourceTypes",
                columns: table => new
                {
                    BitResourceTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitResourceTypes", x => x.BitResourceTypeId);
                });

            migrationBuilder.CreateTable(
                name: "BitResources",
                columns: table => new
                {
                    BitResourceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BitResourceTypeId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BitClientId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_BitResources_BitClientId",
                table: "BitResources",
                column: "BitClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BitResources_BitResourceTypeId",
                table: "BitResources",
                column: "BitResourceTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BitResources");

            migrationBuilder.DropTable(
                name: "BitClients");

            migrationBuilder.DropTable(
                name: "BitResourceTypes");
        }
    }
}
