import { apiFetch, mockRequest } from '@/shared/api'

/** Роль в space. Ролей ровно две: участник — полноправный соавтор, владелец правит доступ и имя. */
export type SpaceRole = 'owner' | 'member'

/** Space глазами вошедшего: сам space плюс его роль в нём. Ответ `GET /api/spaces`. */
export interface Space {
  id: string
  name: string
  role: SpaceRole
  createdAt: string
}

/** Участник space. Состав видят только свои — наружу он не раскрывается. */
export interface SpaceMember {
  userId: string
  email: string
  displayName: string | null
  role: SpaceRole
}

export function getSpaces(): Promise<Space[]> {
  return apiFetch<Space[]>('/spaces')
}

export function getSpaceMembers(spaceId: string): Promise<SpaceMember[]> {
  return apiFetch<SpaceMember[]>(`/spaces/${spaceId}/members`)
}

export function createSpace(name: string): Promise<Space> {
  return apiFetch<Space>('/spaces', { method: 'POST', body: JSON.stringify({ name }) })
}

/** Переименование. Владельцу — 204, участнику — 403: право на имя есть только у владельца. */
export function renameSpace(spaceId: string, name: string): Promise<void> {
  return apiFetch<void>(`/spaces/${spaceId}`, {
    method: 'PATCH',
    body: JSON.stringify({ name }),
  })
}

/**
 * Отзыв доступа. Идемпотентен: отзыв у того, кто и так не состоит, оставляет ровно то
 * состояние, которого просили, и потому не ошибка.
 */
export function removeSpaceMember(spaceId: string, userId: string): Promise<void> {
  return apiFetch<void>(`/spaces/${spaceId}/members/${userId}`, { method: 'DELETE' })
}

/**
 * Приглашение по адресу почты.
 *
 * TODO(бэкенд): `POST /api/spaces/{spaceId}/invitations` с телом `{ email }`. Существующий
 * `POST /members` принимает идентификатор пользователя, а не адрес, и для формы из дизайна
 * не годится: чтобы им воспользоваться, клиенту пришлось бы сначала найти пользователя по
 * почте — то есть получить способ проверять, заведён ли в сервисе такой аккаунт.
 *
 * Ответ обязан быть одинаковым независимо от того, зарегистрирован адрес или нет.
 */
export function inviteSpaceMember(spaceId: string, email: string): Promise<void> {
  void spaceId
  void email

  return mockRequest<void>(undefined, 400)
}

/**
 * Мягкое удаление space.
 *
 * TODO(бэкенд): `DELETE /api/spaces/{spaceId}` — скрыть и вычистить содержимое по истечении
 * 30 дней. До этого срока space должен возвращаться операцией восстановления.
 */
export function deleteSpace(spaceId: string): Promise<void> {
  void spaceId

  return mockRequest<void>(undefined, 400)
}
