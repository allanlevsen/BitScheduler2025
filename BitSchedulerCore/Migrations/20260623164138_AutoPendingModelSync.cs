using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class AutoPendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BitResourceTypes_BitClientId",
                table: "BitResourceTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BitResourceTypes_BitClientId",
                table: "BitResourceTypes",
                column: "BitClientId");
        }
    }
}
