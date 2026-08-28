import type { ReactNode } from 'react'
import { Icon } from './Icon'
import type { IconName } from './Icon'

export type Tone = 'plain' | 'clay' | 'forest' | 'ochre' | 'lake' | 'ink'

/*
 * Las tarjetas se separan del lienzo por TONO —blancas sobre gris— y encima
 * llevan `.pane`: un filo de vidrio de 1px con el canto superior aclarado. El
 * filo es deliberadamente casi invisible; su trabajo es que la tarjeta tenga
 * borde físico, no que se vea una línea.
 *
 * El tablero se lo quita entero con `data-panes="flat"` en su raíz, porque ahí
 * ocho filos seguidos sí se leen como rejilla.
 *
 * El tono no tiñe la tarjeta: solo la pastilla del icono y el texto de acento.
 * Una tarjeta de color entera compite con el dato que contiene.
 */
export const tones: Record<Tone, { wrap: string; badge: string; text: string; dot: string }> = {
  plain:  { wrap: 'bg-surface',            badge: 'bg-bone-deep text-ink-soft',    text: 'text-ink',        dot: 'bg-ink-faint' },
  clay:   { wrap: 'bg-surface',            badge: 'bg-clay-soft text-clay-deep',   text: 'text-clay-deep',  dot: 'bg-clay' },
  forest: { wrap: 'bg-surface',            badge: 'bg-forest-soft text-forest',    text: 'text-forest',     dot: 'bg-forest' },
  ochre:  { wrap: 'bg-surface',            badge: 'bg-ochre-soft text-ochre-deep', text: 'text-ochre-deep', dot: 'bg-ochre' },
  lake:   { wrap: 'bg-surface',            badge: 'bg-lake-soft text-lake',        text: 'text-lake',       dot: 'bg-lake' },
  ink:    { wrap: 'panel-ink text-on-panel', badge: 'bg-white/10 text-on-panel',   text: 'text-on-panel',   dot: 'bg-clay-bright' },
}

export function Card({
  children,
  tone = 'plain',
  className = '',
}: {
  children: ReactNode
  tone?: Tone
  className?: string
}) {
  return (
    <section className={`rounded-card ${tones[tone].wrap} shadow-card
                         ${tone === 'ink' ? '' : 'pane'} ${className}`}>
      {children}
    </section>
  )
}

/**
 * Encabezado interno de tarjeta: etiqueta pequeña, título grande y espacio a la
 * derecha para un control. Se repite en cada bloque del tablero, y tenerlo aquí
 * es lo que evita que cada tarjeta invente su propia jerarquía.
 */
export function CardHead({
  eyebrow,
  title,
  action,
  onInk = false,
}: {
  eyebrow?: string
  title: string
  action?: ReactNode
  onInk?: boolean
}) {
  return (
    <div className="flex items-start justify-between gap-4">
      <div className="min-w-0">
        {eyebrow && (
          <p className={`eyebrow ${onInk ? 'text-on-panel/50' : 'text-ink-soft'}`}>{eyebrow}</p>
        )}
        <h2
          className={`font-display text-xl sm:text-2xl font-medium leading-tight
                      ${eyebrow ? 'mt-0.5' : ''} ${onInk ? 'text-on-panel' : ''}`}
        >
          {title}
        </h2>
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  )
}

/**
 * Tarjeta grande y pulsable — el bloque del bento.
 * Toda la superficie es el objetivo táctil, no solo un botón dentro.
 */
