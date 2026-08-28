import { useState } from 'react'
import { ApiError } from '../../api/client'
import { familyGroups } from '../../api/familyGroups'
import type { FamilyGroupSummary, PersonOption } from '../../api/types'
import { DIAS } from '../../api/types'
import { Button } from '../../components/Button'
import { EmptyState, ErrorState } from '../../components/Feedback'
import { Field } from '../../components/Field'
import { Icon } from '../../components/Icon'
import { PersonPicker } from '../../components/PersonPicker'
import { Avatar } from '../../components/ui/Avatar'
import { PageHeader } from '../../components/ui/PageHeader'
import { Sheet } from '../../components/ui/Sheet'
import { useToast } from '../../components/ui/Toast'
import { useAsync } from '../../lib/useAsync'
import { formatLongDate, formatMoney, formatShortDate, fullName } from '../../lib/format'
import { PersonSheet } from './PersonSheet'

/**
 * Los Grupos Familiares vistos por el Pastor: la lista completa de casas.
 *
 * Es la contraparte de la pantalla del Anfitrión, y a propósito enseña lo que
 * aquélla nunca enseña — todas las casas, sus direcciones, quién las lleva y desde
 * cuándo no reportan.
 */
export function FamilyGroupsScreen() {
  return (
    <div className="mx-auto w-full max-w-[70rem]">
      <PageHeader
        eyebrow="Entre semana"
        title="Grupos Familiares"
        lead="Las casas donde se reúne la iglesia. Cada una tiene su Anfitrión y su Encargado."
      />
      <FamilyGroupsPanel />
    </div>
  )
}

/**
 * El cuerpo de la pantalla, sin encabezado.
 *
 * Vive aparte porque se muestra en DOS sitios: en su propia ruta —a la que se
 * llega desde el tablero— y como pestaña dentro de Personas, que es donde el
 * Pastor ya está cuando piensa en la congregación. Duplicar la lista habría sido
 * la forma más rápida de que una de las dos se quedara vieja.
 */
export function FamilyGroupsPanel() {
  const grupos = useAsync((signal) => familyGroups.all(signal), [])
  const [creando, setCreando] = useState(false)
  const [abierto, setAbierto] = useState<string | null>(null)
  const [persona, setPersona] = useState<PersonOption | null>(null)
  const toast = useToast()

  const lista = grupos.data ?? []

  return (
    <>
      <div className="mb-4">
        <Button icon="plus" onClick={() => setCreando(true)}>
          Dar de alta una casa
        </Button>
      </div>

      {grupos.error ? (
        <ErrorState error={grupos.error} onRetry={grupos.reload} />
      ) : lista.length === 0 && !grupos.loading ? (
        <EmptyState
          title="Todavía no hay ninguna casa"
          description="Da de alta la primera y su Anfitrión podrá levantar el reporte de cada semana desde su teléfono."
        />
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2 stagger">
          {lista.map((g) => (
            <li key={g.familyGroupId} className="min-w-0">
              <GrupoCard grupo={g} onOpen={() => setAbierto(g.familyGroupId)} />
            </li>
          ))}
        </ul>
      )}

      <CrearGrupoSheet
        open={creando}
        onClose={() => setCreando(false)}
        onDone={() => {
          setCreando(false)
          grupos.reload()
        }}
      />

      <DetalleSheet
        groupId={abierto}
        onClose={() => setAbierto(null)}
        onChanged={grupos.reload}
        onPerson={setPersona}
      />

      {/* La ficha de la persona se abre ENCIMA del detalle de la casa: quien está
          mirando un grupo y toca a alguien quiere volver al grupo, no salir de él. */}
      <PersonSheet
        person={persona}
        onClose={() => setPersona(null)}
        onChanged={grupos.reload}
        toast={toast}
      />
    </>
  )
}

