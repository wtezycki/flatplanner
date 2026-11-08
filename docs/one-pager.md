# Flatplanner: one-pager

Flatplanner lets a buyer describe their situation in their own words and shows which flats fit, partly fit, or leave the question unanswered, each verdict backed by a quote or a nearby place. Flat sales in Gdańsk, Gdynia and Sopot: 197 listings from otodom.pl, fetched once. Figures: [ingest-report.md](ingest-report.md) and eval runs of 2026-11-08.

## What data, and why

I chose otodom because it holds the Tricity sale listings, and its typed fields let me measure the gaps instead of guessing them. District, floor, finish condition and build year are filled in 92–100% of listings, so filters use them directly. Sellers' contact details are stripped on import. Before building anything I went through listings by hand and asked of each: what would I still need to know before calling? The fields leave out what decides a purchase: the maintenance fee is in 40% of listings, a lift in 66%, the surroundings in none. That gap is why every listing gets a **dossier**: a summary, quoted buyer risks and places within 1.5 km.

## Unstructured and low-quality data: how I approached it

Every rule below comes from listings I read by hand and answers a number from the import report.

- **Silence is not "no".** `Rent = 0` is 68% of primary listings: the developer did not say. I store "no data", so a buyer never sees a 0 zł fee and underestimates the monthly cost.
- **Flag, don't fix quietly.** 4 price outliers stay in, flagged. 26% of listings sit in a candidate duplicate pair, but the samples look like units of one development, so I merge nothing; the cost is that one development can take several top spots.
- **Approximate is labelled.** 83 of 197 locations are approximate; the listing says so, and the card shows nearby stops and parks only for exact ones.
- **A description is a claim, not a fact.** A risk is kept only if the description actually says it; 2 of 137 failed that check and were dropped.

## Where and why AI

- **Import, the dossier:** for each listing an AI writes, once, a short summary, the buyer risks the description admits to (extra costs, legal status, noise, data that contradicts the text) and what is nearby from OpenStreetMap. **Why:** fields cannot answer "is it quiet?" or "will parking cost extra?", and word search fails: `cisza` (quiet) appears in 50 descriptions, 13 of them only as silent lifts. **Effect:** all 197 listings have one, so every listing shows what is nearby and what its description admits to, even where the seller filled no field; 135 risks kept, 2 dropped as unsupported.
- **Runtime, the chat:** the buyer writes one message instead of setting filters. The portal pulls out hard limits (city, price, area) and the needs behind them ("night shifts", "a dog", "a mother in a wheelchair"), then rates every listing against each need: fits, partly, does not fit, or the listing does not say. Every rating shows its evidence (a quote, a place and its distance, or a field); one without evidence becomes "the listing does not say". **Why:** buyers think in needs, not in fields, and no filter catalogue covers every life situation. **Effect:** each buyer gets the listings grouped by how well they fit their own needs, with the reason next to each, instead of one list for everyone. Needs read as expected on 15 of 15 test queries, and 8 of 10 expected ratings matched. A question costs under $0.01 with spend capped; if the model is down, keyword search still answers.

## Key assumption

**Buyers find the flats worth a call faster, and contact more of the right sellers, when results are matched to their situation and the portal answers the questions a listing leaves open.** The data gap is measured (above); the buyer behaviour is not. I came to this as a buyer: I am looking for a flat now, and the hard part is missing data and not knowing the area (is it quiet, what is within walking distance). That is one buyer, so it is where the hypothesis comes from, not proof. The business link is a hypothesis too: a buyer unsure a flat fits does not call, sellers rarely fill the gaps, and a better-qualified lead is what advertisers pay for. It fails if buyers act just as fast and as well on raw listings and filters; the A/B test below tests exactly that.

## Success metric

**Share of searches that end with a click on "Zobacz na otodom" from the first five results, chat against the filter list in an A/B test.** That click is the closest the prototype gets to a buyer contacting a seller; a real portal would count the contacts themselves. Also watched: time to that first click (is it faster?) and, so the gain is not bought elsewhere, the share of "unknown", searches with no results and cost per search. The prototype records no events yet, so this is the test I would run, not a result.

## Failure mode

**A confident wrong "fits".** Code proves a quoted sentence exists, not that it was read correctly, and 83 of 197 locations are approximate, so a distance a verdict cites can be off. A buyer told a flat suits a wheelchair travels to a viewing for nothing and stops trusting every verdict. Evidence next to every verdict lets a buyer check it before calling, and caution has a price: 94 of 216 verdicts (44%) say the listing does not answer. The 8/10 rests on three listings I read myself: a smoke test, not a measurement. How often "fits" is wrong is not measured yet; that is on the list below.

## With more time, by value

1. **Event tracking and an A/B test on real traffic:** until then, whether this raises lead value is a guess.
2. **Check verdicts against answers verified by hand, to know how often "fits" is wrong:** trust in verdicts is the product.
3. **Dossiers for new listings as they arrive, with the cost per listing known:** at portal scale the data must stay fresh.
4. **Show sellers which buyer questions their listing leaves open:** fewer "unknown" verdicts for buyers, and listing quality becomes something to sell.

**Beyond flats.** Nothing here is specific to flats: most search products hold more in text and reviews than in fields, so a need in the user's own words can be judged against each result, with evidence or "unknown". On ZnanyLekarz, a patient could ask for a doctor "calm with an anxious child" or "a clinic without stairs" and get each match backed by a quote from the profile or reviews.
