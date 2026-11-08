# Flatplanner: UI specification

UI specification: visual principles, design tokens, layout, components, states, copy and accessibility.

As of 2026-11-04.

Complements [design.md](design.md) §9 (frontend), §6 (API contract) and §7 (interpretation). Scope = [product-scope.md](product-scope.md) §4. The map, travel time, contradictions and other items from [product-scope.md](product-scope.md) §8 are out of scope and do not appear in the UI.

---

## 1. Principles

1. **Readability over decoration.** Numbers (price, m², price/m²) are the most important thing on the card and in the details; everything else is secondary through colour and size.
2. **Lots of white.** The page background is white, surfaces (filter panel, boxes) use `--color-surface`, dividers are thin lines instead of shadows.
3. **Green only with a reason:** action (primary button, link), active state (selected page, selected mode, focus), highlight (“Real price”, “Why here”, chips). Green is not a decorative colour.
4. **Density of an advertisement portal.** On desktop 4–5 cards on the screen, a horizontal card ~180 px high; no huge heroes and empty sections.
5. **Data honesty visible in the UI.** Absence in the details is an explicit “brak danych”, never an empty place or “nie”.

---

## 2. Design tokens (`web/src/styles.css`, top of the file)

Light theme only; dark mode is out of scope.

```css
:root {
  /* Neutral */
  --color-bg: #ffffff;              /* page background, cards */
  --color-surface: #f6f8f8;         /* filter panel, boxes, skeleton */
  --color-surface-strong: #eef1f2;  /* row hover, "from ad" badge background */
  --color-border: #d5dcde;          /* dividers, card outline (decorative) */
  --color-border-strong: #8a979b;   /* form field outline (3:1 against white) */
  --color-text: #1f2a2e;            /* main text */
  --color-text-secondary: #5b6b70;  /* labels, metadata, counters */

  /* Green */
  --green-500: #16a374;  /* non-text accent: focus ring, active borders, left edge of highlights */
  --green-600: #0b7a54;  /* primary button (background), links, green text */
  --green-700: #0a6b4a;  /* button and link hover */
  --green-800: #08593d;  /* active (pressed), chip text */
  --green-50:  #e8f6f0;  /* light background: chips, "Real price", "Why here", selected mode */

  /* States */
  --color-warning-text: #8a5a00;  --color-warning-bg: #fff6e0;  --color-warning-border: #e0b44c;
  --color-error-text:   #b42318;  --color-error-bg:   #fdecea;  --color-error-border:   #e8a29b;
  --color-info-text:    #1d5fa8;  --color-info-bg:    #eaf2fb;  --color-info-border:    #9cc0e6;

  /* Provenance badges (provenance) */
  --prov-listing-text: #3f4f55;      --prov-listing-bg: #eef1f2;
  --prov-description-text: #6b4fa3;  --prov-description-bg: #f2eefa;
  --prov-computed-text: #1d5fa8;     --prov-computed-bg: #eaf2fb;
  --prov-missing-text: #5b6b70;      --prov-missing-border: #8a979b;  /* white background, dashed outline */

  /* Typography */
  --font-sans: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "Noto Sans", sans-serif;
  --fs-xs: 12px;  --fs-sm: 14px;  --fs-md: 16px;  --fs-lg: 18px;  --fs-xl: 22px;  --fs-2xl: 28px;
  --fw-regular: 400;  --fw-semibold: 600;  --fw-bold: 700;
  --lh-body: 1.5;  --lh-tight: 1.25;

  /* Spacing, radii, shadows */
  --space-1: 4px; --space-2: 8px; --space-3: 12px; --space-4: 16px; --space-5: 24px; --space-6: 32px; --space-7: 48px;
  --radius-sm: 4px;  --radius-md: 8px;  --radius-pill: 999px;
  --shadow-sm: 0 1px 2px rgba(16, 24, 40, 0.06);
  --shadow-md: 0 2px 8px rgba(16, 24, 40, 0.10);   /* card hover only */
  --focus-ring: 0 0 0 2px #ffffff, 0 0 0 4px var(--green-500);

  --container-max: 1120px;
  --sidebar-width: 280px;
}
body { margin: 0; background: var(--color-bg); color: var(--color-text);
       font: var(--fw-regular) var(--fs-md)/var(--lh-body) var(--font-sans); }
.num { font-variant-numeric: tabular-nums; }
```

**Font:** system stack. Zero dependencies and requests, full Polish characters on every platform, no FOUT. A Google Fonts font adds nothing for “clean and usable”.

**WCAG 2.1 contrast** (computed with the relative luminance formula):

| Pair | Ratio | Result |
|---|---|---|
| white on `--green-500` #16a374 | 3.22:1 | does **not** meet AA for text (only large text ≥ 24 px); that is why the button background is `--green-600` |
| `--green-500` on white | 3.22:1 | AA for non-text elements (1.4.11, ≥ 3:1): focus ring, borders; **not** for text |
| white on `--green-600` #0b7a54 (button) | 5.35:1 | AA |
| `--green-600` on white (links, text) | 5.35:1 | AA |
| white on `--green-700` (hover) / `--green-800` (active) | 6.53:1 / 8.38:1 | AA / AAA |
| `--green-600` on `--green-50` | 4.81:1 | AA |
| `--green-800` on `--green-50` (chips) | 7.53:1 | AAA |
| `--color-text` on white / on `--green-50` | 14.70:1 / 13.21:1 | AAA |
| `--color-text-secondary` on white / on `--color-surface` | 5.55:1 / 5.21:1 | AA |
| warning / error / information (text on its own background) | 5.51 / 5.75 / 5.71:1 | AA |
| badges: from ad / from description / computed / no data | 7.52 / 5.62 / 5.71 / 5.55:1 | AA |
| `--color-border-strong` on white (field outline) | 3.01:1 | AA non-text |

---

## 3. Styling approach

**Plain CSS with tokens in a single file `web/src/styles.css`**, in line with design.md §9 (“one `web/src/styles.css`”, no component library). With 8 components there are no name collisions, and an agent reads a single file in its entirety. Class convention: BEM-light, prefix = component: `.card`, `.card__price`, `.card--outlier`, `.chip`, `.chip__remove`, `.btn`, `.btn--primary`, `.btn--secondary`, `.badge`, `.badge--listing|description|computed|missing`, `.notice--info|warning|error`. Colours exclusively through the variables from §2, never hex in rules.

**Breakpoints** (constants in media queries, CSS variables do not work in them): `max-width: 959px` → collapsible filter panel, one column; `max-width: 640px` → vertical card, details in one column, 16 px gutter. Container: `max-width: var(--container-max); margin: 0 auto; padding: 0 var(--space-5)` (≤ 640 px: `0 var(--space-4)`).

---

## 4. Layout and wireframes

