import { useId, useState } from 'react'
import type { ReactNode } from 'react'

/* ═══════════════════════════════════════════════════════════════════════════
   Gráficas en SVG, sin librería.

   Todas dibujan en un espacio normalizado de 0-100 y estiran con
   `preserveAspectRatio="none"`. Eso las hace responder a cualquier caja sin
   medir nada en JavaScript; el truco para que el trazo no se deforme al
   estirar es `vector-effect="non-scaling-stroke"`, y los marcadores van en
   HTML posicionado en porcentajes por encima del SVG — un círculo SVG
   estirado sería un óvalo.
   ═══════════════════════════════════════════════════════════════════════════ */

export interface Point {
  /** Etiqueta del eje X: "Ene", "Dom 12". */
  label: string
  value: number
}

/**
 * Interpolación cúbica MONÓTONA (Fritsch-Carlson).
 *
 * Sin suavizado, una serie de seis puntos se ve como un rayo. Pero el spline
 * cardinal, que es el suavizado fácil, sobrepasa: entre dos meses que bajan
 * dibuja una curva que baja MÁS que el mes menor, y eso es una gráfica
 * mintiendo. La versión monótona limita las pendientes para que la curva nunca
 * salga del rango de sus dos extremos.
 */
function smoothPath(pts: { x: number; y: number }[]) {
  const n = pts.length
  if (n < 2) return ''
  if (n === 2) return `M ${pts[0].x} ${pts[0].y} L ${pts[1].x} ${pts[1].y}`

  // Pendientes de los segmentos.
  const dx: number[] = []
  const slope: number[] = []
  for (let i = 0; i < n - 1; i++) {
    dx.push(pts[i + 1].x - pts[i].x)
    slope.push((pts[i + 1].y - pts[i].y) / (pts[i + 1].x - pts[i].x))
  }

  // Tangente en cada punto: media de las pendientes vecinas, y CERO cuando
  // cambian de signo — ahí está el extremo local, y una tangente distinta de
  // cero es justo lo que produce el sobrepaso.
  const m: number[] = [slope[0]]
  for (let i = 1; i < n - 1; i++) {
    m.push(slope[i - 1] * slope[i] <= 0 ? 0 : (slope[i - 1] + slope[i]) / 2)
  }
  m.push(slope[n - 2])

  // Recorte de Fritsch-Carlson: mantiene la monotonía tramo a tramo.
  for (let i = 0; i < n - 1; i++) {
    if (slope[i] === 0) {
      m[i] = 0
      m[i + 1] = 0
      continue
    }
    const a = m[i] / slope[i]
    const b = m[i + 1] / slope[i]
    const h = Math.hypot(a, b)
    if (h > 3) {
      m[i] = ((3 / h) * a) * slope[i]
      m[i + 1] = ((3 / h) * b) * slope[i]
    }
  }

  let d = `M ${pts[0].x} ${pts[0].y}`
  for (let i = 0; i < n - 1; i++) {
    const t = dx[i] / 3
    d += ` C ${pts[i].x + t} ${pts[i].y + m[i] * t}, ${pts[i + 1].x - t} ${pts[i + 1].y - m[i + 1] * t}, ${pts[i + 1].x} ${pts[i + 1].y}`
  }
  return d
}

/**
 * Gráfica de área con relleno degradado y punto interactivo.
 *
 * Al pasar el dedo o el cursor, el punto más cercano se marca con una guía
 * vertical y una pastilla flotante con el valor. Es el mismo gesto en teclado:
 * las flechas mueven el punto activo, porque una gráfica que solo habla al
 * ratón no le sirve a la mitad de la gente.
 */
