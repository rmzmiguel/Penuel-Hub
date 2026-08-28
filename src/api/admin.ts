import { api } from './client'
import type { CreatedResource, PersonAdministration } from './types'

/**
 * Administración de personas, permisos y accesos.
 *
 * ══════════════════════════════════════════════════════════════════════════
 * REGLA DE ESTE ARCHIVO: aquí solo entran rutas que EXISTEN en el backend.
 *
 * Una versión anterior declaraba `GET /api/persons/directory` y
 * `GET /api/admin/catalogs` como "pendientes en el backend" y aun así las
 * llamaba. En la aplicación real devolvían 404; en el modo demostración
 * funcionaban, porque el propio modo demostración las fabricaba. Un contrato
 * inventado que solo existe en la simulación no es una especificación.
 * ══════════════════════════════════════════════════════════════════════════
 *
 * Casi todo lo de aquí viene por PAREJAS —otorgar y retirar, encender y
 * apagar—, y eso es deliberado: una pantalla de administración que solo sabe
 * dar es una pantalla que obliga a entrar a la base de datos para arreglar
 * cualquier error.
 */
export const admin = {
  /**
   * Estado administrativo completo de una persona, en una sola llamada.
   * Trae también los catálogos de roles y cargos, con la marca de cuáles tiene.
   */
  administration: (personId: string, signal?: AbortSignal) =>
    api.get<PersonAdministration>(`/api/persons/${personId}/administration`, signal),

  /** Alta en el directorio. No la hace miembro oficial ni le da acceso. */
  registerPerson: (firstName: string, lastName: string) =>
    api.post<CreatedResource>('/api/persons', {
      firstName,
      lastName,
      dateOfBirth: null,
      phoneNumber: null,
    }),

  /**
   * Corrige la ficha: nombre, apellidos, nacimiento y teléfono. Nada de lo que la
   * persona ES en la iglesia — eso son operaciones aparte, cada una con su fila.
   */
  updatePerson: (
    personId: string,
    firstName: string,
    lastName: string,
    dateOfBirth: string | null,
    phoneNumber: string | null,
  ) =>
    api.put<void>(`/api/persons/${personId}`, { firstName, lastName, dateOfBirth, phoneNumber }),

  // ── Presencia en el directorio ───────────────────────────────────────────
  deactivatePerson: (personId: string) =>
    api.post<void>(`/api/persons/${personId}/deactivate`),

  reactivatePerson: (personId: string) =>
    api.post<void>(`/api/persons/${personId}/reactivate`),

  // ── Acceso al sistema ────────────────────────────────────────────────────
  /** Credenciales para entrar. Ortogonal a la membresía. */
  createUserAccount: (personId: string, email: string, password: string) =>
    api.post<CreatedResource>(`/api/persons/${personId}/user-account`, { email, password }),

  /**
   * Enciende o apaga el acceso. La cuenta NUNCA se borra (regla 7.3), así que
   * "quitar el acceso" solo puede ser apagarla — y encenderla devuelve la misma,
   * con su correo y su contraseña intactos.
   */
  setAccountAccess: (personId: string, isActive: boolean) =>
    api.put<void>(`/api/persons/${personId}/user-account/access`, { isActive }),

  // ── Permisos del sistema ─────────────────────────────────────────────────
  /** Va contra la CUENTA, no contra la persona: un rol se otorga a credenciales. */
  assignRole: (userAccountId: string, roleName: string) =>
    api.post<CreatedResource>('/api/roles/assign', { userAccountId, roleName }),

  revokeRole: (userAccountId: string, roleName: string) =>
    api.post<void>('/api/roles/revoke', { userAccountId, roleName }),

  // ── Membresía oficial ────────────────────────────────────────────────────
  grantMembership: (personId: string) =>
    api.post<CreatedResource>('/api/memberships', { personId, joinedAt: null }),

  /** Baja o restitución. Conserva la fila, y con ella la fecha de ingreso. */
  setMembership: (personId: string, isMember: boolean) =>
    api.put<void>(`/api/memberships/${personId}/status`, { isMember }),

  // ── Cargos eclesiásticos ─────────────────────────────────────────────────
  assignPosition: (positionId: string, personId: string) =>
    api.post<CreatedResource>(`/api/positions/${positionId}/holders`, { personId }),

  revokePosition: (positionId: string, personId: string) =>
    api.del<void>(`/api/positions/${positionId}/holders/${personId}`),

  // ── Liderazgo de grupos ──────────────────────────────────────────────────
  /**
   * Un grupo tiene como mucho UN líder activo (regla 7.11), así que asignar uno
   * a un grupo que ya lo tiene falla con un conflicto. Por eso el panel muestra
   * antes quién lo lidera: para que la negativa nunca llegue de sorpresa.
   */
  assignSocietyLeader: (societyId: string, personId: string) =>
    api.post<CreatedResource>(`/api/societies/${societyId}/leader`, { personId }),

  revokeSocietyLeader: (societyId: string) =>
    api.del<void>(`/api/societies/${societyId}/leader`),

  assignMinistryLeader: (ministryId: string, personId: string) =>
    api.post<CreatedResource>(`/api/ministries/${ministryId}/leader`, { personId }),

  revokeMinistryLeader: (ministryId: string) =>
    api.del<void>(`/api/ministries/${ministryId}/leader`),

  // ── Pertenencia a grupos de Escuela Dominical ────────────────────────────
  addSocietyMember: (societyId: string, personId: string) =>
    api.post<CreatedResource>(`/api/societies/${societyId}/members`, { personId }),

  removeSocietyMember: (societyMembershipId: string) =>
    api.del<void>(`/api/societies/members/${societyMembershipId}`),
}
