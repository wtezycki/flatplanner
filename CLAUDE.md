# CLAUDE.md

Operational rules for coding agents in this repository. The specification lives in `docs/`; this file says how to work with it.

## Project

Flatplanner: a simplified flat listings portal for Tricity (Gdańsk, Gdynia, Sopot), sales only, ~150 listings imported once from otodom.pl.
Stack: .NET 9 Minimal API + EF Core 9 (Pomelo MySQL), React + Vite + TypeScript, MySQL 8.4, OpenAI at the core of the product (AI-first pivot, design §16): listing dossier built once locally (nearby places from OSM by code; summary and buyer risks written by a coding agent from a versioned prompt and validated by `import-dossiers`, no API cost), and at runtime the `ask` pipeline that judges every listing against the user's needs, with evidence verified by code and deterministic fallbacks. Deployed on Railway.
Constraint from the task: avoid overengineering; limited time. Scope is fixed; quality of reasoning beats feature count.

## Layout (design §2)

- `src/Flatplanner.Core`: `DbContext`, entities, migrations, enum constants, options, `SearchIntent` + normalizer + schema + fallback parser, `TextFolding`, `AnchorValidator`, `LlmJsonClient`.
- `src/Flatplanner.Api`: Minimal API endpoints (`Endpoints/`), list queries, ranking, interpretation, chat, SPA hosting.
- `src/Flatplanner.Ingest`: local-only console app (scrape, normalize, nearby, export-dossiers, import-dossiers, report, eval-intents). Never deployed.
- `tests/Flatplanner.Tests`: xUnit; `tests/search-queries.json`: the 10 chat queries.
- `web/`: React SPA, built into the API's `wwwroot` in the container. `prompts/`: versioned LLM prompts.
- Data reaches production only as a `mysqldump` of `listings` and `listing_facts` (§11.4).

## Sources of truth (in this order)

1. `docs/product-scope.md`: what is built, acceptance criteria, cut order (§8, §9).
2. `docs/design.md`: how it is built. Wins over `ui-spec.md` on everything except user-facing text.
3. `docs/ui-spec.md`: UI layout, components, states. §7 is the only source of user-facing Polish strings (frontend and API).

`docs/scraping-plan.md` is background; where it differs from `design.md`, the design wins (design §14.1).
Do not import the docs into context wholesale: `design.md` is large. Read the sections the current item references.

## How to work

- Work on exactly one item at a time, in the order of `design.md` §16.5 (which supersedes §13 where they differ), following the cycle below (the `implement-item` skill runs it).
- Read only the design sections listed in the item's "What is produced" column, plus §6 common rules when touching the API.
- An item is done when its "Definition of done" is verified literally by a command, test or recorded manual check. Report how it was verified.
- Do not start the next item, and do not build anything from: design §14.4 (cut order), §14.5 (deliberately not built), product-scope §8 (out of scope) and §9 (cuts).
- Items #12–#14 are cut (design §16.5). Basic features stay minimal; the AI pipeline is the priority.
- If the design is ambiguous, contradictory or wrong for the code at hand: stop and ask. Do not improvise a variant.
- An agreed deviation is recorded in `docs/design.md` (the relevant section or §14.1) in the same commit as the code.
- Resolving a `[TO BE VERIFIED]` / `[TO VERIFY]` marker (design §14.3): record the result in the design section that holds the marker, in the same commit.

## Commands

From the repository root unless stated. Paths and names come from design §2, §9, §11.

```bash
dotnet tool restore                                   # dotnet-ef 9.0.22 (.config/dotnet-tools.json)
dotnet build Flatplanner.sln
dotnet test Flatplanner.sln
dotnet format Flatplanner.sln --verify-no-changes     # not defined by the design; standard SDK command
docker compose up -d mysql                            # local MySQL 8.4
dotnet ef migrations add <Name> --project src/Flatplanner.Core --startup-project src/Flatplanner.Api
dotnet run --project src/Flatplanner.Api --urls http://localhost:5080
dotnet run --project src/Flatplanner.Ingest -- <scrape|normalize|nearby|export-dossiers|import-dossiers|report|all|eval-intents>
docker compose up --build                             # app + MySQL, as in production
```

Frontend (`web/`, scripts from the `create-vite` `react-ts` template):

