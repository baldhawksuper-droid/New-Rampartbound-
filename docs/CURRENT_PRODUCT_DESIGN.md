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

- Assign one primary mode per stage in stage data.
- Communicate the mode before the player commits to a stage and in the battle HUD.
- Teach each mode separately before introducing complexity.
- Reuse the game's hero identity and progression between modes, but do not assume identical resource/move rules.
- Current code sets `PuzzlePreparation` as the default `LevelConfigSO` value. `StageManager` assigns both modes across stages. Review exact stage assignments during the gameplay audit; the default value alone is not the campaign specification.

## Hero roster and acquisition

- **Starting trio:** Cedric (melee damage), Rose Tanker (frontline durability/control), Rabbit Mage (ranged area damage/control).
- **Summon/gacha unlock:** Wind Ranger (long-range physical damage). Treat as an additional unlock, not a required tutorial hero or one of the starting three.
- Any other hero acquisition, probabilities, pity, duplicate conversion, pricing, and real-money purchase claims remain out of scope until separately approved and reviewed for platform/legal compliance.

## Visual direction and UX principles

The available `Gameplay.jpeg` screenshot establishes a colorful, friendly fantasy-puzzle direction, but is only one gameplay view. It does not approve the complete art direction or prove runtime behavior.

Initial observations from that screenshot:
- Retain colorful, friendly fantasy mood and distinct piece silhouettes.
- Increase separation between the green board and grassy background; strengthen tile-state contrast while preserving readability for color-vision differences.
- Reduce competing visual weight around the board. The fence-side structures and large blue/gold object below it need unmistakable gameplay meaning or less prominence.
- Harmonize the soft background with the detailed, strongly shaded board pieces and foreground props.
- Keep the grid the primary focal area; show only essential objective, phase, move/economy, wave, and castle-health information at readable sizes.

These are review findings, not approved asset changes. Complete an audit of lobby, battle, stage map, feature panels, overlays, and multiple portrait aspect ratios before proposing a broad visual overhaul.

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
