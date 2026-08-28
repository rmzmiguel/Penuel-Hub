import type { AuthSession } from '../api/types'
import {
  CAPABILITIES,
  CAPTURE_CONTEXT,
  MINISTRIES,
  PEOPLE,
  POSITIONS,
  ROLES,
  SERVICE_TYPES,
  SESSIONS,
  SOCIETIES,
  SOCIETY_MEMBER_INDEXES,
} from './data'

/**
 * Backend de demostración.
 *
 * Intercepta las peticiones cuando la sesión es de demostración y responde con
 * los datos de `data.ts`. Sirve para enseñar la interfaz sin levantar la API .NET.
 *
 * ══════════════════════════════════════════════════════════════════════════
 * REGLA: esta simulación solo atiende rutas que EXISTEN en el backend real, y
 * responde EXACTAMENTE la forma que ese backend devuelve.
 *
 * La versión anterior servía `GET /api/persons/directory` y
 * `GET /api/admin/catalogs`, que nunca existieron, y aceptaba `personId` en
 * `POST /api/roles/assign`, donde el backend pide `userAccountId`. La pantalla
 * de Personas se construyó contra esta simulación, funcionaba aquí, y contra la
 * API real devolvía 404. Una demostración más generosa que el backend no es una
 * ayuda: es una trampa que retrasa el error hasta producción.
 *
 * Todo lo que no esté cubierto se rechaza con un mensaje explícito, nunca con
 * un vacío silencioso.
 * ══════════════════════════════════════════════════════════════════════════
 *
 * Se activa SOLO con `startDemo()` desde la pantalla de acceso. Ninguna sesión
 * real pasa por aquí.
 */

const DEMO_TOKEN = 'demo'

export function isDemoSession(session: { accessToken: string } | null) {
  return session?.accessToken === DEMO_TOKEN
}

export function demoSession(): AuthSession {
  const hour = 60 * 60 * 1000
  return {
    accessToken: DEMO_TOKEN,
    accessTokenExpiresAt: new Date(Date.now() + 12 * hour).toISOString(),
    refreshToken: DEMO_TOKEN,
    refreshTokenExpiresAt: new Date(Date.now() + 24 * hour).toISOString(),
    userAccountId: 'demo-account',
    personId: CAPABILITIES.personId,
    email: CAPABILITIES.email ?? 'demo@penuel.mx',
    roles: CAPABILITIES.roles,
  }
}

/**
 * Estado mutable de la demostración. Se reinicia en cada carga de la página.
 *
 * El panel de administración obligó a simular de verdad: hasta ahora bastaba con
 * confirmar las operaciones —"cuenta creada"— porque nada las volvía a leer.
 * Ahora `GET /administration` sí las lee, y una simulación que confirmara sin
 * guardar mostraría el interruptor volviendo solo a su sitio.
 */
const state = {
  people: structuredClone(PEOPLE),
  /** Pertenencia a grupos: societyId -> personIds. */
  members: new Map<string, string[]>(
    SOCIETIES.map((s) => [
      s.id,
      (SOCIETY_MEMBER_INDEXES[s.id] ?? []).map((i) => PEOPLE[i]?.id).filter(Boolean),
    ]),
  ),
  /** personId -> cuenta. Solo quien la tiene aparece aquí. */
  accounts: new Map<string, { email: string; isActive: boolean }>([
    [CAPABILITIES.personId, { email: CAPABILITIES.email ?? 'demo@penuel.mx', isActive: true }],
  ]),
  /** personId -> roles vivos. Van contra la cuenta, igual que en el backend. */
  roles: new Map<string, Set<string>>([[CAPABILITIES.personId, new Set(CAPABILITIES.roles)]]),
  /** personId -> membresía. `false` = existe la fila pero está dada de baja. */
  memberships: new Map<string, boolean>([[CAPABILITIES.personId, true]]),
  /** personId -> cargos. */
  positions: new Map<string, Set<string>>([
    [CAPABILITIES.personId, new Set([POSITIONS[0].id])],
  ]),
  /** groupId -> personId del líder vivo. Como mucho uno (regla 7.11). */
  societyLeaders: new Map<string, string>(),
  ministryLeaders: new Map<string, string>(),

  // --- Rama de Grupos Familiares ---
  /** Dos casas de ejemplo, para que la pantalla del Pastor tenga qué enseñar. */
  familyGroups: [
    {
      familyGroupId: 'fg-1',
      address: 'Calle Hidalgo 120, Col. Centro',
      defaultMeetingDayOfWeek: 4,
      isActive: true,
      hostIndex: 7,
      leaderIndex: 7,
    },
    {
      familyGroupId: 'fg-2',
      address: 'Av. Tamaulipas 455, Col. Moderna',
      defaultMeetingDayOfWeek: 4,
      isActive: true,
      hostIndex: 10,
      leaderIndex: 12,
    },
  ],
  /** familyGroupId -> personIds vivos. La regla 7.2 la impone `enOtroGrupo`. */
  groupMembers: new Map<string, string[]>([
    ['fg-1', [1, 3, 5, 9].map((i) => PEOPLE[i]?.id).filter(Boolean)],
    ['fg-2', [11, 13, 15].map((i) => PEOPLE[i]?.id).filter(Boolean)],
  ]),
  /** familyGroupId -> reportes levantados. */
  meetings: new Map<string, { meetingId: string; meetingDate: string; totalOffering: number; presentCount: number; memberCount: number }[]>(),
}

