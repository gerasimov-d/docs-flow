import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'

import { contextKeys, deleteContext } from '@/entities/context'
import { pluralize, useDismiss } from '@/shared/lib'
import { Button, IconButton } from '@/shared/ui'

interface DeleteContextButtonProps {
  spaceId: string
  contextId: string
  contextName: string
  documentCount: number
}

/**
 * Удаление контекста с подтверждением.
 *
 * В подтверждении прямо сказано, что документы останутся: контекст — метка, а не папка
 * хранения, и это единственное место, где разница становится для пользователя важной.
 */
export function DeleteContextButton({
  spaceId,
  contextId,
  contextName,
  documentCount,
}: DeleteContextButtonProps) {
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const queryClient = useQueryClient()

  useDismiss(containerRef, open, () => {
    setOpen(false)
  })

  const mutation = useMutation({
    mutationFn: () => deleteContext(spaceId, contextId),
    onSuccess: async () => {
      setOpen(false)
      await queryClient.invalidateQueries({ queryKey: contextKeys.list(spaceId) })
    },
  })

  return (
    <div ref={containerRef} className="relative">
      <IconButton
        name="ellipsis"
        label={`Действия с контекстом «${contextName}»`}
        bordered={false}
        size="sm"
        onClick={() => {
          setOpen((previous) => !previous)
        }}
      />

      {open && (
        <div className="border-line bg-surface shadow-float absolute top-8 right-0 z-20 flex w-[296px] flex-col gap-3 rounded-2xl border p-4">
          <p className="text-[15px] font-bold">Удалить контекст «{contextName}»?</p>
          <p className="text-ink-muted text-[13px] leading-[1.55] text-pretty">
            {documentCount === 0
              ? 'Документы этого контекста останутся в space и будут видны в библиотеке.'
              : `${pluralize(documentCount, 'документ', 'документа', 'документов')} останется в space и будет виден в библиотеке — без контекста.`}{' '}
            Контекст не является папкой хранения.
          </p>
          <div className="flex justify-end gap-2">
            <Button
              size="sm"
              onClick={() => {
                setOpen(false)
              }}
            >
              Отмена
            </Button>
            <Button
              size="sm"
              variant="danger"
              disabled={mutation.isPending}
              onClick={() => {
                mutation.mutate()
              }}
            >
              Удалить контекст
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
