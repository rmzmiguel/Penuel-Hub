import type { PersonOption } from '../../api/types'
import { Counter, Switch } from '../../components/Field'
import { Icon } from '../../components/Icon'
import { fullName, initials } from '../../lib/format'

export interface AttendanceDraft {
  person: PersonOption
  wasPresent: boolean
  wasPunctual: boolean
  broughtBible: boolean
  chaptersRead: number
  /** Alguien agregado a mano ese domingo: una visita, o quien se cambió de grupo. */
  isGuest: boolean
}

/**
 * Una persona en la lista de asistencia.
 *
 * El detalle (puntualidad, Biblia, capítulos) SOLO aparece cuando la persona está
 * presente. Preguntar si fue puntual quien no vino sería ruido — y esconderlo no
 * quita ninguna capacidad, que es justo la diferencia entre minimalista y simple.
 */
export function AttendanceRow({
  draft,
  onChange,
  onRemove,
}: {
  draft: AttendanceDraft
  onChange: (next: AttendanceDraft) => void
  onRemove?: () => void
}) {
  const present = draft.wasPresent

  return (
    <li
      className={`rounded-card border transition overflow-hidden
                  ${present ? 'bg-surface border-forest-line shadow-card' : 'bg-surface-warm border-line'}`}
    >
      <button
        type="button"
        onClick={() => onChange({ ...draft, wasPresent: !present })}
        aria-pressed={present}
        className="w-full flex items-center gap-4 px-5 py-4 min-h-touch-lg text-left"
      >
        <span
          className={`shrink-0 grid place-items-center size-12 rounded-2xl font-display font-semibold transition
                      ${present ? 'bg-forest text-on-accent' : 'bg-bone-deep text-ink-faint'}`}
        >
          {present ? <Icon name="check" className="size-6" strokeWidth={2.8} /> : initials(draft.person)}
        </span>

        <span className="flex-1 min-w-0">
          <span className={`block font-semibold text-lg leading-snug ${present ? '' : 'text-ink-soft'}`}>
            {fullName(draft.person)}
          </span>
          <span className={`block text-sm ${present ? 'text-forest font-semibold' : 'text-ink-faint'}`}>
            {present ? 'Presente' : 'Toca para marcar presente'}
            {draft.isGuest && ' · agregado hoy'}
          </span>
        </span>
      </button>

      {present && (
        <div className="px-5 pb-5 pt-1 space-y-1 border-t border-forest-line/60 bg-forest-soft/25">
          <Switch
            label="Llegó puntual"
            checked={draft.wasPunctual}
            onChange={(v) => onChange({ ...draft, wasPunctual: v })}
          />
          <Switch
            label="Trajo su Biblia"
            checked={draft.broughtBible}
            onChange={(v) => onChange({ ...draft, broughtBible: v })}
          />
          <div className="pt-2">
            <Counter
              label="Capítulos leídos"
              value={draft.chaptersRead}
              onChange={(v) => onChange({ ...draft, chaptersRead: v })}
            />
          </div>
        </div>
      )}

      {onRemove && !present && (
        <div className="px-5 pb-4">
          <button
            type="button"
            onClick={onRemove}
            className="inline-flex items-center gap-2 min-h-11 px-3 -ml-3 rounded-xl
                       text-sm font-semibold text-ink-soft hover:text-danger hover:bg-danger-soft transition"
          >
            <Icon name="minus" className="size-4" />
            <span>Quitar de la lista</span>
          </button>
        </div>
      )}
    </li>
  )
}
