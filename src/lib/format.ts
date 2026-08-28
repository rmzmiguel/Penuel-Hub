/** Formato de dinero de la iglesia: pesos mexicanos, siempre con dos decimales. */
const money = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
  minimumFractionDigits: 2,
})

export const formatMoney = (value: number) => money.format(value)

const longDate = new Intl.DateTimeFormat('es-MX', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
})

const shortDate = new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'long' })
/** "23 ago" — para filas estrechas donde "23 de agosto" empuja al truncado. */
const compactDate = new Intl.DateTimeFormat('es-MX', { day: 'numeric', month: 'short' })

/** Recibe "YYYY-MM-DD" y lo formatea SIN pasar por UTC, que correría el día. */
function fromIsoDate(iso: string) {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(y, m - 1, d)
}

export const formatLongDate = (iso: string) => longDate.format(fromIsoDate(iso))
export const formatShortDate = (iso: string) => shortDate.format(fromIsoDate(iso))
export const formatCompactDate = (iso: string) => compactDate.format(fromIsoDate(iso)).replace('.', '')

export function capitalize(text: string) {
  return text.charAt(0).toUpperCase() + text.slice(1)
}

/** Fecha de hoy como "YYYY-MM-DD" en hora local, no en UTC. */
export function todayIso() {
  const now = new Date()
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}

export const fullName = (p: { firstName: string; lastName: string }) =>
  `${p.firstName} ${p.lastName}`

/** Iniciales para los avatares de la lista de asistencia. */
export function initials(p: { firstName: string; lastName: string }) {
  return `${p.firstName.charAt(0)}${p.lastName.charAt(0)}`.toUpperCase()
}

/**
 * El domingo más reciente (hoy mismo si hoy es domingo), como "YYYY-MM-DD".
 * Es la fecha que casi siempre quiere quien captura Escuela Dominical.
 */
export function lastSundayIso() {
  const now = new Date()
  now.setDate(now.getDate() - now.getDay())
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}
