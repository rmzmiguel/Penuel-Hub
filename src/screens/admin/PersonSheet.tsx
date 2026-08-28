import { useEffect, useState } from 'react'
import { ApiError } from '../../api/client'
import { admin } from '../../api/admin'
import type { AdminAttendance, PersonAdministration, PersonOption } from '../../api/types'
import { useAuth } from '../../auth/AuthProvider'
import { Button } from '../../components/Button'
import { ErrorState } from '../../components/Feedback'
import { Field } from '../../components/Field'
import { Icon } from '../../components/Icon'
import type { IconName } from '../../components/Icon'
import { Avatar } from '../../components/ui/Avatar'
import { Chip } from '../../components/ui/Chip'
import { Sheet } from '../../components/ui/Sheet'
import { PermissionRow } from '../../components/ui/Toggle'
import { useAsync } from '../../lib/useAsync'
import { formatCompactDate, formatLongDate, fullName } from '../../lib/format'

type Toast = { ok: (t: string, d?: string) => void; error: (t: string, d?: string) => void }

/**
 * Panel de administración de una persona.
 *
 * Sustituye a la versión anterior, que solo sabía DAR: crear cuenta, registrar
 * miembro, quitar del directorio. No podía mostrar roles ni cargos porque no
 * existía ninguna lectura que devolviera los de otra persona, y no podía
 * deshacer nada porque tampoco existían las operaciones inversas.
 *
 * Ahora `GET /api/persons/{id}/administration` devuelve el estado completo y
 * los catálogos, así que cada cosa es un interruptor que SABE su estado y se
 * puede mover en los dos sentidos. Un panel que solo enciende obliga a entrar a
 * la base de datos para arreglar cualquier equivocación.
 *
 * Ningún nombre de rol ni de cargo está escrito aquí: los dos catálogos llegan
 * del servidor. Si mañana se siembra otro rol, aparece en esta pantalla sin
 * tocar una línea.
 */
export function PersonSheet({
  person,
  onClose,
  onChanged,
  toast,
}: {
  person: PersonOption | null
  onClose: () => void
  onChanged: () => void
  toast: Toast
}) {
  const [modo, setModo] = useState<'panel' | 'cuenta' | 'editar'>('panel')

  /*
   * El título sale de la LISTA que abrió el panel, que es un dato viejo en cuanto
   * alguien corrige el nombre: la lista de atrás no se ha recargado todavía. Se
   * guarda aparte y el contenido lo actualiza en cuanto tiene el dato fresco, si
   * no la cabecera se quedaba con el apellido que se acababa de arreglar.
   */
  const [titulo, setTitulo] = useState('')

  useEffect(() => {
    if (person) {
      setModo('panel')
      setTitulo(fullName(person))
    }
  }, [person])

  if (!person) return <Sheet open={false} onClose={onClose} title=""><span /></Sheet>

  return (
    <Sheet open onClose={onClose} eyebrow="Administrar" title={titulo || fullName(person)}>
      <Contenido
        personId={person.id}
        modo={modo}
        setModo={setModo}
        onChanged={onChanged}
        onNombre={setTitulo}
        toast={toast}
      />
    </Sheet>
  )
}