function GrupoCard({ grupo, onOpen }: { grupo: FamilyGroupSummary; onOpen: () => void }) {
  const mismaPersona =
    grupo.hostFirstName === grupo.leaderFirstName && grupo.hostLastName === grupo.leaderLastName

  return (
    <button
      type="button"
      onClick={onOpen}
      className="w-full h-full text-left rounded-card bg-surface pane shadow-card p-6
                 transition hover:shadow-raised active:scale-[0.99]"
    >
      <div className="flex items-start gap-4">
        <span
          className={`shrink-0 grid place-items-center size-12 rounded-2xl
                      ${grupo.isActive ? 'bg-clay-soft text-clay' : 'bg-bone-deep text-ink-faint'}`}
        >
          <Icon name="home" className="size-6" strokeWidth={1.9} />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block font-display text-lg font-medium leading-snug">{grupo.address}</span>
          <span className="block text-sm text-ink-soft leading-snug mt-0.5">
            {mismaPersona
              ? `${grupo.hostFirstName} ${grupo.hostLastName}`
              : `${grupo.hostFirstName} ${grupo.hostLastName} · dirige ${grupo.leaderFirstName}`}
          </span>
        </span>
        {!grupo.isActive && (
          <span className="shrink-0 rounded-full bg-bone-deep px-2.5 py-1 text-xs font-medium text-ink-soft">
            Detenido
          </span>
        )}
      </div>

      <dl className="mt-5 flex items-baseline gap-6">
        <div className="min-w-0">
          <dt className="text-sm text-ink-faint">Asisten</dt>
          <dd className="font-numeral text-2xl font-medium leading-none mt-1">
            {grupo.activeMemberCount}
          </dd>
        </div>
        <div className="min-w-0">
          <dt className="text-sm text-ink-faint">Último reporte</dt>
          {/* Fecha corta: la larga —"martes, 25 de agosto de 2026"— no cabe en la
              tarjeta y se cortaba a media palabra. */}
          <dd className="text-base leading-none mt-1.5 truncate">
            {grupo.lastMeetingDate ? formatShortDate(grupo.lastMeetingDate) : 'Ninguno todavía'}
          </dd>
        </div>
      </dl>

      <p className="mt-4 text-sm text-ink-faint">
        Se reúnen los {DIAS[grupo.defaultMeetingDayOfWeek].toLowerCase()}
      </p>
    </button>
  )
}

/* ── Alta de una casa ───────────────────────────────────────────────────── */

function CrearGrupoSheet({
  open,
  onClose,
  onDone,
}: {
  open: boolean
  onClose: () => void
  onDone: () => void
}) {
  const [anfitrion, setAnfitrion] = useState<PersonOption | null>(null)
  const [encargado, setEncargado] = useState<PersonOption | null>(null)
  const [direccion, setDireccion] = useState('')
  const [dia, setDia] = useState(4) // jueves, el ritmo real de la iglesia
  const [eligiendo, setEligiendo] = useState<'anfitrion' | 'encargado' | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function enviar(e: React.FormEvent) {
    e.preventDefault()
    if (!anfitrion) return
    setBusy(true)
    setError(null)
    try {
      await familyGroups.create(
        anfitrion.id,
        // Nulo significa "el Anfitrión también dirige" (regla 7.1), no "pendiente".
        encargado && encargado.id !== anfitrion.id ? encargado.id : null,
        direccion.trim(),
        dia,
      )
      setAnfitrion(null)
      setEncargado(null)
      setDireccion('')
      onDone()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo dar de alta la casa.')
      setBusy(false)
    }
  }

  if (eligiendo) {
    return (
      <PersonPicker
        title={eligiendo === 'anfitrion' ? '¿Quién pone la casa?' : '¿Quién dirige la reunión?'}
        description={
          eligiendo === 'anfitrion'
            ? 'El Anfitrión abre su casa. Podrá levantar el reporte de cada semana.'
            : 'Solo si dirige alguien distinto del Anfitrión. Los dos tienen los mismos permisos.'
        }
        onPick={(p) => {
          if (eligiendo === 'anfitrion') setAnfitrion(p)
          else setEncargado(p)
          setEligiendo(null)
        }}
        onCancel={() => setEligiendo(null)}
      />
    )
  }

  return (
    <Sheet open={open} onClose={onClose} eyebrow="Grupos Familiares" title="Dar de alta una casa">
      <form onSubmit={enviar} className="space-y-5">
        <p className="text-ink-soft leading-relaxed">
          Dar de alta una casa la convierte en punto de reunión oficial. Su Anfitrión podrá
          entrar y levantar el reporte, sin necesitar ningún otro permiso.
        </p>

        <ElegirPersona
          label="Anfitrión"
          hint="Quien pone la casa."
          person={anfitrion}
          onPick={() => setEligiendo('anfitrion')}
        />

        <ElegirPersona
          label="Encargado (opcional)"
          hint="Solo si dirige alguien distinto. Si lo dejas vacío, dirige el Anfitrión."
          person={encargado}
          onPick={() => setEligiendo('encargado')}
          onClear={() => setEncargado(null)}
        />

        <Field
          label="Dirección"
          icon="home"
          value={direccion}
          onChange={(e) => setDireccion(e.target.value)}
          required
          maxLength={300}
          placeholder="Calle Hidalgo 120, Col. Centro"
          hint="Es como se llama el grupo: la gente dice “nos toca en casa de…”."
        />

        <div>
          <p className="block font-medium text-lg mb-2">Día habitual</p>
          <div className="grid grid-cols-4 sm:grid-cols-7 gap-2">
            {DIAS.map((nombre, i) => (
              <button
                key={nombre}
                type="button"
                aria-pressed={dia === i}
                onClick={() => setDia(i)}
                className={`min-h-touch px-1 rounded-[1.1rem] text-sm font-medium
                            transition-[background-color,color] duration-250 active:scale-95
                            ${dia === i ? 'bg-ink text-on-ink' : 'bg-bone text-ink-soft hover:bg-bone-deep'}`}
              >
                {nombre.slice(0, 3)}
              </button>
            ))}
          </div>
          <p className="mt-2 text-sm text-ink-faint leading-snug">
            Solo informativo: el reporte se puede levantar cualquier día si esa semana hizo falta.
          </p>
        </div>

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

        <div className="flex flex-col sm:flex-row gap-3 pt-1">
          <Button type="submit" full loading={busy} disabled={!anfitrion || !direccion.trim()}>
            Dar de alta
          </Button>
          <Button type="button" variant="secondary" full onClick={onClose}>
            Cancelar
          </Button>
        </div>
      </form>
    </Sheet>
  )
}

