import { NavLink } from 'react-router'

import { useContexts } from '@/entities/context'
import { useInbox } from '@/entities/document'
import { useSession } from '@/entities/session'
import type { Space } from '@/entities/space'
import { routes } from '@/shared/config'
import { cn, initials } from '@/shared/lib'
import { Avatar, Icon, SectionLabel } from '@/shared/ui'
import type { IconName } from '@/shared/ui'

interface AppSidebarProps {
  space: Space
}

/**
 * Постоянная навигация по разделам архива.
 *
 * Счётчики стоят рядом с пунктами не для красоты: инбокс без числа не отличить от пустого,
 * а именно по нему видно, что после загрузки что-то происходит.
 */
export function AppSidebar({ space }: AppSidebarProps) {
  const { user } = useSession()
  const { documents } = useInbox(space.id)
  const { contexts } = useContexts(space.id)

  const pending = documents.filter((document) => document.status !== 'ready').length

  return (
    <nav className="border-line bg-raised flex w-[248px] shrink-0 flex-col border-r px-3.5 pt-[22px] pb-[18px]">
      <div className="flex items-center gap-2.5 px-2 pb-6">
        <span className="bg-accent font-display text-on-accent flex size-[34px] items-center justify-center rounded-full text-[18px] font-extrabold">
          D
        </span>
        <span className="font-display text-[21px] font-extrabold tracking-[-0.015em]">
          DocsFlow
        </span>
      </div>

      <SectionLabel className="px-3 pb-2">Архив</SectionLabel>
      <div className="flex flex-col gap-0.5">
        <SidebarLink
          to={routes.inbox(space.id)}
          icon="inbox"
          label="Инбокс"
          count={pending}
          accentCount
        />
        <SidebarLink
          to={routes.library(space.id)}
          icon="library-big"
          label="Библиотека"
          count={documents.length}
        />
        <SidebarLink
          to={routes.contexts(space.id)}
          icon="tag"
          label="Контексты"
          count={contexts.length}
        />
      </div>

      <SectionLabel className="px-3 pt-[22px] pb-2">Ответы</SectionLabel>
      <SidebarLink to={routes.dialogs(space.id)} icon="messages-square" label="Диалоги" />

      <div className="flex-1" />

      <SidebarLink to={routes.settings(space.id)} icon="settings" label="Настройки space" />

      <div className="bg-line mx-3 my-2.5 h-px" />

      <NavLink
        to={routes.profile(space.id)}
        className={({ isActive }) =>
          cn(
            'flex items-center gap-2.5 rounded-full px-3 py-1.5 transition-colors',
            isActive ? 'bg-page' : 'hover:bg-page/60',
          )
        }
      >
        <Avatar>{initials(user?.displayName ?? null, user?.email ?? '?')}</Avatar>
        <span className="flex min-w-0 flex-1 flex-col">
          <span className="truncate text-[13px] font-bold">
            {user?.displayName ?? user?.email ?? '—'}
          </span>
          <span className="text-ink-subtle truncate text-[11px]">{user?.email}</span>
        </span>
        <Icon name="chevron-up" className="text-ink-subtle text-[16px]" />
      </NavLink>
    </nav>
  )
}

function SidebarLink({
  to,
  icon,
  label,
  count,
  accentCount = false,
}: {
  to: string
  icon: IconName
  label: string
  count?: number
  accentCount?: boolean
}) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        cn(
          'flex items-center gap-[11px] rounded-full px-[13px] py-2.5 text-[14px] font-semibold transition-colors',
          isActive ? 'bg-accent text-on-accent' : 'text-ink-dim hover:bg-page/60',
        )
      }
    >
      {({ isActive }) => (
        <>
          <Icon name={icon} className="text-[18px]" />
          <span className="flex-1">{label}</span>
          {count !== undefined && count > 0 && (
            <span
              className={cn(
                'rounded-full text-[12px] font-bold',
                accentCount && !isActive && 'bg-accent text-on-accent px-2 py-px',
                accentCount && isActive && 'bg-on-accent/28 px-2 py-px',
                !accentCount && (isActive ? 'text-on-accent' : 'text-ink-subtle'),
              )}
            >
              {count}
            </span>
          )}
        </>
      )}
    </NavLink>
  )
}
