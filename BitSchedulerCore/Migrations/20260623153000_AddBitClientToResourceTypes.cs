using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitSchedulerCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBitClientToResourceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BitClientId",
                table: "BitResourceTypes",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "BitResourceTypes" AS rt
                SET "BitClientId" = source."BitClientId"
                FROM (
                    SELECT br."BitResourceTypeId", MIN(br."BitClientId") AS "BitClientId"
                    FROM "BitResources" AS br
                    GROUP BY br."BitResourceTypeId"
                ) AS source
                WHERE rt."BitResourceTypeId" = source."BitResourceTypeId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "BitResourceTypes"
                SET "BitClientId" = fallback."BitClientId"
                FROM (
                    SELECT MIN("BitClientId") AS "BitClientId"
                    FROM "BitClients"
                ) AS fallback
                WHERE "BitResourceTypes"."BitClientId" IS NULL
                  AND fallback."BitClientId" IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "BitClientId",
                table: "BitResourceTypes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BitResourceTypes_BitClientId",
                table: "BitResourceTypes",
                column: "BitClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BitResourceTypes_BitClientId_Name",
                table: "BitResourceTypes",
                columns: new[] { "BitClientId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BitResourceTypes_BitClients_BitClientId",
                table: "BitResourceTypes",
                column: "BitClientId",
                principalTable: "BitClients",
                principalColumn: "BitClientId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BitResourceTypes_BitClients_BitClientId",
                table: "BitResourceTypes");

            migrationBuilder.DropIndex(
                name: "IX_BitResourceTypes_BitClientId",
                table: "BitResourceTypes");

            migrationBuilder.DropIndex(
                name: "IX_BitResourceTypes_BitClientId_Name",
                table: "BitResourceTypes");

            migrationBuilder.DropColumn(
                name: "BitClientId",
                table: "BitResourceTypes");
        }
    }
}
