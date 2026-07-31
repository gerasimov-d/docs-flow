import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useNavigate } from 'react-router'

import { deleteSpace, spaceKeys } from '@/entities/space'
import type { Space } from '@/entities/space'
import { Button, Icon, Modal, TextInput } from '@/shared/ui'

interface DeleteSpaceCardProps {
  space: Space
  /** Что именно исчезнет — считается снаружи, чтобы карточка не ходила за данными сама. */
  documentCount: number
  contextCount: number
}

/**
 * Удаление space.
 *
 * Подтверждение требует ввести название: удаление уносит весь архив, и «нажал не глядя»
 * здесь стоит слишком дорого. Само удаление мягкое — 30 дней space можно вернуть, и об этом
 * сказано в том же окне, а не в справке.
 */
export function DeleteSpaceCard({ space, documentCount, contextCount }: DeleteSpaceCardProps) {
  const [open, setOpen] = useState(false)
  const [confirmation, setConfirmation] = useState('')
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: () => deleteSpace(space.id),
    onSuccess: async () => {
      setOpen(false)
      await queryClient.invalidateQueries({ queryKey: spaceKeys.all })
      await navigate('/')
    },
  })

  return (
    <>
      <section className="border-danger-edge bg-danger-wash flex flex-col gap-3 rounded-2xl border px-5 py-[18px]">
        <h2 className="text-danger-deep flex items-center gap-[9px] text-[16px] font-bold">
          <Icon name="triangle-alert" className="text-[18px]" />
          Удаление space
        </h2>
        <p className="text-ink-muted text-[13px] leading-[1.6] text-pretty">
          Удаление мягкое: space скроется и его можно вернуть в течение 30 дней. По истечении срока
          содержимое вычищается безвозвратно.
        </p>
        <Button
          variant="dangerOutline"
          className="self-start"
          onClick={() => {
            setOpen(true)
          }}
        >
          Удалить space
        </Button>
      </section>

      <Modal
        open={open}
        onClose={() => {
          setOpen(false)
          setConfirmation('')
        }}
        title={`Удалить «${space.name}»?`}
        className="w-[460px]"
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
              variant="danger"
              disabled={confirmation.trim() !== space.name || mutation.isPending}
              onClick={() => {
                mutation.mutate()
              }}
            >
              Удалить space
            </Button>
          </>
        }
      >
        <p className="text-ink-muted text-[13px]">Будет удалено:</p>
        <ul className="flex flex-col gap-[7px] text-[13px]">
          {[
            `${String(documentCount)} документов`,
            'Исходные файлы в хранилище',
            'Результаты распознавания и чанки индекса',
            `${String(contextCount)} контекстов и история вопросов`,
          ].map((line) => (
            <li key={line} className="flex items-center gap-[9px]">
              <span className="bg-danger size-[5px] rounded-full" />
              {line}
            </li>
          ))}
        </ul>

        <p className="border-accent-edge bg-accent-wash text-accent-deep rounded-lg border px-3 py-2.5 text-[12px] leading-[1.5]">
          Вернуть space можно в течение 30 дней. После этого восстановление невозможно.
        </p>

        <label className="flex flex-col gap-[7px]">
          <span className="text-ink-muted text-[12px]">
            Введите название space, чтобы подтвердить
          </span>
          <TextInput
            value={confirmation}
            placeholder={space.name}
            onChange={(event) => {
              setConfirmation(event.target.value)
            }}
          />
        </label>
      </Modal>
    </>
  )
}
