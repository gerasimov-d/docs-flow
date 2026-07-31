import { screen, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'

import { renderWithProviders } from '@/shared/testing'

import type { Answer } from '../model/dialog'
import { AnswerView } from './AnswerView'

const spaceId = '11111111-1111-1111-1111-111111111111'

const answer: Answer = {
  statements: [
    {
      text: 'Заключение: гонартроз правого коленного сустава I стадии.',
      citation: {
        documentId: 'doc-discharge',
        documentName: 'Выписка из истории болезни.pdf',
        page: 2,
        documentDate: '2026-02-04',
        contextName: 'Медицина',
        index: 1,
        quote: 'Заключение: гонартроз правого коленного сустава I стадии.',
      },
    },
  ],
  emptyReason: null,
  disclaimer: 'Ответ построен по найденным фрагментам, а не по всему содержимому space.',
}

function render(value: Answer, citationStyle: 'chips' | 'footnotes' | 'sidebar' = 'chips') {
  return renderWithProviders(
    <MemoryRouter>
      <AnswerView spaceId={spaceId} answer={value} citationStyle={citationStyle} />
    </MemoryRouter>,
  )
}

describe('AnswerView', () => {
  /*
   * Главное продуктовое обещание: ответа без ссылки на первоисточник не бывает. Проверяется
   * на всех трёх подачах цитат — подача меняет вид ссылки, но не сам факт её наличия.
   */
  it.each(['chips', 'footnotes', 'sidebar'] as const)(
    'в подаче %s каждое утверждение ведёт на страницу первоисточника',
    (style) => {
      render(answer, style)

      const links = screen.getAllByRole('link')

      expect(links.length).toBeGreaterThan(0)
      links.forEach((link) => {
        expect(link).toHaveAttribute('href', `/s/${spaceId}/documents/doc-discharge?page=2`)
      })
    },
  )

  it('показывает оговорку о полноте рядом с ответом', () => {
    render(answer)

    expect(screen.getByText(/по найденным фрагментам/)).toBeInTheDocument()
  })

  it('когда фрагментов нет — отказывается отвечать и не выдумывает источники', () => {
    render({
      statements: [],
      emptyReason: 'В этом space нет документа, который содержал бы ответ.',
      disclaimer: 'Ответ построен по найденным фрагментам.',
    })

    expect(screen.getByText(/нет документа, который содержал бы ответ/)).toBeInTheDocument()
    expect(screen.queryByRole('link')).not.toBeInTheDocument()
    expect(screen.queryByText('Источники')).not.toBeInTheDocument()
  })

  it('в подаче со сносками цитата приводится дословно', () => {
    render(answer, 'footnotes')

    const quote = within(screen.getByRole('link')).getByText(/«.+»/)

    expect(quote.textContent).toContain(answer.statements[0]?.citation.quote)
  })
})
