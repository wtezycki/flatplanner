import { useQuery } from '@tanstack/react-query'
import { useEffect, useState, type ReactNode } from 'react'
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
import Icon, { type IconName } from '../components/Icon.tsx'
import LocationSection from '../components/LocationSection.tsx'
import PhotoCarousel from '../components/PhotoCarousel.tsx'
import PhotoLightbox from '../components/PhotoLightbox.tsx'
import PhotoMosaic from '../components/PhotoMosaic.tsx'
import RiskBox from '../components/RiskBox.tsx'
import {
  formatArea,
  formatDate,
  formatDetailFloor,
  formatPrice,
  formatPricePerM2,
} from '../format.ts'

const labelOf = (options: EnumOption[] | undefined, value: string | undefined) =>
  value === undefined ? undefined : (options?.find((option) => option.value === value)?.label ?? value)

const yesOrUnknown = (value: boolean | undefined) => (value ? 'tak' : undefined)

interface RowProps {
  icon: IconName
  label: string
  value: ReactNode
  missing?: string
}

function Row({ icon, label, value, missing = 'brak danych' }: RowProps) {
  return (
    <div className="params__row">
      <dt>
        <Icon name={icon} />
        {label}
      </dt>
      <dd>{value ?? <span className="value--missing">{missing}</span>}</dd>
    </div>
  )
}

function KeyFact({ icon, label, value }: { icon: IconName; label: string; value: ReactNode }) {
  return (
    <li className="key-facts__item">
      <Icon name={icon} />
      <span className="key-facts__value">{value ?? <span className="key-facts__missing">brak danych</span>}</span>
      <span className="key-facts__label">{label}</span>
    </li>
  )
}

function BackLink({ onClose }: { onClose: () => void }) {
  return (
    <button className="btn btn--link" type="button" onClick={onClose}>
      ‹ Wróć do wyników
    </button>
  )
}

function Details({ listing, onClose }: { listing: ListingDetail; onClose: () => void }) {
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
        <BackLink onClose={onClose} />
      </div>
      <article className="sheet">
        <div className="detail__gallery detail__gallery--mosaic">
          <PhotoMosaic urls={listing.imageUrls} onOpen={setOpenedPhoto} />
        </div>
        <div className="detail__gallery detail__gallery--carousel">
          <PhotoCarousel urls={listing.imageUrls} onOpen={setOpenedPhoto} />
        </div>
        <header className="sheet__head">
          <h1 className="detail__title">{listing.title}</h1>
          <p className="detail__location">
            <span className="detail__address">
              <Icon name="pin" />
              <strong>{location}</strong>
            </span>
            {listing.locationApproximate && (
              <span className="detail__approximate" title="Ogłaszający nie podał dokładnej lokalizacji.">
                Lokalizacja przybliżona
              </span>
            )}
          </p>
        </header>
        <div className="sheet__price">
          <div>
            <p className="detail__price num">{formatPrice(listing.pricePln)}</p>
            <p className="detail__price-per-m2 num">{formatPricePerM2(listing.pricePerM2)}</p>
          </div>
          <div className="sheet__source-link">
            <p className="detail__stale">Dane z {formatDate(listing.scrapedAt)}. Aktualną ofertę sprawdź na otodom.</p>
            <a className="btn btn--primary" href={listing.url} target="_blank" rel="noopener noreferrer">
              Zobacz na otodom ↗
            </a>
          </div>
        </div>
        <ul className="key-facts" aria-label="Najważniejsze parametry">
          <KeyFact icon="area" label="Powierzchnia" value={formatArea(listing.areaM2)} />
          <KeyFact icon="rooms" label="Pokoje" value={listing.rooms} />
          <KeyFact icon="floor" label="Piętro" value={floor} />
          <KeyFact icon="year" label="Rok budowy" value={listing.buildYear} />
          <KeyFact icon="ownership" label="Forma własności" value={labelOf(meta?.enums.ownership, listing.ownership)} />
          <KeyFact icon="pin" label="Lokalizacja" value={[listing.street, listing.cityName].filter(Boolean).join(', ')} />
        </ul>
        <RiskBox risks={listing.risks} priceOutlier={listing.priceOutlier} />
        <div className="sheet__split">
          <section>
            <h2 className="detail__heading">Parametry</h2>
            <dl className="params">
              <Row icon="market" label="Rynek" value={labelOf(meta?.enums.market, listing.market)} />
              <Row
                icon="finish"
                label="Stan wykończenia"
                value={labelOf(meta?.enums.finishCondition, listing.finishCondition)}
              />
              <Row icon="building" label="Typ budynku" value={labelOf(meta?.enums.buildingType, listing.buildingType)} />
              <Row
                icon="material"
                label="Materiał budynku"
                value={labelOf(meta?.enums.buildingMaterial, listing.buildingMaterial)}
              />
              <Row icon="windows" label="Okna" value={labelOf(meta?.enums.windowsType, listing.windowsType)} />
              <Row icon="heating" label="Ogrzewanie" value={labelOf(meta?.enums.heating, listing.heating)} />
              <Row icon="rent" label="Czynsz (zł/mies.)" value={rent} />
              <Row icon="lift" label="Winda" value={yesOrUnknown(listing.hasLift)} missing="brak informacji" />
              <Row icon="balcony" label="Balkon" value={yesOrUnknown(listing.hasBalcony)} missing="brak informacji" />
              <Row
                icon="garage"
                label="Garaż / miejsce postojowe"
                value={yesOrUnknown(listing.hasGarage)}
                missing="brak informacji"
              />
              <Row icon="basement" label="Piwnica" value={yesOrUnknown(listing.hasBasement)} missing="brak informacji" />
              <Row
                icon="advertiser"
                label="Ogłaszający"
                value={labelOf(meta?.enums.advertiserType, listing.advertiserType)}
              />
              <Row icon="district" label="Dzielnica" value={listing.districtName} />
            </dl>
          </section>
          <LocationSection lat={listing.lat} lng={listing.lng} approximate={listing.locationApproximate} />
        </div>
        <section className="sheet__section">
          <h2 className="detail__heading">Opis</h2>
          <Description source={listing.descriptionMarkup ?? listing.descriptionText} />
        </section>
        <p className="detail__source">
          Źródło: otodom.pl · pobrano {formatDate(listing.scrapedAt)} · dodano {formatDate(listing.createdAtSource)}
        </p>
      </article>
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

function DetailError({ error, onRetry, onClose }: { error: Error; onRetry: () => void; onClose: () => void }) {
  if (error instanceof ApiError && error.status === 404) {
    return (
      <div className="container detail">
        <div className="notice notice--error">
          <p>Nie znaleziono oferty.</p>
          <button className="btn btn--link" type="button" onClick={onClose}>
            Wróć do listy ofert
          </button>
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

interface Props {
  id: string
  onClose: () => void
}

export default function ListingDetailPage({ id, onClose }: Props) {
  const meta = useQuery(metaQuery)
  const listing = useQuery({
    queryKey: ['listing', id],
    queryFn: ({ signal }) => fetchListing(id, signal),
    retry: (failures, error) => isServerOrNetworkError(error) && failures < 1,
  })

  if (listing.isError) {
    return <DetailError error={listing.error} onRetry={() => void listing.refetch()} onClose={onClose} />
  }
  return listing.data && !meta.isPending ? <Details listing={listing.data} onClose={onClose} /> : <Skeleton />
}
