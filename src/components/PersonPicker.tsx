import { useEffect, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import { directory } from "../api/endpoints";
import type { PersonOption } from "../api/types";
import { fullName, initials } from "../lib/format";
import { Button } from "./Button";
import { ErrorState, Loading } from "./Feedback";
import { Icon } from "./Icon";
import type { ApiError } from "../api/client";
import { useScrollLock } from "../lib/viewport";

/**
 * Selector de persona sobre el directorio.
 *
 * Se presenta como una pantalla completa y no como un desplegable: un `<select>`
 * nativo con cien nombres es de las cosas más difíciles de usar en un teléfono
 * para alguien mayor. Aquí son filas grandes, con buscador y sin gestos.
 */
export function PersonPicker({
  title,
  description,
  excludeIds = [],
  onPick,
  onCancel,
}: {
  title: string;
  description?: string;
  excludeIds?: string[];
  onPick: (person: PersonOption) => void;
  onCancel: () => void;
}) {
  // Mismo tratamiento que los paneles: portal, marco atado al viewport
  // dinámico y bloqueo del fondo. Con `fixed inset-0` el pie —donde está
  // "Cancelar"— quedaba por debajo de lo visible en iOS.
  useScrollLock(true);

  const [search, setSearch] = useState("");
  const [people, setPeople] = useState<PersonOption[]>([]);
  const [error, setError] = useState<ApiError | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    directory
      .persons(undefined, controller.signal)
      .then((list) => {
        setPeople(list);
        setError(null);
      })
      .catch((e: ApiError) => !controller.signal.aborted && setError(e))
      .finally(() => !controller.signal.aborted && setLoading(false));
    return () => controller.abort();
  }, []);

  // El filtrado es local: el directorio de esta iglesia cabe de sobra en memoria,
  // y buscar sin ida y vuelta al servidor se siente inmediato.
  const visible = useMemo(() => {
    const excluded = new Set(excludeIds);
    const term = search.trim().toLowerCase();
    return people
      .filter((p) => !excluded.has(p.id))
      .filter((p) => !term || fullName(p).toLowerCase().includes(term));
  }, [people, search, excludeIds]);

  return createPortal(
    <div className="sheet-frame z-50 bg-bone flex flex-col">
      <header className="sticky top-0 frost">
        <div className="max-w-2xl mx-auto px-5 sm:px-8 py-4">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={onCancel}
              className="shrink-0 inline-flex items-center gap-1.5 -ml-2 pl-2 pr-4
                         min-h-touch rounded-control text-ink-soft
                         font-semibold hover:bg-bone-deep hover:text-ink transition"
            >
              <Icon name="back" className="size-6" />
              <span>Cancelar</span>
            </button>
            <h2 className="font-display text-xl font-medium truncate">
              {title}
            </h2>
          </div>
          {description && <p className="mt-1 text-ink-soft">{description}</p>}

          <div className="relative mt-4">
            <span className="pointer-events-none absolute left-5 top-1/2 -translate-y-1/2 text-ink-faint">
              <Icon name="search" className="size-6" />
            </span>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Buscar por nombre"
              aria-label="Buscar persona por nombre"
              autoComplete="off"
              className="w-full min-h-touch-lg rounded-control bg-surface
                         border border-line pl-14 pr-5 text-lg placeholder:text-ink-faint
                         focus:outline-none focus:border-ink transition"
            />
          </div>
        </div>
      </header>

      <div className="flex-1 overflow-y-auto overscroll-contain">
        <div className="max-w-2xl mx-auto px-5 sm:px-8 py-5">
          {loading ? (
            <Loading label="Cargando el directorio…" />
          ) : error ? (
            <ErrorState error={error} />
          ) : visible.length === 0 ? (
            <p className="py-14 text-center text-lg text-ink-soft">
              {search
                ? `Nadie coincide con “${search}”.`
                : "No hay más personas registradas para elegir."}
            </p>
          ) : (
            <ul className="space-y-2">
              {visible.map((person) => (
                <li key={person.id}>
                  <button
                    type="button"
                    onClick={() => onPick(person)}
                    className="w-full flex items-center gap-4 rounded-card bg-surface pane
                               px-5 py-4 min-h-touch-lg
                               text-left shadow-card transition
                               hover:shadow-raised active:scale-[0.99]"
                  >
                    <span
                      className="shrink-0 grid place-items-center size-12 rounded-full
                                     bg-bone-deep font-medium text-ink-soft"
                    >
                      {initials(person)}
                    </span>
                    <span className="flex-1 min-w-0 font-medium text-lg leading-snug">
                      {fullName(person)}
                    </span>
                    <Icon
                      name="next"
                      className="shrink-0 size-5 text-ink-faint"
                    />
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <div
        className="sticky bottom-0 frost"
        style={{ paddingBottom: "max(0px, env(safe-area-inset-bottom))" }}
      >
        <div className="max-w-2xl mx-auto px-5 sm:px-8 py-4">
          <Button variant="secondary" full icon="back" onClick={onCancel}>
            Cancelar
          </Button>
        </div>
      </div>
    </div>,
    document.body,
  );
}
