import { useState } from 'react'
import { ApiError } from '../../api/client'
import { familyGroups } from '../../api/familyGroups'
import type { MyFamilyGroup } from '../../api/types'
import { DIAS } from '../../api/types'
import { useAuth } from '../../auth/AuthProvider'
import { Button, IconButton } from '../../components/Button'
import { ErrorState, Loading } from '../../components/Feedback'
import { Icon } from '../../components/Icon'
import { Avatar } from '../../components/ui/Avatar'
import { Sheet } from '../../components/ui/Sheet'
import { useToast } from '../../components/ui/Toast'
import { useAsync } from '../../lib/useAsync'
import { formatLongDate, fullName } from '../../lib/format'
import { AddMemberSheet } from './AddMemberSheet'
import { FamilyGroupReportFlow } from './FamilyGroupReportFlow'

/**
 * La aplicación COMPLETA de un Anfitrión o Encargado.
 *
 * No es una versión reducida de la aplicación del Pastor: es toda la que existe
 * para esta persona. Aquí no hay dock, ni barra lateral, ni ficha de cuenta con
 * "Permisos del sistema" y "Tus cargos" vacíos — esas tres tarjetas diciendo "Sin
 * cargo asignado" serían justamente la pista de que hay algo más ahí fuera, que es
 * lo único que la Sección 2.1 del documento pide evitar.
 *
 * Lo que sí tiene: su casa, su gente y su reporte. Y una forma de salir.
 */
export function FamilyGroupApp({ group, onReload }: { group: MyFamilyGroup; onReload: () => void }) {
  const [vista, setVista] = useState<'inicio' | 'reporte'>('inicio')

  if (vista === 'reporte') {
    return (
      <FamilyGroupReportFlow
        group={group}
        onClose={() => setVista('inicio')}
        onDone={() => {
          setVista('inicio')
          onReload()
        }}
      />
    )
  }

  return <Inicio group={group} onReload={onReload} onReport={() => setVista('reporte')} />
}

function Inicio({
  group,
  onReload,
  onReport,
}: {
  group: MyFamilyGroup
  onReload: () => void
  onReport: () => void
}) {
  const [agregando, setAgregando] = useState(false)
  const [cuenta, setCuenta] = useState(false)
  const [quitando, setQuitando] = useState<string | null>(null)
  const toast = useToast()

  async function quitar(personId: string, nombre: string) {
    setQuitando(personId)
    try {
      await familyGroups.removeMember(group.familyGroupId, personId)
      toast.ok('Quitada del grupo', nombre)
      onReload()
    } catch (e) {
      toast.error('No se pudo quitar', e instanceof ApiError ? e.message : String(e))
    } finally {
      setQuitando(null)
    }
  }

  return (
    <div className="min-h-dvh">
      <header className="sticky top-0 z-30 frost">
        <div className="max-w-2xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between gap-3">
          <span className="flex items-center gap-2.5 min-w-0">
            <span className="shrink-0 grid place-items-center size-9 rounded-[0.85rem] bg-clay
                             text-on-accent font-display font-bold text-lg leading-none">
              <span aria-hidden="true" className="-mt-px">P</span>
            </span>
            <span className="font-display text-lg font-medium leading-none truncate">Penuel</span>
          </span>

          <button
            type="button"
            onClick={() => setCuenta(true)}
            aria-label="Tu cuenta"
            className="shrink-0 rounded-full transition active:scale-95"
          >
            <CuentaAvatar />
          </button>
        </div>
      </header>

      <main className="max-w-2xl mx-auto px-4 sm:px-6 pb-16 pt-2 space-y-4 stagger">
        {/* La casa. El grupo no tiene nombre: SE LLAMA por su dirección, que es lo
            que la gente dice de verdad ("nos toca en casa de Rosa"). */}
        <section className="rounded-panel bg-surface pane shadow-card p-6">
          <p className="eyebrow text-ink-soft">
            {group.isHost ? 'Tu casa' : `Diriges en casa de ${group.hostFirstName}`}
          </p>
          <h1 className="font-display text-2xl sm:text-3xl font-medium leading-tight mt-1">
            {group.address}
          </h1>
          <p className="text-ink-soft mt-2 leading-snug">
            Se reúnen los {DIAS[group.defaultMeetingDayOfWeek].toLowerCase()}.{' '}
            {group.lastMeetingDate
              ? `El último reporte fue el ${formatLongDate(group.lastMeetingDate)}.`
              : 'Todavía no has levantado ningún reporte.'}
          </p>

          <div className="mt-6">
            <Button full size="lg" icon="check" onClick={onReport} disabled={group.members.length === 0}>
              Levantar el reporte
            </Button>
            {group.members.length === 0 && (
              <p className="mt-3 text-sm text-ink-faint leading-snug">
                Primero agrega a las personas que asisten; el reporte es la lista de ellas.
              </p>
            )}
          </div>
        </section>

        <section className="rounded-card bg-surface pane shadow-card overflow-hidden">
          <div className="px-6 pt-6 pb-2 flex items-start justify-between gap-4">
            <div className="min-w-0">
              <p className="eyebrow text-ink-soft">Quiénes asisten</p>
              <h2 className="font-display text-xl font-medium leading-tight mt-0.5">
                {group.members.length === 0
                  ? 'Sin personas todavía'
                  : `${group.members.length} ${group.members.length === 1 ? 'persona' : 'personas'}`}
              </h2>
            </div>
          </div>

          <ul className="divide-y divide-line">
            {group.members.map((m) => (
              <li key={m.personId} className="flex items-center gap-4 px-6 py-3">
                <Avatar person={m} size="sm" />
                <span className="min-w-0 flex-1 font-medium leading-snug">{fullName(m)}</span>
                <IconButton
                  icon="close"
                  label={`Quitar a ${fullName(m)} del grupo`}
                  variant="secondary"
                  size="sm"
                  disabled={quitando !== null}
                  onClick={() => quitar(m.personId, fullName(m))}
                />
              </li>
            ))}
          </ul>

          <div className="p-6 pt-4">
            <Button variant="secondary" full icon="plus" onClick={() => setAgregando(true)}>
              Agregar persona
            </Button>
          </div>
        </section>
      </main>

      <AddMemberSheet
        open={agregando}
        groupId={group.familyGroupId}
        onClose={() => setAgregando(false)}
        onDone={() => {
          setAgregando(false)
          onReload()
        }}
      />

      <CuentaSheet open={cuenta} onClose={() => setCuenta(false)} />
    </div>
  )
}

