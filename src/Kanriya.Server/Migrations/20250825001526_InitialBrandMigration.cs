using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kanriya.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialBrandMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    schema_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    database_user = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    encrypted_password = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_brands", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject_template = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    html_body_template = table.Column<string>(type: "text", nullable: true),
                    text_body_template = table.Column<string>(type: "text", nullable: true),
                    default_from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    default_from_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pending_users",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verification_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_mail_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mail_driver = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_port = table.Column<int>(type: "integer", nullable: true),
                    smtp_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_password = table.Column<string>(type: "text", nullable: true),
                    smtp_encryption = table.Column<string>(type: "text", nullable: true),
                    smtp_from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_from_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    api_key = table.Column<string>(type: "text", nullable: true),
                    api_secret = table.Column<string>(type: "text", nullable: true),
                    api_domain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    api_region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    daily_limit = table.Column<int>(type: "integer", nullable: true),
                    sent_today = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_mail_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    profile_picture_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    cc_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    bcc_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    html_body = table.Column<string>(type: "text", nullable: true),
                    text_body = table.Column<string>(type: "text", nullable: true),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_data = table.Column<string>(type: "text", nullable: true),
                    sender_type = table.Column<string>(type: "text", nullable: false),
                    sender_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    mail_driver = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    status = table.Column<string>(type: "text", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    last_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_reason = table.Column<string>(type: "text", nullable: true),
                    scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_outbox", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_outbox_email_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "email_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_logs_email_outboxes_email_outbox_id",
                        column: x => x.email_outbox_id,
                        principalTable: "email_outbox",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_brands_created_at",
                table: "brands",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_brands_database_user",
                table: "brands",
                column: "database_user",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_brands_is_active",
                table: "brands",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_brands_owner_id",
                table: "brands",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_brands_schema_name",
                table: "brands",
                column: "schema_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_created_at",
                table: "email_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_email_outbox_id",
                table: "email_logs",
                column: "email_outbox_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_priority",
                table: "email_outbox",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_processing",
                table: "email_outbox",
                columns: new[] { "status", "priority", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_scheduled_for",
                table: "email_outbox",
                column: "scheduled_for");

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_status",
                table: "email_outbox",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_email_outbox_template_id",
                table: "email_outbox",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_templates_name",
                table: "email_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pending_users_created_at",
                table: "pending_users",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_pending_users_email_unique",
                table: "pending_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pending_users_token_expires",
                table: "pending_users",
                column: "token_expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_pending_users_token_unique",
                table: "pending_users",
                column: "verification_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_mail_settings_user_id",
                table: "user_mail_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_assigned_at",
                table: "user_roles",
                column: "assigned_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role",
                table: "user_roles",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id_role_unique",
                table: "user_roles",
                columns: new[] { "user_id", "role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                table: "users",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_users_email_unique",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "pending_users");

            migrationBuilder.DropTable(
                name: "user_mail_settings");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "email_outbox");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "email_templates");
        }
    }
}
