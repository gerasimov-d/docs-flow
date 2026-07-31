import { screen } from '@testing-library/react'
import { RouterProvider, createMemoryRouter } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { renderWithProviders } from '@/shared/testing'

import { SpaceLayout } from './space-layout'

const own = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Личный архив',
  role: 'owner',
  createdAt: '2025-01-04T10:00:00Z',
}

function stubSpaces() {
  vi.stubGlobal(
    'fetch',
    vi.fn(() => Promise.resolve(Response.json([own]))),
  )
}

function renderAt(path: string) {
  const router = createMemoryRouter(
    [
      {
        path: '/s/:spaceId',
        element: <SpaceLayout />,
        children: [{ index: true, element: <p>содержимое архива</p> }],
      },
    ],
    { initialEntries: [path] },
  )

  return renderWithProviders(<RouterProvider router={router} />)
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('SpaceLayout', () => {
  it('пускает в space, где пользователь состоит', async () => {
    stubSpaces()

    renderAt(`/s/${own.id}`)

    expect(await screen.findByText('содержимое архива')).toBeInTheDocument()
  })

  /*
   * Ключевой инвариант изоляции: чужой space и несуществующий выглядят одинаково.
   * Если ответы начнут отличаться, адресная строка станет способом перебирать чужие архивы.
   */
  it('чужой space показывает то же, что и несуществующий', async () => {
    stubSpaces()

    renderAt('/s/22222222-2222-2222-2222-222222222222')

    expect(await screen.findByText('Страница недоступна')).toBeInTheDocument()
    expect(screen.queryByText('содержимое архива')).not.toBeInTheDocument()
  })

  it('не раскрывает в тексте, существует ли запрошенный space', async () => {
    stubSpaces()

    renderAt('/s/33333333-3333-3333-3333-333333333333')

    const explanation = await screen.findByText(/нет доступа к запрошенному содержимому/)

    expect(explanation.textContent).not.toMatch(/не найден|не существует|удал/i)
  })
})
