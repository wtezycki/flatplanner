import { useQuery } from '@tanstack/react-query'
import { useEffect, useId, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useSearchParams } from 'react-router'
import { metaQuery } from '../api.ts'
import type { EnumOption, MetaResponse, SearchIntent } from '../api.ts'
import { formatArea } from '../format.ts'
import { FILTER_PARAMS, activeFilterCount, clearFilters, intentFromSearchParams } from '../intent.ts'

interface FilterForm {
  cities: string[]
  districts: string[]
  priceMin: string
  priceMax: string
  areaMin: string
  areaMax: string
  roomsMin: string
  roomsMax: string
  floorMin: string
  floorMax: string
  buildYearMin: string
  buildYearMax: string
  pricePerM2Min: string
  pricePerM2Max: string
  rentMin: string
  rentMax: string
  market: string
  advertiserType: string
  finishConditions: string[]
  ownerships: string[]
  buildingTypes: string[]
  heatings: string[]
  buildingMaterials: string[]
  requireLift: boolean
  requireBalcony: boolean
  requireGarage: boolean
  requireBasement: boolean
}

type RangeField = 'price' | 'area' | 'rooms' | 'floor' | 'buildYear' | 'pricePerM2' | 'rent'

const TEXT_DEBOUNCE_MS = 500

const ENUM_PARAMS = ['market', 'advertiserType'] as const
const MULTI_PARAMS = ['cities', 'districts', 'finishConditions', 'ownerships', 'buildingTypes', 'heatings', 'buildingMaterials'] as const
const FLAG_PARAMS = ['requireLift', 'requireBalcony', 'requireGarage', 'requireBasement'] as const

const fieldText = (value: number | undefined) => (value === undefined ? '' : String(value))

function formFromIntent(intent: SearchIntent): FilterForm {
  return {
    cities: intent.cities,
    districts: intent.districts,
    priceMin: fieldText(intent.priceMin),
    priceMax: fieldText(intent.priceMax),
    areaMin: fieldText(intent.areaMin),
    areaMax: fieldText(intent.areaMax),
    roomsMin: fieldText(intent.roomsMin),
    roomsMax: fieldText(intent.roomsMax),
    floorMin: fieldText(intent.floorMin),
    floorMax: fieldText(intent.floorMax),
    buildYearMin: fieldText(intent.buildYearMin),
    buildYearMax: fieldText(intent.buildYearMax),
    pricePerM2Min: fieldText(intent.pricePerM2Min),
    pricePerM2Max: fieldText(intent.pricePerM2Max),
    rentMin: fieldText(intent.rentMin),
    rentMax: fieldText(intent.rentMax),
    market: intent.market ?? '',
    advertiserType: intent.advertiserType ?? '',
    finishConditions: intent.finishConditions,
    ownerships: intent.ownerships,
    buildingTypes: intent.buildingTypes,
    heatings: intent.heatings,
    buildingMaterials: intent.buildingMaterials,
    requireLift: intent.requireLift,
    requireBalcony: intent.requireBalcony,
    requireGarage: intent.requireGarage,
    requireBasement: intent.requireBasement,
  }
}

interface RangeSpec {
  field: RangeField
  integer: boolean
}

const RANGE_SPECS: RangeSpec[] = [
  { field: 'price', integer: true },
  { field: 'area', integer: false },
  { field: 'rooms', integer: true },
  { field: 'floor', integer: true },
  { field: 'buildYear', integer: true },
  { field: 'pricePerM2', integer: true },
  { field: 'rent', integer: true },
]

const RANGE_ERRORS = {
  notNumber: 'Wpisz liczbę.',
  order: 'Wartość „od” nie może być większa niż „do”.',
}

interface RangeIssue {
  message: string
  min: boolean
  max: boolean
}

const sanitizeBound = (raw: string, integer: boolean) => raw.replace(integer ? /[^\d\s]/g : /[^\d\s.,]/g, '')

function parseBound(raw: string): { value?: number; message?: string } {
  const text = raw.replace(/\s/g, '').replace(',', '.')
  if (!text) {
    return {}
  }
  const value = Number(text)
  return Number.isFinite(value) ? { value } : { message: RANGE_ERRORS.notNumber }
}

