import { apiFetch, HttpError } from '@/shared/api'

/** Профиль вошедшего пользователя — ответ `GET /api/me`. Ролей в системе нет, поэтому их здесь нет. */
export interface SessionUser {
  id: string
  email: string
  displayName: string | null
}

/**
 * Читает текущую сессию.
 *
 * 401 — это не сбой, а штатный ответ «не вошёл»: API отвечает им вместо редиректа на Keycloak.
 * Поэтому он превращается в `null`, а исключением остаются только настоящие ошибки.
 */
export async function getSession(): Promise<SessionUser | null> {
  try {
    return await apiFetch<SessionUser>('/me')
  } catch (error) {
    if (error instanceof HttpError && error.status === 401) {
      return null
    }

    throw error
  }
}
