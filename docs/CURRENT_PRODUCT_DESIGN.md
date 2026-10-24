# Rampartbound: Merge Defense — Current Product Design

**Status:** Owner-approved direction; implementation and full-screen UX still under audit
**Updated:** 2026-10-24
**Owner:** Project owner (final product decisions)
**AI project lead:** Hermes Agent
**Project status/source of truth:** [`PROJECT_STATUS.md`](PROJECT_STATUS.md)

## Product promise

A mobile fantasy defense game where each campaign level asks the player to prepare or deploy a small hero team, make meaningful merge/grid decisions, and protect the Citadel from waves of enemies. The campaign is hybrid by level: a level selects its intended battle mode; the game does not force both modes into every level.

## Campaign modes

### Puzzle Preparation

The player uses a limited move budget to manipulate the board and create/evolve units before defense begins. When preparation ends, surviving units defend automatically through the configured wave sequence. The UI must show remaining moves, match/merge feedback, upcoming threat context, and a clear start/phase transition.

### Real-Time Deploy

The player summons units using the level's battle economy and makes deployment/merge decisions while waves are active. The UI must make summon cost, affordability, board capacity, and incoming threats legible without obscuring combat.

### Hybrid campaign rule

These are design requirements, not verified current UI or behavior:

- Assign one primary mode per stage in stage data.
- Communicate the mode before the player commits to a stage and in the battle HUD.
- Teach each mode separately before introducing complexity.
- Reuse the game's hero identity and progression between modes, but do not assume identical resource/move rules.
- Current code sets `PuzzlePreparation` as the default `LevelConfigSO` value. `StageManager` assigns both modes across stages. Review exact stage assignments during the gameplay audit; the default value alone is not the campaign specification.

## Hero roster and acquisition

- **Starting trio:** Cedric (melee damage), Rose Tanker (frontline durability/control), Rabbit Mage (ranged area damage/control).
- **Summon/gacha unlock:** Wind Ranger (long-range physical damage). Treat as an additional unlock, not a required tutorial hero or one of the starting three.
- **Lobby presentation requirement:** Give the starting trio the primary roster space. Present Wind Ranger in a visibly distinct locked/featured summon treatment; do not show her as an equally owned starter card. This is a design requirement, not a verified current UI state.
- Any other hero acquisition, probabilities, pity, duplicate conversion, pricing, and real-money purchase claims remain out of scope until separately approved and reviewed for platform/legal compliance.

## Visual direction and UX principles

`Gameplay.jpeg` is a single image reference, not proof of runtime behavior or approval of the full art direction. The separate `Pixel Art.jpeg` appears to be a third-party/ad-feed image and is not verified as Rampartbound-owned art; do not use it as evidence of the game's style.

Initial observations from `Gameplay.jpeg`:
- The colorful tokens have distinguishable silhouettes, but the green board blends into the field and cell boundaries/states are subtle; increase board and tile-state contrast using visual cues beyond color alone.
- The screenshot does not show an objective, mode/phase, remaining moves/economy, upcoming wave, or castle health; verify if these are intentionally omitted/cropped, then make the battle HUD readable and clear.
- Fence, cropped side structures, and the large blue/gold object below the board compete with the grid; label their function through clear gameplay feedback or reduce their prominence.
- Define consistent original rules for palette, outlines, shading, and detail density using project-owned assets; do not infer the lobby style from `Pixel Art.jpeg`.

These are visual-audit findings and recommendations, not approved asset changes. Audit actual runtime screens (lobby, battle, map, panels, overlays) across portrait aspect ratios before a broad visual overhaul.

## Static source and asset review (not runtime verification)

Inspection of `RampartboundSceneSetup.cs`, `UICitadelLobbyView`, and selected project-owned art suggests:

- The project has a hybrid art treatment: compact/angular hero sprites and blocky backdrop forms sit beside a painterly, gradient-rich title illustration. Describe this as a mixed/retro-inspired fantasy style for now; the assets do not establish a consistent strict pixel-art direction.
- The generated lobby source gives four hero cards similar layout prominence. The three approved starters should be the main roster; Wind Ranger should be clearly presented as a distinct locked/featured summon unlock. This remains a design requirement, not a claim about the rendered screen or actual ownership logic.
- The source assigns different rarity frames to the starter heroes (Cedric/Rose Rare, Rabbit Mage Epic) and Wind Ranger Legendary while displaying comparable level/star text. Explain the progression/rarity meaning or standardize it so card treatments do not suggest unsupported starting advantages.
- The generated lobby uses many flat tinted buttons and emoji/text next to detailed fantasy frames/art. Define a consistent control treatment and verify emoji/font legibility across target devices.

These are recommendations from selected source and asset inspection only. They do not verify layout, binding, state, legibility, or input behavior at runtime. Prioritize a cohesive art/UI pass after the owner approves the intended visual direction.

## First-session experience

1. Explain the defense goal and castle health.
2. Teach the selected level's battle mode with a short, actionable cue.
3. Let the player perform one clearly signposted move/summon/merge.
4. Preview the next threat and explain the preparation-to-combat transition.
5. End the first battle with a clear outcome, cause of success/failure, and one useful next action.

Do not show every meta-game system in the tutorial. Unlock secondary systems progressively after the core loop is understood.

## Scope guardrails

For the next iteration, prioritize clarity and replayable decisions in the campaign battle loop. Treat guilds, battle pass, live events, IAP, cloud synchronization, deep gear/relic/talent layers, and endless modes as existing code/design claims that need validation and product-scope approval—not automatic launch requirements.

## Historical design documents

The following documents contain superseded or conflicting concepts and are retained as historical reference until individually reconciled:

- [`MASTER_GAME_DESIGN_DOCUMENT.md`](MASTER_GAME_DESIGN_DOCUMENT.md)
- [`GDD_CORE_GAMEPLAY_MECHANICS.md`](GDD_CORE_GAMEPLAY_MECHANICS.md)
- [`GDD_LOBBY_CONCEPT.md`](GDD_LOBBY_CONCEPT.md)

When they conflict with this document's owner-approved direction, use this document for current product direction. For implementation facts, inspect code/assets/configuration and record evidence in `PROJECT_STATUS.md`; do not treat either GDD as proof that a feature is implemented or tested.
