using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ByteBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    ProfileBio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProfileWebsite = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProfileTwitterHandle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProfileLinkedInProfile = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProfileAvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProfileInterests = table.Column<string>(type: "text", nullable: false),
                    ProfileLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailVerificationToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ResetPasswordToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ResetPasswordTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ContentUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TotalPages = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PricePerPageAmount = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    PricePerPageCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, defaultValue: "USD"),
                    PricePerHourAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    PricePerHourCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, defaultValue: "USD"),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Books_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Readings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PagesRead = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TimeSpentMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastPageRead = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ChargedAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ChargedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, defaultValue: "USD"),
                    ChargeType = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Readings_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Readings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    PaymentType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RefundedAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    RefundedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, defaultValue: "USD"),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadingSessionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Readings_ReadingSessionId",
                        column: x => x.ReadingSessionId,
                        principalTable: "Readings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferrerId = table.Column<int>(type: "integer", nullable: false),
                    ReferredUserId = table.Column<int>(type: "integer", nullable: false),
                    ReferralCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CommissionEarnedAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    CommissionEarnedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, defaultValue: "USD"),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false, defaultValue: 0.10m),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TriggerPaymentId = table.Column<int>(type: "integer", nullable: true),
                    TriggerAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    TriggerCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true, defaultValue: "USD"),
                    CommissionPaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Payments_TriggerPaymentId",
                        column: x => x.TriggerPaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferrerId",
                        column: x => x.ReferrerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_AuthorId",
                table: "Books",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Category",
                table: "Books",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Books_IsActive",
                table: "Books",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Books_IsPublished",
                table: "Books",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Books_PublishedAt",
                table: "Books",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Status",
                table: "Books",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title_AuthorId",
                table: "Books",
                columns: new[] { "Title", "AuthorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookId",
                table: "Payments",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExternalTransactionId",
                table: "Payments",
                column: "ExternalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProcessedAt",
                table: "Payments",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider",
                table: "Payments",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReadingSessionId",
                table: "Payments",
                column: "ReadingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId_BookId",
                table: "Payments",
                columns: new[] { "UserId", "BookId" });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_BookId",
                table: "Readings",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_EndTime",
                table: "Readings",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_IsCompleted",
                table: "Readings",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_StartTime",
                table: "Readings",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_Status",
                table: "Readings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_UserId",
                table: "Readings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_UserId_BookId",
                table: "Readings",
                columns: new[] { "UserId", "BookId" });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_UserId_StartTime",
                table: "Readings",
                columns: new[] { "UserId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ConvertedAt",
                table: "Referrals",
                column: "ConvertedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_IsActive",
                table: "Referrals",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferralCode",
                table: "Referrals",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredUserId",
                table: "Referrals",
                column: "ReferredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerId",
                table: "Referrals",
                column: "ReferrerId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Status",
                table: "Referrals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TriggerPaymentId",
                table: "Referrals",
                column: "TriggerPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Type",
                table: "Referrals",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailVerificationToken",
                table: "Users",
                column: "EmailVerificationToken");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ResetPasswordToken",
                table: "Users",
                column: "ResetPasswordToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Readings");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
