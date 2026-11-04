---
name: architecture-check
description: Check structural conformance with docs/design.md §1-2 and §8-9 - project boundaries and dependency direction (Core, Api, Ingest, Tests, web), where logic lives, Program.cs middleware order, DTO and contract placement, and the local-ingest/production data flow. Use for changes that add projects, folders, files in new places, project references, packages, middleware or Dockerfile/compose changes.
argument-hint: "[diff-range]"
---

# Architecture check

Target: `$ARGUMENTS` (default: `git diff main...HEAD --stat` and the `.csproj`, `Program.cs`, `Dockerfile`, `docker-compose.yml` diffs).

## Procedure

1. List new or moved files and every changed `.csproj`, `Program.cs`, `Dockerfile`, `docker-compose.yml`, `web/vite.config.ts`.
2. Compare each against the design tree (§2) and the rules below.
3. Report deviations; do not fix unless asked.

## Checklist

**Projects and dependencies (§2)**
- Exactly `Flatplanner.Core` (classlib), `Flatplanner.Api` (web), `Flatplanner.Ingest` (console, `Host.CreateApplicationBuilder`), `Flatplanner.Tests` (xUnit).
- References: Api → Core; Ingest → Core; Tests → Core, Ingest, Api. Core references no other project. Api never references Ingest, Ingest never references Api.
- Packages per project as in §2: `EntityFrameworkCore.Design` only in Api; `Microsoft.Extensions.Hosting` only in Ingest; Core has Pomelo, EF Core, OpenAI, Options; `<NoWarn>OPENAI001</NoWarn>` only in Core.

**Where code lives (§2 table, §7)**
- Core: `FlatplannerDbContext`, entities, migrations, `Enums.cs`, `FlatplannerOptions`, `SearchMeta`/`SearchMetaLoader`, `SearchIntent` + normalizer + schema + `KeywordRules` + `IntentFallbackParser` + `IntentInterpreter`, `TextFolding`, `AnchorValidator`, `LlmJsonClient`. Anything shared by Api and Ingest belongs here.
- Api: `Endpoints/*.cs` (Minimal API only), list query execution, ranking, "what blocks", `ChatInterpreter`, `InterpretationBuilder`, `MetaService`, `Provenance.For`, `ApiJson.Options`, response DTOs, SPA hosting.
- Ingest: `Scraping/`, `Normalization/`, `Enrichment/`, `Reporting/`, `eval-intents`.
- Model translates, code decides: filters, normalization, tolerances, ranking, medians, blockers are deterministic C# outside any LLM call; LLM output flows through `SearchIntentNormalizer` or `AnchorValidator` before use.
- Real price computed in Api on read, not stored (§5.3). Provenance is a per-field constant, not a column (§3.4).

**Api pipeline (§6, §8, §11.3)**
- Middleware order: `UseForwardedHeaders` → `UseExceptionHandler` → `UseRateLimiter` → `UseDefaultFiles` → `UseStaticFiles` → endpoints → `app.Map("/api/{**rest}", …)` 404 → `MapFallbackToFile("index.html")`. `UseResponseCompression` only if §6 check required it, before `UseStaticFiles`.
- No CORS, no `UseHttpsRedirection`, no auth.
- `ConfigureHttpJsonOptions` uses the single `ApiJson.Options`.
- Rate limiter: global `/api/*` 120/min per IP, `"chat"` 5/min on `/api/search/interpret`, 429 with `Retry-After`.
- Migrations at startup only when `DB_MIGRATE_ON_STARTUP=true`.

**Frontend (§9)**
- Files as listed in §9 (`api.ts`, `intent.ts`, `format.ts`, `pages/`, `components/`, single `styles.css`).
- URL is the only state source; DTO types only in `api.ts`; labels of fields/flags/fact keys in `format.ts`, enum labels from `/api/meta`.

**Data flow and deploy (§1, §11)**
- Ingest runs locally only; never in the Docker image. Dockerfile stages `web` → `build` (Api only) → `runtime` (non-root, `prompts/` copied, port 8080).
- Production data only via `mysqldump` of `listings` and `listing_facts` (§11.4); no scraping or LLM enrichment from the API.
- Prompts read from `PROMPTS_DIR`; config only from environment variables (§10); `appsettings.json` holds only `Logging` and `AllowedHosts`.

## Output format

```
Architecture check: <target>
Verdict: CONFORMS | DEVIATES (<n>)

1. [blocker|major|minor] <path>[:<line>] <deviation>
   Rule: design §<x> <rule in one line>
   Fix: <concrete move/removal>
...
Checked: <areas with no deviations>
```

`blocker` = breaks dependency direction, production data flow or middleware order; `major` = logic in the wrong project; `minor` = file placement or naming.
