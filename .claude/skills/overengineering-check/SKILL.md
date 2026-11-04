---
name: overengineering-check
description: Flag YAGNI violations in a diff, branch or plan against Flatplanner's fixed scope - features from the cut lists, single-implementation abstractions, layers the design rejects, speculative configuration, premature generalization, packages not in design §2, endpoints or JSON fields not in the contract. Use before committing an item, when reviewing a plan, or when asked "is this overengineered?".
argument-hint: "[diff-range | plan]"
---

# Overengineering check

Target: `$ARGUMENTS` (default: `git diff main...HEAD` plus uncommitted changes; a plan text if one was given).

The task brief says "avoid overengineering" and values reasoning over feature count. Every line must be traceable to `docs/product-scope.md` §4 or a `docs/design.md` section. Anything else is a finding.

## Procedure

1. Get the diff (`git diff --stat`, then the full diff) or read the plan.
2. Identify the current §13 item (branch name `feat/<n>-…` or the plan). Its row defines what is allowed now.
3. For each added type, member, file, endpoint, field, package and setting, find the design section that requires it. No section → finding.
4. Run the checklist. Propose the cheapest fix for each finding.

## Checklist

**Scope**
- Code for a later §13 item (e.g. `chips[].remove` before #13, `blockers` before #12, `why`/ranking before #8, `askSeller` before #14).
- Anything in design §14.5 (deliberately not built): `CHAT_MODE=off`, `description_html`, `fields.realPricePln`, intent echo in `GET /api/listings`, `interpretation.cityMedians`, `staleNotice`, `modified_at_source`/`normalized_at`, request counters in `report`, text-field `city_out_of_scope` variant.
- Anything cut before implementation (§14.4): keyword parsing of the search field (§6.7), "+N listings without lift info" count.
- Anything in product-scope §8: full W2, UC15 fact filters, map, walking time, contradictions, deduplication, scraper robustness (fingerprinting, circuit breaker), FULLTEXT/stemmer, multi-step chat, per-market medians.
- LLM used beyond the two uses (product-scope §7): no LLM ranking, summaries, embeddings, photo analysis.

**Structure**
- A fifth project, or Domain/Application/Infrastructure layers (design §2).
- Repository, unit of work, MediatR, CQRS, generic `IService<T>`, AutoMapper, controllers (§14.2).
- An interface with one implementation and no test double need (tests use no mocks of the DB or LLM, §12).
- C# `enum` + converters instead of `VARCHAR` constants (§3.1).
- A CLI library in Ingest (`args[0]` switch, §11.1); AngleSharp or an HTML parser (§2, §4).

**Configuration and contract**
- Settings not in the §10 table; constants the design keeps in code turned into options (quotas, page size 20, tolerances).
- Query parameters, endpoints or JSON fields not in §6; `pageSize` as a parameter; sparse fieldsets.
- Application-level budget counter for the LLM (§8 rejects it).
- Response compression added without the §6 check showing it is needed.

**Dependencies**
- Any NuGet or npm package not in design §2 (component libraries, state managers, OpenAPI generators, Polly, Serilog, FluentValidation, test mocking libraries).

**Code shape**
- Generic helpers with one caller; extension points "for later"; options objects with a single used flag.
- Error handling for cases the design does not have (e.g. retry layers on top of the SDK retry, §5.2).
- Tests from §12 "deliberately not tested" (WebApplicationFactory, frontend, live scraper, numeric ranking, LLM).

## Verdicts

- **remove**: not required by any doc, or explicitly cut.
- **simplify**: required, but built bigger than the design (layer, abstraction, config).
- **justify**: plausibly needed but not in the docs; requires the author's decision and, if kept, a design update in the same commit.

## Output format

```
Overengineering check: <target>, item #<n>
Findings: <count> (remove <a>, simplify <b>, justify <c>)

1. [remove|simplify|justify] <path>:<line> <what>
   Why: <cut list entry / missing design reference>
   Cheapest fix: <one concrete action>
...
Clean: <areas checked with no findings>
```

No findings → `Findings: 0` and the "Clean" line. No praise, no style remarks.
