import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { ApiError } from '../../api/client'
import { directory, services } from '../../api/endpoints'
import type { PersonOption, ServiceTypeOption } from '../../api/types'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { MoneyField } from '../../components/Field'
import { ErrorState, Loading, SuccessScreen } from '../../components/Feedback'
import { Icon } from '../../components/Icon'
import { PersonPicker } from '../../components/PersonPicker'
import { Screen } from '../../components/Screen'
import { useAsync } from '../../lib/useAsync'
import { capitalize, formatLongDate, formatMoney, fullName, todayIso } from '../../lib/format'
import { TitheStep } from './TitheStep'

type Step =
  | { name: 'type' }
  | { name: 'form'; type: ServiceTypeOption }
  | { name: 'tithes'; type: ServiceTypeOption; sessionId: string; summary: Summary }
  | { name: 'done'; summary: Summary; identified: number | null }

export interface Summary {
  typeName: string
  sessionDate: string
  totalOffering: number
  totalTithe: number | null
  preacherName: string | null
}

export function GeneralServiceFlow() {
  const navigate = useNavigate()
  const types = useAsync((signal) => directory.serviceTypes(signal), [])
  const [step, setStep] = useState<Step>({ name: 'type' })

  // Solo los que NO se agrupan por Sociedad: Escuela Dominical tiene su propio flujo.
  const options = useMemo(
    () => (types.data ?? []).filter((t) => !t.requiresSocietyGrouping),
    [types.data],
  )

  if (types.loading) {
    return (
      <Screen title="Reporte de culto" onBack={() => navigate('/')}>
        <Loading label="Cargando los tipos de culto…" />
      </Screen>
    )
  }

  if (types.error) {
    return (
      <Screen title="Reporte de culto" onBack={() => navigate('/')}>
        <ErrorState error={types.error} onRetry={types.reload} />
      </Screen>
    )
  }

  if (step.name === 'done') {
    const rows = [
      { label: 'Culto', value: step.summary.typeName },
      { label: 'Fecha', value: capitalize(formatLongDate(step.summary.sessionDate)) },
      { label: 'Ofrenda', value: formatMoney(step.summary.totalOffering) },
    ]
    if (step.summary.totalTithe !== null) {
      rows.push({ label: 'Diezmo total', value: formatMoney(step.summary.totalTithe) })
    }
    if (step.identified !== null && step.identified > 0) {
      rows.push({ label: 'Diezmo identificado', value: formatMoney(step.identified) })
    }
    if (step.summary.preacherName) {
      rows.push({ label: 'Predicó', value: step.summary.preacherName })
    }

    return (
      <SuccessScreen
        title="Reporte guardado"
        summary={rows}
        secondary={
          <Button variant="secondary" full icon="coins" onClick={() => setStep({ name: 'type' })}>
            Levantar otro culto
          </Button>
        }
      />
    )
  }

  if (step.name === 'tithes') {
    return (
      <TitheStep
        sessionId={step.sessionId}
        declaredTithe={step.summary.totalTithe}
        onFinish={(identified) =>
          setStep({ name: 'done', summary: step.summary, identified })
        }
      />
    )
  }

  if (step.name === 'form') {
    return (
      <ServiceForm
        type={step.type}
        onBack={() => setStep({ name: 'type' })}
        onSaved={(sessionId, summary) =>
          step.type.collectsTithe
            ? setStep({ name: 'tithes', type: step.type, sessionId, summary })
            : setStep({ name: 'done', summary, identified: null })
        }
      />
    )
  }

  return (
    <Screen
      title="Reporte de culto"
      subtitle="¿De qué culto vas a levantar el reporte?"
      onBack={() => navigate('/')}
      width="wide"
    >
      <ul className="grid gap-3 sm:grid-cols-2">
        {options.map((type) => (
          <li key={type.id}>
            <button
              type="button"
              onClick={() => setStep({ name: 'form', type })}
              className="w-full h-full flex items-center gap-4 rounded-card bg-surface pane
                         px-5 py-5 min-h-touch-lg text-left
                         shadow-card transition hover:shadow-raised hover:border-clay-line
                         active:scale-[0.99]"
            >
              <span className="shrink-0 grid place-items-center size-12 rounded-2xl bg-clay-soft text-clay">
                <Icon name="coins" className="size-6" />
              </span>
              <span className="flex-1 min-w-0">
                <span className="block font-display text-2xl font-semibold leading-tight">
                  {type.name}
                </span>
                <span className="block text-sm text-ink-soft">
                  {type.collectsTithe ? 'Ofrenda y diezmo' : 'Solo ofrenda'}
                </span>
              </span>
              <Icon name="next" className="shrink-0 size-6 text-ink-faint" />
            </button>
          </li>
        ))}
      </ul>
    </Screen>
  )
}

