import { api } from './client'
import type {
  AvailablePerson,
  CreatedResource,
  FamilyGroupAttendanceInput,
  FamilyGroupDetail,
  FamilyGroupSummary,
  MyFamilyGroup,
} from './types'

/**
 * Grupos Familiares.
 *
 * Dos superficies muy distintas conviven aquí, y conviene no confundirlas:
 *
 *   · `mine` y todo lo que cuelga de `{groupId}/…` lo usa el ANFITRIÓN, que puede
 *     no tener ningún permiso de sistema. El backend lo autoriza comparándolo con
 *     el Anfitrión y el Encargado de ese grupo concreto.
 *   · `all`, `detail`, `reassign` y `setStatus` son del Pastor.
 *
 * No hay ninguna ruta que le deje al Anfitrión pedir "un grupo por identificador":
 * es lo que evita que pueda probar identificadores ajenos y descubrir que existen
 * otras casas.
 */
export const familyGroups = {
  // ── Lo del Anfitrión ─────────────────────────────────────────────────────
  /** Los grupos que lleva quien está dentro. Normalmente cero o uno. */
  mine: (signal?: AbortSignal) =>
    api.get<MyFamilyGroup[]>('/api/family-groups/mine', signal),

  /** Directorio para sumar gente, marcando a quien ya tiene grupo (regla 7.5). */
  availablePersons: (groupId: string, search: string, signal?: AbortSignal) =>
    api.get<AvailablePerson[]>(
      `/api/family-groups/${groupId}/available-persons${search ? `?search=${encodeURIComponent(search)}` : ''}`,
      signal,
    ),

  addMember: (groupId: string, personId: string) =>
    api.post<CreatedResource>(`/api/family-groups/${groupId}/members`, { personId }),

  /** Alta de alguien nuevo. No hay parámetro de membresía porque no existe (regla 7.4). */
  registerMember: (groupId: string, firstName: string, lastName: string, phoneNumber: string | null) =>
    api.post<CreatedResource>(`/api/family-groups/${groupId}/members/register`, {
      firstName,
      lastName,
      phoneNumber,
    }),

  removeMember: (groupId: string, personId: string) =>
    api.del<void>(`/api/family-groups/${groupId}/members/${personId}`),

  submitReport: (
    groupId: string,
    meetingDate: string,
    totalOffering: number,
    attendances: FamilyGroupAttendanceInput[],
  ) =>
    api.post<CreatedResource>(`/api/family-groups/${groupId}/meetings`, {
      meetingDate,
      totalOffering,
      attendances,
    }),

  correctReport: (
    meetingId: string,
    totalOffering: number,
    attendances: FamilyGroupAttendanceInput[] | null,
  ) => api.put<void>(`/api/family-groups/meetings/${meetingId}`, { totalOffering, attendances }),

  // ── Lo del Pastor ────────────────────────────────────────────────────────
  all: (signal?: AbortSignal) => api.get<FamilyGroupSummary[]>('/api/family-groups', signal),

  detail: (groupId: string, signal?: AbortSignal) =>
    api.get<FamilyGroupDetail>(`/api/family-groups/${groupId}`, signal),

  create: (
    hostPersonId: string,
    leaderPersonId: string | null,
    address: string,
    defaultMeetingDayOfWeek: number,
  ) =>
    api.post<CreatedResource>('/api/family-groups', {
      hostPersonId,
      leaderPersonId,
      address,
      defaultMeetingDayOfWeek,
    }),

  reassign: (groupId: string, hostPersonId: string, leaderPersonId: string | null) =>
    api.put<void>(`/api/family-groups/${groupId}/assignment`, { hostPersonId, leaderPersonId }),

  setStatus: (groupId: string, isActive: boolean) =>
    api.put<void>(`/api/family-groups/${groupId}/status`, { isActive }),
}
