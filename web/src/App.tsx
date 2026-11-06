import { Link, Navigate, Route, Routes, useLocation, useParams } from 'react-router'
import wordmark from './assets/flatbanner.svg'
import logo from './assets/flatplanner.svg'
import AskPage from './pages/AskPage.tsx'

function LegacyListings() {
  const { search } = useLocation()
  return <Navigate to={{ pathname: '/', search }} replace />
}

function LegacyListing() {
  const { id = '' } = useParams()
  return <Navigate to={{ pathname: '/', search: new URLSearchParams({ listing: id }).toString() }} replace />
}

export default function App() {
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
        </div>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<AskPage />} />
          <Route path="/listings" element={<LegacyListings />} />
          <Route path="/listings/:id" element={<LegacyListing />} />
        </Routes>
      </main>
    </>
  )
}
