import { useNavigate } from 'react-router'

import { Button, EmptyState } from '@/shared/ui'

/**
 * «Страница недоступна» — один и тот же экран и для чужого space, и для несуществующего.
 *
 * Текст не раскрывает, существует ли запрошенный space: разные ответы на «нет доступа» и
 * «не найдено» превращают адресную строку в способ перебирать чужие архивы.
 */
export function ForbiddenPage() {
  const navigate = useNavigate()

  return (
    <main className="bg-page flex min-h-screen items-center justify-center px-6">
      <EmptyState
        tone="neutral"
        icon="shield-alert"
        title="Страница недоступна"
        description="У вашей учётной записи нет доступа к запрошенному содержимому."
        actions={
          <Button
            onClick={() => {
              void navigate('/', { replace: true })
            }}
          >
            Вернуться в свой space
          </Button>
        }
      />
    </main>
  )
}
