import { useState } from 'react'
import { ApiError, setSession } from '../api/client'
import { demoSession } from '../demo/backend'
import { useAuth } from '../auth/AuthProvider'
import { Button } from '../components/Button'
import { Field } from '../components/Field'
import { Icon } from '../components/Icon'

/**
 * Acceso.
 *
 * En escritorio, dos columnas: una de tinta con la identidad y otra clara con
 * el formulario. En el teléfono la columna de tinta se convierte en una franja
 * superior — el mismo material y la misma jerarquía, ocupando el espacio que
 * hay. El formulario nunca queda bajo el pliegue.
 */
export function LoginScreen() {
  const { signIn } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      await signIn(email.trim(), password)
    } catch (err) {
      // El backend responde lo mismo ante correo inexistente y contraseña mala,
      // a propósito. Aquí se muestra tal cual, sin adornarlo.
      setError(err instanceof ApiError ? err.message : 'No se pudo iniciar sesión. Intenta de nuevo.')
      setBusy(false)
    }
  }

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[1.05fr_1fr]">
      <Aside />

      <div className="flex flex-col justify-center px-5 sm:px-8 py-10 lg:py-16">
        <div className="w-full max-w-md mx-auto animate-rise">
          {/* Sin repetir "Comunidad Cristiana Penuel": ya lo dice el bloque de
              tinta de arriba, y dos veces en la misma pantalla se lee como un
              descuido. */}
          <div className="mb-8 lg:mb-9">
            <p className="text-lg text-ink-soft">Bienvenido de vuelta</p>
            <h1 className="mt-1 font-display text-4xl font-semibold leading-tight">Entra a tu cuenta</h1>
          </div>

          <form onSubmit={submit} className="space-y-5">
            <Field
              label="Correo electrónico"
              type="email"
              icon="mail"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="username"
              inputMode="email"
              autoCapitalize="off"
              autoCorrect="off"
              spellCheck={false}
              required
              placeholder="tu.correo@ejemplo.com"
            />

            <Field
              label="Contraseña"
              type="password"
              icon="lock"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
              placeholder="Tu contraseña"
            />

            {error && (
              <p
                role="alert"
                className="flex items-start gap-3 rounded-[1.25rem] bg-danger-soft
                           px-5 py-4 text-danger font-medium animate-rise"
              >
                <Icon name="alert" className="size-5 shrink-0 mt-0.5" strokeWidth={2.2} />
                <span>{error}</span>
              </p>
            )}

            <Button type="submit" full loading={busy} iconAfter={busy ? undefined : 'next'}>
              {busy ? 'Entrando…' : 'Entrar'}
            </Button>
          </form>

          <p className="mt-8 text-center text-ink-soft leading-relaxed">
            Las cuentas las crea el Pastor desde el sistema.
            <br />
            Si no puedes entrar, pídele ayuda a él.
          </p>

          <DemoEntry />
        </div>
      </div>
    </div>
  )
}

/**
 * Columna de identidad.
 *
 * El nombre viene de Génesis 32: Peniel, «he visto a Dios cara a cara». La
 * línea está ahí porque una pantalla de acceso a una aplicación de iglesia que
 * no dice nada de la iglesia es una oportunidad desperdiciada — y porque quien
 * entra cada domingo merece reconocer su casa antes de teclear.
 */
function Aside() {
  return (
    <aside className="relative aurora overflow-hidden
                      lg:rounded-none rounded-b-[2.5rem] lg:min-h-dvh
                      px-6 sm:px-10 lg:px-14 pt-12 pb-12 lg:py-16
                      flex flex-col justify-between gap-10">
      <div className="flex items-center gap-3">
        <span className="grid place-items-center size-11 rounded-[0.9rem] bg-clay text-on-accent
                         font-display text-xl font-bold leading-none">
          <span aria-hidden="true" className="-mt-px">P</span>
        </span>
        <span>
          <span className="block font-display text-xl font-medium leading-none">Penuel</span>
          <span className="block text-xs text-ink-faint mt-1">Ciudad Victoria, Tamaulipas</span>
        </span>
      </div>

      <div className="max-w-lg">
        <p className="text-lg text-ink-soft">Comunidad Cristiana Penuel</p>
        <p className="mt-2 font-display text-3xl sm:text-4xl lg:text-5xl font-semibold leading-[1.06] text-balance">
          Todo lo que la iglesia levanta cada domingo, en un solo lugar.
        </p>
        <p className="mt-5 text-ink-soft text-lg leading-relaxed max-w-md text-pretty">
          Asistencia, ofrenda, diezmo y ministerios. Registrado una vez, disponible siempre.
        </p>
      </div>

      <div className="hidden lg:flex items-center gap-3 flex-wrap">
        {[
          { icon: 'book' as const, label: 'Escuela Dominical' },
          { icon: 'coins' as const, label: 'Cultos y diezmos' },
          { icon: 'group' as const, label: 'Ministerios' },
        ].map((f) => (
          <span
            key={f.label}
            className="inline-flex items-center gap-2 rounded-control bg-surface/70 backdrop-blur
                       px-4 py-2 text-sm text-ink-soft"
          >
            <Icon name={f.icon} className="size-4" strokeWidth={1.9} />
            {f.label}
          </span>
        ))}
      </div>
    </aside>
  )
}

/**
 * Entrada a la demostración.
 *
 * Levanta una sesión falsa contra `demo/backend`, que responde en memoria con
 * un directorio y seis meses de sesiones. Sirve para ver y juzgar la interfaz
 * completa sin la API .NET corriendo. Se distingue del acceso real a propósito:
 * es texto pequeño y gris, no un botón que compita con «Entrar».
 */
function DemoEntry() {
  return (
    <div className="mt-8 pt-8 border-t border-line text-center">
      <p className="text-sm text-ink-faint">¿Solo quieres ver cómo funciona?</p>
      <div className="mt-3">
        <Button
          variant="quiet"
          size="sm"
          icon="eye"
          onClick={() => setSession(demoSession())}
        >
          Entrar a la demostración
        </Button>
      </div>
    </div>
  )
}
