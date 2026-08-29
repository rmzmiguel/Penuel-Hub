# Penuel — Memoria del proyecto

Sistema de gestión para la **Comunidad Cristiana Penuel** (Ciudad Victoria, Tamaulipas).
Lo usa el Pbro. Fermín Ramírez Vázquez (el Pastor, ~70 años) y su equipo. Sustituye hojas
de papel. Español mexicano en TODA la interfaz, el código y los comentarios.

## Stack

- **Backend** — .NET 8 (SDK 8.0.420 fijado en `global.json`), Clean Architecture
  (Domain/Application/Infrastructure/WebApi), CQRS con MediatR, FluentValidation,
  `Result<T>`/`Error`/`ErrorType`, EF Core 8 + Npgsql + snake_case, **Fluent API only**
  (cero Data Annotations), JWT con `JsonWebTokenHandler`, BCrypt(12), rotación de refresh
  tokens con detección de reuso.
- **Base** — PostgreSQL en Supabase, proyecto `penuel-core`, ref `dkzcrbzkpupvzqiokcts`.
- **Frontend** — Vite 7 + React 19 + TypeScript + Tailwind v4 + React Router 7. Raíz del repo.
- **Pruebas** — xUnit + **SQLite en memoria** (nunca el proveedor InMemory: ignora índices
  únicos parciales y claves foráneas, que es justo lo que aquí hay que verificar). 155 en verde.

## Reglas que NO se rompen

1. **Cero borrado físico.** Todo se cierra con `RevokedAt` / `LeftAt` / `Status`. Nunca DELETE.
2. **Los tres ejes son independientes**: `Role` (permiso de software), `Position` (cargo
   eclesiástico) y liderar un `Ministry`/`Society`. No se infieren entre sí.
3. **Auditoría por `PersonId`**, no por `UserAccountId` (regla 7.4 del Core).
4. **Toda migración que cree tablas activa RLS explícitamente** y revoca `anon`/`authenticated`.
   Supabase expone por PostREST cualquier tabla nueva del esquema public. Ya pasó una vez.
5. **Nombres de rol/cargo centralizados** en `RoleNames` / `PositionNames`. Nunca texto suelto.
6. **Todo caso de uso declara su autorización.** Hay un test guardián estructural que lo exige.

## Los TRES patrones de autorización

Se deciden en **dos únicas puertas**: `AuthorizationBehavior` (Application) y el registro de
políticas de `Program.cs` (WebApi).

1. `IRequirePastor` — rol, viaja en el JWT.
2. `IRequireAuthorization` — roles **o** cargos; los cargos se resuelven contra la base.
3. `IAuthorizeInHandler` — **el recurso decide**. El pipeline solo garantiza sesión; el
   permiso lo resuelve el handler comparando a quien llama con un campo del propio recurso
   (`FamilyGroup.HostPersonId` / `LeaderPersonId`). Lo introdujo la rama de Grupos Familiares.

**Superusuario:** `RoleNames.Developer` ("Desarrollador") **salta** la autorización, no la
acumula. Un rol "equivalente a Pastor" habría que añadirlo a cada marcador nuevo y algún día
alguien olvidaría uno; un salto explícito no se puede olvidar. Miguel (el desarrollador) lo
tiene, sin cargo ni membresía.

## Dos índices contrarios que se confunden

- Core: `ux_society_leaderships_active` sobre `society_id` — el **recurso** limita titulares.
- Grupos Familiares: `ux_group_members_active_person` sobre `person_id` **a secas** — la
  **persona** no puede estar en dos casas a la vez, en todo el sistema.

Añadirle `family_group_id` al segundo "para que quede como los demás" borraría la regla.

## Reglas de UX innegociables