### 4.1 Listings list `/`, desktop (≥ 960 px)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Flatplanner                       Flats for sale · Tricity · 4.11.2026           │  header 56px, border-bottom
├──────────────────────────────────────────────────────────────────────────────┤
│ [ Search | AI chat ]                                                         │  SearchBar
│ ┌──────────────────────────────────────────────────────────┐ ┌──────────┐   │
│ │ e.g. Gdańsk Morena kawalerka                             │ │  Search  │   │
│ └──────────────────────────────────────────────────────────┘ └──────────┘   │
├──────────────────────────────────────────────────────────────────────────────┤
│ ⚠ Simplified query understanding (AI temporarily unavailable): …            │  Interpretation:
│ ⓘ We have no listings in Kraków. … Same criteria in: Gdańsk                 │  banner, warnings, notices,
│ I understood: Gdańsk · approx. 40 m² (35–45 m²) · cheap: … (14 800 zł/m², n=88)│  summary, chips
│ (Gdańsk (instead of: Kraków) ×) (approx. 40 m² (35–45 m²) ×) (cheap: … ×)   │
├───────────────────┬──────────────────────────────────────────────────────────┤
│ FILTERS 280px     │ 54 listings                      Sort: [newest   ▾]  │
│ City              │ ┌────────────┬───────────────────────────────────────┐   │
│ ☑ Gdańsk (92)     │ │            │ 799 000 zł        19 087 zł/m²        │   │  ListingCard
│ ☐ Gdynia (41)     │ │   photo    │ 41,86 m² · 2 rooms · floor 4/6        │   │
│ ☐ Sopot (17)      │ │  240×180   │ Letnica, Gdańsk                       │   │
│ District          │ │            │ Słoneczne | Do wprowadzenia | Widok…  │   │
│ ☐ Letnica (4) …   │ │            │ [secondary market]                    │   │
│ Price(zł)[from][to]│ │          │ Why here: 38,5 m² · … cheaper …       │   │  only with preferences
│ Area (m²)         │ └────────────┴───────────────────────────────────────┘   │
│ Rooms  [from][to] │ … (20 cards, 12px gap)                                   │
│ Floor  [from][to] │                                                          │
│ Market ◉ any      │          ‹ Previous  1 [2] 3 … 9  Next ›                 │  Pagination
│ Advertiser        │                                                          │
│ ☐ Winda          │                                                          │
│ ☐ Balkon         │                                                          │
│ [    Apply      ] │                                                          │
│   Clear filters   │                                                          │
└───────────────────┴──────────────────────────────────────────────────────────┘
```

Grid: `display: grid; grid-template-columns: var(--sidebar-width) 1fr; gap: var(--space-5)`. Filter panel: `--color-surface`, `--radius-md`, 16 px padding, not sticky. DOM order as in design.md §9: SearchBar → Interpretation → Filters → results.

### 4.2 Listings list, mobile (≤ 640 px)

```
┌────────────────────────────────┐
│ Flatplanner         4.11.2026  │
├────────────────────────────────┤
│ [Search | AI chat]             │
│ ┌────────────────────────────┐ │
│ │ e.g. chcę ładne, tanie …    │ │  textarea in chat mode
│ └────────────────────────────┘ │
│ 0/300              [ Ask ]     │
│ ⓘ notice / ⚠ banner            │
│ I understood: …                │
│ (Gdańsk ×) (approx. 40 m² ×) … │  chips wrap (flex-wrap)
│ [ Filters (3) ▾ ]               │  full-width button; expands the panel inline
│ 9 listings    Sort: [……… ▾]    │
│ ┌────────────────────────────┐ │
│ │ photo 16:9, 100% width     │ │  vertical card
│ ├────────────────────────────┤ │
│ │ 459 000 zł   11 922 zł/m²  │ │
│ │ 38,5 m² · 1 room · fl. 2/4 │ │
│ │ Orunia Górna…, Gdańsk      │ │
│ │ Why here: …                │ │
│ └────────────────────────────┘ │
│ ‹  [2] / 9  ›                  │  compact pagination
└────────────────────────────────┘
```

Between 641 and 959 px: like mobile (collapsible filters), but a horizontal card (photo 200×150).

### 4.3 Details `/listings/:id`, desktop

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Flatplanner                                                                  │
├──────────────────────────────────────────────────────────────────────────────┤
│ ‹ Back to results                                          (max-width 1200 px)│
│ ┌──────────────────────────────────────────────────────────────────────────┐ │
│ │ ┌──────────────────────────────────┐ ┌─────────────────────┐             │ │
│ │ │                                  │ │  photo 2            │  mosaic     │ │
│ │ │   photo 1 (click = lightbox)     │ ├─────────────────────┤  (§5.9)     │ │
│ │ └──────────────────────────────────┘ └─────────────────────┘ +5 zdjęć    │ │
│ │ Słoneczne | Do wprowadzenia | Widok na zielone patio                     │ │
│ │ ⌖ Letnica, ul. Starowiejska, Gdańsk (bold)  Approximate location         │ │
│ │ 799 000 zł                       Data from 2026-11-04.  [ View on otodom ↗]│ │
│ │ 19 087 zł/m²                                                             │ │
│ │ ──────────────────────────────────────────────────────────────────────── │ │
│ │ ⛶ 41,86 m² │ ▯ 2 │ ≋ 4 of 6 │ ▦ 2019 │ ⚷ full ownership │ ⌖ ul. Star…, Gdańsk│ │
│ │ ──────────────────────────────────────────────────────────────────────── │ │
│ │ Parameters                          ┌───────────────────────────────────┐│ │
│ │ ⌂ Market        secondary           │ map preview, circle if approx.    ││ │
│ │ …                                   │ [Lokalizacja przybliżona] [Powiększ]││ │
│ │ ──────────────────────────────────  └───────────────────────────────────┘│ │
│ │ Description  (paragraphs, bullets, bold and italic as in the original)   │ │
│ │ Source: otodom.pl · fetched 4.11.2026 · added 4.11.2026                  │ │
│ └──────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

One column, `max-width: 1200px`, and one white sheet (`--color-surface`, 1 px `--color-border`, `--radius-md`) that holds the whole listing; sections inside are separated by 1 px lines, not by separate cards. Order: mosaic, title and location, price bar, key facts strip, a split row with Parameters on the left and the map on the right, Description, source line. Minimal details after the AI pivot (design.md §16.5, R19–R23): no provenance badges, real price, “Ask the seller” or quality notes.

### 4.4 Details, mobile (≤ 640 px)

One column inside the same sheet, in the same order as on desktop: “Back” → gallery (swipeable carousel, 4:3, instead of the mosaic) → title and location → price, then the stale-data note and the full-width button → key facts (2 columns) → Parameters → map (240 px) → Description → Source.

---

## 5. Components

Shared: **button** `.btn` height 40 px, padding `0 16px`, `--radius-md`, `--fw-semibold`, `--fs-sm`. `.btn--primary`: background `--green-600`, white text; hover `--green-700`; active `--green-800`; disabled: background `--color-surface-strong`, text `--color-text-secondary`, `cursor: not-allowed`. `.btn--secondary`: white background, 1 px `--green-600` outline, text `--green-600`; hover background `--green-50`. `.btn--link`: no background, text `--green-600`, underline on hover. **Fields** height 40 px, 1 px `--color-border-strong` outline, `--radius-sm`; disabled: background `--color-surface`. **Focus** (all interactive elements): `:focus-visible { outline: none; box-shadow: var(--focus-ring); }`. Text **link**: `--green-600`, hover `--green-700` + underline.

### 5.1 Header (in `App.tsx`)
Height 56 px, white background, `border-bottom: 1px solid var(--color-border)`. Left: “Flatplanner” (`--fs-xl`, `--fw-bold`, `--green-600`, link to `/`), no logo. Right: “Flats for sale · Tricity · data as of {dataAsOf}” (`--fs-sm`, secondary); at ≤ 640 px only the date.

### 5.2 `SearchBar`
- **Mode switch:** two buttons in an outline (segmented control), `role="radiogroup"` with `role="radio"` + `aria-checked`, ←/→ arrows change the mode. Selected: background `--green-50`, text `--green-800`, outline `--green-500`; unselected: white, secondary text. The mode is local state; default “Search”, after returning with `location.state.fromChat` = “AI chat”.
- **Search:** `<input type="search" maxLength={100}>` + “Search” (primary). Enter submits. Empty after trim → button disabled. Submit writes `text=<value>` to the URL per design.md §9 (no API call; other parameters stay). The field shows the current `text`.
- **Chat:** `<textarea rows={3} maxLength={meta.chatMaxLength}>` + counter “{n}/{max}” (secondary, at ≥ 90% of the limit `--color-warning-text`) + “Ask”. Enter submits, Shift+Enter = new line. The text stays in the field after submit. When `meta.chatMode === "fallback"`: a permanent annotation under the field “Simplified mode, without AI” (`--fs-xs`, secondary).
- **Loading** (both modes): button disabled with the text “Searching…” / “Analysing…”, field `readOnly`, `aria-busy="true"`. Chat can take up to ~5 s (LLM timeout).
- **Errors** (under the field, `.field-error`, `--color-error-text`, `role="alert"`): 400 → “Enter 1 to {max} characters.”; 429 → “Too many requests. Try again in {Retry-After} s.” and the button disabled for that many seconds (without the header: “in a moment”, 10 s); network/5xx → “Could not connect to the server. Try again.”

### 5.3 `Interpretation`
Order of blocks (each only when non-empty), 8 px spacing:
1. **Fallback banner** (`.notice--warning`): when `location.state.usedFallback && fallbackReason !== "disabled"` (for `disabled` the annotation from 5.2 is enough). Text from design.md §7.7.
2. **Warnings:** `location.state.warnings` + `interpretation.warnings`, without duplicates by `message`; `.notice--warning` in a compact version (`--fs-sm`, list).
3. **Notices:** `notice.text` from the API (the UI does not compose the text). `.notice--warning` for `rent_out_of_scope`, `house_out_of_scope`; `.notice--info` for `city_out_of_scope`, `metro_not_available`, `median_fallback`. When `actionLabel` and `remove` are present (they are omitted together, design.md §6.1) → `.btn--secondary` (height 32 px) executing `remove`.
4. **Summary:** when the URL contains `preferences=cheap` (the summary then carries the median and `n`, design.md §6.1) or `location.state.fromChat`. Prefix “I understood: ” (`--fw-semibold`) only with `fromChat`. `--fs-sm`.
5. **Chips:** `<ul class="chips">` (flex-wrap, gap 8 px). Chip: height 28 px, `--radius-pill`, padding `0 4px 0 12px`, background `--green-50`, 1 px `--green-500` outline, text `--green-800` `--fs-sm`. `outOfScope:*` chips: warning palette. Remove button `.chip__remove` 24×24, round, character “×”, `aria-label="Remove: {label}"`; hover: background `--green-500`, white character; focus: focus ring. Click → `remove` per design.md §9 (removal of `page`).

`.notice`: padding `12px 16px`, `--radius-md`, `border-left: 4px solid` (colour `*-border`), background `*-bg`, text `*-text`, `--fs-sm`; text prefix “Note:” / “Information:” visually hidden (`.sr-only`) instead of icons.

### 5.4 `Filters`
- Container `<form aria-label="Filters">`; sections as `<fieldset>` with `<legend>` (`--fs-sm`, `--fw-semibold`), 16 px spacing.
- **City:** checkboxes from `meta.cities`: “Gdańsk (92)”. **District:** checkboxes only for selected cities, grouped by a city heading, with counters; list `max-height: 240px; overflow-y: auto`. With no city selected: “Select a city to see districts.” (secondary). Unchecking a city removes its districts from the form.
- **Price (zł), Area (m²), Rooms, Floor:** pairs of “from” / “to” fields in one row (2 columns, gap 8 px), a visible `<label>` above the pair, each field with an `aria-label` “Price from” etc. Price: `type="text" inputMode="numeric"`, spaces removed before saving. Area: `inputMode="decimal"`, `,` → `.` before saving. Rooms: `inputMode="numeric"`. Floor: `inputMode="numeric"`, hint “ground floor = 0”. All numeric fields are `type="text"` and drop every character except digits (area also `,` and `.`) on input, so negative values cannot be typed; a field with an error (not a number, from > to) is red (`aria-invalid`, `--color-error-text`) with the message under the pair, and a range with an error is not saved to the URL. When the URL has `areaApprox` without a range: under the fields “Now: about {areaApprox} m². An entered range will replace the approximation.”; saving a range removes `areaApprox`.
- **Market, Advertiser:** radio “any” + values from `meta.enums` (`market`, `advertiserType`).
- **More filters** (`<details>` “Więcej filtrów”, open when any of them is active): year built, price per m² (zł), rent (zł/mies.) as “from” / “to” pairs with the hint “Oferty bez roku budowy lub czynszu zostaną pominięte.”; checkbox pills from `meta.enums` (several can be selected at once, OR) for `finishConditions`, `ownerships`, `buildingTypes`, `buildingMaterials`, `heatings`.
- **Amenities:** pills “Winda”, “Balkon”, “Garaż”, “Piwnica”, no hint.
- **Actions:** “Apply” (primary, full width) saves the URL per design.md §9. “Clear filters” (`.btn--link`) removes the filter parameters: `cities`, `districts`, `priceMin/Max`, `areaMin/Max/Approx`, `roomsMin/Max`, `floorMin/Max`, `market`, `advertiserType`, `buildYearMin/Max`, `pricePerM2Min/Max`, `rentMin/Max`, `finishConditions`, `ownerships`, `buildingTypes`, `heatings`, `buildingMaterials`, `requireLift`, `requireBalcony`, `requireGarage`, `requireBasement`, and `page`; `text` and `preferences` stay (they are removed with a chip).
- **Mobile/tablet (< 960 px):** above the panel a full-width `.btn--secondary` button “Filters ({number of active filtering chips})” with `aria-expanded` and `aria-controls`; the panel is collapsed by default, expands inline (no modal). After “Apply” it collapses.
- **Meta unavailable:** the panel shows `.notice--error` “Could not load filters.” + “Try again”.

### 5.5 Results bar (in `ListingsPage`)
Left: “{total} {listing|listings}” (`--fw-semibold`). Right: `<label>Sort</label> <select>` with `meta.enums.sort`; the `relevance` option is visible only with non-empty `intent.preferences`; value = `response.sort`; a change saves the URL immediately (without `page`).

### 5.6 `ListingCard`
The whole is a single `<Link to="/listings/{id}">` (`.card`; the photo is a link too, so the middle button and ctrl-click open a new tab), white background, 1 px `--color-border` outline, `--radius-md`, overflow hidden. Hover: `--shadow-md`, outline `--color-border-strong`. Focus: focus ring on the whole card. Desktop: `grid-template-columns: 240px 1fr`, photo 240×180 `object-fit: cover`; ≤ 640 px: photo on top, `aspect-ratio: 16/9`. Content: 16 px padding, 4 px spacing. Hierarchy:
1. `pricePln` (`--fs-xl`, `--fw-bold`, `.num`) + `pricePerM2` “19 087 zł/m²” (`--fs-sm`, secondary) on one line;
2. parameters: “41,86 m² · 2 rooms · floor 4/6 · 2021” with icons (`--fs-md`); floor per §7.3; no `floor` or `buildYear` → omitted;
3. location: “{districtName}, {cityName}” (without a district: the city alone), secondary;
4. `title`: `--fs-sm`, secondary, one line with `text-overflow: ellipsis`;
5. facts and pills, in two rows after the title (all `.badge`, `--fs-xs`, with a 14 px icon; a fact missing in the data is omitted, never shown as „brak”):
   - signals: a `danger` pill per `riskKinds` entry (label from §7.2), the `priceOutlier` warning pill “unusual price, check the description”, a `positive` pill “{X}% below the city median” with `belowMedianPercent`, `nearbyStop` and `nearbyPark` verbatim;
   - features: `positive` “new listing” for `createdAtSource` younger than 7 days, “Lift” / “Balcony” / “Garage” / “Basement” only when `true`, the `finishCondition` label, the `ownership` label only for `limited_ownership`, the `market` label.
   The parameters line (2) carries an icon per fact (area, rooms, floor, `buildYear`); the location line (3) a pin icon, and with `locationApproximate` a dashed-circle amber marker (instead of the pin) with a tooltip, instead of a pill;
6. **“Why here”** only when `why` is present (`why != null`; omitted fields are `undefined`) (relevance mode): block with background `--green-50`, `border-left: 3px solid var(--green-500)`, padding `4px 8px`, `--fs-sm`, text `--color-text`; prefix “Why here: ” `--fw-semibold`; `why` displayed verbatim.

Photo: `<img loading="lazy" referrerPolicy="no-referrer" alt="">` (decorative, because the title is in the card). No `imageUrl` or `onError` → a `--color-surface` block with the text “No photo” (secondary, centred).

### 5.7 `Pagination`
Hidden when `totalPages <= 1`. `<nav aria-label="Pagination">` with `<Link>` (middle mouse button and Tab work). Desktop: a round 40 px bordered chevron button (icon, no text) for previous, numbers: 1, last, current ±1, “…” in the gaps (`aria-hidden`), a chevron button for next. Number: 36×36, `--radius-sm`; hover background `--green-50`; current: background `--green-600`, white text, `aria-current="page"`, not clickable; The chevron buttons at the ends are `<span aria-disabled="true">` at 40% opacity. ≤ 640 px: the chevron buttons around “{page} / {totalPages}” (all with `aria-label`). After a page change `window.scrollTo(0, 0)`.

### 5.8 `Blockers` (with `total === 0 && !scopeBlocked`)
A `--color-surface` box, 24 px padding, centred text in the results column:
- heading “No listings for these criteria.” (`--fs-lg`, `--fw-semibold`);
- when `blockers` is non-empty: “Results will appear if you change one criterion:” + a list of `.btn--secondary` buttons (in a column, max 420 px): “{label} → {count} {listing|listings}”; a click executes `remove` (like a chip);
- when `blockers` is empty: “No single change will be enough.” + `.btn--link` “Clear filters” (the action from 5.4).

With `scopeBlocked` the list is not shown at all (no “No listings”); the notices explain this.

### 5.9 `ListingDetailPage`
- **Risk box** (`RiskBox`, between the key facts strip and the split row; omitted when `risks` is empty and `priceOutlier` is `false`): warning palette (`--color-warning-bg`, `--color-warning-border`), heading „Na co uważać” with a risk icon. One item per entry: the kind label (§7.1, with the kind explanation as `title`), `reason`, and the `quote` as an italic block quote with a left border. `priceOutlier` adds an item „Nietypowa cena” with the price explanation. A footer note (`--fs-xs`, secondary) says where the risks come from.
- **“‹ Back to results”:** `navigate(-1)` when `location.key !== "default"`; otherwise `<Link to="/">` “‹ All listings”.
- **Gallery (desktop):** a mosaic of up to three photos in a 480 px high block, `--radius-md`, gap 8 px: with three or more photos the first fills the left two thirds, the second and third are stacked on the right; two photos sit side by side; one photo fills the block; the last visible tile carries the badge „+{n} zdjęć” when more photos exist (`n` with the Polish plural, §7.4). Each tile is a button (`aria-label` „Powiększ zdjęcie {i} z {n}”, `alt=""`) that opens the lightbox at that photo. No photos → placeholder „Brak zdjęcia”. A photo that fails to load drops out of the mosaic.
- **Gallery (mobile, ≤ 640 px):** the swipeable `PhotoCarousel` as on the card (counter “3 / 12”), 4:3; a click opens the lightbox.
- **Fullscreen dialog (photo lightbox and map):** one `FullscreenDialog` component, native `<dialog>` (`showModal`), full viewport on `rgba(42,38,35,.94)`; “×” 44×44 (`aria-label` „Zamknij”, top right); closes on Escape, on a click outside the content and on “×”. The photo lightbox shows the photo `object-fit: contain`, buttons “‹” / “›” („Poprzednie zdjęcie” / „Następne zdjęcie”, wrapping at the ends), counter “3 / 12”, arrows on the keyboard, `alt` „{title}, zdjęcie {i} z {n}”.
- **Title and price bar:** inside the sheet, `<h1>` (`--fs-xl`) and the location line (below); then the price bar: `pricePln` (28 px, bold) with `pricePerM2` (`--fs-sm`, secondary) on the left, and on the right the stale-data note built from `scrapedAt` (§7.1; `--fs-xs`, secondary, right-aligned) next to the primary button “Zobacz na otodom ↗” (`target="_blank" rel="noopener noreferrer"`). On mobile the price comes first, then the full-width button with the note under it.
- **Map:** right half of the split row (same height as Parameters, at least 240 px), a Leaflet map, OSM tiles with attribution, loaded lazily. The preview is not interactive (no drag, zoom control or wheel zoom); the whole preview is a button („Powiększ mapę”, `cursor: zoom-in`, label in the corner) that opens the map in the fullscreen dialog, where it is fully interactive. With `locationApproximate` a circle of 500 m around `lat`/`lng` and a „Lokalizacja przybliżona” chip in the top left corner of the preview; otherwise a circle marker. `role="region"` with `aria-label` „Mapa lokalizacji”.
- **Key facts strip:** `<ul class="key-facts" aria-label="Najważniejsze parametry">`, full width of the sheet, one row of six cells divided by 1 px vertical lines and framed by 1 px lines above and below (2 columns on mobile). Order: `areaM2` (Powierzchnia), `rooms` (Pokoje), floor (Piętro, §7.4), `buildYear` (Rok budowy), `ownership` (Forma własności, label from `/api/meta`), location (Lokalizacja: `street` and `cityName` joined with “, ”, just `cityName` without a street); the last two cells are wider. A cell is a column: a 24 px teal icon, the value (`--fs-md`, bold) and the label (`--fs-xs`, secondary). A cell without a value keeps its place and reads „brak danych” (`--fs-sm`, secondary).
- **Parameters:** left half of the split row, `<dl class="params">`, one column; each row has the label with an 18 px teal icon on the left and the value on the right (right-aligned), rows separated by 1 px `--color-border`, padding 8 px. Rows: Rynek, Stan wykończenia, Typ budynku, Materiał budynku, Okna, Ogrzewanie, Czynsz, Winda, Balkon, Garaż / miejsce postojowe, Piwnica, Ogłaszający, Dzielnica (what the key facts strip does not show; labels in §7.2). A missing value is a `.value--missing` element: text “brak danych”, white background, 1 px **dashed** `--color-border-strong` outline. Two-state values (`hasLift`, `hasBalcony`, `hasGarage`, `hasBasement`: `true` or omitted): `true` → “tak”; omitted → `.value--missing` with “brak informacji” (not “nie”).
- **Location line:** under the title, in bold with a 20 px teal pin icon: `districtName`, `street`, `cityName` joined with “, ”. With `locationApproximate` the text „Lokalizacja przybliżona” follows (`--color-warning-text`, `--fs-sm`) with `title="Ogłaszający nie podał dokładnej lokalizacji."`.
- **Description:** h2 “Opis” under a 1 px line, then `descriptionMarkup` (falling back to `descriptionText`) as in the original listing: lines starting with “- ” or “•” form a `<ul>`, other lines are paragraphs whose lines break with `<br>`, blank lines separate paragraphs; `**…**` is `<strong>`, `*…*` is `<em>`, `\` escapes the next character. Parsed by our own code into React elements; HTML is never rendered. Line height 1.65, text width up to 90 characters.
- **Source:** “Źródło: otodom.pl · pobrano {scrapedAt} · dodano {createdAtSource}” (`--fs-xs`, secondary).

### 5.10 Provenance badge `.badge--{provenance}`
Not built (design.md R19): the details page shows no provenance badges. The section stays as background for the dossier (design.md §16.1).

Inline, height 20 px, padding `0 6px`, `--radius-sm`, `--fs-xs`, `--fw-semibold`, text = label from `meta.enums.provenance` (no icons):

| `provenance` | Text | Style | Additionally |
|---|---|---|---|
| `listing` | from ad | `--prov-listing-*` | — |
| `description` | from description | `--prov-description-*` | `title="{quote}"`; the quote is always also visible under the value (a tooltip does not work on touch) |
| `computed` | computed | `--prov-computed-*` | `title="Computed from price and area"` for `pricePerM2` |
| `missing` | — | — | instead of the value and badge a single `.value--missing` element: text “no data”, white background, 1 px **dashed** `--prov-missing-border` outline, text `--prov-missing-text` |

**Two-state values** (`hasLift`, `hasBalcony`, `hasGarage`, `hasBasement`: `true` or `null`): `true` → “yes” + a “from ad” badge; `null` → `.value--missing` with the text “no information” (not “no”). The same vocabulary in the filters (§5.4).

---

## 6. States

| State | View | Behaviour |
|---|---|---|
| Loading the list (first) | **static skeleton** (no animation): 3 cards with `--color-surface` blocks in place of the photo and 3 text lines | container `aria-busy="true"`; results counter “Loading…” |
| Reloading (filter or page change) | previous list with `opacity: 0.6` | `useQuery(..., placeholderData: keepPreviousData)`; no layout jump |
| Loading details | skeleton: 16:10 gallery block + 6 lines | as above |
| Empty results | `Blockers` (§5.8) | — |
| Blocked scope (`scopeBlocked`) | only notices and chips | — |
| List API error (5xx, network) | `.notice--error` “Could not fetch listings.” + `.btn--secondary` “Try again” (`refetch`) | filters and SearchBar keep working |
| List 400 (manually broken URL) | `.notice--error` “Invalid search parameters.” + link “Clear search” → `/` | — |
| 429 (global limiter, list/details/search) | `.notice--warning` “Too many requests. Try again in {Retry-After} s.” | TanStack Query: `retry: false` for 429 and 4xx, 1 retry for 5xx/network |
| Chat 429 | error under the field (§5.2) | list unchanged |
| Chat in fallback (`timeout`, `llm_error`, `schema_mismatch`, `refusal`) | banner §5.3 above the chips | results as normal; one text for all reasons |
| `chatMode = fallback` | annotation “Simplified mode, without AI” under the chat field, no banner | — |
| Details 404 | “Listing not found.” + link “Back to the listings list” | — |

---

## 7. UI copy

**The interface is in Polish** (Polish market, Polish listing data). This section is the single source of every user-facing string, both those built in the frontend and those generated by the API. English text elsewhere in `design.md` and in this document (examples, glosses in brackets) is only an explanation; code emits the Polish strings below. Placeholders in braces are filled by code.

### 7.1 Frontend labels, placeholders, messages

| Place | Text |
|---|---|
| browser tab title | list: „Flatplanner: Przeczytamy ogłoszenia za Ciebie” (the motto, without its full stop); details: „{title} · Flatplanner” |
| mode switch | „Czat AI” / „Lista”; `aria-label` of the navigation „Tryb wyszukiwania” |
| search placeholder | „np. Morena, Wrzeszcz, balkon” |
| chat placeholder (scenario B) | „np. chcę ładne, tanie mieszkanie, 40 m w Gdańsku” |
| chat (R28) | heading „Opisz, czego szukasz.” + „ Resztę przeczytamy za Ciebie.”; placeholder on the landing „np. Pracuję na nocki i mam psa, szukam czegoś w Gdańsku do 700 tys.”; placeholder and `aria-label` of the docked composer „Dopytaj albo zmień kryteria”; `aria-label` of the landing composer „Opisz, czego szukasz”; send button `aria-label` „Wyślij”; „Nowa rozmowa”; limit line „Rozmowa może mieć najwyżej 5 pytań. Zacznij nową.”; steps „Rozumiem pytanie”, „Czytam ogłoszenia i sprawdzam dowody”; folded results „Pokaż wyniki ({n})”; visually hidden prefixes „Twoje pytanie: ” and „Odpowiedź: ”; counter „{n}/{max}” |
| chat annotation when `chatMode = fallback` | „Tryb uproszczony, bez AI” |
| buttons | „Szukaj”, „Zapytaj”, „Wyczyść filtry”, „Spróbuj ponownie”, „Zobacz na otodom ↗”, „‹ Wróć do wyników” |
| filters (legends) | „Miasto”, „Dzielnica”, „Cena (zł)”, „Powierzchnia (m²)”, „Pokoje”, „Piętro”, „Rynek”, „Ogłaszający”, „Udogodnienia”, „Więcej filtrów”, „Rok budowy”, „Cena za m² (zł)”, „Czynsz (zł/mies.)”, „Stan wykończenia”, „Forma własności”, „Typ budynku”, „Materiał budynku”, „Ogrzewanie”; fields „od” / „do”; radio „dowolny”; checkboxes „Winda”, „Balkon”, „Garaż”, „Piwnica”; „Wybierz miasto, aby zobaczyć dzielnice.” |
| results bar | „{n} {oferta/oferty/ofert}”, „Sortuj” |
| card | „Dlaczego tutaj:”, „Brak zdjęcia”, „nietypowa cena, sprawdź opis”, „nowa oferta”, „{X}% poniżej mediany miasta”, „Winda”, „Balkon”, „Garaż”, „Piwnica”; risk pills use the labels of `RISK_KIND_LABELS` (§7.2); the approximate-location marker (dashed circle) has `aria-label` „Lokalizacja przybliżona” and `title` „Ogłaszający nie podał dokładnej lokalizacji.” |
| risk pills and box | kind labels from `RISK_KIND_LABELS`; `title` explanations: `extra_cost` „Opis wspomina o koszcie ponad cenę mieszkania, np. miejsce postojowe, garaż lub komórka za dopłatą.”; `legal` „Opis wskazuje na kwestię prawną, np. sprzedaż z VAT, syndyk, garaż w osobnej księdze wieczystej albo kredyt do spłaty z ceny.”; `occupied` „Opis wskazuje, że lokal jest wynajęty lub zajęty.”; `noise` „Opis wymienia źródło hałasu, np. klub, główną ulicę albo planowaną linię tramwajową.”; `field_mismatch` „Opis przeczy jednemu z pól ogłoszenia, np. liczbie pokoi, piętra albo czynszowi. Dopytaj sprzedającego.”; unusual price „Cena za m² poza zakresem 7 000–40 000 zł. Może to być błąd w ogłoszeniu, sprawdź opis.”; box heading „Na co uważać”, item „Nietypowa cena”, note „Wykryte przez AI w opisie ogłoszenia. Każdy cytat sprawdziliśmy dosłownie w tekście. Zweryfikuj to ze sprzedającym.” |
| card pill titles | below median „Cena za m² niższa niż mediana ofert z tego miasta (bez ofert z nietypową ceną).”; stop „Najbliższy przystanek, odległość w linii prostej (dane OpenStreetMap).”; park „Najbliższy park, odległość w linii prostej (dane OpenStreetMap).” |
| filter field errors (under the field, red) | „Wpisz liczbę.”, „Wartość „od” nie może być większa niż „do”.” |
| details | „Parametry”, „Opis”, „Lokalizacja”, „Lokalizacja przybliżona”, „Mapa lokalizacji”, „Powiększ zdjęcie {i} z {n}”, „+{n} zdjęć”, „Najważniejsze parametry”, „Forma własności”, „Lokalizacja”, „Powiększ mapę”, „Zamknij”, „{tytuł}, zdjęcie {i} z {n}”, „brak danych”, „brak informacji”, „tak”, „Źródło: otodom.pl · pobrano {data} · dodano {data}”, „‹ Wszystkie oferty”; stale-data note from `scrapedAt`: „Dane z {data}. Aktualną ofertę sprawdź na otodom.” |
| summary prefix after chat | „Zrozumiałem: ” |
| loading / empty / errors | „Ładowanie…”; zero results: „Brak ofert dla tych kryteriów.”, „Wyniki pojawią się, jeśli zmienisz jedno kryterium:”, „Żadna pojedyncza zmiana nie wystarczy.”; „Nie udało się pobrać ofert.”; „Nieprawidłowe parametry wyszukiwania.” + link „Wyczyść wyszukiwanie”; „Nie udało się wczytać filtrów.”; „Nie znaleziono oferty.” + link „Wróć do listy ofert” |
| chat errors (under the field) | 429: „Za dużo zapytań. Spróbuj ponownie za {Retry-After} s.”; 400: „Wpisz od 1 do {max} znaków.”; network or 5xx: „Nie udało się połączyć. Skorzystaj z filtrów poniżej.” |
| list 429 | „Za dużo zapytań. Spróbuj ponownie za {Retry-After} s.” |
| motto (top bar, under the wordmark, hidden ≤ 640 px) | „Przeczytamy ogłoszenia za Ciebie.” |
| search field | `aria-label` „Szukaj w ofertach” |
| card photo gallery | `aria-label` „Zdjęcia oferty”; buttons „Poprzednie zdjęcie” / „Następne zdjęcie”; counter „{n} / {total}” |
| filters | form `aria-label` „Filtry”; mobile toggle „Filtry ({n})”; `aria-label` of the range fields „Cena od” / „Cena do”, „Powierzchnia od” / „Powierzchnia do”, „Pokoje od” / „Pokoje do”, „Piętro od” / „Piętro do”; district summary „Dzielnica” + „ ({n})” with n selected districts; with `areaApprox` and no range „Teraz: ok. {areaApprox} m². Wpisany zakres zastąpi przybliżenie.” |
| pagination | `aria-label` „Paginacja”; the arrows are icons only; `aria-label` of the arrows „Poprzednia strona”, „Następna strona” |
| warnings | visually hidden prefix „Uwaga: ” |

### 7.2 Detail field labels (row order) and flags

`areaM2` Powierzchnia · `rooms` Pokoje · `floor` Piętro (with `buildingFloors`: „4 z 6”) · `buildYear` Rok budowy · `market` Rynek · `finishCondition` Stan wykończenia · `buildingType` Typ budynku · `buildingMaterial` Materiał budynku · `windowsType` Okna · `heating` Ogrzewanie · `ownership` Forma własności · `rentPln` Czynsz (zł/mies.) · `hasLift` Winda · `hasBalcony` Balkon · `hasGarage` Garaż / miejsce postojowe · `hasBasement` Piwnica · `advertiserType` Ogłaszający · `districtName` Dzielnica · `street` Ulica · fact `parking` Miejsce postojowe (z opisu) · fact `storage` Komórka lokatorska (z opisu). `pricePln` and `pricePerM2` are in the price box.

Flags (design.md §3.6, only those visible in the details): `price_outlier` „Nietypowa cena za m² (poza zakresem 7 000–40 000 zł). Sprawdź opis.” · `rent_zero_as_missing` „Ogłoszenie podaje czynsz 0 zł; traktujemy to jako brak danych.” · `rent_out_of_range` „Czynsz z ogłoszenia poza zakresem 50–5 000 zł; pominięty.” · `floor_higher_10` „Ogłoszenie podaje piętro powyżej 10; dokładny numer nieznany.” · `floor_garret` „Poddasze; numer piętra nieznany.” · `build_year_out_of_range` „Nieprawdopodobny rok budowy w ogłoszeniu; pominięty.” · `area_out_of_range` „Nietypowa powierzchnia; sprawdź ogłoszenie.” · `coordinates_outside_bbox` „Współrzędne poza Trójmiastem; lokalizacja może być błędna.” · `missing_district` „Ogłoszenie nie podaje dzielnicy.” · `unknown_value:*` „Nierozpoznana wartość pola w ogłoszeniu.” · `created_at_missing` is not shown.

### 7.3 Texts generated by the API (the UI displays them verbatim)

**Enum labels** (`/api/meta.enums`, design.md §3.3): `heating` urban „miejskie”, gas „gazowe”, boiler_room „kotłownia”, electrical „elektryczne” · `buildingType` block „blok”, apartment „apartamentowiec”, tenement „kamienica” · `windowsType` plastic „plastikowe”, wooden „drewniane”, aluminium „aluminiowe” · `buildingMaterial` brick „cegła”, silikat „silikat”, breezeblock „pustak”, concrete_plate „wielka płyta”, concrete „beton”, cellular_concrete „beton komórkowy”, reinforced_concrete „żelbet”, other „inny” · `market` primary „rynek pierwotny”, secondary „rynek wtórny” · `advertiserType` agency „biuro nieruchomości”, private „osoba prywatna” · `finishCondition` ready_to_use „do zamieszkania”, to_completion „do wykończenia”, to_renovation „do remontu” · `ownership` full_ownership „pełna własność”, limited_ownership „spółdzielcze własnościowe prawo do lokalu” · `factStatus` included „w cenie”, extra_cost „dodatkowo płatne”, not_available „brak” · `sort` newest „najnowsze”, price_asc „najtańsze”, price_desc „najdroższe”, price_per_m2_asc „najniższa cena za m²”, relevance „najlepiej dopasowane” · `provenance` listing „z ogłoszenia”, description „z opisu”, computed „wyliczone”, missing „brak danych”.

**Chips** (`chips[].label`, design.md §7.6): cities „Gdańsk”, with an out-of-scope city „Gdańsk (zamiast: Kraków)” · districts „Letnica, Wrzeszcz Górny” · price „do 600 000 zł”, „od 300 000 zł”, „300 000–600 000 zł” · area „40–80 m²”, „ok. 40 m² (35–45 m²)” · rooms „2 pokoje”, „2–3 pokoje”, „od 3 pokoi” · floor „parter”, „piętro 0–2” · market „rynek wtórny” · advertiser „osoba prywatna” · year built „rok budowy 2000–2010”, „rok budowy od 2000” · price per m² „do 9 000 zł/m²” · rent „czynsz do 700 zł” · finish, ownership, building type: the `meta.enums` label · „ogrzewanie: {label}”, „materiał: {label}” · „z windą”, „z balkonem”, „z garażem”, „z piwnicą” · text „„{text}”” · `preferences:cheap` „tanie: niższa cena za m² niż mediana miasta” · `preferences:good_condition` „ładne: stan „do zamieszkania”” · `outOfScope:rent` „wynajem: mamy tylko sprzedaż” · `outOfScope:house` „dom: mamy tylko mieszkania” · `outOfScope:metro` „metro: w Trójmieście go nie ma”.

**Summary** (`interpretation.summary`): chip labels joined with „ · ”; with `cheap` the median variant „tanie: poniżej mediany {miasto} ({mediana} zł/m², n={n})”.

**Why here** (`why`, elements joined with „ · ”): „{m²} m²” · „{dzielnica albo miasto}” · cheap: „{X}% taniej za m² niż mediana {miasta}” / „cena za m² zbliżona do mediany {miasta}” / „{X}% drożej za m² niż mediana {miasta}” · good_condition: „stan: {etykieta} (z ogłoszenia)” / „stan: brak danych”. City names in the genitive: Gdańska, Gdyni, Sopotu, Trójmiasta.

**Notices** (`notices[].text`, `actionLabel`):
- `city_out_of_scope`: „Nie mamy ofert z miasta {X}. Mamy oferty z Gdańska, Gdyni i Sopotu. Te same kryteria w: {miasta z intencji}”;
- `rent_out_of_scope`: „Mamy tylko oferty sprzedaży mieszkań, bez wynajmu.” + „Pokaż oferty sprzedaży”;
- `house_out_of_scope`: „Mamy tylko mieszkania, bez domów.” + „Pokaż mieszkania”;
- `metro_not_available`: „W Trójmieście nie ma metra. Kolej miejska to SKM i PKM; nie liczymy czasu dojścia do stacji, zawęź wyszukiwanie dzielnicą.”;
- `median_fallback`: „Za mało ofert z {miasta} (n={n}), więc „tanie” liczymy względem całego Trójmiasta.”

**Blockers** (`blockers[].label`): „bez limitu ceny (do {kwota} zł)”, „bez warunku: {etykieta chipa}”.

**Validation of `ask`** (`errors.history`): „Rozmowa może mieć najwyżej 5 pytań.”

**Warnings** (`warnings[].message`): „Nieznana dzielnica pominięta.”; „Przybliżony metraż zastąpiony zakresem.”

**Ask the seller** (`askSeller[]`, design.md §6.2, same order): „Wysokość czynszu i co obejmuje” · „Rok budowy budynku” · „Piętro mieszkania” · „Liczbę pięter w budynku” · „Formę własności i księgę wieczystą” · „Czy jest miejsce postojowe i czy jest w cenie” · „Czy miejsce postojowe jest wliczone w cenę mieszkania”.

**Fallback banner** (design.md §7.7): „Uproszczone rozumienie zapytania (AI chwilowo niedostępne): rozpoznaję liczby, miasta i kilka słów kluczowych. Popraw filtry poniżej.”

### 7.4 Formatting

`Intl.NumberFormat("pl-PL")`: „799 000 zł”, „19 087 zł/m²”, „41,86 m²”. Dates: `toLocaleDateString("pl-PL")` → „4.11.2026”. Plural: 1 → „oferta”, 2–4 (except 12–14) → „oferty”, the rest → „ofert”; analogously „pokój / pokoje / pokoi”. Floor: −1 → „suterena”, 0 → „parter”, N → „piętro N” (card: „piętro N/M”); `floor_higher_10` → „piętro 11+”, `floor_garret` with `null` → „poddasze”.

---

## 8. Accessibility

- Focus ring `var(--focus-ring)` (white 2 px + `--green-500` 2 px) on `:focus-visible` of every interactive element, including the card, the `×` chip, the thumbnail, the page number. Never `outline: none` without a replacement.
- Every field has a `<label>` or `aria-label`; groups as `<fieldset>/<legend>`; field errors linked with `aria-describedby`.
- Keyboard: Tab order = DOM order (SearchBar → Interpretation → Filters → results); chips: Tab through the `×` buttons, Enter/Space removes, focus moves to the next chip (or to the results heading when it was the last); pagination and cards are links; mode switch with arrows; Escape closes nothing (no modals).
- Asynchronous messages: results counter in `aria-live="polite"`; errors in `role="alert"`.
- `alt`: main photo in the details “{title}, photo {i} of {n}”; thumbnails `alt=""` + button `aria-label` “Show photo {i}”; photo on the card `alt=""` (title next to it).
- State is not conveyed by colour alone: badges have text, “no data” has a dashed outline and text, the current page has `aria-current`.
- Touch targets ≥ 24×24 px (WCAG 2.2 2.5.8), primary buttons 40 px.

---

## 9. What we deliberately do not do

- **Animations and transitions:** none (also a skeleton without shimmer, no `scroll-behavior: smooth`).
- **Dark mode:** out of scope.
- **Component library, CSS-in-JS, Tailwind, CSS Modules:** no; a single `styles.css` (design.md §9).
- **Icons:** no icon library. Unicode characters “×”, “‹”, “›”, “↗”, “▾” and text serve buttons, badges and notices. The details page uses one small set of inline SVG icons (`Icon` component, 24 px grid, 2 px stroke, `currentColor`, `aria-hidden`) for the location pin, the key facts panel and the parameter rows (design.md R21).
- **Modal (other than the photo and map fullscreen dialog on the details page), sticky elements, infinite scroll, remembering the mode in `localStorage`:** no.
- **Travel time, contradictions, deduplication:** out of scope ([product-scope.md](product-scope.md) §8). A map exists only as the location map on the details page (design.md R20).

---

## 10. Consistency with design.md

All UI decisions that touch the API or the application state have been carried over to `design.md` (§6.1: optional `imageUrl`, `floorHigherThan10`; §9: `format.ts`, `keepPreviousData` and `retry`, “Clear filters”, `relevance` visibility, `location.state` after a chip is removed). In case of a discrepancy `design.md` prevails, except for user-facing texts, whose single source is §7 of this document.

**Optional fields are `undefined`, never `null`:** the API omits fields whose value is `null` (design.md §6), so the UI checks presence (`x != null`, `x?.y`), not `!== null`.

---

## 11. AI-first visual direction (2026-11-05)

Supersedes §1 points 2 and 4, the colour and radius tokens in §2, and the §4.1 layout for `/`. Follows [design.md](design.md) §16. The look follows the visual language of Docplanner's patient-facing products (warm neutral background, deep teal, pill-shaped controls, white rounded cards, one large search field), measured on the live site on 2026-11-05. No logo, name, illustrations or other brand assets are reused.

### 11.1 Tokens (replace the §2 values of the same role)

```css
:root {
  --color-bg: #f5f2ef;              /* warm page background */
  --color-surface: #ffffff;         /* cards, panels, inputs */
  --color-surface-strong: #eee8e3;  /* hover, skeleton, neutral badge */
  --color-border: #dad3ca;          /* inputs, card outline */
  --color-border-strong: #b6aea5;
  --color-text: #2a2623;
  --color-text-secondary: #736c64;

  --teal-900: #006a59;  /* header and hero background, links */
  --teal-700: #007c68;  /* primary button background */
  --teal-50:  #dff9f2;  /* AI blocks: "Dlaczego pasuje", needs chips */
  --on-teal:  #f2fcf8;  /* text on teal */

  --verdict-yes: #007c68;     /* ✓ */
  --verdict-partly: #8a5a00;  /* ◐, reuses the warning palette */
  --verdict-no: #b42318;      /* ✗ */
  --verdict-unknown: #736c64; /* ? */

  --radius-md: 16px;     /* cards, inputs, panels */
  --radius-pill: 999px;  /* buttons, chips */
  --control-height: 52px;
}
```

Green tokens from §2 map to teal (`--green-600` → `--teal-700`, `--green-50` → `--teal-50`, `--green-500` focus ring → `--teal-900`). Contrast: `--on-teal` on `--teal-700` and `--teal-900`, and `--teal-900` on `--color-bg`, are above 4.5:1 (to be confirmed with the §2 formula when the CSS is written).

### 11.2 Layout of `/`

The hero, the example chips and the teal bands below were replaced by the layout of §11.5; the card and group rules still apply.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ Flatplanner                                         teal-900 header, 64 px    │
├──────────────────────────────────────────────────────────────────────────────┤
│                 Opisz, czego szukasz. Resztę przeczytamy za Ciebie.           │  hero, teal-900, white
│   ┌───────────────────────────────────────────────────────────┐ ┌─────────┐  │  heading 32 px / 600
│   │ np. Pracuję na nocki i mam psa, szukam czegoś w Gdańsku…  │ │ Szukaj  │  │  input: white, 52 px,
│   └───────────────────────────────────────────────────────────┘ └─────────┘  │  radius 16; button pill
│   (Ukrainiec, szkoła na Morenie) (Muzyk, ćwiczę codziennie) (Pies i park)     │  example chips, pill,
├──────────────────────────────────────────────────────────────────────────────┤  fill the input on click
│ Zrozumiałem: Gdańsk · podstawówka blisko · zakup bez zezwolenia MSWiA        │  summary + need chips
│ (podstawówka w zasięgu spaceru) (zakup bez zezwolenia MSWiA)                  │  (teal-50, pill)
│ ⓘ Nie oceniamy okolic po tym, kto w nich mieszka. Sprawdziliśmy za to: …     │  declined / notices
├──────────────────────────────────────────────────────────────────────────────┤
│ ┌────────┬──────────────────────────────┬─────────────────────────────────┐  │  card: white, radius 16
│ │ photo  │ 680 000 zł                   │ DLACZEGO PASUJE                 │  │  right column = AI,
│ │ 200×150│ 45,92 m² · 2 pokoje · 4/4    │ ✓ Szkoła podstawowa 2 min pieszo│  │  teal-50 background
│ │ radius │ Ujeścisko-Łostowice, Gdańsk  │   „szkoła podstawowa - 2 minuty │  │  (the place where the
│ │ 12     │ AI: Jasne 2 pokoje na ost.   │    pieszo”                      │  │  booking calendar sits
│ │        │ piętrze, gotowe do wejścia…  │ ◐ Hipoteka banku do spłaty z    │  │  on a doctor card)
│ │        │                              │   ceny – zapytaj o harmonogram  │  │
│ └────────┴──────────────────────────────┴─────────────────────────────────┘  │
│ ▸ Ogłoszenie o tym nie mówi (4)                                               │  collapsed group
│ Wolisz klasyczne filtry? → link to the filter view (§4.1 layout, restyled)    │
└──────────────────────────────────────────────────────────────────────────────┘
```

