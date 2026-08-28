import type {
  CaptureContext,
  MyCapabilities,
  ServiceSessionSummary,
  ServiceTypeOption,
} from '../api/types'

/**
 * Datos de demostración.
 *
 * Existen para UNA cosa: poder ver y juzgar la interfaz sin levantar la API.
 * Nada de aquí llega a producción — `demoBackend` solo se activa cuando la
 * persona entra por el botón de demostración de la pantalla de acceso.
 *
 * Los números están calculados, no inventados al azar: las ofrendas siguen una
 * curva con estacionalidad real (diciembre alto, enero bajo) y la asistencia
 * baja en verano. Una interfaz probada contra datos planos miente sobre cómo se
 * va a ver el día que tenga datos de verdad.
 */

/** Generador congruente lineal: mismos datos en cada carga, sin depender de Math.random. */
function seeded(seed: number) {
  let s = seed >>> 0
  return () => ((s = (s * 1664525 + 1013904223) >>> 0) / 4294967296)
}
const rnd = seeded(20260828)

const NAMES: [string, string][] = [
  ['Rubén', 'Ramírez Solís'],     ['Esperanza', 'Solís de Ramírez'],
  ['Miguel', 'Ramírez Solís'],     ['Guadalupe', 'Treviño Cantú'],
  ['José Luis', 'Hernández Mata'], ['Martha', 'Zúñiga Lara'],
  ['Fernando', 'Cavazos Rangel'],  ['Rosa María', 'Ibarra Ponce'],
  ['Ernesto', 'Villarreal Gómez'], ['Alicia', 'Maldonado Ruiz'],
  ['Salvador', 'Reyna Ochoa'],     ['Norma', 'Castillo Prieto'],
  ['Ismael', 'Gallegos Durán'],    ['Beatriz', 'Nava Escobedo'],
  ['Arturo', 'Peña Sandoval'],     ['Verónica', 'Lozano Fuentes'],
  ['Gerardo', 'Cisneros Ávila'],   ['Leticia', 'Montoya Bravo'],
  ['Homero', 'Barrientos Lugo'],   ['Sandra', 'Quiroz Medellín'],
  ['Efraín', 'Delgado Camacho'],   ['Carmen', 'Salinas Arriaga'],
  ['Jonathan', 'Rocha Estrada'],   ['Dulce', 'Aguilar Berrones'],
  ['Abraham', 'Tovar Guevara'],    ['Silvia', 'Espinoza Farías'],
]

export const MINISTRIES = [
  { id: 'm-1', name: 'Alabanza y Adoración' },
  { id: 'm-2', name: 'Intercesión' },
  { id: 'm-3', name: 'Evangelismo' },
  { id: 'm-4', name: 'Misiones' },
  { id: 'm-5', name: 'Medios y Sonido' },
  { id: 'm-6', name: 'Acción Social' },
]

/** Los mismos cuatro nombres que siembra la migración real del backend. */
export const SOCIETIES = [
  { id: 's-1', name: 'Damas' },
  { id: 's-2', name: 'Varones' },
  { id: 's-3', name: 'Jóvenes' },
  { id: 's-4', name: 'Infantil' },
]

/**
 * Reparto de la congregación de demostración entre los cuatro grupos.
 *
 * Explícito y no por módulo: NAMES alterna varón/mujer, y un reparto ciego
 * acababa poniendo a Rubén y a José Luis en la Sociedad de Damas. En una
 * demostración que se le enseña a la iglesia, ese detalle se nota.
 */
export const SOCIETY_MEMBER_INDEXES: Record<string, number[]> = {
  's-1': [1, 3, 5, 7, 9, 11],        // Damas    — índices impares
  's-2': [0, 2, 4, 6, 8, 10],        // Varones  — índices pares
  's-3': [12, 13, 14, 15, 16, 17],   // Jóvenes  — mixto
  's-4': [18, 19, 20, 21],           // Infantil — mixto
}

/**
 * Catálogo de roles, con la MISMA descripción que siembra el backend real.
 * La pantalla de permisos las muestra tal cual: si aquí dijeran otra cosa, la
 * demostración estaría enseñando permisos que no existen.
 */
