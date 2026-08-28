import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'

/**
 * El tema tiene TRES estados, no dos.
 *
 * "sistema" no es un tercer color: es delegar en el teléfono, que ya cambia
 * solo al anochecer. Es lo que quiere la mayoría y por eso es el valor por
 * omisión. "claro" y "oscuro" son una decisión explícita que gana siempre,
 * incluso si el teléfono opina lo contrario.
 */
export type Preferencia = 'claro' | 'oscuro' | 'sistema'

/** Lo que de verdad queda pintado en la pantalla. "sistema" nunca llega aquí. */
export type TemaEfectivo = 'claro' | 'oscuro'

/**
 * La clave y los valores están DUPLICADOS en el script en línea de index.html.
 * Es duplicación a propósito: ese script corre antes de que exista React, y es
 * lo único que evita el fogonazo blanco al abrir la aplicación en oscuro. Si
 * cambias esta clave, cambia también la de ahí.
 */
export const CLAVE_TEMA = 'penuel.tema'

/** El color de la barra del navegador en el teléfono, por tema. */
const BARRA: Record<TemaEfectivo, string> = { claro: '#F1F2F5', oscuro: '#151A22' }

function leerPreferencia(): Preferencia {
  try {
    const v = localStorage.getItem(CLAVE_TEMA)
    if (v === 'claro' || v === 'oscuro' || v === 'sistema') return v
  } catch {
    /* Safari en privado lanza al tocar localStorage. No es motivo de error. */
  }
  return 'sistema'
}

function consultarSistema(): TemaEfectivo {
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'oscuro' : 'claro'
}

interface Contexto {
  preferencia: Preferencia
  /** El tema realmente aplicado, ya resuelto. Úsalo para decidir en render. */
  tema: TemaEfectivo
  elegir: (p: Preferencia) => void
}

const Ctx = createContext<Contexto | null>(null)

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [preferencia, setPreferencia] = useState<Preferencia>(leerPreferencia)
  const [delSistema, setDelSistema] = useState<TemaEfectivo>(consultarSistema)

  // El sistema puede cambiar mientras la aplicación está abierta —anochece, o
  // el usuario lo cambia en ajustes— y entonces hay que seguirlo en vivo.
  useEffect(() => {
    const mq = window.matchMedia?.('(prefers-color-scheme: dark)')
    if (!mq) return
    const alCambiar = (e: MediaQueryListEvent) => setDelSistema(e.matches ? 'oscuro' : 'claro')
    mq.addEventListener('change', alCambiar)
    return () => mq.removeEventListener('change', alCambiar)
  }, [])

  const tema: TemaEfectivo = preferencia === 'sistema' ? delSistema : preferencia

  useEffect(() => {
    document.documentElement.dataset.theme = tema === 'oscuro' ? 'dark' : 'light'
    document.querySelector('meta[name="theme-color"]')?.setAttribute('content', BARRA[tema])
  }, [tema])

  const elegir = useCallback((p: Preferencia) => {
    setPreferencia(p)
    try {
      localStorage.setItem(CLAVE_TEMA, p)
    } catch {
      /* Si no se puede guardar, la elección vale para esta sesión y ya. */
    }
  }, [])

  const valor = useMemo(() => ({ preferencia, tema, elegir }), [preferencia, tema, elegir])

  return <Ctx.Provider value={valor}>{children}</Ctx.Provider>
}

export function useTheme(): Contexto {
  const c = useContext(Ctx)
  if (!c) throw new Error('useTheme debe usarse dentro de <ThemeProvider>')
  return c
}
