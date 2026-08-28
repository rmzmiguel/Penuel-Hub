import type { ServiceSessionSummary } from '../api/types'
import type { Point } from '../components/ui/Charts'

/**
 * Métricas del tablero, derivadas del historial que YA devuelve el backend.
 *
 * No hay ningún endpoint de agregados y a propósito: `GET /api/service-sessions`
 * trae fecha, tipo, ofrenda, diezmo y asistencia de cada sesión, que es todo lo
 * que hace falta. Calcularlo aquí evita inventar una ruta nueva y —más
 * importante— evita que el tablero y el historial puedan contradecirse, porque
 * leen exactamente la misma lista.
 *
 * El alcance ya viene resuelto: el backend solo devuelve las sesiones que a
 * cada quien le toca ver. Quien únicamente captura Escuela Dominical verá aquí
 * sus grupos, no los cultos generales, sin una sola condición en el cliente.
 */

export type WindowKey = '3m' | '6m' | '12m'

export const WINDOWS: { value: WindowKey; label: string; months: number }[] = [
  { value: '3m', label: '3 meses', months: 3 },
  { value: '6m', label: '6 meses', months: 6 },
  { value: '12m', label: '1 año', months: 12 },
]

const MONTHS_SHORT = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic']

/** "YYYY-MM-DD" a fecha local. Pasar por UTC correría el día entero. */
export function fromIso(iso: string) {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

const monthKey = (d: Date) => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`

/** Primer día del mes que está `back` meses atrás del actual. */
function monthsAgo(back: number) {
  const d = new Date()
  d.setDate(1)
  d.setHours(0, 0, 0, 0)
  d.setMonth(d.getMonth() - back)
  return d
}

export interface Dashboard {
  /** Ofrenda + diezmo de la ventana. Es "lo que entró", que es como se piensa. */
  income: number
  offering: number
  tithe: number
  /** Variación contra la ventana anterior del mismo tamaño. */
  delta: { value: string; direction: 'up' | 'down' | 'flat' }
  /** Serie mensual para la gráfica de área. */
  series: Point[]
  attendance: { label: string; value: number }[]
  attendanceTotal: number
  /**
   * Cuántas sesiones de la ventana registraron asistencia. NO es lo mismo que
   * `sessionCount`: el reporte de culto no toma lista, así que promediar contra
   * el total de sesiones reparte la asistencia entre sesiones que nunca la
   * midieron.
   */
  attendanceSessions: number
  sessionCount: number
  recent: ServiceSessionSummary[]
  /** Un domingo por celda, de los últimos seis meses. */
  consistency: { level: 0 | 1 | 2 | 3; title: string }[]
}

export function buildDashboard(
  sessions: ServiceSessionSummary[],
  months: number,
): Dashboard {
  const from = monthsAgo(months - 1)
  const prevFrom = monthsAgo(months * 2 - 1)

  const inWindow = sessions.filter((s) => fromIso(s.sessionDate) >= from)
  const inPrev = sessions.filter((s) => {
    const d = fromIso(s.sessionDate)
    return d >= prevFrom && d < from
  })

  const sum = (list: ServiceSessionSummary[]) =>
    list.reduce((t, s) => t + s.totalOffering + (s.totalTithe ?? 0), 0)

  const income = sum(inWindow)
  const previous = sum(inPrev)

  // Sin periodo anterior con qué comparar, no se inventa un 0% ni un +100%:
  // se marca como plano y la pastilla lo dice.
  const ratio = previous > 0 ? (income - previous) / previous : 0
  const delta = {
    value: previous > 0 ? `${ratio > 0 ? '+' : ''}${(ratio * 100).toFixed(1)}%` : 'Sin comparación',
    direction: previous === 0 ? 'flat' : ratio > 0.005 ? 'up' : ratio < -0.005 ? 'down' : 'flat',
  } as const

  // Serie mensual: se siembran TODOS los meses de la ventana antes de sumar,
  // para que un mes sin sesiones se dibuje como cero y no desaparezca del eje.
  const buckets = new Map<string, number>()
  for (let i = months - 1; i >= 0; i--) buckets.set(monthKey(monthsAgo(i)), 0)
  for (const s of inWindow) {
    const k = monthKey(fromIso(s.sessionDate))
    if (buckets.has(k)) buckets.set(k, buckets.get(k)! + s.totalOffering + (s.totalTithe ?? 0))
  }

  const series: Point[] = [...buckets].map(([k, value]) => ({
    label: MONTHS_SHORT[Number(k.slice(5)) - 1],
    value,
  }))

  /*
   * Asistencia: solo las sesiones que la REGISTRARON.
   *
   * `presentCount` sale de contar filas en `ServiceAttendances`, y el reporte de
   * culto —General, de Oración, de Jóvenes— no escribe ninguna: su DTO
   * (`GeneralServiceReport`) ni siquiera tiene campo de asistencias. O sea que
   * para esos cultos el cero no significa "no vino nadie", significa "no se
   * preguntó". Meterlos en el promedio lo dividía entre TODAS las sesiones y lo
   * dejaba en la cuarta parte de lo real.
   *
   * Queda una ambigüedad que desde aquí no se puede resolver: una sesión de
   * Escuela Dominical donde se pasó lista y se marcó ausente a todo el mundo
   * también daría cero. En la práctica no ocurre —nadie levanta el reporte de un
   * grupo al que no fue nadie— y el precio de equivocarse es no enseñar un dato,
   * no enseñarlo mal.
   */
  const withAttendance = inWindow.filter((s) => s.presentCount > 0)

  /*
   * Se agrupa por SOCIEDAD cuando la hay, y por tipo de culto cuando no.
   *
   * Como en la práctica solo Escuela Dominical toma lista, agrupar por tipo de
   * culto dejaba la gráfica con una sola porción —un anillo completo que no
   * compara nada—. La sociedad es lo que de verdad varía ahí: Damas,
   * Caballeros, Jóvenes, Infantil. Si algún día un culto general registra
   * asistencia, entra en el reparto con su propio nombre y la gráfica sigue
   * diciendo lo mismo.
   */
  const byGroup = new Map<string, number>()
  for (const s of withAttendance) {
    const label = s.societyName ?? s.serviceTypeName
    byGroup.set(label, (byGroup.get(label) ?? 0) + s.presentCount)
  }
  const attendance = [...byGroup]
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)

  return {
    income,
    offering: inWindow.reduce((t, s) => t + s.totalOffering, 0),
    tithe: inWindow.reduce((t, s) => t + (s.totalTithe ?? 0), 0),
    delta,
    series,
    attendance,
    attendanceTotal: attendance.reduce((t, a) => t + a.value, 0),
    /** El divisor correcto de cualquier promedio de asistencia. */
    attendanceSessions: withAttendance.length,
    sessionCount: inWindow.length,
    recent: sessions.slice(0, 6),
    consistency: consistency(sessions),
  }
}

/**
 * Constancia: un cuadro por domingo de los últimos seis meses. El nivel es
 * cuántas sesiones se levantaron ese día, no cuánto dinero entró — la pregunta
 * que responde es "¿se está registrando con disciplina?".
 */
function consistency(sessions: ServiceSessionSummary[]) {
  const counts = new Map<string, number>()
  for (const s of sessions) counts.set(s.sessionDate, (counts.get(s.sessionDate) ?? 0) + 1)

  const cells: { level: 0 | 1 | 2 | 3; title: string }[] = []
  const sunday = new Date()
  sunday.setHours(0, 0, 0, 0)
  sunday.setDate(sunday.getDate() - sunday.getDay())

  for (let w = 25; w >= 0; w--) {
    const day = new Date(sunday)
    day.setDate(day.getDate() - w * 7)
    const iso = `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, '0')}-${String(day.getDate()).padStart(2, '0')}`
    const n = counts.get(iso) ?? 0
    const label = `${day.getDate()} de ${MONTHS_SHORT[day.getMonth()].toLowerCase()}`
    cells.push({
      level: n === 0 ? 0 : n === 1 ? 1 : n <= 3 ? 2 : 3,
      title: n === 0 ? `${label}: sin reportes` : `${label}: ${n} ${n === 1 ? 'reporte' : 'reportes'}`,
    })
  }
  return cells
}

/** Dinero en corto para etiquetas: $68.4k. Los totales nunca se abrevian. */
export function shortMoney(value: number) {
  if (Math.abs(value) >= 1_000_000) return `$${(value / 1_000_000).toFixed(1)}M`
  if (Math.abs(value) >= 1_000) return `$${(value / 1_000).toFixed(1)}k`
  return `$${Math.round(value)}`
}
