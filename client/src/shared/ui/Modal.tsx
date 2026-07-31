import { useEffect, useRef } from 'react'
import type { ReactNode } from 'react'

import { cn } from '@/shared/lib'

import { IconButton } from './IconButton'

interface ModalProps {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  /** Ширина по дизайну: диалог загрузки — 660px, подтверждения — уже. */
  className?: string
  children: ReactNode
  footer?: ReactNode
}

/**
 * Модальное окно поверх приложения.
 *
 * Построено на `<dialog>`, а не на `div` с `position: fixed`: браузер сам даёт модальность,
 * ловушку фокуса, закрытие по Escape и вывод в top layer. Своя реализация этого набора —
 * это ровно тот код, который потом годами чинят по частям.
 */
export function Modal({
  open,
  onClose,
  title,
  description,
  className,
  children,
  footer,
}: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current

    if (dialog === null) {
      return
    }

    if (open && !dialog.open) {
      dialog.showModal()
    }

    if (!open && dialog.open) {
      dialog.close()
    }
  }, [open])

  return (
    <dialog
      ref={ref}
      aria-labelledby="modal-title"
      onClose={onClose}
      // Клик мимо содержимого закрывает окно: цель события — сам dialog только тогда,
      // когда попали в подложку, потому что внутренности перехватывают клик сами.
      onClick={(event) => {
        if (event.target === ref.current) {
          onClose()
        }
      }}
      className={cn(
        'bg-surface text-ink shadow-float m-auto w-[660px] max-w-[calc(100vw-2rem)] rounded-4xl p-0',
        'backdrop:bg-[rgb(46_43_37/0.42)]',
        className,
      )}
    >
      <div className="flex flex-col gap-[18px] px-7 pt-[26px] pb-[22px]">
        <div className="flex items-start gap-3.5">
          <div className="flex-1">
            <h2
              id="modal-title"
              className="font-display text-[25px] leading-[1.12] font-extrabold tracking-[-0.015em]"
            >
              {title}
            </h2>
            {description !== undefined && (
              <p className="text-ink-muted mt-1 text-[13px]">{description}</p>
            )}
          </div>
          <IconButton name="x" label="Закрыть" onClick={onClose} />
        </div>

        {children}

        {footer !== undefined && (
          <>
            <div className="bg-line h-px" />
            <div className="flex items-center gap-3">{footer}</div>
          </>
        )}
      </div>
    </dialog>
  )
}
