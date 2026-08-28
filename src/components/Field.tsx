import type { InputHTMLAttributes } from 'react'
import { useId } from 'react'
import { Icon } from './Icon'
import type { IconName } from './Icon'

export { Switch } from './ui/Toggle'

interface FieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'className' | 'id'> {
  label: string
  /** Ayuda breve bajo el campo. Se muestra siempre, no solo al fallar. */
  hint?: string
  error?: string | null
  icon?: IconName
}

/*
 * Campos de 64px de alto y texto de 19px. Las etiquetas van ARRIBA y siempre
 * visibles — nunca como marcador que desaparece al escribir, que es una de las
 * peores trampas de usabilidad para quien captura despacio.
 *
 * El foco añade un anillo difuso además de cambiar el borde: sobre un fondo
 * hueso, un borde de 1px cambiando de color no es señal suficiente.
 */
const control =
  'w-full min-w-0 max-w-full min-h-touch-lg rounded-control bg-surface border text-lg ' +
  'placeholder:text-ink-faint shadow-pill ' +
  'transition-[border-color,box-shadow] duration-200 ' +
  'focus:outline-none focus:border-ink focus:ring-4 focus:ring-ink/8'

export function Field({ label, hint, error, icon, ...rest }: FieldProps) {
  const id = useId()
  const hintId = `${id}-hint`

  return (
    <div>
      <label htmlFor={id} className="block font-medium text-lg mb-2">
        {label}
      </label>
      <div className="relative">
        {icon && (
          <span className="pointer-events-none absolute left-5 top-1/2 -translate-y-1/2 text-ink-faint">
            <Icon name={icon} className="size-5" strokeWidth={2.1} />
          </span>
        )}
        <input
          {...rest}
          id={id}
          aria-describedby={hint || error ? hintId : undefined}
          aria-invalid={error ? true : undefined}
          className={`${control} px-5 ${icon ? 'pl-13' : ''} ${error ? 'border-danger ring-4 ring-danger/10' : 'border-line'}`}
        />
      </div>
      {(error || hint) && (
        <p id={hintId} className={`mt-2 text-sm ${error ? 'text-danger font-semibold' : 'text-ink-soft'}`}>
          {error ?? hint}
        </p>
      )}
    </div>
  )
}

/**
 * Campo de dinero. Teclado numérico en el teléfono y el símbolo siempre a la
 * vista, para que nadie tenga que adivinar si escribe pesos o centavos.
 */
export function MoneyField({
  label,
  value,
  onChange,
  hint,
  error,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  hint?: string
  error?: string | null
}) {
  const id = useId()
  const hintId = `${id}-hint`

  return (
    <div>
      <label htmlFor={id} className="block font-medium text-lg mb-2">
        {label}
      </label>
      <div className="relative">
        <span
          aria-hidden="true"
          className="pointer-events-none absolute left-6 top-1/2 -translate-y-1/2
                     font-numeral text-2xl text-ink-faint"
        >
          $
        </span>
        <input
          id={id}
          value={value}
          onChange={(e) => onChange(e.target.value.replace(/[^0-9.]/g, ''))}
          inputMode="decimal"
          placeholder="0.00"
          aria-describedby={hint || error ? hintId : undefined}
          aria-invalid={error ? true : undefined}
          className={`${control} pl-12 pr-6 font-numeral text-3xl font-semibold tabular
                      placeholder:font-normal ${error ? 'border-danger ring-4 ring-danger/10' : 'border-line'}`}
        />
      </div>
      {(error || hint) && (
        <p id={hintId} className={`mt-2 text-sm ${error ? 'text-danger font-semibold' : 'text-ink-soft'}`}>
          {error ?? hint}
        </p>
      )}
    </div>
  )
}

/**
 * Contador de capítulos leídos. Botones grandes en lugar de teclear:
 * los números que se capturan aquí son casi siempre de un dígito.
 */
export function Counter({
  label,
  value,
  onChange,
  max = 99,
}: {
  label: string
  value: number
  onChange: (value: number) => void
  max?: number
}) {
  const step = (delta: number) => onChange(Math.min(max, Math.max(0, value + delta)))

  return (
    <div className="flex items-center justify-between gap-4">
      <span className="font-medium">{label}</span>
      <div className="flex items-center gap-1 rounded-control bg-bone-deep p-1">
        <Step icon="minus" label={`Quitar uno a ${label}`} disabled={value <= 0} onClick={() => step(-1)} />
        <span className="w-12 text-center font-numeral text-2xl font-semibold tabular">{value}</span>
        <Step icon="plus" label={`Sumar uno a ${label}`} disabled={value >= max} onClick={() => step(1)} />
      </div>
    </div>
  )
}

function Step({
  icon,
  label,
  disabled,
  onClick,
}: {
  icon: 'plus' | 'minus'
  label: string
  disabled: boolean
  onClick: () => void
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      aria-label={label}
      className="grid place-items-center size-12 rounded-full bg-surface text-ink
                 shadow-card transition active:scale-90 disabled:opacity-35"
    >
      <Icon name={icon} className="size-5" strokeWidth={2.6} />
    </button>
  )
}
