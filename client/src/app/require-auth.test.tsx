import { screen } from '@testing-library/react'
import { RouterProvider, createMemoryRouter } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { renderWithProviders } from '@/shared/testing'

import { RequireAuth } from './require-auth'

function stubSession(response: Response) {
  vi.stubGlobal(
    'fetch',
    vi.fn(() => Promise.resolve(response)),
  )
}

function renderAt(initialPath: string) {
  const router = createMemoryRouter(
    [
      {
        element: <RequireAuth />,
        children: [{ path: '/secret', element: <p>закрытая страница</p> }],
      },
      { path: '/login', element: <p>страница входа</p> },
    ],
    { initialEntries: [initialPath] },
  )

  return { router, ...renderWithProviders(<RouterProvider router={router} />) }
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('RequireAuth', () => {
  it('пускает вошедшего', async () => {
    stubSession(
      Response.json({
        id: '00000000-0000-0000-0000-000000000001',
        email: 'user@example.com',
        displayName: null,
      }),
    )

    renderAt('/secret')

    expect(await screen.findByText('закрытая страница')).toBeInTheDocument()
  })

  it('отправляет на вход, если сессии нет', async () => {
    stubSession(new Response('', { status: 401 }))

    renderAt('/secret')

    expect(await screen.findByText('страница входа')).toBeInTheDocument()
  })

  it('запоминает исходный путь, чтобы вернуть на него после входа', async () => {
    stubSession(new Response('', { status: 401 }))

    const { router } = renderAt('/secret?q=1')

    await screen.findByText('страница входа')

    expect(router.state.location.search).toBe(`?returnUrl=${encodeURIComponent('/secret?q=1')}`)
  })
})
