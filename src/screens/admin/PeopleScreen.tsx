import { useMemo, useState } from 'react'
import { directory as read, sundaySchool } from '../../api/endpoints'
import type { PersonOption, SocietyOption } from '../../api/types'
import { Button } from '../../components/Button'
import { Card, CardHead } from '../../components/Card'
import { ErrorState, Loading } from '../../components/Feedback'
import { Icon } from '../../components/Icon'
import { PersonLine } from '../../components/ui/Avatar'
import { Chip, Segmented } from '../../components/ui/Chip'
import { PageHeader } from '../../components/ui/PageHeader'
import { SearchField } from '../../components/ui/SearchField'
import { useToast } from '../../components/ui/Toast'
import { useAsync } from '../../lib/useAsync'
import { fullName } from '../../lib/format'
import { PersonSheet, RegisterPersonSheet } from './PersonSheet'
import { AddToGroupSheet } from './GroupSheets'
import { FamilyGroupsPanel } from './FamilyGroupsScreen'

type Tab = 'personas' | 'grupos' | 'casas'

export function PeopleScreen() {
  const [tab, setTab] = useState<Tab>('personas')

  return (
    <div className="mx-auto w-full max-w-[86rem]">
      <PageHeader
        eyebrow="Congregación"
        title="Personas y grupos"
        lead="Da de alta a quien falte, arma los grupos de Escuela Dominical y las casas de entre semana."
        action={
          <Segmented
            label="Sección"
            options={[
              { value: 'personas', label: 'Personas' },
              { value: 'grupos', label: 'Grupos' },
              /* "Casas" y no "Grupos Familiares": junto a "Grupos" a secas, el
                 nombre largo se lee como una variante del anterior en vez de como
                 otra cosa. Y es como los llama la gente. */
              { value: 'casas', label: 'Casas' },
            ]}
            value={tab}
            onChange={setTab}
          />
        }
      />
      {tab === 'personas' && <PeopleTab />}
      {tab === 'grupos' && <GroupsTab />}
      {tab === 'casas' && <FamilyGroupsPanel />}
    </div>
  )
}

/* ── Personas ───────────────────────────────────────────────────────────── */