function ElegirPersona({
  label,
  hint,
  person,
  onPick,
  onClear,
}: {
  label: string
  hint: string
  person: PersonOption | null
  onPick: () => void
  onClear?: () => void
}) {
  return (
    <div>
      <p className="block font-medium text-lg mb-2">{label}</p>
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={onPick}
          className="flex-1 min-w-0 flex items-center gap-3 rounded-field bg-surface pane
                     px-5 min-h-touch-lg text-left transition hover:shadow-raised active:scale-[0.99]"
        >
          {person ? (
            <>
              <Avatar person={person} size="sm" />
              <span className="min-w-0 flex-1 font-medium leading-snug">{fullName(person)}</span>
            </>
          ) : (
            <>
              <Icon name="person" className="size-5 shrink-0 text-ink-faint" />
              <span className="min-w-0 flex-1 text-ink-faint">Elegir persona</span>
            </>
          )}
          <Icon name="next" className="size-5 shrink-0 text-ink-faint" />
        </button>
        {person && onClear && (
          <Button variant="secondary" size="md" onClick={onClear}>
            Quitar
          </Button>
        )}
      </div>
      <p className="mt-2 text-sm text-ink-faint leading-snug">{hint}</p>
    </div>
  )
}

/* ── Detalle de una casa ────────────────────────────────────────────────── */

