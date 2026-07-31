import { cn } from '@/shared/lib'

type AvatarTone = 'sage' | 'neutral' | 'accent'

const toneClasses: Record<AvatarTone, string> = {
  sage: 'bg-sage text-on-sage',
  neutral: 'bg-line-dashed text-ink-soft',
  accent: 'bg-accent text-on-accent',
}

interface AvatarProps {
  /** Уже посчитанные инициалы: `initials()` из `shared/lib`. */
  children: string
  tone?: AvatarTone
  className?: string
}

/**
 * Аватар из инициалов. Фотографий у пользователей нет — Keycloak их не хранит, а грузить
 * картинку со стороннего граватара значит сливать туда почту каждого участника space.
 */
export function Avatar({ children, tone = 'sage', className }: AvatarProps) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        'font-display inline-flex shrink-0 items-center justify-center rounded-full font-bold',
        'size-8 text-[14px]',
        toneClasses[tone],
        className,
      )}
    >
      {children}
    </span>
  )
}
