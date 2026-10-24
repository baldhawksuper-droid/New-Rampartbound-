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
- `StageManager` assigns `PuzzlePreparation` to several configured stages and `RealTimeDeploy` to others. The current intended player-facing mode strategy is not yet stated consistently in design docs.
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
- Inspected `Gameplay.jpeg`: bright casual palette and distinct icons, but weak board/background separation, dense 6x6 symbols, inconsistent softness/detail between background and foreground, crowded lower battlefield props, and unclear function of the large blue-gold object.
- Inspected `Pixel Art.jpeg`: detailed pixel-art village/lobby concept with appealing medieval mood, but potentially inconsistent pixel/detail scale, dense decoration, and no clear gameplay/interactive hierarchy in the provided image. The image also contains social-video overlays. Determine whether it is the intended lobby style and align it with the battle art direction.
- These are image-based observations only, not an in-Unity UI audit or approval of final art.

**Still required to finish Sprint 49:**
1. Complete a full visual inventory/audit of lobby, battle, stage map, modal screens, typography, icon style, safe area, and phone aspect-ratio screenshots. `Gameplay.jpeg` and `Pixel Art.jpeg` are only first-pass concept/image references.
2. Refresh build steps against actual Unity menu commands and a tested artifact; pipeline version is 1.2.17 / code 137, while newest named APK found is v1.2.14; a current APK is not evidenced.
3. Reconcile old Sprint history and feature claims with code/config/test evidence; the legacy documents remain reference-only until audited.
4. Check internal links and remaining stale/version/path/role/mode/roster statements in all current docs.
5. Run documentation checks, review diff, commit, push, and verify remote SHA.

**Definition of done:** one canonical approved product-design brief; portable setup instructions verified; visual/UX audit documented with findings and proposals; unverified claims marked; all critical current-doc conflicts fixed or explicitly tracked; `git diff --check` and link/stale-string audit pass; changes committed and pushed with remote SHA verified.

## Remaining owner decisions (not blockers for current doc cleanup)

- Confirm release stage (prototype, closed test, or public release) and supported Android/device baseline.
- Confirm which of IAP, guild, battle pass, cloud save and live events are in near-term scope. Wind Ranger gacha is approved as direction, but economy and real-money implementation still require separate balance/compliance review.
- Keep “Rampartbound: Merge Defense” as working title; no trademark/legal clearance is claimed.

## Not yet verified

This document is not a Unity compile, playtest, Android install, performance audit, security review, or legal/trademark clearance. Those require separate execution and evidence.
