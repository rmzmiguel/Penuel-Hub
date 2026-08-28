import { useMemo, useState } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth, usePermissions } from '../auth/AuthProvider'
import { Button } from './Button'
import { Icon } from './Icon'
import type { IconName } from './Icon'
import { Avatar } from './ui/Avatar'
import { Chip } from './ui/Chip'
import { Sheet } from './ui/Sheet'
import { useTheme } from '../theme/ThemeProvider'
import type { Preferencia } from '../theme/ThemeProvider'

interface NavItem {
  to: string
  label: string
  /** Etiqueta corta para el dock del teléfono, donde 68px no dan para más. */
  short: string
  icon: IconName
}

/**
 * La navegación se arma de las capacidades reales, nunca de una lista fija.
 *
 * Tope de CINCO entradas, y no es casualidad: el dock reparte el ancho entre
 * ellas, y a partir de la sexta la etiqueta más larga —"Personas", 55px a 13px—
 * ya no cabe en un teléfono de 320px. Lo que no entra aquí entra en el tablero,
 * que es donde vive lo administrativo (Grupos Familiares, por ejemplo). El dock
 * es para lo que se usa cada semana.
 * Si el Pastor le retira un rol a alguien, la entrada desaparece la próxima vez
 * que abra la aplicación, sin tocar una línea de código.
 */
function useNav(): NavItem[] {
  const perms = usePermissions()

  return useMemo(() => {
    const items: NavItem[] = [{ to: '/', label: 'Inicio', short: 'Inicio', icon: 'grid' }]

    if (perms.canAdminister) items.push({ to: '/personas', label: 'Personas', short: 'Personas', icon: 'group' })
    if (perms.canCaptureSundaySchool)
      items.push({ to: '/escuela-dominical', label: 'Escuela Dominical', short: 'Escuela', icon: 'book' })
    if (perms.canCaptureServices) items.push({ to: '/culto', label: 'Reporte de culto', short: 'Culto', icon: 'coins' })
    if (perms.canSeeHistory) items.push({ to: '/historial', label: 'Historial', short: 'Historial', icon: 'list' })

    return items
  }, [perms])
}

/**
 * Armazón de la aplicación.
 *
 * En escritorio, barra lateral de tinta fija a la izquierda. En el teléfono, un
 * dock flotante abajo —donde alcanza el pulgar— y una barra superior mínima con
 * la marca y el acceso a la cuenta.
 *
 * Es la misma navegación en ambos: cambia de sitio, no de contenido.
 */
export function AppShell({ children }: { children: ReactNode }) {
  const items = useNav()
  const [account, setAccount] = useState(false)

  return (
    <div className="relative min-h-dvh">
      <Rail items={items} onAccount={() => setAccount(true)} />
      <TopBar onAccount={() => setAccount(true)} />

      <div className="lg:pl-rail">
        <main
          className="px-4 sm:px-7 lg:pr-8 pt-4 lg:pt-8
                     pb-[calc(var(--spacing-dock)+2.5rem+env(safe-area-inset-bottom))] lg:pb-12"
        >
          {children}
        </main>
      </div>

      <Dock items={items} />
      <AccountSheet open={account} onClose={() => setAccount(false)} />
    </div>
  )
}

/* ── Escritorio ─────────────────────────────────────────────────────────── */

function Rail({ items, onAccount }: { items: NavItem[]; onAccount: () => void }) {
  const { capabilities } = useAuth()
  const perms = usePermissions()

  return (
    <aside className="hidden lg:flex fixed inset-y-4 left-4 z-40 w-62 flex-col
                      rounded-hero bg-surface pane shadow-card overflow-hidden">
      <Brand />

      <nav className="flex-1 px-3 pt-2 space-y-1.5 overflow-y-auto scroll-slim">
        {items.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              `group relative flex items-center gap-3.5 min-h-touch px-4 rounded-control
               font-medium transition-[background-color,color] duration-250
               ${isActive
                 ? 'bg-ink text-on-ink'
                 : 'text-ink-soft hover:text-ink hover:bg-bone'}`
            }
          >
            {({ isActive }) => (
              <>
                <Icon name={item.icon} className="size-5 shrink-0" strokeWidth={isActive ? 2.1 : 1.8} />
                <span className="truncate">{item.label}</span>
              </>
            )}
          </NavLink>
        ))}
      </nav>

      {/* Tarjeta de identidad. No es decorativa: los cargos y liderazgos rotan,
          y ver aquí lo que el sistema cree de uno evita la sorpresa de
          "¿por qué no me deja capturar?". */}
      <div className="p-3">
        <button
          type="button"
          onClick={onAccount}
          className="w-full flex items-center gap-3 p-3 rounded-[1.4rem] bg-bone
                     text-left transition hover:bg-bone-deep active:scale-[0.98]"
        >
          {capabilities && <Avatar person={capabilities} size="sm" />}
          <span className="min-w-0 flex-1">
            <span className="block font-medium text-sm truncate text-ink">
              {capabilities?.firstName} {capabilities?.lastName.split(' ')[0]}
            </span>
            <span className="block text-xs text-ink-faint truncate">
              {perms.canAdminister ? 'Acceso total' : capabilities?.positions[0]?.name ?? 'Colaborador'}
            </span>
          </span>
          <Icon name="up" className="size-4 shrink-0 text-ink-faint" strokeWidth={2.1} />
        </button>
      </div>
    </aside>
  )
}

