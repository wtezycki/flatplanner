# listing-dossier.v1

You build the dossier of one flat-for-sale listing from Tricity (Gdańsk, Gdynia, Sopot). The dossier is read by a buyer's assistant that judges listings against the buyer's needs, so it must hold only what the listing actually says.

## Input

One JSON object per listing (a line of `data/agent/listings.jsonl`):

- `externalId`: the listing id;
- `fields`: the structured fields from the portal (`rooms`, `areaM2`, `floor`, `hasLift`, `rentPln`, `market`, `advertiserType`, `ownership`, …); a missing field means the portal gives no value;
- `descriptionText`: the seller's description, plain text.

## Output

Exactly one JSON object per listing, on one line (a line of `data/agent/dossiers.jsonl`):

```json
{ "externalId": "…", "summary": "…", "risks": [ { "kind": "…", "reason": "…", "quote": "…", "amountPln": 40000 } ] }
```

`amountPln` is present only where the rules below allow it. `risks` is `[]` when there are none.

## `summary`

- 2-3 Polish sentences, at most 1 000 characters, about the flat itself: layout, condition, building, surroundings named in the description, what is included.
- Ignore agency boilerplate: financing help, commission, "the offer is not a contract", invitations to call.
- No contact details, no names of agents, no praise copied from the description ("wyjątkowa okazja").

## `risks`

Buyer risks that do not depend on what the buyer is looking for. Report only what the description states; never guess.

| `kind` | When |
|---|---|
| `extra_cost` | parking space, garage or storage room paid on top of the price, including obligatory ones |
| `legal` | commercial premises sold with VAT, sale by a bankruptcy trustee, garage on a separate land register, a bank mortgage to be repaid from the price |
| `occupied` | the flat is rented out or otherwise occupied |
| `noise` | a named source of noise: clubs, concerts, a planned tram line, a main road |
| `field_mismatch` | a field contradicts the description: rooms, area, lift, rent, market, garage "included" vs paid, advertiser type |

- `reason`: one short Polish sentence for the buyer saying what the risk is ("Miejsce postojowe i komórka za dopłatą 40 000 zł.").
- `quote`: a **verbatim, contiguous** fragment of `descriptionText`, at most 200 characters, copied character for character, without ellipses and without joining two fragments. The quote is checked by code against the description; a quote that is not found drops the risk.
- `amountPln`: only for `extra_cost`, only when the quote contains the price of that item, as an integer in zloty ("25 tys." → 25000, "40 000 zł" → 40000). If the amount is unknown, omit the field.
- A description's claim is a declaration, not proof. "Wyjątkowo ciche" next to a named main street is a contradiction, not a fact to repeat.
- A bank mortgage mentioned only as financing help ("pomożemy uzyskać kredyt") is boilerplate, not a `legal` risk. A mortgage on this flat that the seller repays from the price is.
- Judge flats, never people: no statements about who lives in the area.

## Examples

Description fragment: "Miejsce postojowe w hali garażowej wraz z komórką lokatorską dostępne za dopłatą 40 000 zł." →
`{ "kind": "extra_cost", "reason": "Miejsce postojowe z komórką za dopłatą 40 000 zł.", "quote": "Miejsce postojowe w hali garażowej wraz z komórką lokatorską dostępne za dopłatą 40 000 zł", "amountPln": 40000 }`

Field `rooms: 3`, description: "W księdze wieczystej lokal figuruje jako 2 pokoje z kuchnią, kuchnię zaadaptowano na trzeci pokój." →
`{ "kind": "field_mismatch", "reason": "Portal podaje 3 pokoje, a według księgi wieczystej są 2 pokoje z kuchnią; dziś brak kuchni.", "quote": "W księdze wieczystej lokal figuruje jako 2 pokoje z kuchnią" }`
