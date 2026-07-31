import { cn, plural } from '@/shared/lib'

import type { ProcessingState } from '../model/document'

/**
 * Три подачи одного и того же состояния из раздела «Варианты подачи»:
 *
 * - `stepper` — сегменты по стадиям: видно, сколько пройдено и сколько осталось;
 * - `bar` — одна полоса: компактнее в длинном списке, но структуру конвейера не показывает;
 * - `stages` — стадии словами: честнее всего, когда оценка времени ненадёжна.
 */
export type ProgressVariant = 'stepper' | 'bar' | 'stages'

interface ProcessingProgressProps {
  processing: ProcessingState
  /** Упавшая стадия остаётся на своём месте и окрашивается, а не исчезает из конвейера. */
  failed?: boolean
  variant?: ProgressVariant
  className?: string
}

export function ProcessingProgress({
  processing,
  failed = false,
  variant = 'stepper',
  className,
}: ProcessingProgressProps) {
  const { stages, currentStage, note } = processing
  const caption = note ?? describe(processing, failed)

  return (
    <div className={className}>
      {variant === 'stepper' && (
        <div className="flex gap-[3px]">
          {stages.map((stage, index) => (
            <span
              key={stage}
              className={cn(
                'h-1.5 flex-1 rounded-full',
                segmentClass(index + 1, currentStage, failed),
              )}
            />
          ))}
        </div>
      )}

      {variant === 'bar' && (
        <div className="bg-line h-1.5 overflow-hidden rounded-full">
          <span
            className={cn('block h-full', failed ? 'bg-danger' : 'bg-accent')}
            style={{ width: `${String(Math.round((currentStage / stages.length) * 100))}%` }}
          />
        </div>
      )}

      {variant === 'stages' && (
        <div className="flex flex-wrap gap-1.5">
          {stages.map((stage, index) => (
            <span
              key={stage}
              className={cn(
                'rounded-full px-[9px] py-0.5 text-[11px] font-semibold',
                stageChipClass(index + 1, currentStage, failed),
              )}
            >
              {index + 1 === currentStage && !failed ? `${stage}…` : stage}
            </span>
          ))}
        </div>
      )}

      {caption !== null && (
        <p
          className={cn(
            'mt-[7px] text-[11px]',
            failed ? 'text-danger-deep' : 'text-ink-muted',
            // Стадии словами уже названы чипами — дублировать их подписью незачем.
            variant === 'stages' && 'sr-only',
          )}
        >
          {caption}
        </p>
      )}
    </div>
  )
}

function segmentClass(position: number, current: number, failed: boolean): string {
  if (position < current) {
    return 'bg-sage'
  }

  if (position > current) {
    return 'bg-line'
  }

  return failed ? 'bg-danger' : 'bg-accent animate-beat'
}

function stageChipClass(position: number, current: number, failed: boolean): string {
  if (position < current) {
    return 'bg-sage-tint text-sage-ink'
  }

  if (position > current) {
    return 'bg-muted text-ink-subtle'
  }

  return failed
    ? 'bg-danger-tint font-bold text-danger-deep'
    : 'bg-accent-tint font-bold text-accent-deep'
}

function describe(processing: ProcessingState, failed: boolean): string | null {
  const { stages, currentStage, etaMinutes } = processing
  const stage = stages.at(currentStage - 1)

  if (stage === undefined) {
    return null
  }

  if (failed) {
    return `Упала стадия «${stage}».`
  }

  const position = `стадия ${String(currentStage)} из ${String(stages.length)}`
  const eta =
    etaMinutes === null
      ? ''
      : ` · осталось ~${String(etaMinutes)} ${plural(etaMinutes, 'минута', 'минуты', 'минут')}`

  return `${stage} · ${position}${eta}`
}
