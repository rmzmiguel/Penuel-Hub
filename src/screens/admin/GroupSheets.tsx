import { useEffect, useMemo, useState } from 'react'
import { ApiError } from '../../api/client'
import { admin } from '../../api/admin'
import { directory as read } from '../../api/endpoints'
import type { PersonOption, SocietyOption } from '../../api/types'
import { ErrorState, Loading } from '../../components/Feedback'
import { Icon } from '../../components/Icon'
import { PersonLine } from '../../components/ui/Avatar'
import { SearchField } from '../../components/ui/SearchField'
import { Sheet } from '../../components/ui/Sheet'
import { useAsync } from '../../lib/useAsync'
import { fullName } from '../../lib/format'

/**
 * Agregar a alguien a un grupo de Escuela Dominical.
 *
 * Es lo que hace usable el reporte dominical: sin integrantes, el maestro abre
 * su grupo y encuentra una lista vacía.
 */
export function AddToGroupSheet({
  open,
  society,
  alreadyIn,
  onClose,
  onAdded,
}: {
  open: boolean
  society: SocietyOption
  alreadyIn: string[]
  onClose: () => void
  onAdded: (person: PersonOption) => void
}) {
  const people = useAsync(
    (signal) => (open ? read.persons(undefined, signal) : Promise.resolve([])),
    [open],
  )
  const [search, setSearch] = useState('')
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (open) {
      setSearch('')
      setError(null)
      setBusy(null)
    }
  }, [open])

  const visible = useMemo(() => {
    const dentro = new Set(alreadyIn)
    const term = search.trim().toLowerCase()
    return (people.data ?? [])
      .filter((p) => !dentro.has(p.id))
      .filter((p) => !term || fullName(p).toLowerCase().includes(term))
  }, [people.data, alreadyIn, search])

  async function agregar(person: PersonOption) {
    setBusy(person.id)
    setError(null)
    try {
      await admin.addSocietyMember(society.societyId, person.id)
      onAdded(person)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo agregar.')
      setBusy(null)
    }
  }

  return (
    <Sheet open={open} onClose={onClose} eyebrow={society.societyName} title="Agregar al grupo">
      <div className="space-y-4">
        <SearchField
          label="Buscar persona"
          placeholder="Buscar por nombre"
          value={search}
          onChange={setSearch}
        />

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

        {people.loading ? (
          <Loading label="Cargando el directorio…" />
        ) : people.error ? (
          <ErrorState error={people.error} onRetry={people.reload} />
        ) : visible.length === 0 ? (
          <p className="py-10 text-center text-lg text-ink-soft">
            {search
              ? `Nadie coincide con “${search}”.`
              : 'Todas las personas registradas ya están en este grupo.'}
          </p>
        ) : (
          <ul className="space-y-2">
            {visible.map((person) => (
              <li key={person.id}>
                <button
                  type="button"
                  disabled={busy !== null}
                  onClick={() => agregar(person)}
                  className="w-full rounded-card pane bg-surface px-5 py-3
                             min-h-touch text-left transition hover:border-line-strong
                             disabled:opacity-50 active:scale-[0.99]"
                >
                  <PersonLine
                    person={person}
                    trailing={
                      busy === person.id ? (
                        <span className="text-sm font-semibold text-ink-soft">Agregando…</span>
                      ) : (
                        <Icon name="plus" className="size-5 text-ink-faint" />
                      )
                    }
                  />
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </Sheet>
  )
}
