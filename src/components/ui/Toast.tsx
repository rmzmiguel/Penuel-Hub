import { createContext, use, useCallback, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { Icon } from '../Icon'

interface Toast {
  id: number
  tone: 'ok' | 'error'
  title: string
  detail?: string
  /** Si viene, el aviso ofrece deshacer y dura el doble. */
  undo?: () => void
}

const ToastContext = createContext<((t: Omit<Toast, 'id'>) => void) | null>(null)

/**
 * Avisos de confirmación.
 *
 * Existen para las acciones REVERSIBLES del tablero —otorgar un permiso,
 * cambiar un liderazgo—, donde una pantalla completa de confirmación sería
 * insoportable si se hacen diez seguidas. Las acciones irreversibles no usan
 * esto: usan `ConfirmDialog` antes, y confirmación completa después.
 *
 * Un aviso con "Deshacer" dura 8 segundos y no 4: leer, decidir y alcanzar el
 * botón toma bastante más de lo que dura un aviso convencional.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<Toast[]>([])
  const seq = useRef(0)

  const show = useCallback((t: Omit<Toast, 'id'>) => {
    const id = ++seq.current
    setItems((list) => [...list.slice(-2), { ...t, id }])
    setTimeout(() => setItems((list) => list.filter((x) => x.id !== id)), t.undo ? 8000 : 4200)
  }, [])

  const dismiss = (id: number) => setItems((list) => list.filter((x) => x.id !== id))

  return (
    <ToastContext value={show}>
      {children}

      {/* `pointer-events-none` en el contenedor para que la pila no bloquee la
          interfaz de abajo; cada aviso reactiva los eventos sobre sí mismo.
          El offset inferior deja libre el dock del teléfono. */}
      <div
        className="pointer-events-none fixed inset-x-0 bottom-0 z-[70] flex flex-col items-center gap-2
                   px-4 pb-[calc(var(--spacing-dock)+1.25rem+env(safe-area-inset-bottom))]
                   lg:items-end lg:px-6 lg:pb-6"
        role="status"
        aria-live="polite"
      >
        {items.map((t) => (
          <div
            key={t.id}
            className="pointer-events-auto w-full max-w-md flex items-center gap-3.5
                       rounded-[1.4rem] panel-ink text-on-panel px-4 py-3.5 animate-rise"
          >
            <span
              className={`shrink-0 grid place-items-center size-10 rounded-full
                          ${t.tone === 'ok' ? 'bg-forest-bright/20 text-forest-bright' : 'bg-clay-bright/20 text-clay-bright'}`}
            >
              <Icon name={t.tone === 'ok' ? 'check' : 'alert'} className="size-5" strokeWidth={2.4} />
            </span>

            <span className="min-w-0 flex-1">
              <span className="block font-medium leading-snug">{t.title}</span>
              {t.detail && <span className="block text-sm text-on-panel/60 leading-snug">{t.detail}</span>}
            </span>

            {t.undo ? (
              <button
                type="button"
                onClick={() => { t.undo?.(); dismiss(t.id) }}
                className="shrink-0 inline-flex items-center gap-1.5 min-h-11 px-4 rounded-control
                           bg-white/10 text-on-panel font-medium text-sm
                           hover:bg-white/18 transition active:scale-95"
              >
                <Icon name="undo" className="size-4" strokeWidth={2.4} />
                Deshacer
              </button>
            ) : (
              <button
                type="button"
                onClick={() => dismiss(t.id)}
                aria-label="Cerrar aviso"
                className="shrink-0 grid place-items-center size-10 rounded-full
                           text-on-panel/50 hover:text-on-panel hover:bg-white/10 transition"
              >
                <Icon name="close" className="size-4" strokeWidth={2.4} />
              </button>
            )}
          </div>
        ))}
      </div>
    </ToastContext>
  )
}

export function useToast() {
  const show = use(ToastContext)
  if (!show) throw new Error('useToast debe usarse dentro de <ToastProvider>')
  return useMemo(
    () => ({
      ok: (title: string, detail?: string, undo?: () => void) => show({ tone: 'ok', title, detail, undo }),
      error: (title: string, detail?: string) => show({ tone: 'error', title, detail }),
    }),
    [show],
  )
}
