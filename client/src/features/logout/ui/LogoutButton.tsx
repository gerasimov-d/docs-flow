import { Button } from '@/shared/ui'

/**
 * Выход — отправка формы, а не fetch. Сервер отвечает редиректом на Keycloak: тот гасит SSO-сессию
 * и возвращает браузер обратно. fetch прошёл бы этот редирект в фоне, упёрся в чужой origin, и
 * SSO-сессия осталась бы жива — следующий вход прошёл бы без пароля.
 *
 * Метод POST обязателен: выход меняет состояние, а GET-ссылку на него можно было бы подсунуть
 * картинкой на чужом сайте.
 */
export function LogoutButton() {
  return (
    <form method="post" action="/api/auth/logout">
      <Button type="submit" variant="dangerOutline" icon="log-out">
        Выйти
      </Button>
    </form>
  )
}
