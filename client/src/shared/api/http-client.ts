/** Все запросы идут на относительный `/api`: в dev проксирует Vite, в проде — nginx. */
const API_BASE_URL = '/api'

/** Ответ с кодом вне 2xx. Тело сохраняется как есть — разбирать его решает вызывающий код. */
export class HttpError extends Error {
  readonly status: number
  readonly body: string

  constructor(status: number, statusText: string, body: string) {
    super(`Запрос завершился с кодом ${status} ${statusText}`)
    this.name = 'HttpError'
    this.status = status
    this.body = body
  }
}

/**
 * Единственная точка обращения к API. Сегменты `api` внутри слайсов вызывают её,
 * а не `fetch` напрямую — иначе базовый путь и разбор ошибок разъедутся по коду.
 *
 * Тип ответа не проверяется в рантайме: контракт задаёт бэкенд, и расхождение
 * ловится тестами, а не догадками на клиенте.
 */
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers)

  // FormData сам проставляет boundary в Content-Type — перебивать его нельзя.
  if (
    init?.body !== undefined &&
    !(init.body instanceof FormData) &&
    !headers.has('Content-Type')
  ) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers })

  if (!response.ok) {
    throw new HttpError(response.status, response.statusText, await response.text())
  }

  return (await response.json()) as T
}
