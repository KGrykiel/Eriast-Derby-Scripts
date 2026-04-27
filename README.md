# 🚧 WORK IN PROGRESS 🚧
Note: This project is tightly coupled to Unity and does not work without it. The repository is created for display and version control of the scripts.

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
  from modular components (chassis, power core, drive, weapons,
  navigator, technician, and custom utility components) that can
  be damaged and destroyed independently. Each component has its
  own stats and contributes to the overall vehicle performance.
  Vehicle size (Tiny to Huge) applies automatic AC and mobility
  modifiers to balance larger vehicles against smaller, more
  agile ones.
- **Resource Management**: The vehicles are powered by a
  finite energy resource that must be managed carefully,
  lest you find yourself stranded in the middle of the
  track with no power to accelerate or fire your weapons.
- **Crew System**: Characters with D&D 5e-style attributes,
  skills, and proficiencies sit in vehicle seats operating
  an arbitrary combination of components. Represents the
  players and NPCs piloting the vehicles.
- **Turn-Based Abstract Combat (D20)**: Attack rolls, skill
  checks, saving throws, and opposed checks without gridmap or
  positional movement -- JRPG-style encounters with D&D 5e resolution.
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
  Every attribute and statistic like health and armour class can be modified through various game
  mechanics (skills, status effects, equipment, etc.). Three dedicated calculators own this domain:
  **StatCalculator** for entity attributes, **CharacterStatCalculator** for character scores and
  proficiency bonuses, and **ModifierCalculator** for shared D&D application-order logic
  (base -> Flat -> Multiplier). **DynamicModifierEvaluator** handles contextual on-the-fly modifiers
  (e.g. speed-to-AC bonus) that are gathered at query time rather than stored on the entity.
  All breakdowns include modifier sources for UI tooltips and logging.

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
  Skills are resolved through **RollNodes** -- self-contained resolution steps that make a roll,
  apply success or failure effects, and optionally chain into further nodes. This allows for
  multi-step conditional skills entirely configured in the editor, with no code changes.
  Nine roll types are supported via **IRollSpec**: attack, save, vehicle save, skill check,
  vehicle skill check, character skill check, character save, opposed check, and state threshold
  (a codition-based check with no dice roll). Skills also declare their **action cost** (Action,
  Bonus Action, Free, etc.) and any **costs** (energy, consumables) that are validated and paid
  before execution. **SkillPipeline** is the shared entry point for all skill execution regardless
  of source (player or AI).

- **Combat Targeting**  
  Each effect in a skill can specify targeting rules. Eight targeting modes are supported:
  self, own lane, enemy vehicle, enemy component, any vehicle, any component, component on self,
  and lane. This allows for configuration of complex skills like "damage the enemy while also
  applying a status effect to self", or lane-wide AoE effects.

- **Condition System**  
  A three-tier system for handling buffs, debuffs, damage over time and other behaviours
  common in games of this genre:
  - **Entity conditions** target individual components (modifiers, periodic effects, feature-gated targeting validation).
  - **Vehicle conditions** live on the vehicle as a whole; modifiers are routed to their owning
    components on activation and periodic effects apply to all components each tick.
  - **Character conditions** affect crew members in seats (modifiers only, no periodic effects).

  All tiers support behavioural restrictions (e.g. preventing actions) and advantage/disadvantage
  grants. Fully composable and modular using the effect system. Implemented as a **Flyweight pattern**
  with templates and per-owner managers using **Template Method** for shared stacking/expiry logic.

- **Vehicles and Components**  
  Vehicles are composed of modular **components**, each with their own stats, health and functionality.
  Components can provide modifiers to other components (e.g. an armour upgrade buffing the chassis)
  and declare targeting exposure rules. Vehicle **size category** (Tiny to Huge) applies automatic
  AC and mobility modifiers at initialisation. This allows for creating any sort of vehicle for
  different playstyles and tradeoffs by choosing the components to include, gives interesting
  tactical depth for the players, and also gives an identity to the NPC vehicles.
  Also allows for progression as the players find or create
  better and more powerful components in the parallel D&D campaign.

- **Seat System**  
  The vehicles are meant to support any sort of crew configuration, be it a 5 player team
  with their own specialisations, or a single driver controlling everything. As such, the
  seat system was developed to control how many characters a vehicle can support and which
  components they operate. It also controls which character will be responsible
  for which skill check and saving throw via a dedicated **routing class**.
  Seats also track per-turn **action economy** (Action, Bonus Action, etc.) and carry
  **consumable access** flags that restrict which item categories the occupant can physically reach.

