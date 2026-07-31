import { Link } from 'react-router'

import { useContextUsage, useContexts } from '@/entities/context'
import type { Space } from '@/entities/space'
import { CreateContextButton, DeleteContextButton } from '@/features/manage-contexts'
import { routes } from '@/shared/config'
import { formatDate, pluralize } from '@/shared/lib'
import { EmptyState, Icon } from '@/shared/ui'
import { AppShell } from '@/widgets/app-shell'

interface ContextsPageProps {
  space: Space
}

/**
 * Контексты — плоский список тематических меток внутри space.
 *
 * Дерева нет намеренно: как только у меток появляется вложенность, документ начинают
 * «класть в папку», а контекст перестаёт быть тем, чем является, — необязательным признаком.
 */
export function ContextsPage({ space }: ContextsPageProps) {
  const { contexts, isLoading } = useContexts(space.id)
  const usage = useContextUsage(space.id)

  return (
    <AppShell space={space} actions={<CreateContextButton spaceId={space.id} />}>
      <div className="flex min-h-0 flex-1 flex-col gap-[18px] overflow-y-auto px-7 py-[26px]">
        <div>
          <h1 className="font-display text-[28px] leading-[1.12] font-extrabold tracking-[-0.015em]">
            Контексты
          </h1>
          <p className="text-ink-muted mt-1 max-w-[640px] text-[13px] text-pretty">
            Тематические группы внутри space. Плоский список без вложенности; у документа один
            контекст, и он необязателен.
          </p>
        </div>

        {!isLoading && contexts.length === 0 ? (
          <EmptyState
            className="border-line bg-surface rounded-2xl border py-16"
            tone="neutral"
            icon="tag"
            title="Контекстов пока нет"
            description="Контекст — необязательная метка вроде «медицина» или «авто». Документы прекрасно живут и без неё, но с ней их проще отбирать."
          />
        ) : (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] content-start gap-3.5">
            {contexts.map((context) => {
              const stats = usage[context.name] ?? { documentCount: 0, lastDocumentDate: null }

              return (
                <article
                  key={context.id}
                  className="border-line bg-surface flex flex-col gap-3 rounded-2xl border px-[18px] py-4"
                >
                  <div className="flex items-center gap-[11px]">
                    <span className="bg-muted text-ink-muted flex size-[34px] items-center justify-center rounded-full">
                      <Icon name="tag" className="text-[17px]" />
                    </span>
                    <h2 className="flex-1 text-[16px] font-bold">{context.name}</h2>
                    <DeleteContextButton
                      spaceId={space.id}
                      contextId={context.id}
                      contextName={context.name}
                      documentCount={stats.documentCount}
                    />
                  </div>

                  <p className="text-ink-muted text-[13px]">
                    {stats.documentCount === 0
                      ? 'Пока нет документов'
                      : `${pluralize(stats.documentCount, 'документ', 'документа', 'документов')}${
                          stats.lastDocumentDate === null
                            ? ''
                            : ` · последний ${formatDate(stats.lastDocumentDate)}`
                        }`}
                  </p>

                  <Link
                    to={routes.library(space.id)}
                    className="text-accent-deep hover:text-accent inline-flex items-center gap-1.5 text-[13px] font-bold no-underline"
                  >
                    Открыть документы
                    <Icon name="arrow-right" className="text-[14px]" />
                  </Link>
                </article>
              )
            })}
          </div>
        )}

        <div className="border-line-dashed bg-raised flex items-center gap-3 rounded-2xl border border-dashed px-[18px] py-[15px]">
          <span className="bg-page text-ink-subtle flex size-8 shrink-0 items-center justify-center rounded-full">
            <Icon name="folder-open" className="text-[16px]" />
          </span>
          <div className="flex-1">
            <div className="text-[14px] font-bold">Без контекста</div>
            <div className="text-ink-muted text-[12px]">
              Видны в библиотеке и участвуют в поиске наравне с остальными.
            </div>
          </div>
          <Link
            to={routes.library(space.id)}
            className="text-accent-deep hover:text-accent inline-flex items-center gap-1.5 text-[13px] font-bold no-underline"
          >
            Открыть
            <Icon name="arrow-right" className="text-[14px]" />
          </Link>
        </div>
      </div>
    </AppShell>
  )
}
