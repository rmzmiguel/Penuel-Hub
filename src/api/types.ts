/**
 * Contratos del backend .NET, transcritos de los records reales de
 * Penuel.Application — no inventados ni supuestos.
 *
 * ASP.NET serializa en camelCase, los `DateOnly` como "2026-03-01",
 * los `decimal` como número y los enums como ENTERO (no como texto).
 */

// ── Errores ────────────────────────────────────────────────────────────────
/** Forma única de error de toda la API: ProblemDetails con un `code` estable. */
export interface ApiProblem {
  type?: string
  title?: string
  status?: number
  detail?: string
  /** Lo que el frontend debe leer; `detail` es para humanos. */
  code?: string
  traceId?: string
}

// ── Autenticación ──────────────────────────────────────────────────────────
export interface AuthSession {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  userAccountId: string
  personId: string
  email: string
  roles: string[]
}

// ── Capacidades (Core, Sección 8.4) ────────────────────────────────────────
export interface CapabilityRef {
  id: string
  name: string
}

export interface MyCapabilities {
  personId: string
  firstName: string
  lastName: string
  email: string | null
  isOfficialMember: boolean
  isExecutiveBodyMember: boolean
  roles: string[]
  positions: CapabilityRef[]
  ledMinistries: CapabilityRef[]
  ledSocieties: CapabilityRef[]
}

/**
 * Nombres de rol, centralizados igual que en RoleNames del Dominio.
 *
 * OJO: esta lista NO es la que dibuja la pantalla de permisos. Ahí el catálogo
 * lo manda el servidor dentro de `PersonAdministration.roles`, que es quien de
 * verdad lo sabe. Estas constantes existen solo para las pocas decisiones que el
 * frontend toma por sí mismo (qué entradas de navegación mostrar).
 */
export const RoleNames = {
  Pastor: 'Pastor',
  SundaySchoolRecorder: 'SundaySchoolRecorder',
  /** Acceso irrestricto para quien mantiene el sistema. No es un cargo. */
  Developer: 'Desarrollador',
} as const

/** Nombres de cargo, espejo de PositionNames del Dominio. */
export const PositionNames = {
  Pastor: 'Pastor',
  Diacono: 'Diácono',
  SecretarioGeneral: 'Secretario General',
  TesoreroGeneral: 'Tesorero General',
} as const

// ── Catálogos ──────────────────────────────────────────────────────────────
export interface PersonOption {
  id: string
  firstName: string
  lastName: string
}

export interface ServiceTypeOption {
  id: string
  name: string
  requiresSocietyGrouping: boolean
  collectsTithe: boolean
  attendanceCustomary: boolean
}

export interface SocietyMembers {
  societyId: string
  societyName: string
  members: PersonOption[]
}

// ── Escuela Dominical ──────────────────────────────────────────────────────
/** El enum viaja como entero. Coincide con SundaySchoolCaptureMode del backend. */
export const CaptureMode = {
  SingleFixedGroup: 0,
  MultipleFixedGroups: 1,
  NoFixedGroup: 2,
} as const
export type CaptureModeValue = (typeof CaptureMode)[keyof typeof CaptureMode]

export interface TeacherOption {
  personId: string
  firstName: string
  lastName: string
  /** Distingue al titular del grupo del sustituto flotante. */
  hasFixedGroup: boolean
}

export interface SocietyOption {
  societyId: string
  societyName: string
  teacherCandidates: TeacherOption[]
}

export interface CaptureContext {
  personId: string
  mode: CaptureModeValue
  isFloatingSubstitute: boolean
  mySocieties: SocietyOption[]
  allSocieties: SocietyOption[]
}

export interface AttendanceInput {
  personId: string
  wasPresent: boolean
  wasPunctual: boolean | null
  broughtBible: boolean | null
  chaptersRead: number | null
}

export interface SundaySchoolReport {
  serviceTypeId: string
  societyId: string
  /** "YYYY-MM-DD" */
  sessionDate: string
  totalOffering: number
  teacherPersonId: string | null
  attendances: AttendanceInput[]
}

// ── Cultos y tesorería ─────────────────────────────────────────────────────
export interface GeneralServiceReport {
  serviceTypeId: string
  sessionDate: string
  totalOffering: number
  totalTithe: number | null
  preacherPersonId: string | null
}

export interface ServiceSessionSummary {
  sessionId: string
  sessionDate: string
  serviceTypeName: string
  societyId: string | null
  societyName: string | null
  totalOffering: number
  totalTithe: number | null
  teacherName: string | null
  preacherName: string | null
  recordedByName: string
  presentCount: number
}

export interface TitheEntryDetail {
  titheEntryId: string
  personId: string
  firstName: string
  lastName: string
  amount: number
}

export interface SessionTitheDetail {
  serviceSessionId: string
  sessionDate: string
  totalTithe: number | null
  identifiedTotal: number
  /**
   * Lo que se dio sin anotar el nombre en el sobre. Es informativo:
   * la regla 7.5 del backend dice que NO es una discrepancia a corregir.
   */
  unidentifiedAmount: number | null
  entries: TitheEntryDetail[]
}

export interface CreatedResource {
  id: string
}

// ── Grupos Familiares ──────────────────────────────────────────────────────
/**
 * `System.DayOfWeek` viaja como ENTERO, igual que el resto de enums del backend.
 * 0 es domingo, que es la convención de .NET y también la de JavaScript.
 */
