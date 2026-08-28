import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { Icon } from './Icon'

interface ScreenProps {
  title: string
  /** Contexto de una línea bajo el título. Nunca decorativo: siempre dice algo útil. */
  subtitle?: string
  children: ReactNode
  /** Acción fija al pie. Se queda visible aunque la lista sea larguísima. */
  footer?: ReactNode
  onBack?: () => void
  /** Ancho máximo. Las pantallas de tesorería usan uno más amplio en escritorio. */
  width?: 'narrow' | 'wide'
  /** Progreso del flujo: `[paso, total]`. Dibuja una barra bajo el encabezado. */
  step?: [number, number]
}

/**
 * Armazón de los flujos de captura.
 *
 * Un solo objetivo por pantalla: encabezado, contenido y —cuando hay que
 * decidir algo— una única acción al pie, siempre visible. Sin barra lateral y
 * sin dock a propósito: un flujo de captura no debe ofrecer salidas laterales,
 * porque la salida más fácil no puede ser abandonar un reporte a medias.
 */
export function Screen({
  title,
  subtitle,
  children,
  footer,
  onBack,
  width = 'narrow',
  step,
}: ScreenProps) {
  const navigate = useNavigate()
  const max = width === 'wide' ? 'max-w-5xl' : 'max-w-2xl'

  return (
    <div className="min-h-dvh flex flex-col">
      <header className="sticky top-0 z-30 frost border-b border-line">
        <div className={`${max} mx-auto px-4 sm:px-8 py-3 flex items-center gap-3`}>
          <button
            type="button"
            onClick={() => (onBack ? onBack() : navigate(-1))}
            className="shrink-0 inline-flex items-center gap-1.5 pl-3 pr-4 min-h-touch
                       rounded-control bg-surface border border-line shadow-pill
                       text-ink font-medium
                       hover:bg-surface-warm hover:border-line-strong transition active:scale-95"
          >
            <Icon name="back" className="size-5" strokeWidth={2.2} />
            {/* El icono nunca va solo, ni siquiera aquí. */}
            <span>Atrás</span>
          </button>

          <div className="min-w-0 flex-1">
            <h1 className="font-display text-xl sm:text-2xl font-medium leading-tight truncate">
              {title}
            </h1>
            {/* El subtítulo NO se trunca: casi siempre es la instrucción de la
                pantalla ("¿De qué grupo vas a levantar el reporte?") y cortarla
                a la mitad la vuelve inservible. */}
            {subtitle && <p className="text-sm text-ink-soft leading-snug">{subtitle}</p>}
          </div>

          {step && (
            <span className="shrink-0 hidden sm:inline-flex items-center rounded-full bg-bone-deep
                             px-3.5 py-1.5 text-xs font-semibold text-ink-soft tabular">
              Paso {step[0]} de {step[1]}
            </span>
          )}
        </div>

        {step && (
          <div
            className="h-1 bg-bone-deep"
            role="progressbar"
            aria-valuenow={step[0]}
            aria-valuemin={1}
            aria-valuemax={step[1]}
            aria-label={`Paso ${step[0]} de ${step[1]}`}
          >
            <div
              className="h-full bg-clay rounded-r-full transition-[width] duration-500 ease-[cubic-bezier(0.22,1,0.36,1)]"
              style={{ width: `${(step[0] / step[1]) * 100}%` }}
            />
          </div>
        )}
      </header>

      <main className={`${max} w-full mx-auto flex-1 px-4 sm:px-8 py-6 sm:py-8`}>{children}</main>

      {footer && (
        <div className="sticky bottom-0 z-30 frost border-t border-line">
          <div
            className={`${max} mx-auto px-4 sm:px-8 py-4`}
            style={{ paddingBottom: 'max(1rem, env(safe-area-inset-bottom))' }}
          >
            {footer}
          </div>
        </div>
      )}
    </div>
  )
}
