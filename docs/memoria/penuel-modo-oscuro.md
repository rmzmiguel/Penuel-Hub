---
name: penuel-modo-oscuro
description: Cómo está construido el modo oscuro de penuel-hub y por qué no es una inversión
metadata:
  type: project
---

Añadido el 2026-08-28 sobre el sistema de [[penuel-design-system]]. Vive en el bloque
`:root[data-theme="dark"]` de `src/styles.css`, **una sola vez**: no hay bloque
`@media (prefers-color-scheme)` duplicado porque el JS resuelve "sistema" y siempre
escribe `data-theme` en `<html>`. Preferencia en `localStorage` bajo `penuel.tema`
(`claro` | `oscuro` | `sistema`), con un script en línea en `index.html` que la aplica
**antes del primer pintado**; esa clave está duplicada a propósito en los dos sitios.

Cuatro tokens semánticos de primer plano existen porque en oscuro **no todo se invierte
igual**: `--color-on-ink` (sobre el relleno `bg-ink`, que pasa a ser CLARO),
`--color-on-accent` (sobre naranja/verde/azul/rojo), `--color-on-panel` (sobre
`panel-ink`, que sigue oscuro) y `--color-scrim` (los velos, que deben seguir oscuros
en ambos temas).

**Why:** Miguel pidió contrastes *moderados*, nunca `#000000` ni el máximo posible.
Medido sobre los 96 textos de pantalla, el tema claro va de 2.30 a 19.05:1 y el oscuro
de 2.89 a 11.37:1 — el techo baja para quitar deslumbre y el suelo sube. El lienzo es
`#151A22` con margen por debajo (`#0C1014`) porque `bg-bone-deep` se usa 36 veces
DENTRO de tarjetas como hueco hundido.

**How to apply:** Nunca escribir `text-white` ni `bg-white` sólidos: usar el token
`on-*` que corresponda a la superficie de debajo. Los `bg-white/N` translúcidos sí son
válidos, pero **solo** sobre `panel-ink`. Verificar componiendo el píxel en un canvas:
Tailwind v4 emite los modificadores de opacidad como `oklab(... / a)` y un parser de
`rgb()` los mide mal. Las transiciones no avanzan si el panel del navegador está
oculto, así que los valores computados salen congelados: medir por captura o
desactivando transiciones.
