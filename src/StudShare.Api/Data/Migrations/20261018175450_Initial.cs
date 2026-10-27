using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudShare.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    FolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Extension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    author_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    author_middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    author_role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    author_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    author_middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    author_role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "group_invitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    invited_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by_first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invited_by_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invited_by_middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    invited_by_role = table.Column<string>(type: "text", nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Faculty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    StorageUsedBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    StorageLimitBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1000000000L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "history_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    actor_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    actor_middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    actor_role = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_history_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadAccess = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_group_invitations_GroupId_Email",
                table: "group_invitations",
                columns: new[] { "GroupId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "folders");

            migrationBuilder.DropTable(
                name: "group_invitations");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "history_events");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
