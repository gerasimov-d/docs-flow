// Матчеры вида `toBeInTheDocument` для проверок над DOM.
import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// Тесты не должны видеть DOM, оставшийся от предыдущего теста.
afterEach(() => {
  cleanup()
})
