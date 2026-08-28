import type { ReactNode } from 'react'
import { initials as toInitials } from '../../lib/format'

type Size = 'xs' | 'sm' | 'md' | 'lg' | 'xl'

const sizes: Record<Size, string> = {
  xs: 'size-8  text-xs',
  sm: 'size-10 text-sm',
  md: 'size-12 text-base',
  lg: 'size-14 text-lg',
  xl: 'size-20 text-3xl',
}

/*
 * Seis tintes cálidos. El tinte se deriva del nombre, así que la misma persona
 * sale siempre del mismo color en toda la aplicación: en una lista de sesenta
 * personas, ese color constante es lo que deja reconocer a alguien de reojo,
 * antes de leer.
 */
const tints = [
  'bg-clay-soft   text-clay-deep',
  'bg-forest-soft text-forest-deep',
  'bg-ochre-soft  text-ochre-deep',
  'bg-lake-soft   text-lake-deep',
  'bg-bone-deep   text-ink-soft',
  'bg-rose-soft   text-rose-deep',
]

function tintOf(seed: string) {
  let h = 0
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0
  return tints[h % tints.length]
}

export function Avatar({
  person,
  size = 'md',
  /** Anillo del color de acento: marca a quien está seleccionado en una lista. */
  ring = false,
  className = '',
}: {
  person: { firstName: string; lastName: string }
  size?: Size
  ring?: boolean
  className?: string
}) {
  const label = `${person.firstName} ${person.lastName}`
  return (
    <span
      title={label}
      className={`shrink-0 grid place-items-center rounded-full font-medium tracking-tight
                  ${sizes[size]} ${tintOf(label)}
                  ${ring ? 'ring-2 ring-clay ring-offset-2 ring-offset-surface' : ''} ${className}`}
    >
      <span aria-hidden="true">{toInitials(person)}</span>
      <span className="sr-only">{label}</span>
    </span>
  )
}

/**
 * Pila de avatares solapados con un contador al final. La usan las tarjetas que
 * hablan de un grupo sin espacio para listarlo.
 */
export function AvatarStack({
  people,
  max = 4,
  size = 'sm',
}: {
  people: { firstName: string; lastName: string }[]
  max?: number
  size?: Size
}) {
  const shown = people.slice(0, max)
  const rest = people.length - shown.length

  return (
    <span className="flex items-center">
      {shown.map((p, i) => (
        <span
          key={`${p.firstName}-${p.lastName}-${i}`}
          className="-ml-2.5 first:ml-0 rounded-full ring-2 ring-surface"
        >
          <Avatar person={p} size={size} />
        </span>
      ))}
      {rest > 0 && (
        <span
          className={`-ml-2.5 grid place-items-center rounded-full ring-2 ring-surface
                      bg-ink text-on-ink font-medium ${sizes[size]}`}
        >
          +{rest}
        </span>
      )}
    </span>
  )
}

/** Avatar + nombre + línea de apoyo. La fila base de todo listado de personas. */
export function PersonLine({
  person,
  secondary,
  size = 'md',
  trailing,
}: {
  person: { firstName: string; lastName: string }
  secondary?: ReactNode
  size?: Size
  trailing?: ReactNode
}) {
  return (
    <span className="flex items-center gap-3.5 min-w-0">
      <Avatar person={person} size={size} />
      <span className="min-w-0 flex-1">
        <span className="block font-medium leading-snug">
          {person.firstName} {person.lastName}
        </span>
        {secondary && <span className="block text-sm text-ink-soft leading-snug">{secondary}</span>}
      </span>
      {trailing}
    </span>
  )
}
