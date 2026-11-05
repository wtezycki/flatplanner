import { useQuery } from '@tanstack/react-query'
import { lazy, Suspense, useEffect, useState, type ReactNode } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import {
  ApiError,
  DEFAULT_RETRY_AFTER_SECONDS,
  fetchListing,
  isServerOrNetworkError,
  metaQuery,
  type EnumOption,
  type ListingDetail,
} from '../api.ts'
import Description from '../components/Description.tsx'
import PhotoCarousel from '../components/PhotoCarousel.tsx'
import PhotoLightbox from '../components/PhotoLightbox.tsx'
import {
  formatArea,
  formatDate,
  formatDetailFloor,
  formatPrice,
  formatPricePerM2,
  formatRooms,
} from '../format.ts'

const LocationMap = lazy(() => import('../components/LocationMap.tsx'))

const labelOf = (options: EnumOption[] | undefined, value: string | undefined) =>
  value === undefined ? undefined : (options?.find((option) => option.value === value)?.label ?? value)

const yesOrUnknown = (value: boolean | undefined) => (value ? 'tak' : undefined)

function Row({ label, value, missing = 'brak danych' }: { label: string; value: ReactNode; missing?: string }) {
  return (
    <div className="params__row">
      <dt>{label}</dt>
      <dd>{value ?? <span className="value--missing">{missing}</span>}</dd>
    </div>
  )
}

function BackLink() {
  const navigate = useNavigate()
  const location = useLocation()

  return location.key === 'default' ? (
    <Link to="/">‹ Wszystkie oferty</Link>
  ) : (
    <button className="btn btn--link" type="button" onClick={() => void navigate(-1)}>
      ‹ Wróć do wyników
    </button>
  )
}

function Details({ listing }: { listing: ListingDetail }) {
  const meta = useQuery(metaQuery).data
  const [openedPhoto, setOpenedPhoto] = useState<string>()
  const location = [listing.districtName, listing.street, listing.cityName].filter(Boolean).join(', ')
  const floor = formatDetailFloor(listing.floor, listing.buildingFloors, listing.floorHigherThan10, listing.floorIsGarret)
  const rent = listing.rentPln === undefined ? undefined : formatPrice(listing.rentPln)

  useEffect(() => {
    const listTitle = document.title
    document.title = `${listing.title} · Flatplanner`
    return () => {
      document.title = listTitle
    }
  }, [listing.title])

  return (
    <div className="container detail">
      <div className="detail__back">
        <BackLink />
      </div>
      <h1 className="detail__title">{listing.title}</h1>
      <p className="detail__location">
        {location}
        {listing.locationApproximate && (
          <span className="detail__approximate" title="Ogłaszający nie podał dokładnej lokalizacji.">
            {' '}
            · Lokalizacja przybliżona
          </span>
        )}
      </p>
      <div className="detail__layout">
        <div className="detail__gallery">
          <PhotoCarousel urls={listing.imageUrls} onOpen={setOpenedPhoto} />
        </div>
        <div className="detail__info">
          <h2 className="detail__heading">Parametry</h2>
          <dl className="params">
            <Row label="Powierzchnia" value={formatArea(listing.areaM2)} />
            <Row label="Pokoje" value={listing.rooms} />
            <Row label="Piętro" value={floor} />
            <Row label="Rok budowy" value={listing.buildYear} />
            <Row label="Rynek" value={labelOf(meta?.enums.market, listing.market)} />
            <Row label="Stan wykończenia" value={labelOf(meta?.enums.finishCondition, listing.finishCondition)} />
            <Row label="Forma własności" value={labelOf(meta?.enums.ownership, listing.ownership)} />
            <Row label="Czynsz (zł/mies.)" value={rent} />
            <Row label="Winda" value={yesOrUnknown(listing.hasLift)} missing="brak informacji" />
            <Row label="Balkon" value={yesOrUnknown(listing.hasBalcony)} missing="brak informacji" />
            <Row
              label="Garaż / miejsce postojowe"
              value={yesOrUnknown(listing.hasGarage)}
              missing="brak informacji"
            />
            <Row label="Piwnica" value={yesOrUnknown(listing.hasBasement)} missing="brak informacji" />
            <Row label="Ogłaszający" value={labelOf(meta?.enums.advertiserType, listing.advertiserType)} />
            <Row label="Dzielnica" value={listing.districtName} />
            <Row label="Ulica" value={listing.street} />
          </dl>
          <h2 className="detail__heading">Opis</h2>
          <Description text={listing.descriptionText} />
          <p className="detail__source">
            Źródło: otodom.pl · pobrano {formatDate(listing.scrapedAt)} · dodano {formatDate(listing.createdAtSource)}
          </p>
        </div>
        <div className="detail__side">
          <aside className="detail__price-card">
            <p className="detail__price num">{formatPrice(listing.pricePln)}</p>
            <p className="detail__price-per-m2 num">{formatPricePerM2(listing.pricePerM2)}</p>
            <p className="detail__summary">
              {formatArea(listing.areaM2)} · {formatRooms(listing.rooms)}
            </p>
            <a className="btn btn--primary" href={listing.url} target="_blank" rel="noopener noreferrer">
              Zobacz na otodom ↗
            </a>
            <p className="detail__stale">Dane z {formatDate(listing.scrapedAt)}. Aktualną ofertę sprawdź na otodom.</p>
          </aside>
          <section className="detail__map">
            <h2 className="detail__heading">Lokalizacja</h2>
            <Suspense fallback={<div className="map" />}>
              <LocationMap lat={listing.lat} lng={listing.lng} approximate={listing.locationApproximate} />
            </Suspense>
            {listing.locationApproximate && <p className="detail__map-note">Lokalizacja przybliżona</p>}
          </section>
        </div>
      </div>
      {openedPhoto && (
        <PhotoLightbox
          urls={listing.imageUrls}
          startUrl={openedPhoto}
          title={listing.title}
          onClose={() => setOpenedPhoto(undefined)}
        />
      )}
    </div>
  )
}

function Skeleton() {
  return <div className="container detail detail--skeleton" aria-busy="true" />
}

function DetailError({ error, onRetry }: { error: Error; onRetry: () => void }) {
  if (error instanceof ApiError && error.status === 404) {
    return (
      <div className="container detail">
        <div className="notice notice--error">
          <p>Nie znaleziono oferty.</p>
          <Link to="/">Wróć do listy ofert</Link>
        </div>
      </div>
    )
  }
  if (error instanceof ApiError && error.status === 429) {
    return (
      <div className="container detail">
        <div className="notice notice--warning">
          <p>Za dużo zapytań. Spróbuj ponownie za {error.retryAfter ?? DEFAULT_RETRY_AFTER_SECONDS} s.</p>
        </div>
      </div>
    )
  }
  return (
    <div className="container detail">
      <div className="notice notice--error">
        <p>Nie udało się pobrać oferty.</p>
        <button className="btn btn--secondary" type="button" onClick={onRetry}>
          Spróbuj ponownie
        </button>
      </div>
    </div>
  )
}

export default function ListingDetailPage() {
  const { id = '' } = useParams()
  const meta = useQuery(metaQuery)
  const listing = useQuery({
    queryKey: ['listing', id],
    queryFn: ({ signal }) => fetchListing(id, signal),
    retry: (failures, error) => isServerOrNetworkError(error) && failures < 1,
  })

  if (listing.isError) {
    return <DetailError error={listing.error} onRetry={() => void listing.refetch()} />
  }
  return listing.data && !meta.isPending ? <Details listing={listing.data} /> : <Skeleton />
}
