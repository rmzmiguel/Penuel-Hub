import { useMemo, useState } from 'react'
import { ApiError } from '../../api/client'
import { familyGroups } from '../../api/familyGroups'
import type { MyFamilyGroup } from '../../api/types'
import { Button } from '../../components/Button'
import { SuccessScreen } from '../../components/Feedback'
import { Field } from '../../components/Field'
import { Icon } from '../../components/Icon'
import { Screen } from '../../components/Screen'
import { Avatar } from '../../components/ui/Avatar'
import { formatLongDate, formatMoney, fullName, todayIso } from '../../lib/format'

/**
 * El reporte de la reunión. Es lo que sustituye la hoja de papel, y por eso son
 * exactamente tres cosas: la fecha, quién vino y cuánto se ofrendó.
 *
 * No hay puntualidad, ni Biblia, ni capítulos leídos — eso es de Escuela Dominical.
 * Meterlos aquí convertiría un formulario de dos campos en uno de cinco para gente
 * que hoy lo lleva a mano.
 *
 * La fecha viene por defecto en HOY y no en "el jueves pasado": el grupo levanta su
 * reporte al terminar la reunión, esa misma noche. Y se puede cambiar sin que el
 * sistema opine, porque el día habitual es informativo (regla 7.7).
 */
export function FamilyGroupReportFlow({
  group,
  onClose,
  onDone,
}: {
  group: MyFamilyGroup
  onClose: () => void
  onDone: () => void
}) {
  const [fecha, setFecha] = useState(todayIso)
  const [ofrenda, setOfrenda] = useState('')
  const [presentes, setPresentes] = useState<Set<string>>(() => new Set())
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [guardado, setGuardado] = useState<{ presentes: number; ofrenda: number } | null>(null)

  const total = useMemo(() => Number(ofrenda.replace(',', '.')) || 0, [ofrenda])

  function alternar(personId: string) {
    setPresentes((prev) => {
      const next = new Set(prev)
      if (next.has(personId)) next.delete(personId)
      else next.add(personId)
      return next
    })
  }

  async function guardar() {
    setBusy(true)
    setError(null)
    try {
      await familyGroups.submitReport(
        group.familyGroupId,
        fecha,
        total,
        group.members.map((m) => ({ personId: m.personId, wasPresent: presentes.has(m.personId) })),
      )
      setGuardado({ presentes: presentes.size, ofrenda: total })
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo guardar el reporte.')
      setBusy(false)
    }
  }

  if (guardado) {
    return (
      <SuccessScreen
        title="Reporte guardado"
        summary={[
          { label: 'Reunión', value: formatLongDate(fecha) },
          { label: 'Presentes', value: `${guardado.presentes} de ${group.members.length}` },
          { label: 'Ofrenda', value: formatMoney(guardado.ofrenda) },
        ]}
        primaryLabel="Volver a mi grupo"
        onPrimary={onDone}
      />
    )
  }

  return (
    <Screen
      title="Reporte de la reunión"
      subtitle={group.address}
      onBack={onClose}
      footer={
        <Button full size="lg" icon="check" loading={busy} onClick={guardar}>
          Guardar reporte
        </Button>
      }
    >
      <div className="space-y-5">
        <div className="rounded-card bg-surface pane shadow-card p-5 space-y-5">
          <Field
            label="Fecha de la reunión"
            type="date"
            icon="calendar"
            value={fecha}
            max={todayIso()}
            onChange={(e) => setFecha(e.target.value)}
            hint="Por lo general hoy. Cámbiala si estás capturando una reunión anterior."
          />

          <Field
            label="Ofrenda total"
            type="text"
            inputMode="decimal"
            icon="coins"
            value={ofrenda}
            onChange={(e) => setOfrenda(e.target.value.replace(/[^\d.,]/g, ''))}
            placeholder="0.00"
            hint="Lo que se recogió en la reunión. Si no hubo, déjalo en cero."
          />
        </div>

        <section className="rounded-card bg-surface pane shadow-card overflow-hidden">
          <div className="px-5 sm:px-6 pt-5 pb-3 flex items-baseline justify-between gap-3">
            <h2 className="font-display text-xl font-medium leading-tight">Quiénes vinieron</h2>
            <span className="shrink-0 text-ink-soft tabular">
              {presentes.size} de {group.members.length}
            </span>
          </div>

          {/* Marcar a todos de una vez: en un grupo pequeño lo normal es que venga
              casi todo el mundo, y once toques para decir "vinieron todos" es
              exactamente el tipo de fricción que hace volver al papel. */}
          <div className="px-5 sm:px-6 pb-3">
            <Button
              variant="secondary"
              full
              icon={presentes.size === group.members.length ? 'undo' : 'check'}
              onClick={() =>
                setPresentes(
                  presentes.size === group.members.length
                    ? new Set()
                    : new Set(group.members.map((m) => m.personId)),
                )
              }
            >
              {presentes.size === group.members.length ? 'Quitar a todos' : 'Marcar todos presentes'}
            </Button>
          </div>

          <ul className="divide-y divide-line">
            {group.members.map((m) => {
              const presente = presentes.has(m.personId)
              return (
                <li key={m.personId}>
                  <button
                    type="button"
                    onClick={() => alternar(m.personId)}
                    aria-pressed={presente}
                    className="w-full flex items-center gap-4 px-5 sm:px-6 py-3 min-h-touch-lg
                               text-left transition active:scale-[0.99]"
                  >
                    {/* La marca lleva palomita además de color: el verde solo no
                        basta para quien no lo distingue del gris. */}
                    <span
                      className={`shrink-0 grid place-items-center size-12 rounded-2xl transition-colors
                                  ${presente ? 'bg-forest text-on-accent' : 'bg-bone-deep text-ink-faint'}`}
                    >
                      {presente ? (
                        <Icon name="check" className="size-6" strokeWidth={2.6} />
                      ) : (
                        <Avatar person={m} size="sm" />
                      )}
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block font-medium text-lg leading-snug">{fullName(m)}</span>
                      <span
                        className={`block text-sm leading-snug ${presente ? 'text-forest' : 'text-ink-faint'}`}
                      >
                        {presente ? 'Presente' : 'Toca para marcar presente'}
                      </span>
                    </span>
                  </button>
                </li>
              )
            })}
          </ul>
        </section>

        {error && (
          <p
            role="alert"
            className="flex items-start gap-3 rounded-card bg-danger-soft border border-danger-line
                       px-5 py-4 text-danger font-semibold"
          >
            <Icon name="alert" className="size-5 shrink-0 mt-0.5" />
            <span>{error}</span>
          </p>
        )}
      </div>
    </Screen>
  )
}
