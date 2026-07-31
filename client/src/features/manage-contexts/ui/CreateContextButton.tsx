import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'

import { contextKeys, createContext } from '@/entities/context'
import { HttpError } from '@/shared/api'
import { Button, Modal, TextInput } from '@/shared/ui'

interface CreateContextButtonProps {
  spaceId: string
}

/**
 * Создание контекста.
 *
 * Роль не проверяется: контексты в space общие, и участник — полноправный соавтор.
 * Персональных и скрытых контекстов в модели нет.
 */
export function CreateContextButton({ spaceId }: CreateContextButtonProps) {
  const [open, setOpen] = useState(false)
  const [name, setName] = useState('')
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: (value: string) => createContext(spaceId, value),
    onSuccess: async () => {
      setOpen(false)
      setName('')
      await queryClient.invalidateQueries({ queryKey: contextKeys.list(spaceId) })
    },
  })

  return (
    <>
      <Button
        variant="primary"
        icon="plus"
        onClick={() => {
          setOpen(true)
        }}
      >
        Новый контекст
      </Button>

      <Modal
        open={open}
        onClose={() => {
          setOpen(false)
          mutation.reset()
        }}
        title="Новый контекст"
        description="Тематическая метка внутри space: «авто», «медицина». Вложенности нет."
        className="w-[440px]"
        footer={
          <>
            <span className="flex-1" />
            <Button
              onClick={() => {
                setOpen(false)
              }}
            >
              Отмена
            </Button>
            <Button
              variant="primary"
              disabled={name.trim() === '' || mutation.isPending}
              onClick={() => {
                mutation.mutate(name.trim())
              }}
            >
              Создать
            </Button>
          </>
        }
      >
        <TextInput
          value={name}
          autoFocus
          placeholder="Название контекста"
          onChange={(event) => {
            setName(event.target.value)
          }}
        />

        {mutation.error !== null && (
          <p className="text-danger-deep text-[13px]">{describe(mutation.error)}</p>
        )}
      </Modal>
    </>
  )
}

function describe(error: Error): string {
  // 409 — имя занято. Остальное для пользователя одинаково: повторить попытку.
  if (error instanceof HttpError && error.status === 409) {
    return 'Контекст с таким именем в этом space уже есть.'
  }

  return 'Не удалось создать контекст. Попробуйте ещё раз.'
}
