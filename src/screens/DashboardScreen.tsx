import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { services } from '../api/endpoints'
import type { ServiceSessionSummary } from '../api/types'
import { useAuth, usePermissions } from '../auth/AuthProvider'
import { ActionCard, Card, CardHead, Delta, StatTile } from '../components/Card'
import { Button } from '../components/Button'
import { ErrorState } from '../components/Feedback'
import { Icon } from '../components/Icon'
import type { IconName } from '../components/Icon'
import { AreaChart, Donut, DotMatrix, Legend } from '../components/ui/Charts'
import { Segmented } from '../components/ui/Chip'
import { PageHeader } from '../components/ui/PageHeader'
import { Avatar } from '../components/ui/Avatar'
import { useAsync } from '../lib/useAsync'
import { capitalize, formatCompactDate, formatMoney } from '../lib/format'
import { WINDOWS, buildDashboard, shortMoney } from '../lib/stats'
import type { WindowKey } from '../lib/stats'

/** Saludo según la hora, con su propio icono. El detalle cuesta nada y se nota. */
function greeting() {
  const h = new Date().getHours()
  if (h < 12) return { text: 'Buenos días', icon: 'sun' as IconName }
  if (h < 19) return { text: 'Buenas tardes', icon: 'sunset' as IconName }
  return { text: 'Buenas noches', icon: 'moon' as IconName }
}

const todayLabel = () =>
  capitalize(
    new Intl.DateTimeFormat('es-MX', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date()),
  )

export function DashboardScreen() {
  const navigate = useNavigate()
  const { capabilities } = useAuth()
  const [win, setWin] = useState<WindowKey>('6m')

  const history = useAsync((signal) => services.history(signal), [])

  const months = WINDOWS.find((w) => w.value === win)!.months
  const data = useMemo(
    () => buildDashboard(history.data ?? [], months),
    [history.data, months],
  )

  const hello = greeting()

  /*
   * Primer arranque: la API respondió y no hay ni una sola sesión. Sin esto,
   * el Pastor abre la app el primer día y ve un "$0.00" enorme sobre gráficas
   * vacías — que se lee como algo descompuesto, no como algo que aún no empieza.
   */
  const sinNada = !history.loading && !history.error && (history.data?.length ?? 0) === 0

  return (
    /*
     * `data-panes="flat"` apaga el filo de vidrio en TODA la pantalla. Aquí las
     * tarjetas son grandes y desiguales —cifra héroe, bento, cuatro métricas— y
     * ya se separan por tamaño y por dato; ponerles canto a las ocho las
     * convierte en una rejilla, que es justo lo que este tablero evita.
     */
    <div data-panes="flat" className="mx-auto w-full max-w-[86rem]">
      <PageHeader
        eyebrow={todayLabel()}
        title={
          <>
            {hello.text}
            {capabilities?.firstName && (
              <>
                ,<br className="sm:hidden" /> {capabilities.firstName}
              </>
            )}
          </>
        }
        action={
          /* Sin un solo reporte levantado, elegir "3 meses o 6 meses" no
             compara nada. El control aparece cuando empieza a haber historia. */
          sinNada ? undefined : (
            <Segmented
              label="Periodo del resumen"
              options={WINDOWS.map((w) => ({ value: w.value, label: w.label }))}
              value={win}
              onChange={setWin}
            />
          )
        }
      />

      {history.error ? (
        <Card className="p-2">
          <ErrorState error={history.error} onRetry={history.reload} />
        </Card>
      ) : sinNada ? (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 stagger">
          <div className="lg:col-span-7 min-w-0">
            <FirstRun />
          </div>
          <div className="lg:col-span-5 min-w-0">
            <Actions />
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 stagger">
          {/* Fila 1 — el ancla del tablero y lo que se puede hacer hoy. */}
          <div className="lg:col-span-7 min-w-0">
            <IncomeHero data={data} months={months} loading={history.loading} />
          </div>
          <div className="lg:col-span-5 min-w-0">
            <Actions />
          </div>

          {/* Fila 2 — cuatro cifras. */}
          <div className="lg:col-span-12 min-w-0">
            <Numbers data={data} />
          </div>

          {/* Fila 3 — quién vino y qué se levantó. */}
          <div className="lg:col-span-5 min-w-0">
            <Attendance data={data} />
          </div>
          <div className="lg:col-span-7 min-w-0">
            <Recent sessions={data.recent} loading={history.loading} onAll={() => navigate('/historial')} />
          </div>

          {/* Fila 4 — la disciplina de registro, que ninguna cifra promedio dice. */}
          <div className="lg:col-span-12 min-w-0">
            <Consistency data={data} />
          </div>
        </div>
      )}
    </div>
  )
}

