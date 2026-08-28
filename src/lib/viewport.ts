import { useLayoutEffect } from 'react'

/*
 * Bloqueo del scroll del fondo mientras hay un panel abierto.
 *
 * POR QUÉ NO BASTA `overflow: hidden`
 * En Safari de iOS el `body` con `overflow: hidden` sigue admitiendo el rebote
 * elástico: el fondo se arrastra y con él se desplaza lo que está en `fixed`.
 * Fijar el `body` y devolverle el scroll al soltar sí lo detiene, y es la
 * técnica que usan todas las librerías serias de diálogos.
 *
 * POR QUÉ `useLayoutEffect` Y NO `useEffect`
 * Fijar el `body` colapsa la altura del documento y lleva el scroll a 0. Si eso
 * ocurre DESPUÉS de pintar —que es cuando corre `useEffect`—, iOS se queda con
 * la capa ya pintada del panel pero calcula los toques contra la geometría
 * nueva: el panel se ve en un sitio y responde en otro. Con `useLayoutEffect`
 * el cambio entra antes del primer pintado y solo hay una geometría.
 *
 * Lleva cuenta de referencias porque un panel puede abrir un diálogo de
 * confirmación encima: el primero en abrir bloquea y el último en cerrar suelta.
 */

let locks = 0
let restoreTo = 0

export function useScrollLock(active: boolean) {
  useLayoutEffect(() => {
    if (!active) return

    locks++
    if (locks === 1) {
      restoreTo = window.scrollY
      const body = document.body.style
      body.position = 'fixed'
      body.top = `-${restoreTo}px`
      body.left = '0'
      body.right = '0'
      body.width = '100%'
      body.overflow = 'hidden'
      // Corta el encadenamiento del rebote hacia la raíz, que es la otra vía
      // por la que el fondo se movía bajo el panel.
      document.documentElement.style.overscrollBehavior = 'none'
    }

    return () => {
      locks--
      if (locks === 0) {
        const body = document.body.style
        body.position = ''
        body.top = ''
        body.left = ''
        body.right = ''
        body.width = ''
        body.overflow = ''
        document.documentElement.style.overscrollBehavior = ''
        window.scrollTo(0, restoreTo)
      }
    }
  }, [active])
}
