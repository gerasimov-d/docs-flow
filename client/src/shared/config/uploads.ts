/**
 * Что принимается на загрузку.
 *
 * Клиент проверяет формат и размер до отправки — не вместо серверной проверки, а чтобы
 * не гнать 51 мегабайт по сети ради отказа. Настоящая граница остаётся на бэкенде.
 */
export const MAX_UPLOAD_BYTES = 50 * 1024 * 1024

const acceptedMimeTypes = ['image/jpeg', 'image/png', 'image/heic', 'image/heif', 'application/pdf']
const acceptedExtensions = ['.jpg', '.jpeg', '.png', '.heic', '.heif', '.pdf']

export const ACCEPTED_FORMATS_HINT = 'JPEG, PNG, HEIC, PDF · до 50 МБ на файл'

/** Причина отказа человеческим языком либо `null`, если файл принят. */
export function isAcceptedFile(file: File): string | null {
  const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase()

  // HEIC браузеры часто отдают с пустым type — по одному MIME отказывать нельзя.
  if (!acceptedMimeTypes.includes(file.type) && !acceptedExtensions.includes(extension)) {
    const format = extension.replace('.', '').toUpperCase()

    return `Формат ${format === '' ? 'файла' : format} не поддерживается. Файл не отправлен.`
  }

  if (file.size > MAX_UPLOAD_BYTES) {
    const size = (file.size / 1024 ** 2).toLocaleString('ru-RU', { maximumFractionDigits: 1 })

    return `${size} МБ — больше лимита 50 МБ. Файл не отправлен.`
  }

  return null
}