- Before the first question the page shows only the hero and the example chips; no list. The classic list with filters (§4.1) moves behind the link "Wolisz klasyczne filtry?".
- While `ask` runs (5–10 s): the hero input stays, below it a progress card with three steps that light up in turn: "Rozumiem pytanie" → "Czytam ogłoszenia" → "Sprawdzam dowody". This is a fixed animation on a timer, not real progress.
- Card desktop: `grid-template-columns: 200px 1fr 320px`; ≤ 959 px the AI column goes under the content; ≤ 640 px vertical card. Per need: verdict icon in its colour, reason (`--fs-sm`), evidence under it (`--fs-xs`, secondary; quotes in „…”, places as "Nazwa · 148 m", fields as "Pole: wartość").
- Groups (design §16.2 step 5) are separate sections with the group heading; the `near` group shows the failing need first, in `--verdict-no`, so the user sees at once what they would give up. When `full` is empty, the relaxation lead and buttons sit above the groups.
- Risks: under the needs in the AI column, heading "Na co uważać", one line per risk with its kind as a small badge and the evidence under it; an `extra_cost` risk with `totalWithExtrasPln` shows "Łącznie z dopłatą: {total}" in `--fs-md`, bold.
- Details page: same tokens, no provenance badges (design §16.5).

### 11.3 Copy for §7 (implemented in #7′b; the single source stays this table, §7.1 adds only the strings below)

