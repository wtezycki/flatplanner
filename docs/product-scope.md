# Flatplanner: product scope

The binding build scope: who the product is for, which features go into the MVP, what is deliberately left out of scope and why.

As of 2026-11-05 (AI-first pivot, section P).

Basis of decisions: a sample of the 20 newest flat sale listings in Gdańsk from otodom (2026-11-04) and [scraping-plan.md](scraping-plan.md).

**Value proposition.** The buyer describes their life in their own words; Flatplanner reads every listing for them and says which flats fit and why, with evidence for every claim. Filters exist for data the listing does not have as fields: the description and the surroundings of the flat.

**Why a portal would build it.** Buyers who get listings judged against their actual needs, with evidence and visible risks, reach the seller better qualified, with fewer dead ends and fewer contacts spent on information the ad should have given. For the portal that means more qualified leads per search and listings whose quality is visible and measurable (fill rate, quality flags, rejected facts), which is a lever towards advertisers as well.

**Identifiers.** UC are use cases, W are product differentiators. The same numbering applies in all documents in `docs/`:

| Id | Meaning | Id | Meaning |
|---|---|---|---|
| UC1 | list of listings with pagination | UC12 | zero results: what blocks |
| UC2 | filters | UC13 | query outside the data (other city, rental) |
| UC3 | text search | UC14 | legal status visible immediately |
| UC4 | listing details | UC15 | filters on facts from the description, not only on otodom fields |
| UC5 | fact available only in the description | UC16 | district selection: list and map |
| UC6 | contradiction between field and description | UC17 | walking time (SKM/PKM, school, beach) |
| UC7 | duplicates | W1 | provenance of every piece of information |
| UC8 | suspicious price | W2 | extraction of facts from the description with a text anchor |
| UC9 | natural-language query → filters and preferences | W3 | intent-based search with a visible, editable interpretation |
| UC10 | “cheap” in numbers | W4 | “cheap” in the context of city prices, with the sample size |
| UC11 | unverifiable criteria (“spokojna okolica” (quiet neighbourhood)) | W5 | Real price (price + paid extras) |
| | | W6 | declarations from the description vs facts |
| | | W7 | “Ask the seller about:” |
| | | W8 | duplicate grouping |

---

## P. AI-first pivot (2026-11-05)

This section supersedes the rows and paragraphs of sections 0, 3, 4, 7, 8, 9 and 11 it names. The technical design is [design.md](design.md) §16, the UI is [ui-spec.md](ui-spec.md) §11.

**Problem, measured on the import (197 listings).** Portal filters cover only the fields the source has; what matters to a buyer sits in the description or in the surroundings. `kredyt` appears in 81 descriptions, ~77 of them as agency boilerplate; `cisza` in 50, 13 of them only as "cichobieżne windy" (quiet lifts); 20 "pet" hits are all "kotłownia" (boiler room). Keyword search returns noise, fields return nothing.

**Product.** One chat field: "Opisz, czego szukasz. Resztę przeczytamy za Ciebie." The user states any need (night shifts, a dog, a child walking to school, a wheelchair, an EV, a non-EU passport). AI judges every listing against those needs using its dossier: description, fields, an AI summary and nearby places from OpenStreetMap. Every verdict (fits / partly / does not fit / unknown) points at evidence that code verifies: a quote, a place with a distance, or a field. No evidence → "unknown", shown as "Ogłoszenie o tym nie mówi". There is no catalog of criteria.

**Principle.** "The model judges, code verifies." Replaces "the model translates, code decides" (section 7).

**Guardrails.** The system judges flats and places, never people: requests about who lives in an area are declined with a checkable alternative. Personal traits are context, not stored and not logged; they affect a verdict only through a stated legal rule (non-EU citizenship → MSWiA permit for full ownership in the border zone, to be verified).

**Scope changes.**