/* ── Primer arranque ────────────────────────────────────────────────────── */

/**
 * Lo que se ve antes de que exista el primer reporte.
 *
 * No es una pantalla de relleno: sustituye a las cifras y las gráficas mientras
 * no signifiquen nada, dice sin rodeos que todavía no hay nada, y adelanta qué
 * va a aparecer aquí. El bloque de acciones se conserva al lado, porque es lo
 * único accionable del momento.
 */
function FirstRun() {
  const proximamente = [
    { icon: 'coins' as IconName, label: 'Ofrenda y diezmo del periodo, con su tendencia' },
    { icon: 'group' as IconName, label: 'Cuánta gente asiste a cada grupo' },
    { icon: 'list' as IconName, label: 'Los últimos reportes levantados' },
  ]

  return (
    <Card className="h-full p-7 sm:p-9 flex flex-col">
      <span className="grid place-items-center size-16 rounded-[1.4rem] bg-clay-soft text-clay">
        <Icon name="book" className="size-8" />
      </span>

      <h2 className="mt-6 text-3xl font-semibold tracking-tight text-pretty">
        Aquí aparecerá todo lo que se levante
      </h2>
      <p className="mt-3 text-lg text-ink-soft text-pretty">
        Todavía no hay ningún reporte registrado. En cuanto se levante el primero, este
        tablero se llena solo.
      </p>

      <ul className="mt-7 pt-6 border-t border-line space-y-4">
        {proximamente.map((item) => (
          <li key={item.label} className="flex items-center gap-4">
            <span className="shrink-0 grid place-items-center size-11 rounded-2xl bg-bone-deep text-ink-faint">
              <Icon name={item.icon} className="size-5" />
            </span>
            <span className="text-ink-soft">{item.label}</span>
          </li>
        ))}
      </ul>
    </Card>
  )
}

/* ── Tarjeta héroe ──────────────────────────────────────────────────────── */

function IncomeHero({
  data,
  months,
  loading,
}: {
  data: ReturnType<typeof buildDashboard>
  months: number
  loading: boolean
}) {
  const windowLabel = months === 12 ? 'el último año' : `los últimos ${months} meses`

  return (
    <section className="h-full rounded-hero bg-surface shadow-card p-6 sm:p-8 flex flex-col">
      <div className="flex items-start justify-between gap-4">
        <div className="min-w-0">
          {/* El periodo se calla en el teléfono: ya lo dice el selector de
              arriba, y con él la etiqueta se partía en dos líneas. */}
          <p className="eyebrow text-ink-soft">
            Ofrenda y diezmo<span className="hidden sm:inline"> · {windowLabel}</span>
          </p>
          <p className="mt-2 font-numeral text-5xl sm:text-6xl font-semibold leading-none">
            {loading ? <span className="text-ink-faint/40">$——</span> : formatMoney(data.income)}
          </p>
          <div className="mt-4 flex flex-wrap items-center gap-2.5">
            <Delta {...data.delta} />
            <span className="text-sm text-ink-soft">
              {data.delta.direction === 'flat' && data.delta.value === 'Sin comparación'
                ? 'No hay periodo anterior con qué comparar'
                : 'contra el periodo anterior'}
            </span>
          </div>
        </div>

        <span className="hidden sm:grid shrink-0 place-items-center size-11 rounded-full bg-clay-soft text-clay-deep">
          <Icon name="wallet" className="size-5" strokeWidth={1.9} />
        </span>
      </div>

      <div className="mt-8 flex-1 min-h-0">
        <AreaChart points={data.series} format={formatMoney} tone="clay" height="h-44 sm:h-56" />
      </div>

      {/* El desglose vive dentro de la tarjeta y no en tarjetas aparte: son
          partes del mismo total, y separarlas invitaría a sumarlas otra vez. */}
      <dl className="mt-8 pt-6 border-t border-line grid grid-cols-1 sm:grid-cols-3 gap-y-2.5 gap-x-3">
        <Split label="Ofrenda" value={formatMoney(data.offering)} dot="bg-clay" />
        <Split label="Diezmo" value={formatMoney(data.tithe)} dot="bg-ochre" />
        <Split label="Sesiones" value={String(data.sessionCount)} dot="bg-ink-faint" />
      </dl>
    </section>
  )
}

