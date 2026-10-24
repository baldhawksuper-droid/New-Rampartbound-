# Rampartbound — Project Status & Source of Truth

**Last checked:** 2026-10-24
**Project:** Rampartbound: Merge Defense
**Unity editor version in `ProjectSettings/ProjectVersion.txt`:** 6000.6.5f1
**Current working directory (this machine):** `D:\Hermes Project\Rampartbound`

## Purpose and authority

This document is the concise index for current, verified project facts. If older concept documents conflict, prefer this status page for facts that were directly inspected; the approved design decisions still require the game owner's confirmation. The game owner is the final decision-maker. Hermes Agent is an AI assistant serving as planning, implementation, and review lead when requested; role names in older documents do not imply that staffed specialists have performed work.

## Verified repository facts

- Unity project root contains `Assets/`, `Packages/`, `ProjectSettings/`, and `Builds/`.
- Main scene file exists: `Assets/Scenes/RampartboundMainScene.unity`.
- Hero ScriptableObject assets exist for Cedric, Rose Tanker, Rabbit Mage, and Wind Ranger under `Assets/ScriptableObjects/Units/`.
- Hero sprite files exist under `Assets/Art/Sprites/Heroes/`.
- `LevelConfigSO` defaults its battle mode to `PuzzlePreparation`.
- `StageManager` assigns `PuzzlePreparation` to several configured stages and `RealTimeDeploy` to others. The current canonical brief sets hybrid modes by level; historical GDDs conflict and stage-specific assignments have not been fully audited.
- `RampartboundBuildPipeline.cs` declares app version `1.2.17` and Android bundle version code `137`.
- The `Builds/` tree contains artifacts named `Rampartbound_v1.2.14_release.apk`, `Rampartbound_v1.2.3_debug.apk`, and `Rampartbound_debug.apk`. Artifact names alone do not establish build success, installability, or current release status.
- The working tree was already dirty before this documentation update: `AGENTS.md` and `docs/TEAM_PROJECT_WORKFLOW.md` had edits replacing BaldHawk with Hermes Agent.

## Product goal (owner-approved direction)

Deliver a readable mobile hybrid merge/puzzle defense campaign. Levels deliberately select one of the battle modes—Puzzle Preparation or Real-Time Deploy—rather than combining both in every level. Players make meaningful grid decisions, evolve a compact hero roster, and defend the Citadel against escalating waves. The first three heroes are Cedric, Rose Tanker, and Rabbit Mage; Wind Ranger is a gacha/summon unlock. Visual design and UX review is part of Sprint 49 before gameplay implementation changes.

## Decisions still needing owner approval

1. **Target platform and release stage:** Is the current goal Android prototype, closed testing, or public release? Confirm supported devices and minimum Android/API requirements.
2. **Monetization scope:** Are IAP, guild, battle pass, cloud save and live events in the intended near-term product or just prototypes/future concepts? Wind Ranger gacha is approved as a design direction; its economy and commercial implementation still need balancing and compliance review.
3. **Official project title and product claim:** Use “Rampartbound: Merge Defense” as working title. Do not claim trademark clearance or originality verification without documented professional search/legal review.

## Documentation rules

- Separate **verified implementation**, **design decision**, **proposal**, and **not yet tested**; do not label features “complete” based only on a sprint note or script existing.
- For every release/version statement, cite the source of truth in code/settings and the tested build artifact. Do not infer current version from stale README examples.
- Avoid absolute guarantees such as “zero defects”, “100% complete”, or “100% original”. State test scope, date, result, and limitations instead.
- Keep environment-specific paths out of portable setup instructions; use `<project-root>` and mention the current local path only in this status page.
- Keep older concept documents as historical material until reconciled; mark them “legacy/unverified” rather than silently representing them as current implementation.
- Update this page after a product decision or verified project change.

## Sprint 49 — Product, Documentation & Visual/UX Audit

**Goal:** Align the product direction, documents, and visual design evidence before gameplay implementation changes.

**Owner-approved direction:** campaign is hybrid across levels (each level uses a clearly communicated battle mode); starting trio is Cedric, Rose Tanker, Rabbit Mage; Wind Ranger is a gacha/summon unlock. Visual/design audit is included before gameplay changes.

**Completed in this pass:**
- Added this status/source-of-truth page with confirmed direction, directly verified project facts, limitations, and unresolved platform/monetization decisions.
- Updated README and setup guide to remove old machine-specific path examples, use the recorded Unity Editor version, and avoid absolute claims of readiness or missing-reference guarantees.
- Updated the project role descriptions to identify Hermes Agent as AI project lead and the user as product owner; reduced some absolute QA/sprint claims.
- Inspected `Gameplay.jpeg`: colorful tokens are distinguishable, but board/background separation and cell boundaries are weak; the image shows no objective, mode/phase, moves/economy, next-wave context, or castle-health indicator; the fence, cropped side structures, and large blue/gold object compete with the grid. These are image-only findings, not proof of runtime UI state.
- `Pixel Art.jpeg` appears to be a third-party/ad-feed composition with platform overlays, and is not verified as Rampartbound-owned art or in-game UI. Do not use it as evidence of the game's art direction. Establish a consistent original pixel-art direction from project-owned assets instead.
- Unity runtime behavior, other screens, portrait aspect ratios, device-scale readability, and actual use of the referenced sprites remain unverified.

**Bounded work completed so far (not full design audit):**

- [x] Record approved product direction: per-level hybrid modes; starting trio Cedric/Rose/Rabbit Mage; Wind Ranger via gacha.
- [x] Create canonical current product brief and project status page.
- [x] Mark the older Master GDD, core gameplay GDD, and lobby concept as historical references.
- [x] Update README/setup repo references to actual repository/build-pipeline facts and use portable project-root instructions.
- [x] Record first-pass image observations. `Pixel Art.jpeg` is not verified as project-owned art and is excluded as evidence of this game's visual style.
- [x] A local Markdown link check found no broken links; `git diff --check` passed on the preceding change set. Re-run after pending edits.
- [ ] Review every visual surface in the running Unity project: lobby, stage map, battle HUD, modals, tutorials, typography, icons, safe areas, and multiple mobile aspect ratios. Current review is image-only.
- [ ] Verify actual Unity menu/build paths and create/install/run a current build. Pipeline code says 1.2.17 / code 137; newest named APK found is 1.2.14. No current build is evidenced.
- [ ] Reconcile remaining historical sprint statements against source, assets, serialized scene/config, and test evidence. Legacy labels mean “not authoritative”, not that every claim was independently audited.
- [ ] Recheck docs after final edits, commit, push, and verify remote SHA.

**Sprint 49 is in progress, not complete.** A documentation commit exists, but its existence does not validate gameplay, complete the visual audit, or mean Sprint 49 is done. Definition of done: current product brief and portable setup guidance; full visual/UX audit with proposals; remaining sprint claims verified or explicitly tracked; link/stale-string checks pass; and final changes committed/pushed with remote SHA confirmed.

## Remaining owner decisions (not blockers for current doc cleanup)

- Confirm release stage (prototype, closed test, or public release) and supported Android/device baseline.
- Confirm which of IAP, guild, battle pass, cloud save and live events are in near-term scope. Wind Ranger gacha is approved as direction, but economy and real-money implementation still require separate balance/compliance review.
- Keep “Rampartbound: Merge Defense” as working title; no trademark/legal clearance is claimed.

## Not yet verified

This document is not a Unity compile, playtest, Android install, performance audit, security review, or legal/trademark clearance. Those require separate execution and evidence.