| Was | Now |
|---|---|
| §4 #6 mini-W2 + Real price | listing dossier at import: AI summary + nearby places (design §16.1) |
| §4 #7 chat → `SearchIntent`, #9 ranking and interpretation | one `ask` endpoint: needs → AI verdict per listing → evidence check → grouping (design §16.2) |
| §4 #5 details with provenance badges | plain details: fields, description, link to otodom |
| §4 #10 chips, #12 ask the seller | cut |
| §4 #11 what blocks | replaced: results in groups (fits / partly / one thing does not fit / unknown) and "show without this condition" computed from the verdicts already made (design §16.2) |
| §4 #6 Real price | returns at query time for verified extra costs; extra costs are the most common hard fact in descriptions (~18% of listings) |
| — | new: buyer risks per listing (extra cost, legal status, occupied, noise, field vs description mismatch), each with evidence |
| §4 #2–#4 list, filters, text search | kept as built, secondary path ("Wolisz klasyczne filtry?") |
| §8 full W2 + UC15, UC17 walking distance, UC6 contradictions, UC11 unverifiable criteria | in scope through AI judgement over the dossier (distances, not walking time; contradictions surface as verdict evidence) |
| §7 "no LLM ranking, no summaries" | the LLM produces verdicts that order results and per-listing summaries; both are bounded by evidence verification |

**Personas, re-read.** P1 mortgage buyer: land register and bank-mortgage risk from the description. P2 newcomer: places around the flat instead of district names. P4 family: school, park, lift from fields and surroundings. New: P5 foreign buyer (MSWiA permit vs cooperative ownership).

**Demo listings:** 70 (one listing, six scenarios, mortgage risk), 97 ("3 rooms" in the field, no kitchen in the description), 166 (cooperative, foreign buyers welcome). Scenarios: design §16.6.

**Key assumption (replaces §11).** Descriptions and surroundings hold enough checkable evidence to answer non-standard needs for a meaningful share of listings. Verified by `eval-intents` on the chat scenarios and by the share of verdicts downgraded to "unknown" for missing evidence.

**Success metric (unchanged in shape).** Share of chat searches where the user opens a listing from the top five and clicks "Zobacz na otodom". Guardrails: share of `unknown` verdicts, fallback share, cost per question (~$0.3, estimate).

**Failure mode (replaces §11).** The judge is only as good as the dossier: 83 of 197 listings have an approximate location, OSM is incomplete, and descriptions are marketing. A listing that does not describe a quality it has is ranked below one that does, which favours verbose agencies over private sellers.

**`unknown` is a feature.** For personal needs (pets, named schools, lift dimensions, tolerance) the data often has no answer; saying so is correct behaviour, not a model failure, and the 1-pager presents it that way.

**Cut order (replaces §9).** 1. classic filters page restyle; 2. AI summary on cards; 3. guardrail notices beyond `declined`; 4. risks other than `extra_cost`; 5. domain rules (16.3); never cut: `ask` with evidence verification, demo listings, deploy.

---

## 0. Key scope decisions

| Element | Decision | Rejected alternative | Reason |
|---|---|---|---|
| Deploy and chat protection | MUST; deploy of an empty skeleton as the second implementation step | no deploy | a public URL is a task requirement |
| Extraction from the description (W2) | **mini-W2**: only parking and storage unit, with a text anchor, in details | rent (czynsz, maintenance fee), parking, finish condition, land and mortgage register (KW, Księga Wieczysta), caveats + filters (UC15) | maintenance fee, finish condition and form of ownership are otodom fields; KW in 1/20 descriptions; parking with a price in 7/20 |
| Legal status (UC14) | form of ownership from the field, in details | block at the top of the listing, KW, filters | as above: KW is almost never in the description |
| District (UC16) | `reverseGeocoding` from otodom, list in the filter | point-in-polygon on OSM boundaries + map | the district is in 20/20 listings, already derived from GPS |
| Lift, balcony (UC2) | two-state “yes / no information” | three-state yes/no/no information | `Extras_types` is a list, absence of an element ≠ “no” |
| Maintenance fee | from the field, `Rent ≤ 0 → null` | from the field or from the description | `Rent="0"` in 7/20 listings |
| Scenario A | filters + district list | map, polygons, walking time | the district is in the data; the map brings no new information |
| Map, walking time (UC16, UC17) | out of scope | MUST/SHOULD | OSM cost ~1.9h |
| Contradictions (UC6/W6) | out of scope | MUST + COULD | they require full extraction from the description (W2) |
| Scraper robustness | raw JSON + hard-field contract + fill-rate report | carrier classification, path fingerprinting, circuit breaker ([scraping-plan.md](scraping-plan.md) §7) | one-off import |
| Key assumption | otodom fields already cover the facts buyers ask about; the description's unique value is paid extras | “cheap is relative to the city” (a definition, not a risk) | it is testable on the import (fill rate per field, share of descriptions with a priced parking space) and justifies why AI extracts only two facts |
| Text search (UC3) | `LIKE` on `search_text` without diacritics | FULLTEXT + declension dictionary | with ~150 records `LIKE` is enough |
| UC12, chips with removal, W7 | in scope | out of MVP | low implementation cost |

