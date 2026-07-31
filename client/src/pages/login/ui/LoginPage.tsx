import { Navigate, useSearchParams } from 'react-router'

import { useSession } from '@/entities/session'
import { LoginButton } from '@/features/login'
import { Icon } from '@/shared/ui'

/**
 * Отсекает чужие адреса. Дублирует проверку на бэкенде (`LocalUrlOrRoot`) — она и есть
 * настоящая защита от open redirect, здесь же просто не собираем заведомо негодную ссылку.
 */
function localPathOrRoot(returnUrl: string | null): string {
  if (returnUrl === null || !returnUrl.startsWith('/')) {
    return '/'
  }

  // `//host` и `/\host` браузер разбирает как переход на другой сайт.
  return returnUrl.startsWith('//') || returnUrl.startsWith('/\\') ? '/' : returnUrl
}

/**
 * Вход. Своей формы логина нет и не будет: логин и пароль вводятся на стороне Keycloak,
 * и приложение их не видит — об этом сказано прямо на экране, а не в политике.
 *
 * Этот же экран закрывает состояние «сессия истекла». Отличает их `returnUrl`: он есть
 * только тогда, когда человек шёл на конкретную страницу и был с неё снят. Обещание вернуть
 * на ту же страницу даётся до входа, а не после — иначе оно бесполезно.
 */
export function LoginPage() {
  const [searchParams] = useSearchParams()
  const { user, isLoading } = useSession()
  const returnUrl = localPathOrRoot(searchParams.get('returnUrl'))
  const interrupted = returnUrl !== '/'

  if (isLoading) {
    return (
      <main className="bg-page flex min-h-screen items-center justify-center">
        <p className="text-ink-muted text-[14px]">Загрузка…</p>
      </main>
    )
  }

  // Вошедшему показывать форму входа незачем.
  if (user) {
    return <Navigate to={returnUrl} replace />
  }

  return (
    <main className="bg-page relative flex min-h-screen items-center justify-center overflow-hidden px-6">
      <div
        aria-hidden="true"
        className="bg-raised absolute -top-[120px] -right-[120px] size-[520px] rounded-full"
      />
      <div
        aria-hidden="true"
        className="bg-panel absolute -bottom-[140px] -left-[90px] size-[380px] rounded-full"
      />

      <div className="relative flex w-[520px] max-w-full flex-col items-center gap-[22px] text-center">
        <div className="flex items-center gap-3">
          <span className="bg-accent font-display text-on-accent flex size-11 items-center justify-center rounded-full text-[23px] font-extrabold">
            D
          </span>
          <span className="font-display text-[32px] font-extrabold tracking-[-0.015em]">
            DocsFlow
          </span>
        </div>

        {interrupted ? (
          <>
            <h1 className="font-display text-[25px] leading-[1.15] font-extrabold tracking-[-0.015em]">
              Сессия истекла
            </h1>
            <p className="text-ink-dim text-[16px] leading-[1.6] text-pretty">
              Войдите заново — вы вернётесь на эту же страницу. Ничего из загруженного не потеряно.
            </p>
            <LoginButton returnUrl={returnUrl}>Войти заново</LoginButton>
          </>
        ) : (
          <>
            <p className="text-ink-dim text-[16px] leading-[1.6] text-pretty">
              Хранение, распознавание и поиск по личным документам. Ответ всегда со ссылкой на
              первоисточник.
            </p>
            <LoginButton returnUrl={returnUrl} />
          </>
        )}

        <p className="text-ink-subtle max-w-[400px] text-[12px] leading-[1.55]">
          Вход и регистрация проходят на стороне Keycloak. DocsFlow не хранит пароли.
        </p>

        {interrupted && (
          <p className="border-line bg-surface text-ink-muted flex max-w-[440px] items-center gap-2.5 rounded-2xl border px-4 py-[11px] text-left text-[12px] leading-[1.5]">
            <Icon name="file-text" className="text-ink-subtle text-[16px]" />
            После входа откроется страница, которую вы запрашивали.
          </p>
        )}
      </div>
    </main>
  )
}