- **Items & Consumables**  
  Vehicles carry an inventory of consumable items capped by chassis cargo capacity.
  **CombatConsumables** (grenades, explosives) require a seat with exterior access;
  **UtilityConsumables** (potions, stims) can be used from any utility-access seat.
  **AmmunitionType** is a special item that loads into a weapon and fires an `onHitNode` effect
  after a successful `WeaponAttackSkill` hit, consuming one charge. Consumable spending is
  validated and executed through **VehicleInventoryCoordinator**.

- **AI System**  
  Each AI vehicle has a **VehicleAIComponent** that coordinates per-seat decision making.
  Each seat runs a full four-stage pipeline via **SeatAI**: Perception (ThreatTracker,
  OpportunityTracker) -> Scoring (CommandWeights + SkillScorer) -> Selection (ArgMax over
  skill x target pairs) -> Execution (SkillPipeline). **SkillScorer** walks the RollNode tree
  and scores each skill as a dot product against a four-axis utility vector (attack, heal,
  disrupt, flee). **PersonalityProfile** is a designer-friendly ScriptableObject with five
  named presets (Ruthless, Honourable, Cunning, Reckless, Defensive) that modulate how
  sensitive a seat is to each axis.

- **Turn Management**  
  A layered system controlling the flow of the game on a turn-by-turn basis:
  - **TurnStateMachine** is a pure phase FSM (RoundStart -> TurnStart -> Action -> TurnEnd -> RoundEnd).
  - **IVehicleTurnController** with **PlayerTurnController** and **AITurnController** implementations
    handles the Action phase for each control type, keeping player input pausing and AI step logic
    cleanly separated.
  - **TurnService** owns the vehicle roster and provides targeting queries (e.g. vehicles in the
    same stage).
  - **VehicleActionManager** manages per-action execution and playback delay.
  - **TurnPhaseContext** is a context object passed to every phase handler, holding references to
    all top-level systems.

- **Stage & Lane System**  
  The idea behind the race track is to make each stage distinct with its own imagery
  and tactical considerations. Mapping each stage on a typical D&D gridmap
  would not only be too time consuming, but it was also not very fun during early playtesting.
  On the flipside, having simple, uniform and abstract stages felt like it stripped
  away their identity. Therefore a middle ground was devised to include multiple **lanes**
  in each stage, with flavour and tactical implications. Vehicles can move between lanes
  to attain different benefits and drawbacks, while also triggering different checks, saves and events.
  **TrackDefinition** is a ScriptableObject that defines explicit lane-to-lane connections between
  stages, acting as the single source of truth for course topology.

  The lane system offers ample opportunity for future expansions, like blocking lanes, more
  complex AoE targeting rules, combat considerations like flanking, conditional hazards, and much more.

- **Event Card System**  
  A complement to the stage and lane system aimed at enforcing stage identity even more, as well as
  adding an element of unpredictability. **Event cards** can trigger at any point during a stage
  and can include narrative flavour, player choices, bonuses, penalties, or environmental changes.
  Vaguely similar to events in strategy games like Europa Universalis or Crusader Kings.
  Player vehicles show a UI choice prompt; AI vehicles resolve automatically (currently defaulting
  to the first available option, with hooks in place for smarter selection later).

- **Event & Logging System**  
  A layered logging system that records everything that happens within a race, from attacks,
  damage, and skill checks, to lane changes, event card triggers, and movement. Two dedicated
  event buses keep concerns separated: **CombatEventBus** handles scoped combat action aggregation
  (grouping multi-hit attacks into one log entry) and **TurnEventBus** handles turn lifecycle events
  (movement, stage transitions, round start/end, etc.). **RaceHistory** is the central event store.
  All calculation breakdowns include modifier sources for the DM to inspect.

  In the future, this system could be used to generate a narrative summary of the race and its key moments.

- **Statistics System**  
  **RaceStatsTracker** subscribes to both event buses and accumulates per-vehicle and race-wide
  statistics (attack hits/misses, nat 20s/1s, damage dealt and received, saving throw pass rates,
  conditions inflicted, skill use counts). Results are stored in **VehicleMetrics** snapshots and
  can be displayed in the post-race stats panel.

- **UI**  
  A rudimentary UI in Unity used for debugging and display purposes. Offers all the required
  information to understand the state of the game such as the state of every vehicle and its components,
  their positions in the stage and lanes, a log of all the actions that happened, and a post-race
  statistics breakdown.

- **Test Suite**  
  300+ automated tests covering core mechanics and integration scenarios: damage calculation,
  stat modifiers, status effects, combat rolls, advantage/disadvantage, vehicle physics, component
  destruction, action economy, targeting, effect routing, consumables, race management, check
  routing, and more. The architecture was designed with testability in mind, so tests require no
  special setup or workarounds.


