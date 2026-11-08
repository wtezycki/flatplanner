import { Link } from 'react-router'
import type { To } from 'react-router'
import type { ReactNode } from 'react'
import type { ListingItem } from '../api.ts'
import {
  PRICE_OUTLIER_HINT,
  RISK_KIND_HINTS,
  RISK_KIND_LABELS,
  formatArea,
  formatBelowMedian,
  formatFloor,
  formatPrice,
  formatPricePerM2,
  formatRooms,
  isNewListing,
} from '../format.ts'
import type { CardLabels } from '../useCardLabels.ts'
import Icon from './Icon.tsx'
import type { IconName } from './Icon.tsx'

const LIMITED_OWNERSHIP = 'limited_ownership'

interface Props {
  item: ListingItem
  labels: CardLabels
  href: To
  priceNote?: string
  children?: ReactNode
}

function Fact({ icon, children }: { icon: IconName; children: string }) {
  return (
    <span className="card__fact">
      <Icon name={icon} />
      {children}
    </span>
  )
}

interface BadgeProps {
  icon: IconName
  variant?: 'positive' | 'warning'
  hint?: string
  children: string
}

function Badge({ icon, variant, hint, children }: BadgeProps) {
  return (
    <span className={variant ? `badge badge--${variant}` : 'badge'} title={hint}>
      <Icon name={icon} />
      {children}
    </span>
  )
}

export default function ListingCardBody({ item, labels, href, priceNote, children }: Props) {
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
    <div className="card__body">
      <p className="card__price-line">
        <span className="card__price num">{formatPrice(item.pricePln)}</span>
        <span className="card__price-per-m2 num">{formatPricePerM2(item.pricePerM2)}</span>
      </p>
      {priceNote && <p className="card__price-note num">{priceNote}</p>}
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
            <Badge key={kind} icon="risk" variant="warning" hint={RISK_KIND_HINTS[kind]}>
              {RISK_KIND_LABELS[kind] ?? kind}
            </Badge>
          ))}
          {item.priceOutlier && (
            <Badge icon="risk" variant="warning" hint={PRICE_OUTLIER_HINT}>
              nietypowa cena, sprawdź opis
            </Badge>
          )}
          {item.belowMedianPercent !== undefined && (
            <Badge icon="cheaper" variant="positive" hint="Cena za m² niższa niż mediana ofert z tego miasta (bez ofert z nietypową ceną).">
              {formatBelowMedian(item.belowMedianPercent)}
            </Badge>
          )}
          {item.nearbyStop && (
            <Badge icon="stop" hint="Najbliższy przystanek, odległość w linii prostej (dane OpenStreetMap).">
              {item.nearbyStop}
            </Badge>
          )}
          {item.nearbyPark && (
            <Badge icon="park" hint="Najbliższy park, odległość w linii prostej (dane OpenStreetMap).">
              {item.nearbyPark}
            </Badge>
          )}
        </p>
      )}
      <p className="card__badges card__badges--features">
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
      {children}
    </div>
  )
}