---

## 1. Settled decisions

- **Data:** Tricity (Trójmiasto) (Gdańsk, Gdynia, Sopot), flat sales only, ~120–150 listings. Scenarios A and B run on Gdańsk. Rationale: several cities and many districts give sensible location filters and stable medians ([scraping-plan.md](scraping-plan.md) §2).
- **Kraków and other cities outside the data:** in the chat, an explicit answer with a proposal of the same criteria in Gdańsk (UC13).
- **Source:** otodom.pl, `__NEXT_DATA__`. Details in [scraping-plan.md](scraping-plan.md), with the scraper robustness simplified relative to §7 of the plan (section 0).
- **Stack:** C#/.NET (ASP.NET Core) + React + MySQL: a stack allowed in the task requirements and consistent with the team's stack. The 1-pager and README give the reason for the choice in one sentence.
- **Application under a public URL** (task requirement). Data prepared locally and uploaded to production.
- **Code is written mainly by an LLM agent**; the author reviews, integrates and makes decisions.

---

## 2. What the data says (sample: 20 listings, Gdańsk, 6 from the primary market, 14 from the secondary)

| Field | Fill | Consequence |
|---|---|---|
| price, m², rooms, market, GPS | 20/20 | hard fields, the contract rejects a record without them |
| floor `Floor_no` | 19/20 | `floor_N → N`, ground floor → 0, `floor_higher_10 → 11` (the value name for the ground floor to be confirmed in the import; only `floor_N` occurred in the sample) |
| year built | 17/20 | |
| form of ownership `Building_ownership` | 19/20 | `full_ownership`, `limited_ownership`; label from `localizedValue`, if present |
| status `Construction_status` | 17/20 | primary always `to_completion` (6/6) |
| maintenance fee `Rent` | 12/20 real, 7/20 `"0"`, 1/20 missing | `Rent ≤ 0 → null` + flag |
| amenities `Extras_types` | list | `lift` in 12/20; absence from the list = “no information” |
| district `reverseGeocoding` | 20/20 | administrative names; unusual label = flag |
| `mapDetails.radius` | 20/20 | `> 0` (6/20) = approximate location |
| `user_type` | 20/20 | only `agency` / `private` |
| description: parking with a price or a condition | 7/20 | the only fact worth LLM extraction |
| description: maintenance fee | 6/20 | only where the `Rent` field already exists |
| description: KW / mortgage / perpetual usufruct | 1 / 1 / 0 out of 20 | extraction not worthwhile |

**Unverified:** Gdynia and Sopot. The fill-rate report from the first import checks them (section 10).

---

## 3. Who it is for

| Persona | Main concern | What in scope answers it |
|---|---|---|
| P1 buyer with a mortgage | the price from the ad ≠ the real cost | Real price with the parking surcharge (mini-W2), maintenance fee, form of ownership, “Ask the seller about:” |
| P2 newcomer | choosing the wrong area | district list per city; walking time is out of scope |
| P3 investor | overpaying | “cheap” = price/m² relative to the city median with `n` |
| P4 family | wasted viewings | floor, lift (“yes / no information”), rooms, balcony |

---

## 4. Features in scope

