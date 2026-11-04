---
name: clean-code
description: Readability pass on changed Flatplanner code (C# and TypeScript) - naming per docs/design.md, function size, dead code, duplication, removing comments that are not strictly necessary, consistent error handling, design-named constants instead of magic numbers. Reports by default; applies fixes only when explicitly asked ("fix", "apply"). Use after an item is functionally complete or when asked to tidy or simplify code.
argument-hint: "[diff-range] [apply]"
---

# Clean code pass

Target: `$ARGUMENTS` (default: files changed in `git diff main...HEAD` and uncommitted changes). Mode: **report** unless the arguments or the user say `apply`/`fix`.

Scope: readability of changed lines and their immediate context. Not behaviour (`code-review`), not scope (`overengineering-check`), not formatting (`dotnet format`, ESLint).

## Procedure

1. List changed files; read each changed region with enough context.
2. Run the checklist. Each finding must make the code shorter, clearer or more consistent with the design; skip taste-only remarks.
3. Report mode: output findings. Apply mode: make the fixes, keep behaviour identical, then run `dotnet format Flatplanner.sln --verify-no-changes`, `dotnet build`, `dotnet test` (and `npm run build` for `web/`), and report results.
4. Never rename a type, member, column, JSON field or parameter that the design names; flag it as a design question instead.

## Checklist

**Naming**
- Names match the design spelling (`SearchIntentNormalizer`, `IntentFallbackParser`, `facts_status`, `fallbackReason` …), not near-synonyms.
- Local names say what the value is (`pricePerM2`, not `p`); booleans read as predicates (`isOutlier`, `RequireLift`).
- TS helpers follow §9 names (`intentFromSearchParams`, `intentToSearchParams`, `fetchListings`).

**Size and structure**
- Methods doing several steps of a numbered design procedure (e.g. §7.3 normalizer steps, §4.2 rules) read as one step per block or private method, in the design's order.
- Endpoint handlers stay thin: parse → call → map; query building and ranking in named methods.
- React components follow the ui-spec §5 split; no component holding another component's logic.

**Dead code and duplication**
- Unused members, parameters, usings, imports, commented-out code, debug output.
- The same rule implemented twice (e.g. folding outside `TextFolding`, labels outside `format.ts`/`/api/meta`, a second normalization path besides `SearchIntentNormalizer`).

**Constants**
- Values the design names live in their named home and are referenced, not repeated: `SearchDefinitions` (±12%, n < 10, weights 0.7/0.3, 5% delta), page size 20, `FallbackDictionaries`, `KeywordRules`, quality-flag ranges (§3.6), anchor amount ranges (§5.1), `DbConstants.ServerVersion`, scrape quotas.
- Enum strings via `Enums.cs` constants, not literals.

**Comments**
- Default is no comments. Flag for removal every comment that restates the code, narrates steps, repeats a name, is an XML doc comment on an internal member, a section banner or commented-out code. Prefer a better name or an extracted function over a comment.
- Keep only one-line comments that state a reason the code cannot show (a non-obvious constraint or a design decision, e.g. `ForwardLimit = 1`, `has_lift` never `false`). Add one only where such a reason exists and is missing.
- No `TODO` without context; no references to private notes.

**Error handling consistency**
- Same pattern for the same failure kind: ProblemDetails for client errors, `LogWarning` + designed fallback for LLM failures, `LogWarning("Rejected {ExternalId}: {Reason}")` for rejects.
- Structured logging templates, no interpolation.

## Output format

```
Clean code: <target> (report | applied)
Findings: <n>

1. <path>:<line> [naming|structure|dead-code|duplication|constant|comment|errors] <issue>
   Fix: <concrete change, or "applied">
...
Gate (apply mode only): format ✓/✗  build ✓/✗  test ✓/✗
Design questions: <names that look wrong but are fixed by the design | none>
```
