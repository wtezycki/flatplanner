# Flatplanner: one-pager

Flat sales in Gdańsk, Gdynia and Sopot: 197 listings from otodom.pl, fetched once. C#/.NET. Figures: [ingest-report.md](ingest-report.md) and eval runs, 2026-11-08.

## What data, and why

I chose otodom for its easy-to-use data: typed fields in one consistent model. Fill rate on 197 listings: district 100%, floor 92%, finish condition 96%, so filters use them directly. Before building anything I went through listings by hand and asked of each: what would I still need to know before calling, and what would add value to it? The fields leave out what decides a purchase: the maintenance fee is in 40% of listings, a lift in 66%, the surroundings in none. That gap is why I added a **listing dossier**: an AI summary, buyer risks (each quoted) and places within 1.5 km from OpenStreetMap.

## Unstructured and low-quality data: how I approached it

- **Silence is not "no".** `Rent = 0` is 68% of primary listings: the developer did not say, so I store "no data".
- **Flag, don't fix quietly.** 4 price outliers stay in, flagged. 26% of listings (52 of 197) sit in a candidate duplicate pair, but they look like units of one development, so I merge nothing.
- **A description is a claim, not a fact.** Every risk drawn from text must quote it; code checks the quote and amount (2 of 137 risks dropped).

## Where and why AI

- **Import:** an agent writes the dossier once from a versioned prompt, no API bill.
- **Runtime:** one chat field; the model turns the message into filters plus needs and judges every listing per need. Each verdict must cite a quote, a place or a field; code downgrades anything it cannot find to "unknown". A question costs under $0.01; a rate limit and a kill switch cap spend.

## Key assumption

**Listings alone do not let a buyer decide, so the portal must create the missing data itself** from surroundings and description instead of waiting for sellers. I know this first-hand: I am looking for a flat to buy right now, and the hard part is the data, both what is missing and the lack of context for someone who does not know the area (is it quiet, what is within walking distance). A buyer unsure a flat fits does not call, sellers rarely edit listings, and better-qualified leads are what advertisers pay for. Evidence: the fee is missing in ~60% of listings, `kredyt` is in 81 descriptions (~77 boilerplate), and no description mentions a synagogue or Orthodox church, which OpenStreetMap knows. It fails if buyers act just as well on the raw listing.

## Success metric

**Qualified-lead rate: share of AI-search sessions ending in a click-out ("Zobacz na otodom") from a top-five result**, against the filter list as baseline. The closest in-product proxy for a lead. Guardrails: share of "unknown", zero-result searches, cost per session. An A/B test would measure it; a higher rate raises a listing's worth to advertisers. The prototype collects no events; offline: `eval-intents` 15/15, `eval-judge` 8/10.

## Failure mode

The verdict is only as good as the dossier: code proves a quote exists, not that the model read it correctly. 94 of 216 verdicts (44%) are "unknown", so on thin listings the product says "I don't know" too often, though a confident wrong answer would cost more trust.

## With more time, by value

1. **Event tracking and the A/B test:** without them nobody knows if it earns.
2. **A labelled set to measure judge precision:** trust in verdicts is the product.
3. **Dossiers for new listings:** freshness at portal scale.
4. **Show sellers what their listing leaves unanswered:** listing quality becomes something to sell.