| # | Feature | Ref | Acceptance criterion (short) | Data | LLM |
|---|---|---|---|---|---|
| 1 | Import + normalization + quality flags | scraping-plan, UC8 | ≥ 100 `FLAT` listings; hard-field contract; `Rent ≤ 0 → null`; `price_outlier` outside 7–40 thousand zł/m²; `location_approximate` from `radius > 0`; fill-rate report per city | otodom | — |
| 2 | List with pagination | UC1 | 20 per page, total count, state in the query string, “back” preserves filters; card: price, m², price/m², rooms, floor, city and district, photo or placeholder | otodom | — |
| 3 | Filters | UC2 | city (multiple choice), district dependent on city, price, m², rooms, floor, market, advertiser, lift/balcony “yes”; boundaries inclusive; min > max → swap; under the list “+N listings with no information about the lift” | otodom | — |
| 4 | Text search | UC3 | `LIKE` on `search_text` (title + description + district + street; lowercase, no diacritics, “ł → l”) | otodom | — |
| 5 | Listing details | UC4, W1 | all fields; empty ones as “no data”; provenance label: “from the ad / from the description / calculated / no data”; form of ownership from the field; “approximate location”; link to the source and retrieval date | otodom | — |
| 6 | Mini-W2 + Real price | W2, W5 | parking and storage unit: `included` / `extra_cost` + amount / `not_available`; the quote must occur in the description after normalization; the amount must be in the quote; in details “price + parking = Real price” with the quote; log of the rejection rate | description | **yes** (import) |
| 7 | Chat → `SearchIntent` | UC9, W3, UC13 | structured output validated against a schema; city only from the enum; `requestedCityOutOfScope` (Kraków); `outOfScope` (rental, house); “metro” → a message that there is none in Tricity; timeout 5 s → fallback | enums from DB | **yes** (runtime) |
| 8 | Chat fallback | W3 | regex of numbers with a unit + Tricity cities with declension + ~10 largest cities outside the scope; message “simplified query understanding” | code | — |
| 9 | Ranking and interpretation | W3, W4, UC10 | “cheap” = percentile of price/m² in the listing's city, median from `n`, Sopot at n < 10 → Tricity with an annotation; “nice” = finish condition; fixed weights listed in the README; “I understood: …” line; “Why here” in 1 line | otodom | — |
| 10 | Chips with removal | W3 | removing a chip changes `SearchIntent` without calling the model again | code | — |
| 11 | Zero results: what blocks | UC12 | COUNT for each condition removed separately, clickable options | SQL | — |
| 12 | “Ask the seller about:” | W7 | rules from empty fields: maintenance fee, year built, floor, form of ownership, parking | code | — |
| 13 | Chat protection | public URL requirement | `AddRateLimiter` per IP + `UseForwardedHeaders`; ≤ 300 characters; hard spend limit at the API provider; `CHAT_MODE=fallback` as the kill switch | code | — |
| 14 | Set of 10 test queries | UC9 | expected `SearchIntent`; xUnit test for the fallback, script for the LLM; result in the README | code | tests the LLM |
| 15 | Deploy + data in production | task requirement | one container (the API serves the SPA), managed MySQL, HTTPS, secrets in env; `mysqldump` → import; smoke test of A and B | — | — |
| 16 | 1-pager + README | task requirement | the 7 points required in the task description on one page; README: how to run, scenarios A and B, reason for choosing C#, ranking weights | — | — |

---

## 5. Scenario A: “Gdańsk, 40–80 m²”

1. The user goes to `/` and sees all listings, sorted from the newest.
2. They choose the city “Gdańsk” and the floor area 40–80 m². URL: `/?cities=gdansk&areaMin=40&areaMax=80`.
3. They see the number of results and 20 cards on the page.
4. They narrow down to a district from the list (e.g. Wrzeszcz Górny) and sort by price/m² ascending.
5. They open a listing. They see all fields with a provenance label, “no data” instead of empty places, and the form of ownership. If the description mentions parking, they see the Real price with a quote. Below is the “Ask the seller about:” list.
6. They return with the “back” button to the same page with the same filters.

