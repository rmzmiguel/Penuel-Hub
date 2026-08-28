import { useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import type { ReactNode } from "react";
import { IconButton } from "../Button";
import { useScrollLock } from "../../lib/viewport";

/*
 * Los paneles se montan en `document.body` y no donde se declaran.
 *
 * `position: fixed` deja de medir contra la ventana en cuanto CUALQUIER
 * antecesor tiene `transform`, `filter`, `backdrop-filter` o `will-change` —
 * pasa a medir contra ese antecesor. Estos paneles se declaran a seis niveles
 * de profundidad, dentro del armazón y de pantallas que ya usan animaciones con
 * `transform`. Sacarlos por un portal no es un adorno: es lo que hace que
 * mañana nadie los rompa añadiendo una animación tres niveles más arriba.
 */
function Portal({ children }: { children: ReactNode }) {
  return createPortal(children, document.body);
}

/**
 * Panel de detalle.
 *
 * En escritorio entra por la derecha y ocupa 30rem; en el teléfono sube desde
 * abajo y ocupa el 92% del alto. Es el MISMO componente y el mismo contenido:
 * la adaptación es de posición, nunca de capacidades — un panel que en móvil
 * ofrece menos que en escritorio es un panel roto.
 *
 * Se cierra con Escape, con el botón (que tiene su etiqueta) y tocando fuera.
 * No se cierra con gestos de arrastre: descubrir un gesto no puede ser el
 * único camino a nada.
 */
export function Sheet({
  open,
  onClose,
  title,
  eyebrow,
  children,
  footer,
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  eyebrow?: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  const panel = useRef<HTMLDivElement>(null);

  useScrollLock(open);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
    document.addEventListener("keydown", onKey);
    panel.current?.focus();
    return () => document.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <Portal>
      {/* `sheet-frame` en lugar de `inset-0`: ver la nota del marco en
          styles.css — no coinciden el viewport de maqueta y el dinámico. */}
      <div className="sheet-frame z-50 flex sm:justify-end">
        <button
          type="button"
          aria-label="Cerrar el panel"
          onClick={onClose}
          className="absolute inset-0 bg-scrim/45 backdrop-blur-[3px] animate-fade"
        />

        <div
          ref={panel}
          tabIndex={-1}
          role="dialog"
          aria-modal="true"
          aria-label={title}
          /* `h-[92%]` y no `92dvh`: el porcentaje se mide contra el contenedor,
           que ya está atado al viewport visual. Así el borde inferior del panel
           coincide exactamente con el borde inferior de lo que se ve. */
          className="relative mt-auto sm:mt-0 w-full sm:w-[30rem] sm:max-w-[92vw]
                   h-[92%] sm:h-full bg-bone
                   rounded-t-[2rem] rounded-b-none sm:rounded-none sm:rounded-l-[2rem]
                   shadow-hero flex flex-col overflow-hidden outline-none
                   animate-slide-up sm:animate-fade"
        >
          {/* Asa visual del sheet móvil. Es solo una señal de "esto sube y baja";
            el cierre real está en el botón de al lado. */}
          <span
            aria-hidden="true"
            className="sm:hidden mx-auto mt-3 h-1.5 w-11 rounded-full bg-line-strong"
          />

          <header className="shrink-0 px-5 sm:px-7 pt-4 sm:pt-7 pb-5 flex items-start gap-4">
            <div className="min-w-0 flex-1">
              {eyebrow && <p className="eyebrow text-ink-soft">{eyebrow}</p>}
              <h2 className="font-display text-2xl font-semibold leading-tight mt-0.5">
                {title}
              </h2>
            </div>
            <IconButton
              icon="close"
              label="Cerrar"
              variant="secondary"
              size="sm"
              onClick={onClose}
            />
          </header>

          {/* `pt-1` deja respirar la primera tarjeta bajo la cabecera: sin él queda
            colgando del título y el panel se lee como una lista apretada.
            `overscroll-contain` corta el encadenamiento del rebote: al llegar al
            final de la lista, iOS ya no arrastra la página de detrás. */}
          {/* `overflow-x-hidden` explícito: al declarar `overflow-y`, el eje
              contrario pasa a `auto` por especificación, y cualquier campo que se
              salga un píxel convierte el panel en algo que se arrastra en dos
              direcciones a la vez. */}
          <div className="flex-1 overflow-y-auto overflow-x-hidden overscroll-contain scroll-slim
                          px-5 sm:px-7 pt-1 pb-8">
            {children}
          </div>

          {footer && (
            <div
              className="shrink-0 frost border-t border-line px-5 sm:px-7 py-4"
              style={{
                paddingBottom: "max(1rem, env(safe-area-inset-bottom))",
              }}
            >
              {footer}
            </div>
          )}
        </div>
      </div>
    </Portal>
  );
}

/**
 * Confirmación de una acción con consecuencias.
 *
 * Aparece centrada y bloquea todo lo demás. Dice en una frase QUÉ cambia y
 * QUÉ deja de poder hacer la persona afectada — nunca "¿Estás seguro?", que no
 * informa nada y solo entrena a la gente a confirmar sin leer.
 */
export function ConfirmDialog({
  open,
  title,
  body,
  confirmLabel,
  tone = "danger",
  busy = false,
  onConfirm,
  onCancel,
}: {
  open: boolean;
  title: string;
  body: ReactNode;
  confirmLabel: string;
  tone?: "danger" | "primary";
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  useScrollLock(open);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) =>
      e.key === "Escape" && !busy && onCancel();
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open, busy, onCancel]);

  if (!open) return null;

  return (
    <Portal>
      <div className="sheet-frame z-[60] grid place-items-end sm:place-items-center p-0 sm:p-6">
        <button
          type="button"
          aria-label="Cancelar"
          onClick={() => !busy && onCancel()}
          className="absolute inset-0 bg-scrim/55 backdrop-blur-[3px] animate-fade"
        />
        <div
          role="alertdialog"
          aria-modal="true"
          aria-label={title}
          className="relative w-full sm:max-w-lg max-h-full overflow-y-auto overflow-x-hidden overscroll-contain
                   bg-surface shadow-hero
                   rounded-t-[2rem] rounded-b-none sm:rounded-[2rem] p-6 sm:p-8
                   animate-slide-up sm:animate-pop"
          style={{ paddingBottom: "max(1.5rem, env(safe-area-inset-bottom))" }}
        >
          <h2 className="font-display text-2xl font-semibold leading-tight">
            {title}
          </h2>
          <div className="mt-3 text-ink-soft leading-relaxed">{body}</div>

          {/* En móvil la acción principal va arriba y al ancho completo: es donde
            cae el pulgar. En escritorio, a la derecha, como manda la costumbre. */}
          <div className="mt-7 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <ConfirmButton variant="cancel" onClick={onCancel} disabled={busy}>
              Cancelar
            </ConfirmButton>
            <ConfirmButton
              variant={tone}
              onClick={onConfirm}
              disabled={busy}
              busy={busy}
            >
              {confirmLabel}
            </ConfirmButton>
          </div>
        </div>
      </div>
    </Portal>
  );
}

function ConfirmButton({
  children,
  variant,
  busy,
  ...rest
}: {
  children: ReactNode;
  variant: "cancel" | "danger" | "primary";
  busy?: boolean;
} & React.ButtonHTMLAttributes<HTMLButtonElement>) {
  const style = {
    cancel: "bg-surface text-ink border border-line hover:bg-bone-deep",
    danger: "bg-danger text-on-accent hover:bg-danger/90",
    primary: "bg-clay text-on-accent hover:bg-clay-deep",
  }[variant];

  return (
    <button
      {...rest}
      type="button"
      className={`inline-flex items-center justify-center gap-2.5 min-h-touch-lg px-7
                  rounded-control font-medium text-lg transition active:scale-[0.97]
                  disabled:opacity-45 disabled:pointer-events-none ${style}`}
    >
      {busy && (
        <span className="size-4 rounded-full border-2 border-current border-t-transparent animate-spin" />
      )}
      {children}
    </button>
  );
}
