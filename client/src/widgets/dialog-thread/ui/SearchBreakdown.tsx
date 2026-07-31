import type { SearchResult } from '@/entities/dialog'
import { pluralize } from '@/shared/lib'
import { Icon, SectionLabel } from '@/shared/ui'

interface SearchBreakdownProps {
  result: SearchResult
}

/**
 * Как собрана гибридная выдача.
 *
 * Панель отвечает на вопрос «почему здесь документ без слов из запроса»: BM25 и вектор ищут
 * по-разному, и без объяснения результат выглядит случайным. Здесь же сказано главное про
 * изоляцию: оба индекса фильтруются по space внутри запроса.
 */
export function SearchBreakdown({ result }: SearchBreakdownProps) {
  if (result.breakdown === null) {
    return null
  }

  const { exactCount, semanticCount, mergedCount } = result.breakdown

  const rows = [
    {
      icon: 'search' as const,
      title: 'OpenSearch · BM25',
      note: `${pluralize(exactCount, 'фрагмент', 'фрагмента', 'фрагментов')} по словам запроса`,
      tint: 'bg-accent-tint text-accent-deep',
    },
    {
      icon: 'database' as const,
      title: 'pgvector · kNN',
      note: `${pluralize(semanticCount, 'фрагмент', 'фрагмента', 'фрагментов')} по смыслу`,
      tint: 'bg-sage-tint text-sage-ink',
    },
    {
      icon: 'layers' as const,
      title: 'Слияние RRF',
      note: `объединено в ${String(mergedCount)} без дублей`,
      tint: 'bg-muted text-ink-muted',
    },
  ]

  return (
    <aside className="flex w-[326px] shrink-0 flex-col gap-3.5">
      <section className="border-line bg-surface flex flex-col gap-3 rounded-2xl border px-[18px] py-4">
        <h2 className="text-[14px] font-bold">Как собрана выдача</h2>

        {rows.map((row) => (
          <div key={row.title} className="flex items-start gap-2.5">
            <span
              className={`mt-px flex size-[22px] shrink-0 items-center justify-center rounded-full ${row.tint}`}
            >
              <Icon name={row.icon} className="text-[12px]" />
            </span>
            <div className="flex-1">
              <div className="text-[13px] font-bold">{row.title}</div>
              <div className="text-ink-muted text-[12px]">{row.note}</div>
            </div>
          </div>
        ))}

        <div className="bg-line-soft h-px" />
        <p className="text-ink-muted text-[12px] leading-[1.55] text-pretty">
          Оба индекса фильтруются по текущему space внутри запроса. Фрагмент из чужого space не
          может попасть в слияние.
        </p>
      </section>

      <section className="border-line bg-surface flex flex-col gap-2.5 rounded-2xl border px-[18px] py-4">
        <h2 className="text-[14px] font-bold">Что теперь находится</h2>
        <ul className="flex flex-col gap-2 text-[12px] leading-[1.5]">
          {[
            'Номера полиса, СНИЛС, ИНН, серии документов',
            'Точная фраза в кавычках и минус-слова',
            'Редкая фамилия, название организации',
          ].map((line) => (
            <li key={line} className="flex items-start gap-[9px]">
              <Icon name="check" className="text-sage-strong mt-0.5 text-[14px]" />
              {line}
            </li>
          ))}
        </ul>
        <div className="bg-line-soft h-px" />
        <p className="text-ink-muted text-[12px] leading-[1.55] text-pretty">
          Вопросы «сколько» и «все» по-прежнему строятся по топ-K и полноту не гарантируют — об этом
          сказано в ответе.
        </p>
      </section>

      <section className="bg-raised flex flex-col gap-2 rounded-2xl px-4 py-3.5">
        <SectionLabel>Конвейер</SectionLabel>
        <p className="text-ink-muted text-[12px] leading-[1.55] text-pretty">
          Индексация двойная: чанки уходят и в pgvector, и в OpenSearch. В инбоксе это последняя
          стадия «Полнотекстовый индекс».
        </p>
      </section>
    </aside>
  )
}
