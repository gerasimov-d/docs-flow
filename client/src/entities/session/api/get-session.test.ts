import { afterEach, describe, expect, it, vi } from 'vitest'

import { getSession } from './get-session'

function stubFetch(response: Response) {
  vi.stubGlobal(
    'fetch',
    vi.fn(() => Promise.resolve(response)),
  )
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('getSession', () => {
  it('отдаёт профиль, когда пользователь вошёл', async () => {
    stubFetch(
      Response.json({
        id: '00000000-0000-0000-0000-000000000001',
        email: 'user@example.com',
        displayName: 'Пользователь',
      }),
    )

    await expect(getSession()).resolves.toEqual({
      id: '00000000-0000-0000-0000-000000000001',
      email: 'user@example.com',
      displayName: 'Пользователь',
    })
  })

  it('превращает 401 в отсутствие сессии, а не в ошибку', async () => {
    stubFetch(new Response('', { status: 401 }))

    await expect(getSession()).resolves.toBeNull()
  })

  it('пробрасывает остальные ошибки — их нельзя принять за «не вошёл»', async () => {
    stubFetch(new Response('', { status: 500 }))

    await expect(getSession()).rejects.toThrow()
  })
})
