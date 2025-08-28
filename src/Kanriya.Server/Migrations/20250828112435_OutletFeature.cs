using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kanriya.Server.Migrations
{
    /// <inheritdoc />
    public partial class OutletFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_brands_name",
                table: "brands",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_brands_name",
                table: "brands");
        }
    }
}