```bash
npm ci
npm run dev       # port 5173, proxies /api to http://localhost:5080
npm run build     # tsc + vite build -> web/dist
npm run lint      # template script; the design does not mention it
```

Environment: `set -a; source .env; set +a` before `dotnet run` (design §10). `eval-intents` calls the paid OpenAI API: run it only when the item requires it. The dossier text is produced by the agent in-session from `prompts/listing-dossier.v1.md`, never through the API.

## Hard rules

- **Names are final.** Tables, columns, C# types, endpoints, JSON fields, query parameters, environment variables and file names are copied 1:1 from the design. URL parameters = `SearchIntent` property names (R2).
- **Serialization rule (design §6):** null fields are omitted (`JsonIgnoreCondition.WhenWritingNull` in `ApiJson.Options`); arrays and booleans are always present; the only exception is `Sourced<T>.Value`, always written, also as `null`. TypeScript marks every omittable field as optional.
- **Migrations:** every entity or model-configuration change gets `dotnet ef migrations add <Name>` in the same commit. EF Core 9 raises `PendingModelChangesWarning` in `Migrate()`, and the API migrates at startup, so a missing migration is a production restart loop.
- **No layers:** `DbContext` used directly. No repository pattern, unit of work, MediatR, CQRS, generic services, AutoMapper, controllers. Four projects only: Core, Api, Ingest, Tests (design §2).
- **Enums are `VARCHAR`** with `string` constants (`Flatplanner.Core/Data/Enums.cs`); no C# `enum` + `HasConversion`. Unknown source value → `NULL` + `unknown_value:<column>` flag, never silently mapped.
- **Dependencies:** only the packages and versions in design §2. A new package needs approval and a design update.
- **Model judges, code verifies (design §16):** the LLM interprets needs and gives a verdict per listing; every verdict must cite evidence from the listing dossier (quote, nearby place, field), and C# (`EvidenceVerifier`) downgrades anything unverifiable to `unknown`. Hard filters, grouping order and distances are deterministic C#.
- **LLM calls:** prompts only in versioned files under `prompts/` (`search-intent.v2.md`, `listing-dossier.v1.md` for the agent, `listing-judge.v1.md`); a changed prompt is a new version file. Structured output with `strict: true`; output validated (schema, `AnchorValidator`, `SearchIntentNormalizer`) before use. Every call has the design's failure path: chat → `IntentFallbackParser` with `fallbackReason` (§7.7), never 5xx; `import-dossiers` drops invalid risks and marks a listing without a valid line `facts_status = 'failed'` (§16.1); judge batch failure → its listings `unknown` with a notice (§16.2).
- **HTML is never rendered.** Descriptions are shown as text (`description_text`, `white-space: pre-line`); no `dangerouslySetInnerHTML`.
- **User-facing text is Polish and comes verbatim from `ui-spec.md` §7.** English strings in `design.md` are glosses. Do not invent copy; if a string is missing from §7, ask.
- **Secrets:** never in code, `appsettings.json`, commits or logs. Local `.env` (gitignored), Railway variables in production; `.env.example` carries keys with placeholder values only. Never read or print `.env`.
- **Scraped data stays out of git:** `data/` is ignored. Contact keys (`owner`, `agency`, `contactDetails`, `userAdverts`) are stripped before saving (§4.1). The two fixtures in `tests/Flatplanner.Tests/Fixtures/` are committed only after contact masking and a manual review by the author (§12).
- **Chat logs** never contain the user's message (§7.7): log reason and prompt version only.

## C# conventions

- `Directory.Build.props`: `net9.0`, `Nullable=enable`, `ImplicitUsings=enable`. The design sets no warnings-as-errors or analyzer packages; do not add them.
- File-scoped namespaces; one public type per file, named after the type.
- `sealed record` for DTOs and value types; `Sourced<T>` exactly as in §6.
- `async` all the way with `CancellationToken` passed through; no `.Result` / `.Wait()`.
- Configuration only through `FlatplannerOptions` (`[ConfigurationKeyName]`, `ValidateOnStart`); no direct `Environment.GetEnvironmentVariable`.
- Logging via `ILogger<T>` with message templates (`"Rejected {ExternalId}: {Reason}"`), no string interpolation, no `Console.WriteLine` except Ingest command output the design specifies (`report`, `eval-intents`, `scrape` counters).
- Exceptions: catch only what the design maps (e.g. §7.7 table), log it, and take the designed path. No empty `catch`. Errors to clients as `ProblemDetails`.
- Constants the design names (`SearchDefinitions`, `DbConstants.ServerVersion`, `FallbackDictionaries`, `KeywordRules`, page size 20, scrape quotas) live in code, not configuration, and are not duplicated as literals.
- **Comments: default is none.** Names and small functions carry the meaning. Add a comment only when the code cannot show the reason: a non-obvious constraint or a decision from the design (e.g. why `ForwardLimit = 1`, why `has_lift` is never `false`), one line, with the design section if there is one. No comments that restate the code, no XML doc comments on internal members, no section banners, no commented-out code. Applies to C#, TypeScript, SQL and config files.