function PeopleTab() {
  const toast = useToast()
  const people = useAsync((signal) => read.persons(undefined, signal), [])
  const [search, setSearch] = useState('')
  const [selected, setSelected] = useState<PersonOption | null>(null)
  const [registering, setRegistering] = useState(false)

  const visible = useMemo(() => {
    const term = search.trim().toLowerCase()
    return (people.data ?? []).filter((p) => !term || fullName(p).toLowerCase().includes(term))
  }, [people.data, search])

  return (
    <>
      <div className="flex flex-col sm:flex-row gap-3 mb-4">
        <div className="flex-1 min-w-0">
          <SearchField
            label="Buscar persona"
            placeholder="Buscar por nombre"
            value={search}
            onChange={setSearch}
          />
        </div>
        <Button icon="plus" onClick={() => setRegistering(true)}>
          Registrar persona
        </Button>
      </div>

      <Card className="overflow-hidden">
        {/* El padding lo pone la tarjeta en el resto de la aplicación, pero aquí
            las filas deben llegar de borde a borde para que los separadores
            crucen entera la tarjeta. Por eso el encabezado lleva el suyo. */}
        <div className="px-6 pt-6 pb-2">
          <CardHead
            eyebrow="Directorio"
            title={
              people.data
                ? `${people.data.length} ${people.data.length === 1 ? 'persona activa' : 'personas activas'}`
                : 'Directorio'
            }
          />
        </div>

        {people.loading ? (
          <Loading label="Cargando el directorio…" />
        ) : people.error ? (
          <ErrorState error={people.error} onRetry={people.reload} />
        ) : visible.length === 0 ? (
          <p className="px-6 pb-8 pt-2 text-center text-lg text-ink-soft">
            {search
              ? `Nadie coincide con “${search}”.`
              : 'Todavía no hay nadie registrado. Empieza dando de alta a alguien.'}
          </p>
        ) : (
          <ul className="divide-y divide-line">
            {visible.map((person) => (
              <li key={person.id}>
                <button
                  type="button"
                  onClick={() => setSelected(person)}
                  className="w-full px-6 py-4 min-h-touch text-left transition hover:bg-surface-warm"
                >
                  <PersonLine
                    person={person}
                    trailing={<Icon name="next" className="size-5 text-ink-faint" />}
                  />
                </button>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {/*
       * El panel NO se cierra al guardar. Repartir permisos son ocho o diez
       * toques seguidos sobre la misma persona; cerrarlo en cada uno obligaba a
       * reabrir y volver a bajar hasta donde se estaba.
       *
       * La lista de detrás se relee en silencio —`refresh`, no `reload`— porque
       * el directorio solo devuelve personas ACTIVAS: si desde aquí se quita a
       * alguien del directorio, tiene que desaparecer de la lista. Callado, para
       * que no parpadee el fondo mientras el panel está abierto.
       */}
      <PersonSheet
        person={selected}
        onClose={() => setSelected(null)}
        onChanged={() => void people.refresh()}
        toast={toast}
      />

      <RegisterPersonSheet
        open={registering}
        onClose={() => setRegistering(false)}
        onDone={(nombre) => {
          setRegistering(false)
          void people.refresh()
          toast.ok('Persona registrada', nombre)
        }}
      />
    </>
  )
}

/* ── Grupos ─────────────────────────────────────────────────────────────── */

/**
 * Los grupos de Escuela Dominical y quiénes pertenecen a cada uno.
 *
 * La lista de sociedades se lee del contexto de captura de Escuela Dominical,
 * que es el único endpoint del backend que las enumera. No es un rodeo elegante,
 * pero es honesto: la alternativa sería escribir los cuatro grupos a mano, y
 * entonces dejarían de reflejar la base.
 */
function GroupsTab() {
  const context = useAsync((signal) => sundaySchool.captureContext(signal), [])

  if (context.loading) return <Loading label="Cargando los grupos…" />
  if (context.error) return <ErrorState error={context.error} onRetry={context.reload} />

  const societies = context.data?.allSocieties ?? []

  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {societies.map((society) => (
        <GroupCard key={society.societyId} society={society} />
      ))}
    </div>
  )
}

function GroupCard({ society }: { society: SocietyOption }) {
  const toast = useToast()
  const members = useAsync(
    (signal) => read.societyMembers(society.societyId, signal),
    [society.societyId],
  )
  const [adding, setAdding] = useState(false)

  const list = members.data?.members ?? []

  return (
    <>
      <Card className="min-w-0 flex flex-col">
        <div className="px-6 pt-6 pb-2">
          <CardHead
            eyebrow="Grupo"
            title={society.societyName}
            action={
              <Chip tone={list.length ? 'forest' : 'neutral'}>
                {list.length} {list.length === 1 ? 'integrante' : 'integrantes'}
              </Chip>
            }
          />
        </div>

        {members.loading ? (
          <Loading label="Cargando…" />
        ) : members.error ? (
          <ErrorState error={members.error} onRetry={members.reload} />
        ) : list.length === 0 ? (
          <p className="px-6 pb-6 text-ink-soft">
            Todavía no hay nadie en este grupo. Al agregarlos, aparecerán ya marcados
            en el reporte dominical.
          </p>
        ) : (
          <ul className="px-6 pb-2 space-y-3">
            {list.map((person) => (
              <li key={person.id}>
                <PersonLine person={person} size="sm" />
              </li>
            ))}
          </ul>
        )}

        <div className="mt-auto p-6 pt-4">
          <Button variant="secondary" full icon="plus" onClick={() => setAdding(true)}>
            Agregar al grupo
          </Button>
        </div>
      </Card>

      <AddToGroupSheet
        open={adding}
        society={society}
        alreadyIn={list.map((p) => p.id)}
        onClose={() => setAdding(false)}
        onAdded={(person) => {
          setAdding(false)
          void members.refresh()
          toast.ok('Agregado al grupo', `${fullName(person)} · ${society.societyName}`)
        }}
      />
    </>
  )
}