---

## Core Design Patterns & Key Files

| Pattern | Where | Why |
|---|---|---|
| **Strategy** | [`RollNodeExecutor`](Combat/Rolls/RollSpecs/RollNodeExecutor.cs) -> [`IRollSpec`](Combat/Rolls/RollSpecs/IRollSpec.cs)<br/>[`IFormulaProvider`](Combat/Damage/FormulaProviders/IFormulaProvider.cs) -> damage formulas | Swap resolution algorithms without touching callers. Nine roll types (attack, save, skill check, opposed check, state threshold, and variants) route through the same interface. |
| **Single Source of Truth** | [`StatCalculator`](Core/StatCalculator.cs) -- entity stats<br/>[`CharacterStatCalculator`](Core/CharacterStatCalculator.cs) -- character stats<br/>[`D20Calculator`](Combat/Rolls/D20Calculator.cs) -- all d20 rolls<br/>[`DamageApplicator`](Combat/Damage/DamageApplicator.cs) -- all damage<br/>[`RestorationApplicator`](Combat/Restoration/RestorationApplicator.cs) -- all healing/drain | Centralised logic guarantees consistency. Every attack, skill check, damage event, or restoration flows through one verifiable entry point. |
| **Flyweight** | [`EntityCondition`](Conditions/EntityConditions/EntityCondition.cs) (template) / [`AppliedEntityCondition`](Conditions/EntityConditions/AppliedEntityCondition.cs) (instance) | Shared template + per-entity runtime state. Compose modifiers + DoT/HoT + behavioural restrictions without subclass explosion. Same pattern applies to `VehicleCondition` and `CharacterCondition`. |
| **State Machine** | [`TurnStateMachine`](Managers/Turn/TurnStateMachine.cs) | Tracks current phase and manages transitions. Fires events on phase changes for loose coupling. Pauses execution when waiting for player input. |
| **Chain of Responsibility** | [`ITurnPhaseHandler`](Managers/Turn/TurnPhases/ITurnPhaseHandler.cs) -> phase handlers | Each handler executes its phase logic and returns the next phase. Returns null to pause execution (player input). Clean separation of phase-specific behaviour. |
| **Template Method** | [`ConditionManagerBase<T,U>`](Conditions/ConditionManagerBase.cs) -> concrete managers | Stacking/expiry algorithms in base class. Subclasses override hooks (`OnTick`, `OnExpired`, `OnDeactivate`, etc.) for entity vs. character-specific behaviour. Eliminates duplicate control flow. |
| **Observer / Event Bus** | [`CombatEventBus`](Combat/CombatEventBus.cs), [`TurnEventBus`](Managers/Turn/TurnEventBus.cs) | Scoped action aggregation for multi-event combat logging. Critical for multi-hit attacks that need to log as one action. `SkillPipeline.OnSkillUsed` feeds the statistics system. |
| **Context Object** | [`RollContext`](Combat/Rolls/RollSpecs/RollContext.cs), [`EffectContext`](Effects/EffectContext.cs), [`FormulaContext`](Combat/Damage/FormulaProviders/FormulaContext.cs), [`TurnPhaseContext`](Managers/Turn/TurnPhases/ITurnPhaseHandler.cs) | Bundle execution data, eliminate parameter sprawl. |
| **Facade** | [`GameManager`](Managers/GameManager.cs) | Single initialisation and coordination point. Wires up all subsystems and delegates to them without exposing internals. |
| **Registry / Self-Registration** | [`RacePositionTracker`](Managers/Race/RacePositionTracker.cs), [`TrackDefinition`](Stages/TrackDefinition.cs) | Vehicles and stages register themselves on `OnEnable`/`OnDisable`. Centralises position and topology queries without requiring explicit wiring. |
| **ScriptableObject Architecture** | [`Skill`](Skills/Skill.cs), [`EntityCondition`](Conditions/EntityConditions/EntityCondition.cs), [`Character`](Characters/Character.cs), [`EventCard`](Events/EventCard/EventCard.cs), [`PersonalityProfile`](AI/Personality/PersonalityProfile.cs) | Data-driven design -- all game content configured in editor, no code changes needed. |

---

## Tech Stack

- **Engine:** Unity 6
- **Language:** C# 9.0 / .NET Standard 2.1
- **Testing:** NUnit + Unity Test Framework (PlayMode)
- **Serialization:** `[SerializeReference]` + SR Editor for polymorphic Inspector editing