/**
 * La marca. El monograma es una P en Fraunces sobre brasa: es lo único
 * saturado de toda la barra, y por eso funciona como ancla.
 */
function Brand({ compact = false }: { compact?: boolean }) {
  return (
    <div className={compact ? 'flex items-center gap-2.5' : 'flex items-center gap-3 px-5 pt-6 pb-5'}>
      <span
        className={`shrink-0 grid place-items-center rounded-[0.85rem] bg-clay text-on-accent
                    font-display font-bold leading-none
                    ${compact ? 'size-9 text-lg' : 'size-10 text-xl'}`}
      >
        <span aria-hidden="true" className="-mt-px">P</span>
      </span>
      <span className="min-w-0">
        <span className={`block font-display font-medium leading-none
                          ${compact ? 'text-lg' : 'text-xl'}`}>
          Penuel
        </span>
        {!compact && (
          <span className="block text-xs text-ink-faint mt-1 truncate">Comunidad Cristiana</span>
        )}
      </span>
    </div>
  )
}

/* ── Teléfono ───────────────────────────────────────────────────────────── */

function TopBar({ onAccount }: { onAccount: () => void }) {
  const { capabilities } = useAuth()

  return (
    <header className="lg:hidden sticky top-0 z-40 frost">
      <div className="flex items-center justify-between gap-3 px-4 h-16">
        <span className="text-ink">
          <Brand compact />
        </span>
        <button
          type="button"
          onClick={onAccount}
          aria-label="Tu cuenta"
          className="shrink-0 rounded-full transition active:scale-95"
        >
          {capabilities && <Avatar person={capabilities} size="sm" />}
        </button>
      </div>
    </header>
  )
}

/**
 * Dock del teléfono.
 *
 * La versión anterior tenía un defecto de fondo: la pastilla activa cubría la
 * celda ENTERA, así que la etiqueta tenía que caber dentro de ella y acabó a
 * 11px, truncada. Es la letra más pequeña de toda la aplicación, puesta
 * justamente en la navegación, en una app cuya escala arranca en 16.5px porque
 * "se nota a los 70 años". La navegación no puede ser lo menos legible.
 *
 * El arreglo es separar las dos cosas: el indicador se encoge hasta quedar solo
 * DETRÁS DEL ICONO, y la etiqueta baja fuera de él, en tinta normal y a 13px.
 * Nadie pierde nada y el texto crece un 20%.
 *
 * Y se adapta a cuántas entradas dio `/api/me/capabilities`:
 *
 *   · 4 o 5 (el Pastor)  — icono arriba, etiqueta debajo. Es lo único que cabe.
 *   · 1 a 3 (un maestro) — icono y etiqueta EN FILA dentro de una pastilla
 *     ancha, a 15px. Con tres entradas sobra sitio, y así el dock se lee igual
 *     que la barra lateral de escritorio: la misma navegación, no dos.
 *
 * El indicador se mueve con `translateX` y no con `left`: es la misma
 * animación a la vista, pero la compone la GPU y no obliga a recalcular la
 * disposición en cada fotograma.
 */
