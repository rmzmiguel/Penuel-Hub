import { useEffect, useState } from 'react'
import { ApiError } from '../../api/client'
import { familyGroups } from '../../api/familyGroups'
import type { AvailablePerson } from '../../api/types'
import { Button } from '../../components/Button'
import { Field } from '../../components/Field'
import { Icon } from '../../components/Icon'
import { Avatar } from '../../components/ui/Avatar'
import { SearchField } from '../../components/ui/SearchField'
import { Sheet } from '../../components/ui/Sheet'
import { useToast } from '../../components/ui/Toast'
import { useAsync } from '../../lib/useAsync'
import { fullName } from '../../lib/format'

/**
 * Sumar a alguien al grupo, por los dos caminos que existen: buscarla en el
 * directorio, o darla de alta ahí mismo si es nueva.
 *
 * Las personas que YA pertenecen a otro grupo aparecen igualmente, en gris y sin
 * poder tocarlas. Esconderlas sería peor: quien busca a "Rosa" y no la ve va a
 * suponer que no está registrada y la va a dar de alta otra vez, duplicándola.
 * Lo que nunca se dice es a QUÉ grupo pertenece (regla 7.5) — el backend tampoco
 * lo manda.
 */
export function AddMemberSheet({
  open,
  groupId,
  onClose,
  onDone,
}: {
  open: boolean
  groupId: string
  onClose: () => void
  onDone: () => void
}) {
  const [modo, setModo] = useState<'buscar' | 'nueva'>('buscar')

  useEffect(() => {
    if (open) setModo('buscar')
  }, [open])

  return (
    <Sheet open={open} onClose={onClose} eyebrow="Tu grupo" title="Agregar persona">
      {modo === 'buscar' ? (
        <Buscar groupId={groupId} onNueva={() => setModo('nueva')} onDone={onDone} />
      ) : (
        <Nueva groupId={groupId} onCancel={() => setModo('buscar')} onDone={onDone} />
      )}
    </Sheet>
  )
}

function Buscar({
  groupId,
  onNueva,
  onDone,
}: {
  groupId: string
  onNueva: () => void
  onDone: () => void
}) {
  const [texto, setTexto] = useState('')
  const [ocupado, setOcupado] = useState<string | null>(null)
  const toast = useToast()

  const personas = useAsync(
    (signal) => familyGroups.availablePersons(groupId, texto.trim(), signal),
    [groupId, texto],
  )

  async function agregar(p: AvailablePerson) {
    setOcupado(p.personId)
    try {
      await familyGroups.addMember(groupId, p.personId)
      toast.ok('Agregada al grupo', fullName(p))
      onDone()
    } catch (e) {
      toast.error('No se pudo agregar', e instanceof ApiError ? e.message : String(e))
      setOcupado(null)
    }
  }

  const lista = personas.data ?? []

  return (
    <div className="space-y-5">
      <SearchField
        value={texto}
        onChange={setTexto}
        placeholder="Buscar por nombre"
        label="Buscar persona"
      />

      <ul className="rounded-card bg-surface pane shadow-card overflow-hidden divide-y divide-line">
        {personas.loading && lista.length === 0 && (
          <li className="px-5 py-8 text-center text-ink-soft">Buscando…</li>
        )}

        {!personas.loading && lista.length === 0 && (
          <li className="px-5 py-8 text-center text-ink-soft leading-snug">
            Nadie con ese nombre en el directorio.
          </li>
        )}

        {lista.map((p) => (
          <li key={p.personId}>
            <button
              type="button"
              disabled={!p.isAvailable || ocupado !== null}
              onClick={() => agregar(p)}
              className="w-full flex items-center gap-4 px-5 py-4 min-h-touch text-left
                         transition hover:bg-surface-warm disabled:hover:bg-transparent
                         disabled:opacity-55 disabled:cursor-not-allowed active:scale-[0.99]"
            >
              <Avatar person={p} size="sm" />
              <span className="min-w-0 flex-1">
                <span className="block font-medium leading-snug">{fullName(p)}</span>
                {!p.isAvailable && (
                  /* Frase deliberadamente genérica: no dice a qué grupo pertenece,
                     ni el backend lo manda. */
                  <span className="block text-sm text-ink-faint leading-snug">
                    Ya pertenece a un grupo
                  </span>
                )}
              </span>
              {p.isAvailable && (
                <Icon name="plus" className="size-5 shrink-0 text-ink-faint" strokeWidth={2.2} />
              )}
            </button>
          </li>
        ))}
      </ul>

      <div>
        <p className="text-ink-soft leading-snug mb-3">
          ¿No aparece porque es la primera vez que viene?
        </p>
        <Button variant="secondary" full icon="person" onClick={onNueva}>
          Registrar persona nueva
        </Button>
      </div>
    </div>
  )
}

/**
 * Alta de alguien nuevo desde el grupo.
 *
 * Aquí NO hay ninguna casilla de "miembro oficial", y no es que esté escondida: el
 * comando del backend no tiene ese parámetro (regla 7.4). Que alguien sea miembro
 * de la iglesia lo decide el Pastor, después y por su cuenta.
 */
function Nueva({
  groupId,
  onCancel,
  onDone,
}: {
  groupId: string
  onCancel: () => void
  onDone: () => void
}) {
  const [nombre, setNombre] = useState('')
  const [apellido, setApellido] = useState('')
  const [telefono, setTelefono] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const toast = useToast()

  async function enviar(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await familyGroups.registerMember(
        groupId, nombre.trim(), apellido.trim(), telefono.trim() || null,
      )
      toast.ok('Registrada y agregada', `${nombre.trim()} ${apellido.trim()}`)
      onDone()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo registrar.')
      setBusy(false)
    }
  }

  return (
    <form onSubmit={enviar} className="space-y-5">
      <p className="text-ink-soft leading-relaxed">
        Queda registrada y agregada a tu grupo de una vez. Con el nombre basta; el teléfono
        es opcional.
      </p>

      <Field
        label="Nombre"
        value={nombre}
        onChange={(e) => setNombre(e.target.value)}
        required
        maxLength={100}
        placeholder="Elena"
      />
      <Field
        label="Apellidos"
        value={apellido}
        onChange={(e) => setApellido(e.target.value)}
        required
        maxLength={100}
        placeholder="Ruiz Gómez"
      />
      <Field
        label="Teléfono (opcional)"
        type="tel"
        icon="person"
        value={telefono}
        onChange={(e) => setTelefono(e.target.value)}
        maxLength={30}
        placeholder="834 000 0000"
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

      <div className="flex flex-col sm:flex-row gap-3 pt-1">
        <Button type="submit" full loading={busy} disabled={!nombre.trim() || !apellido.trim()}>
          Registrar y agregar
        </Button>
        <Button type="button" variant="secondary" full onClick={onCancel}>
          Cancelar
        </Button>
      </div>
    </form>
  )
}
