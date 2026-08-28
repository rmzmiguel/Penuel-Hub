import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { directory, sundaySchool } from '../../api/endpoints'
import type { PersonOption, SocietyOption, TeacherOption } from '../../api/types'
import { CaptureMode } from '../../api/types'
import { useAuth } from '../../auth/AuthProvider'
import { Card } from '../../components/Card'
import { Button } from '../../components/Button'
import { ErrorState, Loading, SuccessScreen } from '../../components/Feedback'
import { Icon } from '../../components/Icon'
import { PersonPicker } from '../../components/PersonPicker'
import { Screen } from '../../components/Screen'
import { useAsync } from '../../lib/useAsync'
import { capitalize, formatLongDate, formatMoney, fullName } from '../../lib/format'
import { ReportForm } from './ReportForm'
import type { SubmittedReport } from './ReportForm'

type Step =
  | { name: 'group' }
  | { name: 'teacher'; society: SocietyOption }
  | { name: 'report'; society: SocietyOption; teacher: { personId: string | null; name: string } }
  | { name: 'done'; report: SubmittedReport }

/**
 * Los tres escenarios de captura que el backend ya resuelve (Sección 8.2):
 *   grupo fijo único  -> directo al reporte, sin preguntar nada
 *   varios grupos     -> se elige cuál
 *   sin grupo fijo    -> se elige el grupo y luego quién dio la clase
 *
 * El frontend NO adivina ninguno: se lo pregunta al backend cada vez.
 */
export function SundaySchoolFlow() {
  const navigate = useNavigate()
  const { capabilities } = useAuth()
  const context = useAsync((signal) => sundaySchool.captureContext(signal), [])
  const types = useAsync((signal) => directory.serviceTypes(signal), [])
  const [step, setStep] = useState<Step | null>(null)

  const myName = capabilities ? fullName(capabilities) : 'Yo'

  const serviceTypeId = useMemo(
    () => types.data?.find((t) => t.requiresSocietyGrouping)?.id ?? null,
    [types.data],
  )

  // El paso inicial se deduce del modo que devolvió el backend.
  const current: Step | null = useMemo(() => {
    if (step) return step
    if (!context.data) return null
    const c = context.data
    if (c.mode === CaptureMode.SingleFixedGroup && c.mySocieties.length === 1) {
      return {
        name: 'report',
        society: c.mySocieties[0],
        teacher: { personId: c.personId, name: myName },
      }
    }
    return { name: 'group' }
  }, [step, context.data, myName])

  if (context.loading || types.loading) {
    return (
      <Screen title="Escuela Dominical" onBack={() => navigate('/')}>
        <Loading label="Preparando tu reporte…" />
      </Screen>
    )
  }

  if (context.error || types.error) {
    return (
      <Screen title="Escuela Dominical" onBack={() => navigate('/')}>
        <ErrorState
          error={(context.error ?? types.error)!}
          onRetry={() => {
            context.reload()
            types.reload()
          }}
        />
      </Screen>
    )
  }

  if (!serviceTypeId) {
    return (
      <Screen title="Escuela Dominical" onBack={() => navigate('/')}>
        <Card className="p-7 text-center">
          <p className="text-lg text-ink-soft">
            No se encontró el tipo de servicio de Escuela Dominical en el sistema.
            Avísale al Pastor.
          </p>
        </Card>
      </Screen>
    )
  }

  const ctx = context.data!

  if (current?.name === 'done') {
    const r = current.report
    return (
      <SuccessScreen
        title="Reporte guardado"
        summary={[
          { label: 'Grupo', value: r.societyName },
          { label: 'Domingo', value: capitalize(formatLongDate(r.sessionDate)) },
          { label: 'Dio la clase', value: r.teacherName },
          { label: 'Presentes', value: String(r.presentCount) },
          { label: 'Ofrenda', value: formatMoney(r.totalOffering) },
        ]}
        secondary={
          <Button variant="secondary" full icon="book" onClick={() => setStep({ name: 'group' })}>
            Levantar otro grupo
          </Button>
        }
      />
    )
  }

  if (current?.name === 'teacher') {
    return (
      <TeacherPicker
        society={current.society}
        myPersonId={ctx.personId}
        myName={myName}
        onBack={() => setStep({ name: 'group' })}
        onPick={(teacher) =>
          setStep({ name: 'report', society: current.society, teacher })
        }
      />
    )
  }

  if (current?.name === 'report') {
    return (
      <ReportForm
        serviceTypeId={serviceTypeId}
        society={current.society}
        teacher={current.teacher}
        onChangeTeacher={() => setStep({ name: 'teacher', society: current.society })}
        onBack={() =>
          ctx.mode === CaptureMode.SingleFixedGroup
            ? navigate('/')
            : setStep({ name: 'group' })
        }
        onDone={(report) => setStep({ name: 'done', report })}
      />
    )
  }

  // Elegir grupo. Quien tiene grupos fijos ve los suyos primero, pero SIEMPRE
  // puede reportar otro: cubrir a alguien más es normal, no una excepción.
  const mine = ctx.mySocieties
  const others = ctx.allSocieties.filter(
    (s) => !mine.some((m) => m.societyId === s.societyId),
  )

  const choose = (society: SocietyOption) =>
    setStep(
      mine.some((m) => m.societyId === society.societyId)
        ? { name: 'report', society, teacher: { personId: ctx.personId, name: myName } }
        : { name: 'teacher', society },
    )

  return (
    <Screen
      title="Escuela Dominical"
      subtitle="¿De qué grupo vas a levantar el reporte?"
      onBack={() => navigate('/')}
    >
      {mine.length > 0 && (
        <>
          <h2 className="font-display text-xl font-semibold mb-3">
            {mine.length === 1 ? 'Tu grupo' : 'Tus grupos'}
          </h2>
          <ul className="space-y-3">
            {mine.map((s) => (
              <GroupOption key={s.societyId} society={s} onClick={() => choose(s)} highlight />
            ))}
          </ul>
        </>
      )}

      {others.length > 0 && (
        <>
          <h2 className={`font-display text-xl font-semibold mb-3 ${mine.length ? 'mt-8' : ''}`}>
            {mine.length > 0 ? 'Otros grupos' : 'Grupos'}
          </h2>
          {mine.length > 0 && (
            <p className="text-ink-soft mb-3 -mt-1">
              Por si hoy cubriste a otro maestro.
            </p>
          )}
          <ul className="space-y-3">
            {others.map((s) => (
              <GroupOption key={s.societyId} society={s} onClick={() => choose(s)} />
            ))}
          </ul>
        </>
      )}
    </Screen>
  )
}

