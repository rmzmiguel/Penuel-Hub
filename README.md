# Penuel

Sistema de gestión de la **Comunidad Cristiana Penuel** (Ciudad Victoria, Tamaulipas).

```
penuel-hub/
├── backend/     API .NET 8 + PostgreSQL en Supabase   (ver backend/README.md)
└── src/         Frontend React + TypeScript + Vite
```

## Levantar todo

Hacen falta dos procesos. **El backend primero**, porque el frontend le habla desde el arranque.

```bash
# 1) API — en una terminal
cd backend/Penuel.WebApi && dotnet run
```

```bash
# 2) Frontend — en otra terminal, desde la raíz
npm install && npm run dev
```

La app queda en `http://localhost:5173`, y también en `http://<tu-ip-local>:5173` desde
cualquier dispositivo de la misma red WiFi — el `host: true` de `vite.config.ts` es lo que
la ata a todas las interfaces en vez de solo a localhost. Vite imprime la dirección de red
al arrancar. Vite hace de proxy de `/api` hacia la API, así que no hay CORS ni URLs
distintas entre desarrollo y producción. Si tu API corre en otro puerto:

```bash
PENUEL_API=http://localhost:5000 npm run dev
```

Otros comandos: `npm run build` (compilación de producción), `npm run typecheck`.

## Ver la interfaz sin levantar la API

La pantalla de acceso tiene, al pie, **«Entrar a la demostración»**. Levanta una sesión
falsa contra `src/demo/`, que responde en memoria con un directorio de 26 personas, los 6
ministerios, las 4 sociedades y año y medio de sesiones con estacionalidad real. Las
escrituras se aplican —otorgar un permiso se ve y se siente igual que contra el backend—,
pero se pierden al recargar.

Solo se activa desde ese botón: el interceptor de `client.ts` comprueba que la sesión sea
la de demostración, así que ninguna sesión real pasa por ahí. Para tirar todo el modo
demostración basta con borrar `src/demo/` y las diez líneas que lo llaman en `client.ts`.

## Para quién está hecho, y qué se sigue de eso

El usuario principal es el Pastor: una persona mayor, desde su teléfono. El resto son
personas de 40-50+ años, salvo quien lleva la tesorería, que trabaja desde una laptop.
De ahí salen las reglas de la interfaz:

- **Todo control pulsable es una píldora** de 64px de alto en acciones primarias y 56px
  en secundarias (`--spacing-touch-lg` / `--spacing-touch`). El mínimo de accesibilidad
  son 44px; aquí no se usa el mínimo.
- **Ningún icono carga el significado solo.** Siempre lleva texto al lado. La única
  excepción es `IconButton`, y por eso su `label` es obligatorio: se convierte en
  `aria-label` y en `title`.
- **Nada se trunca si es información que la persona necesita** — ni nombres, ni correos,
  ni instrucciones. Un nombre cortado a la mitad no identifica a nadie. Lo que no cabe
  envuelve; lo que envuelve gana altura, que sobra.
- **Los flujos de captura no tienen barra de navegación.** Viven fuera del armazón: una
  salida lateral a la vista invita a abandonar un reporte a medias.
- **Confirmación de pantalla completa** después de guardar un reporte. Nunca un regreso
  silencioso.
- **Cero gestos que haya que descubrir.** Solo toques directos y scroll.

## El sistema de diseño

Todo vive en `src/styles.css`, en un solo bloque `@theme` que Tailwind v4 convierte en
utilidades. Es minimalismo de presencia, no de ausencia: la jerarquía la hacen la escala,
el espacio y el contraste, nunca la decoración. Tres reglas:

**Las tarjetas son blancas y no llevan borde.** Se separan del lienzo gris por diferencia
de tono, no por línea — con ocho tarjetas en pantalla, los bordes forman una rejilla que
compite con los datos. La sombra es tan tenue que no se ve, solo se siente. El borde queda
para lo que de verdad informa: los campos de entrada, que sin él no se distinguirían de la
tarjeta que los contiene.

**El negro es un estado, no una superficie.** Lo lleva la entrada activa de la navegación,
la píldora seleccionada del periodo y la acción principal. Ninguna pantalla tiene un panel
oscuro; la única superficie de tinta que queda son los avisos flotantes.

**El color solo existe dentro de los datos.** Naranja, azul, verde y violeta son cuatro
categorías de gráfica que también se distinguen en escala de grises. Fuera de ahí, el
naranja aparece exactamente dos veces: en el monograma de la marca y en el degradado del
acceso. La interfaz alrededor es negra, blanca y gris.

