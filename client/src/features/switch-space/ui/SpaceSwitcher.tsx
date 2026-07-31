import { useRef, useState } from 'react'
import { useNavigate } from 'react-router'

import { useProcessingCount } from '@/entities/document'
import { useSpaces } from '@/entities/space'
import type { Space } from '@/entities/space'
import { routes } from '@/shared/config'
import { cn, pluralize, useDismiss } from '@/shared/lib'
import { Icon, SectionLabel, SpaceMark } from '@/shared/ui'

interface SpaceSwitcherProps {
  current: Space
}

/**
 * Переключатель space — он же индикатор границы: в шапке всегда видно, чей архив открыт.
 *
 * Переключение — это переход по адресу, а не смена состояния: space стоит в пути, поэтому
 * ссылку на документ можно послать себе же, и она откроет тот же space. Заодно исчезает
 * класс ошибок, когда экран уже перерисовался под новый space, а запрос ушёл со старым.
 */
export function SpaceSwitcher({ current }: SpaceSwitcherProps) {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const { spaces } = useSpaces()
  const processing = useProcessingCount(current.id)

  useDismiss(containerRef, open, () => {
    setOpen(false)
  })

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => {
          setOpen((previous) => !previous)
        }}
        className={cn(
          'bg-surface flex cursor-pointer items-center gap-[9px] rounded-full border py-[7px] pr-[13px] pl-[9px] transition-colors',
          open ? 'border-accent shadow-pop' : 'border-line hover:border-line-dashed',
        )}
      >
        <SpaceMark name={current.name} />
        <span className="text-[14px] font-bold">{current.name}</span>
        {processing > 0 && (
          <span
            className="animate-beat bg-accent size-[7px] rounded-full"
            title={`${pluralize(processing, 'документ', 'документа', 'документов')} в обработке`}
          />
        )}
        <Icon name={open ? 'chevron-up' : 'chevron-down'} className="text-ink-muted text-[16px]" />
      </button>

      {open && (
        <div
          role="menu"
          className="border-line bg-surface shadow-float absolute top-[calc(100%+8px)] left-0 z-30 flex w-[352px] flex-col gap-0.5 rounded-2xl border p-2"
        >
          <SectionLabel className="px-3 pt-2 pb-1.5">Ваши space</SectionLabel>

          {spaces.map((space) => {
            const selected = space.id === current.id

            return (
              <button
                key={space.id}
                type="button"
                role="menuitem"
                onClick={() => {
                  setOpen(false)
                  void navigate(routes.inbox(space.id))
                }}
                className={cn(
                  'flex cursor-pointer items-center gap-[11px] rounded-2xl px-3 py-2.5 text-left transition-colors',
                  selected ? 'bg-accent-tint' : 'hover:bg-muted',
                )}
              >
                <SpaceMark
                  name={space.name}
                  tone={selected ? 'accent' : 'sage'}
                  className="size-[30px] text-[13px]"
                />
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-[14px] font-bold">{space.name}</span>
                  <span
                    className={cn(
                      'block text-[11px]',
                      selected ? 'text-accent-deep' : 'text-ink-muted',
                    )}
                  >
                    {space.role === 'owner' ? 'Владелец' : 'Участник'}
                  </span>
                </span>
                {selected && <Icon name="check" className="text-accent-deep text-[16px]" />}
              </button>
            )
          })}

          <div className="bg-line-soft mx-3 my-1.5 h-px" />

          <button
            type="button"
            role="menuitem"
            // TODO(бэкенд есть): подключить к `POST /api/spaces` вместе с формой имени —
            // сейчас создание space доступно только через API, экрана для него в дизайне нет.
            disabled
            className="flex items-center gap-[11px] rounded-2xl px-3 py-2.5 text-left opacity-45"
          >
            <span className="bg-muted text-ink-muted flex size-[30px] items-center justify-center rounded-lg">
              <Icon name="plus" className="text-[15px]" />
            </span>
            <span className="flex-1 text-[14px] font-bold">Создать space</span>
          </button>

          <p className="text-ink-subtle flex items-start gap-2 px-3 pt-2 pb-1.5 text-[11px] leading-[1.5]">
            <Icon name="info" className="text-ink-faint mt-0.5 text-[13px]" />
            Переключение меняет инбокс, библиотеку, фильтры, поиск и историю вопросов целиком.
          </p>
        </div>
      )}
    </div>
  )
}