function ServiceForm({
  type,
  onBack,
  onSaved,
}: {
  type: ServiceTypeOption
  onBack: () => void
  onSaved: (sessionId: string, summary: Summary) => void
}) {
  const [sessionDate, setSessionDate] = useState(todayIso())
  const [offering, setOffering] = useState('')
  const [tithe, setTithe] = useState('')
  const [preacher, setPreacher] = useState<PersonOption | null>(null)
  const [picking, setPicking] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  const [busy, setBusy] = useState(false)

  const parse = (v: string) => {
    const n = Number.parseFloat(v)
    return Number.isFinite(n) ? n : 0
  }

  async function submit() {
    setBusy(true)
    setError(null)
    const totalOffering = parse(offering)
    const totalTithe = type.collectsTithe ? parse(tithe) : null
    try {
      const created = await services.submitGeneralReport({
        serviceTypeId: type.id,
        sessionDate,
        totalOffering,
        // El backend rechaza un diezmo donde no se recoge (regla 7.3), así que
        // aquí ni se manda: el propio tipo de culto decide.
        totalTithe,
        preacherPersonId: preacher?.id ?? null,
      })
      onSaved(created.id, {
        typeName: type.name,
        sessionDate,
        totalOffering,
        totalTithe,
        preacherName: preacher ? fullName(preacher) : null,
      })
    } catch (e) {
      setError(e as ApiError)
      setBusy(false)
    }
  }

  return (
    <>
      <Screen
        title={type.name}
        subtitle="Reporte del culto"
        onBack={onBack}
        width="wide"
        footer={
          <Button full loading={busy} icon={busy ? undefined : 'check'} onClick={submit}>
            {busy
              ? 'Guardando…'
              : type.collectsTithe
                ? 'Guardar y registrar diezmos'
                : 'Guardar reporte'}
          </Button>
        }
      >
        {error && (
          <Card className="mb-6 border-danger/30 bg-danger-soft p-5">
            <p className="flex items-start gap-3 text-danger font-semibold">
              <Icon name="alert" className="size-6 shrink-0 mt-0.5" />
              <span>{error.message}</span>
            </p>
          </Card>
        )}

        {/* El `min-w-0` de los hijos es indispensable: un elemento de grid no se
            encoge por debajo de su contenido salvo que se le diga, y las filas
            de abajo llevan texto con `truncate` —o sea, sin salto de línea—,
            cuyo ancho mínimo es el del texto completo. Sin esto, la tarjeta
            medía 456px dentro de una pantalla de 390 y todo se salía. */}
        <div className="grid gap-6 lg:grid-cols-2 lg:items-start">
          <Card className="min-w-0 divide-y divide-line">
            <div className="relative flex items-center gap-4 px-5 py-4 min-h-touch-lg">
              <span className="shrink-0 grid place-items-center size-11 rounded-2xl bg-bone-deep text-ink-soft">
                <Icon name="calendar" className="size-5" />
              </span>
              <span className="flex-1 min-w-0">
                <span className="block text-sm text-ink-soft">Fecha del culto</span>
                <span className="block font-semibold leading-snug">
                  {capitalize(formatLongDate(sessionDate))}
                </span>
              </span>
              <span aria-hidden="true" className="shrink-0 text-clay font-semibold">
                Cambiar
              </span>
              <input
                type="date"
                value={sessionDate}
                max={todayIso()}
                onChange={(e) => e.target.value && setSessionDate(e.target.value)}
                aria-label="Fecha del culto"
                className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
              />
            </div>

            <div className="flex items-center gap-4 px-5 py-4 min-h-touch-lg">
              <span className="shrink-0 grid place-items-center size-11 rounded-2xl bg-bone-deep text-ink-soft">
                <Icon name="person" className="size-5" />
              </span>
              <span className="flex-1 min-w-0">
                <span className="block text-sm text-ink-soft">Quién predicó</span>
                <span className={`block font-semibold leading-snug ${preacher ? '' : 'text-ink-faint'}`}>
                  {preacher ? fullName(preacher) : 'Sin registrar'}
                </span>
              </span>
              <button
                type="button"
                onClick={() => setPicking(true)}
                className="shrink-0 min-h-11 px-3 -mr-2 rounded-xl text-clay font-semibold
                           hover:bg-clay-soft transition"
              >
                {preacher ? 'Cambiar' : 'Elegir'}
              </button>
            </div>

            {preacher && (
              <div className="px-5 py-3">
                <button
                  type="button"
                  onClick={() => setPreacher(null)}
                  className="inline-flex items-center gap-2 min-h-11 px-3 -ml-3 rounded-xl
                             text-sm font-semibold text-ink-soft hover:text-danger
                             hover:bg-danger-soft transition"
                >
                  <Icon name="minus" className="size-4" />
                  <span>Quitar el predicador</span>
                </button>
              </div>
            )}
          </Card>

          <div className="min-w-0 space-y-5">
            <Card className="p-5 sm:p-6">
              <MoneyField
                label="Ofrenda total"
                value={offering}
                onChange={setOffering}
                hint="El total recogido en el culto."
              />
            </Card>

            {type.collectsTithe && (
              <Card tone="ochre" className="p-5 sm:p-6">
                <MoneyField
                  label="Diezmo total"
                  value={tithe}
                  onChange={setTithe}
                  hint="El total contado. Después podrás anotar quiénes lo identificaron."
                />
              </Card>
            )}
          </div>
        </div>
      </Screen>

      {picking && (
        <PersonPicker
          title="¿Quién predicó?"
          onPick={(p) => {
            setPreacher(p)
            setPicking(false)
          }}
          onCancel={() => setPicking(false)}
        />
      )}
    </>
  )
}