export const ROLES = [
  {
    name: 'Desarrollador',
    description:
      'Acceso irrestricto para el mantenimiento técnico del sistema. ' +
      'No es un cargo de la iglesia y no implica membresía ni liderazgo.',
  },
  {
    name: 'Pastor',
    description:
      'Control total del sistema: gestiona personas, membresías, roles, ' +
      'ministerios, sociedades y cargos.',
  },
  {
    name: 'SundaySchoolRecorder',
    description:
      'Puede levantar y corregir los reportes de Escuela Dominical de ' +
      'cualquier grupo. No implica ser maestro de ninguno.',
  },
]

/** Los mismos cuatro cargos que siembra la migración real del backend. */
export const POSITIONS = [
  { id: 'c-1', name: 'Pastor' },
  { id: 'c-2', name: 'Diácono' },
  { id: 'c-3', name: 'Secretario General' },
  { id: 'c-4', name: 'Tesorero General' },
]

/** Quién tiene qué. Todo lo demás se deriva de esta tabla. */


/**
 * Personas de la demostración.
 *
 * La forma es EXACTAMENTE la que devuelve `GET /api/persons` del backend real
 * —id, nombre y apellido— más el estado, que la simulación necesita para que
 * dar de baja a alguien se vea. Nada más: si la demostración inventa campos que
 * la API no da, la interfaz se construye contra una fantasía. Ya pasó una vez.
 */
export interface DemoPerson {
  id: string
  firstName: string
  lastName: string
  status: 0 | 1
}

export const PEOPLE: DemoPerson[] = NAMES.map(([firstName, lastName], i) => ({
  id: `demo-p${i}`,
  firstName,
  lastName,
  status: 0 as const,
}))

export const CAPABILITIES: MyCapabilities = {
  personId: PEOPLE[0].id,
  firstName: PEOPLE[0].firstName,
  lastName: PEOPLE[0].lastName,
  email: 'demo@penuel.mx',
  isOfficialMember: true,
  isExecutiveBodyMember: true,
  roles: ['Pastor'],
  positions: [POSITIONS[0]],
  ledMinistries: [],
  ledSocieties: [],
}


/** Catálogo de tipos de culto, con las banderas que cambian la captura. */
export const SERVICE_TYPES: ServiceTypeOption[] = [
  { id: 't-1', name: 'Culto General', requiresSocietyGrouping: false, collectsTithe: true, attendanceCustomary: true },
  { id: 't-2', name: 'Culto de Oración', requiresSocietyGrouping: false, collectsTithe: false, attendanceCustomary: true },
  { id: 't-3', name: 'Culto de Jóvenes', requiresSocietyGrouping: false, collectsTithe: false, attendanceCustomary: true },
  { id: 't-4', name: 'Escuela Dominical', requiresSocietyGrouping: true, collectsTithe: false, attendanceCustomary: true },
]

/**
 * Contexto de captura del Pastor: no tiene grupo fijo, así que puede levantar
 * el reporte de cualquiera. Es el modo 2 (`NoFixedGroup`) del backend.
 */
export const CAPTURE_CONTEXT: CaptureContext = {
  personId: PEOPLE[0].id,
  mode: 2,
  isFloatingSubstitute: true,
  mySocieties: [],
  allSocieties: SOCIETIES.map((s, i) => ({
    societyId: s.id,
    societyName: s.name,
    teacherCandidates: [
      { personId: PEOPLE[10 + i].id, firstName: PEOPLE[10 + i].firstName, lastName: PEOPLE[10 + i].lastName, hasFixedGroup: true },
      { personId: PEOPLE[0].id, firstName: PEOPLE[0].firstName, lastName: PEOPLE[0].lastName, hasFixedGroup: false },
    ],
  })),
}

/* ── Sesiones ───────────────────────────────────────────────────────────── */

