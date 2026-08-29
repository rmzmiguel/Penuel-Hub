---
name: penuel-backend-pendientes
description: Los dos GET que le faltan al backend .NET para que la administración funcione de verdad
metadata:
  type: project
---

La pantalla de personas y permisos (`src/screens/admin/`) usa rutas de escritura que ya
existen, pero necesita dos GET que el backend .NET todavía NO expone:
`GET /api/persons/directory` → `AdminPerson[]` y `GET /api/admin/catalogs` →
`AdminCatalogs`. Ambos tipos están declarados en `src/api/admin.ts` como especificación.

`AdminPerson` es deliberadamente `MyCapabilities` más el estado de la persona, su cuenta
y su membresía — la misma pregunta que ya resuelve `/api/me/capabilities`, hecha sobre
otro.

**Why:** Sin esos dos endpoints, la administración solo corre en el modo demostración
(`src/demo/`, se entra por el botón al pie del login). Es lo primero que hay que
construir del lado .NET.

**How to apply:** Al implementarlos en `Penuel.Application`, respetar los nombres y la
forma de `src/api/admin.ts` para no tocar el frontend. Ver [[penuel-design-system]].