La tipografía es **DM Sans** en todo — una sola familia geométrica. La jerarquía es de
tamaño y peso, nunca de familia: dos tipografías en un tablero se leen como dos productos
pegados. `.font-display` y `.font-numeral` no cambian de familia, solo aprietan el
tracking; a 44px, la misma tipografía con -3% de espaciado se lee como titular, y sin
apretarla se lee como cuerpo agrandado — que es el error que hace que un tablero parezca
una plantilla.

Píldora completa (`--radius-control`) en todo lo pulsable; de 28px a 36px en lo que
contiene.

## La navegación se arma sola

Al entrar, la app llama a `/api/me/capabilities` y construye la navegación con lo que esa
persona puede hacer **hoy** — la barra lateral en escritorio y el dock flotante en el
teléfono salen de la misma lista (`useNav` en `AppShell.tsx`). No hay ninguna lista de
pantallas escrita a mano ni ninguna suposición sobre quién es el Pastor. Si le retiran un
rol, la entrada desaparece la próxima vez que abra la app, sin tocar código.

Lo mismo con los datos: el tablero enseña cifras de la congregación al Pastor y cifras de
captura a quien captura, decidido por las mismas capacidades.

Las guardas de ruta (`Guard` en `App.tsx`) son comodidad de navegación, **no seguridad**:
quien decide de verdad es el backend, que revalida roles y cargos contra la base en cada
petición.

## El tablero no inventa endpoints

`src/lib/stats.ts` deriva TODAS las métricas del tablero de `GET /api/service-sessions`,
que ya devuelve fecha, tipo, ofrenda, diezmo y asistencia de cada sesión. No hay ninguna
ruta de agregados y es a propósito: además de no inventar una, evita que el tablero y el
historial puedan contradecirse, porque leen exactamente la misma lista. El alcance ya
viene resuelto desde el backend.

## Lo que le falta al backend

La pantalla de personas y permisos usa las rutas de escritura que ya existen
(`/api/roles/assign`, `/api/positions/{id}/holders`, `/api/ministries/{id}/leader`…), pero
necesita dos GET que todavía no están. `src/api/admin.ts` los declara con su tipo, que es
la especificación de lo que deben devolver:

| Ruta | Devuelve |
|---|---|
| `GET /api/persons/directory` | `AdminPerson[]` — es `MyCapabilities` más el estado de la persona, su cuenta y su membresía. La misma pregunta que ya sabe responder `/api/me/capabilities`, hecha sobre otro. |
| `GET /api/admin/catalogs` | `AdminCatalogs` — roles, cargos, ministerios y sociedades, cada grupo con su líder actual. |

Mientras no existan, esas dos pantallas solo funcionan en modo demostración.

## Estructura

```
src/
├── api/          client.ts (fetch, sesión, renovación) · endpoints.ts · admin.ts · types.ts
├── auth/         AuthProvider + usePermissions
├── components/   AppShell (barra lateral + dock) · Button · Card · Screen · Field
│   └── ui/       Avatar · Chip · Charts · PageHeader · SearchField · Sheet · Toast · Toggle
├── screens/      Login · Dashboard · Historial
│   ├── admin/           directorio, grupos y el panel de permisos del Pastor
│   ├── sundaySchool/    los tres escenarios de captura + formulario de reporte
│   └── generalService/  reporte de culto + diezmos identificados
├── demo/         backend en memoria — solo para ver la interfaz sin la API
├── lib/          format.ts · stats.ts (métricas del tablero) · useAsync.ts
└── styles.css    el sistema de diseño completo
```

`src/api/types.ts` transcribe los contratos reales de `Penuel.Application`. Al tocar un
DTO del backend, ese archivo es lo primero que hay que actualizar.

## Notas de implementación que ahorran tiempo

- **Una sola renovación de token en vuelo.** Tres peticiones que caducan juntas dispararían
  tres refresh; el segundo presentaría un token ya rotado y el backend lo leería como
  **reuso**, cerrando todas las sesiones del usuario. Ver `refreshing` en `client.ts`.
- **En iOS conviven tres alturas de viewport y no se pueden mezclar.** `position: fixed;
  inset: 0` mide el viewport de MAQUETA (en iOS, el alto con las barras ocultas); `dvh` mide
  el DINÁMICO, que encoge con la barra a la vista. Un panel `92dvh` con `mt-auto` dentro de
  un contenedor `inset: 0` deja su borde inferior ~90px por debajo de lo visible, dentro de
  la máscara redondeada con la que Safari recorta el contenido junto a la barra flotante —
  y el panel parece tener un `border-radius` inferior gigante que en CSS no existe. La
  solución son dos líneas de CSS, `.sheet-frame`: `top: 0` + `height: 100dvh`.