function Contenido({
  personId,
  modo,
  setModo,
  onChanged,
  onNombre,
  toast,
}: {
  personId: string
  modo: 'panel' | 'cuenta' | 'editar'
  setModo: (m: 'panel' | 'cuenta' | 'editar') => void
  onChanged: () => void
  /** Avisa del nombre real hacia la cabecera del panel. */
  onNombre: (nombre: string) => void
  toast: Toast
}) {
  const estado = useAsync((signal) => admin.administration(personId, signal), [personId])
  const { capabilities } = useAuth()

  /*
   * Una sola clave de "ocupado" para todo el panel. Con interruptores que
   * dependen unos de otros —sin cuenta no hay roles— permitir dos cambios a la
   * vez deja la pantalla diciendo algo que el servidor ya no cree.
   */
  const [ocupado, setOcupado] = useState<string | null>(null)

  /*
   * Guarda un cambio y deja la pantalla contando la verdad del servidor.
   *
   * El interruptor sigue en "ocupado" hasta que llega el estado FRESCO, no
   * hasta que responde la escritura: si se soltara antes, la fila volvería un
   * instante a su valor anterior y luego saltaría al nuevo. Y la relectura es
   * `refresh` y no `reload` justamente para que el panel no se vacíe ni
   * parpadee — quien está repartiendo permisos lleva media pantalla scrolleada
   * y no puede perder el sitio en cada toque.
   *
   * Se relee del servidor en vez de dar por bueno el cambio en local porque
   * estas cosas se encadenan: apagar una cuenta apaga sus roles, y el cuerpo
   * ejecutivo se calcula a partir de los cargos. Duplicar esas reglas aquí
   * sería tener dos verdades.
   */
  async function aplicar(clave: string, accion: () => Promise<unknown>, exito: string) {
    setOcupado(clave)
    try {
      await accion()
      await estado.refresh()
      toast.ok(exito)
      onChanged()
    } catch (e) {
      toast.error('No se pudo completar', e instanceof ApiError ? e.message : String(e))
      // La escritura falló: puede haber cambiado algo a medias. Se vuelve a
      // preguntar antes de dejar que nadie siga tocando interruptores.
      await estado.refresh()
    } finally {
      setOcupado(null)
    }
  }

  useEffect(() => {
    if (estado.data) onNombre(`${estado.data.firstName} ${estado.data.lastName}`)
  }, [estado.data, onNombre])

  if (estado.loading && !estado.data) {
    return (
      <div className="space-y-3">
        {[0, 1, 2].map((i) => (
          <div key={i} className="h-28 rounded-card bg-bone-deep/60 animate-pulse" />
        ))}
      </div>
    )
  }

  if (estado.error) return <ErrorState error={estado.error} onRetry={estado.reload} />
  if (!estado.data) return null

  const a = estado.data

  if (modo === 'editar') {
    return (
      <EditPersonForm
        persona={a}
        onCancel={() => setModo('panel')}
        onDone={(nombre) => {
          setModo('panel')
          toast.ok('Ficha actualizada', nombre)
          estado.reload()
          onChanged()
        }}
      />
    )
  }

  if (modo === 'cuenta') {
    return (
      <AccountForm
        personId={personId}
        firstName={a.firstName}
        onCancel={() => setModo('panel')}
        onDone={async () => {
          setModo('panel')
          await estado.refresh()
          toast.ok('Cuenta creada', `${a.firstName} ya puede entrar`)
          onChanged()
        }}
      />
    )
  }

  const esYoMismo = capabilities?.personId === personId

  return (
    /*
     * Mientras hay un cambio en vuelo no se admite otro. Los interruptores de
     * este panel dependen unos de otros —sin cuenta activa no hay roles— y dos
     * escrituras solapadas dejarían la pantalla afirmando algo que el servidor
     * ya no cree. La cabecera del panel queda fuera, así que cerrar siempre se
     * puede.
     */
    <div className={`space-y-4 ${ocupado ? 'pointer-events-none' : ''}`} aria-busy={ocupado !== null}>
      <Ficha persona={a} onEditar={() => setModo('editar')} />

      <Bloque icon="key" title="Acceso al sistema">
        {a.hasAccount ? (
          <PermissionRow
            title="Puede entrar al sistema"
            description={
              a.accountIsActive
                ? 'Su cuenta está activa y puede iniciar sesión.'
                : 'Su cuenta existe pero está apagada: no puede entrar.'
            }
            checked={a.accountIsActive}
            busy={ocupado === 'acceso'}
            /* Apagarse la cuenta a uno mismo es el único movimiento sin vuelta
               atrás del panel. El backend también lo impide; aquí se muestra
               desactivado para que ni siquiera se intente. */
            disabled={esYoMismo && a.accountIsActive}
            note={
              esYoMismo && a.accountIsActive
                ? 'Es tu propia cuenta: apagarla te dejaría fuera del sistema.'
                : a.email
            }
            onChange={(next) =>
              aplicar(
                'acceso',
                () => admin.setAccountAccess(personId, next),
                next ? 'Acceso devuelto' : 'Acceso retirado',
              )
            }
          />
        ) : (
          <Accion
            title="Todavía no tiene cuenta"
            description="Sin cuenta no puede entrar al sistema ni recibir permisos."
            label="Crear cuenta de acceso"
            onClick={() => setModo('cuenta')}
          />
        )}
      </Bloque>

      {/*
       * Los permisos van contra la CUENTA, no contra la persona (regla 7.4).
       * Por eso el bloque se muestra igualmente sin cuenta, pero bloqueado y
       * diciendo por qué: ocultarlo dejaría al Pastor buscando dónde están.
       */}
      <Bloque icon="shield" title="Permisos del sistema">
        {a.roles.map((rol) => (
          <PermissionRow
            key={rol.name}
            title={rol.name}
            description={rol.description}
            checked={rol.granted}
            busy={ocupado === `rol:${rol.name}`}
            disabled={!a.hasAccount}
            note={!a.hasAccount ? 'Necesita una cuenta de acceso para recibir permisos.' : undefined}
            onChange={(next) =>
              aplicar(
                `rol:${rol.name}`,
                () =>
                  next
                    ? admin.assignRole(a.userAccountId!, rol.name)
                    : admin.revokeRole(a.userAccountId!, rol.name),
                next ? `Permiso otorgado: ${rol.name}` : `Permiso retirado: ${rol.name}`,
              )
            }
          />
        ))}
      </Bloque>

      <Bloque icon="church" title="Membresía oficial">
        <PermissionRow
          title="Es miembro oficial"
          description="Decisión administrativa aparte: asistir no hace miembro a nadie."
          checked={a.isOfficialMember}
          busy={ocupado === 'membresia'}
          note={
            a.memberSince
              ? `Ingresó el ${a.memberSince}.`
              : a.hasMembershipRecord
                ? 'Sin fecha de ingreso registrada.'
                : undefined
          }
          onChange={(next) =>
            aplicar(
              'membresia',
              /* La primera vez hay que CREAR la membresía; a partir de ahí la
                 fila ya existe y solo cambia de estado. Nunca se borra: se
                 perdería la fecha de ingreso y quién la registró. */
              () =>
                a.hasMembershipRecord
                  ? admin.setMembership(personId, next)
                  : admin.grantMembership(personId),
              next ? 'Registrado como miembro' : 'Dado de baja como miembro',
            )
          }
        />
      </Bloque>

      <Bloque icon="star" title="Cargos">
        {a.positions.map((cargo) => (
          <PermissionRow
            key={cargo.positionId}
            title={cargo.name}
            description={
              cargo.isExecutiveBody
                ? 'Cargo del cuerpo ejecutivo de la iglesia.'
                : 'Cargo eclesiástico. No otorga permisos por sí solo.'
            }
            checked={cargo.held}
            busy={ocupado === `cargo:${cargo.positionId}`}
            onChange={(next) =>
              aplicar(
                `cargo:${cargo.positionId}`,
                () =>
                  next
                    ? admin.assignPosition(cargo.positionId, personId)
                    : admin.revokePosition(cargo.positionId, personId),
                next ? `Cargo asignado: ${cargo.name}` : `Cargo retirado: ${cargo.name}`,
              )
            }
          />
        ))}
      </Bloque>

      <Bloque icon="group" title="Liderazgo de ministerios">
        {a.ministries.map((m) => (
          <PermissionRow
            key={m.ministryId}
            title={m.name}
            description="Responsable de este ministerio."
            checked={m.ledByThisPerson}
            busy={ocupado === `min:${m.ministryId}`}
            /* Un grupo tiene como mucho un líder activo (regla 7.11). Si ya lo
               lidera otra persona, asignar fallaría: se bloquea y se dice quién. */
            disabled={!m.ledByThisPerson && m.currentLeaderName !== null}
            note={
              !m.ledByThisPerson && m.currentLeaderName
                ? `Hoy lo lidera ${m.currentLeaderName}. Retíraselo primero.`
                : undefined
            }
            onChange={(next) =>
              aplicar(
                `min:${m.ministryId}`,
                () =>
                  next
                    ? admin.assignMinistryLeader(m.ministryId, personId)
                    : admin.revokeMinistryLeader(m.ministryId),
                next ? `Ahora lidera ${m.name}` : `Ya no lidera ${m.name}`,
              )
            }
          />
        ))}
      </Bloque>

      {/* Las sociedades llevan DOS vínculos independientes: liderarla y ser
          integrante. El líder no es necesariamente integrante. */}
      <Bloque icon="book" title="Grupos de Escuela Dominical">
        {a.societies.map((s) => (
          <div key={s.societyId} className="border-t border-line first:border-t-0">
            <PermissionRow
              title={`Lidera ${s.name}`}
              description="Responsable del grupo."
              checked={s.ledByThisPerson}
              busy={ocupado === `soc:${s.societyId}`}
              disabled={!s.ledByThisPerson && s.currentLeaderName !== null}
              note={
                !s.ledByThisPerson && s.currentLeaderName
                  ? `Hoy lo lidera ${s.currentLeaderName}. Retíraselo primero.`
                  : undefined
              }
              onChange={(next) =>
                aplicar(
                  `soc:${s.societyId}`,
                  () =>
                    next
                      ? admin.assignSocietyLeader(s.societyId, personId)
                      : admin.revokeSocietyLeader(s.societyId),
                  next ? `Ahora lidera ${s.name}` : `Ya no lidera ${s.name}`,
                )
              }
            />
            <PermissionRow
              title={`Pertenece a ${s.name}`}
              description="Aparece en la lista de asistencia de este grupo."
              checked={s.isMember}
              busy={ocupado === `soc-m:${s.societyId}`}
              onChange={(next) =>
                aplicar(
                  `soc-m:${s.societyId}`,
                  () =>
                    next
                      ? admin.addSocietyMember(s.societyId, personId)
                      : admin.removeSocietyMember(s.societyMembershipId!),
                  next ? `Agregada a ${s.name}` : `Quitada de ${s.name}`,
                )
              }
            />
          </div>
        ))}
      </Bloque>

      <Bloque icon="person" title="Presencia en el directorio" tone="danger">
        <PermissionRow
          title="Aparece en el directorio"
          description="Al quitarla deja de salir en las listas. No se borra nada: su historial se conserva."
          checked={a.isActive}
          busy={ocupado === 'directorio'}
          disabled={esYoMismo && a.isActive}
          note={esYoMismo && a.isActive ? 'Es tu propia ficha.' : undefined}
          onChange={(next) =>
            aplicar(
              'directorio',
              () => (next ? admin.reactivatePerson(personId) : admin.deactivatePerson(personId)),
              next ? 'Devuelta al directorio' : 'Quitada del directorio',
            )
          }
        />
      </Bloque>
    </div>
  )
}

