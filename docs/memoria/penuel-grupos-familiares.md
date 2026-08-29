---
name: penuel-grupos-familiares
description: La rama de Grupos Familiares y el tercer patrón de autorización que introdujo
metadata:
  type: project
---

Construida el 2026-08-28 desde `PENUEL_FAMILY_GROUPS_ARCHITECTURE.md`. Son las casas donde
se reúne la iglesia entre semana, y es la rama que dio origen al proyecto.

**El tercer patrón de autorización.** Antes había dos: por `Role` (JWT) y por `Position`
(contra la base). Esta rama añade comparar a la persona autenticada con un campo del PROPIO
recurso (`FamilyGroup.HostPersonId` / `LeaderPersonId`). Se llama `IAuthorizeInHandler` y vive
en `Penuel.Application/Abstractions`. El `AuthorizationBehavior` lo deja pasar tras comprobar
la sesión; el permiso lo resuelve el handler con `FamilyGroupPermissions.LoadOwnedAsync`.

**Why:** el documento afirmaba en su §5 que la rama "no tocaría el Core en absoluto", pero no
podía cumplirse: el behavior solo conocía dos marcadores y un test guardián exige que todo caso
de uso declare uno. Un `IRequireAuthorization` con listas vacías habría rechazado al Anfitrión
antes de llegar al handler. El propio §2.2 llamaba a esto "un tercer patrón", así que darle
nombre en la capa que autoriza era lo coherente.

**Dos índices contrarios que se confunden.** `ux_society_leaderships_active` (Core) va sobre
`society_id`: el recurso limita cuántos titulares tiene. `ux_group_members_active_person` (esta
rama) va sobre `person_id` A SECAS: la persona no puede estar en dos casas a la vez, en todo el
sistema. Añadirle `family_group_id` borraría la regla en silencio.

**How to apply:** el Anfitrión no es "un usuario limitado", es uno que no sabe que existe algo
más. En el frontend eso es una aplicación DISTINTA (`FamilyGroupApp`), sin dock ni ficha de
cargos vacía — `App.tsx` decide cuál montar. Nunca revelar a qué otro grupo pertenece alguien
(regla 7.5): el contrato `AvailablePerson` no tiene dónde decirlo, y así debe seguir.
Ver [[penuel-design-system]] y [[penuel-modo-oscuro]].
