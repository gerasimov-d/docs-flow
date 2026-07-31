import type { ReactNode } from 'react'

import { useProcessingCount } from '@/entities/document'
import type { Space } from '@/entities/space'
import { SpaceSwitcher } from '@/features/switch-space'
import { pluralize } from '@/shared/lib'

import { AppSidebar } from './AppSidebar'

interface AppShellProps {
  space: Space
  /** Действия справа в шапке: «Найти», «Загрузить», «Новый контекст». */
  actions?: ReactNode
  /** Крошки или пояснение слева от действий. Заменяют счётчик обработки, когда он не к месту. */
  lead?: ReactNode
  children: ReactNode
}

/**
 * Оболочка приложения: постоянный сайдбар и шапка с переключателем space.
 *
 * Переключатель виден со всех экранов и стоит первым в шапке намеренно — это индикатор
 * границы данных. Пользователь должен видеть, в чьём архиве он ищет, до того как задаст вопрос.
 */
export function AppShell({ space, actions, lead, children }: AppShellProps) {
  const processing = useProcessingCount(space.id)

  return (
    <div className="bg-page text-ink flex h-screen">
      <AppSidebar space={space} />

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="border-line flex h-16 shrink-0 items-center gap-3.5 border-b px-7">
          <SpaceSwitcher current={space} />

          {lead ??
            (processing > 0 && (
              <span className="text-ink-subtle text-[12px]">
                {pluralize(processing, 'документ', 'документа', 'документов')} в обработке
              </span>
            ))}

          <div className="flex-1" />
          {actions}
        </header>

        <main className="flex min-h-0 flex-1 flex-col">{children}</main>
      </div>
    </div>
  )
}