function Dock({ items }: { items: NavItem[] }) {
  const { pathname } = useLocation()

  /* -1 cuando ninguna entrada corresponde a la ruta. El código anterior hacía
     `Math.max(0, ...)` y en ese caso encendía "Inicio" por error. */
  const index = items.findIndex((i) => (i.to === '/' ? pathname === '/' : pathname.startsWith(i.to)))
  const apilado = items.length >= 4
  const ancho = 100 / items.length

  return (
    <nav
      aria-label="Navegación principal"
      className="lg:hidden fixed inset-x-0 bottom-0 z-40 px-3 pointer-events-none"
      style={{ paddingBottom: 'max(0.75rem, env(safe-area-inset-bottom))' }}
    >
      <div className="pointer-events-auto relative mx-auto max-w-lg h-dock rounded-card
                      bg-surface pane shadow-dock flex items-stretch p-1.5">
        {index >= 0 && (
          /*
           * El indicador repite la MISMA caja que el enlace: `inset-1.5` para
           * arrancar donde arranca la fila y el mismo centrado. Fijar aquí un
           * `top` a ojo dejaba la pastilla 6px por debajo del icono, porque un
           * hijo absoluto mide desde el borde de RELLENO y el enlace desde el de
           * contenido. Compartiendo caja, no pueden desalinearse.
           */
          <span aria-hidden="true" className="pointer-events-none absolute inset-1.5">
            <span
              className="h-full flex transition-transform duration-[420ms]
                         ease-[cubic-bezier(0.22,1,0.36,1)]"
              style={{ width: `${ancho}%`, transform: `translateX(${index * 100}%)` }}
            >
              <span
                className={`w-full flex items-center justify-center ${apilado ? 'flex-col gap-1' : ''}`}
              >
                <span
                  className={`rounded-full bg-ink ${apilado ? 'h-9 w-[3.35rem] max-w-full' : 'h-full w-full'}`}
                />
                {/* Fantasma de la etiqueta: ocupa su mismo alto para que el
                    centrado del indicador coincida con el del enlace. */}
                {apilado && <span aria-hidden="true" className="text-2xs leading-none">&nbsp;</span>}
              </span>
            </span>
          </span>
        )}

        {items.map((item, i) => {
          const activo = i === index
          /* Apilado, la pastilla solo queda bajo el icono; en fila, cubre la
             celda entera y también la etiqueta. De eso depende qué se tiñe. */
          const etiquetaSobrePastilla = activo && !apilado

          return (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              aria-current={activo ? 'page' : undefined}
              className={`relative z-10 flex-1 min-w-0 flex items-center rounded-full
                          transition-transform active:scale-95
                          ${apilado ? 'flex-col justify-center gap-1' : 'flex-row justify-center gap-2.5'}`}
            >
              {/* Caja de alto fijo: es lo que hace que el indicador y el icono
                  caigan exactamente en la misma línea sin depender del centrado. */}
              <span className={`grid place-items-center shrink-0 ${apilado ? 'h-9' : ''}`}>
                <Icon
                  name={item.icon}
                  className={`size-5 transition-colors duration-250
                              ${activo ? 'text-on-ink' : 'text-ink-soft'}`}
                  strokeWidth={activo ? 2.2 : 1.85}
                />
              </span>

              <span
                className={`truncate max-w-full leading-none transition-colors duration-250
                            ${apilado ? 'text-2xs' : 'text-sm'}
                            ${activo ? 'font-semibold' : 'font-medium'}
                            ${etiquetaSobrePastilla ? 'text-on-ink' : activo ? 'text-ink' : 'text-ink-soft'}`}
              >
                {item.short}
              </span>
            </NavLink>
          )
        })}
      </div>
    </nav>
  )
}

/* ── Cuenta ─────────────────────────────────────────────────────────────── */

/**
 * Lo que esta persona ES dentro de la iglesia, con el cierre de sesión al pie.
 * Los tres ejes —rol de sistema, cargo y liderazgo— se muestran separados
 * porque en el dominio son independientes: ser Diácono no da permisos, y tener
 * un permiso no da un cargo.
 */