export function ActionCard({
  title,
  description,
  icon,
  tone = 'clay',
  onClick,
  meta,
}: {
  title: string
  description: string
  icon: IconName
  tone?: Tone
  onClick: () => void
  /** Dato de una palabra a la derecha: "3 pendientes", "Domingo". */
  meta?: string
}) {
  const t = tones[tone]
  return (
    <button
      type="button"
      onClick={onClick}
      className={`group relative w-full h-full flex flex-col text-left rounded-panel ${t.wrap} shadow-card
                  ${tone === 'ink' ? '' : 'pane'}
                  p-6 transition-[transform,box-shadow] duration-300 ease-[cubic-bezier(0.22,1,0.36,1)]
                  hover:shadow-raised hover:-translate-y-0.5 active:scale-[0.99] active:translate-y-0`}
    >
      {/* Icono y título en una sola fila para que el título NO se parta: en un
          teléfono de 375px, "Escuela Dominical" en dos líneas hacía que la
          tarjeta creciera al doble sin decir nada más. */}
      <span className="flex items-center gap-4">
        <span className={`shrink-0 grid place-items-center size-12 rounded-2xl ${t.badge}`}>
          <Icon name={icon} className="size-6" strokeWidth={1.9} />
        </span>
        {/* Sin `truncate`: cortar el nombre de la pantalla a la que alguien
            quiere llegar es peor que dejar que ocupe dos líneas. */}
        <span className="flex-1 min-w-0 font-display text-xl font-medium leading-tight">
          {title}
        </span>
        <span
          className="shrink-0 grid place-items-center size-9 rounded-full border border-line text-ink-soft
                     transition-[transform,background-color,color,border-color] duration-300
                     group-hover:bg-ink group-hover:border-ink group-hover:text-on-ink group-hover:translate-x-0.5"
        >
          <Icon name="arrowUpRight" className="size-4" strokeWidth={2.2} />
        </span>
      </span>

      <span className="block mt-4 text-ink-soft leading-snug">{description}</span>
      <span aria-hidden="true" className="block flex-1" />

      {meta && (
        <span className={`mt-4 inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-xs font-medium ${t.badge}`}>
          {meta}
        </span>
      )}
    </button>
  )
}

/**
 * Métrica suelta del bento. La cifra manda: grande, apretada y en tinta. La
 * etiqueta va arriba en gris y pequeña — ese contraste hace legible una fila de
 * cuatro números seguidos sin necesitar bordes entre ellos.
 */
export function StatTile({
  label,
  value,
  unit,
  icon,
  tone = 'plain',
  delta,
  footer,
}: {
  label: string
  value: string
  unit?: string
  icon?: IconName
  tone?: Tone
  /** Variación contra el periodo anterior. El signo decide el color. */
  delta?: { value: string; direction: 'up' | 'down' | 'flat' }
  footer?: ReactNode
}) {
  const t = tones[tone]
  const onInk = tone === 'ink'

  return (
    <div className={`rounded-card ${t.wrap} shadow-card ${onInk ? '' : 'pane'}
                     p-5 sm:p-6 flex flex-col gap-5 h-full`}>
      <div className="flex items-start justify-between gap-3">
        <p className={`eyebrow ${onInk ? 'text-on-panel/50' : 'text-ink-soft'}`}>{label}</p>
        {icon && (
          <span className={`shrink-0 grid place-items-center size-9 rounded-full ${t.badge}`}>
            <Icon name={icon} className="size-4.5" strokeWidth={1.9} />
          </span>
        )}
      </div>

      <div className="mt-auto">
        <p className="flex items-baseline gap-1.5">
          <span className={`font-numeral text-3xl sm:text-4xl font-medium leading-none ${onInk ? 'text-on-panel' : 'text-ink'}`}>
            {value}
          </span>
          {unit && <span className={`text-sm ${onInk ? 'text-on-panel/50' : 'text-ink-faint'}`}>{unit}</span>}
        </p>

        {delta && <Delta {...delta} className="mt-3" />}
        {footer && <div className="mt-3">{footer}</div>}
      </div>
    </div>
  )
}

/** Pastilla de variación. Verde sube, rojo baja, gris se queda igual. */
export function Delta({
  value,
  direction,
  className = '',
}: {
  value: string
  direction: 'up' | 'down' | 'flat'
  className?: string
}) {
  const style = {
    up:   'bg-forest-soft text-forest-deep',
    down: 'bg-danger-soft text-danger',
    flat: 'bg-bone-deep text-ink-soft',
  }[direction]
  const icon: IconName = direction === 'up' ? 'trendUp' : direction === 'down' ? 'trendDown' : 'minus'

  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full pl-2 pr-2.5 py-1 text-xs font-semibold tabular ${style} ${className}`}>
      <Icon name={icon} className="size-3.5" strokeWidth={2.4} />
      {value}
    </span>
  )
}
