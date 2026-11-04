# Flatplanner: data acquisition plan

Source selection, geographic scope, scale, field scope and the listing import pipeline from otodom.pl.

As of 2026-11-03. Sources checked manually (curl, no browser).

Resilience to structure changes (§7) is scoped to a one-off import; the decisions in §8 are settled in [design.md](design.md).

## 1. Source: otodom.pl

### Comparison (probe with plain HTTP GET, browser UA)

| Site | HTTP | Structured data | Notes |
|---|---|---|---|
| **otodom.pl** | 200 | `__NEXT_DATA__` (full advertisement JSON) + JSON-LD | Gdańsk: ~8 850 flat sale listings |
| olx.pl | 403 | — | Blocks plain HTTP, requires headless/workarounds |
| gratka.pl, morizon.pl, domiporta.pl, nieruchomosci-online.pl | 200 | JSON-LD only | Data has to be extracted from HTML (CSS selectors, brittle) |

### Why otodom

1. **Data quality at the source.** The listing page contains in `__NEXT_DATA__` an `ad` object with typed fields: price, m², price/m², rooms, floor, number of floors, year built, market (primary/secondary), finish condition, form of ownership, heating, rent (czynsz, maintenance fee), extras (balcony, lift, garage), GPS coordinates, location hierarchy down to the district, creation/modification date, advertiser type (private/agency/developer). No HTML parsing.
2. **There is still something to normalise** — that is, material for the “messy data” part of the task:
   - values as strings/enums (`"Area": "32.89"`, `"Floor_no": ["floor_10"]`, `"Rooms_num": ["1"]`), some fields empty in `characteristics`;
   - description as HTML, important facts often only in it (`Rent`/maintenance fee, cellar, parking space included in the price or not, legal status);
   - search results mix `FLAT` and `INVESTMENT` (a development project with many units) — these have to be filtered out;
   - the same flat listed by several agencies — a natural case for deduplication.
3. **Task domain.** The task text says outright “inspired by otodom.pl” — the data model and UX can be compared 1:1 with the original.
4. **robots.txt** does not block `/pl/wyniki/` or `/pl/oferta/` (it blocks e.g. `/ajax/`, `/oferta/kontakt/`).
5. **C# stack:** `HttpClient` + `AngleSharp` (or regex) are enough to extract `<script id="__NEXT_DATA__">` + `System.Text.Json`. No Playwright.

### Risks and mitigation