| Key | Text |
|---|---|
| hero heading | Opisz, czego szukasz. Resztę przeczytamy za Ciebie. |
| input placeholder | np. Pracuję na nocki i mam psa, szukam czegoś w Gdańsku do 700 tys. |
| button | Szukaj |
| progress steps | Rozumiem pytanie · Czytam ogłoszenia i sprawdzam dowody |
| summary prefix | Zrozumiałem: |
| AI column heading | Dlaczego pasuje |
| verdicts (aria-label) | pasuje · częściowo · nie pasuje · brak danych |
| group headings | Pasuje · Pasuje częściowo · Prawie pasuje: jedna rzecz nie gra |
| unknown group | Ogłoszenie o tym nie mówi ({n}) |
| relaxation lead (no `full`) | Żadne mieszkanie nie spełnia wszystkich warunków. Najczęściej odpada: {need} ({count} z {total}). |
| relaxation button | Pokaż bez tego warunku ({count}) |
| need chip remove button (aria-label) | Usuń warunek „{need}” |
| restore excluded needs | Przywróć wszystkie warunki |
| clear the question (aria-label) | Wyczyść pytanie |
| hard-filter relaxation | Bez warunku „{filter}”: {count} mieszkań |
| risks heading | Na co uważać |
| risk kinds | Dopłata · Stan prawny · Lokal zajęty · Hałas · Niezgodność z danymi ogłoszenia |
| real price | Łącznie z dopłatą: {total} |
| ask network or 5xx error (under the field) | Nie udało się połączyć. Spróbuj ponownie lub skorzystaj z klasycznych filtrów. |
| AI unavailable | AI chwilowo niedostępne — pokazujemy wyniki według podstawowych kryteriów. |
| no results (no candidates and no relaxation helps) | Nie mamy mieszkań, które spełniają te warunki. Spróbuj opisać to inaczej. |
| judged window (`remainingCount > 0`; design 16.9) | Sprawdzone: {shown} z {total} pasujących ogłoszeń, od najbardziej obiecujących. |
| next window button (design 16.9) | Sprawdź kolejne ogłoszenia |
| judge batch failed (API notice `judge_failed`) | Części ogłoszeń nie udało się sprawdzić — oznaczyliśmy je jako brak danych. |
| field evidence (API) | {7.2 label}: {value}; booleans „tak” / „nie”; floor as in 7.2 („4 z 6”, „parter”, „suterena”) |
| unnamed place evidence (API, „{name} · {d} m”) | `amenity`: school szkoła · kindergarten przedszkole · place_of_worship miejsce kultu · pharmacy apteka · clinic przychodnia · hospital szpital · doctors gabinet lekarski · university uczelnia · veterinary weterynarz · bar bar · pub pub · nightclub klub nocny · restaurant restauracja · cafe kawiarnia · charging_station stacja ładowania · bicycle_rental wypożyczalnia rowerów; `leisure`: park park · playground plac zabaw · dog_park wybieg dla psów · fitness_centre siłownia · sports_centre centrum sportowe · swimming_pool basen · stadium stadion; `natural=wood`, `landuse=forest` las · `natural=beach` plaża · `landuse=cemetery` cmentarz; `railway`: station stacja kolejowa · halt przystanek kolejowy · tram_stop przystanek tramwajowy · rail linia kolejowa · tram torowisko tramwajowe; `highway`: bus_stop przystanek autobusowy · motorway autostrada · trunk droga ekspresowa · primary główna ulica; `shop`: supermarket supermarket · convenience sklep spożywczy · mall centrum handlowe; `place=city|town` centrum miasta · `aeroway=aerodrome` lotnisko; `route`: bus linia autobusowa · trolleybus linia trolejbusowa · tram linia tramwajowa · train linia kolejowa · light_rail kolej miejska; any other tag „miejsce” |