function parseRange(form: FilterForm, spec: RangeSpec) {
  const min = parseBound(form[`${spec.field}Min`])
  const max = parseBound(form[`${spec.field}Max`])
  const message = min.message ?? max.message
  let issue: RangeIssue | undefined
  if (message) {
    issue = { message, min: min.message !== undefined, max: max.message !== undefined }
  } else if (min.value !== undefined && max.value !== undefined && min.value > max.value) {
    issue = { message: RANGE_ERRORS.order, min: true, max: true }
  }
  return { min: min.value, max: max.value, issue }
}

function applyFilters(searchParams: URLSearchParams, form: FilterForm): URLSearchParams {
  const next = new URLSearchParams(searchParams)
  for (const param of FILTER_PARAMS) {
    if (param !== 'areaApprox') {
      next.delete(param)
    }
  }
  next.delete('page')

  for (const param of MULTI_PARAMS) {
    for (const value of form[param]) {
      next.append(param, value)
    }
  }
  for (const spec of RANGE_SPECS) {
    const { min, max, issue } = parseRange(form, spec)
    if (issue) {
      continue
    }
    if (min !== undefined) {
      next.set(`${spec.field}Min`, String(min))
    }
    if (max !== undefined) {
      next.set(`${spec.field}Max`, String(max))
    }
  }
  for (const param of ENUM_PARAMS) {
    if (form[param]) {
      next.set(param, form[param])
    }
  }
  for (const param of FLAG_PARAMS) {
    if (form[param]) {
      next.set(param, 'true')
    }
  }

  if (next.has('areaMin') || next.has('areaMax')) {
    next.delete('areaApprox')
  }
  if (form.cities.length === 0) {
    next.delete('requestedCityOutOfScope')
  }
  return next
}

function toggle(values: string[], value: string): string[] {
  return values.includes(value) ? values.filter((v) => v !== value) : [...values, value]
}

interface RangeProps {
  legend: string
  label: string
  field: RangeField
  form: FilterForm
  onChange: (patch: Partial<FilterForm>) => void
  hint?: string
}

function Range({ legend, label, field, form, onChange, hint }: RangeProps) {
  const minKey = `${field}Min` as const
  const maxKey = `${field}Max` as const
  const errorId = useId()
  const spec = RANGE_SPECS.find((candidate) => candidate.field === field)
  const integer = spec?.integer ?? true
  const issue = spec ? parseRange(form, spec).issue : undefined
  const bound = (key: typeof minKey | typeof maxKey, suffix: 'od' | 'do', invalid: boolean | undefined) => (
    <input
      className="field"
      type="text"
      inputMode={integer ? 'numeric' : 'decimal'}
      aria-label={`${label} ${suffix}`}
      placeholder={suffix}
      value={form[key]}
      aria-invalid={invalid || undefined}
      aria-describedby={issue ? errorId : undefined}
      onChange={(event) => onChange({ [key]: sanitizeBound(event.target.value, integer) })}
    />
  )
  return (
    <fieldset className="filters__group">
      <legend>{legend}</legend>
      <div className="filters__pair">
        {bound(minKey, 'od', issue?.min)}
        {bound(maxKey, 'do', issue?.max)}
      </div>
      {issue && (
        <p id={errorId} className="filters__error">
          {issue.message}
        </p>
      )}
      {hint && <p className="filters__hint">{hint}</p>}
    </fieldset>
  )
}

interface PillProps {
  type: 'checkbox' | 'radio'
  name?: string
  checked: boolean
  onChange: (checked: boolean) => void
  children: ReactNode
}

function Pill({ type, name, checked, onChange, children }: PillProps) {
  return (
    <label className="pill">
      <input
        className="pill__input"
        type={type}
        name={name}
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
      />
      <span className="pill__label">{children}</span>
    </label>
  )
}

interface RadiosProps {
  legend: string
  name: string
  value: string
  options: EnumOption[]
  onChange: (value: string) => void
}

function Radios({ legend, name, value, options, onChange }: RadiosProps) {
  return (
    <fieldset className="filters__group">
      <legend>{legend}</legend>
      <div className="filters__pills">
        {[{ value: '', label: 'dowolny' }, ...options].map((option) => (
          <Pill key={option.value} type="radio" name={name} checked={value === option.value} onChange={() => onChange(option.value)}>
            {option.label}
          </Pill>
        ))}
      </div>
    </fieldset>
  )
}

interface ChecksProps {
  legend: string
  values: string[]
  options: EnumOption[]
  onChange: (values: string[]) => void
}

