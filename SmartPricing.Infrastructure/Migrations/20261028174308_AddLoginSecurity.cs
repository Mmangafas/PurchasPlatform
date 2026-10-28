using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEnd",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // The original seed hash did not match the documented default password (Admin@123), so the
            // seeded admin could never sign in. Replace it with a valid hash and force a change at first
            // login. An admin password that was already changed is left untouched.
            migrationBuilder.Sql(
                "UPDATE [Users] SET [PasswordHash] = '$2a$11$ghAD8klr2jISKJMfrLypKePXE.MtbAvpNQj6Hh2pDwNWala2UIe5a', " +
                "[MustChangePassword] = 1 " +
                "WHERE [UserID] = 1 AND [PasswordHash] = '$2a$11$K5DSFLEWBfB3rOZoizqJuuHk1HHxGh4GH2YFMoaqkCuF.MxhZLqrS'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");
        }
    }
}
