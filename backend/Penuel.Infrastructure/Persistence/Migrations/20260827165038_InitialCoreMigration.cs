using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Penuel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCoreMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "churches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    founded_year = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_churches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ministries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    church_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ministries", x => x.id);
                    table.ForeignKey(
                        name: "fk_ministries_churches_church_id",
                        column: x => x.church_id,
                        principalTable: "churches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "persons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    church_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persons", x => x.id);
                    table.ForeignKey(
                        name: "fk_persons_churches_church_id",
                        column: x => x.church_id,
                        principalTable: "churches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_persons_persons_created_by_person_id",
                        column: x => x.created_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_persons_persons_updated_by_person_id",
                        column: x => x.updated_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    church_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_executive_body = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_positions_churches_church_id",
                        column: x => x.church_id,
                        principalTable: "churches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    church_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    is_system_role = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_roles_churches_church_id",
                        column: x => x.church_id,
                        principalTable: "churches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "societies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    church_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_societies", x => x.id);
                    table.ForeignKey(
                        name: "fk_societies_churches_church_id",
                        column: x => x.church_id,
                        principalTable: "churches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    church_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at = table.Column<DateOnly>(type: "date", nullable: true),
                    registered_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_memberships", x => x.id);
                    table.ForeignKey(
                        name: "fk_memberships_churches_church_id",
                        column: x => x.church_id,
                        principalTable: "churches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_memberships_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_memberships_persons_registered_by_person_id",
                        column: x => x.registered_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ministry_leaderships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ministry_leaderships", x => x.id);
                    table.ForeignKey(
                        name: "fk_ministry_leaderships_ministries_ministry_id",
                        column: x => x.ministry_id,
                        principalTable: "ministries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ministry_leaderships_persons_assigned_by_person_id",
                        column: x => x.assigned_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ministry_leaderships_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ministry_leaderships_persons_revoked_by_person_id",
                        column: x => x.revoked_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_accounts_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "person_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_person_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_person_positions_persons_assigned_by_person_id",
                        column: x => x.assigned_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_person_positions_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_person_positions_persons_revoked_by_person_id",
                        column: x => x.revoked_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_person_positions_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "society_leaderships",
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
                    table.PrimaryKey("pk_society_leaderships", x => x.id);
                    table.ForeignKey(
                        name: "fk_society_leaderships_persons_assigned_by_person_id",
                        column: x => x.assigned_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_society_leaderships_persons_person_id",
                        column: x => x.person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_society_leaderships_persons_revoked_by_person_id",
                        column: x => x.revoked_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_society_leaderships_societies_society_id",
                        column: x => x.society_id,
                        principalTable: "societies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_person_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_persons_assigned_by_person_id",
                        column: x => x.assigned_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_persons_revoked_by_person_id",
                        column: x => x.revoked_by_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "churches",
                columns: new[] { "id", "address", "created_at", "currency", "founded_year", "name", "time_zone" },
                values: new object[] { new Guid("10000000-0000-4000-8000-000000000001"), "Manzana 8, Lote 2, Calle Enrique Higuera M, S/N, C.P. 87270, Colonia Ejido Loma Alta, Ciudad Victoria, Tamaulipas", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", 1997, "Comunidad Cristiana Penuel", "America/Mexico_City" });

            migrationBuilder.InsertData(
                table: "ministries",
                columns: new[] { "id", "church_id", "created_at", "description", "name" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-4000-8000-000000000001"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Campañas, evangelismo casa por casa y el proyecto UNO+UNO.", "Evangelismo" },
                    { new Guid("30000000-0000-4000-8000-000000000002"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Organiza los cultos, recibe a los visitantes y tiene bajo su cuidado a las Sociedades de Damas, Varones y Jóvenes.", "Comunión" },
                    { new Guid("30000000-0000-4000-8000-000000000003"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Escuela Bíblica Local y cursos de Catecúmenos, Prebautismal y Prematrimonial.", "Discipulado" },
                    { new Guid("30000000-0000-4000-8000-000000000004"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Grupos de alabanza, vida devocional y la agenda de veladas y vigilias.", "Adoración" },
                    { new Guid("30000000-0000-4000-8000-000000000005"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ujieres, eventos especiales, ACUPYHNAD y mantenimiento de los bienes.", "Servicio" },
                    { new Guid("30000000-0000-4000-8000-000000000006"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Atiende a los niños de la congregación. Ministerio propio e independiente, con encargado propio igual que los otros cinco.", "Ministerio Infantil" }
                });

            migrationBuilder.InsertData(
                table: "positions",
                columns: new[] { "id", "church_id", "created_at", "description", "is_executive_body", "name" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-4000-8000-000000000001"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Máxima autoridad. Preside la Asamblea General de miembros y el Cuerpo Ejecutivo.", true, "Pastor" },
                    { new Guid("50000000-0000-4000-8000-000000000002"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Oficio eclesiástico. Admite varios titulares activos a la vez (Sección 6.13).", true, "Diácono" },
                    { new Guid("50000000-0000-4000-8000-000000000003"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Registros y actas de la iglesia. Integra el Cuerpo Ejecutivo.", true, "Secretario General" },
                    { new Guid("50000000-0000-4000-8000-000000000004"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Administra la Ofrenda. El registro y control de los Diezmos corresponde directamente al Pastor (regla 7.12).", true, "Tesorero General" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "church_id", "created_at", "description", "is_system_role", "name" },
                values: new object[] { new Guid("20000000-0000-4000-8000-000000000001"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Control total del sistema: gestiona personas, membresías, roles, ministerios, sociedades y cargos.", true, "Pastor" });

            migrationBuilder.InsertData(
                table: "societies",
                columns: new[] { "id", "church_id", "created_at", "description", "name" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-4000-8000-000000000001"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bajo el cuidado general del Ministerio de Comunión.", "Damas" },
                    { new Guid("40000000-0000-4000-8000-000000000002"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bajo el cuidado general del Ministerio de Comunión.", "Varones" },
                    { new Guid("40000000-0000-4000-8000-000000000003"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bajo el cuidado general del Ministerio de Comunión.", "Jóvenes" },
                    { new Guid("40000000-0000-4000-8000-000000000004"), new Guid("10000000-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Agrupa a los niños de la congregación. Complementa al Ministerio Infantil sin duplicarlo: son dos registros distintos. El ministerio es el departamento funcional con su encargado; esta sociedad existe para agrupar la asistencia igual que Damas, Varones y Jóvenes.", "Infantil" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_memberships_church_id",
                table: "memberships",
                column: "church_id");

            migrationBuilder.CreateIndex(
                name: "ix_memberships_person_id",
                table: "memberships",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_memberships_registered_by_person_id",
                table: "memberships",
                column: "registered_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_memberships_status",
                table: "memberships",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_ministries_church_id_name",
                table: "ministries",
                columns: new[] { "church_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ministry_leaderships_assigned_by_person_id",
                table: "ministry_leaderships",
                column: "assigned_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_ministry_leaderships_person_id_revoked_at",
                table: "ministry_leaderships",
                columns: new[] { "person_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ministry_leaderships_revoked_by_person_id",
                table: "ministry_leaderships",
                column: "revoked_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ux_ministry_leaderships_active",
                table: "ministry_leaderships",
                column: "ministry_id",
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_positions_assigned_by_person_id",
                table: "person_positions",
                column: "assigned_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_positions_person_id_revoked_at",
                table: "person_positions",
                columns: new[] { "person_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_person_positions_revoked_by_person_id",
                table: "person_positions",
                column: "revoked_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ux_person_positions_active",
                table: "person_positions",
                columns: new[] { "position_id", "person_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_persons_church_id",
                table: "persons",
                column: "church_id");

            migrationBuilder.CreateIndex(
                name: "ix_persons_created_by_person_id",
                table: "persons",
                column: "created_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_persons_last_name_first_name",
                table: "persons",
                columns: new[] { "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "ix_persons_status",
                table: "persons",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_persons_updated_by_person_id",
                table: "persons",
                column: "updated_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_positions_is_executive_body",
                table: "positions",
                column: "is_executive_body");

            migrationBuilder.CreateIndex(
                name: "ux_positions_church_id_name",
                table: "positions",
                columns: new[] { "church_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_account_id_revoked_at",
                table: "refresh_tokens",
                columns: new[] { "user_account_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_roles_church_id_name",
                table: "roles",
                columns: new[] { "church_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_societies_church_id_name",
                table: "societies",
                columns: new[] { "church_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_society_leaderships_assigned_by_person_id",
                table: "society_leaderships",
                column: "assigned_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_society_leaderships_person_id_revoked_at",
                table: "society_leaderships",
                columns: new[] { "person_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_society_leaderships_revoked_by_person_id",
                table: "society_leaderships",
                column: "revoked_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ux_society_leaderships_active",
                table: "society_leaderships",
                column: "society_id",
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_accounts_person_id",
                table: "user_accounts",
                column: "person_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_accounts_email",
                table: "user_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_assigned_by_person_id",
                table: "user_roles",
                column: "assigned_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_revoked_by_person_id",
                table: "user_roles",
                column: "revoked_by_person_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_account_id_revoked_at",
                table: "user_roles",
                columns: new[] { "user_account_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ux_user_roles_active",
                table: "user_roles",
                columns: new[] { "user_account_id", "role_id" },
                unique: true,
                filter: "revoked_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "memberships");

            migrationBuilder.DropTable(
                name: "ministry_leaderships");

            migrationBuilder.DropTable(
                name: "person_positions");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "society_leaderships");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "ministries");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "societies");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "persons");

            migrationBuilder.DropTable(
                name: "churches");
        }
    }
}