## TypeScript conventions

- `strict` from the template; no `any`, no non-null `!` to silence the compiler.
- Optional API fields are `field?: T` and are `undefined` when absent, never `null`. The exception mirrors the API: `Sourced<T>.value: T | null`.
- DTO types by hand in `web/src/api.ts`; no OpenAPI generator. State lives in the URL (`useSearchParams`), server data in TanStack Query; no global store.
- Comments: the same rule as for C# (default is none).
- Only `react-router` and `@tanstack/react-query` beyond the template. One `web/src/styles.css`, tokens from ui-spec §2; no component library.

## Testing

- xUnit in `tests/Flatplanner.Tests`. Test classes and their place are fixed by design §2 and §12: `NormalizationTests`, `AnchorValidatorTests`, `SearchIntentSchemaTests`, `FallbackParserTests`, `ApiSerializationTests`, fixtures in `Fixtures/`, queries in `tests/search-queries.json`.
- Test method names: `Method_Scenario_Expected`. Arrange / Act / Assert, one behaviour per test; `[Theory]` for tables of cases.
- No network, no database, no `WebApplicationFactory`, no LLM in tests. `ApiSerializationTests` serialize DTOs with `ApiJson.Options`.
- Each item adds exactly the assertions §12 assigns to it (e.g. `ApiSerializationTests` grows at #4, #6, #12, #13).
- Deliberately not tested (§12): HTTP endpoints (covered by smoke test §11.5), the frontend, the live scraper, ranking numerically, LLM in CI (`eval-intents` instead). Do not add these.

## Git

- Conventional Commits with scope: `feat(api): add listings endpoint`. Scopes: `core`, `api`, `ingest`, `web`, `tests`, `infra` (Dockerfile, compose, Railway), `docs`, `prompts`.
- Imperative mood, subject ≤ 72 characters, body says why when not obvious. One logical change per commit; a migration ships with its entity change.
- Branch per §13 item: `feat/<n>-short-name` (e.g. `feat/4-list-api`); otherwise `fix/…`, `docs/…`, `chore/…`. PR title = the squash commit message.
- Before every commit: `dotnet format --verify-no-changes`, `dotnet build`, `dotnet test`, and `npm run build` when `web/` changed. All green, no exceptions.
- Never commit `.env`, `data/`, `bin/`, `obj/`, `node_modules/`, `web/dist/`, `wwwroot/`. Check `git status` before committing.

## Public repository hygiene

The repository is read by reviewers. Keep it a product, not a diary.
- No planning chatter, agent transcripts or scratch files in commits, code or docs.
- No `TODO` without context: either do it or state why not and where it is tracked (design section or the one-pager's "with more time").
- No references to private or local-only files. Generated `docs/ingest-report.md` is committed; nothing else generated is.

## Workflow per item

1. **Plan** (`implement-item`): files, names from the design, tests, how each DoD clause is checked. **Wait for the user's approval**; skip the wait only if the user asked for autonomous work in this session.
2. **Implement + tests**, then format, build and tests green.
3. **`clean-code`** (apply) on the changed files.
4. **`overengineering-check`** on the branch diff; fix every "remove".
5. **`architecture-check`**, only when the item adds a project, folder, reference, package, middleware or Docker/compose change.
6. **`code-review`** on the cleaned branch diff; fix blocker and major findings.
7. **Verify the DoD** clause by clause; manual steps go to the user.
8. **Commit, push, PR** to `main` (title = squash-commit message, body from the PR template).
9. **Stop** until the user merges.

Outside the cycle: `overengineering-check` on any plan or design change; `code-review` on any PR on request; `pre-submission-check` only when the user starts it, before handing in the task.
