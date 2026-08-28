import { useState } from 'react'
import type { ApiError } from '../../api/client'
import { services } from '../../api/endpoints'
import type { PersonOption } from '../../api/types'
import { Button } from '../../components/Button'
import { Card } from '../../components/Card'
import { MoneyField } from '../../components/Field'
import { Icon } from '../../components/Icon'
import { PersonPicker } from '../../components/PersonPicker'
import { Screen } from '../../components/Screen'
import { formatMoney, fullName, initials } from '../../lib/format'

interface Entry {
  person: PersonOption
  amount: number
}

/**
 * Diezmos identificados por persona.
 *
 * Punto CRÍTICO de esta pantalla: la suma de lo identificado casi nunca coincide
 * con el total contado, porque no todos anotan su nombre en el sobre. El backend
 * no lo valida (regla 7.5) y la interfaz TAMPOCO debe sugerir que deban cuadrar:
 * nada de avisos en rojo, nada de "faltan $X". Se muestra la diferencia como el
 * dato informativo que es, con su explicación al lado.
 */
export function TitheStep({
  sessionId,
  declaredTithe,
  onFinish,
}: {
  sessionId: string
  declaredTithe: number | null
  onFinish: (identifiedTotal: number) => void
}) {
  const [entries, setEntries] = useState<Entry[]>([])
  const [picking, setPicking] = useState(false)
  const [pending, setPending] = useState<PersonOption | null>(null)
  const [amount, setAmount] = useState('')
  const [error, setError] = useState<ApiError | null>(null)
  const [busy, setBusy] = useState(false)

  const identified = entries.reduce((sum, e) => sum + e.amount, 0)
  const difference = declaredTithe !== null ? declaredTithe - identified : null

  async function add() {
    if (!pending) return
    const value = Number.parseFloat(amount)
    if (!Number.isFinite(value) || value <= 0) return

    setBusy(true)
    setError(null)
    try {
      await services.recordTithe(sessionId, pending.id, value)
      setEntries((list) => [...list, { person: pending, amount: value }])
      setPending(null)
      setAmount('')
    } catch (e) {
      setError(e as ApiError)
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <Screen
        title="Diezmos identificados"
        subtitle="Solo de quienes anotaron su nombre"
        width="wide"
        onBack={() => onFinish(identified)}
        footer={
          <Button full icon="check" onClick={() => onFinish(identified)}>
            Terminar
          </Button>
        }
      >
        <Card tone="ochre" className="p-6">
          <div className="grid gap-5 sm:grid-cols-3 sm:items-end">
            <div>
              <p className="text-sm text-ink-soft">Diezmo total contado</p>
              <p className="font-display text-4xl font-semibold tabular text-ink">
                {declaredTithe !== null ? formatMoney(declaredTithe) : '—'}
              </p>
            </div>
            <div>
              <p className="text-sm text-ink-soft">Identificado hasta ahora</p>
              <p className="font-display text-4xl font-semibold tabular text-ochre">
                {formatMoney(identified)}
              </p>
            </div>
            {difference !== null && (
              <div>
                <p className="text-sm text-ink-soft">Sin identificar</p>
                <p className="font-display text-4xl font-semibold tabular text-ink-soft">
                  {formatMoney(difference)}
                </p>
              </div>
            )}
          </div>

          {/* Explicación tranquila, en gris, no una advertencia. */}
          <p className="mt-5 pt-5 border-t border-line text-ink-soft leading-relaxed">
            Es normal que no cuadren. No todos anotan sus datos en el sobre, y el total
            contado es el número que vale. No hace falta que ajustes nada.
          </p>
        </Card>

        {error && (
          <Card className="mt-5 border-danger/30 bg-danger-soft p-5">
            <p className="flex items-start gap-3 text-danger font-semibold">
              <Icon name="alert" className="size-6 shrink-0 mt-0.5" />
              <span>{error.message}</span>
            </p>
          </Card>
        )}

        {pending ? (
          <Card className="mt-6 p-5 sm:p-6">
            <div className="flex items-center gap-4 mb-5">
              <span className="shrink-0 grid place-items-center size-12 rounded-2xl bg-bone-deep
                               font-display font-semibold text-ink-soft">
                {initials(pending)}
              </span>
              <span className="flex-1 min-w-0">
                <span className="block text-sm text-ink-soft">Registrando el diezmo de</span>
                <span className="block font-semibold text-lg leading-snug">{fullName(pending)}</span>
              </span>
            </div>

            <MoneyField label="Cantidad" value={amount} onChange={setAmount} />

            <div className="mt-5 flex flex-col sm:flex-row gap-3">
              <Button
                full
                loading={busy}
                icon={busy ? undefined : 'plus'}
                onClick={add}
                disabled={!Number.parseFloat(amount)}
              >
                Agregar
              </Button>
              <Button
                variant="secondary"
                full
                onClick={() => {
                  setPending(null)
                  setAmount('')
                }}
              >
                Cancelar
              </Button>
            </div>
          </Card>
        ) : (
          <div className="mt-6">
            <Button variant="secondary" full icon="plus" onClick={() => setPicking(true)}>
              Registrar un diezmo
            </Button>
          </div>
        )}

        {entries.length > 0 && (
          <>
            <h2 className="mt-9 mb-3 font-display text-xl font-semibold">
              Registrados ({entries.length})
            </h2>
            <ul className="space-y-2">
              {entries.map((e, i) => (
                <li
                  key={`${e.person.id}-${i}`}
                  className="flex items-center gap-4 rounded-card bg-surface pane
                             px-5 py-4 shadow-card"
                >
                  <span className="shrink-0 grid place-items-center size-11 rounded-2xl
                                   bg-ochre-soft text-ochre">
                    <Icon name="check" className="size-5" strokeWidth={2.6} />
                  </span>
                  <span className="flex-1 min-w-0 font-semibold leading-snug">
                    {fullName(e.person)}
                  </span>
                  <span className="shrink-0 font-display text-xl font-semibold tabular">
                    {formatMoney(e.amount)}
                  </span>
                </li>
              ))}
            </ul>
          </>
        )}

        <p className="mt-8 text-center text-ink-soft">
          Si nadie anotó su nombre, puedes terminar sin registrar ninguno.
        </p>
      </Screen>

      {picking && (
        <PersonPicker
          title="¿De quién es el diezmo?"
          excludeIds={entries.map((e) => e.person.id)}
          onPick={(p) => {
            setPending(p)
            setPicking(false)
          }}
          onCancel={() => setPicking(false)}
        />
      )}
    </>
  )
}
