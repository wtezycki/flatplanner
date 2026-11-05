import { useQuery } from '@tanstack/react-query'
import { Link, Route, Routes } from 'react-router'
import { metaQuery } from './api.ts'
import { formatDate } from './format.ts'
import ListingsPage from './pages/ListingsPage.tsx'

export default function App() {
  const meta = useQuery(metaQuery)
  const dataAsOf = meta.data?.dataAsOf

  return (
    <>
      <header className="header">
        <div className="container header__inner">
          <Link to="/" className="header__brand">
            Flatplanner
          </Link>
          {dataAsOf && (
            <span className="header__meta">
              <span className="header__meta-long">Mieszkania na sprzedaż · Trójmiasto · dane z </span>
              {formatDate(dataAsOf)}
            </span>
          )}
        </div>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<ListingsPage />} />
        </Routes>
      </main>
    </>
  )
}