function Split({ label, value, dot }: { label: string; value: string; dot: string }) {
  return (
    <div className="min-w-0 flex items-baseline justify-between gap-3 sm:block">
      <dt className="flex items-center gap-2 text-sm text-ink-soft">
        <span className={`size-2 shrink-0 rounded-full ${dot}`} />
        {label}
      </dt>
      <dd className="sm:mt-1.5 font-numeral text-lg sm:text-xl font-medium tabular text-ink">{value}</dd>
    </div>
  )
}

/* ── Acciones ───────────────────────────────────────────────────────────── */

/**
 * Lo que esta persona puede hacer HOY, armado de `/api/me/capabilities`.
 * Nada está escrito a mano: si el Pastor le quita un rol a alguien, la tarjeta
 * deja de aparecer la próxima vez que abra la aplicación.
 */
function Actions() {
  const navigate = useNavigate()
  const perms = usePermissions()

  const actions = [
    perms.canCaptureSundaySchool && {
      key: 'ed',
      title: 'Escuela Dominical',
      description: 'Levantar el reporte de tu grupo: asistencia y ofrenda.',
      icon: 'book' as const,
      tone: 'forest' as const,
      to: '/escuela-dominical',
    },
    perms.canCaptureServices && {
      key: 'culto',
      title: 'Reporte de culto',
      description: 'Ofrenda, diezmo y quién predicó.',
      icon: 'coins' as const,
      tone: 'lake' as const,
      to: '/culto',
    },
    perms.canAdminister && {
      key: 'personas',
      title: 'Personas y permisos',
      description: 'Quién puede hacer qué, quién lidera cada grupo.',
      icon: 'group' as const,
      tone: 'clay' as const,
      to: '/personas',
    },
    perms.canAdminister && {
      key: 'grupos',
      title: 'Grupos Familiares',
      description: 'Las casas donde se reúne la iglesia entre semana.',
      icon: 'home' as const,
      tone: 'ochre' as const,
      to: '/grupos',
    },
  ].filter(Boolean) as {
    key: string
    title: string
    description: string
    icon: 'book' | 'coins' | 'group' | 'home'
    tone: 'forest' | 'lake' | 'clay' | 'ochre'
    to: string
  }[]

  if (actions.length === 0) {
    return (
      <Card className="h-full p-7 flex flex-col items-center justify-center text-center gap-4">
        <span className="grid place-items-center size-16 rounded-[1.4rem] bg-bone-deep text-ink-faint">
          <Icon name="clock" className="size-8" />
        </span>
        <p className="text-lg text-ink-soft text-pretty">
          Todavía no tienes tareas asignadas. Cuando el Pastor te asigne alguna, aparecerá aquí.
        </p>
      </Card>
    )
  }

  return (
    <div className="h-full flex flex-col gap-4">
      <p className="eyebrow text-ink-soft px-1">Qué puedes hacer</p>
      <div className="flex-1 grid gap-4" style={{ gridTemplateRows: `repeat(${actions.length}, minmax(0, 1fr))` }}>
        {actions.map((a) => (
          <ActionCard
            key={a.key}
            title={a.title}
            description={a.description}
            icon={a.icon}
            tone={a.tone}
            onClick={() => navigate(a.to)}
          />
        ))}
      </div>
    </div>
  )
}

/* ── Cifras ─────────────────────────────────────────────────────────────── */

/**
 * Las cifras del periodo.
 *
 * Son cuatro cuando hay asistencia registrada y dos cuando no, así que la
 * retícula se dimensiona con las que de verdad hay: dejar huecos en una fila de
 * cuatro se lee como que algo falló al cargar.
 *
 * Antes había una variante para el Pastor —miembros oficiales, cuentas con
 * acceso, cuerpo ejecutivo— construida sobre un endpoint de directorio que
 * nunca existió en el backend: siempre daba 404 y esa rama nunca se ejecutaba.
 * Se eliminó en vez de fingirla.
 */
