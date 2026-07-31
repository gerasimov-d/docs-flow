import { useState } from 'react'
import { useNavigate } from 'react-router'

import { countUndatedDocuments, useLibrary } from '@/entities/document'
import type { Space } from '@/entities/space'
import { DocumentFilterBar, useDocumentFilters } from '@/features/filter-documents'
import { UploadDialog } from '@/features/upload-documents'
import { routes } from '@/shared/config'
import { pluralize } from '@/shared/lib'
import { Button, EmptyState, Icon, Segmented } from '@/shared/ui'
import { AppShell } from '@/widgets/app-shell'
import { LibraryView } from '@/widgets/library-view'
import type { LibraryLayout } from '@/widgets/library-view'

interface LibraryPageProps {
  space: Space
}

const layoutOptions = [
  { value: 'table' as const, label: 'Список', icon: 'list' as const },
  { value: 'grid' as const, label: 'Сетка', icon: 'grid-2x2' as const },
]

/**
 * Библиотека — весь архив, отсортированный по дате самого документа.
 *
 * Фильтры здесь те же, что под полем запроса в диалоге: человек, отобравший «медицину за
 * 2026», не должен собирать тот же отбор заново, перейдя к вопросу.
 */
export function LibraryPage({ space }: LibraryPageProps) {
  const [layout, setLayout] = useState<LibraryLayout>('table')
  const [uploadOpen, setUploadOpen] = useState(false)
  const navigate = useNavigate()
  const { filters, query, activeCount, set, reset } = useDocumentFilters()
  const { documents, isLoading } = useLibrary(space.id, query)

  const periodFiltered = query.from !== undefined || query.to !== undefined
  const undated = countUndatedDocuments()

  return (
    <AppShell
      space={space}
      actions={
        <>
          <Button
            icon="search"
            onClick={() => {
              void navigate(routes.dialogs(space.id))
            }}
          >
            Найти
          </Button>
          <Button
            variant="primary"
            icon="upload"
            onClick={() => {
              setUploadOpen(true)
            }}
          >
            Загрузить
          </Button>
        </>
      }
    >
      <div className="flex min-h-0 flex-1 flex-col gap-4 px-7 py-[26px]">
        <div className="flex items-end gap-4">
          <div className="flex-1">
            <h1 className="font-display text-[28px] leading-[1.12] font-extrabold tracking-[-0.015em]">
              Библиотека
            </h1>
            {/* Название space не склоняется — оно рядом, в переключателе шапки, и повторять
                его здесь значило бы выбирать между «в Личный архив» и морфологией имён. */}
            <p className="text-ink-muted mt-1 text-[13px]">
              {pluralize(documents.length, 'документ', 'документа', 'документов')} · сортировка по
              дате документа
            </p>
          </div>
          <Segmented
            label="Вид библиотеки"
            tone="surface"
            options={layoutOptions}
            value={layout}
            onChange={setLayout}
          />
        </div>

        <div className="flex flex-col gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <DocumentFilterBar spaceId={space.id} filters={filters} onChange={set} />
            <span className="flex-1" />
            {activeCount > 0 && (
              <Button size="sm" onClick={reset}>
                Снять все фильтры
              </Button>
            )}
            <span className="text-ink-muted text-[13px]">Найдено {documents.length}</span>
          </div>

          {periodFiltered && undated > 0 && (
            <p className="text-ink-muted flex items-center gap-2 pl-0.5 text-[12px]">
              <Icon name="info" className="text-ink-subtle text-[14px]" />
              {pluralize(undated, 'документ', 'документа', 'документов')} без указанной даты в
              период не попадают.
              <button
                type="button"
                className="text-accent-deep cursor-pointer font-semibold underline underline-offset-[3px]"
                onClick={() => {
                  set('period', '')
                }}
              >
                Показать их
              </button>
            </p>
          )}
        </div>

        {!isLoading && documents.length === 0 ? (
          <EmptyState
            className="border-line bg-surface flex-1 rounded-2xl border"
            tone="neutral"
            icon="folder-open"
            title="Под фильтры ничего не подошло"
            description={
              activeCount === 0
                ? 'В этом space пока нет документов. Загрузите первый — он появится здесь после обработки.'
                : `Активны фильтры (${String(activeCount)}). Снимите часть из них, чтобы увидеть больше.`
            }
            actions={
              activeCount > 0 ? (
                <Button onClick={reset}>Снять все фильтры</Button>
              ) : (
                <Button
                  variant="primary"
                  icon="upload"
                  onClick={() => {
                    setUploadOpen(true)
                  }}
                >
                  Загрузить документы
                </Button>
              )
            }
          />
        ) : (
          <LibraryView spaceId={space.id} documents={documents} layout={layout} />
        )}
      </div>

      <UploadDialog
        spaceId={space.id}
        spaceName={space.name}
        open={uploadOpen}
        onClose={() => {
          setUploadOpen(false)
        }}
      />
    </AppShell>
  )
}
