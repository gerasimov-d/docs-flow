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
      <button
        type="submit"
        className="rounded border border-gray-300 px-4 py-2 transition-colors hover:bg-gray-100"
      >
        Выйти
      </button>
    </form>
  )
}