function Checks({ legend, values, options, onChange }: ChecksProps) {
  return (
    <fieldset className="filters__group">
      <legend>{legend}</legend>
      <div className="filters__pills">
        {options.map((option) => (
          <Pill key={option.value} type="checkbox" checked={values.includes(option.value)} onChange={() => onChange(toggle(values, option.value))}>
            {option.label}
          </Pill>
        ))}
      </div>
    </fieldset>
  )
}

interface FieldsProps {
  meta: MetaResponse
  intent: SearchIntent
}

function FilterFields({ meta, intent }: FieldsProps) {
  const [searchParams, setSearchParams] = useSearchParams()
  const latestParams = useRef(searchParams)
  const timer = useRef<number | undefined>(undefined)
  const urlForm = formFromIntent(intent)
  const urlKey = JSON.stringify(urlForm)
  const [form, setForm] = useState(urlForm)
  const [seenUrlKey, setSeenUrlKey] = useState(urlKey)

  if (urlKey !== seenUrlKey) {
    setSeenUrlKey(urlKey)
    if (urlKey !== JSON.stringify(formFromIntent(intentFromSearchParams(applyFilters(searchParams, form))))) {
      setForm(urlForm)
    }
  }

  useEffect(() => {
    latestParams.current = searchParams
  })
  useEffect(() => () => window.clearTimeout(timer.current), [])

  const commit = (next: FilterForm) => {
    const params = applyFilters(latestParams.current, next)
    if (params.toString() === latestParams.current.toString()) {
      return
    }
    setSearchParams(params)
  }

  const change = (changes: Partial<FilterForm>, delayMs = 0) => {
    const next = { ...form, ...changes }
    setForm(next)
    window.clearTimeout(timer.current)
    if (delayMs === 0) {
      commit(next)
    } else {
      timer.current = window.setTimeout(() => commit(next), delayMs)
    }
  }
  const patch = (changes: Partial<FilterForm>) => change(changes)
  const patchText = (changes: Partial<FilterForm>) => change(changes, TEXT_DEBOUNCE_MS)

  const toggleCity = (slug: string) => {
    const cities = toggle(form.cities, slug)
    patch({ cities, districts: form.districts.filter((key) => cities.includes(key.split(':')[0])) })
  }

  const clear = () => {
    window.clearTimeout(timer.current)
    setSearchParams(clearFilters(searchParams))
  }

  const selectedCities = meta.cities.filter((city) => form.cities.includes(city.slug))
  const moreActive =
    [form.buildYearMin, form.buildYearMax, form.pricePerM2Min, form.pricePerM2Max, form.rentMin, form.rentMax].some(Boolean) ||
    [form.finishConditions, form.ownerships, form.buildingTypes, form.buildingMaterials, form.heatings].some((values) => values.length > 0)
  const approximateArea = intent.areaMin === undefined && intent.areaMax === undefined ? intent.areaApprox : undefined

  return (
    <form className="filters__form" aria-label="Filtry" onSubmit={(event) => event.preventDefault()}>
      <div className="filters__head">
        <button className="btn btn--link" type="button" onClick={clear}>
          Wyczyść filtry
        </button>
      </div>

      <fieldset className="filters__group">
        <legend>Miasto</legend>
        <div className="filters__pills">
          {meta.cities.map((city) => (
            <Pill key={city.slug} type="checkbox" checked={form.cities.includes(city.slug)} onChange={() => toggleCity(city.slug)}>
              {city.name} ({city.count})
            </Pill>
          ))}
        </div>
      </fieldset>

      <details className="filters__details">
        <summary>Dzielnica{form.districts.length > 0 && ` (${form.districts.length})`}</summary>
        {selectedCities.length === 0 ? (
          <p className="filters__hint">Wybierz miasto, aby zobaczyć dzielnice.</p>
        ) : (
          <div className="filters__districts">
            {selectedCities.map((city) => (
              <div key={city.slug}>
                <p className="filters__city-title">{city.name}</p>
                <div className="filters__pills">
                  {city.districts.map((district) => (
                    <Pill
                      key={district.key}
                      type="checkbox"
                      checked={form.districts.includes(district.key)}
                      onChange={() => patch({ districts: toggle(form.districts, district.key) })}
                    >
                      {district.name} ({district.count})
                    </Pill>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </details>

      <Range
        legend="Cena (zł)"
        label="Cena"
        field="price"
        form={form}
        onChange={patchText}
      />
      <Range
        legend="Powierzchnia (m²)"
        label="Powierzchnia"
        field="area"
        form={form}
        onChange={patchText}
        hint={
          approximateArea === undefined
            ? undefined
            : `Teraz: ok. ${formatArea(approximateArea)}. Wpisany zakres zastąpi przybliżenie.`
        }
      />
      <Range
        legend="Pokoje"
        label="Pokoje"
        field="rooms"
        form={form}
        onChange={patchText}
      />
      <Range
        legend="Piętro"
        label="Piętro"
        field="floor"
        form={form}
        onChange={patchText}
      />

      <Radios
        legend="Rynek"
        name="market"
        value={form.market}
        options={meta.enums.market}
        onChange={(market) => patch({ market })}
      />
      <Radios
        legend="Ogłaszający"
        name="advertiserType"
        value={form.advertiserType}
        options={meta.enums.advertiserType}
        onChange={(advertiserType) => patch({ advertiserType })}
      />

      <fieldset className="filters__group">
        <legend>Udogodnienia</legend>
        <div className="filters__pills">
          <Pill type="checkbox" checked={form.requireLift} onChange={(requireLift) => patch({ requireLift })}>
            Winda
          </Pill>
          <Pill type="checkbox" checked={form.requireBalcony} onChange={(requireBalcony) => patch({ requireBalcony })}>
            Balkon
          </Pill>
          <Pill type="checkbox" checked={form.requireGarage} onChange={(requireGarage) => patch({ requireGarage })}>
            Garaż
          </Pill>
          <Pill type="checkbox" checked={form.requireBasement} onChange={(requireBasement) => patch({ requireBasement })}>
            Piwnica
          </Pill>
        </div>
      </fieldset>

      <details className="filters__details" open={moreActive}>
        <summary>Więcej filtrów</summary>
        <div className="filters__more">
          <Range legend="Rok budowy" label="Rok budowy" field="buildYear" form={form} onChange={patchText} />
          <Range legend="Cena za m² (zł)" label="Cena za m²" field="pricePerM2" form={form} onChange={patchText} />
          <Range legend="Czynsz (zł/mies.)" label="Czynsz" field="rent" form={form} onChange={patchText} />
          <Checks
            legend="Stan wykończenia"
            values={form.finishConditions}
            options={meta.enums.finishCondition}
            onChange={(finishConditions) => patch({ finishConditions })}
          />
          <Checks
            legend="Forma własności"
            values={form.ownerships}
            options={meta.enums.ownership}
            onChange={(ownerships) => patch({ ownerships })}
          />
          <Checks
            legend="Typ budynku"
            values={form.buildingTypes}
            options={meta.enums.buildingType}
            onChange={(buildingTypes) => patch({ buildingTypes })}
          />
          <Checks
            legend="Materiał budynku"
            values={form.buildingMaterials}
            options={meta.enums.buildingMaterial}
            onChange={(buildingMaterials) => patch({ buildingMaterials })}
          />
          <Checks
            legend="Ogrzewanie"
            values={form.heatings}
            options={meta.enums.heating}
            onChange={(heatings) => patch({ heatings })}
          />
        </div>
      </details>
    </form>
  )
}

export default function Filters() {
  const [searchParams] = useSearchParams()
  const meta = useQuery(metaQuery)
  const [expanded, setExpanded] = useState(false)
  const panelId = useId()
  const intent = intentFromSearchParams(searchParams)

  let content
  if (meta.data) {
    content = <FilterFields meta={meta.data} intent={intent} />
  } else if (meta.isError) {
    content = (
      <div className="notice notice--error">
        <p>Nie udało się wczytać filtrów.</p>
        <button className="btn btn--secondary" type="button" onClick={() => void meta.refetch()}>
          Spróbuj ponownie
        </button>
      </div>
    )
  } else {
    content = <p className="filters__hint">Ładowanie…</p>
  }

  return (
    <aside className="filters">
      <button
        className="btn btn--secondary filters__toggle"
        type="button"
        aria-expanded={expanded}
        aria-controls={panelId}
        onClick={() => setExpanded(!expanded)}
      >
        Filtry ({activeFilterCount(intent)})
      </button>
      <div id={panelId} className={expanded ? 'filters__panel' : 'filters__panel filters__panel--collapsed'}>
        {content}
      </div>
    </aside>
  )
}
