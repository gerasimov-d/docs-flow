import { createBrowserRouter } from 'react-router'

import { ContextsPage } from '@/pages/contexts'
import { DialogsPage } from '@/pages/dialogs'
import { DocumentPage } from '@/pages/document'
import { InboxPage } from '@/pages/inbox'
import { LibraryPage } from '@/pages/library'
import { LoginPage } from '@/pages/login'
import { ProfilePage } from '@/pages/profile'
import { SpaceSettingsPage } from '@/pages/space-settings'

import { RequireAuth } from './require-auth'
import { SpaceHome, SpaceLayout, SpaceScreen } from './space-layout'

/**
 * Карта маршрутов. Всё, кроме страницы входа, лежит под `RequireAuth`: закрытым по умолчанию
 * ошибиться сложнее, чем открытым, где легко забыть навесить проверку на новый маршрут.
 *
 * Идентификатор space стоит в пути каждого экрана с данными. Так ссылка на документ остаётся
 * ссылкой на документ — её можно сохранить, послать себе и открыть после повторного входа, —
 * а арендатор берётся из адреса, который проверяется, а не из состояния клиента.
 */
export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    element: <RequireAuth />,
    children: [
      { path: '/', element: <SpaceHome /> },
      {
        path: '/s/:spaceId',
        element: <SpaceLayout />,
        children: [
          {
            index: true,
            element: <SpaceScreen>{(space) => <InboxPage space={space} />}</SpaceScreen>,
          },
          {
            path: 'inbox',
            element: <SpaceScreen>{(space) => <InboxPage space={space} />}</SpaceScreen>,
          },
          {
            path: 'library',
            element: <SpaceScreen>{(space) => <LibraryPage space={space} />}</SpaceScreen>,
          },
          {
            path: 'documents/:documentId',
            element: <SpaceScreen>{(space) => <DocumentPage space={space} />}</SpaceScreen>,
          },
          {
            path: 'dialogs',
            element: <SpaceScreen>{(space) => <DialogsPage space={space} />}</SpaceScreen>,
          },
          {
            path: 'contexts',
            element: <SpaceScreen>{(space) => <ContextsPage space={space} />}</SpaceScreen>,
          },
          {
            path: 'settings',
            element: <SpaceScreen>{(space) => <SpaceSettingsPage space={space} />}</SpaceScreen>,
          },
          {
            path: 'profile',
            element: <SpaceScreen>{(space) => <ProfilePage space={space} />}</SpaceScreen>,
          },
        ],
      },
    ],
  },
])
