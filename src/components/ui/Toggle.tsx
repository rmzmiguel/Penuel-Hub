import type { ReactNode } from 'react'
import { Icon } from '../Icon'
import type { IconName } from '../Icon'

/**
 * Fila de permiso.
 *
 * Es el componente central de la pantalla del Pastor, y por eso NO es un
 * interruptor con una etiqueta: es una fila de 80px con nombre, explicación en
 * lenguaje llano de lo que la persona podrá hacer, y el interruptor a la
 * derecha. Toda la fila responde al toque.
 *
 * La explicación no es opcional a propósito. "SundaySchoolRecorder" no le dice
 * nada a nadie; "Puede levantar el reporte de cualquier grupo de Escuela
 * Dominical" sí. Quien otorga un permiso tiene que poder leer sus consecuencias
 * sin salir de la pantalla.
 */
export function PermissionRow({
  title,
  description,
  checked,
  onChange,
  icon,
  disabled = false,
  /** Nota bajo la fila: por qué está bloqueada, o desde cuándo está activa. */
  note,
  busy = false,
}: {
  title: string
  description: string
  checked: boolean
  onChange: (next: boolean) => void
  icon?: IconName
  disabled?: boolean
  note?: ReactNode
  busy?: boolean
}) {
  return (
    <div className={`px-5 sm:px-6 py-4 transition-colors ${disabled ? 'opacity-55' : ''}`}>
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        disabled={disabled || busy}
        onClick={() => onChange(!checked)}
        className="w-full flex items-center gap-4 text-left min-h-touch
                   transition active:scale-[0.99] disabled:pointer-events-none rounded-card"
      >
        {/* El icono se va en el teléfono. Ahí compite por 60px con la
            explicación, que es lo único que de verdad hay que leer antes de
            mover un permiso; el bloque ya lleva su propio icono en la cabecera. */}
        {icon && (
          <span
            className={`shrink-0 hidden sm:grid place-items-center size-11 rounded-full transition-colors duration-300
                        ${checked ? 'bg-forest-soft text-forest' : 'bg-bone-deep text-ink-faint'}`}
          >
            <Icon name={icon} className="size-5" strokeWidth={2.1} />
          </span>
        )}

        <span className="min-w-0 flex-1">
          <span className="block font-medium leading-snug">{title}</span>
          <span className="block text-sm text-ink-soft leading-snug mt-0.5">{description}</span>
        </span>

        <Track checked={checked} busy={busy} />
      </button>

      {note && <p className="mt-2 text-sm text-ink-faint leading-snug">{note}</p>}
    </div>
  )
}

/**
 * El riel del interruptor. La palanca lleva una palomita cuando está encendido:
 * el color solo no basta para quien no distingue verde de gris.
 */
function Track({ checked, busy }: { checked: boolean; busy?: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={`shrink-0 relative w-[4.5rem] h-10 rounded-full border transition-colors duration-300
                  ${checked ? 'bg-forest border-forest' : 'bg-bone-deep border-line-strong'}`}
    >
      <span
        className={`absolute top-1 grid place-items-center size-8 rounded-full bg-knob shadow-card
                    transition-[left] duration-300 ease-[cubic-bezier(0.22,1,0.36,1)]
                    ${checked ? 'left-[2.05rem]' : 'left-1'}`}
      >
        {busy ? (
          <span className="size-3.5 rounded-full border-2 border-ink-faint border-t-transparent animate-spin" />
        ) : checked ? (
          <Icon name="check" className="size-4 text-forest" strokeWidth={3} />
        ) : null}
      </span>
    </span>
  )
}

/** Interruptor suelto con etiqueta grande. Toda la fila es pulsable. */
export function Switch({
  label,
  checked,
  onChange,
  description,
}: {
  label: string
  checked: boolean
  onChange: (checked: boolean) => void
  description?: ReactNode
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      onClick={() => onChange(!checked)}
      className="w-full flex items-center justify-between gap-4 min-h-touch
                 text-left transition active:scale-[0.99]"
    >
      <span className="min-w-0">
        <span className="block font-medium">{label}</span>
        {description && <span className="block text-sm text-ink-soft">{description}</span>}
      </span>
      <Track checked={checked} />
    </button>
  )
}
