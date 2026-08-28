using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penuel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSocietyMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "society_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    society_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_society_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_society_memberships_persons_assigned_by_person_id",
                        column: x => x.assigned_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_society_memberships_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_society_memberships_persons_revoked_by_person_id",
                        column: x => x.revoked_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_society_memberships_societies_society_id",
                        column: x => x.society_id,
                        principalTable: "societies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_society_memberships_assigned_by_person_id",
                table: "society_memberships",
                column: "assigned_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_society_memberships_person_id",
                table: "society_memberships",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_society_memberships_revoked_by_person_id",
                table: "society_memberships",
                column: "revoked_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_society_memberships_society_id_revoked_at",
                table: "society_memberships",
                columns: new[] { "society_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ux_society_memberships_active",
                table: "society_memberships",
                columns: new[] { "society_id", "person_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            // Candado de acceso obligatorio para toda tabla nueva — ver la nota del README.
            // Los GRANT los cubre el ALTER DEFAULT PRIVILEGES aplicado en su momento, pero
            // activar RLS es por tabla y NO se hereda: sin esta línea, society_memberships
            // nacería legible desde PostgREST con la clave anónima.
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    REVOKE ALL ON TABLE public.society_memberships FROM anon, authenticated;
                    ALTER TABLE public.society_memberships ENABLE ROW LEVEL SECURITY;
                    """);
            }
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "society_memberships");
        }
    }
}