function AccountSheet({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { capabilities, signOut } = useAuth()
  const perms = usePermissions()
  const navigate = useNavigate()

  if (!capabilities) return null

  const blocks = [
    {
      icon: 'shield' as IconName,
      label: 'Permisos del sistema',
      empty: 'Sin permisos especiales',
      values: capabilities.roles,
      tone: 'clay' as const,
    },
    {
      icon: 'star' as IconName,
      label: capabilities.positions.length === 1 ? 'Tu cargo' : 'Tus cargos',
      empty: 'Sin cargo asignado',
      values: capabilities.positions.map((p) => p.name),
      tone: 'ochre' as const,
    },
    {
      icon: 'group' as IconName,
      label: 'Liderazgo',
      empty: 'No lideras ningún grupo',
      values: [
        ...capabilities.ledMinistries.map((m) => m.name),
        ...capabilities.ledSocieties.map((s) => s.name),
      ],
      tone: 'forest' as const,
    },
  ]

  return (
    <Sheet open={open} onClose={onClose} eyebrow="Tu cuenta" title={`${capabilities.firstName} ${capabilities.lastName}`}>
      <div className="flex items-center gap-4 rounded-panel bg-surface pane p-5 shadow-card">
        <Avatar person={capabilities} size="xl" />
        <div className="min-w-0">
          <p className="font-display text-xl font-medium leading-tight">
            {capabilities.firstName} {capabilities.lastName}
          </p>
          <p className="text-sm text-ink-soft break-all leading-snug mt-0.5">
            {capabilities.email ?? 'Sin correo registrado'}
          </p>
          <div className="mt-2.5 flex flex-wrap gap-1.5">
            <Chip tone={capabilities.isOfficialMember ? 'forest' : 'neutral'} icon={capabilities.isOfficialMember ? 'check' : 'clock'}>
              {capabilities.isOfficialMember ? 'Miembro oficial' : 'No es miembro oficial'}
            </Chip>
            {capabilities.isExecutiveBodyMember && <Chip tone="ochre" icon="star">Cuerpo ejecutivo</Chip>}
          </div>
        </div>
      </div>

      <div className="mt-4 space-y-4">
        {blocks.map((b) => (
          <div key={b.label} className="rounded-card bg-surface pane p-5 shadow-card">
            <div className="flex items-center gap-2.5">
              <Icon name={b.icon} className="size-4.5 text-ink-faint" strokeWidth={2.1} />
              <p className="eyebrow text-ink-soft">{b.label}</p>
            </div>
            <div className="mt-3 flex flex-wrap gap-2">
              {b.values.length > 0 ? (
                b.values.map((v, i) => <Chip key={`${b.label}-${i}-${v}`} tone={b.tone}>{v}</Chip>)
              ) : (
                <span className="text-ink-faint">{b.empty}</span>
              )}
            </div>
          </div>
        ))}

        <Apariencia />
      </div>

      {perms.canAdminister && (
        <div className="mt-4">
          <Button
            variant="secondary"
            full
            icon="group"
            iconAfter="next"
            onClick={() => { onClose(); navigate('/personas') }}
            size="lg"
          >
            Administrar la congregación
          </Button>
        </div>
      )}

      <div className="mt-3">
        <Button variant="danger" full icon="logout" size="lg" onClick={signOut}>
          Cerrar sesión
        </Button>
      </div>
    </Sheet>
  )
}

/**
 * Elección de apariencia.
 *
 * Tres opciones y no un interruptor de dos, porque "sistema" es la respuesta
 * correcta para casi todo el mundo: el teléfono ya cambia solo al anochecer.
 * Cuando está elegida se dice EN PALABRAS cómo se está viendo ahora mismo; un
 * interruptor que a veces está en un lado y a veces en otro, sin explicar por
 * qué, es exactamente el tipo de misterio que esta aplicación no puede
 * permitirse.
 */
function Apariencia() {
  const { preferencia, tema, elegir } = useTheme()

  const opciones: { valor: Preferencia; label: string; icon: IconName }[] = [
    { valor: 'claro', label: 'Claro', icon: 'sun' },
    { valor: 'oscuro', label: 'Oscuro', icon: 'moon' },
    { valor: 'sistema', label: 'Sistema', icon: 'settings' },
  ]

  return (
    <div className="rounded-card bg-surface pane p-5 shadow-card">
      <div className="flex items-center gap-2.5">
        <Icon name="sunset" className="size-4.5 text-ink-faint" strokeWidth={2.1} />
        <p className="eyebrow text-ink-soft">Apariencia</p>
      </div>

      <div className="mt-3 grid grid-cols-3 gap-2" role="group" aria-label="Apariencia">
        {opciones.map((o) => {
          const activa = preferencia === o.valor
          return (
            <button
              key={o.valor}
              type="button"
              aria-pressed={activa}
              onClick={() => elegir(o.valor)}
              className={`min-h-touch flex flex-col items-center justify-center gap-1.5 px-2
                          rounded-[1.25rem] font-medium text-sm
                          transition-[background-color,color] duration-250 active:scale-95
                          ${activa
                            ? 'bg-ink text-on-ink'
                            : 'bg-bone text-ink-soft hover:bg-bone-deep hover:text-ink'}`}
            >
              <Icon name={o.icon} className="size-5 shrink-0" strokeWidth={activa ? 2.1 : 1.8} />
              <span className="truncate max-w-full">{o.label}</span>
            </button>
          )
        })}
      </div>

      {preferencia === 'sistema' && (
        <p className="mt-3 text-sm text-ink-faint leading-snug">
          Sigue a tu teléfono. Ahora mismo lo está mostrando {tema}.
        </p>
      )}
    </div>
  )
}
