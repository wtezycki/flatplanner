---
name: code-review
description: Review a Flatplanner diff, branch or PR for correctness against the docs/design.md contract (names, JSON shape, status codes, serialization rule, LLM fallback paths), security (secrets, XSS, rate limiting, contact data), error handling, §12 tests and Polish UI texts from docs/ui-spec.md §7. Use when asked to review changes, a PR or a branch in this repository. Severity-tagged findings only.
argument-hint: "[diff-range | PR number]"
---

# Code review

Target: `$ARGUMENTS` (default: `git diff main...HEAD` plus uncommitted changes).

This skill checks behaviour and contract. Scope creep → `overengineering-check`; project boundaries and pipeline → `architecture-check`; readability → `clean-code`. Run those separately and link their output; do not repeat their checklists here.

## Procedure

1. Read the diff and identify the §13 item (branch `feat/<n>-…`).
2. Open the design sections the changed code implements (§3 data, §4 normalization, §5 enrichment, §6 API, §7 search, §8 protection, §9 frontend) and ui-spec §7 for strings. Review against them, not against general taste.
3. Run `dotnet build` and `dotnet test` if not already run; failures are blockers.
4. Report findings. Skip anything `dotnet format`, the compiler or ESLint already reports.

## Checklist

**Contract (design §6, §7)**
- Endpoint paths, query parameter names (= `SearchIntent` properties), JSON field names in camelCase, types and nesting exactly as §6.1–6.6.
- Serialization: no `null` in list responses; arrays and booleans always present; `Sourced<T>.Value` written as `null`; `null` kept inside `remove` maps. TS types mark omittable fields optional.
- Status codes: type error in query → 400 ProblemDetails; semantic error → value dropped + `interpretation.warnings`; unknown listing → 404; unknown `/api/*` → 404 ProblemDetails; chat message length outside 1–`CHAT_MAX_LENGTH` → 400 with `errors.message`, model not called; rate limit → 429 + `Retry-After`.
- LLM failure never yields 5xx: every §7.7 error maps to the right `fallbackReason`; `promptVersion` omitted on fallback; `CHAT_MODE=fallback` → `disabled`.
- Enrich: SDK failure, refusal, `Length`/`ContentFilter`, JSON error → `facts_status='failed'`, import continues (§5.2). `not_mentioned` never stored.
- Normalizer order and warnings exactly as §7.3; tolerances and ranking weights match §7.4–7.5; pagination: page > total → last page.
- Normalization rules match §4.2 and quality flags §3.6; hard-field rejects use the §4.3 codes.
- Every entity change has a migration in the same commit.

**Security and privacy**
- No secrets in code, config, tests, logs or `.env.example` values.
- No HTML rendering: no `dangerouslySetInnerHTML`; description from `description_text`.
- `requestedCityOutOfScope` validated by the §7.3 regex before it reaches a message.
- Contact keys stripped before raw save (§4.1); phone/e-mail masking in `description_text` (§4.2 step 9); fixtures masked.
- Chat logs omit the user message (§7.7). Rate limiter partitions by client IP behind the proxy (§8).
- `LIKE` built from folded tokens via `EF.Functions.Like`, no string-concatenated SQL.
- Images: `loading="lazy"`, `referrerPolicy="no-referrer"`.

**Error handling**
- No swallowed exceptions; caught exceptions are the ones the design maps, logged with structured templates.
- `CancellationToken` propagated; chat timeout via `CHAT_TIMEOUT_SECONDS` CTS.
- Options validated at startup (`OPENAI_API_KEY` required when `CHAT_MODE=llm`).
- Frontend: 429 / 400 / 5xx / network handled per §9; 4xx not retried.

**Tests (§12)**
- Tests the item owes per §12/§13 exist and assert the listed cases; names `Method_Scenario_Expected`; no network, DB or LLM.

**UI texts (ui-spec §7)**
- Every user-facing string (frontend and API: labels, chips, summary, `why`, notices, warnings, errors) is Polish and matches ui-spec §7 verbatim, including typographic quotes „…”, ` · ` separators, Polish plural forms and city genitives (ui-spec §7.3).

## Severity

- **blocker**: breaks the contract, loses data, leaks secrets/PII, crashes, 5xx on LLM failure, missing migration.
- **major**: wrong behaviour in a spec'd case, missing required test, wrong Polish text.
- **minor**: edge case or robustness gap with low impact.
- **question**: design unclear; needs the author's decision.

## Output format

```
Code review: <target>, item #<n>
Build/tests: ✓ | ✗ <summary>
Findings: blocker <a>, major <b>, minor <c>, question <d>

[blocker] <path>:<line> <problem>
  Spec: design §<x> | ui-spec §7.<y>
  Fix: <concrete change>
...
Related checks: overengineering-check <run/not run>, architecture-check <run/not run/n.a.>, clean-code <run/not run>
```

No praise, no summary of what the code does, no style nits.