function Numbers({ data }: { data: ReturnType<typeof buildDashboard> }) {
  const avgOffering = data.sessionCount ? data.income / data.sessionCount : 0

  /*
   * Las dos casillas de asistencia solo existen si alguien tomó lista en el
   * periodo, y el promedio se divide entre ESAS sesiones, no entre todas: el
   * reporte de culto no pregunta asistencia, así que contar esas sesiones como
   * si hubieran tenido cero personas hundía la media a la cuarta parte.
   */
  const hayAsistencia = data.attendanceSessions > 0

  const casillas = [
    { key: 'sesiones', label: 'Sesiones', value: String(data.sessionCount), icon: 'calendar' as const, tone: 'clay' as const },
    hayAsistencia && {
      key: 'total',
      label: 'Asistencia total',
      value: String(data.attendanceTotal),
      unit: 'personas',
      icon: 'group' as const,
      tone: 'lake' as const,
    },
    hayAsistencia && {
      key: 'promedio',
      label: 'Promedio por sesión',
      value: String(Math.round(data.attendanceTotal / data.attendanceSessions)),
      unit: 'personas',
      icon: 'chart' as const,
      tone: 'forest' as const,
    },
    { key: 'entrada', label: 'Entrada promedio', value: shortMoney(avgOffering), icon: 'coins' as const, tone: 'ochre' as const },
  ].filter(Boolean) as {
    key: string
    label: string
    value: string
    unit?: string
    icon: IconName
    tone: 'clay' | 'lake' | 'forest' | 'ochre'
  }[]

  return (
    <div className={`grid gap-4 grid-cols-2 ${casillas.length === 4 ? 'lg:grid-cols-4' : 'lg:grid-cols-2'}`}>
      {casillas.map((c) => (
        <StatTile key={c.key} label={c.label} value={c.value} unit={c.unit} icon={c.icon} tone={c.tone} />
      ))}
    </div>
  )
}

/* ── Asistencia ─────────────────────────────────────────────────────────── */

const SLICE_COLORS = [
  'var(--color-clay)',
  'var(--color-lake)',
  'var(--color-forest)',
  'var(--color-ochre)',
  'var(--color-ink-faint)',
]

function Attendance({ data }: { data: ReturnType<typeof buildDashboard> }) {
  const slices = data.attendance.slice(0, 5).map((a, i) => ({ ...a, color: SLICE_COLORS[i] }))
  // Entre las sesiones que tomaron lista, no entre todas. Ver lib/stats.ts.
  const average = data.attendanceSessions
    ? Math.round(data.attendanceTotal / data.attendanceSessions)
    : 0

  return (
    <Card className="h-full p-6 flex flex-col">
      <CardHead eyebrow="Personas presentes" title="Asistencia por grupo" />

      {slices.length === 0 ? (
        /* Dos vacíos distintos, y confundirlos manda a buscar el problema al
           sitio equivocado: uno es que no se levantó nada, el otro que sí se
           levantó pero sin pasar lista. */
        <p className="mt-8 text-ink-soft text-pretty">
          {data.sessionCount === 0
            ? 'Sin sesiones registradas en este periodo.'
            : 'En este periodo no se tomó asistencia en ninguna sesión.'}
        </p>
      ) : (
        <>
          <div className="mt-6 flex flex-col sm:flex-row lg:flex-col items-center gap-6">
            <Donut
              slices={slices}
              centerValue={data.attendanceTotal.toLocaleString('es-MX')}
              centerLabel="asistencias"
              size="size-48"
            />
            <div className="w-full min-w-0">
              <Legend slices={slices} format={(v) => v.toLocaleString('es-MX')} />
            </div>
          </div>

          {/* Dice sobre cuántas sesiones se promedia. Sin eso, el número parece
              el promedio de TODAS las sesiones del periodo y no lo es. */}
          <p className="mt-auto pt-6 text-ink-soft leading-snug text-pretty">
            <strong className="text-ink font-medium tabular">{average}</strong> personas por sesión,
            en promedio, sobre las{' '}
            <strong className="text-ink font-medium tabular">{data.attendanceSessions}</strong>{' '}
            {data.attendanceSessions === 1 ? 'sesión que tomó' : 'sesiones que tomaron'} asistencia.
          </p>
        </>
      )}
    </Card>
  )
}

/* ── Actividad ──────────────────────────────────────────────────────────── */