**Edge cases:** “40,5” with a comma; area `"40.50"` normalized at import; 0 results → UC12; “Kraków” typed into the search field is plain text search (0 results with “what blocks”); the explicit out-of-scope answer (UC13) is given by the chat, because keyword parsing of the search field is cut ([design.md](design.md) §6.7).

## 6. Scenario B: "chcę ładne, tanie mieszkanie, 40 m w Gdańsku" (I want a nice, cheap flat, 40 m in Gdańsk)

Flow: the sentence from the chatbox goes to the model, which returns a `SearchIntent` validated against a schema. Hard criteria filter the list with the same query as in scenario A, and soft preferences (“cheap”, “nice”) set the ranking in C#. Above the results there is an “I understood: …” line and chips, and next to each listing “Why here”. Details: [design.md](design.md) §6.3 and §7. Clarifications:
- “nice” = `Construction_status` (`ready_to_use` +, `to_renovation` −, `to_completion` −). Stated explicitly in the README: on the primary market this works like “secondary market” (failure mode, section 11);
- “40 m” → `areaApprox: 40`, tolerance ±12% (35–45 m²) computed in C#, not in the model;
- after a timeout, an API error or the provider's spend limit the fallback kicks in, and the fallback also recognizes “w Krakowie” (in Kraków);
- multi-step refinement (“a coś bliżej morza?” (and something closer to the sea?)) is out of scope.

**Kraków variant:** “…w Krakowie” → `requestedCityOutOfScope: "Kraków"`, the rest of the intent preserved. UI: “We have no listings from Kraków. The same criteria in Gdańsk:” + results with the chip `Gdańsk (instead of Kraków)`.

---

## 7. Where the LLM is

| Place | When | Input → output | When the model is wrong | When the model does not respond |
|---|---|---|---|---|
| Mini-W2 | import, one-off, ~150 calls | description → `parking`, `storage`, each with a quote | the anchor rejects facts without a quote in the description and amounts outside the quote; the quote is visible in the UI | fact = “no data”, the import continues |
| Chat | runtime, 1 call per query | sentence + enums from DB → `SearchIntent` | validation against the schema and the enum; tolerances in code; chips show the interpretation | timeout, error or limit → deterministic fallback |

Everything that decides the results (filters, ranking, medians, “what blocks”) is deterministic. The model translates text into structure, the code does the rest.

**What we deliberately do not do with the LLM:** extraction of maintenance fee, finish condition and form of ownership from the description (they are in the otodom fields), KW extraction (1/20 descriptions), ranking (non-deterministic and untestable), listing summaries (they repeat the marketing of the description), embeddings (with ~150 listings SQL returns everything that matches, and the problem is understanding the query), photo analysis for “nice”.

---

## 8. Out of scope → "What would you improve with more time"

| Feature | Why not now | Sentence for the 1-pager |
|---|---|---|
| Full W2 + filters on facts from the description (UC5, UC15) | maintenance fee, finish condition and ownership are in the fields; KW in 1/20 descriptions | “Filters on facts from the description and a measurement of extraction precision on a hand-labelled sample.” |
| Legal status as a block with KW (UC14) | KW is almost never in the description | “Legal status requires a source outside the ad; the description does not contain it.” |
| Map, clickable districts (UC16) | the district is already in the data; the map is a UI cost without new information | “A map for choosing districts for people who do not know the names.” |
| Walking time (UC17) | OSM export and processing ~1h | “Walking time to SKM/PKM, school and beach from a one-off OSM export.” |
| Field vs description contradictions (UC6/W6) | requires full W2 | “Detection of declarations that contradict the ad data.” |
| Deduplication across agencies (UC7/W8) | grouping is a UI and data-model cost; the import report measures candidate pairs instead of assuming there are few ([design.md](design.md) §4.4) | “Grouping the same unit listed by several agencies; the import found {n} candidate pairs among {N} listings.” |
| Scraper robustness ([scraping-plan.md](scraping-plan.md) §7) | one-off import | “With continuous crawling: response classification and an alert on a change in the `__NEXT_DATA__` structure.” |
| Declension dictionary, FULLTEXT | with 150 records `LIKE` is enough | “At large scale: an ngram parser or a search engine with a Polish stemmer.” |
| Multi-step refinement in the chat | UI and test cost | “A conversation that modifies the previous `SearchIntent`.” |
| Median per (city, market) | n ≥ 30 realistically only in Gdańsk | “Price comparison within the primary and secondary market separately.” |

