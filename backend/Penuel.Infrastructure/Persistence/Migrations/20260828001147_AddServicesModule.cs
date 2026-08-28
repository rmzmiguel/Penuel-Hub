using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Penuel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServicesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    church_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requires_society_grouping = table.Column<bool>(type: "boolean", nullable: false),
                    collects_tithe = table.Column<bool>(type: "boolean", nullable: false),
                    attendance_customary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_types_churches_church_id",
                        column: x => x.church_id,
                        principalTable: "churches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sunday_school_teaching_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    society_id = table.Column<Guid>(type: "uuid", nullable: true),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sunday_school_teaching_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_sunday_school_teaching_assignments_persons_assigned_by_pers",
                        column: x => x.assigned_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sunday_school_teaching_assignments_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sunday_school_teaching_assignments_persons_revoked_by_perso",
                        column: x => x.revoked_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sunday_school_teaching_assignments_societies_society_id",
                        column: x => x.society_id,
                        principalTable: "societies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    society_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_offering = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_tithe = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    teacher_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    preacher_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_sessions_persons_created_by_person_id",
                        column: x => x.created_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_sessions_persons_preacher_person_id",
                        column: x => x.preacher_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_sessions_persons_teacher_person_id",
                        column: x => x.teacher_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_sessions_persons_updated_by_person_id",
                        column: x => x.updated_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_sessions_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_sessions_societies_society_id",
                        column: x => x.society_id,
                        principalTable: "societies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_attendances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    was_present = table.Column<bool>(type: "boolean", nullable: false),
                    was_punctual = table.Column<bool>(type: "boolean", nullable: true),
                    brought_bible = table.Column<bool>(type: "boolean", nullable: true),
                    chapters_read = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_attendances", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_attendances_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_attendances_persons_updated_by_person_id",
                        column: x => x.updated_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_attendances_service_sessions_service_session_id",
                        column: x => x.service_session_id,
                        principalTable: "service_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tithe_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_by_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tithe_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_tithe_entries_persons_created_by_person_id",
                        column: x => x.created_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tithe_entries_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tithe_entries_persons_updated_by_person_id",
                        column: x => x.updated_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tithe_entries_service_sessions_service_session_id",
                        column: x => x.service_session_id,
                        principalTable: "service_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "church_id", "created_at", "description", "is_system_role", "name" },
                values: new object[] { new Guid("20000000-0000-4000-8000-000000000002"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Puede levantar y corregir los reportes de Escuela Dominical de cualquier grupo. No implica ser maestro de ninguno.", true, "SundaySchoolRecorder" });

            migrationBuilder.InsertData(
                table: "service_types",
                columns: new[] { "id", "attendance_customary", "church_id", "collects_tithe", "created_at", "name", "requires_society_grouping" },
                values: new object[,]
                {
                    { new Guid("60000000-0000-4000-8000-000000000001"), true, new Guid("10000000-0000-4000-8000-000000000001"), false, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Escuela Dominical", true },
                    { new Guid("60000000-0000-4000-8000-000000000002"), false, new Guid("10000000-0000-4000-8000-000000000001"), true, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Culto General", false },
                    { new Guid("60000000-0000-4000-8000-000000000003"), false, new Guid("10000000-0000-4000-8000-000000000001"), false, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Culto de Oración", false },
                    { new Guid("60000000-0000-4000-8000-000000000004"), false, new Guid("10000000-0000-4000-8000-000000000001"), false, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Culto de Jóvenes", false }
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_attendances_person_id",
                table: "service_attendances",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_attendances_updated_by_person_id",
                table: "service_attendances",
                column: "updated_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ux_service_attendances_session_person",
                table: "service_attendances",
                columns: new[] { "service_session_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_sessions_created_by_person_id",
                table: "service_sessions",
                column: "created_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_sessions_preacher_person_id",
                table: "service_sessions",
                column: "preacher_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_sessions_session_date",
                table: "service_sessions",
                column: "session_date");

            migrationBuilder.CreateIndex(
                name: "ix_service_sessions_society_id",
                table: "service_sessions",
                column: "society_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_sessions_teacher_person_id",
                table: "service_sessions",
                column: "teacher_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_sessions_updated_by_person_id",
                table: "service_sessions",
                column: "updated_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ux_service_sessions_by_society",
                table: "service_sessions",
                columns: new[] { "service_type_id", "session_date", "society_id" },
                unique: true,
                filter: "society_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_service_sessions_without_society",
                table: "service_sessions",
                columns: new[] { "service_type_id", "session_date" },
                unique: true,
                filter: "society_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_service_types_church_id_name",
                table: "service_types",
                columns: new[] { "church_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sunday_school_teaching_assignments_assigned_by_person_id",
                table: "sunday_school_teaching_assignments",
                column: "assigned_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_sunday_school_teaching_assignments_person_id_revoked_at",
                table: "sunday_school_teaching_assignments",
                columns: new[] { "person_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sunday_school_teaching_assignments_revoked_by_person_id",
                table: "sunday_school_teaching_assignments",
                column: "revoked_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_sunday_school_teaching_assignments_society_id_revoked_at",
                table: "sunday_school_teaching_assignments",
                columns: new[] { "society_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tithe_entries_created_by_person_id",
                table: "tithe_entries",
                column: "created_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_tithe_entries_person_id",
                table: "tithe_entries",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_tithe_entries_updated_by_person_id",
                table: "tithe_entries",
                column: "updated_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ux_tithe_entries_session_person",
                table: "tithe_entries",
                columns: new[] { "service_session_id", "person_id" },
                unique: true);

            // ====================================================================================
            //  CANDADO DE ACCESO — obligatorio para toda tabla nueva, no solo estas.
            //
            //  Supabase publica el esquema public por HTTP vía PostgREST. Dejar una tabla sin
            //  RLS ahí significa que cualquiera con la clave anónima —que por diseño se publica
            //  en el frontend— puede leerla. Ya nos pasó con el Core: un GET a
            //  /rest/v1/user_accounts devolvía HTTP 200.
            //
            //  El ALTER DEFAULT PRIVILEGES que se aplicó entonces cubre los GRANT de las tablas
            //  futuras, PERO NO activa RLS: eso es una acción explícita por tabla y no se hereda.
            //  Por eso va aquí, dentro de la migración, y no como un paso manual que alguien
            //  pueda olvidar al desplegar en otro entorno.
            //
            //  Esto NO es la capa de autorización por RLS que la Sección 5.4 del Core descarta:
            //  no se crea ninguna política. RLS activado SIN políticas significa "denegar a todo
            //  el que no sea el dueño de la tabla", y la API .NET se conecta como ese dueño.
            // ====================================================================================
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    REVOKE ALL ON TABLE public.service_types                        FROM anon, authenticated;
                    REVOKE ALL ON TABLE public.service_sessions                     FROM anon, authenticated;
                    REVOKE ALL ON TABLE public.service_attendances                  FROM anon, authenticated;
                    REVOKE ALL ON TABLE public.tithe_entries                        FROM anon, authenticated;
                    REVOKE ALL ON TABLE public.sunday_school_teaching_assignments   FROM anon, authenticated;

                    ALTER TABLE public.service_types                      ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE public.service_sessions                   ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE public.service_attendances                ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE public.tithe_entries                      ENABLE ROW LEVEL SECURITY;
                    ALTER TABLE public.sunday_school_teaching_assignments ENABLE ROW LEVEL SECURITY;
                    """);
            }
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_attendances");

            migrationBuilder.DropTable(
                name: "sunday_school_teaching_assignments");

            migrationBuilder.DropTable(
                name: "tithe_entries");

            migrationBuilder.DropTable(
                name: "service_sessions");

            migrationBuilder.DropTable(
                name: "service_types");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: new Guid("20000000-0000-4000-8000-000000000002"));
        }
    }
}
