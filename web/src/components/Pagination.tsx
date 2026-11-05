import { Link, useSearchParams } from 'react-router'

interface Props {
  page: number
  totalPages: number
}

function pagesToShow(page: number, totalPages: number): number[] {
  const wanted = new Set([1, totalPages, page - 1, page, page + 1])
  return [...wanted].filter((p) => p >= 1 && p <= totalPages).sort((a, b) => a - b)
}

export default function Pagination({ page, totalPages }: Props) {
  const [searchParams] = useSearchParams()

  if (totalPages <= 1) {
    return null
  }

  const target = (n: number) => {
    const next = new URLSearchParams(searchParams)
    if (n === 1) {
      next.delete('page')
    } else {
      next.set('page', String(n))
    }
    return { search: next.toString() }
  }
  const scrollToTop = () => window.scrollTo(0, 0)
  const pages = pagesToShow(page, totalPages)

  return (
    <nav className="pagination" aria-label="Paginacja">
      {page > 1 ? (
        <Link className="pagination__step" to={target(page - 1)} aria-label="Poprzednia strona" onClick={scrollToTop}>
          ‹<span className="pagination__step-text"> Poprzednia</span>
        </Link>
      ) : (
        <span className="pagination__step pagination__step--disabled" aria-disabled="true">
          ‹<span className="pagination__step-text"> Poprzednia</span>
        </span>
      )}
      <ul className="pagination__numbers">
        {pages.map((n, index) => (
          <li key={n} className="pagination__item">
            {index > 0 && n - pages[index - 1] > 1 && (
              <span className="pagination__gap" aria-hidden="true">
                …
              </span>
            )}
            {n === page ? (
              <span className="pagination__number pagination__number--current" aria-current="page">
                {n}
              </span>
            ) : (
              <Link className="pagination__number" to={target(n)} onClick={scrollToTop}>
                {n}
              </Link>
            )}
          </li>
        ))}
      </ul>
      <span className="pagination__compact num">
        {page} / {totalPages}
      </span>
      {page < totalPages ? (
        <Link className="pagination__step" to={target(page + 1)} aria-label="Następna strona" onClick={scrollToTop}>
          <span className="pagination__step-text">Następna </span>›
        </Link>
      ) : (
        <span className="pagination__step pagination__step--disabled" aria-disabled="true">
          <span className="pagination__step-text">Następna </span>›
        </span>
      )}
    </nav>
  )
}