/** Regla 7.2 simulada: ¿esta persona ya está viva en ALGÚN grupo? */
const enOtroGrupo = (personId: string) =>
  [...state.groupMembers.values()].some((ids) => ids.includes(personId))

function grupoDemo(id: string) {
  return state.familyGroups.find((g) => g.familyGroupId === id)
}

function integrantesDemo(id: string) {
  const ids = state.groupMembers.get(id) ?? []
  return ids
    .map((pid) => state.people.find((p) => p.id === pid))
    .filter((p): p is (typeof state.people)[number] => Boolean(p))
    .map((p) => ({ personId: p.id, firstName: p.firstName, lastName: p.lastName }))
}

function ultimoReporte(id: string) {
  const lista = state.meetings.get(id) ?? []
  return lista.length > 0 ? lista[0].meetingDate : null
}

const nombreDe = (personId: string) => {
  const p = find(personId)
  return p ? `${p.firstName} ${p.lastName}` : null
}

/**
 * Racha de asistencias inventada, pero NO al azar puro.
 *
 * Se deriva del identificador de la persona, así que la misma sale siempre igual:
 * una racha que cambia en cada recarga no sirve para enseñar la pantalla, porque
 * quien la mira no sabe si está viendo un dato o un parpadeo. Y se sesga hacia
 * presente —la mayoría de la gente sí va— para que las ausencias se lean como lo
 * que son: la excepción que vale la pena mirar.
 */
function rachaDemo(personId: string) {
  let semilla = 0
  for (let i = 0; i < personId.length; i++) semilla = (semilla * 31 + personId.charCodeAt(i)) >>> 0

  const fuentes = ['Culto General', 'Culto de Oración', 'Grupo Familiar', 'Escuela Dominical']
  const marcas: { date: string; wasPresent: boolean; source: string }[] = []
  const hoy = new Date()

  for (let i = 17; i >= 0; i--) {
    semilla = (semilla * 1103515245 + 12345) >>> 0
    const dia = new Date(hoy)
    dia.setDate(hoy.getDate() - i * 4)
    marcas.push({
      date: dia.toISOString().slice(0, 10),
      wasPresent: (semilla >>> 16) % 10 > 2,
      source: fuentes[(semilla >>> 8) % fuentes.length],
    })
  }
  return marcas
}

