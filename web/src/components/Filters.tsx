import { useQuery } from '@tanstack/react-query'
import { useEffect, useId, useRef, useState } from 'react'
import type { InputHTMLAttributes, ReactNode } from 'react'
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
  market: string
  advertiserType: string
  requireLift: boolean
  requireBalcony: boolean
}

type RangeField = 'price' | 'area' | 'rooms' | 'floor'

const TEXT_DEBOUNCE_MS = 500

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
    market: intent.market ?? '',
    advertiserType: intent.advertiserType ?? '',
    requireLift: intent.requireLift,
    requireBalcony: intent.requireBalcony,
  }
}

function numberValue(raw: string, integer: boolean): string | undefined {
  const value = Number(raw.replace(/\s/g, '').replace(',', '.'))
  if (!raw.trim() || !Number.isFinite(value) || (integer && !Number.isInteger(value))) {
    return undefined
  }
  return String(value)
}

function applyFilters(searchParams: URLSearchParams, form: FilterForm): URLSearchParams {
  const next = new URLSearchParams(searchParams)
  for (const param of FILTER_PARAMS) {
    if (param !== 'areaApprox') {
      next.delete(param)
    }
  }
  next.delete('page')

  for (const city of form.cities) {
    next.append('cities', city)
  }
  for (const district of form.districts) {
    next.append('districts', district)
  }
  const numbers: [string, string, boolean][] = [
    ['priceMin', form.priceMin, true],
    ['priceMax', form.priceMax, true],
    ['areaMin', form.areaMin, false],
    ['areaMax', form.areaMax, false],
    ['roomsMin', form.roomsMin, true],
    ['roomsMax', form.roomsMax, true],
    ['floorMin', form.floorMin, true],
    ['floorMax', form.floorMax, true],
  ]
  for (const [param, raw, integer] of numbers) {
    const value = numberValue(raw, integer)
    if (value !== undefined) {
      next.set(param, value)
    }
  }
  if (form.market) {
    next.set('market', form.market)
  }
  if (form.advertiserType) {
    next.set('advertiserType', form.advertiserType)
  }
  if (form.requireLift) {
    next.set('requireLift', 'true')
  }
  if (form.requireBalcony) {
    next.set('requireBalcony', 'true')
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
  inputProps: InputHTMLAttributes<HTMLInputElement>
  hint?: string
}

function Range({ legend, label, field, form, onChange, inputProps, hint }: RangeProps) {
  const minKey = `${field}Min` as const
  const maxKey = `${field}Max` as const
  return (
    <fieldset className="filters__group">
      <legend>{legend}</legend>
      <div className="filters__pair">
        <input
          className="field"
          aria-label={`${label} od`}
          placeholder="od"
          value={form[minKey]}
          onChange={(event) => onChange({ [minKey]: event.target.value })}
          {...inputProps}
        />
        <input
          className="field"
          aria-label={`${label} do`}
          placeholder="do"
          value={form[maxKey]}
          onChange={(event) => onChange({ [maxKey]: event.target.value })}
          {...inputProps}
        />
      </div>
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
        inputProps={{ type: 'text', inputMode: 'numeric' }}
      />
      <Range
        legend="Powierzchnia (m²)"
        label="Powierzchnia"
        field="area"
        form={form}
        onChange={patchText}
        inputProps={{ type: 'text', inputMode: 'decimal' }}
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
        inputProps={{ type: 'number', min: 1, max: 10 }}
      />
      <Range
        legend="Piętro"
        label="Piętro"
        field="floor"
        form={form}
        onChange={patchText}
        inputProps={{ type: 'number', min: -1, max: 40 }}
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
            Tylko z windą
          </Pill>
          <Pill type="checkbox" checked={form.requireBalcony} onChange={(requireBalcony) => patch({ requireBalcony })}>
            Tylko z balkonem
          </Pill>
        </div>
        <p className="filters__hint">Oferty bez tej informacji zostaną pominięte.</p>
      </fieldset>
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
