import { handleDemo, isDemoSession } from '../demo/backend'
import type { ApiProblem, AuthSession } from './types'

const STORAGE_KEY = 'penuel.session'

/** Error de API con el código estable del backend, no solo un mensaje suelto. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string,
    message: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }

  /** 401 — no hay sesión, o dejó de valer. Manda a iniciar sesión. */
  get isUnauthenticated() {
    return this.status === 401
  }

  /** 403 — hay sesión pero no alcanza. NO tiene caso volver a entrar. */
  get isForbidden() {
    return this.status === 403
  }
}

// ── Sesión persistida ──────────────────────────────────────────────────────
let session: AuthSession | null = readStoredSession()
const listeners = new Set<(s: AuthSession | null) => void>()

function readStoredSession(): AuthSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as AuthSession) : null
  } catch {
    return null
  }
}

export function getSession() {
  return session
}

export function setSession(next: AuthSession | null) {
  session = next
  try {
    if (next) localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    else localStorage.removeItem(STORAGE_KEY)
  } catch {
    /* Modo privado o almacenamiento bloqueado: la sesión vive solo en memoria. */
  }
  listeners.forEach((l) => l(next))
}

export function onSessionChange(listener: (s: AuthSession | null) => void) {
  listeners.add(listener)
  // Devuelve void, no boolean: es la función de limpieza de un useEffect.
  return () => {
    listeners.delete(listener)
  }
}

// ── Renovación ─────────────────────────────────────────────────────────────
/**
 * Una sola renovación en vuelo a la vez. Sin esto, tres peticiones que caducan
 * juntas dispararían tres refresh; el segundo presentaría un token ya rotado y
 * el backend lo leería como REUSO, cerrando todas las sesiones del usuario.
 */
/**
 * Base de la API. Vacía en desarrollo: el proxy de Vite sirve /api desde el mismo
 * origen. En producción, `VITE_API_URL` apunta al backend desplegado.
 */
const API = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')

/** Une la base con la ruta relativa que usan todas las llamadas. */
export const apiUrl = (path: string) => `${API}${path}`

let refreshing: Promise<boolean> | null = null

async function refreshSession(): Promise<boolean> {
  if (!session?.refreshToken) return false

  refreshing ??= (async () => {
    try {
      const res = await fetch(apiUrl('/api/auth/refresh'), {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: session!.refreshToken }),
      })
      if (!res.ok) {
        setSession(null)
        return false
      }
      setSession((await res.json()) as AuthSession)
      return true
    } catch {
      return false
    } finally {
      refreshing = null
    }
  })()

  return refreshing
}

// ── Petición ───────────────────────────────────────────────────────────────
interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
  /** Interno: evita reintentar en bucle tras una renovación. */
  retried?: boolean
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, signal, retried = false } = options

  /*
   * Modo demostración. Solo se cumple si la sesión activa se creó desde el
   * botón de demostración de la pantalla de acceso; una sesión real nunca
   * entra aquí. Sirve para enseñar la interfaz completa sin levantar la API.
   */
  if (isDemoSession(session)) {
    const handled = handleDemo(path, method, body as Record<string, unknown> | undefined)
    if (handled) {
      try {
        return (await handled) as T
      } catch (e) {
        throw new ApiError(501, 'Demo.NotImplemented', (e as Error).message)
      }
    }
  }

  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (session?.accessToken) headers.Authorization = `Bearer ${session.accessToken}`

  let res: Response
  try {
    res = await fetch(apiUrl(path), {
      method,
      headers,
      signal,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, 'Network.Unreachable', 'No se pudo conectar. Revisa tu conexión.')
  }

  // El backend revalida los roles en CADA petición, así que un 401 puede
  // aparecer en cualquier momento: no solo al expirar el token.
  if (res.status === 401 && !retried && session?.refreshToken) {
    if (await refreshSession()) {
      return request<T>(path, { ...options, retried: true })
    }
    setSession(null)
  }

  if (res.status === 204) return undefined as T

  const text = await res.text()

  /*
   * El cuerpo puede NO ser JSON. Cuando la API está apagada, el proxy de Vite
   * responde 500 con cuerpo vacío; un servidor intermedio puede devolver HTML.
   * Antes esto reventaba con un SyntaxError suelto, o caía en un "Ocurrió un
   * error inesperado" que no le decía nada a nadie.
   */
  let payload: unknown = null
  let parseable = true
  if (text) {
    try {
      payload = JSON.parse(text)
    } catch {
      parseable = false
    }
  }

  if (!res.ok) {
    const problem = (payload ?? {}) as ApiProblem

    // Sin un ProblemDetails reconocible, el error no viene de la aplicación:
    // viene de que nadie contestó al otro lado. Decirlo así ahorra horas.
    const sinRespuestaDeLaApi = !parseable || (!problem.code && !problem.detail && !problem.title)

    if (sinRespuestaDeLaApi && res.status >= 500) {
      throw new ApiError(
        res.status,
        'Server.Unreachable',
        'No se pudo conectar con el servidor de Penuel. Inténtalo de nuevo en un momento.' +
          (import.meta.env.DEV
            ? ' En desarrollo esto casi siempre significa que la API .NET no está encendida: ejecuta dotnet run en backend/Penuel.WebApi.'
            : ''),
      )
    }

    throw new ApiError(
      res.status,
      problem.code ?? 'Unknown',
      problem.detail ?? problem.title ?? 'Ocurrió un error inesperado.',
    )
  }

  return payload as T
}

export const api = {
  get: <T>(path: string, signal?: AbortSignal) => request<T>(path, { signal }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