### 11.4 Classic list as built (item #5, restyled; the results of `/` without a question since R26)

The classic list uses the §11.1 tokens. Since R26 it is what the results column of §11.5 shows while there is no `q`; the text search sits at the top of the filter panel. Deviations from §4.1, §5.4 and §9, decided with the restyle (the top-bar search field of the first bullet moved to the filter panel with R26):

- **One top bar, no hero:** a single 72 px bar (64 px ≤ 640 px) on `--color-surface` with a 1 px `--color-border` bottom edge holds, in one row, the logo, the wordmark, the search field and the „Szukaj” button. There is no teal header or hero band and no gradient (the §11.2 teal bands are dropped for the classic list; the teal stays in buttons, pills, links and the focus ring). The search field is a 52 px pill on `--color-bg`. ≤ 640 px the wordmark is hidden. No tagline or data date, so the §5.1 right-hand text and the §7.1 header string are dropped.
- **Brand assets:** `web/src/assets/flatplanner.svg` (logo, 40 px; the supplied file without its `#F5F2EF` background tile) and `flatbanner.svg` (wordmark, 26 px high, the supplied file as is) with the motto from §7.1 under it (`--fs-xs`, secondary). The logo link has `aria-label` „Flatplanner”. The logo is also the favicon (`index.html`).
- **Pills in the filter panel:** city, market, advertiser and the two amenities are pill toggles (hidden native `checkbox`/`radio`, styled label) instead of checkbox and radio rows. Labels and counts are unchanged, e.g. „Gdańsk (108)”.
- **Districts:** a `<details>` titled „Dzielnica” ({n} selected) closed by default; inside, district pills grouped by city (the §5.4 scroll limit of 240 px stays).
- **No Apply button:** the filters write the URL on every change (design §9). „Wyczyść filtry” is a link at the top of the panel; nothing in the panel is sticky. On mobile the panel stays open until the „Filtry ({n})” toggle is pressed again.
- **Bigger cards, swipeable photos:** the container is 1280 px and the photo column is 42 % of the card (min 260 px, 4:3), so the photo is roughly twice as large as in §5.6. The photo is a gallery of all `imageUrls` (design §6.1): CSS scroll-snap track (native swipe on touch, trackpad and drag-scroll), a „{n} / {total}” counter, and „‹” / „›” buttons on hover on devices with a pointer (hidden on touch and at the ends). A click on the photo opens the listing. The card is an `<article>`; the title is the link (stretched over the whole card, focus ring on the card) instead of the whole card being one `<a>`, because the gallery buttons cannot live inside a link. The §5.6 title line is two lines instead of one.
- **No floor hint:** the „parter = 0” note under the floor fields (§5.4) is dropped.
- **Fields:** filter fields are 44 px (`--field-height`), the search field 52 px.
- **`--color-border-strong`** is `#8a8279`, not `#b6aea5`: the §11.1 value gives 2.19:1 on white, below the 3:1 §2 requires for field outlines (`#8a8279`: 3.78:1 on white, 3.39:1 on `--color-bg`).
- **Contrast checked** with the WCAG formula: `--on-teal` on `--teal-900` 6.26:1 and on `--teal-700` 4.91:1; `--teal-900` on `--color-bg` 5.87:1; white on `--teal-700` 5.14:1; `--color-text-secondary` on `--color-bg` 4.64:1.
- Provenance, verdict and info tokens are not defined in `styles.css` until a view uses them.

