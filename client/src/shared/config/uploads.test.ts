import { describe, expect, it } from 'vitest'

import { MAX_UPLOAD_BYTES, isAcceptedFile } from './uploads'

/**
 * Файл нужного размера без выделения этих байтов: `size` подменяется на готовом `File`.
 * У `Blob` его подменять бессмысленно — конструктор `File` пересчитывает размер по содержимому.
 */
function file(name: string, sizeBytes: number, type = ''): File {
  const created = new File([''], name, { type })

  Object.defineProperty(created, 'size', { value: sizeBytes })

  return created
}

describe('isAcceptedFile', () => {
  it('принимает поддерживаемые форматы', () => {
    expect(isAcceptedFile(file('скан.pdf', 1024, 'application/pdf'))).toBeNull()
    expect(isAcceptedFile(file('фото.jpg', 1024, 'image/jpeg'))).toBeNull()
  })

  it('принимает HEIC, у которого браузер не определил MIME', () => {
    expect(isAcceptedFile(file('Полис ОМС.heic', 1024))).toBeNull()
  })

  it('отказывает по формату до отправки и называет причину', () => {
    expect(isAcceptedFile(file('Опись.tiff', 1024, 'image/tiff'))).toBe(
      'Формат TIFF не поддерживается. Файл не отправлен.',
    )
  })

  it('отказывает по размеру до отправки и называет причину', () => {
    const oversized = file('Скан договора.pdf', MAX_UPLOAD_BYTES + 1_300_000, 'application/pdf')

    expect(isAcceptedFile(oversized)).toContain('больше лимита 50 МБ. Файл не отправлен.')
  })

  it('пропускает файл ровно по границе лимита', () => {
    expect(isAcceptedFile(file('ровно.pdf', MAX_UPLOAD_BYTES, 'application/pdf'))).toBeNull()
  })
})
