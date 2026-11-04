# Flatplanner Documentation

Index of the project documents for the Flatplanner flat listings portal (C#/.NET + React + MySQL).

As of 2026-11-04.

## Documents

| Document | Contents |
|---|---|
| [product-scope.md](product-scope.md) | The binding product scope: personas, MVP features with acceptance criteria, scenarios A and B, deliberate cuts. |
| [scraping-plan.md](scraping-plan.md) | Plan for acquiring data from otodom.pl: choice of source, geographic scope, scale, fields and the import pipeline. |
| [design.md](design.md) | Implementation design: data model, normalization, API contract, LLM, chat protection, deploy, tests and implementation order. |
| [ui-spec.md](ui-spec.md) | UI specification: design tokens, layout, components, states, interface texts and accessibility. |

## Suggested reading order

1. Scope: `product-scope.md`
2. Data plan: `scraping-plan.md`
3. Design: `design.md`
4. UI: `ui-spec.md`

**Roles of the documents:** `product-scope.md` and `scraping-plan.md` record the product and data decisions. `design.md` and `ui-spec.md` are the detailed specification the implementation (written largely with a coding agent) follows, so they are deliberately exhaustive; the rationale in short is the one-pager (`one-pager.md`), added together with the import report `ingest-report.md`. Feature identifiers (UC/W) are defined in `product-scope.md`.
