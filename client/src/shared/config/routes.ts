/**
 * Адреса экранов в одном месте.
 *
 * Space стоит в пути, а не в состоянии приложения: ссылка на документ должна открывать
 * именно его и после входа заново, а идентификатор арендатора обязан приходить из адреса,
 * который проверяется на членство, — не из того, что клиент хранит у себя.
 */
export const routes = {
  login: '/login',
  /* Профиль тоже внутри space: сайдбар с ним никуда не девается, а адрес без space
     заставил бы гадать, куда возвращать пользователя после закрытия страницы. */
  profile: (spaceId: string) => `/s/${spaceId}/profile`,
  space: (spaceId: string) => `/s/${spaceId}`,
  inbox: (spaceId: string) => `/s/${spaceId}/inbox`,
  library: (spaceId: string) => `/s/${spaceId}/library`,
  document: (spaceId: string, documentId: string) => `/s/${spaceId}/documents/${documentId}`,
  dialogs: (spaceId: string) => `/s/${spaceId}/dialogs`,
  contexts: (spaceId: string) => `/s/${spaceId}/contexts`,
  settings: (spaceId: string) => `/s/${spaceId}/settings`,
} as const
