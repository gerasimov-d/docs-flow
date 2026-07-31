import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { LoginButton } from './LoginButton'

describe('LoginButton', () => {
  it('ведёт на эндпоинт входа и передаёт адрес возврата', () => {
    render(<LoginButton returnUrl="/documents?q=счёт" />)

    expect(screen.getByRole('link', { name: 'Войти' })).toHaveAttribute(
      'href',
      `/api/auth/login?returnUrl=${encodeURIComponent('/documents?q=счёт')}`,
    )
  })
})
