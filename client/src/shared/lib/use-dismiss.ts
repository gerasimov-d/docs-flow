import { useEffect } from 'react'
import type { RefObject } from 'react'

/**
 * Закрывает всплывающую панель по клику мимо неё и по Escape.
 *
 * Оба способа обязательны и оба — про разных людей: мышью панель закрывают кликом в пустоту,
 * с клавиатуры — Escape, и панель, которую нельзя убрать без мыши, оказывается ловушкой.
 */
export function useDismiss(
  ref: RefObject<HTMLElement | null>,
  open: boolean,
  onDismiss: () => void,
): void {
  useEffect(() => {
    if (!open) {
      return
    }

    const onPointerDown = (event: PointerEvent) => {
      if (!(event.target instanceof Node) || ref.current?.contains(event.target) === true) {
        return
      }

      onDismiss()
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onDismiss()
      }
    }

    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)

    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [ref, open, onDismiss])
}
