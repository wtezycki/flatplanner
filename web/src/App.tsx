import { Link, Route, Routes } from 'react-router'
import logo from './assets/flatplanner.svg'
import ListingsPage from './pages/ListingsPage.tsx'

export default function App() {
  return (
    <>
      <header className="header">
        <div className="container header__inner">
          <Link to="/" className="header__brand" aria-label="Flatplanner">
            <img className="header__logo" src={logo} alt="" />
            <span className="header__wordmark" aria-hidden="true" />
          </Link>
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
