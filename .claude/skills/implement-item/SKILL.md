---
name: implement-item
description: Implement exactly one item from docs/design.md §13 (implementation order) end to end: approved plan, implementation, clean-code, overengineering and code review, verified definition of done, commit and PR. Use when asked to implement, build or continue "item #N", "the next item", or a feature that maps to a §13 row. Refuses to start the following item.
argument-hint: "[item-number]"
---

# Implement one §13 item

Item: `$ARGUMENTS` (if empty, the lowest-numbered §13 item not yet done according to `git log`; confirm it with the user before starting).

## Procedure

1. **Locate the item.** Read its row in `docs/design.md` §13: "What is produced", "Project", "Definition of done". Check the previous item's DoD is met (`git log`, files present). If not, stop and say so.
2. **Gate #12–#14.** For items #12, #13, #14 ask the user whether #15–#17 are at risk before starting (design §13).
3. **Read only what the item references.** The design sections named in its row, plus: §6 common rules (any API change), §3 (any entity change), §12 (tests for this item), ui-spec §7 (any user-facing string), ui-spec §4–§6 for the components the item names.
4. **Plan and wait for approval** (≤ 10 lines): files to create or change, types and names as spelled in the design, tests from §12 assigned to this item, how each DoD clause will be verified. **Stop and wait for the user's explicit approval** before writing any code. Skip the wait only if the user said in this session to work autonomously. Ambiguity or a design gap → ask in the plan; do not improvise.
5. **Branch:** `feat/<n>-short-name` from an up-to-date `main`.
6. **Implement** in the projects the row lists. Rules from `CLAUDE.md` apply in full; in particular:
   - entity or model change → `dotnet ef migrations add <Name> --project src/Flatplanner.Core --startup-project src/Flatplanner.Api` in the same commit;
   - names 1:1 from the design; Polish strings verbatim from ui-spec §7;
   - LLM work: prompt file in `prompts/`, strict schema, validation, fallback path (§5.2, §7.7).
7. **Tests:** add exactly the §12 tests or assertions this item introduces. Nothing from "Deliberately not tested".
8. **Gate:** `dotnet format Flatplanner.sln --verify-no-changes`, `dotnet build Flatplanner.sln`, `dotnet test Flatplanner.sln`; `npm run build` (and `npm run lint` if present) in `web/` when the item touches `web/`. Re-run after every later step that changes code.
9. **Clean up:** run the `clean-code` skill in apply mode on the changed files (including removing comments that are not strictly necessary).
10. **Cut:** run the `overengineering-check` skill on `git diff main...HEAD`. Fix every "remove" finding; "justify" findings go to the summary.
11. **Structure** (only if the item adds a project, folder, project reference, package, middleware or Docker/compose change): run the `architecture-check` skill and fix its findings.
12. **Review:** run the `code-review` skill on `git diff main...HEAD`. Fix blocker and major findings; list the rest in the summary. Review runs last, on the cleaned diff, so nothing is reviewed that the previous steps remove.
13. **Verify the DoD literally.** Each clause of the DoD maps to one check: a command with its output, a test name, or a manual browser/deploy step the user must perform. Manual steps are listed for the user, never claimed as done.
14. **Design sync:** a deviation agreed with the user or a resolved `[TO BE VERIFIED]` is written into `docs/design.md` in the same commit.
15. **Commit, push, PR** (skip any of these if the user said so): Conventional Commits `feat(<scope>): <imperative summary>` with scopes from `CLAUDE.md`; show `git status` first (`data/`, `.env`, build outputs must not appear); `git push -u origin <branch>`; open a PR to `main` whose title is the squash-commit message and whose body fills `.github/pull_request_template.md`. If no tool can open the PR, give the user the compare link.
16. **Stop.** Do not begin the next item until the user has merged this one.

## Checklist

- [ ] Only files the item's row implies; no code for later items (e.g. no `remove` on chips before #13, no `why` before #8, no `blockers` before #12).
- [ ] Nothing from design §14.4/§14.5 or product-scope §8/§9 cut lists.
- [ ] No new NuGet/npm package beyond design §2.
- [ ] Serialization rule (§6) respected by every new DTO; `ApiSerializationTests` extended if the item is #4, #6, #12 or #13.
- [ ] Migration present for every entity change; `dotnet build` shows no pending model change.
- [ ] No secrets, no scraped data, unmasked fixtures not staged.
- [ ] Format, build and tests green; DoD verified clause by clause.

## Output format

```
Item #<n> <name>: DONE | BLOCKED
Branch: feat/<n>-...
Changed: <file list, grouped by project>
Tests added: <test names>
Gate: format ✓/✗  build ✓/✗  test ✓/✗ (<passed>/<total>)  web build ✓/✗/n.a.
DoD:
- <clause> → <command/test/manual step> → <result | "manual: for the user">
Design updated: <section> | none
Clean-code: <n fixes>
Overengineering check: <n findings, all resolved | list>
Architecture check: <findings | not needed>
Code review: <blocker/major fixed; minor listed | none>
Commits: <hashes + subjects> | not committed
PR: <url | compare link | not opened>
Open questions: <list | none>
```
