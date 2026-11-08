import { Link, useSearchParams } from 'react-router'
import type { ListingItem } from '../api.ts'
import { listingSearch } from '../intent.ts'
import {
  RISK_KIND_LABELS,
  formatArea,
  formatBelowMedian,
  formatFloor,
  formatPrice,
  formatPricePerM2,
  formatRooms,
  isNewListing,
} from '../format.ts'
import Icon from './Icon.tsx'
import type { IconName } from './Icon.tsx'
import PhotoCarousel from './PhotoCarousel.tsx'

const LIMITED_OWNERSHIP = 'limited_ownership'

interface Props {
  item: ListingItem
  labels: { market?: string; finishCondition?: string; ownership?: string }
}

function Fact({ icon, children }: { icon: IconName; children: string }) {
  return (
    <span className="card__fact">
      <Icon name={icon} />
      {children}
    </span>
  )
}

function Badge({ icon, variant, children }: { icon: IconName; variant?: 'danger' | 'positive' | 'warning'; children: string }) {
  return (
    <span className={variant ? `badge badge--${variant}` : 'badge'}>
      <Icon name={icon} />
      {children}
    </span>
  )
}

export default function ListingCard({ item, labels }: Props) {
  const [searchParams] = useSearchParams()
  const href = { search: listingSearch(searchParams, item.id) }
  const location = item.districtName ? `${item.districtName}, ${item.cityName}` : item.cityName
  const amenities: [boolean | undefined, IconName, string][] = [
    [item.hasLift, 'lift', 'Winda'],
    [item.hasBalcony, 'balcony', 'Balkon'],
    [item.hasGarage, 'garage', 'Garaż'],
    [item.hasBasement, 'basement', 'Piwnica'],
  ]
  const hasSignals =
    item.riskKinds.length > 0 ||
    item.belowMedianPercent !== undefined ||
    item.priceOutlier ||
    item.nearbyStop !== undefined ||
    item.nearbyPark !== undefined

  return (
    <article className="card">
      <PhotoCarousel urls={item.imageUrls} href={href} />
      <div className="card__body">
        <p className="card__price-line">
          <span className="card__price num">{formatPrice(item.pricePln)}</span>
          <span className="card__price-per-m2 num">{formatPricePerM2(item.pricePerM2)}</span>
        </p>
        <p className="card__parameters">
          <Fact icon="area">{formatArea(item.areaM2)}</Fact>
          <Fact icon="rooms">{formatRooms(item.rooms)}</Fact>
          {item.floor !== undefined && (
            <Fact icon="floor">{formatFloor(item.floor, item.buildingFloors, item.floorHigherThan10)}</Fact>
          )}
          {item.buildYear !== undefined && <Fact icon="year">{String(item.buildYear)}</Fact>}
        </p>
        <p className="card__location">
          {item.locationApproximate ? (
            <span className="card__approximate" title="Ogłaszający nie podał dokładnej lokalizacji." role="img" aria-label="Lokalizacja przybliżona">
              <Icon name="pinApprox" />
            </span>
          ) : (
            <Icon name="pin" />
          )}
          {location}
        </p>
        <p className="card__title">
          <Link className="card__link" to={href}>
            {item.title}
          </Link>
        </p>
        {hasSignals && (
          <p className="card__badges card__badges--signals">
            {item.riskKinds.map((kind) => (
              <Badge key={kind} icon="risk" variant="danger">
                {RISK_KIND_LABELS[kind] ?? kind}
              </Badge>
            ))}
            {item.priceOutlier && (
              <Badge icon="risk" variant="warning">
                nietypowa cena, sprawdź opis
              </Badge>
            )}
            {item.belowMedianPercent !== undefined && (
              <Badge icon="cheaper" variant="positive">
                {formatBelowMedian(item.belowMedianPercent)}
              </Badge>
            )}
            {item.nearbyStop && <Badge icon="stop">{item.nearbyStop}</Badge>}
            {item.nearbyPark && <Badge icon="park">{item.nearbyPark}</Badge>}
          </p>
        )}
        <p className="card__badges">
          {isNewListing(item.createdAtSource) && (
            <Badge icon="fresh" variant="positive">
              nowa oferta
            </Badge>
          )}
          {amenities.map(
            ([present, icon, label]) =>
              present === true && (
                <Badge key={icon} icon={icon}>
                  {label}
                </Badge>
              ),
          )}
          {labels.finishCondition && <Badge icon="finish">{labels.finishCondition}</Badge>}
          {item.ownership === LIMITED_OWNERSHIP && labels.ownership && <Badge icon="ownership">{labels.ownership}</Badge>}
          {labels.market && <Badge icon="market">{labels.market}</Badge>}
        </p>
      </div>
    </article>
  )
}