export function AreaChart({
  points,
  format,
  tone = 'clay',
  onInk = false,
  height = 'h-44 sm:h-52',
}: {
  points: Point[]
  format: (value: number) => string
  tone?: 'clay' | 'forest' | 'ochre' | 'lake'
  onInk?: boolean
  height?: string
}) {
  const gid = useId().replace(/:/g, '')
  const [active, setActive] = useState<number | null>(null)

  const colors = {
    clay:   { line: onInk ? 'var(--color-clay-bright)'   : 'var(--color-clay)',   glow: 'glow-clay' },
    forest: { line: onInk ? 'var(--color-forest-bright)' : 'var(--color-forest)', glow: 'glow-forest' },
    ochre:  { line: onInk ? 'var(--color-ochre-bright)'  : 'var(--color-ochre)',  glow: '' },
    lake:   { line: onInk ? 'var(--color-lake-bright)'   : 'var(--color-lake)',   glow: '' },
  }[tone]

  const values = points.map((p) => p.value)
  const max = Math.max(...values, 1)
  // El piso baja un 12% bajo el mínimo para que la serie no toque el borde
  // inferior: una línea pegada al marco se lee como un error de dibujo.
  const min = Math.min(...values)
  const floor = min - (max - min) * 0.28 - max * 0.06
  const span = max - floor || 1

  const xy = points.map((p, i) => ({
    x: points.length === 1 ? 50 : (i / (points.length - 1)) * 100,
    y: 100 - ((p.value - floor) / span) * 100,
  }))

  const line = smoothPath(xy)
  const area = `${line} L 100 100 L 0 100 Z`
  const shown = active ?? points.length - 1
  const marker = xy[shown]

  return (
    <div className="w-full">
      <div
        className={`relative w-full ${height}`}
        onPointerLeave={() => setActive(null)}
        onPointerMove={(e) => {
          const box = e.currentTarget.getBoundingClientRect()
          const ratio = (e.clientX - box.left) / box.width
          setActive(Math.max(0, Math.min(points.length - 1, Math.round(ratio * (points.length - 1)))))
        }}
      >
        <svg
          viewBox="0 0 100 100"
          preserveAspectRatio="none"
          className="absolute inset-0 size-full overflow-visible"
          aria-hidden="true"
        >
          <defs>
            <linearGradient id={`fill-${gid}`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={colors.line} stopOpacity={onInk ? 0.34 : 0.28} />
              <stop offset="100%" stopColor={colors.line} stopOpacity="0" />
            </linearGradient>
          </defs>

          {/* Retícula: tres líneas, no más. Cada línea extra es ruido que
              compite con el dato. */}
          {[25, 50, 75].map((y) => (
            <line
              key={y}
              x1="0" y1={y} x2="100" y2={y}
              stroke={onInk ? 'rgb(255 255 255 / 0.07)' : 'var(--color-line)'}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          <path d={area} fill={`url(#fill-${gid})`} />
          <path
            d={line}
            fill="none"
            stroke={colors.line}
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            className={`${onInk ? colors.glow : ''} animate-draw`}
            style={{ strokeDasharray: 1000, ['--draw-length' as string]: '1000' }}
          />
        </svg>

        {/* Guía vertical y punto activo, en HTML para que no se deformen. */}
        <div
          className="pointer-events-none absolute inset-y-0 w-0 border-l border-dashed
                     transition-[left] duration-150 ease-out"
          style={{
            left: `${marker.x}%`,
            borderColor: onInk ? 'rgb(255 255 255 / 0.28)' : 'var(--color-line-strong)',
          }}
        />
        {/* Anillo blanco con el borde del color de la serie: el punto se lee
            sobre el relleno degradado sin depender del contraste del tono. */}
        <div
          className="pointer-events-none absolute size-3.5 -translate-x-1/2 -translate-y-1/2 rounded-full
                     transition-[left,top] duration-150 ease-out"
          style={{
            left: `${marker.x}%`,
            top: `${marker.y}%`,
            background: onInk ? 'var(--color-ink-panel)' : 'var(--color-surface)',
            border: `3px solid ${colors.line}`,
          }}
        />

        {/* La pastilla se ancla al borde cuando el punto está en un extremo,
            para que no se salga de la tarjeta.

            Y se VOLTEA hacia abajo cuando el punto está tan alto que arriba no
            cabe: si no, un mes con un valor alto empuja la pastilla fuera de la
            gráfica y termina tapando la cifra principal del encabezado. */}
        <div
          className="pointer-events-none absolute transition-[left,top] duration-150 ease-out"
          style={{
            left: `${marker.x}%`,
            top: `calc(${marker.y}% ${marker.y < 30 ? '+' : '-'} 14px)`,
            transform: `translate(${
              marker.x > 78 ? '-100%' : marker.x < 22 ? '0%' : '-50%'
            }, ${marker.y < 30 ? '0%' : '-100%'})`,
          }}
        >
          <span className="inline-flex items-center gap-2.5 rounded-2xl bg-surface px-3.5 py-2.5
                           shadow-raised whitespace-nowrap">
            <span className="size-2 shrink-0 rounded-full" style={{ background: colors.line }} />
            <span className="flex flex-col items-start leading-tight">
              <span className="text-xs text-ink-soft">{points[shown].label}</span>
              <span className="font-numeral text-base font-medium text-ink">
                {format(points[shown].value)}
              </span>
            </span>
          </span>
        </div>
      </div>

      {/* Etiquetas del eje. En móvil se muestra una de cada dos para que no
          se encimen; nunca se rotan, que es ilegible en pantalla pequeña. */}
      <div className={`mt-3 flex justify-between text-xs ${onInk ? 'text-on-panel/40' : 'text-ink-faint'}`}>
        {points.map((p, i) => (
          <span
            key={p.label}
            className={`tabular ${i % 2 === 1 && points.length > 6 ? 'hidden sm:inline' : ''}
                        ${i === shown ? (onInk ? 'text-on-panel font-medium' : 'text-ink font-medium') : ''}`}
          >
            {p.label}
          </span>
        ))}
      </div>
    </div>
  )
}

export interface Slice {
  label: string
  value: number
  color: string
}

/**
 * Dona con hueco grande y separación entre segmentos.
 *
 * El hueco no es estético: ahí va la cifra total, que es lo que la gente lee
 * primero. Los segmentos llevan 2° de separación para que se distingan sin
 * depender de que los colores contrasten entre sí.
 */
export function Donut({
  slices,
  centerValue,
  centerLabel,
  size = 'size-44',
}: {
  slices: Slice[]
  centerValue: string
  centerLabel: string
  size?: string
}) {
  const total = slices.reduce((s, x) => s + x.value, 0) || 1
  const R = 41
  const C = 2 * Math.PI * R
  const GAP = 2.6

  /*
   * LA DONA NO ANIMA SU TRAZO, y es a propósito.
   *
   * Aquí `stroke-dashoffset` es la POSICIÓN de cada porción en el aro: la
   * primera arranca en 0, la segunda donde acaba la primera, y así. La
   * animación de trazo que sí usa la gráfica de área termina en
   * `stroke-dashoffset: 0` y con `fill-mode: both` se queda ahí para siempre,
   * de modo que pisaba el atributo y dejaba a todas las porciones arrancando
   * del mismo punto, encimadas: se veía la mayor y el resto quedaba debajo.
   *
   * El intento siguiente —crecer el `dasharray` desde 0 tras un
   * `requestAnimationFrame`— era peor: en una pestaña en segundo plano ese
   * callback no corre nunca y la gráfica se quedaba en blanco. Una decoración
   * no puede poder dejar sin dato.
   *
   * La entrada la hace el contenedor con opacidad y escala, que no toca ninguna
   * propiedad con significado y no depende de que nada se dispare.
   */
  let offset = 0

  return (
    <div className={`relative ${size} shrink-0`}>
      <svg viewBox="0 0 100 100" className="size-full -rotate-90 animate-pop" aria-hidden="true">
        <circle cx="50" cy="50" r={R} fill="none" stroke="var(--color-bone-deep)" strokeWidth="11" />
        {slices.map((s) => {
          const len = (s.value / total) * C
          const dash = Math.max(0, len - GAP)
          const el = (
            <circle
              key={s.label}
              cx="50" cy="50" r={R}
              fill="none"
              stroke={s.color}
              strokeWidth="11"
              strokeLinecap="round"
              strokeDasharray={`${dash} ${C - dash}`}
              strokeDashoffset={-offset}
            />
          )
          offset += len
          return el
        })}
      </svg>

      <div className="absolute inset-0 grid place-items-center text-center">
        <div>
          <p className="font-numeral text-3xl font-semibold leading-none">{centerValue}</p>
          <p className="eyebrow text-ink-soft mt-1.5">{centerLabel}</p>
        </div>
      </div>
    </div>
  )
}

/**
 * Leyenda de la dona. La etiqueta NO se trunca: "Escuela Dom…" deja al lector
 * adivinando de qué grupo se habla, y una segunda línea cuesta 20px.
 */
export function Legend({ slices, format }: { slices: Slice[]; format?: (v: number) => string }) {
  return (
    <ul className="w-full">
      {slices.map((s) => (
        <li key={s.label} className="flex items-baseline gap-3 py-1.5">
          <span className="size-2.5 shrink-0 rounded-full translate-y-[-1px]" style={{ background: s.color }} />
          <span className="flex-1 min-w-0 text-ink-soft leading-snug">{s.label}</span>
          <span className="shrink-0 font-numeral font-medium tabular">
            {format ? format(s.value) : s.value}
          </span>
        </li>
      ))}
    </ul>
  )
}

/**
 * Barra de progreso con el porcentaje montado dentro cuando cabe, y fuera
 * cuando no. Es el patrón que evita el problema clásico: un 4% con la etiqueta
 * dentro queda ilegible.
 */
export function Meter({
  label,
  value,
  max,
  format,
  color = 'var(--color-clay)',
  caption,
}: {
  label: string
  value: number
  max: number
  format?: (v: number) => string
  color?: string
  caption?: ReactNode
}) {
  const pct = Math.max(0, Math.min(100, (value / (max || 1)) * 100))
  const inside = pct > 22

  return (
    <div>
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-medium truncate">{label}</span>
        <span className="shrink-0 text-sm text-ink-soft tabular">
          {format ? `${format(value)} / ${format(max)}` : `${value} / ${max}`}
        </span>
      </div>

      <div className="mt-2 h-9 rounded-full bg-bone-deep overflow-hidden flex items-center">
        <div
          className="h-full rounded-full flex items-center justify-end px-3 origin-left animate-sweep"
          style={{ width: `${Math.max(pct, 3)}%`, background: color }}
        >
          {inside && (
            <span className="text-xs font-semibold text-on-accent tabular">{Math.round(pct)}%</span>
          )}
        </div>
        {!inside && (
          <span className="pl-3 text-xs font-semibold text-ink-soft tabular">{Math.round(pct)}%</span>
        )}
      </div>

      {caption && <p className="mt-2 text-sm text-ink-soft">{caption}</p>}
    </div>
  )
}

/**
 * Matriz de puntos: una celda por sesión, del más antiguo al más reciente.
 * Dice de un vistazo si hubo constancia, que es una pregunta que ninguna cifra
 * promedio responde.
 */
export function DotMatrix({
  cells,
  columns = 13,
  onInk = false,
}: {
  cells: { level: 0 | 1 | 2 | 3; title: string }[]
  columns?: number
  onInk?: boolean
}) {
  const levels = onInk
    ? ['bg-white/8', 'bg-forest-bright/30', 'bg-forest-bright/60', 'bg-forest-bright']
    : ['bg-bone-deep', 'bg-forest-soft', 'bg-forest/40', 'bg-forest']

  return (
    <div
      className="grid gap-1.5"
      style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}
      role="img"
      aria-label={`Constancia por sesión: ${cells.length} sesiones registradas`}
    >
      {cells.map((c, i) => (
        <span
          key={i}
          title={c.title}
          className={`aspect-square rounded-[0.4rem] ${levels[c.level]} animate-pop`}
          style={{ animationDelay: `${Math.min(i * 12, 400)}ms` }}
        />
      ))}
    </div>
  )
}

/**
 * Barras verticales compactas. Se usan donde la tendencia importa pero el
 * valor exacto no: la barra destacada lleva su cifra encima y las demás no.
 */
export function MiniBars({
  points,
  highlight,
  onInk = false,
  format,
}: {
  points: Point[]
  /** Índice de la barra que lleva el valor visible. Por omisión, la última. */
  highlight?: number
  onInk?: boolean
  format?: (v: number) => string
}) {
  const max = Math.max(...points.map((p) => p.value), 1)
  const hot = highlight ?? points.length - 1

  return (
    <div className="flex items-end gap-1.5 sm:gap-2 h-32">
      {points.map((p, i) => {
        const on = i === hot
        return (
          <div key={p.label} className="flex-1 min-w-0 h-full flex flex-col justify-end items-center gap-2">
            {on && format && (
              <span className={`text-xs font-semibold tabular whitespace-nowrap ${onInk ? 'text-on-panel' : 'text-ink'}`}>
                {format(p.value)}
              </span>
            )}
            <div
              title={`${p.label}: ${format ? format(p.value) : p.value}`}
              className={`w-full rounded-full origin-bottom transition-colors duration-300
                          ${on
                            ? onInk ? 'bg-clay-bright' : 'bg-clay'
                            : onInk ? 'bg-white/12' : 'bg-bone-deep'}`}
              style={{
                height: `${Math.max(6, (p.value / max) * 100)}%`,
                animation: `grow 0.7s cubic-bezier(0.22,1,0.36,1) ${i * 45}ms both`,
              }}
            />
            <span className={`text-xs ${onInk ? 'text-on-panel/40' : 'text-ink-faint'} ${on ? 'font-medium' : ''}`}>
              {p.label}
            </span>
          </div>
        )
      })}
    </div>
  )
}