function Bloque({
  icon,
  title,
  tone = 'plain',
  children,
}: {
  icon: IconName
  title: string
  tone?: 'plain' | 'danger'
  children: React.ReactNode
}) {
  return (
    <section className="rounded-card pane bg-surface overflow-hidden">
      <div className="flex items-center gap-2.5 px-5 sm:px-6 pt-5 pb-1">
        <Icon
          name={icon}
          className={`size-4.5 ${tone === 'danger' ? 'text-danger' : 'text-ink-faint'}`}
          strokeWidth={2.1}
        />
        <p className="eyebrow text-ink-soft">{title}</p>
      </div>
      <div className="pb-2 divide-y divide-line">{children}</div>
    </section>
  )
}

function Accion({
  title,
  description,
  label,
  onClick,
}: {
  title: string
  description: string
  label: string
  onClick: () => void
}) {
  return (
    <div className="px-5 sm:px-6 py-4">
      <p className="font-medium">{title}</p>
      <p className="text-sm text-ink-soft leading-snug mt-0.5">{description}</p>
      <div className="mt-4">
        <Button variant="secondary" full icon="plus" onClick={onClick}>
          {label}
        </Button>
      </div>
    </div>
  )
}

/** BCrypt trunca más allá de 72 bytes; el backend rechaza lo que lo exceda. */
const MIN_CLAVE = 8

