import type { ReactNode } from 'react'
import { Icon } from '../Icon'
import type { IconName } from '../Icon'

export type ChipTone = 'neutral' | 'clay' | 'forest' | 'ochre' | 'lake' | 'danger' | 'ink' | 'onInk'

const chipTones: Record<ChipTone, string> = {
  neutral: 'bg-bone-deep text-ink-soft',
  clay:    'bg-clay-soft text-clay-deep',
  forest:  'bg-forest-soft text-forest-deep',
  ochre:   'bg-ochre-soft text-ochre-deep',
  lake:    'bg-lake-soft text-lake-deep',
  danger:  'bg-danger-soft text-danger',
  ink:     'bg-ink text-on-ink',
  onInk:   'bg-white/10 text-on-panel/80',
}

/**
 * Etiqueta de estado. No es pulsable: si algo se puede tocar, es un `Button` o
 * un `FilterChip`, nunca esto. Mantener esa frontera evita el peor problema de
 * los tableros — no saber qué responde al dedo.
 */
export function Chip({
  children,
  tone = 'neutral',
  icon,
  dot = false,
}: {
  children: ReactNode
  tone?: ChipTone
  icon?: IconName
  /** Punto de color en lugar de icono: para leyendas de gráficas. */
  dot?: boolean
}) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1
                  text-xs font-medium leading-5 whitespace-nowrap ${chipTones[tone]}`}
    >
      {dot && <span className="size-1.5 rounded-full bg-current" />}
      {icon && <Icon name={icon} className="size-3.5 -ml-0.5" strokeWidth={2.4} />}
      {children}
    </span>
  )
}

/**
 * Control segmentado: el selector de periodo del tablero y los filtros de las
 * listas. La pastilla activa es tinta sólida — el mismo recurso que usan los
 * tableros de referencia, y el único indicador que se lee sin depender de un
 * matiz de gris.
 *
 * En móvil la fila se desliza horizontalmente sin barra visible; en escritorio
 * cabe entera.
 */
export function Segmented<T extends string>({
  options,
  value,
  onChange,
  label,
  size = 'md',
}: {
  options: { value: T; label: string; count?: number }[]
  value: T
  onChange: (value: T) => void
  /** Nombre del grupo para lectores de pantalla. */
  label: string
  size?: 'md' | 'sm'
}) {
  const pad = size === 'sm' ? 'min-h-11 px-4 text-sm' : 'min-h-touch px-5 text-base'

  return (
    <div
      role="tablist"
      aria-label={label}
      /* `.rail` lo lleva hasta el borde de la pantalla y desvanece lo que se sale.
         El `py-1` deja respirar el anillo de foco, que si no se recortaría. */
      className="rail flex items-center gap-2 py-1"
    >
      {options.map((o) => {
        const active = o.value === value
        return (
          <button
            key={o.value}
            role="tab"
            aria-selected={active}
            type="button"
            onClick={() => onChange(o.value)}
            className={`shrink-0 inline-flex items-center gap-2 rounded-control font-medium border
                        transition-[background-color,color,border-color] duration-250 ease-[cubic-bezier(0.22,1,0.36,1)]
                        active:scale-[0.97] ${pad}
                        ${active
                          ? 'bg-ink text-on-ink border-ink'
                          : 'bg-surface text-ink-soft border-line hover:text-ink hover:border-line-strong'}`}
          >
            {o.label}
            {o.count !== undefined && (
              <span
                className={`rounded-full px-1.5 min-w-6 text-xs tabular
                            ${active ? 'bg-on-ink/15 text-on-ink' : 'bg-bone-deep text-ink-faint'}`}
              >
                {o.count}
              </span>
            )}
          </button>
        )
      })}
    </div>
  )
}

/** Chip pulsable de filtro, con estado de selección. */
export function FilterChip({
  children,
  active,
  onClick,
  icon,
}: {
  children: ReactNode
  active: boolean
  onClick: () => void
  icon?: IconName
}) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onClick}
      className={`shrink-0 inline-flex items-center gap-2 rounded-control min-h-11 px-4
                  text-sm font-medium border transition duration-200 active:scale-[0.97]
                  ${active
                    ? 'bg-ink text-on-ink border-ink'
                    : 'bg-surface text-ink-soft border-line hover:border-line-strong hover:text-ink'}`}
    >
      {icon && <Icon name={icon} className="size-4" strokeWidth={2.2} />}
      {children}
    </button>
  )
}