---

## 9. Priorities and cut order

Implementation order and definition of done of each item: [design.md](design.md) §13.

**Not subject to cuts:** deploy, list + filters + pagination + details, scenario A, 1-pager.

**Cut order** (when time runs out, item 1 goes first; the technical list is in [design.md](design.md) §14.4). Keyword parsing of the search field and the “+N listings without information” count are cut before implementation:
1. chips with removal (filters remain a way to correct the interpretation)
2. “Ask the seller about” (W7): the only feature that turns missing data into an action, so it goes second
3. “What blocks” at zero results (UC12)
4. “Why here”
5. “nice” in the ranking
6. mini-W2
7. fallback regex
8. chat in production without the LLM (`CHAT_MODE=fallback`) if the protection does not work

Each cut goes to section 8 with one sentence for the 1-pager.

**Technical notes:**
- `global.json` and the `aspnet:9.0` image, because Pomelo 9.0.0 does not support EF Core 10.
- Enums as `VARCHAR`.
- `CharSet=utf8mb4` in the connection string and in `CREATE DATABASE`.
- SPA served from `wwwroot`, without CORS.

---

## 10. Assumptions to verify at the first import

The fill-rate report from item 1 in section 4 checks all of them.

| # | Assumption | Decision threshold |
|---|---|---|
| 1 | Gdynia and Sopot have fields like Gdańsk | field < 60% in a city → “no data” in the UI instead of a filter for that field |
| 2 | Districts from `reverseGeocoding` are consistent | > 10% of labels outside the list of administrative districts → mapping dictionary |
| 3 | `Rent = 0` means no data | always `null`; ≥ 30% of secondary-market listings with zero → a sentence in the 1-pager |
| 4 | Parking with a price in descriptions | < 15% of listings → cut mini-W2, a sentence remains in section 8 |
| 5 | Sopot has n ≥ 10 | fewer → comparison with Tricity with an annotation |
| 6 | Duplicates across agencies are rare | > 5 % of listings in candidate pairs → a sentence with the number in the 1-pager and grouping first in “with more time” |
| 7 | “Cheap” does not systematically exclude the primary market | primary-market share in the top 20 by `cheap` far below its share in the data → failure mode stated with the number |

---

## 11. For the 1-pager: assumption, metric, failure mode

- **Key assumption:** the facts buyers ask about first (price, area, rooms, floor, maintenance fee, ownership, finish condition) are already otodom fields; the unique value of the description is paid extras such as parking. Verified at import: fill rate per field and the share of descriptions with a priced parking space (section 10). If it fails, extraction from the description has to grow beyond two facts.
- **Success metric:** share of searches (chat and filters) in which the user opens a listing from the top five **and** clicks “Zobacz na otodom”, a proxy for contacting the seller, i.e. a qualified lead. Diagnostics: share of chat searches corrected with a chip (the interpretation was close but not exact). Guardrails: fallback share and zero-result share. Offline, before any traffic: `SearchIntent` match on 10 test queries and the share of extracted facts rejected by the text anchor. The prototype does not collect events; instrumentation is “with more time”.
- **Failure mode:** “nice” is mapped to finish condition, and primary-market listings are almost always “to be completed”, so the ranking pushes developers' listings down; “cheap” computed on a single city median may do the same (checked in the import report). Business consequence: lower visibility for the segment that pays for listings, which a real portal would have to correct (median per city × market, a better “nice” signal).

---

## 12. Data model

The schema (DDL, types, constraints) is defined once, in [design.md](design.md) §3.2: `raw_listings` (local only), `listings` and `listing_facts`.