/** Espejo de `PersonAdministrationResponse`, armado desde el estado de arriba. */
function administracion(personId: string) {
  const persona = find(personId)
  if (!persona) return null

  const cuenta = state.accounts.get(personId)
  const susRoles = state.roles.get(personId) ?? new Set<string>()
  const susCargos = state.positions.get(personId) ?? new Set<string>()
  const membresia = state.memberships.get(personId)

  return {
    personId,
    firstName: persona.firstName,
    lastName: persona.lastName,
    isActive: persona.status === 0,
    userAccountId: cuenta ? `demo-acc-${personId}` : null,
    email: cuenta?.email ?? null,
    hasAccount: cuenta !== undefined,
    accountIsActive: cuenta?.isActive ?? false,
    dateOfBirth: null,
    phoneNumber: null,
    isOfficialMember: membresia === true,
    hasMembershipRecord: membresia !== undefined,
    memberSince: null,
    roles: ROLES.map((r) => ({ ...r, granted: susRoles.has(r.name) })),
    positions: POSITIONS.map((c) => ({
      positionId: c.id,
      name: c.name,
      isExecutiveBody: c.name !== 'Diácono',
      held: susCargos.has(c.id),
    })),
    ministries: MINISTRIES.map((m) => {
      const lider = state.ministryLeaders.get(m.id)
      return {
        ministryId: m.id,
        name: m.name,
        ledByThisPerson: lider === personId,
        currentLeaderName: lider ? nombreDe(lider) : null,
      }
    }),
    societies: SOCIETIES.map((sc) => {
      const lider = state.societyLeaders.get(sc.id)
      const esIntegrante = (state.members.get(sc.id) ?? []).includes(personId)
      return {
        societyId: sc.id,
        name: sc.name,
        ledByThisPerson: lider === personId,
        currentLeaderName: lider ? nombreDe(lider) : null,
        isMember: esIntegrante,
        societyMembershipId: esIntegrante ? `demo-sm~${sc.id}~${personId}` : null,
      }
    }),
    familyGroup: (() => {
      const casa = state.familyGroups.find((g) =>
        (state.groupMembers.get(g.familyGroupId) ?? []).includes(personId),
      )
      if (!casa) return null
      const host = PEOPLE[casa.hostIndex]
      const lider = PEOPLE[casa.leaderIndex]
      return {
        familyGroupId: casa.familyGroupId,
        address: casa.address,
        joinedAt: '2026-01-15',
        isHost: host?.id === personId,
        isLeader: lider?.id === personId,
        hostName: `${host?.firstName ?? ''} ${host?.lastName ?? ''}`.trim(),
        leaderName: `${lider?.firstName ?? ''} ${lider?.lastName ?? ''}`.trim(),
      }
    })(),
    recentAttendance: rachaDemo(personId),
  }
}

const find = (personId: string) => state.people.find((p) => p.id === personId)

const activos = () => state.people.filter((p) => p.status === 0)

const comoOpcion = ({ id, firstName, lastName }: { id: string; firstName: string; lastName: string }) => ({
  id,
  firstName,
  lastName,
})

/** Latencia simulada: sin ella, los estados de carga nunca se ven ni se prueban. */
const delay = <T,>(value: T, ms = 260) => new Promise<T>((r) => setTimeout(() => r(value), ms))

type Body = Record<string, unknown> | undefined

