import { useDialogHistory } from '@/entities/dialog'
import { cn, pluralize } from '@/shared/lib'
import { Button, Icon, SectionLabel } from '@/shared/ui'

interface DialogHistoryProps {
  spaceId: string
  activeId: string | null
  onSelect: (question: string) => void
  onReset: () => void
}

/**
 * История вопросов текущей сессии.
 *
 * Живёт внутри space и обрывается при переключении — об этом сказано прямо внизу колонки.
 * Иначе пользователь решит, что диалог общий, и удивится, куда делись вопросы.
 */
export function DialogHistory({ spaceId, activeId, onSelect, onReset }: DialogHistoryProps) {
  const history = useDialogHistory(spaceId)

  return (
    <aside className="border-line bg-panel flex w-[266px] shrink-0 flex-col border-r px-3.5 py-[18px]">
      <Button variant="primary" icon="plus" className="w-full" onClick={onReset}>
        Новый вопрос
      </Button>

      <SectionLabel className="px-2.5 pt-5 pb-2">Эта сессия</SectionLabel>

      <ol className="flex flex-col gap-[3px]">
        {history.map((entry) => (
          <li key={entry.id}>
            <button
              type="button"
              onClick={() => {
                onSelect(entry.question)
              }}
              className={cn(
                'flex w-full cursor-pointer flex-col gap-[3px] rounded-2xl px-3 py-2.5 text-left transition-colors',
                entry.id === activeId
                  ? 'border-line bg-surface border'
                  : 'hover:bg-surface/60 border border-transparent',
              )}
            >
              <span
                className={cn(
                  'text-[13px] leading-[1.35]',
                  entry.id === activeId ? 'font-bold' : 'text-ink-dim font-semibold',
                )}
              >
                {entry.question}
              </span>
              <span className="text-ink-subtle text-[11px]">
                {entry.sourceCount === 0
                  ? 'ответа нет'
                  : pluralize(entry.sourceCount, 'источник', 'источника', 'источников')}
              </span>
            </button>
          </li>
        ))}
      </ol>

      <div className="flex-1" />

      <p className="bg-raised text-ink-muted flex items-start gap-[9px] rounded-2xl px-3 py-[11px] text-[11px] leading-[1.5]">
        <Icon name="info" className="text-ink-subtle mt-px text-[14px]" />
        История живёт в рамках сессии. Смена space обрывает диалог.
      </p>
    </aside>
  )
}
