import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'

import { inviteSpaceMember, removeSpaceMember, spaceKeys, useSpaceMembers } from '@/entities/space'
import type { Space, SpaceMember } from '@/entities/space'
import { initials, pluralize } from '@/shared/lib'
import { Avatar, Button, Callout, Icon, TextInput } from '@/shared/ui'

interface MembersCardProps {
  space: Space
  currentUserId: string
}

/**
 * Состав space: кто имеет доступ и как его отозвать.
 *
 * Управление доступом видно всем участникам, а действует только у владельца — так же, как
 * в API: 403 здесь честнее 404, потому что о существовании этого space спрашивающий и так
 * знает, он в нём состоит.
 */
export function MembersCard({ space, currentUserId }: MembersCardProps) {
  const [email, setEmail] = useState('')
  const [invited, setInvited] = useState<string | null>(null)
  const queryClient = useQueryClient()
  const { members } = useSpaceMembers(space.id)
  const isOwner = space.role === 'owner'

  const invite = useMutation({
    mutationFn: (value: string) => inviteSpaceMember(space.id, value),
    onSuccess: (_result, value) => {
      setInvited(value)
      setEmail('')
    },
  })

  const revoke = useMutation({
    mutationFn: (userId: string) => removeSpaceMember(space.id, userId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: spaceKeys.members(space.id) })
    },
  })

  return (
    <section className="border-line bg-surface flex flex-col gap-3.5 rounded-2xl border px-5 py-[18px]">
      <div className="flex items-center gap-2.5">
        <h2 className="flex-1 text-[16px] font-bold">Участники</h2>
        <span className="text-ink-muted text-[12px]">
          {pluralize(members.length, 'человек', 'человека', 'человек')}
        </span>
      </div>

      {members.map((member) => (
        <MemberRow
          key={member.userId}
          member={member}
          isSelf={member.userId === currentUserId}
          canRevoke={isOwner && member.role !== 'owner' && member.userId !== currentUserId}
          revoking={revoke.isPending && revoke.variables === member.userId}
          onRevoke={() => {
            revoke.mutate(member.userId)
          }}
        />
      ))}

      {invited !== null && (
        <div className="border-line-soft flex items-center gap-3 border-t pt-3">
          <span className="bg-muted text-ink-subtle flex size-9 shrink-0 items-center justify-center rounded-full">
            <Icon name="mail" className="text-[16px]" />
          </span>
          <div className="min-w-0 flex-1">
            <div className="truncate text-[14px] font-bold">{invited}</div>
            <div className="text-ink-muted text-[12px]">Приглашение отправлено</div>
          </div>
          <span className="border-line-dashed bg-muted text-ink-muted rounded-full border border-dashed px-3 py-[3px] text-[12px] font-bold">
            Ожидает входа
          </span>
        </div>
      )}

      {isOwner && (
        <form
          onSubmit={(event) => {
            event.preventDefault()
            invite.mutate(email.trim())
          }}
          className="border-line-soft flex flex-col gap-[9px] border-t pt-3.5"
        >
          <h3 className="text-[14px] font-bold">Пригласить участника</h3>
          <div className="flex gap-2.5">
            <TextInput
              type="email"
              value={email}
              required
              aria-label="Адрес электронной почты"
              placeholder="адрес электронной почты"
              onChange={(event) => {
                setEmail(event.target.value)
              }}
            />
            <Button type="submit" disabled={email.trim() === '' || invite.isPending}>
              Пригласить
            </Button>
          </div>
          <Callout tone="quiet" className="bg-transparent px-0 py-0">
            Ответ одинаков независимо от того, зарегистрирован такой адрес или нет. Отзыв доступа не
            удаляет загруженные этим участником документы.
          </Callout>
        </form>
      )}
    </section>
  )
}

function MemberRow({
  member,
  isSelf,
  canRevoke,
  revoking,
  onRevoke,
}: {
  member: SpaceMember
  isSelf: boolean
  canRevoke: boolean
  revoking: boolean
  onRevoke: () => void
}) {
  return (
    <div className="border-line-soft flex items-center gap-3 border-b pb-3 last:border-b-0 last:pb-0">
      <Avatar tone={member.role === 'owner' ? 'sage' : 'neutral'} className="size-9">
        {initials(member.displayName, member.email)}
      </Avatar>

      <div className="min-w-0 flex-1">
        <div className="truncate text-[14px] font-bold">{member.displayName ?? member.email}</div>
        <div className="text-ink-muted truncate text-[12px]">{member.email}</div>
      </div>

      <span
        className={
          member.role === 'owner'
            ? 'bg-accent-tint text-accent-deep rounded-full px-3 py-1 text-[12px] font-bold'
            : 'bg-muted text-ink-dim rounded-full px-3 py-1 text-[12px] font-bold'
        }
      >
        {member.role === 'owner' ? 'Владелец' : 'Участник'}
      </span>

      <span className="w-24 text-right">
        {isSelf && <span className="text-ink-faint text-[13px]">— это вы</span>}
        {canRevoke && (
          <Button size="sm" variant="dangerOutline" disabled={revoking} onClick={onRevoke}>
            Отозвать
          </Button>
        )}
      </span>
    </div>
  )
}
