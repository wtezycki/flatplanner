---
name: pre-submission-check
description: Final gate before handing the Flatplanner repository to reviewers - public repo hygiene, README and one-pager completeness, resolved [TO BE VERIFIED] markers, no secrets or scraped data in git history, and the production smoke test from docs/design.md §11.5. Use when asked whether the repo is ready to submit or share, or after §13 items #15-#17.
disable-model-invocation: true
---

# Pre-submission check

Run on `main` with a clean working tree. Read-only: report, do not fix. Steps needing the production URL or a browser are listed for the user, not claimed.

## Procedure

1. `git status` clean; `git log --oneline` follows Conventional Commits.
2. Run each checklist group; record evidence (command + result) per line.
3. Produce the report. Any ✗ in "Blocking" means NOT READY.

## Checklist

**Blocking: secrets and data**
- `git ls-files | grep -E '^(data/|\.env$)|/bin/|/obj/|node_modules|wwwroot'` → empty.
- `git log --all -p | grep -nP 'sk-(proj-)?[A-Za-z0-9_-]{20,}|Password=(?!change-me|\$)[^;\s]+'` → empty; `.env.example` holds placeholders only.
- No raw otodom JSON outside `tests/Flatplanner.Tests/Fixtures/`; the two fixtures contain no `owner`, `agency`, `contactDetails`, `userAdverts` keys and no phone numbers or e-mails in `description` (§4.1, §12).
- No `mysqldump` output in the repo (§11.4).

**Blocking: build and tests**
- `dotnet format Flatplanner.sln --verify-no-changes`, `dotnet build`, `dotnet test` green; `npm ci && npm run build` in `web/` green.
- `docker compose up --build` from a fresh clone starts; `/api/health` returns `db: ok` (§13 #1, #17).

**Blocking: documents**
- `README.md` (§13 #17): how to run from a fresh clone, scenarios A and B, reason for choosing C#, ranking weights (`cheap 0.7`, `good_condition 0.3`, §7.5), the "ładne" failure mode, the `eval-intents` X/10 score, the .NET 9 end-of-support sentence (§14.2).
- `docs/one-pager.md` (§13 #16): the 7 points required by the task description, fits one page, numbers consistent with `docs/ingest-report.md` and the `eval-intents` result; cuts appear under "with more time" with the product-scope §8 sentences.
- `docs/ingest-report.md` generated and committed.
- `grep -rn 'TO BE VERIFIED\|TO VERIFY' docs/` → every remaining marker either resolved with the recorded outcome or deliberately left with a reason (e.g. cost **[estimate]** markers).
- Design matches the code: every agreed deviation recorded in `docs/design.md`.

**Hygiene**
- `grep -rnI 'TODO\|FIXME\|HACK' src web/src tests prompts` → none without context.
- No scratch files, transcripts or personal notes tracked; no references to local-only or private files in code or docs.
- No commented-out code blocks, `Console.WriteLine` debug output or `console.log` in `web/src`.
- Polish UI strings match ui-spec §7 (spot-check list, details, chat errors, notices).

**Production smoke test (§11.5; user runs with `$URL`)**
1. `/api/health`: `db: ok`, `clientIp` correct from two networks.
2. Scenario A: `cities=gdansk&areaMin=40&areaMax=80` → `total > 0`; details show `pricePln`, `askSeller`; browser flow incl. "back".
3. Scenario B via `/api/search/interpret`: `usedFallback: false`, `areaApprox: 40`, both preferences; list → `sort: relevance`, `why` non-empty.
4. `text=morena` matches title, description or street.
5. "2 pokoje w Krakowie" → `requestedCityOutOfScope: "Kraków"`, `cities: ["gdansk"]`.
6. Sixth interpret request in a minute → 429.
7. `grep -c ':null'` on the list response → 0; `content-encoding` present.
- Listing count in production = local (§13 #15). Raw forwarded headers removed from `/api/health` (§13 #2).

## Output format

```
Pre-submission check: READY | NOT READY
Blocking:
- [✓|✗] <check> — <evidence>
Hygiene:
- [✓|✗] <check> — <evidence>
Smoke test (for the user): <steps not yet confirmed | all confirmed by the user>
Remaining markers: <list with reason | none>
```
