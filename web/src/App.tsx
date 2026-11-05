import { Link, Route, Routes, useMatch } from 'react-router'
import wordmark from './assets/flatbanner.svg'
import logo from './assets/flatplanner.svg'
import SearchBar from './components/SearchBar.tsx'
import ListingDetailPage from './pages/ListingDetailPage.tsx'
import ListingsPage from './pages/ListingsPage.tsx'

export default function App() {
  const onList = useMatch('/') !== null

  return (
    <>
      <header className="header">
        <div className="container header__inner">
          <Link to="/" className="header__brand" aria-label="Flatplanner">
            <img className="header__logo" src={logo} alt="" />
            <span className="header__titles">
              <img className="header__wordmark" src={wordmark} alt="" />
              <span className="header__motto">Przeczytamy ogłoszenia za Ciebie.</span>
            </span>
          </Link>
          {onList && <SearchBar />}
        </div>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<ListingsPage />} />
          <Route path="/listings/:id" element={<ListingDetailPage />} />
        </Routes>
      </main>
    </>
  )
}
