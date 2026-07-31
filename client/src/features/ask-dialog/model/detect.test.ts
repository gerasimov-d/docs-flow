import { describe, expect, it } from 'vitest'

import { detectMode, detectStrategy } from './detect'

describe('detectMode', () => {
  it('считает вопросом фразу с вопросительным знаком', () => {
    expect(detectMode('Что было с коленом за последние два года?')).toBe('question')
  })

  it('считает вопросом фразу, начатую вопросительным словом', () => {
    expect(detectMode('когда истекает страховка на машину')).toBe('question')
  })

  it('считает поиском одиночный термин', () => {
    expect(detectMode('гонартроз')).toBe('search')
  })

  it('пустую строку считает поиском — режим по умолчанию', () => {
    expect(detectMode('   ')).toBe('search')
  })
})

describe('detectStrategy', () => {
  it('переключается на точный поиск от кавычек', () => {
    expect(detectStrategy('"полис ОМС"')).toBe('exact')
  })

  it('переключается на точный поиск от минус-слова', () => {
    expect(detectStrategy('полис -стоматология')).toBe('exact')
  })

  it('не принимает дефис внутри слова за минус-слово', () => {
    expect(detectStrategy('2-НДФЛ')).toBe('hybrid')
  })

  it('по умолчанию ищет гибридно', () => {
    expect(detectStrategy('страховка на машину')).toBe('hybrid')
  })
})