- **No midas el viewport con `visualViewport.offsetTop`.** Fue el primer intento de arreglar
  lo anterior y salió peor: al final de la página ese valor es POSITIVO —la ventana visual
  queda desplazada dentro de la de maqueta— así que el panel se abría a media pantalla, y
  además el valor se quedaba obsoleto porque el bloqueo del fondo no dispara ningún evento
  de `visualViewport`. `dvh` ya dice lo mismo sin medir nada.
- **Los paneles se montan en `document.body` por un portal.** `position: fixed` deja de
  medir contra la ventana en cuanto cualquier antecesor tiene `transform`, `filter`,
  `backdrop-filter` o `will-change`. Estos paneles se declaran a seis niveles de
  profundidad, dentro de pantallas que ya animan con `transform`; el portal es lo que
  impide que una animación añadida tres niveles más arriba los rompa.
- **`overflow: hidden` en el `body` no detiene el rebote elástico de iOS.** El fondo sigue
  arrastrándose y con él se desplazan los elementos `fixed`. El bloqueo real es fijar el
  `body` y devolverle el scroll al soltar (`useScrollLock`), más `overscroll-contain` en el
  área que sí scrollea. Va en **`useLayoutEffect`, no en `useEffect`**: fijar el `body`
  colapsa la altura del documento, y si eso pasa después de pintar, iOS conserva la capa ya
  pintada del panel pero calcula los toques contra la geometría nueva — el panel se ve en un
  sitio y responde en otro.
- **`presentCount === 0` no significa "no fue nadie".** Sale de contar filas en
  `ServiceAttendances`, y el reporte de culto no escribe ninguna — su DTO
  (`GeneralServiceReport`) ni siquiera tiene campo de asistencias. Para General, Oración y
  Jóvenes el cero es estructural. Por eso los resúmenes omiten la casilla cuando es cero y
  los promedios se dividen entre `attendanceSessions`, no entre `sessionCount`: contra el
  total, la media caía a la cuarta parte de la real.
- **En la dona, `stroke-dashoffset` es POSICIÓN, no progreso.** La animación de trazo que usa
  la gráfica de área termina en `stroke-dashoffset: 0` y con `fill-mode: both` se queda ahí,
  así que pisaba el atributo y apilaba todas las porciones en el mismo punto de arranque:
  se veía la mayor y el resto quedaba debajo. La dona entra con opacidad y escala; no anima
  su trazo.
- **`reload()` parpadea; `refresh()` no.** `useAsync` expone las dos a propósito: `reload`
  enciende `loading` y es para cuando la persona pidió los datos; `refresh` es callada y
  esperable, y es la que va después de guardar algo. Usar `reload` tras cada interruptor era
  lo que vaciaba y remontaba el panel de administración en cada toque.
- **Los enums viajan como enteros.** `SundaySchoolCaptureMode` es `0 | 1 | 2`, no texto.
- **Las fechas `DateOnly` se formatean sin pasar por UTC**, que correría el día completo.
- **En Tailwind v4 la sintaxis `rounded-[--mi-var]` no resuelve.** Los tokens del tema
  generan utilidades por su espacio de nombres: `--radius-card` → `rounded-card`,
  `--spacing-touch` → `min-h-touch`.
- **Los elementos de grid necesitan `min-w-0`** o no se encogen por debajo de su contenido
  y desbordan la pantalla en el teléfono. Basta con un `truncate` —que implica
  `white-space: nowrap`— en cualquier descendiente para que el ancho mínimo del elemento
  sea el del texto completo.
- **Un `position: relative` con `z-index` crea un contexto de apilamiento** y encierra
  dentro los `fixed` descendientes. Por eso `main` no lleva `z-index` y el grano del
  fondo vive en `z-index: -1`: con `main` en `z-10`, los paneles `fixed z-50` quedaban
  por debajo del dock (`z-40`, hermano del contexto).
- **La interpolación de las gráficas es monótona**, no un spline cardinal. El cardinal
  sobrepasa: entre dos meses que bajan dibuja una curva que baja más que el mes menor,
  y eso es una gráfica mintiendo.

## Lo que cubre

Inicio de sesión · tablero con las métricas de la congregación · administración de
personas, permisos, cargos y liderazgos · reporte completo de Escuela Dominical con los
tres escenarios de captura · reporte de culto con diezmos identificados · historial.

**Todavía no:** alta de personas y de cuentas de acceso desde la interfaz (las rutas
existen; falta la pantalla), Grupos Familiares ni Contabilidad general.
