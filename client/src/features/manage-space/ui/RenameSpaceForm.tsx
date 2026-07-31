import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'

import { renameSpace, spaceKeys } from '@/entities/space'
import type { Space } from '@/entities/space'
import { Button, TextInput } from '@/shared/ui'

interface RenameSpaceFormProps {
  space: Space
}

/** Переименование space. Право есть только у владельца — участнику API отвечает 403. */
export function RenameSpaceForm({ space }: RenameSpaceFormProps) {
  const [name, setName] = useState(space.name)
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: (value: string) => renameSpace(space.id, value),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: spaceKeys.list() })
    },
  })

  const unchanged = name.trim() === space.name || name.trim() === ''

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault()
        mutation.mutate(name.trim())
      }}
      className="border-line bg-surface flex flex-col gap-3 rounded-2xl border px-5 py-[18px]"
    >
      <h2 className="text-[16px] font-bold">Название</h2>
      <div className="flex gap-2.5">
        <TextInput
          value={name}
          aria-label="Название space"
          onChange={(event) => {
            setName(event.target.value)
          }}
        />
        <Button type="submit" variant="primary" disabled={unchanged || mutation.isPending}>
          Сохранить
        </Button>
      </div>

      {mutation.isError && (
        <p className="text-danger-deep text-[13px]">
          Не удалось сохранить название. Менять его может только владелец space.
        </p>
      )}
    </form>
  )
}
