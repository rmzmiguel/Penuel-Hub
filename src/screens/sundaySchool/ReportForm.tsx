import { useMemo, useState } from 'react'
import type { ApiError } from '../../api/client'
import { directory, sundaySchool } from '../../api/endpoints'
import type { PersonOption, SocietyOption } from '../../api/types'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { MoneyField } from '../../components/Field'
import { ErrorState, Loading } from '../../components/Feedback'
import { Icon } from '../../components/Icon'
import { PersonPicker } from '../../components/PersonPicker'
import { Screen } from '../../components/Screen'
import { useAsync } from '../../lib/useAsync'
import { capitalize, formatLongDate, fullName, lastSundayIso, todayIso } from '../../lib/format'
import { AttendanceRow } from './AttendanceRow'
import type { AttendanceDraft } from './AttendanceRow'

export interface SubmittedReport {
  societyName: string
  sessionDate: string
  presentCount: number
  totalOffering: number
  teacherName: string
}

export function ReportForm({
  serviceTypeId,
  society,
  teacher,
  onChangeTeacher,
  onBack,
  onDone,
}: {
  serviceTypeId: string
  society: SocietyOption
  teacher: { personId: string | null; name: string }
  onChangeTeacher: () => void
  onBack: () => void
  onDone: (report: SubmittedReport) => void
}) {
  const members = useAsync(
    (signal) => directory.societyMembers(society.societyId, signal),
    [society.societyId],
  )

  const [drafts, setDrafts] = useState<AttendanceDraft[] | null>(null)
  const [sessionDate, setSessionDate] = useState(lastSundayIso())
  const [offering, setOffering] = useState('')
  const [picking, setPicking] = useState(false)
  const [submitError, setSubmitError] = useState<ApiError | null>(null)
  const [busy, setBusy] = useState(false)

  // Los integrantes del grupo se precargan una sola vez; a partir de ahí manda
  // lo que el maestro haya tocado. Recargar encima de sus cambios sería cruel.
  const list =
    drafts ??
    (members.data?.members.map<AttendanceDraft>((person) => ({
      person,
      wasPresent: false,
      wasPunctual: true,
      broughtBible: false,
      chaptersRead: 0,
      isGuest: false,
    })) ?? [])

  const presentCount = list.filter((d) => d.wasPresent).length
  const everyonePresent = list.length > 0 && presentCount === list.length

  const offeringValue = useMemo(() => {
    const n = Number.parseFloat(offering)
    return Number.isFinite(n) ? n : 0
  }, [offering])

  function update(next: AttendanceDraft[]) {
    setDrafts(next)
  }

  function addGuest(person: PersonOption) {
    update([
      ...list,
      {
        person,
        wasPresent: true,
        wasPunctual: true,
        broughtBible: false,
        chaptersRead: 0,
        isGuest: true,
      },
    ])
    setPicking(false)
  }

  async function submit() {
    setBusy(true)
    setSubmitError(null)
    try {
      await sundaySchool.submitReport({
        serviceTypeId,
        societyId: society.societyId,
        sessionDate,
        totalOffering: offeringValue,
        teacherPersonId: teacher.personId,
        // Se manda TODA la lista, presentes y ausentes: la ausencia es un dato,
        // no la falta de uno. El backend guarda ambos por igual.
        attendances: list.map((d) => ({
          personId: d.person.id,
          wasPresent: d.wasPresent,
          wasPunctual: d.wasPresent ? d.wasPunctual : null,
          broughtBible: d.wasPresent ? d.broughtBible : null,
          chaptersRead: d.wasPresent ? d.chaptersRead : null,
        })),
      })
      onDone({
        societyName: society.societyName,
        sessionDate,
        presentCount,
        totalOffering: offeringValue,
        teacherName: teacher.name,
      })
    } catch (e) {
      setSubmitError(e as ApiError)
      setBusy(false)
      window.scrollTo({ top: 0, behavior: 'smooth' })
    }
  }

  if (members.loading) {
    return (
      <Screen title={society.societyName} subtitle="Escuela Dominical" onBack={onBack}>
        <Loading label="Cargando tu grupo…" />
      </Screen>
    )
  }

  if (members.error) {
    return (
      <Screen title={society.societyName} subtitle="Escuela Dominical" onBack={onBack}>
        <ErrorState error={members.error} onRetry={members.reload} />
      </Screen>
    )
  }

  return (
    <>
      <Screen
        title={society.societyName}
        subtitle="Reporte de Escuela Dominical"
        onBack={onBack}
        footer={
          <Button full loading={busy} icon={busy ? undefined : 'check'} onClick={submit}>
            {busy ? 'Guardando…' : 'Guardar reporte'}
          </Button>
        }
      >
        {submitError && (
          <div className="mb-6">
            <Card tone="plain" className="border-danger/30 bg-danger-soft p-5">
              <p className="flex items-start gap-3 text-danger font-semibold">
                <Icon name="alert" className="size-6 shrink-0 mt-0.5" />
                <span>{submitError.message}</span>
              </p>
            </Card>
          </div>
        )}

        {/* ── Fecha y maestro ─────────────────────────────────────────── */}
        <Card className="divide-y divide-line">
          {/* El input de fecha cubre TODA la fila en transparente: así se abre el
              selector nativo tocando en cualquier parte, y no hay que acertarle a
              un control diminuto. Debajo se muestra la fecha ya en palabras. */}
          <div className="relative flex items-start gap-4 px-5 py-4 min-h-touch-lg">
            <span className="shrink-0 grid place-items-center size-11 rounded-2xl bg-bone-deep text-ink-soft">
              <Icon name="calendar" className="size-5" />
            </span>
            <span className="flex-1 min-w-0">
              <span className="block text-sm text-ink-soft">Domingo del reporte</span>
              <span className="block font-semibold leading-snug">
                {capitalize(formatLongDate(sessionDate))}
              </span>
            </span>
            <span aria-hidden="true" className="shrink-0 text-clay font-semibold text-sm pt-0.5">
              Cambiar
            </span>
            <input
              type="date"
              value={sessionDate}
              max={todayIso()}
              onChange={(e) => e.target.value && setSessionDate(e.target.value)}
              aria-label="Fecha del reporte"
              className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
            />
          </div>

          <div className="flex items-start gap-4 px-5 py-4 min-h-touch-lg">
            <span className="shrink-0 grid place-items-center size-11 rounded-2xl bg-bone-deep text-ink-soft">
              <Icon name="person" className="size-5" />
            </span>
            <span className="flex-1 min-w-0">
              <span className="block text-sm text-ink-soft">Quién dio la clase</span>
              <span className="block font-semibold leading-snug">{teacher.name}</span>
            </span>
            <button
              type="button"
              onClick={onChangeTeacher}
              className="shrink-0 min-h-11 px-3 -mr-2 -mt-1 rounded-xl text-clay font-semibold
                         text-sm hover:bg-clay-soft transition"
            >
              Cambiar
            </button>
          </div>
        </Card>

        {/* ── Asistencia ──────────────────────────────────────────────── */}
        <div className="mt-8">
          <div className="flex items-baseline justify-between gap-4">
            <h2 className="font-display text-2xl font-semibold">Asistencia</h2>
            <p className="text-ink-soft shrink-0 tabular">
              {presentCount} de {list.length}
            </p>
          </div>
          {list.length > 0 && (
            <div className="mt-3">
              <Button
                variant="secondary"
                full
                icon={everyonePresent ? 'minus' : 'check'}
                onClick={() => update(list.map((d) => ({ ...d, wasPresent: !everyonePresent })))}
              >
                {everyonePresent ? 'Quitar todos' : 'Marcar todos presentes'}
              </Button>
            </div>
          )}
        </div>

        {list.length === 0 ? (
          <Card className="mt-4 p-7 text-center">
            <p className="text-lg text-ink-soft">
              Este grupo todavía no tiene integrantes registrados. Puedes agregar a las
              personas que vinieron hoy con el botón de abajo.
            </p>
          </Card>
        ) : (
          <ul className="mt-4 space-y-3">
            {list.map((draft, i) => (
              <AttendanceRow
                key={draft.person.id}
                draft={draft}
                onChange={(next) => update(list.map((d, j) => (j === i ? next : d)))}
                onRemove={draft.isGuest ? () => update(list.filter((_, j) => j !== i)) : undefined}
              />
            ))}
          </ul>
        )}

        <div className="mt-4">
          <Button variant="secondary" full icon="plus" onClick={() => setPicking(true)}>
            Agregar a alguien más
          </Button>
        </div>

        {/* ── Ofrenda ─────────────────────────────────────────────────── */}
        <div className="mt-10">
          <h2 className="font-display text-2xl font-semibold mb-4">Ofrenda del grupo</h2>
          <Card className="p-5 sm:p-6">
            <MoneyField
              label="Total recogido"
              value={offering}
              onChange={setOffering}
              hint="Si no se recogió nada, déjalo vacío."
            />
          </Card>
        </div>

        <div className="h-4" />
      </Screen>

      {picking && (
        <PersonPicker
          title="Agregar a la lista"
          description="Búscala por su nombre. Puede ser una visita o alguien de otro grupo."
          excludeIds={list.map((d) => d.person.id)}
          onPick={addGuest}
          onCancel={() => setPicking(false)}
        />
      )}
    </>
  )
}

export const teacherLabel = (p: { firstName: string; lastName: string }) => fullName(p)
