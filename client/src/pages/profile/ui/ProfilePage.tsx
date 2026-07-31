import { useSession } from '@/entities/session'
import type { Space } from '@/entities/space'
import { useSpaces } from '@/entities/space'
import { LogoutButton } from '@/features/logout'
import { initials, pluralize } from '@/shared/lib'
import { Avatar, Icon } from '@/shared/ui'
import { AppShell } from '@/widgets/app-shell'

interface ProfilePageProps {
  space: Space
}

/**
 * Профиль — только чтение.
 *
 * Имя, почта и пароль живут в Keycloak. Дублировать их редактирование здесь значило бы
 * заводить вторую копию учётных данных и вечно разбираться, какая из них главная.
 */
export function ProfilePage({ space }: ProfilePageProps) {
  const { user } = useSession()
  const { spaces } = useSpaces()

  if (user === null) {
    return null
  }

  const rows = [
    { label: 'Имя', value: user.displayName ?? 'не указано' },
    { label: 'Почта', value: user.email },
    { label: 'Идентификатор Keycloak', value: user.id, mono: true },
    {
      label: 'Доступные space',
      value:
        spaces.length === 0
          ? 'нет'
          : `${pluralize(spaces.length, 'space', 'space', 'space')} — ${spaces
              .map((item) => `«${item.name}» (${item.role === 'owner' ? 'владелец' : 'участник'})`)
              .join(', ')}`,
    },
  ]

  return (
    <AppShell space={space}>
      <div className="flex min-h-0 flex-1 flex-col gap-[18px] overflow-y-auto px-7 py-[26px]">
        <div>
          <h1 className="font-display text-[28px] leading-[1.12] font-extrabold tracking-[-0.015em]">
            Профиль
          </h1>
          <p className="text-ink-muted mt-1 text-[13px]">
            Данные приходят из Keycloak и здесь не редактируются.
          </p>
        </div>

        <section className="border-line bg-surface flex max-w-[620px] flex-col gap-[18px] rounded-2xl border p-[22px]">
          <div className="flex items-center gap-4">
            <Avatar className="size-16 text-[24px]">
              {initials(user.displayName, user.email)}
            </Avatar>
            <div>
              <h2 className="font-display text-[25px] leading-[1.15] font-extrabold tracking-[-0.015em]">
                {user.displayName ?? user.email}
              </h2>
              <p className="text-ink-muted mt-0.5 text-[13px]">{user.email}</p>
            </div>
          </div>

          <div className="bg-line-soft h-px" />

          <dl className="flex flex-col gap-3">
            {rows.map((row) => (
              <div key={row.label} className="flex gap-4">
                <dt className="text-ink-muted w-[190px] shrink-0 text-[13px]">{row.label}</dt>
                <dd
                  className={
                    row.mono === true
                      ? 'text-ink-dim font-mono text-[13px] font-semibold'
                      : 'text-[14px] font-semibold'
                  }
                >
                  {row.value}
                </dd>
              </div>
            ))}
          </dl>

          <div className="bg-line-soft h-px" />

          <div className="flex flex-wrap items-center gap-3">
            {/*
              Ссылка ведёт на страницу аккаунта Keycloak. Адрес realm приходит с бэкенда
              вместе с сессией, поэтому пока это заглушка.
              TODO(бэкенд): отдавать `accountUrl` в `GET /api/me`.
            */}
            <span className="border-line font-display inline-flex cursor-not-allowed items-center gap-2 rounded-full border px-[18px] py-[9px] text-[14px] font-bold opacity-45">
              <Icon name="external-link" className="text-[16px]" />
              Сменить пароль в Keycloak
            </span>
            <LogoutButton />
          </div>
        </section>
      </div>
    </AppShell>
  )
}
