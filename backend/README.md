# Penuel — Backend (Fase 1: Core)

API de administración de la **Comunidad Cristiana Penuel** (Ciudad Victoria, Tamaulipas).

**Construido hasta hoy:**
- **Core** — identidad, membresía, estructura organizacional y control de acceso.
- **Servicios y Cultos** — Escuela Dominical, Culto General, de Oración y de Jóvenes:
  asistencia con detalle, ofrenda, diezmo y maestros.

La fuente de verdad del diseño son `PENUEL_CORE_ARCHITECTURE.md` y
`PENUEL_SERVICES_ARCHITECTURE.md`. Este README solo explica cómo levantar y operar lo que
ya está construido.

## Stack

.NET 8 (LTS) · PostgreSQL en Supabase · EF Core 8 con Fluent API · CQRS con MediatR ·
FluentValidation · JWT + BCrypt · Swagger.

## Estructura

```
backend/
├── Penuel.Domain          entidades, enums, Result<T>/Error, RoleNames, PositionNames
│                          Entities/Services/  -> rama de Servicios
├── Penuel.Application     casos de uso (CQRS), validadores, behaviors
│                          Services/           -> rama de Servicios
├── Penuel.Infrastructure  EF Core, configuraciones, seguridad, migraciones
├── Penuel.WebApi          controladores, Swagger, middleware  (punto de entrada)
├── Penuel.Application.Tests   117 pruebas (xUnit + SQLite en memoria)
└── Penuel.Bootstrap       siembra del primer Pastor — USO ÚNICO
```

Las dependencias apuntan siempre hacia adentro: `WebApi` conoce todo, `Domain` no conoce nada.

## Puesta en marcha

Requiere el SDK de .NET 8 (`global.json` lo fija; con el SDK 10 instalado, se respeta el 8).

**1. Configurar los secretos** (nunca se versionan):

```bash
cd Penuel.WebApi
dotnet user-secrets set "ConnectionStrings:Penuel" "Host=aws-0-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Jwt:SecretKey" "<al menos 32 bytes>"
```

Usa el **Session pooler** (puerto 5432), no el Transaction pooler (6543): el modo transacción
no soporta sentencias preparadas, que Npgsql usa por omisión. La conexión directa
`db.<ref>.supabase.co` solo responde por IPv6 en el plan gratuito.

La aplicación **no arranca** si falta la clave JWT o mide menos de 32 bytes, y lo dice.

