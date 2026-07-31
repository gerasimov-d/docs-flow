interface LoginButtonProps {
  /** Куда вернуть пользователя после входа. Только локальный путь. */
  returnUrl: string
}

/**
 * Вход — обычная навигация браузера, а не запрос через fetch: за `/api/auth/login` следует
 * цепочка редиректов на Keycloak и обратно, которую fetch не проведёт (чужой origin), а
 * пользователь должен увидеть форму входа Keycloak.
 *
 * Поэтому это ссылка, а не кнопка с обработчиком: работает без JavaScript и не изобретает
 * навигацию заново.
 */
export function LoginButton({ returnUrl }: LoginButtonProps) {
  const href = `/api/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`

  return (
    <a
      href={href}
      className="rounded bg-gray-900 px-4 py-2 text-white transition-colors hover:bg-gray-700"
    >
      Войти
    </a>
  )
}