const iso = (d: Date) => {
  const local = new Date(d.getTime() - d.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}

/** Estacionalidad real: diciembre arriba, enero abajo, verano flojo. */
const SEASON = [0.82, 0.88, 0.97, 1.02, 1.0, 0.93, 0.86, 0.9, 1.04, 1.06, 1.1, 1.34]

function buildSessions(): ServiceSessionSummary[] {
  const out: ServiceSessionSummary[] = []
  const today = new Date()
  const recorders = [PEOPLE[2], PEOPLE[3], PEOPLE[11], PEOPLE[0]]
  let n = 0

  // Semanas hacia atrás desde el domingo más reciente. Son 80 y no 30 porque
  // el tablero compara contra el periodo anterior: con año y medio de historia,
  // la ventana de "1 año" tiene con qué compararse.
  const sunday = new Date(today)
  sunday.setDate(sunday.getDate() - sunday.getDay())

  for (let w = 79; w >= 0; w--) {
    const day = new Date(sunday)
    day.setDate(day.getDate() - w * 7)
    const s = SEASON[day.getMonth()]
    const jitter = () => 0.86 + rnd() * 0.3

    // Culto General de cada domingo, con diezmo identificado.
    const offering = Math.round(3200 * s * jitter())
    const tithe = Math.round(6400 * s * jitter())
    out.push({
      sessionId: `ss-${++n}`,
      sessionDate: iso(day),
      serviceTypeName: 'Culto General',
      societyId: null,
      societyName: null,
      totalOffering: offering,
      totalTithe: tithe,
      teacherName: null,
      preacherName: `${PEOPLE[0].firstName} ${PEOPLE[0].lastName}`,
      recordedByName: `${PEOPLE[4].firstName} ${PEOPLE[4].lastName}`,
      // Cero a propósito: el reporte de culto no toma lista. Su DTO no tiene
      // campo de asistencias, así que el backend real tampoco puede devolver
      // otra cosa. La demostración enseña lo que se va a ver de verdad.
      presentCount: 0,
    })

    // Escuela Dominical: dos o tres grupos por domingo.
    const groups = SOCIETIES.slice(0, 2 + (w % 3 === 0 ? 1 : 0))
    for (const g of groups) {
      const r = recorders[(w + groups.indexOf(g)) % recorders.length]
      out.push({
        sessionId: `ss-${++n}`,
        sessionDate: iso(day),
        serviceTypeName: 'Escuela Dominical',
        societyId: g.id,
        societyName: g.name,
        totalOffering: Math.round(420 * s * jitter()),
        totalTithe: null,
        teacherName: `${PEOPLE[10].firstName} ${PEOPLE[10].lastName}`,
        preacherName: null,
        recordedByName: `${r.firstName} ${r.lastName}`,
        presentCount: Math.round(22 * s * jitter()),
      })
    }

    // Culto de Oración entre semana (miércoles).
    if (w % 1 === 0) {
      const wed = new Date(day)
      wed.setDate(wed.getDate() + 3)
      if (wed <= today) {
        out.push({
          sessionId: `ss-${++n}`,
          sessionDate: iso(wed),
          serviceTypeName: 'Culto de Oración',
          societyId: null,
          societyName: null,
          totalOffering: Math.round(880 * s * jitter()),
          totalTithe: null,
          teacherName: null,
          preacherName: `${PEOPLE[6].firstName} ${PEOPLE[6].lastName}`,
          recordedByName: `${PEOPLE[4].firstName} ${PEOPLE[4].lastName}`,
          presentCount: 0,
        })
      }
    }

    // Culto de Jóvenes cada dos sábados.
    if (w % 2 === 0) {
      const sat = new Date(day)
      sat.setDate(sat.getDate() + 6)
      if (sat <= today) {
        out.push({
          sessionId: `ss-${++n}`,
          sessionDate: iso(sat),
          serviceTypeName: 'Culto de Jóvenes',
          societyId: null,
          societyName: null,
          totalOffering: Math.round(640 * s * jitter()),
          totalTithe: null,
          teacherName: null,
          preacherName: `${PEOPLE[22].firstName} ${PEOPLE[22].lastName}`,
          recordedByName: `${PEOPLE[7].firstName} ${PEOPLE[7].lastName}`,
          presentCount: 0,
        })
      }
    }
  }

  return out.sort((a, b) => b.sessionDate.localeCompare(a.sessionDate))
}

export const SESSIONS = buildSessions()
