const dateFormat = new Intl.DateTimeFormat('ru-RU', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

const timeFormat = new Intl.DateTimeFormat('ru-RU', { hour: '2-digit', minute: '2-digit' })

/** Дата документа: `12.03.2026`. Отсутствие даты — не ошибка, а частый случай. */
export function formatDate(value: string | null): string {
  return value === null ? 'дата не указана' : dateFormat.format(new Date(value))
}

/**
 * Момент загрузки в терминах пользователя: «сегодня 14:32», «вчера 19:04», иначе дата.
 * Инбокс отсортирован по этому полю, и точная дата в первых строках только мешает.
 */
export function formatUploadedAt(value: string, now = new Date()): string {
  const uploaded = new Date(value)
  const days = calendarDaysBetween(uploaded, now)

  if (days === 0) {
    return `сегодня ${timeFormat.format(uploaded)}`
  }

  if (days === 1) {
    return `вчера ${timeFormat.format(uploaded)}`
  }

  return dateFormat.format(uploaded)
}

function calendarDaysBetween(from: Date, to: Date): number {
  const startOfDay = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const millisecondsPerDay = 24 * 60 * 60 * 1000

  return Math.round((startOfDay(to).getTime() - startOfDay(from).getTime()) / millisecondsPerDay)
}

/** Размер файла с русским разделителем дробной части: `2,4 МБ`. */
export function formatFileSize(bytes: number): string {
  const units = [
    { limit: 1024 ** 2, suffix: 'КБ', divisor: 1024 },
    { limit: 1024 ** 3, suffix: 'МБ', divisor: 1024 ** 2 },
  ]
  const unit = units.find(({ limit }) => bytes < limit)

  if (bytes < 1024) {
    return `${String(bytes)} Б`
  }

  const { suffix, divisor } = unit ?? { suffix: 'ГБ', divisor: 1024 ** 3 }
  const value = bytes / divisor

  return `${value.toLocaleString('ru-RU', { maximumFractionDigits: value < 10 ? 1 : 0 })} ${suffix}`
}

/**
 * Русское склонение после числа: `1 документ`, `2 документа`, `5 документов`.
 * Без него интерфейс на каждом счётчике сваливается в «5 документа».
 */
export function plural(count: number, one: string, few: string, many: string): string {
  const mod100 = Math.abs(count) % 100
  const mod10 = mod100 % 10

  if (mod100 >= 11 && mod100 <= 14) {
    return many
  }

  if (mod10 === 1) {
    return one
  }

  return mod10 >= 2 && mod10 <= 4 ? few : many
}

/** Число вместе со склонённым словом: `184 документа`. */
export function pluralize(count: number, one: string, few: string, many: string): string {
  return `${String(count)} ${plural(count, one, few, many)}`
}

/**
 * Инициалы для аватара: «Дмитрий Герасимов» → «ДГ», «d.gerasimov@mail.ru» → «D».
 * Имени может не быть — Keycloak отдаёт только то, что пользователь указал сам.
 */
export function initials(name: string | null, fallback: string): string {
  const source = name?.trim() ?? ''

  if (source === '') {
    return fallback.trim().slice(0, 1).toUpperCase()
  }

  return source
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.slice(0, 1).toUpperCase())
    .join('')
}
