import { Icon } from '@/shared/ui'

interface LoginButtonProps {
  /** Куда вернуть пользователя после входа. Только локальный путь. */
  returnUrl: string
  children?: string
}

/**
 * Вход — обычная навигация браузера, а не запрос через fetch: за `/api/auth/login` следует
 * цепочка редиректов на Keycloak и обратно, которую fetch не проведёт (чужой origin), а
 * пользователь должен увидеть форму входа Keycloak.
 *
 * Поэтому это ссылка, а не кнопка с обработчиком: работает без JavaScript и не изобретает
 * навигацию заново. Выглядит она при этом как главная кнопка экрана — так в дизайне.
 */
export function LoginButton({ returnUrl, children = 'Войти' }: LoginButtonProps) {
  const href = `/api/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`

  return (
    <a
      href={href}
      className="border-accent bg-accent font-display text-on-accent hover:border-accent-strong hover:bg-accent-strong inline-flex items-center gap-2.5 rounded-full border px-[30px] py-[13px] text-[16px] font-bold no-underline transition-colors"
    >
      <Icon name="lock" className="text-[18px]" />
      {children}
    </a>
  )
}
