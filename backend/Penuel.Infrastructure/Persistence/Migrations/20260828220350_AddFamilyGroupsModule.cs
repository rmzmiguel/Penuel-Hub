using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penuel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyGroupsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "family_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    church_id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leader_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    default_meeting_day_of_week = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_family_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_family_groups_churches_church_id",
                        column: x => x.church_id,
                        principalTable: "churches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_family_groups_persons_created_by_person_id",
                        column: x => x.created_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_family_groups_persons_host_person_id",
                        column: x => x.host_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_family_groups_persons_leader_person_id",
                        column: x => x.leader_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_family_groups_persons_updated_by_person_id",
                        column: x => x.updated_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "family_group_meetings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meeting_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_offering = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_by_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_family_group_meetings", x => x.id);
                    table.ForeignKey(
                        name: "fk_family_group_meetings_family_groups_family_group_id",
                        column: x => x.family_group_id,
                        principalTable: "family_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_family_group_meetings_persons_created_by_person_id",
                        column: x => x.created_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_family_group_meetings_persons_updated_by_person_id",
                        column: x => x.updated_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateOnly>(type: "date", nullable: false),
                    left_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_by_person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_members_family_groups_family_group_id",
                        column: x => x.family_group_id,
                        principalTable: "family_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_members_persons_created_by_person_id",
                        column: x => x.created_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_group_members_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "family_group_attendances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_group_meeting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    was_present = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_family_group_attendances", x => x.id);
                    table.ForeignKey(
                        name: "fk_family_group_attendances_family_group_meetings_family_group",
                        column: x => x.family_group_meeting_id,
                        principalTable: "family_group_meetings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_family_group_attendances_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_family_group_attendances_person_id",
                table: "family_group_attendances",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ux_family_group_attendances_person",
                table: "family_group_attendances",
                columns: new[] { "family_group_meeting_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_family_group_meetings_created_by_person_id",
                table: "family_group_meetings",
                column: "created_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_group_meetings_updated_by_person_id",
                table: "family_group_meetings",
                column: "updated_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ux_family_group_meetings_date",
                table: "family_group_meetings",
                columns: new[] { "family_group_id", "meeting_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_family_groups_church_id",
                table: "family_groups",
                column: "church_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_groups_created_by_person_id",
                table: "family_groups",
                column: "created_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_family_groups_host_person_id_status",
                table: "family_groups",
                columns: new[] { "host_person_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_family_groups_leader_person_id_status",
                table: "family_groups",
                columns: new[] { "leader_person_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_family_groups_updated_by_person_id",
                table: "family_groups",
                column: "updated_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_members_created_by_person_id",
                table: "group_members",
                column: "created_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_members_family_group_id_left_at",
                table: "group_members",
                columns: new[] { "family_group_id", "left_at" });

            migrationBuilder.CreateIndex(
                name: "ux_group_members_active_person",
                table: "group_members",
                column: "person_id",
                unique: true,
                filter: "left_at IS NULL");

            // ====================================================================================
            //  RLS OBLIGATORIO (README del backend): toda migración que cree tablas la activa.
            //
            //  Supabase expone por PostgREST cualquier tabla nueva del esquema public. Sin esto,
            //  las cuatro tablas de esta rama quedarían legibles con la clave anónima desde
            //  internet — direcciones de casas incluidas, que es de lo más sensible del sistema.
            //
            //  Se activa RLS SIN NINGUNA POLÍTICA, que en Postgres significa denegar a todos, y
            //  además se revocan los grants. La autorización real vive en la aplicación
            //  (Sección 5.4 del Core); esto solo cierra la puerta lateral.
            // ====================================================================================
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    REVOKE ALL ON TABLE public.family_groups             FROM anon, authenticated;
                    REVOKE ALL ON TABLE public.group_members             FROM anon, authenticated;
                    REVOKE ALL ON TABLE public.family_group_meetings     FROM anon, authenticated;
                    REVOKE ALL ON TABLE public.family_group_attendances  FROM anon, authenticated;

                    ALTER TABLE public.family_groups            ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE public.group_members            ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE public.family_group_meetings    ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE public.family_group_attendances ENABLE ROW LEVEL SECURITY;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_group_attendances");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "family_group_meetings");

            migrationBuilder.DropTable(
                name: "family_groups");
        }
    }
}