- Objetivos táctiles **56–64px**, no los 44 del mínimo de accesibilidad.
- Escala tipográfica **arranca en 16.5px**: se nota a los 70 años.
- **Ningún icono va solo**: siempre con texto o `aria-label` en el control.
- Confirmación explícita tras cada guardado. Cero navegación anidada, cero gestos ocultos.
- **La navegación se arma de `/api/me/capabilities`**, jamás de una lista fija.
- El **dock móvil admite 5 entradas como máximo**: a la sexta, "Personas" (55px a 13px) no
  cabe en un teléfono de 320px. Lo administrativo va al tablero.
- **Nunca truncar** nombres, correos ni instrucciones… salvo en filas de lista que abren un
  detalle donde el nombre completo sí se ve.

## Diseño

Sistema completo en `src/styles.css`. Lienzo gris, tarjetas blancas, `.pane` (filo de vidrio
de 1px con el canto superior aclarado; el tablero lo apaga con `data-panes="flat"`).

**Modo oscuro medido, no invertido.** Nunca negro puro. Contraste moderado a propósito: en
los 96 textos de pantalla el claro va de 2.30 a 19.05:1 y el oscuro de 2.89 a **11.37:1** —
el techo baja para quitar deslumbre. Cuatro tokens `on-*` porque **no todo se invierte igual**:
`bg-ink` (pastilla) se aclara y `panel-ink` (avisos) sigue oscuro.

Iconos: **Heroicons outline 24 v2.2.0**, trazos copiados en `Icon.tsx` (el paquete NO es
dependencia). Trazo por omisión 1.7.

`.rail` = carrusel a sangre con `mask-image` (no un degradado encima: la máscara deja ver el
fondo real sea cual sea). Se apaga en escritorio.

## Método de trabajo que el usuario valora

**Medir, no suponer.** Reproducir el fallo, leer los contratos reales, verificar contra la
base de verdad, y corregir las propias afirmaciones cuando estén mal. Varios fallos serios
salieron de aquí: `rounded-[--var]` de Tailwind v3 que no resolvía y dejaba los objetivos
táctiles en 0px; un frontend construido contra rutas inventadas que la demostración fabricaba;
tablas expuestas por PostgREST con la clave anónima.

**Avisar por Telegram** al terminar cada fase. El script vive en el scratchpad de la sesión;
el token NUNCA en el repo.

## Estado

Core, Servicios/Cultos, Grupos Familiares y administración de permisos: **terminados,
probados y desplegados contra Supabase**. 155 tests.

Pendiente: pruebas de despliegue en Render (backend) y Vercel (frontend).

**Render no tiene entorno nativo de .NET**, así que el backend va por Docker. El
`backend/Dockerfile` está probado en local corriendo como lo hará Render —con `PORT`
inyectado— y haciendo login real contra Supabase. En Render: *New → Web Service*,
**Root Directory `backend`**, runtime **Docker**, plan Free. Con el Root Directory
puesto, el `Dockerfile` se detecta solo y el frontend de la raíz se ignora.

## Variables de entorno

Backend (Render). En local viven en `dotnet user-secrets`; en producción, como variables.
El doble guion bajo es la convención de .NET para anidar (`ConnectionStrings:Penuel`).

| Variable | Qué es |
|---|---|
| `ConnectionStrings__Penuel` | Cadena de Postgres. Usar el **Session pooler** de Supabase, no la conexión directa. |
| `Jwt__SecretKey` | Mínimo 32 bytes o la aplicación no arranca. |
| `Jwt__Issuer`, `Jwt__Audience` | Solo si se cambian los de `appsettings.json`. |
| `FRONTEND_URL` | Origen exacto de Vercel, con esquema y sin barra final. Admite varios separados por coma. Sin ella no se registra ninguna política de CORS. |
| — | El puerto NO se configura: `Program.cs` lee `PORT`, que Render inyecta, y escucha ahí. |

Frontend (Vercel):

| Variable | Qué es |
|---|---|
| `VITE_API_URL` | URL del backend en Render, sin barra final. Vacía en local: el proxy de Vite sirve `/api` desde el mismo origen. |

Los dos valores secretos están en la máquina; se leen con:
`cd backend/Penuel.WebApi && dotnet user-secrets list`
