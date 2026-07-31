import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

import { LogoutButton } from './LogoutButton'

describe('LogoutButton', () => {
  it('отправляет POST-форму на эндпоинт выхода', () => {
    render(<LogoutButton />)

    const form = screen.getByRole('button', { name: 'Выйти' }).closest('form')

    // Не fetch и не GET-ссылка: сервер отвечает редиректом на Keycloak, а выход меняет состояние.
    expect(form).toHaveAttribute('method', 'post')
    expect(form).toHaveAttribute('action', '/api/auth/logout')
  })
})
