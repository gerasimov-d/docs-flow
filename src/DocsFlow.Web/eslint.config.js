import js from '@eslint/js'
import boundaries from 'eslint-plugin-boundaries'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import prettier from 'eslint-config-prettier'
import globals from 'globals'
import tseslint from 'typescript-eslint'

/**
 * Слои FSD от старшего к младшему. Порядок — не украшение: слой может импортировать
 * только те слои, что стоят ниже него в этом списке. Всё остальное запрещено.
 */
const LAYERS = ['app', 'pages', 'widgets', 'features', 'entities', 'shared']

/**
 * Что разрешено импортировать слою. Слайсы одного слоя друг друга не видят — общий код
 * поднимается на слой ниже. Исключение — `shared`: там не слайсы, а сегменты,
 * и они могут опираться друг на друга.
 */
const importableFrom = (layer) =>
  layer === 'shared' ? ['shared'] : LAYERS.slice(LAYERS.indexOf(layer) + 1)

export default tseslint.config(
  { ignores: ['dist', 'coverage', 'node_modules'] },

  js.configs.recommended,
  tseslint.configs.recommendedTypeChecked,

  {
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
      globals: globals.browser,
    },
  },

  reactHooks.configs.flat['recommended-latest'],
  reactRefresh.configs.vite,

  // Границы FSD. Нарушение архитектуры — ошибка линтера, а не тема для code review.
  {
    files: ['src/**/*.{ts,tsx}'],
    plugins: { boundaries },
    settings: {
      // Без TS-резолвера импорты вида `@/pages/home` не находят `index.ts`,
      // считаются нераспознанными и молча проходят мимо правил.
      'import/resolver': {
        typescript: { alwaysTryTypes: true, project: './tsconfig.app.json' },
      },
      'boundaries/include': ['src/**/*'],
      'boundaries/elements': [
        { type: 'app', pattern: 'src/app' },
        { type: 'pages', pattern: 'src/pages/*', capture: ['slice'] },
        { type: 'widgets', pattern: 'src/widgets/*', capture: ['slice'] },
        { type: 'features', pattern: 'src/features/*', capture: ['slice'] },
        { type: 'entities', pattern: 'src/entities/*', capture: ['slice'] },
        { type: 'shared', pattern: 'src/shared/*', capture: ['segment'] },
      ],
    },
    rules: {
      /*
       * Два правила FSD, по политике на каждое. Выигрывает последняя совпавшая политика,
       * поэтому запрет глубокого импорта идёт после разрешения слоя.
       * Импорты внутри одного слайса и внешние пакеты правило не трогает.
       */
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          message: 'FSD: слою {{from.element.type}} запрещено импортировать {{to.element.type}}',
          policies: [
            // 1. Импортировать можно только нижележащие слои. Соседний слайс того же
            //    слоя сюда не попадает — общий код поднимается слоем ниже.
            ...LAYERS.map((layer) => ({
              from: { element: { type: layer } },
              allow: { to: { element: { types: { anyOf: importableFrom(layer) } } } },
            })),
            // 2. И только через public API: внутренности чужого слайса не видны.
            {
              disallow: { to: { element: { fileInternalPath: '!index.ts' } } },
              message:
                'FSD: импорт в обход public API — у слайса берётся только index.ts, ' +
                'а нужное из него реэкспортируется',
            },
          ],
        },
      ],
    },
  },

  {
    files: ['src/**/*.{ts,tsx}'],
    rules: {
      // При verbatimModuleSyntax импорт типа обязан быть помечен как `import type`.
      '@typescript-eslint/consistent-type-imports': 'error',
    },
  },

  // Barrel-файлы реэкспортируют что угодно — правило про Fast Refresh к ним неприменимо.
  {
    files: ['src/**/index.ts'],
    rules: { 'react-refresh/only-export-components': 'off' },
  },

  // Конфиги в корне — обычный JS, тип-зависимые правила к ним не применяются.
  {
    files: ['**/*.js'],
    extends: [tseslint.configs.disableTypeChecked],
  },

  // Отключает правила, конфликтующие с Prettier. Обязан идти последним.
  prettier,
)
