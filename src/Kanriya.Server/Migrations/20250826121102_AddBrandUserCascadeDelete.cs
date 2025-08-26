using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kanriya.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandUserCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "fk_brands_users_owner_id",
                table: "brands",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_brands_users_owner_id",
                table: "brands");
        }
    }
}
