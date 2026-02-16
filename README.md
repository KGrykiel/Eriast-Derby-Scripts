# 🚧 WORK IN PROGRESS 🚧

# Eriast Derby

A simulator for a D&D module centered around vehicular racing with combat. 
It follows a custom ruleset inspired by D&D 5e but heavily modified to fit the racing theme.

Part of a larger project to build a homebrew D&D campaign setting which will see 
the players travel to an abandoned ancient city to compete in a seasonal racing tournament alongside
many other adventurers and glory-seekers. This simulator is being built to allow the Dungeon Master to
automate large scale races across ~40 stages and ~50 NPC vehicles, which would be impossible
to manage manually with pen and paper. The primary focus of this simulator is to act as a
DM tool to help run the session, with possible future expansion to a player-facing application.



---
## Game Mechanics/Rules
- **Racing and Movement**: The race track is composed of
  discrete stages and the objective, as with many races, is
  to reach the finish line first. The way however is
  perilous and filled with hazards, while the other teams
  will be more than happy to use their weapons and make
  sure you don't make it to the end at all. Luckily, you
  have weapons of your own.
- **Vehicle Construction**: Players build their vehicles
  from modular components (chassis, power core, drive,
  weapons, utilities) that can be damaged and destroyed
  independently. Each component has its own stats and
  contributes to the overall vehicle performance.
- **Resource Management**: The vehicles are powered by a
  finite energy resource that must be managed carefully,
  lest you find yourself stranded in the middle of the
  track with no power to accelerate or fire your weapons.
- **Crew System**: Characters with D&D 5e-style attributes,
  skills, and proficiencies sit in vehicle seats operating
  an arbitrary combination of components. Represents the
  players and NPCs piloting the vehicles.
- **Turn-Based Abstract Combat (D20)**: Attack rolls, skill
  checks, and saving throws without gridmap or positional
  movement — JRPG-style encounters with D&D 5e resolution.
  Includes status effects, multi-hit attacks, damage over
  time, and healing alongside custom mechanics.
- **Lane Tactics**: Each stage has multiple lanes with
  different tradeoffs that the vehicles can move between.
  Lane choice affects route, speed, and triggers different
  checks/saves/effects and allows each stage to have its
  own distinct identity.
- **Event Cards**: Semi-random occurrences within stages
  that add flavour and unpredictability. Narrative events
  requiring skill checks and presenting choices, penalties,
  and rewards.


## Architecture

The game mechanics are complex by necessity as they are meant to simulate a D&D experience that in 
regular circumstances would be run by a human DM with no technical limitations. This includes many 
complex and interlocking behaviours like conditional effects, changing state of environment,
multi-vehicle rolls, and more. As such, strong architectural discipline is not vanity, but a necessity. 

The architecture is designed to separate calculation logic from data and state management, while
ensuring that all game rules like damage or skill checks pass through a single entry point and code 
duplication is minimized or preferably completely eliminated.

Moreover the architecture is designed to be **Designer-Friendly**. All game content including skills,
stages, status effects, vehicles and components, can be created and configured directly from
the Unity editor, without looking at a single line of code.



---

## Features

- **Stat & Modifier System**  
  Every attribute and statistic like health and armour class
  can be modified through various game mechanics (skills, status effects, equipment etc.).
  The modifiers are aggregated in **StatCalculator** while keeping note
  of their source for logging and UI tooltips.

- **Effect System**  
  A unified way of handling all kinds of common actions that happen in a
  D&D setting such as dealing damage, applying buffs, draining resources, etc.
  This allows code reuse across multiple sources of such actions like **player skills** or
  **environmental effects** (e.g., from event cards). This system allows for fully
  modular and composable skills and events that can be created and configured
  in the editor.

- **Skill System**  
  System for evaluating and executing skills and abilities. 
  This can include attacks, buffs, debuffs or any combination composed of effects.
  Also includes the logic of **skill checks**, **saving throws** and **attack rolls**, with more
  possibilities in the future.

- **Combat Targeting**  
  Each effect in a skill can specify targeting rules. Those include
  the source, a target entity, the enemy vehicle as a whole,
  multiple targets, and more. This allows for configuration of complex skills
  like "damage the enemy while also applying a status effect to self."

- **Status Effects**  
  Used for handling buffs, debuffs, damage over time and other behaviours 
  common in games of this genre. Fully composable and modular using the effect system.
  Implemented as a **Flyweight pattern** with a template and runtime instances.

- **Vehicles and Components**  
  Vehicles are composed of modular **components**, each with their own stats, health and functionality.
  This allows for creating any sort of vehicle for different playstyles and tradeoffs by
  choosing the components to include. Gives interesting tactical depth for the players
  while also giving an identity to the NPC vehicles. 
  Also allows for progression as the players find or create
  better and more powerful components in the parallel D&D campaign.

- **Seat System**  
  The vehicles are meant to support any sort of crew configuration, be it a 5 player team
  with their own specialisations, or a single driver controlling everything. As such, the
  seat system was developed to control how many characters a vehicle can support and which
  components they operate. It also controls which character will be responsible
  for which skill check and saving throw via a dedicated **routing class**.

- **Turn Management**  
  A **state machine** that controls the flow of the game and progression of the race on a turn-by-turn basis.
  Determines which vehicle goes next, when event cards are drawn, when status effects tick, and so on.

