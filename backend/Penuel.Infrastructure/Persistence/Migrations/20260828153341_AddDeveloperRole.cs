using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penuel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeveloperRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "church_id", "created_at", "description", "is_system_role", "name" },
                values: new object[] { new Guid("20000000-0000-4000-8000-000000000003"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Acceso irrestricto para el mantenimiento técnico del sistema. No es un cargo de la iglesia y no implica membresía ni liderazgo.", true, "Desarrollador" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000003"));
        }
    }
}
