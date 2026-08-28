import { useId } from 'react'
import { Icon } from '../Icon'

/**
 * Buscador. Píldora de 64px con el icono a la izquierda y un botón de limpiar
 * que solo aparece cuando hay texto — una "x" permanente en un campo vacío es
 * un objetivo táctil que no hace nada.
 */
export function SearchField({
  value,
  onChange,
  placeholder,
  label,
}: {
  value: string
  onChange: (value: string) => void
  placeholder: string
  label: string
}) {
  const id = useId()

  return (
    <div className="relative w-full">
      <label htmlFor={id} className="sr-only">{label}</label>
      <span className="pointer-events-none absolute left-5 top-1/2 -translate-y-1/2 text-ink-faint">
        <Icon name="search" className="size-5" strokeWidth={2.1} />
      </span>
      <input
        id={id}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        type="search"
        autoComplete="off"
        className="w-full min-h-touch-lg rounded-control bg-surface border border-line shadow-pill
                   pl-13 pr-14 text-lg placeholder:text-ink-faint
                   transition focus:outline-none focus:border-ink focus:ring-4 focus:ring-ink/8
                   [&::-webkit-search-cancel-button]:hidden"
      />
      {value && (
        <button
          type="button"
          onClick={() => onChange('')}
          aria-label="Limpiar la búsqueda"
          className="absolute right-2.5 top-1/2 -translate-y-1/2 grid place-items-center
                     size-11 rounded-full text-ink-soft hover:bg-bone-deep transition active:scale-95"
        >
          <Icon name="close" className="size-4.5" strokeWidth={2.4} />
        </button>
      )}
    </div>
  )
}