function CuentaAvatar() {
  const { capabilities } = useAuth()
  return capabilities ? <Avatar person={capabilities} size="sm" /> : <span className="size-10 block" />
}

/**
 * La ficha de cuenta del Anfitrión.
 *
 * A propósito NO reutiliza la del armazón general: aquella muestra permisos, cargos
 * y liderazgos, que para esta persona estarían los tres vacíos. Tres tarjetas
 * diciendo "Sin cargo asignado" no informan de nada y sí insinúan que existe un
 * sistema de cargos en el que uno no entró.
 */
function CuentaSheet({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { capabilities, signOut } = useAuth()

  if (!capabilities) return null

  return (
    <Sheet open={open} onClose={onClose} eyebrow="Tu cuenta" title={fullName(capabilities)}>
      <div className="flex items-center gap-4 rounded-panel bg-surface pane p-5 shadow-card">
        <Avatar person={capabilities} size="xl" />
        <div className="min-w-0">
          <p className="font-display text-xl font-medium leading-tight">{fullName(capabilities)}</p>
          <p className="text-sm text-ink-soft break-all leading-snug mt-0.5">
            {capabilities.email ?? 'Sin correo registrado'}
          </p>
        </div>
      </div>

      <p className="mt-5 text-ink-soft leading-relaxed">
        Si algo no cuadra —una persona que ya no viene, un dato mal anotado— avísale al Pastor.
      </p>

      <div className="mt-5">
        <Button variant="danger" full icon="logout" size="lg" onClick={signOut}>
          Cerrar sesión
        </Button>
      </div>
    </Sheet>
  )
}

/**
 * Punto de entrada: resuelve los grupos de quien entró y decide qué mostrar.
 * Con exactamente uno se va directo a su pantalla, sin selector (Sección 8.4).
 */
export function FamilyGroupRoot() {
  const grupos = useAsync((signal) => familyGroups.mine(signal), [])
  const [elegido, setElegido] = useState<string | null>(null)

  if (grupos.loading && !grupos.data) return <Loading label="Abriendo tu grupo…" />
  if (grupos.error) return <ErrorState error={grupos.error} onRetry={grupos.reload} />

  const lista = grupos.data ?? []
  if (lista.length === 0) return null

  const grupo = lista.length === 1 ? lista[0] : lista.find((g) => g.familyGroupId === elegido)

  if (!grupo) {
    // Solo aparece si alguien lleva dos casas. No se impone un límite artificial en
    // el backend, así que la pantalla tiene que saber qué hacer si ocurre.
    return (
      <div className="min-h-dvh px-5 py-10 max-w-2xl mx-auto">
        <h1 className="font-display text-2xl font-medium">¿En qué casa estás?</h1>
        <ul className="mt-6 space-y-3">
          {lista.map((g) => (
            <li key={g.familyGroupId}>
              <button
                type="button"
                onClick={() => setElegido(g.familyGroupId)}
                className="w-full flex items-center gap-4 rounded-card bg-surface pane px-5 py-5
                           min-h-touch-lg text-left shadow-card transition hover:shadow-raised
                           active:scale-[0.99]"
              >
                <span className="shrink-0 grid place-items-center size-12 rounded-2xl bg-clay-soft text-clay">
                  <Icon name="home" className="size-6" strokeWidth={1.9} />
                </span>
                <span className="min-w-0 flex-1 font-medium text-lg leading-snug">{g.address}</span>
                <Icon name="next" className="size-5 shrink-0 text-ink-faint" />
              </button>
            </li>
          ))}
        </ul>
      </div>
    )
  }

  return <FamilyGroupApp group={grupo} onReload={grupos.reload} />
}