**2. Migraciones:**

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef database update --project Penuel.Infrastructure --startup-project Penuel.WebApi
```

Las herramientas de EF toman la conexión de la configuración de `Penuel.WebApi`, así que leen
los mismos user-secrets. **No** hay `IDesignTimeDbContextFactory`: se eliminó a propósito,
porque tenía prioridad sobre el host y hacía que EF ignorara los secretos.

La migración inicial siembra la iglesia, el rol `Pastor`, los 6 ministerios, las 4 sociedades
y los 4 cargos.

**3. Levantar la API:**

```bash
dotnet run --project Penuel.WebApi
```

Swagger queda en `/swagger` (solo en Development).

**4. Pruebas:**

```bash
dotnet test
```

## Siembra del primer Pastor (uso único)

La regla 7.5 exige el rol `Pastor` para otorgar roles, así que el primer Pastor no puede
crearse por la API. Para eso existe `Penuel.Bootstrap`:

```bash
dotnet run --project Penuel.Bootstrap
```

Pide correo y contraseña en la terminal (la contraseña no se muestra, se pide dos veces y no
se guarda en ningún lado: solo genera el hash BCrypt). Crea `Person`, `UserAccount`,
`Membership`, `UserRole` = Pastor y `PersonPosition` = Pastor en una sola transacción.

**Se niega a ejecutarse dos veces** y requiere una terminal interactiva. Una vez sembrado,
toda alta posterior se hace por la API.

## Endpoints

Salvo `/api/auth/*` y `/api/me/*`, **todos requieren el rol `Pastor`** (Sección 8.2).

| Método | Ruta | |
|---|---|---|
| POST | `/api/auth/login` | abierto |
| POST | `/api/auth/refresh` | abierto |
| GET | `/api/me/capabilities` | solo autenticado |
| POST | `/api/persons` | registra una persona |
| POST | `/api/persons/{id}/deactivate` · `/reactivate` | borrado lógico |
| POST | `/api/persons/{id}/user-account` | credenciales de acceso |
| POST | `/api/memberships` | membresía oficial |
| POST | `/api/roles/assign` · `/api/roles/revoke` | roles de sistema |
| POST | `/api/ministries` | |
| POST · DELETE | `/api/ministries/{id}/leader` | |
| POST | `/api/societies` | |
| POST · DELETE | `/api/societies/{id}/leader` | |
| POST | `/api/positions` | |
| POST | `/api/positions/{id}/holders` | admite varios titulares |
| DELETE | `/api/positions/{id}/holders/{personId}` | |
| GET | `/api/positions/executive-body` | computado, nunca almacenado |

### Servicios y Cultos

| Método | Ruta | |
|---|---|---|
| POST | `/api/service-types` | catálogo (Pastor) |
| POST | `/api/sunday-school/reports` | sesión + asistencias + ofrenda, en una transacción |
| GET | `/api/sunday-school/capture-context` | qué preguntar antes de capturar |
| PUT | `/api/sunday-school/attendances/{id}` | corregir una asistencia |
| POST · DELETE | `/api/sunday-school/teachers[/{id}]` | maestros (Pastor) |
| GET | `/api/sunday-school/reading-habits` | % con Biblia y promedio de capítulos |
| POST | `/api/service-sessions/general` | Culto General, de Oración o de Jóvenes |
| PUT | `/api/service-sessions/{id}/totals` | corregir totales |
| GET | `/api/service-sessions` | historial (alcance según quién pregunta) |
| POST · GET | `/api/service-sessions/{id}/tithes` | diezmo identificado |
| PUT | `/api/service-sessions/tithes/{id}` | corregir un diezmo |

**Quién puede qué:** Escuela Dominical requiere el rol `SundaySchoolRecorder` (o Pastor).
Todo lo que toca dinero requiere el **cargo** `Tesorero General` (o Pastor) — y como un cargo
no viaja en el JWT, esos endpoints llevan `[Authorize]` a secas y la decisión la toma
`AuthorizationBehavior` contra la base. El historial es mixto: quien solo captura Escuela
Dominical ve únicamente sus sesiones, nunca los cultos generales.

Los errores usan siempre la misma forma (`ProblemDetails` con un `code` estable):
400 validación · 401 sin sesión · 403 sin el rol · 404 no encontrado · 409 conflicto · 500 inesperado.

### Grupos Familiares

Las casas donde se reúne la iglesia entre semana. Es la primera rama cuya autorización no
depende de ningún rol ni cargo.

| Método | Ruta | Quién |
|---|---|---|
| `POST` | `/api/family-groups` | Pastor |
| `GET` | `/api/family-groups` | Pastor |
| `GET` | `/api/family-groups/{id}` | Pastor |
| `PUT` | `/api/family-groups/{id}/assignment` | Pastor |
| `PUT` | `/api/family-groups/{id}/status` | Pastor |
| `GET` | `/api/family-groups/mine` | Autenticado (devuelve solo lo suyo) |
| `GET` | `/api/family-groups/{id}/available-persons` | Anfitrión o Encargado del grupo |
| `POST` | `/api/family-groups/{id}/members` | Anfitrión o Encargado del grupo |
| `POST` | `/api/family-groups/{id}/members/register` | Anfitrión o Encargado del grupo |
| `DELETE` | `/api/family-groups/{id}/members/{personId}` | Anfitrión o Encargado del grupo |
| `POST` | `/api/family-groups/{id}/meetings` | Anfitrión o Encargado del grupo |
| `PUT` | `/api/family-groups/meetings/{id}` | Anfitrión o Encargado del grupo |

**El tercer patrón de autorización.** Hasta esta rama había dos: por `Role` (viaja en el JWT)
y por `Position` (se resuelve contra la base). Aquí aparece un tercero — la persona autenticada
se compara con un campo del PROPIO RECURSO (`FamilyGroup.HostPersonId` / `LeaderPersonId`)—, y
está nombrado: `IAuthorizeInHandler`. El pipeline garantiza que hay sesión; el permiso lo
resuelve el handler con `FamilyGroupPermissions.LoadOwnedAsync`, que es obligatorio.

Cualquier persona del directorio, **sin un solo permiso de sistema**, queda autorizada a operar
un grupo por el mero hecho de ser esa casa.

**Dos reglas que se confunden con facilidad y son contrarias:**

- Core: `ux_society_leaderships_active` va sobre `society_id` — el RECURSO limita cuántos
  titulares tiene, y una persona puede liderar varias sociedades.
- Esta rama: `ux_group_members_active_person` va sobre `person_id` A SECAS — la PERSONA limita
  en cuántos grupos está, que es como mucho uno en todo el sistema.

Añadirle `family_group_id` a ese índice "para que quede como los demás" borraría la regla en
silencio.

## Notas de seguridad

- **Revocación inmediata.** Cada petición revalida contra la base que la cuenta siga activa,
  que la `Person` siga en `Active` y que los roles del token sigan vigentes. Revocar un rol
  corta el acceso al instante, sin esperar a que expire el token.
- **Rotación de refresh tokens.** Cada renovación revoca el anterior. Presentar uno ya revocado
  cierra **todas** las sesiones de la cuenta.
- **Acceso REST de Supabase cerrado.** PostgREST expone el esquema `public` y concede
  privilegios a `anon`/`authenticated` por omisión — con la clave anónima se podían leer los
  hashes de contraseña. Se revocaron esos privilegios y se activó RLS sin políticas. Esto no es
  la capa de autorización por RLS que la Sección 5.4 descarta: la autorización sigue estando en
  `Penuel.Application`. Cualquier migración futura que cree tablas hereda la restricción.
- **Toda migración que cree tablas debe activar RLS explícitamente.** El
  `ALTER DEFAULT PRIVILEGES` cubre los `GRANT` de las tablas futuras, pero **no** activa RLS:
  eso es una acción por tabla y no se hereda. Ver el bloque al final del `Up()` de
  `AddServicesModule` — cópialo en la siguiente migración que agregue tablas.
- **Pendiente**, no bloqueante: la API se conecta como `postgres`. Vale la pena crear un rol de
  aplicación con privilegios acotados.

## Alcance de esta fase

**Construido:** personas, membresías, cuentas, roles, ministerios, sociedades, cargos, el
cómputo del Cuerpo Ejecutivo, y los servicios semanales con su asistencia, ofrenda y diezmo.

**No construido (ramas futuras):** Grupos Familiares y Contabilidad general.
