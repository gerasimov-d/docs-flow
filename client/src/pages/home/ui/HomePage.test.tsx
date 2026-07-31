import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'

import { renderWithProviders } from '@/shared/testing'

import { HomePage } from './HomePage'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('HomePage', () => {
  it('показывает вошедшего пользователя', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          Response.json({
            id: '00000000-0000-0000-0000-000000000001',
            email: 'user@example.com',
            displayName: 'Пользователь',
          }),
        ),
      ),
    )

    renderWithProviders(<HomePage />)

    expect(screen.getByRole('heading', { name: 'DocsFlow' })).toBeInTheDocument()
    expect(await screen.findByText('Пользователь')).toBeInTheDocument()
  })
})
