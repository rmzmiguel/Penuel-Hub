import { createContext, use, useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { getSession, onSessionChange } from '../api/client'
import { auth, me } from '../api/endpoints'
import type { AuthSession, MyCapabilities } from '../api/types'
import { PositionNames, RoleNames } from '../api/types'

interface AuthValue {
  session: AuthSession | null
  capabilities: MyCapabilities | null
  loadingCapabilities: boolean
  signIn: (email: string, password: string) => Promise<void>
  signOut: () => void
  /** Vuelve a preguntar al backend qué puede hacer esta persona hoy. */
  refreshCapabilities: () => void
}

const AuthContext = createContext<AuthValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSessionState] = useState(getSession)
  const [capabilities, setCapabilities] = useState<MyCapabilities | null>(null)
  const [loadingCapabilities, setLoading] = useState(false)
  const [nonce, setNonce] = useState(0)

  useEffect(() => onSessionChange(setSessionState), [])

  // La navegación NUNCA se arma de una lista fija: se pregunta al backend qué
  // puede hacer esta persona HOY. Si el Pastor le reasigna un liderazgo, se
  // refleja la próxima vez que abra la app, sin cambiar una línea de código.
  useEffect(() => {
    if (!session) {
      setCapabilities(null)
      return
    }
    const controller = new AbortController()
    setLoading(true)
    me.capabilities(controller.signal)
      .then((c) => setCapabilities(c))
      .catch(() => setCapabilities(null))
      .finally(() => setLoading(false))
    return () => controller.abort()
  }, [session, nonce])

  const signIn = useCallback(async (email: string, password: string) => {
    await auth.login(email, password)
  }, [])

  const signOut = useCallback(() => {
    auth.logout()
    setCapabilities(null)
  }, [])

  const value = useMemo<AuthValue>(
    () => ({
      session,
      capabilities,
      loadingCapabilities,
      signIn,
      signOut,
      refreshCapabilities: () => setNonce((n) => n + 1),
    }),
    [session, capabilities, loadingCapabilities, signIn, signOut],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}

export function useAuth() {
  const value = use(AuthContext)
  if (!value) throw new Error('useAuth debe usarse dentro de <AuthProvider>')
  return value
}

/**
 * Lo que esta persona puede hacer, derivado de las capacidades reales.
 * Es la única fuente de la navegación: ninguna pantalla asume quién es quién.
 */
export function usePermissions() {
  const { capabilities } = useAuth()

  return useMemo(() => {
    const roles = capabilities?.roles ?? []
    const positions = capabilities?.positions ?? []

    /*
     * El superusuario no "es Pastor": SALTA la comprobación, exactamente igual
     * que en `AuthorizationBehavior` del backend. Va aquí arriba y no sumado a
     * cada bandera porque de lo contrario habría que acordarse de incluirlo en
     * cada capacidad nueva — y el día que a alguien se le olvidara, el rol
     * quedaría con permiso en el servidor y sin pantallas donde ejercerlo.
     */
    const esSuperusuario = roles.includes(RoleNames.Developer)

    const isTreasurer = positions.some((p) => p.name === PositionNames.TesoreroGeneral)

    if (esSuperusuario) {
      return {
        canAdminister: true,
        isTreasurer,
        canCaptureSundaySchool: true,
        canCaptureServices: true,
        canSeeHistory: true,
      }
    }

    /*
     * `canAdminister` y no `isPastor`: en ninguno de sus usos hacía falta saber
     * si alguien ES el Pastor, sino si puede administrar. Nombrarlo por el rol
     * obligaba a mentir —marcar a un desarrollador como Pastor— para dejarle
     * ver las mismas pantallas.
     */
    const esPastor = roles.includes(RoleNames.Pastor)

    return {
      canAdminister: esPastor,
      isTreasurer,
      canCaptureSundaySchool: esPastor || roles.includes(RoleNames.SundaySchoolRecorder),
      canCaptureServices: esPastor || isTreasurer,
      canSeeHistory: esPastor || isTreasurer || roles.includes(RoleNames.SundaySchoolRecorder),
    }
  }, [capabilities])
}
