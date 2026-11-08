# Flatplanner

A flat listings portal for the Tricity (Gdańsk, Gdynia, Sopot), sales only, where the buyer describes their life in their own words and the portal says which flats fit, partly fit, or leave the question open, each verdict backed by evidence.

## What this is and why

**The problem.** A listing's fields do not answer what decides a purchase. In the 197 listings used here the maintenance fee is filled in for 40%, the lift for 66%, and the surroundings for none. Buyers think in needs ("I work nights and sleep in the day", "my mother uses a wheelchair"), not in fields, and no filter catalogue covers every life situation.

**What the product does.**
- **Once, at import:** every listing gets a *dossier*: a short summary, the buyer risks its description admits to (extra costs, legal status, noise, fields that contradict the text), each backed by a quote, and the places within 1.5 km from OpenStreetMap.
- **At question time:** the buyer writes one message. The portal pulls out hard limits (city, price, area) and the needs behind them, then rates listings against each need as fits, partly, does not fit, or the listing does not say. Every rating shows its evidence: a quote, a nearby place with its distance, or a field. A rating without evidence becomes "the listing does not say".
- Listings come back grouped by how well they fit the buyer, with the reason next to each. If the model is down, keyword search still answers.

**The goal.** This is a prototype built for the Docplanner recruitment task. It is meant to show product reasoning: which gap in the data matters, where AI earns its place, what the key assumption is, how it would be measured and how it can fail. The business case is that a buyer who sees listings judged against their own situation reaches a seller better qualified, and a better-qualified lead is what a portal's advertisers pay for. That link is a hypothesis, not a result: the prototype records no events.

**Data.** 197 flat-sale listings fetched once from otodom.pl (Gdańsk, Gdynia, Sopot). Demo use only. Sellers' contact details are stripped on import, the data is not committed and not indexed; every listing links back to its otodom page.

## Where to read more

| Document | Contents |
|---|---|
| [One-pager (English)](docs/one-pager.md) | data choices, where and why AI, key assumption, success metric, failure mode, what comes next |
| [One-pager (Polski)](docs/one-pager-pl.md) | the same, in Polish |
| [docs/product-scope.md](docs/product-scope.md) | personas, features and acceptance criteria, deliberate cuts |
| [docs/design.md](docs/design.md) | implementation design: data model, API contract, LLM pipeline, tests, deploy |
| [docs/ui-spec.md](docs/ui-spec.md) | UI layout, components, states, Polish interface texts |
| [docs/ingest-report.md](docs/ingest-report.md) | generated import report: fill rates, flags, rent gaps |
| [docs/README.md](docs/README.md) | index of the documents above |

The interface and every answer are in Polish; the documentation is in English except the Polish one-pager.

## Two-minute demo

