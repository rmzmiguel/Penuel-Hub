import { useMemo, useState } from 'react'
import { services } from '../api/endpoints'
import type { ServiceSessionSummary } from '../api/types'
import { Card } from '../components/Card'
import { EmptyState, ErrorState, Loading } from '../components/Feedback'
import { Icon } from '../components/Icon'
import { Avatar } from '../components/ui/Avatar'
import { Chip, Segmented } from '../components/ui/Chip'
import { PageHeader } from '../components/ui/PageHeader'
import { useAsync } from '../lib/useAsync'
import { capitalize, formatMoney } from '../lib/format'
import { fromIso } from '../lib/stats'

type Filter = 'todo' | 'cultos' | 'escuela'

/**
 * Historial de lo capturado.
 *
 * No lleva NINGÚN filtro por permisos aquí: el backend ya devuelve solo lo que
 * a cada quien le toca ver — quien únicamente captura Escuela Dominical no
 * recibe los cultos generales. Filtrar otra vez en el cliente sería duplicar
 * una regla de negocio en el lugar equivocado.
 *
 * Los filtros de esta pantalla son de LECTURA, no de permiso: acotan lo que ya
 * llegó, y por eso llevan el conteo a la vista.
 */
export function HistoryScreen() {
  const [filter, setFilter] = useState<Filter>('todo')
  const history = useAsync((signal) => services.history(signal), [])
  const all = history.data ?? []

  const counts = useMemo(
    () => ({
      todo: all.length,
      cultos: all.filter((s) => s.societyName === null).length,
      escuela: all.filter((s) => s.societyName !== null).length,
    }),
    [all],
  )

  // Agrupado por mes. Un listado de 200 filas sin cortes es una pared: los
  // encabezados de mes son lo que deja recorrerlo con el pulgar sin perderse.
  const months = useMemo(() => {
    const visible = all.filter((s) =>
      filter === 'todo' ? true : filter === 'escuela' ? s.societyName !== null : s.societyName === null,
    )

    const groups = new Map<string, { label: string; total: number; sessions: ServiceSessionSummary[] }>()
    for (const s of visible) {
      const d = fromIso(s.sessionDate)
      const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
      const label = capitalize(new Intl.DateTimeFormat('es-MX', { month: 'long', year: 'numeric' }).format(d))
      const g = groups.get(key) ?? { label, total: 0, sessions: [] }
      g.total += s.totalOffering + (s.totalTithe ?? 0)
      g.sessions.push(s)
      groups.set(key, g)
    }
    return [...groups.values()]
  }, [all, filter])

  return (
    <div className="mx-auto w-full max-w-4xl">
      <PageHeader
        eyebrow={all.length ? `${all.length} reportes levantados` : 'Historial'}
        title="Historial"
        lead="Lo que se ha levantado, del más reciente al más antiguo."
        action={
          <Segmented
            label="Filtrar el historial"
            value={filter}
            onChange={setFilter}
            options={[
              { value: 'todo', label: 'Todo', count: counts.todo },
              { value: 'cultos', label: 'Cultos', count: counts.cultos },
              { value: 'escuela', label: 'Escuela', count: counts.escuela },
            ]}
          />
        }
      />

      {history.loading ? (
        <Loading label="Cargando el historial…" />
      ) : history.error ? (
        <Card className="p-2">
          <ErrorState error={history.error} onRetry={history.reload} />
        </Card>
      ) : months.length === 0 ? (
        <EmptyState
          title="Todavía no hay reportes"
          description="Cuando se levante el primero, aparecerá aquí."
        />
      ) : (
        <div className="space-y-8">
          {months.map((month) => (
            <section key={month.label}>
              {/* El encabezado de mes se queda pegado bajo la barra superior del
                  teléfono: al recorrer una lista larga siempre se sabe en qué mes
                  se está sin volver a subir. */}
              <div className="sticky top-16 lg:top-0 z-20 -mx-1 px-1 py-2 frost">
                <div className="flex items-baseline justify-between gap-4">
                  <h2 className="font-display text-xl font-medium">{month.label}</h2>
                  <span className="text-sm text-ink-soft tabular">
                    {month.sessions.length} · {formatMoney(month.total)}
                  </span>
                </div>
              </div>

              <ul className="mt-3 space-y-3 stagger">
                {month.sessions.map((session) => (
                  <SessionCard key={session.sessionId} session={session} />
                ))}
              </ul>
            </section>
          ))}
        </div>
      )}
    </div>
  )
}

function SessionCard({ session }: { session: ServiceSessionSummary }) {
  const isSchool = session.societyName !== null
  const recorder = session.recordedByName.split(' ')

  const stats = [
    { label: 'Ofrenda', value: formatMoney(session.totalOffering), strong: true },
    session.totalTithe !== null && { label: 'Diezmo', value: formatMoney(session.totalTithe), strong: true },
    /* Sin asistencia registrada no se enseña la casilla. Un "Presentes 0" no
       dice que no fue nadie: dice que no se preguntó — el reporte de culto ni
       siquiera toma lista. Ver la nota de `withAttendance` en lib/stats.ts. */
    session.presentCount > 0 && { label: 'Presentes', value: String(session.presentCount), strong: false },
    isSchool
      ? session.teacherName && { label: 'Maestro', value: session.teacherName, strong: false }
      : session.preacherName && { label: 'Predicó', value: session.preacherName, strong: false },
  ].filter(Boolean) as { label: string; value: string; strong: boolean }[]

  return (
    <li>
      <Card tone={isSchool ? 'forest' : 'clay'} className="p-5">
        <div className="flex items-start gap-4">
          <span
            className={`shrink-0 grid place-items-center size-12 rounded-[1rem]
                        ${isSchool ? 'bg-forest-soft text-forest' : 'bg-clay-soft text-clay-deep'}`}
          >
            <Icon name={isSchool ? 'book' : 'coins'} className="size-5.5" strokeWidth={2} />
          </span>

          <div className="min-w-0 flex-1">
            <h3 className="font-display text-lg font-medium leading-tight">
              {isSchool ? session.societyName : session.serviceTypeName}
            </h3>
            <p className="text-sm text-ink-soft">
              {capitalize(
                new Intl.DateTimeFormat('es-MX', { weekday: 'long', day: 'numeric', month: 'long' })
                  .format(fromIso(session.sessionDate)),
              )}
              {isSchool && ` · ${session.serviceTypeName}`}
            </p>
          </div>

          {session.totalTithe !== null && <Chip tone="ochre" icon="wallet">Con diezmo</Chip>}
        </div>

        <dl className="mt-5 grid grid-cols-2 sm:grid-cols-4 gap-x-4 gap-y-4">
          {stats.map((s) => (
            <div key={s.label} className="min-w-0">
              <dt className="eyebrow text-ink-soft">{s.label}</dt>
              <dd
                className={`mt-1 leading-snug ${
                  s.strong ? 'font-numeral text-xl font-medium tabular' : 'font-medium'
                }`}
              >
                {s.value}
              </dd>
            </div>
          ))}
        </dl>

        <div className="mt-5 pt-4 border-t border-line/70 flex items-center gap-2.5 text-sm text-ink-soft">
          <Avatar person={{ firstName: recorder[0] ?? '', lastName: recorder[1] ?? '' }} size="xs" />
          <span className="leading-snug">Levantado por {session.recordedByName}</span>
        </div>
      </Card>
    </li>
  )
}
