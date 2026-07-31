import { useContexts } from '@/entities/context'
import { useInbox } from '@/entities/document'
import { useSession } from '@/entities/session'
import type { Space } from '@/entities/space'
import { DeleteSpaceCard, MembersCard, RenameSpaceForm } from '@/features/manage-space'
import { Callout } from '@/shared/ui'
import { AppShell } from '@/widgets/app-shell'

interface SpaceSettingsPageProps {
  space: Space
}

/**
 * Настройки space — единственное место, где управляют доступом и именем.
 *
 * Участник видит те же карточки, но без действий: скрывать существование управления
 * бессмысленно, а обманывать интерфейсом, будто кнопка сработает, — вредно.
 */
export function SpaceSettingsPage({ space }: SpaceSettingsPageProps) {
  const { user } = useSession()
  const { documents } = useInbox(space.id)
  const { contexts } = useContexts(space.id)
  const isOwner = space.role === 'owner'

  return (
    <AppShell space={space}>
      <div className="flex min-h-0 flex-1 gap-[26px] overflow-y-auto px-7 py-[26px]">
        <div className="flex min-w-0 flex-1 flex-col gap-[18px]">
          <div>
            <h1 className="font-display text-[28px] leading-[1.12] font-extrabold tracking-[-0.015em]">
              Настройки space
            </h1>
            {/* Без подстановки имени в падеж: «владелец «Личный архив»» читается как ошибка,
                а склонять произвольные названия нечем. Имя рядом — в шапке. */}
            <p className="text-ink-muted mt-1 text-[13px]">
              {isOwner ? 'Вы владелец этого space.' : 'Вы участник этого space.'}
            </p>
          </div>

          {isOwner ? (
            <RenameSpaceForm space={space} />
          ) : (
            <Callout>
              Имя space и состав участников меняет владелец. Всё остальное — загрузка, контексты,
              поиск — вам доступно наравне с ним.
            </Callout>
          )}

          {user !== null && <MembersCard space={space} currentUserId={user.id} />}
        </div>

        {isOwner && (
          <div className="flex w-[400px] shrink-0 flex-col gap-4">
            <DeleteSpaceCard
              space={space}
              documentCount={documents.length}
              contextCount={contexts.length}
            />
          </div>
        )}
      </div>
    </AppShell>
  )
}
