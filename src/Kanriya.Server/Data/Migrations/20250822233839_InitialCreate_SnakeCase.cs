using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kanriya.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_SnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "greet_logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, comment: "Unique identifier for the greet log entry"),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP", comment: "UTC timestamp when the greeting was logged"),
                    content = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, comment: "The greeting message content")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_greet_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_greet_logs_timestamp",
                table: "greet_logs",
                column: "timestamp",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "greet_logs");
        }
    }
}
