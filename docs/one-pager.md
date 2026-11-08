# Flatplanner: one-pager

Flat sales in Gdańsk, Gdynia and Sopot: 197 listings from otodom.pl, fetched once. C#/.NET. Figures: [ingest-report.md](ingest-report.md), eval runs of 2026-11-08.

## What data, and why

I chose otodom for easy-to-use data: a large public catalogue, typed fields, one consistent model. Fill rate on my 197 listings: district 100%, floor 92%, finish condition 96%, build year 96%, so filters use them directly, no guessing. But the fields leave out what decides a purchase: the maintenance fee is present in 40% of listings, a lift in 66%, and nothing describes the surroundings. That gap is why I added a **listing dossier**: an AI summary, buyer risks (each quoted) and places within 1.5 km from OpenStreetMap.

## Unstructured and low-quality data: how I approached it

- **Measure first.** The import report came before the filters, so each rule answers a number.
- **Silence is not "no".** `Rent = 0` is 68% of primary listings: the developer did not say, so I store "no data", never zero.
- **Flag, don't fix quietly.** 4 price outliers stay in, flagged. 26% of listings (52 of 197) sit in a candidate duplicate pair, but the sample pairs look like units of one development, so I report and merge nothing.
- **A description is a claim, not a fact.** Every risk drawn from text must quote it, and code checks the quote and any amount (2 of 137 risks dropped).

## Where and why AI

- **Import:** an agent writes the dossier once from a versioned prompt: no API bill.
- **Runtime:** one chat field. The model turns the message into filters plus free-form needs and judges every listing per need (fits / partly / no / unknown). Each verdict must cite a quote, a place or a field; code downgrades anything it cannot find to "unknown". A question costs under $0.01; a rate limit and a kill switch bound spend.

## Key assumption

**Listings alone do not let a buyer decide, so the portal must create the missing data itself** from surroundings and description instead of waiting for sellers. A buyer unsure a flat fits does not call, sellers rarely edit listings, portal-made data cannot be copied by a scrape, and a better-qualified lead is what advertisers pay for. Evidence: the fee is missing in ~60% of listings, `kredyt` is in 81 descriptions (~77 boilerplate), and none mentions a synagogue or Orthodox church, which OpenStreetMap knows. It fails if buyers act just as well on the raw listing.

## Success metric

**Qualified-lead rate: share of AI-search sessions ending in a click-out ("Zobacz na otodom") from a top-five result**, against the filter list as baseline. It is the closest in-product proxy for a lead. Guardrails: share of "unknown", zero-result searches, cost per session. An A/B test would measure it; a higher rate raises what a listing is worth to advertisers. The prototype collects no events; offline proxies: `eval-intents` 15/15, `eval-judge` 8/10.

## Failure mode

The verdict is only as good as the dossier: code proves a quote exists, not that the model read it correctly. 94 of 216 verdicts (44%) are "unknown": on thin listings the product says "I don't know" too often, while a confident wrong answer would cost trust faster. The 8/10 rests on three hand-read descriptions.

## With more time, by value

1. **Event tracking and the A/B test:** without them nobody knows if this earns money.
2. **A labelled set to measure judge precision:** trust in verdicts is the product.
3. **Dossiers for new listings, cost measured:** freshness at portal scale.
4. **Show sellers what their listing leaves unanswered:** listing quality becomes something to sell.
