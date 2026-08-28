import type { ReactNode } from 'react'

/**
 * Encabezado de pantalla.
 *
 * Una línea gris y pequeña arriba, y debajo un titular grande y apretado. La
 * jerarquía completa se hace con esos dos tamaños; no hace falta una regla, ni
 * un fondo, ni un icono. Es el gesto que sostiene todas las pantallas.
 *
 * En el teléfono el control baja a su propia línea y ocupa el ancho completo:
 * apretujarlo junto al título deja los dos ilegibles.
 */
export function PageHeader({
  eyebrow,
  title,
  lead,
  action,
}: {
  eyebrow: string
  title: ReactNode
  /** Una frase bajo el título. Dice para qué sirve la pantalla. */
  lead?: string
  action?: ReactNode
}) {
  return (
    <header className="mb-7 sm:mb-9">
      <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-5">
        <div className="min-w-0 animate-rise">
          <p className="text-lg text-ink-soft">{eyebrow}</p>
          <h1 className="mt-1 font-display text-4xl sm:text-5xl font-semibold leading-[1.02] text-balance">
            {title}
          </h1>
          {lead && <p className="mt-3 text-ink-soft text-lg max-w-xl text-pretty">{lead}</p>}
        </div>
        {action && <div className="shrink-0 animate-rise">{action}</div>}
      </div>
    </header>
  )
}