function AccountForm({
  personId,
  firstName,
  onCancel,
  onDone,
}: {
  personId: string
  firstName: string
  onCancel: () => void
  onDone: () => void
}) {
  const [email, setEmail] = useState('')
  const [clave, setClave] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const corta = clave.length > 0 && clave.length < MIN_CLAVE

  async function enviar(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await admin.createUserAccount(personId, email.trim(), clave)
      onDone()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo crear la cuenta.')
      setBusy(false)
    }
  }

  return (
    <form onSubmit={enviar} className="space-y-5">
      <p className="text-ink-soft">
        Con esto {firstName} podrá entrar al sistema. Dale la contraseña en persona;
        no se envía por ningún lado.
      </p>

      <Field
        label="Correo electrónico"
        type="email"
        icon="mail"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        autoComplete="off"
        autoCapitalize="off"
        spellCheck={false}
        required
        placeholder="nombre@ejemplo.com"
      />

      <Field
        label="Contraseña"
        type="text"
        icon="lock"
        value={clave}
        onChange={(e) => setClave(e.target.value)}
        autoComplete="off"
        required
        placeholder="Mínimo 8 caracteres"
        hint="Se muestra a propósito, para que puedas dictársela sin equivocarte."
        error={corta ? `Faltan ${MIN_CLAVE - clave.length} caracteres.` : null}
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
        <Button type="submit" full loading={busy} disabled={corta || !email || !clave}>
          Crear cuenta
        </Button>
        <Button type="button" variant="secondary" full onClick={onCancel}>
          Cancelar
        </Button>
      </div>
    </form>
  )
}

