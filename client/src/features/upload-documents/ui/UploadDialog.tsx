import { useRef, useState } from 'react'

import { useContexts } from '@/entities/context'
import { ACCEPTED_FORMATS_HINT } from '@/shared/config'
import { cn, formatFileSize } from '@/shared/lib'
import { Button, Icon, Modal } from '@/shared/ui'

import { useUploadBatch } from '../model/use-upload-batch'
import type { UploadItem } from '../model/use-upload-batch'

interface UploadDialogProps {
  spaceId: string
  spaceName: string
  open: boolean
  onClose: () => void
}

/**
 * Загрузка пачки файлов.
 *
 * Диалог не блокирует работу: обработка идёт в фоне, и об этом сказано прямо — иначе
 * пользователь будет сидеть над окном, ожидая, что закрытие всё отменит.
 */
export function UploadDialog({ spaceId, spaceName, open, onClose }: UploadDialogProps) {
  const [dragging, setDragging] = useState(false)
  const [contextId, setContextId] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)
  const { contexts } = useContexts(spaceId)
  const { items, acceptedCount, rejectedCount, add, retryRejected, reset } = useUploadBatch()

  const close = () => {
    reset()
    onClose()
  }

  return (
    <Modal
      open={open}
      onClose={close}
      title={`Загрузка в «${spaceName}»`}
      description="Обработка идёт в фоне — вкладку можно закрыть."
      footer={
        <>
          <span className="text-ink-muted flex-1 text-[13px]">
            {items.length === 0
              ? 'Файлы ещё не выбраны'
              : `${String(acceptedCount)} из ${String(items.length)} приняты${rejectedCount === 0 ? '' : ` · ${String(rejectedCount)} не отправлены`}`}
          </span>
          {rejectedCount > 0 && <Button onClick={retryRejected}>Повторить неудачные</Button>}
          <Button variant="primary" onClick={close}>
            Готово
          </Button>
        </>
      }
    >
      <div
        onDragOver={(event) => {
          event.preventDefault()
          setDragging(true)
        }}
        onDragLeave={() => {
          setDragging(false)
        }}
        onDrop={(event) => {
          event.preventDefault()
          setDragging(false)
          add([...event.dataTransfer.files])
        }}
        className={cn(
          'flex flex-col items-center gap-2.5 rounded-2xl border-2 border-dashed p-[26px] transition-colors',
          dragging ? 'border-accent bg-accent-wash' : 'border-line-dashed bg-page',
        )}
      >
        <span className="bg-accent-tint text-accent flex size-10 items-center justify-center rounded-full">
          <Icon name="upload" className="text-[20px]" />
        </span>
        <p className="text-[15px] font-bold">Перетащите файлы сюда</p>
        <Button
          onClick={() => {
            inputRef.current?.click()
          }}
        >
          Выбрать на диске
        </Button>
        <p className="text-ink-subtle text-[12px]">{ACCEPTED_FORMATS_HINT}</p>

        <input
          ref={inputRef}
          type="file"
          multiple
          accept=".jpg,.jpeg,.png,.heic,.heif,.pdf"
          className="sr-only"
          onChange={(event) => {
            add([...(event.target.files ?? [])])
            event.target.value = ''
          }}
        />
      </div>

      <label className="flex items-center gap-2.5">
        <span className="text-ink-muted text-[13px]">Контекст</span>
        <select
          value={contextId}
          onChange={(event) => {
            setContextId(event.target.value)
          }}
          className="border-line bg-page flex-1 rounded-full border px-3.5 py-2 text-[14px]"
        >
          <option value="">Не указан — можно назначить позже</option>
          {contexts.map((context) => (
            <option key={context.id} value={context.id}>
              {context.name}
            </option>
          ))}
        </select>
      </label>

      {items.length > 0 && (
        <ul className="flex max-h-[250px] flex-col gap-[9px] overflow-y-auto">
          {items.map((item) => (
            <UploadRow key={item.id} item={item} />
          ))}
        </ul>
      )}
    </Modal>
  )
}

function UploadRow({ item }: { item: UploadItem }) {
  if (item.state === 'rejected') {
    return (
      <li className="flex items-center gap-3">
        <Icon name="circle-alert" className="text-danger text-[16px]" />
        <span className="min-w-0 flex-1">
          <span className="block truncate text-[13px] font-semibold">{item.name}</span>
          <span className="text-danger-deep block text-[11px]">{item.reason}</span>
        </span>
        <span className="text-danger-deep w-[110px] text-right text-[12px] font-bold">
          Не отправлен
        </span>
      </li>
    )
  }

  if (item.state === 'accepted') {
    return (
      <li className="flex items-center gap-3">
        <Icon name="circle-check" className="text-sage-strong text-[16px]" />
        <span className="min-w-0 flex-1 truncate text-[13px] font-semibold">{item.name}</span>
        <span className="text-ink-subtle w-16 text-right text-[12px]">
          {formatFileSize(item.sizeBytes)}
        </span>
        <span className="text-sage-ink w-[110px] text-right text-[12px] font-bold">Принят</span>
      </li>
    )
  }

  return (
    <li className="flex items-center gap-3">
      <Icon name="upload" className="text-accent text-[16px]" />
      <span className="min-w-0 flex-1 truncate text-[13px] font-semibold">{item.name}</span>
      <span className="bg-line h-1.5 w-[120px] overflow-hidden rounded-full">
        <span
          className="bg-accent block h-full transition-[width] duration-200"
          style={{ width: `${String(item.progress)}%` }}
        />
      </span>
      <span className="text-accent-deep w-[110px] text-right text-[12px] font-bold">
        {item.progress}%
      </span>
    </li>
  )
}
