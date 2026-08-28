import { api, setSession } from './client'
import type {
  AuthSession,
  CaptureContext,
  CreatedResource,
  GeneralServiceReport,
  MyCapabilities,
  PersonOption,
  ServiceSessionSummary,
  ServiceTypeOption,
  SessionTitheDetail,
  SocietyMembers,
  SundaySchoolReport,
} from './types'

export const auth = {
  async login(email: string, password: string) {
    const session = await api.post<AuthSession>('/api/auth/login', { email, password })
    setSession(session)
    return session
  },
  logout() {
    setSession(null)
  },
}

export const me = {
  capabilities: (signal?: AbortSignal) =>
    api.get<MyCapabilities>('/api/me/capabilities', signal),
}

export const directory = {
  persons: (search?: string, signal?: AbortSignal) =>
    api.get<PersonOption[]>(
      `/api/persons${search ? `?search=${encodeURIComponent(search)}` : ''}`,
      signal,
    ),
  serviceTypes: (signal?: AbortSignal) =>
    api.get<ServiceTypeOption[]>('/api/service-types', signal),
  societyMembers: (societyId: string, signal?: AbortSignal) =>
    api.get<SocietyMembers>(`/api/societies/${societyId}/members`, signal),
}

export const sundaySchool = {
  captureContext: (signal?: AbortSignal) =>
    api.get<CaptureContext>('/api/sunday-school/capture-context', signal),
  submitReport: (report: SundaySchoolReport) =>
    api.post<CreatedResource>('/api/sunday-school/reports', report),
}

export const services = {
  submitGeneralReport: (report: GeneralServiceReport) =>
    api.post<CreatedResource>('/api/service-sessions/general', report),
  recordTithe: (sessionId: string, personId: string, amount: number) =>
    api.post<CreatedResource>(`/api/service-sessions/${sessionId}/tithes`, { personId, amount }),
  sessionTithes: (sessionId: string, signal?: AbortSignal) =>
    api.get<SessionTitheDetail>(`/api/service-sessions/${sessionId}/tithes`, signal),
  history: (signal?: AbortSignal) =>
    api.get<ServiceSessionSummary[]>('/api/service-sessions', signal),
}