/* ── Alta de una persona ────────────────────────────────────────────────── */

export function RegisterPersonSheet({
  open,
  onClose,
  onDone,
}: {
  open: boolean
  onClose: () => void
  onDone: (nombre: string) => void
}) {
  const [nombre, setNombre] = useState('')
  const [apellido, setApellido] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (open) {
      setNombre('')
      setApellido('')
      setError(null)
      setBusy(false)
    }
  }, [open])

  async function enviar(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await admin.registerPerson(nombre.trim(), apellido.trim())
      onDone(`${nombre.trim()} ${apellido.trim()}`)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo registrar.')
      setBusy(false)
    }
  }

  return (
    <Sheet open={open} onClose={onClose} eyebrow="Directorio" title="Registrar persona">
      <form onSubmit={enviar} className="space-y-5">
        <p className="text-ink-soft">
          Solo queda registrada en el directorio. No la hace miembro oficial ni le da
          acceso al sistema: eso se decide después, por separado.
        </p>

        <Field
          label="Nombre"
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          required
          maxLength={100}
          placeholder="Ana"
        />
        <Field
          label="Apellidos"
          value={apellido}
          onChange={(e) => setApellido(e.target.value)}
          required
          maxLength={100}
          placeholder="Gómez Ruiz"
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
            Registrar
          </Button>
          <Button type="button" variant="secondary" full onClick={onClose}>
            Cancelar
          </Button>
        </div>
      </form>
    </Sheet>
  )
}

/* ── Ficha: quién es esta persona ───────────────────────────────────────── */

/**
 * Lo primero que se ve al abrir a alguien: su nombre completo, dónde asiste y qué
 * tan constante ha sido. Va ARRIBA de los permisos a propósito — antes de decidir
 * qué puede hacer alguien conviene recordar quién es.
 *
 * El nombre aquí NO se trunca. En la lista sí, porque una fila que crece a tres
 * renglones descoloca todo lo demás; pero cuando alguien abre a una persona es
 * justamente para leer su nombre entero.
 */
function Ficha({ persona, onEditar }: { persona: PersonAdministration; onEditar: () => void }) {
  const nombre = `${persona.firstName} ${persona.lastName}`
  const casa = persona.familyGroup

  return (
    <section className="rounded-panel bg-surface pane shadow-card p-5 sm:p-6">
      <div className="flex items-start gap-4">
        <Avatar person={persona} size="xl" />
        <div className="min-w-0 flex-1">
          {/* Sin `truncate`: aquí el nombre completo es el dato. */}
          <h2 className="font-display text-xl sm:text-2xl font-medium leading-tight text-balance">
            {nombre}
          </h2>
          <div className="mt-2 flex flex-wrap gap-1.5">
            <Chip
              tone={persona.isOfficialMember ? 'forest' : 'neutral'}
              icon={persona.isOfficialMember ? 'check' : 'clock'}
            >
              {persona.isOfficialMember ? 'Miembro oficial' : 'No es miembro oficial'}
            </Chip>
            {!persona.isActive && <Chip tone="danger" icon="ban">Fuera del directorio</Chip>}
          </div>
        </div>
      </div>

      <dl className="mt-5 space-y-3">
        <Dato etiqueta="Grupo Familiar">
          {casa ? (
            <>
              <span className="block">{casa.address}</span>
              <span className="block text-sm text-ink-faint leading-snug mt-0.5">
                {/* Anfitrión y Encargado son roles distintos que a menudo recaen en
                    la misma persona; solo se nombran los dos cuando difieren. */}
                {casa.isHost && casa.isLeader
                  ? 'Es su casa y dirige la reunión'
                  : casa.isHost
                    ? `Es su casa · dirige ${casa.leaderName}`
                    : casa.isLeader
                      ? `Dirige la reunión · casa de ${casa.hostName}`
                      : casa.hostName === casa.leaderName
                        ? `Casa de ${casa.hostName}`
                        : `Casa de ${casa.hostName} · dirige ${casa.leaderName}`}
                {' · desde '}
                {formatLongDate(casa.joinedAt)}
              </span>
            </>
          ) : (
            <span className="text-ink-faint">No asiste a ningún grupo</span>
          )}
        </Dato>

        {persona.phoneNumber && <Dato etiqueta="Teléfono">{persona.phoneNumber}</Dato>}
        {persona.dateOfBirth && (
          <Dato etiqueta="Nacimiento">{formatLongDate(persona.dateOfBirth)}</Dato>
        )}
      </dl>

      <Racha marcas={persona.recentAttendance} />

      <div className="mt-5">
        <Button variant="secondary" full icon="edit" onClick={onEditar}>
          Editar datos
        </Button>
      </div>
    </section>
  )
}