Run the app first (see [Run it](#run-it)); links below use `http://localhost:8080`. Listing numbers are local ids of the demo import, so they match the dump described under [Data](#data).

1. **Scenario A, filters and a list:** [`/listings?cities=gdansk&areaMin=40&areaMax=80`](http://localhost:8080/listings?cities=gdansk&areaMin=40&areaMax=80). Narrow to a district, sort by price per m², open a listing, go back: the filters stay, because they live in the URL.
2. **A listing whose description holds what no field has:** [listing 70](http://localhost:8080/listings?listing=70), Ujeścisko-Łostowice, 680 000 zł. The description says nobody walks above the flat and a primary school is two minutes away; parking and storage are an optional extra of 40 000 zł, and a bank mortgage is to be repaid from the price. The card shows these as risks with their quotes.
3. **A listing whose field contradicts its description:** [listing 97](http://localhost:8080/listings?listing=97), Wrzeszcz Górny. otodom says 3 rooms, the description says the land register shows 2 rooms and the kitchen was turned into an office.
4. **A needs-based question** on [`/`](http://localhost:8080/), paste:
   > mam psa i pracuję na nocki, 2 pokoje w Gdańsku do 700 tys.

   First the portal shows how it understood the question (hard limits and needs, a few seconds), then the listings grouped by fit, each verdict with its evidence. Steps 4–6 need `CHAT_MODE=llm` and an `OPENAI_API_KEY` (see [Configuration](#configuration)); with `CHAT_MODE=fallback` the portal shows only the notice "AI chwilowo niedostępne" and the results of the basic filters, with no needs and no verdicts.
5. **A question from a different life:** paste `Jestem z Ukrainy, szukam mieszkania w starej kamienicy z osobnym pokojem na biuro` and open [listing 166](http://localhost:8080/listings?listing=166), Grabówek: the description says foreigners can buy without the MSWiA permit, and `ownership` confirms it.
6. **Outside the data:** paste `2 pokoje w Krakowie`. The portal says it has listings only from Gdańsk, Gdynia and Sopot and shows no result from Kraków. The task's Kraków example therefore runs on Gdańsk.

The chat thread is kept in the browser's query cache for five minutes and is not in the URL, so a question cannot be shared as a link.

## How it works

1. **Import (local only, never deployed):** `scrape` and `normalize` produce `listings` from otodom's raw data; `nearby` fetches places from OpenStreetMap by code; `export-dossiers` writes the descriptions to a file; a coding agent writes the summaries and risks from the versioned prompt [`prompts/listing-dossier.v2.md`](prompts/listing-dossier.v2.md), at no API cost; `import-dossiers` validates them. A risk survives only if its quote is in the description (after normalisation) and an amount is in the quote; invalid risks are dropped and counted.
2. **Interpret:** one model call turns the message into hard filters and up to four needs, validated against a strict schema. On failure the keyword fallback answers, never a 5xx.
3. **Rank, then judge one window:** code ranks the candidates (below), and the model judges only the 20 most promising per call, in 2 parallel batches; "Sprawdź kolejne ogłoszenia" asks for the next window. Cost and wait therefore do not grow with the number of listings.
4. **Verify:** the model judges, code verifies. Quotes must be substrings of the description, places must be real entries of the listing's nearby list, fields must exist. Anything unverifiable is downgraded to "the listing does not say". Hard filters, grouping and distances are plain C#.

Stack: .NET 9 Minimal API with EF Core 9 (Pomelo MySQL), React + Vite + TypeScript, MySQL 8.4, OpenAI (`gpt-5.6-luna` by default). Why C#: the stack is allowed by the task and matches the team's, and it keeps the verification code (anchor checks, distances, grouping) strongly typed and testable without the model.

### Ranking

`CandidateRanker` scores every listing per need and sums over needs; this decides which 20 the model reads, not what it concludes.

- For each place the need plan asks for (a school "near", a main road "far"), the nearest matching entry of the listing's `nearby_json` gives `1 − d/1500` for *near* and `d/1500` for *far* (metres). For *far* the worst present tag counts, because the nearest noise source decides.
- A description sentence, or a nearby place's name, containing a keyword of the need adds 0.5.
- No entry within 1 500 m scores 0: an absent place is not citable evidence.
- Ties go to the newest listing. A listing with no context for any need is not sent to the model and is shown as "the listing does not say".

Details: [design.md §16.9](docs/design.md). There is no price or condition weighting: after the AI-first pivot "cheap" and "nice" are needs like any other, judged with evidence.

### Failure mode

A confident wrong "fits". Code proves that a quoted sentence exists, not that the model read it correctly, and 83 of 197 locations are approximate, so a cited distance can be off. Mitigations: evidence next to every verdict so the buyer can check before calling, and caution as the default (44% of verdicts in the eval say the listing does not answer). How often "fits" is wrong is not measured yet; see the [one-pager](docs/one-pager.md).

## Run it

Requirements: Docker with Compose, the .NET SDK from [`global.json`](global.json) (9.0.203, roll-forward to the latest feature band), Node 22.

### Configuration

```bash
cp .env.example .env
```

Edit `.env`: set `MYSQL_ROOT_PASSWORD` and `MYSQL_PASSWORD`, and keep the password inside `ConnectionStrings__Default` equal to `MYSQL_PASSWORD`. Settings that matter:

| Variable | Meaning |
|---|---|
| `CHAT_MODE` | `fallback` (default): no model call, keyword parsing only. `llm`: the full `ask` pipeline; needs `OPENAI_API_KEY`, and a missing key stops the API at startup. |
| `OPENAI_API_KEY` | only for `CHAT_MODE=llm` and for `eval-intents` / `eval-judge`. Never commit it. |
| `LLM_MODEL`, `LLM_JUDGE_MODEL` | interpretation and judge models. |
| `CHAT_MAX_LENGTH`, `CHAT_TIMEOUT_SECONDS` | message length limit (300) and model timeout (5 s). |

The chat endpoint is limited to 5 requests per minute per IP.

### Start the app in containers

```bash
docker compose up -d --build
```

The app is at http://localhost:8080 and `/api/health` returns `db: ok`. The API creates the schema at startup; the database is empty until you load the data.

### Data

Listings arrive as a dump. `data/` is not in git, so the dump is not in the repository: ask the author for `flatplanner-data.sql` and put it in `data/dumps/`, then load it into the running stack:

```bash
set -a; source .env; set +a
docker compose exec -T -e MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysql mysql -uroot --default-character-set=utf8mb4 flatplanner < data/dumps/flatplanner-data.sql
```

The dump holds `listings` and `listing_facts`. Check: `SELECT COUNT(*) FROM listings;` returns 197.

To rebuild the data instead of loading a dump, run the Ingest commands in order (the listings fetched today will differ from the demo set):

```bash
dotnet run --project src/Flatplanner.Ingest -- scrape
dotnet run --project src/Flatplanner.Ingest -- normalize
dotnet run --project src/Flatplanner.Ingest -- nearby
dotnet run --project src/Flatplanner.Ingest -- export-dossiers
```

Then have a coding agent write `data/agent/dossiers.jsonl` from `data/agent/listings.jsonl` following `prompts/listing-dossier.v2.md`, and finish with `import-dossiers` and `report`. The dossiers of the demo set were produced this way with prompt `listing-dossier.v2`; no API cost is involved.

### Development

```bash
docker compose up -d mysql
dotnet tool restore
set -a; source .env; set +a
PROMPTS_DIR=../../prompts dotnet run --project src/Flatplanner.Api --urls http://localhost:5080
```
```bash
cd web && npm ci && npm run dev
```

The Vite dev server is at http://localhost:5173 and proxies `/api` to the API on port 5080. `PROMPTS_DIR` is needed because the API resolves it relative to its project folder.

### Checks

```bash
dotnet format Flatplanner.sln --verify-no-changes
dotnet build Flatplanner.sln
dotnet test Flatplanner.sln
```
```bash
cd web && npm run lint && npm run build
```

CI (`.github/workflows/ci.yml`) runs the same checks on every pull request. The tests use no network, database or model.

### Evaluation (paid)

`eval-intents` runs the 10 questions of [`tests/search-queries.json`](tests/search-queries.json) through the real model and the keyword fallback; `eval-judge` runs the judge cases of `tests/judge-cases.json` against a running API with `CHAT_MODE=llm`. Both call the OpenAI API and write their reports to `data/eval/`.

```bash
dotnet run --project src/Flatplanner.Ingest -- eval-intents
```

## Layout

| Path | Contents |
|---|---|
| `src/Flatplanner.Core` | entities, `DbContext`, migrations, search intent and its normaliser, evidence anchors, LLM client |
| `src/Flatplanner.Api` | Minimal API endpoints, list queries, ranking, judging, SPA hosting |
| `src/Flatplanner.Ingest` | local-only console app: scrape, normalize, nearby, dossier export and import, report, evals |
| `tests/` | xUnit tests and the evaluation query sets |
| `web/` | React SPA, built into the API's `wwwroot` in the container |
| `prompts/` | versioned LLM prompts; a changed prompt is a new file |
| `docs/` | product scope, design, UI spec, one-pagers, import report |

## Notes

- .NET 9 support ends on 2026-11-10. Irrelevant for a prototype; moving to .NET 10 is blocked by Pomelo, which has no release for EF Core 10 yet ([design.md §14.2](docs/design.md)).
- Descriptions are always rendered as text, never as HTML.
- Secrets live only in the local `.env` (git-ignored) and in the hosting provider's variables.
