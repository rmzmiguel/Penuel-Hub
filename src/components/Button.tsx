import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Icon, Spinner } from './Icon'
import type { IconName } from './Icon'

type Variant = 'primary' | 'secondary' | 'quiet' | 'danger' | 'ink' | 'soft' | 'onInk'
type Size = 'lg' | 'md' | 'sm'

interface ButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'className'> {
  children: ReactNode
  variant?: Variant
  size?: Size
  icon?: IconName
  /** El icono a la derecha se usa para "continuar", nunca para acciones destructivas. */
  iconAfter?: IconName
  loading?: boolean
  full?: boolean
}

/*
 * Todo botón es una píldora. Los tamaños no son negociables: 64px de alto en
 * acciones primarias y 56px en las secundarias. El mínimo de accesibilidad son
 * 44px — aquí no se usa el mínimo, porque el objetivo es que una persona mayor
 * NUNCA falle al tocar.
 *
 * El `active:scale` es de 0.97 y no de 0.99: en una píldora grande, un 1% no se
 * percibe y el botón se siente muerto al tocarlo.
 */
const base =
  'relative inline-flex items-center justify-center gap-2.5 rounded-control ' +
  'font-medium tracking-[-0.012em] select-none whitespace-nowrap ' +
  'transition-[transform,background-color,border-color,color,box-shadow] duration-200 ease-[cubic-bezier(0.22,1,0.36,1)] ' +
  'active:scale-[0.97] disabled:opacity-40 disabled:pointer-events-none'

const sizes: Record<Size, string> = {
  lg: 'min-h-touch-lg px-8 text-lg',
  md: 'min-h-touch px-6 text-lg',
  sm: 'min-h-11 px-5 text-base',
}

const variants: Record<Variant, string> = {
  primary:   'bg-ink text-on-ink shadow-pill hover:bg-ink-raised',
  secondary: 'bg-surface text-ink border border-line shadow-pill hover:border-line-strong hover:bg-surface-warm',
  quiet:     'text-ink-soft hover:text-ink hover:bg-bone-deep',
  danger:    'bg-danger-soft text-danger hover:bg-danger hover:text-on-accent',
  ink:       'bg-ink text-on-ink shadow-pill hover:bg-ink-raised',
  /** El acento, para la única acción que quiere destacar sobre otra negra. */
  soft:      'bg-clay text-on-accent shadow-pill hover:bg-clay-deep',
  /** Sobre superficies de tinta. El borde es lo único que lo separa del fondo. */
  onInk:     'bg-white/10 text-on-panel border border-white/15 hover:bg-white/18',
}

/** Las primarias son grandes por omisión; el resto, medianas. */
const defaultSize: Record<Variant, Size> = {
  primary: 'lg', ink: 'lg', soft: 'md',
  secondary: 'md', quiet: 'md', danger: 'md', onInk: 'md',
}

export function Button({
  children,
  variant = 'primary',
  size,
  icon,
  iconAfter,
  loading = false,
  full = false,
  disabled,
  type = 'button',
  ...rest
}: ButtonProps) {
  return (
    <button
      {...rest}
      type={type}
      disabled={disabled || loading}
      className={`${base} ${sizes[size ?? defaultSize[variant]]} ${variants[variant]} ${full ? 'w-full' : ''}`}
    >
      {loading ? <Spinner className="size-5" /> : icon ? <Icon name={icon} className="size-5 shrink-0" strokeWidth={2.1} /> : null}
      <span>{children}</span>
      {iconAfter && !loading ? <Icon name={iconAfter} className="size-5 shrink-0" strokeWidth={2.1} /> : null}
    </button>
  )
}

/**
 * Botón de solo icono. Es la ÚNICA excepción a la regla de "ningún icono va
 * solo", y por eso `label` es obligatorio: se convierte en `aria-label` y en
 * el `title` del navegador. Reservado para cerrar, volver y acciones repetidas
 * dentro de una fila que ya está etiquetada por su contenido.
 */
export function IconButton({
  icon,
  label,
  variant = 'secondary',
  size = 'md',
  ...rest
}: {
  icon: IconName
  label: string
  variant?: Variant
  size?: Size
} & Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'className' | 'children'>) {
  const box = size === 'sm' ? 'size-11' : size === 'md' ? 'size-touch' : 'size-touch-lg'
  return (
    <button
      {...rest}
      type={rest.type ?? 'button'}
      aria-label={label}
      title={label}
      className={`${base} ${box} shrink-0 p-0 ${variants[variant]}`}
    >
      <Icon name={icon} className="size-5" strokeWidth={2.1} />
    </button>
  )
}
