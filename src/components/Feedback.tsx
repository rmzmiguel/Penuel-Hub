import type { ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import type { ApiError } from '../api/client'
import { Button } from './Button'
import { Icon, Spinner } from './Icon'

export function Loading({ label = 'Cargando…' }: { label?: string }) {
  return (
    <div className="py-24 flex flex-col items-center gap-4 text-ink-soft animate-fade">
      <Spinner className="size-9 text-clay" label={label} />
      <p className="text-lg">{label}</p>
    </div>
  )
}

/**
 * Estado de error. Distingue 403 de todo lo demás a propósito: ante un permiso
 * insuficiente NO tiene caso ofrecer "reintentar", porque volver a intentarlo
 * dará exactamente lo mismo. El backend ya manda un mensaje que dice qué haría
 * falta, y ese es el que se muestra.
 */
export function ErrorState({ error, onRetry }: { error: ApiError; onRetry?: () => void }) {
  const forbidden = error.isForbidden

  return (
    <div className="py-16 flex flex-col items-center text-center gap-5 animate-rise">
      <span
        className={`grid place-items-center size-18 rounded-[1.6rem]
                    ${forbidden ? 'bg-ochre-soft text-ochre-deep' : 'bg-danger-soft text-danger'}`}
      >
        <Icon name={forbidden ? 'lock' : 'alert'} className="size-8" strokeWidth={2} />
      </span>
      <div className="max-w-md">
        <h2 className="font-display text-2xl font-medium">
          {forbidden ? 'No tienes acceso a esto' : 'Algo salió mal'}
        </h2>
        <p className="mt-2 text-ink-soft text-lg text-pretty">{error.message}</p>
      </div>
      {onRetry && !forbidden && (
        <Button variant="secondary" onClick={onRetry} icon="undo">
          Intentar de nuevo
        </Button>
      )}
    </div>
  )
}

export function EmptyState({
  title,
  description,
  action,
}: {
  title: string
  description: string
  action?: ReactNode
}) {
  return (
    <div className="py-16 flex flex-col items-center text-center gap-4 animate-rise">
      <span className="grid place-items-center size-18 rounded-[1.6rem] bg-bone-deep text-ink-faint">
        <Icon name="list" className="size-8" strokeWidth={2} />
      </span>
      <div className="max-w-md">
        <h2 className="font-display text-2xl font-medium">{title}</h2>
        <p className="mt-2 text-ink-soft text-lg text-pretty">{description}</p>
      </div>
      {action}
    </div>
  )
}

/**
 * Confirmación explícita después de guardar.
 *
 * Es una pantalla completa, no un mensajito que se desvanece: cuando alguien
 * termina de capturar un reporte tiene que SENTIR que quedó guardado, y verlo
 * el tiempo que necesite. Nunca un regreso silencioso a donde estaba.
 */
export function SuccessScreen({
  title,
  summary,
  primaryLabel = 'Volver al inicio',
  onPrimary,
  secondary,
}: {
  title: string
  summary: { label: string; value: string }[]
  primaryLabel?: string
  onPrimary?: () => void
  secondary?: ReactNode
}) {
  const navigate = useNavigate()

  return (
    <div className="min-h-dvh flex flex-col items-center justify-center px-5 py-10">
      <div className="w-full max-w-md text-center animate-rise">
        <span className="relative mx-auto grid place-items-center size-24 rounded-full bg-forest text-on-accent animate-pop">
          {/* El anillo que se expande cierra el gesto: no solo aparece una
              palomita, algo SUCEDIÓ en esa posición. */}
          <span
            aria-hidden="true"
            className="absolute inset-0 rounded-full border-2 border-forest/35"
            style={{ animation: 'pop 0.9s cubic-bezier(0.22,1,0.36,1) 0.15s both', scale: '1.35' }}
          />
          <Icon name="check" className="size-11" strokeWidth={2.6} />
        </span>

        <h1 className="mt-7 font-display text-4xl font-semibold leading-tight text-balance">{title}</h1>
        <p className="mt-3 text-lg text-ink-soft">
          Ya quedó registrado. No necesitas hacer nada más.
        </p>

        <dl className="mt-8 rounded-panel bg-surface pane shadow-card
                       divide-y divide-line text-left overflow-hidden">
          {summary.map((row) => (
            <div key={row.label} className="flex items-baseline justify-between gap-4 px-6 py-4">
              <dt className="text-ink-soft">{row.label}</dt>
              <dd className="font-medium text-right tabular">{row.value}</dd>
            </div>
          ))}
        </dl>

        <div className="mt-8 flex flex-col gap-3">
          <Button full icon="home" onClick={() => (onPrimary ? onPrimary() : navigate('/'))}>
            {primaryLabel}
          </Button>
          {secondary}
        </div>
      </div>
    </div>
  )
}