function Recent({
  sessions,
  loading,
  onAll,
}: {
  sessions: ServiceSessionSummary[]
  loading: boolean
  onAll: () => void
}) {
  return (
    <Card className="h-full p-6 flex flex-col">
      <CardHead
        eyebrow="Lo último levantado"
        title="Actividad reciente"
        action={
          <Button variant="quiet" size="sm" iconAfter="next" onClick={onAll}>
            Ver todo
          </Button>
        }
      />

      {loading ? (
        <ul className="mt-5 space-y-2.5">
          {[0, 1, 2, 3].map((i) => (
            <li key={i} className="h-16 rounded-card bg-bone-deep/60 animate-pulse" />
          ))}
        </ul>
      ) : sessions.length === 0 ? (
        <p className="mt-8 text-ink-soft">Cuando se levante el primer reporte, aparecerá aquí.</p>
      ) : (
        <ul className="mt-5 -mx-2 flex-1 space-y-0.5">
          {sessions.map((s) => (
            <SessionRow key={s.sessionId} session={s} />
          ))}
        </ul>
      )}
    </Card>
  )
}

function SessionRow({ session }: { session: ServiceSessionSummary }) {
  const isSchool = session.societyName !== null
  const total = session.totalOffering + (session.totalTithe ?? 0)
  const who = session.recordedByName.split(' ')

  return (
    <li className="flex items-center gap-3 px-2 py-2.5 rounded-card transition-colors hover:bg-bone-deep/45">
      <span
        className={`shrink-0 grid place-items-center size-10 sm:size-11 rounded-[0.9rem]
                    ${isSchool ? 'bg-forest-soft text-forest' : 'bg-clay-soft text-clay-deep'}`}
      >
        <Icon name={isSchool ? 'book' : 'coins'} className="size-5" strokeWidth={2} />
      </span>

      {/* Sin `truncate`: "Sociedad de D…" no identifica ningún grupo. Que el
          nombre ocupe dos líneas cuesta 20px y se entiende. */}
      <span className="min-w-0 flex-1">
        <span className="block font-medium leading-snug">
          {isSchool ? session.societyName : session.serviceTypeName}
        </span>
        <span className="block text-sm text-ink-soft leading-snug">
          {formatCompactDate(session.sessionDate)}
          {/* La asistencia solo se menciona si se registró. Ver lib/stats.ts. */}
          {session.presentCount > 0 && ` · ${session.presentCount} personas`}
        </span>
      </span>

      <span className="hidden xl:block shrink-0" title={`Levantado por ${session.recordedByName}`}>
        <Avatar person={{ firstName: who[0] ?? '', lastName: who[1] ?? '' }} size="xs" />
      </span>

      <span className="shrink-0 text-right">
        <span className="block font-numeral text-lg font-medium tabular leading-tight">
          {shortMoney(total)}
        </span>
        <span
          className={`block text-xs font-medium ${
            session.totalTithe !== null ? 'text-ochre-deep' : 'text-ink-faint'
          }`}
        >
          {session.totalTithe !== null ? 'con diezmo' : 'ofrenda'}
        </span>
      </span>
    </li>
  )
}

/* ── Constancia ─────────────────────────────────────────────────────────── */

function Consistency({ data }: { data: ReturnType<typeof buildDashboard> }) {
  const withReports = data.consistency.filter((c) => c.level > 0).length

  return (
    <Card className="p-6">
      <div className="flex flex-col lg:flex-row lg:items-center gap-6">
        <div className="lg:w-72 shrink-0">
          <CardHead eyebrow="Últimos seis meses" title="Constancia del registro" />
          <p className="mt-3 text-ink-soft leading-snug">
            <strong className="text-ink font-medium tabular">{withReports}</strong> de{' '}
            <span className="tabular">{data.consistency.length}</span> domingos con al menos un reporte
            levantado.
          </p>
          <div className="mt-4 flex items-center gap-3">
            <span className="text-xs text-ink-faint">Menos</span>
            <span className="flex gap-1">
              {['bg-bone-deep', 'bg-forest-soft', 'bg-forest/40', 'bg-forest'].map((c) => (
                <span key={c} className={`size-3.5 rounded-[0.3rem] ${c}`} />
              ))}
            </span>
            <span className="text-xs text-ink-faint">Más</span>
          </div>
        </div>

        <div className="flex-1 min-w-0">
          {/* 13 columnas en el teléfono (dos filas) y 26 en escritorio (una). */}
          <div className="sm:hidden">
            <DotMatrix cells={data.consistency} columns={13} />
          </div>
          <div className="hidden sm:block">
            <DotMatrix cells={data.consistency} columns={26} />
          </div>
        </div>
      </div>
    </Card>
  )
}