- **Change in the `__NEXT_DATA__` structure** — the parser maps to its own DTO, missing field = `null` + a warning in the log, not an exception. We store the raw JSON, so it can be re-parsed offline.
- **Rate limit / blocking** — 1 request every ~1.5–3 s with jitter, retry with backoff on 429/5xx, limit of ~250 requests per run. At this scale the risk is low.
- **Law/ToS** — data only for a recruitment demo: the deployed demo shows ~150 listings with a link to the source, the data is not redistributed as a dataset (no raw files or dumps in the repository), and **no contact data is stored** (phone, a person's first and last name; phone numbers typed into descriptions are masked). Photos: only CDN URLs, no downloading of files.

## 2. Geographic scope: Tricity (Trójmiasto)

- Gdańsk + Gdynia + Sopot (optionally Rumia/Pruszcz Gd.), **flat sales**.
- Rationale: several cities + many districts (Wrzeszcz, Oliwa, Przymorze, Orłowo…) make the location filters and the scenario “chcę tanie, ładne mieszkanie ok. 40 m² w Gdańsku” (“I want a cheap, nice flat of approx. 40 m² in Gdańsk”) meaningful. One city = data that is too homogeneous.
- Rentals are skipped — different price semantics (monthly + deposit + utilities), it would blur the model. Can be added later as a separate `TransactionType`.

## 3. Scale

| Stage | How many | Requests |
|---|---|---|
| Results pages | ~5–6 pages × 36–72 items, spread across 3 cities | ~6 |
| Detail pages | **~200 listings** | ~200 |
| After filtering/deduplication | **~120–150 listings** in the database | — |

Run time: ~6–10 min at 2 s/request. One-off import + the ability to re-run (upsert).

Why ~200, and not 100 or 2000:
- The task requires ~100; the margin covers rejected records (investments, missing price, duplicates) and gives sensible distributions on the filters (e.g. 40–60 m² in Gdańsk returns a dozen or so results, not 2).
- A larger scale adds nothing to the assessment (“not a scraping challenge”), but increases the risk of being blocked and the cost of any LLM enrichment.

### Sample selection — so that it is representative, not random

- Sorting by date added (newest) — fresh, active listings.
- A mix of primary and secondary market: separate queries with `market=PRIMARY` / `market=SECONDARY`, ~50/50. The first pages alone are mostly developers.
- City proportions roughly matching supply: Gdańsk ~60%, Gdynia ~30%, Sopot ~10%.
- We reject: `estate != FLAT`, missing price (`hidePrice`), missing area.

## 4. What we extract

### Structured fields (from `ad.target` / `ad.characteristics` / `ad.location`)

- Identification: otodom `id`, `slug`, URL, `createdAt`, `modifiedAt`, scraping date.
- Price: total price (PLN), price/m², `Rent`/maintenance fee (if given).
- Flat: area, number of rooms, floor (normalised to int, ground floor = 0, `floor_higher_10` = special case), number of floors in the building, year built, market, finish condition, form of ownership, heating, building type.
- Amenities: balcony/terrace/garden, lift, garage/parking space, cellar — as bool flags.
- Location: city, district, street (if available), lat/lng.
- Advertiser: only the **type** (`agency`/`private`; the data has no separate developer value, [design.md](design.md) §14.1 R9). No names: the `agency` key is removed together with the contact data.
- Content: title, description (HTML stored raw + plain text version), list of photo URLs (max ~10).

### Raw data

The whole `ad` JSON is kept in the `raw_json` column (MySQL `JSON`). Normalisation is a separate step that can be repeated without re-scraping.

## 5. Pipeline (high level, details at implementation time)

```
Discover (results pages) → Fetch (listing pages, throttling, retry)
  → Extract (__NEXT_DATA__ → DTO) → Store raw (raw_listings)
  → Normalize (DTO → domain model, validation, quality flags) → listings
  → Enrich LLM (parking and storage unit from the description, [design.md](design.md) §5)
```

- Stages are separated: scraping stores raw data, normalisation reads from the database. This gives repeatability and tests on stored fixtures (a few `__NEXT_DATA__` files in `tests/`).
- Idempotency: upsert by `source + external_id`.
- Each record gets `quality_flags` (e.g. `rent_zero_as_missing`, `price_outlier`; full list in [design.md](design.md) §3.6) — useful in the UI and in the rationale document.

## 6. Data quality control (definition of done for scraping)

- ≥100 listings of type `FLAT` in the database after normalisation.
- 100% of records have: price, area, rooms, city, coordinates.
- ≥80% have district and floor.
- Price/m² within a plausible range for Tricity (~7–40 thousand zł/m²); outside it the `price_outlier` flag, we do not delete.
- Report after the run in the log: how many fetched, rejected (with reason), duplicates, fill percentage of each field.

## 7. Resilience to page structure changes

Scale of the risk: scraping is a one-off import, and the application reads only from its own database. A change on otodom
breaks only the **next import run**, not the stored listings. Goal: a change must be **loud, cheap to fix and must not corrupt the data**.

The real threat is a change of the **data carrier** (disappearance of `__NEXT_DATA__`), not a CSS change — we do not parse the layout at all.

### Three kinds of “structure change”

| What breaks | Symptom | Defence |
|---|---|---|
| Fields in `ad` (rename, enum, unit change) | `__NEXT_DATA__` is there, fields empty or silently mapped wrongly | hard-field contract + fill-rate report |
| Next.js migration to App Router | no `__NEXT_DATA__`, data in `self.__next_f.push(...)` (RSC/Flight) | the run ends with an error; adapting the parser is “with more time” |
| Blocking (403/429, captcha, challenge page) | looks like “there is no JSON” | run aborted — **never LLM** |

As of 2026-11-03: otodom is on the Pages Router (`__NEXT_DATA__` present, `self.__next_f` absent — checked on 8 listings).

### What is built

For a one-off import the defences are reduced to three ([design.md](design.md) §4, §14.1 R10):
- the raw `ad` JSON (without contact data) is stored locally, so a parser fix means re-running `normalize`, not re-scraping;
- a **contract of hard fields** (id, price, m², rooms, city, coordinates): a missing field rejects the record with a reason; a value outside the known list yields `NULL` and a flag, never a silent conversion;
- a **fill-rate report** after every run: a field whose fill rate collapses (e.g. `Rent` from ~50% to 0%) is visible immediately.

A missing `__NEXT_DATA__` or a block page (403/429, captcha) ends the run; it is never passed to the LLM or the database. Carrier classification, a path fingerprint, a circuit breaker and an LLM extraction fallback would matter for a recurring crawler and are listed under “with more time”.

## 8. Decisions deferred to the design stage

- Whether we do LLM enrichment at import (one-off cost, ~150 calls) or not at all. **Resolution:** at import, only parking and storage unit (mini-W2, [design.md](design.md) §5).
- Deduplication heuristic: (district, area ±0.5 m², floor, price ±3%) vs description embeddings. **Resolution:** deduplication out of scope ([product-scope.md](product-scope.md) §8).
- Whether the scraper is a separate console project or a `BackgroundService` in the API. **Resolution:** a separate console `Flatplanner.Ingest`, simpler and run once ([design.md](design.md) §2).
