using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ByteBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_ProcessedAt",
                table: "Payments",
                columns: new[] { "Status", "ProcessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Books_AverageRating",
                table: "Books",
                column: "AverageRating");

            migrationBuilder.CreateIndex(
                name: "IX_Books_CreatedAt",
                table: "Books",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ReviewCount",
                table: "Books",
                column: "ReviewCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CreatedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_ProcessedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Books_AverageRating",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_CreatedAt",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_ReviewCount",
                table: "Books");
        }
    }
}
