import { useCallback, useEffect, useRef, useState } from 'react'
import { ApiError } from '../api/client'

interface AsyncState<T> {
  data: T | null
  error: ApiError | null
  loading: boolean
}

/**
 * Carga de datos con cancelación. Deliberadamente pequeño: esta aplicación tiene
 * pocas pantallas y ninguna necesita caché entre ellas — al contrario, los datos
 * de capacidades y de grupos DEBEN releerse, porque el backend los recalcula.
 *
 * Devuelve DOS maneras de releer, y la diferencia importa:
 *
 * · `reload()` es la relectura visible. Enciende `loading`, así que la pantalla
 *   muestra su esqueleto. Es la correcta cuando la persona pidió los datos —
 *   reintentar tras un error, cambiar de filtro.
 *
 * · `refresh()` es la relectura CALLADA y esperable. No toca `loading` ni borra
 *   lo que ya había, y se puede `await`. Es la correcta después de guardar algo:
 *   la pantalla ya está en su sitio y encender `loading` la haría parpadear
 *   entera para acabar enseñando casi lo mismo.
 */
export function useAsync<T>(
  loader: (signal: AbortSignal) => Promise<T>,
  deps: unknown[] = [],
): AsyncState<T> & { reload: () => void; refresh: () => Promise<void> } {
  const [state, setState] = useState<AsyncState<T>>({ data: null, error: null, loading: true })
  const [nonce, setNonce] = useState(0)
  const mounted = useRef(true)

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const run = useCallback(loader, deps)

  useEffect(() => {
    mounted.current = true
    return () => {
      mounted.current = false
    }
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    let alive = true
    setState((s) => ({ ...s, loading: true, error: null }))

    run(controller.signal)
      .then((data) => alive && setState({ data, error: null, loading: false }))
      .catch((error: unknown) => {
        if (controller.signal.aborted || !alive) return
        setState({
          data: null,
          loading: false,
          error: error instanceof ApiError ? error : new ApiError(0, 'Unknown', String(error)),
        })
      })

    return () => {
      alive = false
      controller.abort()
    }
  }, [run, nonce])

  const refresh = useCallback(async () => {
    try {
      const data = await run(new AbortController().signal)
      if (mounted.current) setState((s) => ({ ...s, data, error: null }))
    } catch {
      /*
       * Se conserva lo que ya se estaba mostrando. Quien llamó a `refresh` viene
       * de una escritura que SÍ funcionó; tumbar la pantalla a un estado de
       * error porque la relectura falló contaría una historia falsa. El siguiente
       * `reload` lo resolverá.
       */
    }
  }, [run])

  return { ...state, reload: () => setNonce((n) => n + 1), refresh }
}