function GroupOption({
  society,
  onClick,
  highlight = false,
}: {
  society: SocietyOption
  onClick: () => void
  highlight?: boolean
}) {
  return (
    <li>
      <button
        type="button"
        onClick={onClick}
        className={`w-full flex items-center gap-4 rounded-card border px-5 py-5
                    min-h-touch-lg text-left shadow-card transition
                    hover:shadow-raised active:scale-[0.99]
                    ${highlight ? 'bg-surface border-forest-line' : 'bg-surface-warm border-line'}`}
      >
        <span
          className={`shrink-0 grid place-items-center size-12 rounded-2xl
                      ${highlight ? 'bg-forest-soft text-forest' : 'bg-bone-deep text-ink-soft'}`}
        >
          <Icon name="group" className="size-6" />
        </span>
        <span className="flex-1 min-w-0">
          <span className="block font-display text-2xl font-semibold leading-tight">
            {society.societyName}
          </span>
          <span className="block text-sm text-ink-soft">
            {society.teacherCandidates.length === 0
              ? 'Sin maestros registrados'
              : `${society.teacherCandidates.length} ${
                  society.teacherCandidates.length === 1
                    ? 'maestro registrado'
                    : 'maestros registrados'
                }`}
          </span>
        </span>
        <Icon name="next" className="shrink-0 size-6 text-ink-faint" />
      </button>
    </li>
  )
}

function TeacherPicker({
  society,
  myPersonId,
  myName,
  onBack,
  onPick,
}: {
  society: SocietyOption
  myPersonId: string
  myName: string
  onBack: () => void
  onPick: (teacher: { personId: string | null; name: string }) => void
}) {
  const [searching, setSearching] = useState(false)

  // El backend acepta a CUALQUIER persona como maestro, no solo a quien tenga
  // asignación previa: cubrir sin nombramiento formal es lo normal.
  const candidates: TeacherOption[] = society.teacherCandidates
  const alreadyListed = candidates.some((c) => c.personId === myPersonId)

  return (
    <>
      <Screen
        title={society.societyName}
        subtitle="¿Quién dio la clase hoy?"
        onBack={onBack}
      >
        <ul className="space-y-3">
          {!alreadyListed && (
            <TeacherOptionRow
              name={`${myName} (yo)`}
              note="Tú levantas el reporte"
              onClick={() => onPick({ personId: myPersonId, name: myName })}
            />
          )}
          {candidates.map((c) => (
            <TeacherOptionRow
              key={c.personId}
              name={c.personId === myPersonId ? `${fullName(c)} (yo)` : fullName(c)}
              note={c.hasFixedGroup ? 'Maestro de este grupo' : 'Maestro sustituto'}
              onClick={() => onPick({ personId: c.personId, name: fullName(c) })}
            />
          ))}
        </ul>

        <div className="mt-6">
          <Button variant="secondary" full icon="search" onClick={() => setSearching(true)}>
            Buscar otra persona
          </Button>
          <p className="mt-3 text-center text-ink-soft">
            Cualquier persona puede aparecer como quien dio la clase, aunque no esté
            registrada como maestro.
          </p>
        </div>
      </Screen>

      {searching && (
        <PersonPicker
          title="¿Quién dio la clase?"
          excludeIds={candidates.map((c) => c.personId)}
          onPick={(p: PersonOption) => {
            setSearching(false)
            onPick({ personId: p.id, name: fullName(p) })
          }}
          onCancel={() => setSearching(false)}
        />
      )}
    </>
  )
}

function TeacherOptionRow({
  name,
  note,
  onClick,
}: {
  name: string
  note: string
  onClick: () => void
}) {
  return (
    <li>
      <button
        type="button"
        onClick={onClick}
        className="w-full flex items-center gap-4 rounded-card bg-surface pane
                   px-5 py-4 min-h-touch-lg text-left shadow-card
                   transition hover:border-clay-line active:scale-[0.99]"
      >
        <span className="shrink-0 grid place-items-center size-12 rounded-2xl bg-bone-deep text-ink-soft">
          <Icon name="person" className="size-6" />
        </span>
        <span className="flex-1 min-w-0">
          <span className="block font-semibold text-lg leading-snug">{name}</span>
          <span className="block text-sm text-ink-soft">{note}</span>
        </span>
        <Icon name="next" className="shrink-0 size-5 text-ink-faint" />
      </button>
    </li>
  )
}
