import { useCallback, useEffect, useRef, useState } from 'react'

import { isAcceptedFile } from '@/shared/config'

/**
 * Состояние одного файла в пачке.
 *
 * `rejected` отличается от `failed` тем, что файл вообще не уходил на сервер: формат и размер
 * проверены до отправки, и пользователю честно сказано «не отправлен», а не «не загрузился».
 */
export type UploadState = 'uploading' | 'accepted' | 'rejected'

export interface UploadItem {
  id: string
  name: string
  sizeBytes: number
  state: UploadState
  progress: number
  /** Причина отказа. Заполнена только у `rejected`. */
  reason: string | null
}

interface UploadBatch {
  items: UploadItem[]
  acceptedCount: number
  rejectedCount: number
  add: (files: File[]) => void
  retryRejected: () => void
  reset: () => void
}

/**
 * Пачка загрузки: у каждого файла свой прогресс и своя судьба.
 *
 * Одна общая полоса на всю пачку скрывала бы главное — какой именно файл не прошёл и почему.
 *
 * TODO(бэкенд): `POST /api/spaces/{spaceId}/documents` с `multipart/form-data` и прогрессом
 * через `XMLHttpRequest.upload` (у `fetch` прогресса отправки нет). Пока эндпоинта нет,
 * принятые файлы дозаполняют прогресс таймером — проверка формата и размера при этом
 * настоящая и работает ровно так, как будет работать потом.
 */
export function useUploadBatch(): UploadBatch {
  const [items, setItems] = useState<UploadItem[]>([])
  const timers = useRef<number[]>([])

  useEffect(
    () => () => {
      timers.current.forEach((timer) => {
        clearInterval(timer)
      })
    },
    [],
  )

  const simulate = useCallback((id: string) => {
    const timer = window.setInterval(() => {
      setItems((previous) =>
        previous.map((item) => {
          if (item.id !== id || item.state !== 'uploading') {
            return item
          }

          const progress = Math.min(100, item.progress + 17)

          return progress === 100
            ? { ...item, progress, state: 'accepted' as const }
            : { ...item, progress }
        }),
      )
    }, 240)

    timers.current.push(timer)
  }, [])

  const add = useCallback(
    (files: File[]) => {
      const added = files.map((file, index) => {
        const reason = isAcceptedFile(file)

        return {
          id: `${file.name}-${String(file.size)}-${String(index)}`,
          name: file.name,
          sizeBytes: file.size,
          state: reason === null ? ('uploading' as const) : ('rejected' as const),
          progress: 0,
          reason,
        }
      })

      setItems((previous) => [...previous, ...added])
      added
        .filter((item) => item.state === 'uploading')
        .forEach((item) => {
          simulate(item.id)
        })
    },
    [simulate],
  )

  /**
   * Повтор касается только тех, что не ушли по временной причине. Файл, отклонённый по
   * формату или размеру, повтором не чинится — его надо заменить, и кнопка это не скрывает.
   */
  const retryRejected = useCallback(() => {
    setItems((previous) => previous.filter((item) => item.state !== 'rejected'))
  }, [])

  const reset = useCallback(() => {
    setItems([])
  }, [])

  return {
    items,
    acceptedCount: items.filter((item) => item.state === 'accepted').length,
    rejectedCount: items.filter((item) => item.state === 'rejected').length,
    add,
    retryRejected,
    reset,
  }
}