### 11.5 Ask page `/` as built (item #7′b)

**Superseded in part by R28 (user decision 2026-11-08):** the chat is the landing page `/` and the classic list with its filter panel and text search is a separate mode at `/listings`; the filter panel is no longer part of the chat page. A segmented switch in the header (`ModeSwitch`, `<nav aria-label="Tryb wyszukiwania">`: sparkles icon „Czat AI” and list icon „Lista”, `NavLink`s, labels hidden ≤ 640 px) moves between the modes. `?listing=<id>` opens the details in either mode and „Wróć” returns to that mode; `/listings/:id` redirects to `/listings?listing=<id>`. The bullets below that describe the one-screen layout hold only where R28 does not say otherwise. The classic list (R28 polish): the sidebar (text search and filter panel) is sticky and scrolls on its own on desktop (static under 960 px), the panel and the cards carry a one-pixel soft shadow (the card shadow grows on hover), range fields are 40 px high, the sort select is a pill, loading skeletons shimmer (static under `prefers-reduced-motion`) and the empty result shows a round search icon above „Brak ofert dla tych kryteriów.”.

- **Landing (R28):** without `q`, a centred hero on the page background: a 56 px round badge with the sparkles icon, the heading „Opisz, czego szukasz.” plus „ Resztę przeczytamy za Ciebie.” in a teal-to-blue text gradient (`clamp(28px, 4vw, 40px)`), and the composer. No example prompts, no filter panel. The composer is a white rounded box (radius 28 px, 1 px `--color-border`, soft shadow, focus ring on focus-within) with a `textarea` that grows to 160 px and a round 40 px send button with an up-arrow icon (`aria-label` „Wyślij”, disabled while the text is empty). Enter sends, Shift+Enter inserts a new line (not while an IME composes). From 90% of `meta.chatMaxLength` a counter „{n}/{max}” shows under the box.
- **Thread (R28, design 16.5a):** every question is a turn; the URL repeats `q` (`?q=…&q=…`, the last one is current, at most 5) so refresh and „back” work. A turn is the user's message in a right-aligned `--teal-50` bubble and below it the assistant block: a 32 px round avatar with the sparkles icon (teal-to-blue gradient) and, without a bubble, the reply (the „Zrozumiałem: …” line, need chips, notes, results). Older turns keep their reply and need chips (read-only, no „×”) and fold their results into „Pokaż wyniki ({n})”; the last turn shows its results, pagination (`page`), relaxations, the unknown group and „Sprawdź kolejne ogłoszenia”. A new turn scrolls into view. The composer is docked to the bottom of the viewport (sticky, fading page background) with the placeholder „Dopytaj albo zmień kryteria”, and a round „Nowa rozmowa” button (new-chat icon, `aria-label` and `title`) left of it clears every `q`. With 5 turns the composer gives way to the line „Rozmowa może mieć najwyżej 5 pytań. Zacznij nową.”. Each turn asks with `history` = the earlier messages; `excludeNeeds` and the „Przywróć wszystkie warunki” link apply to the last turn only.
- **Streaming (design 16.5b):** the client reads `POST /api/search/ask/stream`. Until `understood` arrives the assistant block shows „Rozumiem pytanie” with the bouncing dots; the reply then appears (the „Zrozumiałem:” text types in over about a second, off under `prefers-reduced-motion`), and until `results` arrives three skeleton tiles and „Czytam ogłoszenia i sprawdzam dowody” follow. An `error` event or a failed request shows the connection error with „Spróbuj ponownie”.
- The AI results are paginated, 12 tiles per page (`ASK_PAGE_SIZE`, shared `Pagination`, parameter `page`) in the last turn; the unknown group „Ogłoszenie o tym nie mówi ({n})” is a collapsed block on the last page.
- The header is the white top bar of §11.4 on every page and holds no search field.
- The progress indicator (CSS animation, a user-requested exception to §9) is the three bouncing dots after a step label; it reflects the two stages of the stream, not a timer.
- **Tiles (R29, user decision 2026-11-08; replaces the text-heavy tiles of 2026-11-06):** results are a grid of exactly two tiles per row (one column ≤ 640 px) that look like the classic list card (§11.4 and R27): the photo carousel on top (16:10, a link), then the same body as `ListingCard` (`ListingCardBody`: price, facts with icons, location with the pin icon, title, signal and feature pills; the approximate-location note is only the dashed-circle marker, never a text pill). Below a top border come the need pills (verdict icon and the need, in the verdict colour; in the `near` group the failing need comes first) and a collapsed `<details>` „Dlaczego pasuje” with one short line per need (the `reason`, at most 10 words by the judge prompt) and its first evidence chip (a quote in „…”, a place or field as given). There is no AI summary box and no emoji; risks are the risk pills with tooltips (the full reasons and quotes are in the risk box on the details page); „Łącznie z dopłatą: {kwota}” stays visible under the price. Excluded needs are left out of the pills and the reasons. Tiles are top-aligned (the grid does not stretch them), so opening „Dlaczego pasuje” in one tile never moves its neighbour; a need whose `reason` is empty shows the need text.
- **Group headings (R29):** every group starts with a full-width coloured bar, a 22 px icon, the heading in 18 px bold and the number of listings in the group as a pill on the right: `full` „Pasuje” (teal, check), `partial` „Pasuje częściowo” (amber, half circle), `near` „Prawie pasuje: jedna rzecz nie gra” (red, warning).
- The assistant avatar of a turn is the Flatplanner logo in a 36 px white circle with a thin border (R29).
- Need chips (`--teal-50`, pill) have a „×” button (aria-label „Usuń warunek „{need}””) that re-sends the question with that need excluded; an excluded need leaves the chip row and „Przywróć wszystkie warunki” (link button) appears after the chips. Notices (`clarify`, `ai_unavailable`, `judge_failed`) and declined parts are plain secondary-colour lines with „ⓘ”, not coloured boxes. The relaxation lead stays a white panel with buttons.
- Relaxation lead takes `{total}` as the number of cards shown; one button per relaxation. With no cards, hard-filter relaxations are plain lines.
- **Judged window (design 16.9):** below the last tile, while `remainingCount > 0`, the judged-window line (secondary colour, `{shown}` = cards of the shown set, `{total}` = `{shown}` + `remainingCount`) and the „Sprawdź kolejne ogłoszenia” button (secondary button), which re-sends the question with `more: true`; while it runs, the progress steps replace the button. The 12-tile pagination stays; the new window regroups the shown set.
- With `needs: []` (AI fallback) the group headings are not shown, because the cards are plain filter results.
