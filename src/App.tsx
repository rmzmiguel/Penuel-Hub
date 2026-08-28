import { Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { familyGroups } from './api/familyGroups'
import { AuthProvider, useAuth, usePermissions } from './auth/AuthProvider'
import { AppShell } from './components/AppShell'
import { Loading } from './components/Feedback'
import { ToastProvider } from './components/ui/Toast'
import { useAsync } from './lib/useAsync'
import { DashboardScreen } from './screens/DashboardScreen'
import { HistoryScreen } from './screens/HistoryScreen'
import { LoginScreen } from './screens/LoginScreen'
import { PeopleScreen } from './screens/admin/PeopleScreen'
import { FamilyGroupsScreen } from './screens/admin/FamilyGroupsScreen'
import { FamilyGroupRoot } from './screens/familyGroups/FamilyGroupApp'
import { GeneralServiceFlow } from './screens/generalService/GeneralServiceFlow'
import { SundaySchoolFlow } from './screens/sundaySchool/SundaySchoolFlow'

export function App() {
  return (
    <AuthProvider>
      <ToastProvider>
        <Shell />
      </ToastProvider>
    </AuthProvider>
  )
}

function Shell() {
  const { session, capabilities, loadingCapabilities } = useAuth()

  if (!session) return <LoginScreen />

  // Se espera a saber qué puede hacer esta persona ANTES de dibujar nada: si no,
  // la navegación parpadearía con las entradas equivocadas durante un instante.
  if (loadingCapabilities && !capabilities) {
    return <Loading label="Preparando tu inicio…" />
  }

  return <Enrutado />
}

/**
 * Decide QUÉ APLICACIÓN es esta.
 *
 * No son dos vistas de lo mismo: quien solo lleva una casa recibe una aplicación
 * distinta —sin dock, sin barra lateral, sin ficha de cargos vacía— porque para esa
 * persona no existe nada más (Sección 2.1 de la rama). Una versión "simplificada"
 * de la aplicación del Pastor seguiría enseñando los huecos de lo que no puede
 * hacer, que es justo lo que hay que evitar.
 */
function Enrutado() {
  const perms = usePermissions()

  // Con cualquier permiso de sistema, la aplicación es la de siempre y no hace
  // falta preguntar por grupos para decidirlo.
  const tieneAlgoMas =
    perms.canAdminister ||
    perms.canCaptureSundaySchool ||
    perms.canCaptureServices ||
    perms.canSeeHistory

  const mios = useAsync(
    (signal) => (tieneAlgoMas ? Promise.resolve([]) : familyGroups.mine(signal)),
    [tieneAlgoMas],
  )

  if (!tieneAlgoMas) {
    if (mios.loading && !mios.data) return <Loading label="Preparando tu inicio…" />
    // Si falla la consulta se sigue de largo a la aplicación normal en vez de
    // bloquear: quedarse en un error sin salida sería peor que un tablero vacío.
    if ((mios.data?.length ?? 0) > 0) return <FamilyGroupRoot />
  }

  return (
    <Routes>
      {/*
       * Los flujos de captura viven FUERA del armazón: son de un solo objetivo,
       * y una barra de navegación al lado invita a abandonar un reporte a
       * medias. Todo lo demás —tablero, personas, historial— vive dentro.
       */}
      <Route
        path="/escuela-dominical"
        element={<Guard allow="canCaptureSundaySchool" element={<SundaySchoolFlow />} />}
      />
      <Route
        path="/culto"
        element={<Guard allow="canCaptureServices" element={<GeneralServiceFlow />} />}
      />

      <Route element={<Framed />}>
        <Route path="/" element={<DashboardScreen />} />
        <Route path="/personas" element={<Guard allow="canAdminister" element={<PeopleScreen />} />} />
        <Route path="/grupos" element={<Guard allow="canAdminister" element={<FamilyGroupsScreen />} />} />
        <Route path="/historial" element={<Guard allow="canSeeHistory" element={<HistoryScreen />} />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

/** Envuelve las rutas del armazón sin repetirlo en cada una. */
function Framed() {
  return (
    <AppShell>
      <Outlet />
    </AppShell>
  )
}

/**
 * Guarda de ruta. Es solo comodidad de navegación —evitar que alguien llegue por
 * URL a una pantalla que no le sirve—, NO seguridad: quien decide de verdad es
 * el backend, que revalida roles y cargos contra la base en cada petición.
 */
function Guard({
  allow,
  element,
}: {
  allow: 'canCaptureSundaySchool' | 'canCaptureServices' | 'canSeeHistory' | 'canAdminister'
  element: React.ReactElement
}) {
  const perms = usePermissions()
  return perms[allow] ? element : <Navigate to="/" replace />
}