export function handleDemo(path: string, method: string, body: Body): Promise<unknown> | null {
  const route = `${method} ${path.split('?')[0]}`

  switch (route) {
    case 'GET /api/me/capabilities':
      return delay(CAPABILITIES, 380)
    case 'GET /api/persons':
      return delay(activos().map(comoOpcion), 320)
    case 'GET /api/service-sessions':
      return delay(SESSIONS, 420)
    case 'GET /api/service-types':
      return delay(SERVICE_TYPES)
    case 'GET /api/sunday-school/capture-context':
      return delay(CAPTURE_CONTEXT, 380)

    case 'GET /api/family-groups/mine': {
      // En la demostración se entra como Pastor, que no lleva ninguna casa. Devolver
      // vacío es lo fiel: es lo que respondería la API real para esa cuenta.
      const mios = state.familyGroups.filter(
        (g) =>
          g.isActive &&
          (PEOPLE[g.hostIndex]?.id === CAPABILITIES.personId ||
            PEOPLE[g.leaderIndex]?.id === CAPABILITIES.personId),
      )
      return delay(
        mios.map((g) => ({
          familyGroupId: g.familyGroupId,
          address: g.address,
          defaultMeetingDayOfWeek: g.defaultMeetingDayOfWeek,
          isHost: PEOPLE[g.hostIndex]?.id === CAPABILITIES.personId,
          isLeader: PEOPLE[g.leaderIndex]?.id === CAPABILITIES.personId,
          hostFirstName: PEOPLE[g.hostIndex]?.firstName ?? '',
          hostLastName: PEOPLE[g.hostIndex]?.lastName ?? '',
          lastMeetingDate: ultimoReporte(g.familyGroupId),
          members: integrantesDemo(g.familyGroupId),
        })),
        320,
      )
    }

    case 'GET /api/family-groups':
      return delay(
        state.familyGroups.map((g) => ({
          familyGroupId: g.familyGroupId,
          address: g.address,
          defaultMeetingDayOfWeek: g.defaultMeetingDayOfWeek,
          isActive: g.isActive,
          hostFirstName: PEOPLE[g.hostIndex]?.firstName ?? '',
          hostLastName: PEOPLE[g.hostIndex]?.lastName ?? '',
          leaderFirstName: PEOPLE[g.leaderIndex]?.firstName ?? '',
          leaderLastName: PEOPLE[g.leaderIndex]?.lastName ?? '',
          activeMemberCount: (state.groupMembers.get(g.familyGroupId) ?? []).length,
          lastMeetingDate: ultimoReporte(g.familyGroupId),
        })),
        360,
      )

    case 'POST /api/family-groups': {
      const id = `fg-${state.familyGroups.length + 1}`
      const idx = (pid: unknown) => state.people.findIndex((p) => p.id === pid)
      state.familyGroups.push({
        familyGroupId: id,
        address: String(body?.address ?? ''),
        defaultMeetingDayOfWeek: Number(body?.defaultMeetingDayOfWeek ?? 4),
        isActive: true,
        hostIndex: idx(body?.hostPersonId),
        leaderIndex: idx(body?.leaderPersonId ?? body?.hostPersonId),
      })
      state.groupMembers.set(id, [])
      return delay({ id }, 520)
    }

    case 'POST /api/persons': {
      const persona = {
        id: `demo-p${state.people.length}`,
        firstName: String(body?.firstName ?? ''),
        lastName: String(body?.lastName ?? ''),
        status: 0 as const,
      }
      state.people.push(persona)
      return delay({ id: persona.id }, 500)
    }

    case 'POST /api/memberships': {
      const personId = String(body?.personId ?? '')
      if (!find(personId)) return delay(null)
      state.memberships.set(personId, true)
      return delay({ id: 'demo' }, 500)
    }

    case 'POST /api/roles/assign': {
      // Contra la CUENTA, igual que el backend real. La demostración la resuelve
      // de vuelta a su persona porque su estado se indexa así.
      const cuentaId = String(body?.userAccountId ?? '')
      const personId = cuentaId.replace('demo-acc-', '')
      if (!state.accounts.has(personId)) return delay(null)
      state.roles.set(personId, new Set([...(state.roles.get(personId) ?? []), String(body?.roleName)]))
      return delay({ id: 'demo' }, 420)
    }

    case 'POST /api/roles/revoke': {
      const cuentaId = String(body?.userAccountId ?? '')
      const personId = cuentaId.replace('demo-acc-', '')
      state.roles.get(personId)?.delete(String(body?.roleName))
      return delay(null, 420)
    }

    case 'POST /api/sunday-school/reports':
    case 'POST /api/service-sessions/general':
      // La demostración acepta el reporte y devuelve un identificador, pero no
      // lo guarda: el historial se recalcula al recargar y volvería a salir sin él.
      return delay({ id: 'demo-session' }, 700)
  }

  // ── Rutas con identificador en el camino ────────────────────────────────
  const miembros = path.match(/^\/api\/societies\/([^/]+)\/members$/)
  if (miembros) {
    const society = SOCIETIES.find((x) => x.id === miembros[1])
    if (!society) return delay(null)

    if (method === 'POST') {
      const personId = String(body?.personId ?? '')
      const lista = state.members.get(society.id) ?? []
      if (!lista.includes(personId)) state.members.set(society.id, [...lista, personId])
      return delay({ id: 'demo' }, 450)
    }

    const ids = new Set(state.members.get(society.id) ?? [])
    return delay(
      {
        societyId: society.id,
        societyName: society.name,
        members: activos().filter((p) => ids.has(p.id)).map(comoOpcion),
      },
      320,
    )
  }

  // ── Grupos Familiares con identificador en el camino ────────────────────
  const detalleGrupo = path.match(/^\/api\/family-groups\/(fg-[^/]+)$/)
  if (detalleGrupo && method === 'GET') {
    const g = grupoDemo(detalleGrupo[1])
    if (!g) return delay(null)
    const miembros = integrantesDemo(g.familyGroupId)
    return delay(
      {
        familyGroupId: g.familyGroupId,
        address: g.address,
        defaultMeetingDayOfWeek: g.defaultMeetingDayOfWeek,
        isActive: g.isActive,
        hostPersonId: PEOPLE[g.hostIndex]?.id ?? '',
        hostFirstName: PEOPLE[g.hostIndex]?.firstName ?? '',
        hostLastName: PEOPLE[g.hostIndex]?.lastName ?? '',
        leaderPersonId: PEOPLE[g.leaderIndex]?.id ?? '',
        leaderFirstName: PEOPLE[g.leaderIndex]?.firstName ?? '',
        leaderLastName: PEOPLE[g.leaderIndex]?.lastName ?? '',
        members: miembros.map((m) => ({ ...m, joinedAt: '2026-01-15' })),
        recentMeetings: state.meetings.get(g.familyGroupId) ?? [],
      },
      380,
    )
  }

  const estadoGrupo = path.match(/^\/api\/family-groups\/(fg-[^/]+)\/status$/)
  if (estadoGrupo) {
    const g = grupoDemo(estadoGrupo[1])
    if (g) g.isActive = Boolean(body?.isActive)
    return delay(null, 420)
  }

  const disponibles = path.match(/^\/api\/family-groups\/(fg-[^/]+)\/available-persons$/)
  if (disponibles) {
    const texto = (new URLSearchParams(path.split('?')[1] ?? '').get('search') ?? '').toLowerCase()
    const lista = activos()
      .filter((p) => !texto || `${p.firstName} ${p.lastName}`.toLowerCase().includes(texto))
      .slice(0, 40)
      // Regla 7.5: se dice que NO está disponible, nunca en qué otra casa está.
      .map((p) => ({
        personId: p.id,
        firstName: p.firstName,
        lastName: p.lastName,
        isAvailable: !enOtroGrupo(p.id),
      }))
    return delay(lista, 320)
  }

  const registrarEnGrupo = path.match(/^\/api\/family-groups\/(fg-[^/]+)\/members\/register$/)
  if (registrarEnGrupo) {
    const persona = {
      id: `demo-p${state.people.length}`,
      firstName: String(body?.firstName ?? ''),
      lastName: String(body?.lastName ?? ''),
      status: 0 as const,
    }
    state.people.push(persona)
    const actuales = state.groupMembers.get(registrarEnGrupo[1]) ?? []
    state.groupMembers.set(registrarEnGrupo[1], [...actuales, persona.id])
    return delay({ id: persona.id }, 520)
  }

  const miembroGrupo = path.match(/^\/api\/family-groups\/(fg-[^/]+)\/members(?:\/([^/]+))?$/)
  if (miembroGrupo) {
    const [, grupoId, personIdEnRuta] = miembroGrupo

    if (method === 'DELETE') {
      state.groupMembers.set(
        grupoId,
        (state.groupMembers.get(grupoId) ?? []).filter((id) => id !== personIdEnRuta),
      )
      return delay(null, 420)
    }

    const personId = String(body?.personId ?? '')
    if (enOtroGrupo(personId)) {
      // Mismo mensaje genérico que devuelve la API real.
      return new Promise((_, reject) =>
        setTimeout(
          () =>
            reject(
              new Error(
                'Esta persona ya pertenece a un Grupo Familiar. Para moverla, primero hay que quitarla del suyo.',
              ),
            ),
          320,
        ),
      )
    }
    const actuales = state.groupMembers.get(grupoId) ?? []
    state.groupMembers.set(grupoId, [...actuales, personId])
    return delay({ id: 'demo' }, 450)
  }

  const reunion = path.match(/^\/api\/family-groups\/(fg-[^/]+)\/meetings$/)
  if (reunion) {
    const grupoId = reunion[1]
    const lista = (body?.attendances ?? []) as { wasPresent: boolean }[]
    const previos = state.meetings.get(grupoId) ?? []
    state.meetings.set(grupoId, [
      {
        meetingId: `fgm-${previos.length + 1}`,
        meetingDate: String(body?.meetingDate ?? ''),
        totalOffering: Number(body?.totalOffering ?? 0),
        presentCount: lista.filter((a) => a.wasPresent).length,
        memberCount: lista.length,
      },
      ...previos,
    ])
    return delay({ id: 'fgm-demo' }, 640)
  }

  const admin = path.match(/^\/api\/persons\/([^/]+)\/administration$/)
  if (admin) {
    const estado = administracion(admin[1])
    return estado ? delay(estado, 380) : delay(null)
  }

  const cuenta = path.match(/^\/api\/persons\/([^/]+)\/user-account$/)
  if (cuenta) {
    const personId = cuenta[1]
    if (!find(personId)) return delay(null)
    state.accounts.set(personId, { email: String(body?.email ?? ''), isActive: true })
    return delay({ id: 'demo' }, 600)
  }

  const acceso = path.match(/^\/api\/persons\/([^/]+)\/user-account\/access$/)
  if (acceso) {
    const c = state.accounts.get(acceso[1])
    if (c) c.isActive = Boolean(body?.isActive)
    return delay(null, 420)
  }

  const estadoMembresia = path.match(/^\/api\/memberships\/([^/]+)\/status$/)
  if (estadoMembresia) {
    state.memberships.set(estadoMembresia[1], Boolean(body?.isMember))
    return delay(null, 420)
  }

  const cargo = path.match(/^\/api\/positions\/([^/]+)\/holders(?:\/([^/]+))?$/)
  if (cargo) {
    const positionId = cargo[1]
    const personId = cargo[2] ?? String(body?.personId ?? '')
    const suyos = state.positions.get(personId) ?? new Set<string>()
    if (method === 'POST') suyos.add(positionId)
    else suyos.delete(positionId)
    state.positions.set(personId, suyos)
    return delay(method === 'POST' ? { id: 'demo' } : null, 420)
  }

  const liderSociedad = path.match(/^\/api\/societies\/([^/]+)\/leader$/)
  if (liderSociedad) {
    if (method === 'POST') state.societyLeaders.set(liderSociedad[1], String(body?.personId ?? ''))
    else state.societyLeaders.delete(liderSociedad[1])
    return delay(method === 'POST' ? { id: 'demo' } : null, 420)
  }

  const liderMinisterio = path.match(/^\/api\/ministries\/([^/]+)\/leader$/)
  if (liderMinisterio) {
    if (method === 'POST') state.ministryLeaders.set(liderMinisterio[1], String(body?.personId ?? ''))
    else state.ministryLeaders.delete(liderMinisterio[1])
    return delay(method === 'POST' ? { id: 'demo' } : null, 420)
  }

  /* El Id sintético lleva dentro la sociedad y la persona. El separador es `~`
     y no `-` porque los identificadores de la demostración YA llevan guion
     (`s-1`, `demo-p3`): con guion, la primera captura cortaba en `s`. */
  const quitarDeGrupo = path.match(/^\/api\/societies\/members\/demo-sm~([^~]+)~(.+)$/)
  if (quitarDeGrupo) {
    const [, societyId, personId] = quitarDeGrupo
    state.members.set(societyId, (state.members.get(societyId) ?? []).filter((id) => id !== personId))
    return delay(null, 420)
  }

  const fichaPersona = path.match(/^\/api\/persons\/([^/]+)$/)
  if (fichaPersona && method === 'PUT') {
    const persona = find(fichaPersona[1])
    if (!persona) return delay(null)
    persona.firstName = String(body?.firstName ?? persona.firstName)
    persona.lastName = String(body?.lastName ?? persona.lastName)
    return delay(null, 480)
  }

  const ciclo = path.match(/^\/api\/persons\/([^/]+)\/(deactivate|reactivate)$/)
  if (ciclo) {
    const persona = find(ciclo[1])
    if (persona) persona.status = ciclo[2] === 'deactivate' ? 1 : 0
    return delay(null, 450)
  }

  // Cualquier otra ruta cae aquí. Decirlo es mejor que devolver un vacío
  // silencioso — y mucho mejor que inventar una respuesta que la API no da.
  return new Promise((_, reject) =>
    setTimeout(
      () =>
        reject(
          new Error('Esta parte no está incluida en la demostración. Conecta la API para probarla.'),
        ),
      260,
    ),
  )
}