- **Stage & Lane System**  
  The idea behind the race track is to make each stage distinct with its own imagery
  and tactical considerations. Mapping each stage on a typical D&D gridmap
  would not only be too time consuming, but it was also not very fun during early playtesting.
  On the flipside, having simple, uniform and abstract stages felt like it stripped
  away their identity. Therefore a middle ground was devised to include multiple **lanes**
  in each stage, with flavour and tactical implications. Vehicles can move between lanes
  to attain different benefits and drawbacks, while also triggering different checks, saves and events.

  The lane system offers ample opportunity for future expansions, like blocking lanes, more
  complex AoE targeting rules, combat considerations like flanking, conditional hazards, and much more.

- **Event Card System**  
  A complement to the stage and lane system aimed at enforcing stage identity even more, as well as
  adding an element of unpredictability. **Event cards** can happen at any point during the stage and can be 
  They can also include narrative flavour and
  player choices while providing bonuses, penalties, or environmental changes. Vaguely similar
  to events in strategy games like Europa Universalis or Crusader Kings.

- **Event & Logging System**  
  A complex logging system that allows for meticulous recording of everything that happens within a race,
  from attacks, damage, skill checks, to lane changes, event card triggers, and much more. Meant to
  be the ultimate monitoring tool for the DM to keep track of what's going on, while also offering
  excellent debugging capabilities during development. Also includes a full breakdown of all
  calculations with modifiers and their sources.

  In the future, this system could be used to generate a summary of the race and its key moments.

- **UI**  
  A rudimentary UI in Unity used for debugging and display purposes. Offers all the required
  information to understand the state of the game such as the state of every vehicle and its components,
  their positions in the stage and lanes, or a log of all the actions that happened.

- **Test Suite** 
  Automated test covering most core mechanics as well as several more complex integration tests.
  While most testing happens in the Unity editor, the automated tests provide an indication if
  any of the core mechanics might have been broken by a change. The core architecture was 
  designed with testability in mind, so writing automated tests is straightforward and doesn't require any special setup or workarounds.


---

## Core Design Patterns & Key Files

| Pattern | Where | Why |
|---|---|---|
| **Strategy** | [`SkillExecutor`](Skills/Helpers/SkillExecutor.cs) → Resolvers<br/>[`IFormulaProvider`](Combat/Damage/FormulaProviders/IFormulaProvider.cs) → damage formulas | Swap resolution algorithms without touching callers. Five different skill roll types route through the same interface. |
| **Single Source of Truth** | [`StatCalculator`](Core/StatCalculator.cs) — all stat queries<br/>[`D20Calculator`](Combat/D20Calculator.cs) — all d20 rolls<br/>[`DamageApplicator`](Combat/Damage/DamageApplicator.cs) — all damage | Centralized logic guarantees consistency. Every attack, skill check, or damage event flows through one verifiable entry point. |
| **Flyweight** | [`StatusEffect`](StatusEffects/StatusEffect.cs) (template) / [`AppliedStatusEffect`](StatusEffects/AppliedStatusEffect.cs) (instance) | Shared template + per-entity runtime state. Compose modifiers + DoT/HoT + behavioral restrictions without subclass explosion. |
| **State Machine** | [`TurnStateMachine`](Managers/TurnStateMachine.cs) | Tracks current phase and manages transitions. Fires events on phase changes for loose coupling. Pauses execution when waiting for player input. |
| **Chain of Responsibility** | [`ITurnPhaseHandler`](Managers/TurnPhases/ITurnPhaseHandler.cs) → phase handlers | Each handler executes its phase logic and returns the next phase. Returns null to pause execution (player input). Clean separation of phase-specific behavior. |
| **Observer / Event Bus** | [`CombatEventBus`](Combat/CombatEventBus.cs), [`TurnEventBus`](Managers/TurnEventBus.cs) | Scoped action aggregation for multi-event combat logging. Critical for multi-hit attacks that need to log as one action. |
| **Context Object** | [`SkillContext`](Skills/Helpers/SkillContext.cs), [`EffectContext`](Effects/EffectContext.cs), [`FormulaContext`](Combat/Damage/FormulaProviders/FormulaContext.cs) | Bundle execution data, eliminate parameter sprawl. Immutable copy helpers for clean data flow. |
| **ScriptableObject Architecture** | [`Skill`](Skills/Skill.cs), [`StatusEffect`](StatusEffects/StatusEffect.cs), [`Character`](Characters/Character.cs), [`EventCard`](Events/EventCard/EventCard.cs) | Data-driven design — all game content configured in editor, no code changes needed. |
| **Coordinator / Mediator** | [`VehicleComponentCoordinator`](Entities/Vehicle/VehicleComponentCoordinator.cs), [`CheckRouter`](Combat/CheckRouter.cs) | Subsystems interact through coordinator. CheckRouter is the *only* class that knows vehicle internals; calculators stay agnostic. |

---

## Tech Stack

- **Engine:** Unity 6
- **Language:** C# 9.0 / .NET Standard 2.1
- **Testing:** NUnit + Unity Test Framework (PlayMode)
- **Serialization:** `[SerializeReference]` + SR Editor for polymorphic Inspector editing