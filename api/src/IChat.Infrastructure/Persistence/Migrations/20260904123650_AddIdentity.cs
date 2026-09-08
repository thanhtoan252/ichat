using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IChat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // conversations.user_id từng là chuỗi tự do do client gửi — không có cách nào
            // ánh xạ nó sang một tài khoản có thật. Ứng dụng chưa phát hành và đây là
            // project showcase, nên hội thoại cũ bị xoá thay vì đoán chủ sở hữu cho chúng.
            // messages và message_citations đi theo bằng cascade đã có sẵn.
            migrationBuilder.Sql("DELETE FROM conversations;");

            // Drop rồi add thay vì ALTER TYPE: PostgreSQL không có cast ngầm từ text sang
            // uuid, nên ALTER COLUMN sẽ hỏng ngay cả khi bảng đã rỗng.
            migrationBuilder.DropColumn(name: "user_id", table: "conversations");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "conversations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Default all-zeros chỉ là thứ AddColumn cần để thêm cột NOT NULL vào bảng đã
            // rỗng. Giữ nó lại sẽ để một INSERT thô quên user_id âm thầm nhận id rác, nên
            // bỏ đi: từ giờ mọi hàng buộc phải nói rõ chủ sở hữu.
            migrationBuilder.Sql("ALTER TABLE conversations ALTER COLUMN user_id DROP DEFAULT;");

            // Dựng lại index mà DropColumn vừa kéo theo: mọi lần liệt kê hội thoại của
            // một người dùng đều đi qua nó.
            migrationBuilder.CreateIndex(
                name: "ix_conversations_user_id",
                table: "conversations",
                column: "user_id");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_expires_at",
                table: "refresh_tokens",
                columns: new[] { "user_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_users_user_name",
                table: "users",
                column: "user_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_conversations_users_user_id",
                table: "conversations",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_conversations_users_user_id",
                table: "conversations");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropColumn(name: "user_id", table: "conversations");

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "conversations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_user_id",
                table: "conversations",
                column: "user_id");
        }
    }
}