function Dato({ etiqueta, children }: { etiqueta: string; children: React.ReactNode }) {
  return (
    <div>
      <dt className="text-sm text-ink-faint">{etiqueta}</dt>
      <dd className="leading-snug mt-0.5">{children}</dd>
    </div>
  )
}

/**
 * Constancia: una casilla por reunión, de la más antigua a la más reciente.
 *
 * Es la misma idea que la matriz del tablero, pero por persona. Un promedio
 * ("asiste al 70%") esconde justo lo que importa — si ese 30% que falta está
 * repartido o son las últimas seis seguidas.
 */
function Racha({ marcas }: { marcas: AdminAttendance[] }) {
  if (marcas.length === 0) {
    return (
      <div className="mt-5">
        <p className="text-sm text-ink-faint">Constancia</p>
        <p className="text-ink-faint leading-snug mt-1">
          Todavía no aparece en ningún reporte.
        </p>
      </div>
    )
  }

  const presentes = marcas.filter((m) => m.wasPresent).length

  return (
    <div className="mt-5">
      <div className="flex items-baseline justify-between gap-3">
        <p className="text-sm text-ink-faint">Constancia</p>
        <p className="text-sm text-ink-soft tabular">
          {presentes} de {marcas.length}
        </p>
      </div>

      <div className="mt-2 flex flex-wrap gap-1.5">
        {marcas.map((m, i) => (
          <span
            key={`${m.date}-${m.source}-${i}`}
            title={`${formatCompactDate(m.date)} · ${m.source} · ${m.wasPresent ? 'presente' : 'ausente'}`}
            className={`size-5 rounded-[0.4rem] ${m.wasPresent ? 'bg-forest' : 'bg-bone-deep'}`}
          />
        ))}
      </div>

      <p className="mt-2 text-sm text-ink-faint leading-snug">
        De {formatCompactDate(marcas[0].date)} a {formatCompactDate(marcas[marcas.length - 1].date)}.
        Cuenta cultos y Grupos Familiares.
      </p>
    </div>
  )
}

/* ── Editar la ficha ────────────────────────────────────────────────────── */

/**
 * Corrige los datos de la persona. Nada de lo que ES en la iglesia: eso vive en
 * los interruptores de abajo, y cada cosa tiene su propia operación.
 *
 * Está pensado para crecer — el día que haga falta dirección, correo o lo que
 * sea, se agrega un campo aquí y otro al comando, sin tocar nada más.
 */
function EditPersonForm({
  persona,
  onCancel,
  onDone,
}: {
  persona: PersonAdministration
  onCancel: () => void
  onDone: (nombre: string) => void
}) {
  const [nombre, setNombre] = useState(persona.firstName)
  const [apellido, setApellido] = useState(persona.lastName)
  const [nacimiento, setNacimiento] = useState(persona.dateOfBirth ?? '')
  const [telefono, setTelefono] = useState(persona.phoneNumber ?? '')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function enviar(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await admin.updatePerson(
        persona.personId,
        nombre.trim(),
        apellido.trim(),
        nacimiento || null,
        telefono.trim() || null,
      )
      onDone(`${nombre.trim()} ${apellido.trim()}`)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudieron guardar los datos.')
      setBusy(false)
    }
  }

  return (
    <form onSubmit={enviar} className="space-y-5">
      <p className="text-ink-soft leading-relaxed">
        Corrige su ficha. Esto no cambia su membresía, sus permisos ni sus grupos: cada una
        de esas cosas se decide por separado.
      </p>

      <Field
        label="Nombre"
        value={nombre}
        onChange={(e) => setNombre(e.target.value)}
        required
        maxLength={100}
      />
      <Field
        label="Apellidos"
        value={apellido}
        onChange={(e) => setApellido(e.target.value)}
        required
        maxLength={100}
      />
      <Field
        label="Fecha de nacimiento (opcional)"
        type="date"
        icon="calendar"
        value={nacimiento}
        onChange={(e) => setNacimiento(e.target.value)}
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
          Guardar cambios
        </Button>
        <Button type="button" variant="secondary" full onClick={onCancel}>
          Cancelar
        </Button>
      </div>
    </form>
  )
}
