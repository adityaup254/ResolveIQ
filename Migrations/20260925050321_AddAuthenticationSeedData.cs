using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveIQ.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Technicians",
                columns: new[] { "Id", "Email", "FullName", "Phone" },
                values: new object[] { 3, "technician@resolveiq.com", "Sample Tech", "0000000000" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "FullName", "Password", "Role" },
                values: new object[] { "admin@resolveiq.com", "Admin User", "6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=", "Admin" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "FullName", "Password" },
                values: new object[] { "user@resolveiq.com", "Normal User", "PnwZV2SIhigW8TtRLKzz5LqX3ZckPqC9airRZC2GunI=" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FullName", "Password", "Role" },
                values: new object[] { 3, "technician@resolveiq.com", "Sample Tech", "qzgyLx5MpgYEUiTpD9MDP45ZC/FZF63v2t33iQygPZk=", "Technician" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Technicians",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "FullName", "Password", "Role" },
                values: new object[] { "john@example.com", "John Doe", "Password123!", "User" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "FullName", "Password" },
                values: new object[] { "jane@example.com", "Jane Smith", "Password123!" });
        }
    }
}
