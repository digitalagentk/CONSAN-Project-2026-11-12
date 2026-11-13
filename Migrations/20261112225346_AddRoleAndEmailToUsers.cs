using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace text_2.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAndEmailToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) إضافة FullName
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // 2) إضافة IsVerified
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // 3) إضافة Role مع default "Manager" (وليس "")
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Manager");

            // 4) إصلاح البيانات القديمة: إيميلات فارغة أو null
            //    (نعطي كل مستخدم قديم إيميل فريد مؤقت)
            migrationBuilder.Sql(@"
                UPDATE Users
                SET Email = 'legacy_' + CAST(Id AS NVARCHAR(50)) + '@temp.local'
                WHERE Email IS NULL OR LTRIM(RTRIM(Email)) = '';
            ");

            // 5) إصلاح البيانات القديمة: FullName فارغ
            migrationBuilder.Sql(@"
                UPDATE Users
                SET FullName = Username
                WHERE FullName IS NULL OR LTRIM(RTRIM(FullName)) = '';
            ");

            // 6) إصلاح البيانات القديمة: Role فارغ
            migrationBuilder.Sql(@"
                UPDATE Users
                SET Role = 'Manager'
                WHERE Role IS NULL OR LTRIM(RTRIM(Role)) = '';
            ");

            // 7) الآن يمكن تحويل Email إلى unique
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // 8) إنشاء unique index على Email
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            // 9) CHECK constraint على Role
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN ('Admin', 'Manager')");

            // 10) CHECK constraint على IndustrialSettings (singleton)
            }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

                        migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}

