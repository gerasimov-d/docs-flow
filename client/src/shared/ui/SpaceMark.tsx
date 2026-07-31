import { cn } from '@/shared/lib'

interface SpaceMarkProps {
  /** Название space — первая буква и станет знаком. */
  name: string
  tone?: 'accent' | 'sage'
  className?: string
}

/**
 * Квадратный знак space. Отличается от круглого аватара пользователя формой, а не только
 * цветом: в шапке они стоят рядом, и путать «чей архив» с «кто вошёл» нельзя.
 */
export function SpaceMark({ name, tone = 'accent', className }: SpaceMarkProps) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        'inline-flex shrink-0 items-center justify-center rounded-lg text-[12px] font-extrabold',
        tone === 'accent' ? 'bg-accent text-on-accent' : 'bg-sage text-on-sage',
        'size-6',
        className,
      )}
    >
      {name.trim().slice(0, 1).toUpperCase()}
    </span>
  )
}
