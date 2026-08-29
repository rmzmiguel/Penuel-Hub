---
name: penuel-design-system
description: El lenguaje visual de penuel-hub y las tres reglas que lo gobiernan
metadata:
  type: project
---

El frontend de penuel-hub se rediseñó el 2026-08-28. El sistema vive entero en
`src/styles.css` (bloque `@theme` de Tailwind v4) y descansa en tres reglas:

1. **Tarjetas blancas sin borde** sobre lienzo gris `#F1F2F5`. Se separan por tono, no por
   línea. Sombras casi invisibles. El borde solo en campos de entrada.
2. **El negro es un estado, no una superficie**: lo lleva la nav activa, la píldora de
   periodo seleccionada y la acción principal. No hay paneles oscuros.
3. **El color solo dentro de los datos** (naranja, azul, verde, violeta). Fuera de ahí, el
   naranja aparece dos veces: el monograma y el degradado del acceso.

Tipografía: **DM Sans** en todo. `.font-display` / `.font-numeral` solo aprietan el
tracking, no cambian de familia.

**Why:** Miguel rechazó dos entregas — la primera por simple, la segunda (papel hueso,
grano, serif Fraunces, panel de tinta) por exagerada. La referencia que aprobó es un
tablero tipo Panze Studio: claro, geométrico, minimalismo de presencia y no de ausencia.
El punto de equilibrio se inclina hacia lo rico, pero con la interfaz en negro/blanco/gris.

**How to apply:** Antes de añadir cualquier pantalla, leer `src/styles.css` y reutilizar
las primitivas de `src/components/ui/`. Nunca truncar nombres, correos ni instrucciones.
Mobile-first, y los flujos de captura van FUERA del `AppShell`.
Ver [[penuel-backend-pendientes]].