export const DIAS = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'] as const

export interface GroupMemberOption {
  personId: string
  firstName: string
  lastName: string
}

/**
 * Un grupo que la persona autenticada lleva. Trae ya la lista de integrantes: la
 * pantalla del Anfitrión se dibuja con UNA llamada, porque se abre dentro de una
 * casa y con la señal que haya.
 */
export interface MyFamilyGroup {
  familyGroupId: string
  address: string
  defaultMeetingDayOfWeek: number
  isHost: boolean
  isLeader: boolean
  hostFirstName: string
  hostLastName: string
  /** "YYYY-MM-DD" del último reporte, para no levantar dos veces el mismo. */
  lastMeetingDate: string | null
  members: GroupMemberOption[]
}

/**
 * Persona del directorio al buscar a quién sumar. `isAvailable` en false significa
 * exclusivamente "ya pertenece a algún grupo": el contrato NO tiene dónde decir a
 * cuál, y es a propósito (regla 7.5).
 */
export interface AvailablePerson {
  personId: string
  firstName: string
  lastName: string
  isAvailable: boolean
}

export interface FamilyGroupAttendanceInput {
  personId: string
  wasPresent: boolean
}

/** Fila de la lista general del Pastor. */
export interface FamilyGroupSummary {
  familyGroupId: string
  address: string
  defaultMeetingDayOfWeek: number
  isActive: boolean
  hostFirstName: string
  hostLastName: string
  leaderFirstName: string
  leaderLastName: string
  activeMemberCount: number
  lastMeetingDate: string | null
}

export interface FamilyGroupMemberDetail {
  personId: string
  firstName: string
  lastName: string
  joinedAt: string
}

export interface FamilyGroupMeetingSummary {
  meetingId: string
  meetingDate: string
  totalOffering: number
  presentCount: number
  memberCount: number
}

export interface FamilyGroupDetail {
  familyGroupId: string
  address: string
  defaultMeetingDayOfWeek: number
  isActive: boolean
  hostPersonId: string
  hostFirstName: string
  hostLastName: string
  leaderPersonId: string
  leaderFirstName: string
  leaderLastName: string
  members: FamilyGroupMemberDetail[]
  recentMeetings: FamilyGroupMeetingSummary[]
}

// ── Administración de una persona ──────────────────────────────────────────
/**
 * Espejo de `PersonAdministrationResponse`.
 *
 * Los roles y los cargos llegan como CATÁLOGO COMPLETO con una marca de cuáles
 * tiene, no como la lista de los que tiene. Esa diferencia es la que permite
 * dibujar los interruptores sin escribir un solo nombre de rol en el frontend:
 * si mañana el backend siembra otro rol, aparece aquí solo.
 */
export interface AdminRole {
  name: string
  description: string
  granted: boolean
}

export interface AdminPosition {
  positionId: string
  name: string
  isExecutiveBody: boolean
  held: boolean
}

/**
 * Un ministerio del catálogo, con su liderazgo actual. Llegan TODOS, no solo los
 * que la persona lidera: para poder ponerla al frente de uno hay que listarlos.
 */
export interface AdminMinistry {
  ministryId: string
  name: string
  ledByThisPerson: boolean
  /** Quién lo lidera hoy. Evita el 409 sin explicación al asignar. */
  currentLeaderName: string | null
}

/**
 * Una sociedad, con los DOS vínculos posibles: liderarla y pertenecer a ella.
 * Son independientes — el líder no es necesariamente integrante.
 */
export interface AdminSociety {
  societyId: string
  name: string
  ledByThisPerson: boolean
  currentLeaderName: string | null
  isMember: boolean
  /** Quitar a alguien del grupo va contra ESTE Id, no contra el de la sociedad. */
  societyMembershipId: string | null
}

/** La casa a la que asiste, con quién la lleva. Nulo si no va a ninguna. */
export interface AdminFamilyGroup {
  familyGroupId: string
  address: string
  joinedAt: string
  isHost: boolean
  isLeader: boolean
  hostName: string
  leaderName: string
}

/**
 * Una marca de asistencia. Mezcla cultos y Grupos Familiares a propósito:
 * mirar media vida daría una respuesta falsa a "¿qué tan constante es?".
 */
export interface AdminAttendance {
  date: string
  wasPresent: boolean
  source: string
}

export interface PersonAdministration {
  personId: string
  firstName: string
  lastName: string
  /** Los tres campos editables de la ficha. */
  dateOfBirth: string | null
  phoneNumber: string | null
  isActive: boolean
  userAccountId: string | null
  email: string | null
  hasAccount: boolean
  accountIsActive: boolean
  isOfficialMember: boolean
  /** Si existe la fila, aunque esté dada de baja. Distingue "nunca fue" de "ya no es". */
  hasMembershipRecord: boolean
  /** "YYYY-MM-DD" */
  memberSince: string | null
  roles: AdminRole[]
  positions: AdminPosition[]
  ministries: AdminMinistry[]
  societies: AdminSociety[]
  familyGroup: AdminFamilyGroup | null
  /** De la más antigua a la más reciente, lista para dibujarse en una fila. */
  recentAttendance: AdminAttendance[]
}
