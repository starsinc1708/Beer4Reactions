using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Beer4Reactions.BotLogic.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MediaGroupId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastMessageContent = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    StatisticsPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatisticsPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileUniqueId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    MediaGroupId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    FileSize = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_MediaGroups_MediaGroupId",
                        column: x => x.MediaGroupId,
                        principalTable: "MediaGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Photos_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TopPhotoId = table.Column<int>(type: "integer", nullable: true),
                    TopPhotoReactionCount = table.Column<int>(type: "integer", nullable: false),
                    TopMediaGroupId = table.Column<int>(type: "integer", nullable: true),
                    TopMediaGroupReactionCount = table.Column<int>(type: "integer", nullable: false),
                    TopUserId = table.Column<int>(type: "integer", nullable: true),
                    TopUserReactionCount = table.Column<int>(type: "integer", nullable: false),
                    TopReactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TopReactionUsageCount = table.Column<int>(type: "integer", nullable: false),
                    TotalPhotos = table.Column<int>(type: "integer", nullable: false),
                    TotalMediaGroups = table.Column<int>(type: "integer", nullable: false),
                    TotalReactions = table.Column<int>(type: "integer", nullable: false),
                    TotalActiveUsers = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyStatistics_MediaGroups_TopMediaGroupId",
                        column: x => x.TopMediaGroupId,
                        principalTable: "MediaGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MonthlyStatistics_Photos_TopPhotoId",
                        column: x => x.TopPhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MonthlyStatistics_Users_TopUserId",
                        column: x => x.TopUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    PhotoId = table.Column<int>(type: "integer", nullable: true),
                    MediaGroupId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reactions", x => x.Id);
                    table.CheckConstraint("CK_Reaction_Target", "(\"PhotoId\" IS NOT NULL AND \"MediaGroupId\" IS NULL) OR (\"PhotoId\" IS NULL AND \"MediaGroupId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Reactions_MediaGroups_MediaGroupId",
                        column: x => x.MediaGroupId,
                        principalTable: "MediaGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reactions_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaGroups_MediaGroupId_ChatId",
                table: "MediaGroups",
                columns: new[] { "MediaGroupId", "ChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatistics_ChatId_Year_Month",
                table: "MonthlyStatistics",
                columns: new[] { "ChatId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatistics_TopMediaGroupId",
                table: "MonthlyStatistics",
                column: "TopMediaGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatistics_TopPhotoId",
                table: "MonthlyStatistics",
                column: "TopPhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatistics_TopUserId",
                table: "MonthlyStatistics",
                column: "TopUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_ChatId_MessageId",
                table: "Photos",
                columns: new[] { "ChatId", "MessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_FileId",
                table: "Photos",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_MediaGroupId",
                table: "Photos",
                column: "MediaGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_UserId",
                table: "Photos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_MediaGroupId",
                table: "Reactions",
                column: "MediaGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_PhotoId",
                table: "Reactions",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId_PhotoId_MediaGroupId_Type",
                table: "Reactions",
                columns: new[] { "UserId", "PhotoId", "MediaGroupId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopMessages_ChatId_IsActive",
                table: "TopMessages",
                columns: new[] { "ChatId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ChatId",
                table: "Users",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TelegramUserId_ChatId",
                table: "Users",
                columns: new[] { "TelegramUserId", "ChatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyStatistics");

            migrationBuilder.DropTable(
                name: "Reactions");

            migrationBuilder.DropTable(
                name: "TopMessages");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "MediaGroups");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
