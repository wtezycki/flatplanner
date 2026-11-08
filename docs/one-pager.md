# Flatplanner: one-pager

Flat sales in Gdańsk, Gdynia and Sopot, 197 listings from otodom.pl fetched once. C#/.NET instead of TS/Node: it is the stack of the team that will read the code. Numbers: [ingest-report.md](ingest-report.md) and the eval commands, 2026-11-08.

## What data, and why

- **otodom fields** (price, area, rooms, floor, district, ownership, finish condition, fee, lift): they feed the filters and the classic list.
- **Listing dossier** built once at import: an AI summary, buyer risks with a quote each (`extra_cost`, `legal`, `occupied`, `noise`, `field_mismatch`) and places within 1.5 km from OpenStreetMap. What a buyer cares about is not in the fields: `kredyt` is in 81 descriptions, ~77 of them agency boilerplate; `cisza` in 50, 13 only as "cichobieżne windy". Keyword search returns noise, fields return nothing.

## Unstructured and low-quality data

- `Rent = 0` is "no data", never zero (68% of primary-market listings); a missing lift or balcony is "no data", never `false`; an unknown source value becomes `NULL` plus a flag; 4 price outliers are flagged, not removed.
- 26% of listings (52 of 197) sit in a candidate duplicate pair; nothing is merged, the number is reported.
- Every risk must quote the description and an amount must appear in the quote: 2 of 137 risks were dropped. 83 of 197 locations are approximate.

## Where and why AI

- **At import** a coding agent writes the dossier from a versioned prompt (no API cost); code validates it like an API response.
- **At runtime** one chat field: the model turns the message into hard filters plus free-form needs, then judges every listing against each need (fits / partly / does not fit / unknown). **The model judges, code verifies:** a verdict must cite a quote, a place with a distance or a field, and code downgrades anything it cannot find to "unknown". Filters, grouping and distances are plain code; if the model fails, a keyword fallback answers. A question costs about $0.01.

## Key assumption

A buyer's real criteria (a night shift, a dog, a wheelchair, a school to walk to) are in the description and the surroundings, so a model that reads both with checkable evidence beats filters.

## Success metric

Share of searches where the user opens a listing from the top five and clicks "Zobacz na otodom", a proxy for a qualified lead (not collected by the prototype). Offline today: `eval-intents` 15/15 on 15 test queries (the fallback behaves as expected on all 15); `eval-judge` 8/10 expected verdicts on three demo listings.

## Failure mode

The verdict is only as good as the dossier: code proves a quote exists, not that the model read it correctly. The two `eval-judge` misses are "unknown" where "no" was expected and "unknown" for "quiet by day", so the model errs towards silence, but 94 of 216 verdicts (44%) are "unknown". The expected verdicts come from three descriptions read by hand, so 8/10 is a smoke signal, not a precision figure.

## With more time

A hand-labelled set to measure judge precision and recall; event tracking for the metric; grouping the same unit listed by several agencies; walking time and a district map; continuous crawling with an alert on a change of otodom's `__NEXT_DATA__`.
