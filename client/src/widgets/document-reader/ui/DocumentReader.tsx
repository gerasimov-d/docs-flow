import { useState } from 'react'

import type { DocumentDetail } from '@/entities/document'
import { cn } from '@/shared/lib'
import { Icon, IconButton, SectionLabel } from '@/shared/ui'

interface DocumentReaderProps {
  document: DocumentDetail
  /** Страница, на которой документ открыли по цитате. */
  page: number
}

const zoomSteps = [80, 100, 120, 150, 200]

/**
 * Оригинал и распознанный текст рядом, а не вместо друг друга.
 *
 * Это следствие принципа «оригинал — источник истины»: текст извлечён машиной и может врать,
 * поэтому проверить его по скану должно быть можно не уходя с экрана. Правки текста руками
 * нет намеренно — иначе появится третья версия документа, которую никто не сверял.
 */
export function DocumentReader({ document, page }: DocumentReaderProps) {
  const [currentPage, setCurrentPage] = useState(page)
  const [zoom, setZoom] = useState(120)
  const pageCount = document.pageCount ?? 1

  return (
    <div className="flex min-h-0 flex-1 gap-4">
      <section className="border-line bg-surface flex min-w-0 flex-1 flex-col overflow-hidden rounded-2xl border">
        <header className="border-line bg-raised flex h-[46px] shrink-0 items-center gap-2.5 border-b px-3.5">
          <SectionLabel className="flex-1">Оригинал</SectionLabel>

          <IconButton
            name="chevron-left"
            label="Предыдущая страница"
            size="sm"
            disabled={currentPage <= 1}
            onClick={() => {
              setCurrentPage((value) => Math.max(1, value - 1))
            }}
          />
          <span className="text-[13px] font-bold">
            {currentPage} / {pageCount}
          </span>
          <IconButton
            name="chevron-right"
            label="Следующая страница"
            size="sm"
            disabled={currentPage >= pageCount}
            onClick={() => {
              setCurrentPage((value) => Math.min(pageCount, value + 1))
            }}
          />

          <span className="bg-line mx-1 h-[18px] w-px" />

          <IconButton
            name="zoom-out"
            label="Уменьшить"
            size="sm"
            disabled={zoom === zoomSteps[0]}
            onClick={() => {
              setZoom((value) => zoomSteps.at(Math.max(0, zoomSteps.indexOf(value) - 1)) ?? value)
            }}
          />
          <span className="text-ink-muted w-10 text-center text-[12px] font-semibold">{zoom}%</span>
          <IconButton
            name="zoom-in"
            label="Увеличить"
            size="sm"
            disabled={zoom === zoomSteps.at(-1)}
            onClick={() => {
              setZoom(
                (value) =>
                  zoomSteps.at(Math.min(zoomSteps.length - 1, zoomSteps.indexOf(value) + 1)) ??
                  value,
              )
            }}
          />
        </header>

        <div className="flex min-h-0 flex-1">
          {pageCount > 1 && (
            <ol className="border-line bg-page flex w-[74px] shrink-0 flex-col gap-2 overflow-y-auto border-r px-2 py-2.5">
              {Array.from({ length: pageCount }, (_unused, index) => index + 1).map((number) => (
                <li key={number}>
                  <button
                    type="button"
                    aria-label={`Страница ${String(number)}`}
                    aria-current={number === currentPage}
                    onClick={() => {
                      setCurrentPage(number)
                    }}
                    className={cn(
                      'bg-muted h-[74px] w-full cursor-pointer rounded-md',
                      number === currentPage ? 'border-accent border-2' : 'border-line border',
                    )}
                  />
                </li>
              ))}
            </ol>
          )}

          <div className="bg-muted flex min-w-0 flex-1 items-start justify-center overflow-auto p-4">
            {/*
              Настоящего скана нет: файлы живут в объектном хранилище, а эндпоинта для их
              выдачи в API ещё не существует.
              TODO(бэкенд): `GET /api/spaces/{spaceId}/documents/{documentId}/pages/{page}` —
              отдавать изображение страницы по временной ссылке, не раскрывая адрес в S3.
            */}
            <div
              className="border-line bg-surface text-ink-faint relative flex aspect-[210/297] w-full max-w-[430px] flex-col items-center justify-center gap-3 rounded-lg border"
              style={{ zoom: zoom / 100 }}
            >
              <Icon name="file-text" className="text-[40px]" />
              <span className="text-[12px]">Страница {currentPage}</span>

              {currentPage === document.recognizedPage && (
                <span className="border-accent pointer-events-none absolute top-[41%] left-[9%] h-[8.5%] w-[74%] rounded border-2 bg-[color-mix(in_srgb,var(--color-accent)_20%,transparent)]" />
              )}
            </div>
          </div>
        </div>
      </section>

      <section className="border-line bg-surface flex w-[430px] shrink-0 flex-col overflow-hidden rounded-2xl border">
        <header className="border-line bg-raised flex h-[46px] shrink-0 items-center gap-2.5 border-b px-4">
          <SectionLabel className="flex-1">Распознанный текст</SectionLabel>
          <span className="bg-accent-tint text-accent-deep rounded-full px-2.5 py-[3px] text-[12px] font-bold">
            страница {document.recognizedPage}
          </span>
        </header>

        <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-[18px] py-4">
          <SectionLabel className="text-ink-faint">
            — страница {document.recognizedPage} —
          </SectionLabel>

          {document.recognizedParagraphs.length === 0 ? (
            <p className="text-ink-muted text-[13px]">
              Текст ещё не извлечён — документ не прошёл распознавание.
            </p>
          ) : (
            document.recognizedParagraphs.map((paragraph) => (
              <p
                key={paragraph.text}
                className={cn(
                  'text-ink-soft text-[14px] leading-[1.65] text-pretty',
                  paragraph.highlighted &&
                    'border-accent rounded-r-lg border-l-[3px] bg-[color-mix(in_srgb,var(--color-accent)_22%,transparent)] px-3 py-2',
                )}
              >
                {paragraph.text}
              </p>
            ))
          )}

          <div className="flex-1" />
          <div className="bg-line h-px" />
          <p className="text-ink-subtle flex items-center gap-[9px] text-[11px]">
            <Icon name="scan-text" className="text-ink-faint text-[14px]" />
            Текст извлечён автоматически и вручную не правится. Чтобы улучшить — обработайте заново.
          </p>
        </div>
      </section>
    </div>
  )
}
