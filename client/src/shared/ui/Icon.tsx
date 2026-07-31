import { cn } from '@/shared/lib'

import { glyphs } from './glyphs'
import type { IconName } from './glyphs'

interface IconProps {
  name: IconName
  /** Размер задаётся классами `size-*`; по умолчанию иконка занимает 1em строки. */
  className?: string
}

/**
 * Иконка интерфейса. Красится `currentColor`, поэтому цвет наследуется от текста и не
 * дублируется отдельным пропом.
 *
 * Всегда `aria-hidden`: в дизайне иконка либо сопровождает подпись, либо стоит в кнопке,
 * у которой есть `aria-label`. Иконка, которую озвучивает скринридер, — это подпись,
 * и её место в разметке рядом, а не внутри svg.
 */
export function Icon({ name, className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2.75}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={cn('size-[1em] shrink-0', className)}
    >
      {glyphs[name]}
    </svg>
  )
}