function DetalleSheet({
  groupId,
  onClose,
  onChanged,
  onPerson,
}: {
  groupId: string | null
  onClose: () => void
  onChanged: () => void
  onPerson: (person: PersonOption) => void
}) {
  const detalle = useAsync(
    (signal) => (groupId ? familyGroups.detail(groupId, signal) : Promise.resolve(null)),
    [groupId],
  )
  const [busy, setBusy] = useState(false)
  const toast = useToast()

  if (!groupId) return <Sheet open={false} onClose={onClose} title=""><span /></Sheet>

  const d = detalle.data

  async function cambiarEstado(activo: boolean) {
    if (!groupId) return
    setBusy(true)
    try {
      await familyGroups.setStatus(groupId, activo)
      toast.ok(activo ? 'Grupo reanudado' : 'Grupo detenido')
      detalle.reload()
      onChanged()
    } catch (e) {
      toast.error('No se pudo completar', e instanceof ApiError ? e.message : String(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Sheet open onClose={onClose} eyebrow="Casa" title={d?.address ?? 'Cargando…'}>
      {detalle.error ? (
        <ErrorState error={detalle.error} onRetry={detalle.reload} />
      ) : !d ? (
        <div className="space-y-3">
          {[0, 1, 2].map((i) => (
            <div key={i} className="h-24 rounded-card bg-bone-deep/60 animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="space-y-4">
          <section className="rounded-card bg-surface pane shadow-card p-5">
            <p className="eyebrow text-ink-soft">Quién la lleva</p>
            <ul className="mt-3 space-y-3">
              <li className="flex items-center gap-3">
                <Avatar person={{ firstName: d.hostFirstName, lastName: d.hostLastName }} size="sm" />
                <span className="min-w-0">
                  <span className="block font-medium leading-snug">
                    {d.hostFirstName} {d.hostLastName}
                  </span>
                  <span className="block text-sm text-ink-faint">Anfitrión · pone la casa</span>
                </span>
              </li>
              {d.leaderPersonId !== d.hostPersonId && (
                <li className="flex items-center gap-3">
                  <Avatar
                    person={{ firstName: d.leaderFirstName, lastName: d.leaderLastName }}
                    size="sm"
                  />
                  <span className="min-w-0">
                    <span className="block font-medium leading-snug">
                      {d.leaderFirstName} {d.leaderLastName}
                    </span>
                    <span className="block text-sm text-ink-faint">Encargado · dirige la reunión</span>
                  </span>
                </li>
              )}
            </ul>
            <p className="mt-4 text-sm text-ink-faint leading-snug">
              Los dos pueden hacer exactamente lo mismo en el grupo. Se reúnen los{' '}
              {DIAS[d.defaultMeetingDayOfWeek].toLowerCase()}.
            </p>
          </section>

          <section className="rounded-card bg-surface pane shadow-card overflow-hidden">
            <div className="px-5 pt-5 pb-2">
              <p className="eyebrow text-ink-soft">Quiénes asisten</p>
              <h3 className="font-display text-lg font-medium mt-0.5">
                {d.members.length} {d.members.length === 1 ? 'persona' : 'personas'}
              </h3>
            </div>
            <ul className="divide-y divide-line">
              {d.members.map((m) => (
                <li key={m.personId}>
                  {/*
                   * El nombre y la fecha iban en la MISMA línea, uno al lado del
                   * otro. Con "Esperanza Solís de Ramírez" el nombre se partía en
                   * cuatro renglones y la fecha lo cruzaba por encima. Ahora el
                   * nombre manda en su propia línea, cortado con puntos suspensivos
                   * si hace falta, y la fecha baja como dato secundario — que es lo
                   * que es.
                   */}
                  <button
                    type="button"
                    onClick={() => onPerson({ id: m.personId, firstName: m.firstName, lastName: m.lastName })}
                    className="w-full flex items-center gap-3 px-5 py-3 min-h-touch text-left
                               transition hover:bg-surface-warm active:scale-[0.99]"
                  >
                    <Avatar person={m} size="sm" />
                    <span className="min-w-0 flex-1">
                      <span className="block font-medium leading-snug truncate">{fullName(m)}</span>
                      <span className="block text-sm text-ink-faint leading-snug truncate">
                        desde {formatLongDate(m.joinedAt)}
                      </span>
                    </span>
                    <Icon name="next" className="size-5 shrink-0 text-ink-faint" />
                  </button>
                </li>
              ))}
              {d.members.length === 0 && (
                <li className="px-5 py-6 text-ink-soft leading-snug">
                  Todavía nadie. Su Anfitrión los agrega desde su propia pantalla.
                </li>
              )}
            </ul>
          </section>

          <section className="rounded-card bg-surface pane shadow-card overflow-hidden">
            <div className="px-5 pt-5 pb-2">
              <p className="eyebrow text-ink-soft">Últimos reportes</p>
            </div>
            <ul className="divide-y divide-line">
              {d.recentMeetings.map((m) => (
                <li key={m.meetingId} className="flex items-baseline gap-4 px-5 py-3">
                  <span className="min-w-0 flex-1 leading-snug">{formatLongDate(m.meetingDate)}</span>
                  <span className="shrink-0 text-sm text-ink-soft tabular">
                    {m.presentCount}/{m.memberCount}
                  </span>
                  <span className="shrink-0 font-numeral tabular">{formatMoney(m.totalOffering)}</span>
                </li>
              ))}
              {d.recentMeetings.length === 0 && (
                <li className="px-5 py-6 text-ink-soft leading-snug">Todavía no hay reportes.</li>
              )}
            </ul>
          </section>

          <section className="rounded-card bg-surface pane shadow-card p-5">
            <p className="eyebrow text-ink-soft">Estado</p>
            <p className="text-ink-soft leading-snug mt-2">
              {d.isActive
                ? 'La casa está activa. Al detenerla, su Anfitrión deja de poder levantar reportes; nada se borra.'
                : 'La casa está detenida. Sus reportes y su gente se conservan.'}
            </p>
            <div className="mt-4">
              <Button
                variant={d.isActive ? 'danger' : 'secondary'}
                full
                icon={d.isActive ? 'ban' : 'check'}
                loading={busy}
                onClick={() => cambiarEstado(!d.isActive)}
              >
                {d.isActive ? 'Detener el grupo' : 'Reanudar el grupo'}
              </Button>
            </div>
          </section>
        </div>
      )}
    </Sheet>
  )
}
