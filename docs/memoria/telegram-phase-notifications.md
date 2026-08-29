---
name: telegram-phase-notifications
description: Miguel quiere aviso por Telegram al cerrar cada fase y cuando haga falta una decisión suya.
metadata:
  type: feedback
---

Avisar por Telegram (Bot API `sendMessage`; `sendDocument` cuando el reporte pase de 4096 caracteres) al terminar cada fase de un trabajo por pasos, y también cada vez que surja una pregunta que lo bloquee — no solo al final de todo.

**Why:** se separa del teclado durante construcciones largas por pasos; una pregunta que nadie ve detiene el avance completo.

**How to apply:** pedirle el bot token y el chat id al inicio de la sesión (no se guardan aquí: un bot token es una credencial viva). Mantenerlos en el scratchpad de la sesión, nunca en el repositorio del proyecto. La estructura del mensaje queda a mi criterio: qué paso cerró, el resultado concreto, y qué decisión queda pendiente. Relacionado: [[penuel-core-phase]]
