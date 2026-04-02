# CLAUDE.md

## Project Overview

**EverRealm: Exiles** is a medieval voxel extraction game built in **Unity**.

It is a focused spin-off of the broader **EverRealm** vision and acts as both:
- a standalone game with a tight, replayable extraction loop
- a proving ground for core systems that may later feed into the full EverRealm experience

The design combines:
- **Minecraft-style procedural voxel worlds** with destructible terrain, resource gathering, and hidden loot
- **Cube World-style combat** with responsive movement, dodging, multiple weapon archetypes, and dynamic PvE/PvP encounters
- **Extraction gameplay** centered on short, high-tension runs where players must loot, survive, and escape

## Core Fantasy

Players enter a hostile, procedurally generated medieval voxel map, scavenge valuable loot, fight enemies and other players, then extract before losing everything.

Every run should create meaningful decisions:
- push deeper for higher-value rewards
- play safe and extract early
- risk combat for better gear
- use knowledge of terrain, mobility, and extraction routes to survive

## Core Gameplay Loop

1. **Prepare loadout**
   - choose gear, weapons, consumables, and perks/upgrades unlocked through progression
2. **Drop into a generated map**
   - map contains resources, hidden caches, dungeons, enemy camps, traversal challenges, and extraction points
3. **Loot and fight**
   - gather resources, defeat enemies, find upgrades, and survive player or world threats
4. **Decide when to extract**
   - extraction points create pressure, routing decisions, and conflict
5. **Persist rewards / lose carried gear**
   - successful extractions fuel long-term progression
   - failed runs should carry meaningful consequences
6. **Upgrade and re-enter**
   - unlock new options, refine build choices, and improve mastery

## Design Pillars

### 1. Tight, Readable Combat
Combat must feel responsive, skill-based, and easy to read.

Priorities:
- low input latency
- clear hit timing and feedback
- distinct weapon identities
- dodge/mobility that rewards timing and positioning
- enemies with understandable behaviors and punish windows

### 2. Strong Run Variety
Each run should feel fresh without becoming noisy or unfocused.

Priorities:
- procedurally generated maps with readable structure
- meaningful POI distribution
- varied risk zones and loot density
- replayable encounter combinations
- strong route-planning decisions

### 3. High-Stakes Extraction Tension
The extraction loop is the heart of the game.

Priorities:
- valuable loot should create tension
- extraction should be a strategic commitment, not a trivial exit button
- players should regularly face risk-vs-reward decisions
- losses should matter, but not feel random or unfair

### 4. Voxel World Utility
Voxel terrain is not just aesthetic; it should affect gameplay.

Priorities:
- terrain readability
- destructibility where it adds tactical value
- traversal opportunities
- hidden spaces, shortcuts, and ambush routes
- resource harvesting that supports the run economy

### 5. Reusable EverRealm Foundations
Systems built here should be modular and extensible.

Priorities:
- reusable combat architecture
- reusable procedural generation tooling
- data-driven content definitions
- minimal hard-coded gameplay assumptions

### 6. AI and Encounters
Enemies should support the extraction loop by creating pressure and tactical variety.

Priorities:
- readable behavior states
- different engagement ranges and movement profiles
- synergy with terrain and POIs
- quick understanding, slow mastery

## Product Scope

This project is **not** trying to be a full sandbox RPG.

### What Exiles is
- a session-based extraction game
- a focused testbed for combat and procedural content systems
- a replayable loop with meaningful persistence between runs

### What Exiles is not
- a massive open-world sandbox
- a fully simulation-heavy survival game
- a content-bloated RPG with sprawling questlines
- a building-centric experience like Minecraft proper

When in doubt, choose the option that improves:
- session quality
- combat feel
- extraction tension
- iteration speed

## Technical Direction

### Tech Stack
- **Engine**: Unity 6.3 LTS
- **Editor Version**: 6000.3.8f1
- **Language**: C#
- **Rendering**: URP (Universal Render Pipeline) — good fit for stylized voxel art

### Expected Technical Priorities
- deterministic-feeling combat responsiveness
- scalable procedural voxel generation
- performant terrain chunking/streaming
- modular gameplay systems
- data-driven content authoring where practical
- clean separation between runtime systems and content definitions

## Performance Guidelines

Because this is a voxel-based Unity project, performance is a first-class concern.

Prioritize:
- chunk-based world management
- minimizing per-frame allocations
- careful physics usage
- batching where possible
- efficient AI updates
- profiling before and after major system changes
- greedy meshing

Any feature touching terrain, generation, AI counts, or combat effects should consider performance impact from the start.

## Code Style Expectations

### General
- write clear, maintainable C#
- favor explicit naming over clever abstractions
- keep classes focused on one responsibility
- avoid giant “manager” classes that own unrelated systems
- document non-obvious decisions

### Unity-specific
- keep MonoBehaviours thin where possible
- isolate pure logic into testable C# classes/services
- use ScriptableObjects for authoring configuration/data when appropriate
- avoid hidden inspector coupling unless clearly intentional
- be careful with Update-heavy patterns; prefer event-driven or scheduled approaches when reasonable

### Maintainability
When adding new systems, leave behind:
- concise comments for tricky logic
- clear setup instructions if tooling is involved
- minimal but useful documentation for content pipelines

### Code Conventions

- **Naming**: PascalCase for classes, methods, properties. camelCase for local variables and parameters. _camelCase for private fields.
- **Architecture**: Prefer composition over inheritance. Use ScriptableObjects for data-driven design (items, weapons, enemies, loot tables).
- **MonoBehaviour usage**: Keep MonoBehaviours thin — delegate logic to plain C# classes where possible.
- **Namespaces**: Use `EverRealm.Exiles.<System>` (e.g., `EverRealm.Exiles.World`, `EverRealm.Exiles.Combat`).
- **Comments**: Summarize *why*, not *what*. XML doc comments on public APIs.

## Prototyping Rules

When prototyping:
- get the feel working first
- keep code disposable when exploring uncertain mechanics
- once a mechanic proves valuable, refactor it into production-quality architecture

Do not prematurely polish experimental code.
Do not leave proven systems in prototype-quality shape.

## AI Assistant Guidance

When helping on this repository:

### Do
- preserve the core identity: **medieval voxel extraction game**
- optimize for tight gameplay loops and rapid iteration
- favor solutions that improve combat feel, map readability, and extraction tension
- suggest modular, reusable systems
- keep the project’s scoped nature in mind
- think in terms of player-facing impact, not only technical neatness

### Don’t
- turn the project into an MMO, survival simulator, or open-world sandbox
- add unnecessary systemic complexity without a direct gameplay payoff
- propose features that dilute the extraction loop
- overbuild tools or frameworks before the need is proven
- ignore performance implications of voxel and combat-heavy systems

## Development Approach

This project is in early development. Prioritize getting core systems playable over polish:
1. Voxel terrain generation (walkable world)
2. Player controller (movement + camera)
3. Basic combat (one weapon, one enemy type)
4. Loot pickup system
5. Extraction flow (extract point + run end)
6. Iterate and expand from there

## Notes

- This is a solo/small-team project — keep scope tight per feature
- Systems built here are intended to eventually port into the full EverRealm open-world game
- When in doubt, build modular and data-driven so systems transfer cleanly