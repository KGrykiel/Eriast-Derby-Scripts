# 📚 Eriast Derby - Project Technical Documentation

## 🎯 Project Overview

**Eriast Derby** is a turn-based tactical racing game combining D&D 5e-style combat with vehicle racing mechanics. Players control vehicles racing through stages while engaging in combat, managing resources, and navigating hazards.

**Core Concept:** D&D combat meets Mario Kart-style racing. Vehicles move automatically each turn, but players control combat actions, skills, and stage transitions.

---

## 🏗️ System Architecture

### Core Game Loop:
1.	Initialize Race (GameManager) ├─ Place vehicles at starting stage ├─ Roll initiative (determines turn order) └─ Clear event history
2.	Turn Processing (TurnController) ├─ For each vehicle in initiative order: │  ├─ Check if destroyed/invalid → skip │  ├─ If Player: Wait for input, then apply movement │  └─ If AI: Auto-process actions + movement └─ Loop until player turn or no vehicles remain
3.	Player Turn (PlayerController) ├─ Apply movement (add speed to progress) ├─ Show UI: Skills, Targets, Stage Choices ├─ Player selects action ├─ Resolve action (skill/attack) ├─ Handle stage transitions (if progress > stage.length) └─ End turn → call GameManager.OnPlayerTurnComplete()
4.	Post-Turn (GameManager) ├─ Advance turn counter ├─ Refresh all UI panels └─ Resume turn processing
	1. 
---

## 📁 Key Systems

### 1. Event Logging System (RaceHistory)

**Purpose:** Replace simple `SimulationLogger` with structured, filterable event tracking for DM analytics.

**Components:**
- `EventType.cs` - Categorizes events (Combat, Movement, SkillUse, Modifier, etc.)
- `EventImportance.cs` - Prioritizes events (Critical, High, Medium, Low, Debug)
- `RaceEvent.cs` - Data structure for individual events with metadata
- `RaceHistory.cs` - Singleton manager for storing/querying events

**Key Features:**
- **Metadata-rich:** Every event stores context (damage, rolls, sources, targets)
- **Filterable:** DM can filter by importance/type
- **Per-vehicle tracking:** Query all events for specific vehicle
- **Highlight reel ready:** Drama scoring for post-race replays
- **Backwards compatible:** Still logs to `SimulationLogger`

**Usage Pattern:**
RaceHistory.Log( EventType.Combat, EventImportance.High, "Red Racer dealt 12 damage to Bandit", currentStage, attackerVehicle, targetVehicle ).WithMetadata("damage", 12) .WithMetadata("weaponUsed", "Fireball");


**Design Pattern:** Observer + Singleton + Repository patterns combined.

---

### 2. UI System (Multi-Tab DM Interface)

**Purpose:** Provide DM with analytics, leaderboards, and detailed vehicle inspection while keeping gameplay controls accessible.

**Components:**

#### A. TabManager.cs
- Switches between tabs (Focus, Overview, Inspector, Log)
- Highlights active tab
- Hides inactive panels

#### B. FocusPanel.cs
- **Purpose:** Show player vehicle + same-stage combat context
- **Displays:**
  - Player vehicle stats (HP, Energy, Modifiers, Skills)
  - Vehicles in same stage (potential combat targets)
  - Recent high-importance events
- **Refreshes:** On turn changes

#### C. OverviewPanel.cs
- **Purpose:** Race-wide view (leaderboards + critical events)
- **Displays:**
  - Leaderboard (sorted by progress)
  - Critical/High importance events (last 10)
  - Race statistics (total events, combats, destructions)
- **Refreshes:** On turn changes

#### D. VehicleInspectorPanel.cs
- **Purpose:** Deep-dive into any vehicle's state
- **Displays:**
  - Full stats (HP, Energy, Speed, AC, etc.)
  - Active modifiers with durations
  - Skill list with affordability checks
  - Event history (last 10 events)
  - Narrative summary (generated story)
- **Features:**
  - Dropdown to select any vehicle
  - Auto-refreshes on turn changes
  - Manual refresh button
- **Refreshes:** On vehicle selection, turn changes, manual button

#### E. EventFeed.cs
- **Purpose:** Real-time event log with filtering
- **Displays:**
  - Scrollable list of events
  - Color-coded by importance
  - Icons for event types
- **Features:**
  - Filter toggles (Critical, High, Medium, Low, Debug)
  - Auto-scrolls to latest
  - Limits to 100 events for performance
- **Refreshes:** Automatically detects new events in Update()

#### F. UIManager.cs
- **Purpose:** Toggle between Game View and DM Interface
- **Features:**
  - Overlay mode (DM interface on top of game)
  - Hotkey toggle (Tab key)
  - Buttons: "Show DM Interface" / "Back to Game"
  - Sort orders: GameView = 0, DMInterface = 10

**UI Design Decision:**
- **Action happens BEFORE movement** (current implementation)
- Player chooses action → Action resolves → Movement applied → Stage transition
- Matches D&D convention, easier for players to understand

---

### 3. Effect System (Skills & Combat)

**Purpose:** Modular, data-driven effects applied by skills, stages, and event cards.

**Components:**

#### A. IEffect Interface
void Apply(Entity user, Entity target, Object context, Object source);

#### B. Effect Types:

**DamageEffect.cs:**
- Rolls damage dice (e.g., 2d6+3)
- Applies to vehicle
- Logs with dice roll details
- Metadata: `damageRolled`, `damageDice`, `damageDieSize`, `damageBonus`, `source`

**AttributeModifierEffect.cs:**
- Applies stat modifiers (Flat/Percent)
- Duration-based (turns or permanent)
- Can be "local" (removed when leaving stage)
- Logs with duration and source
- Metadata: `modifierType`, `attribute`, `value`, `duration`, `isPositive`

**ResourceRestorationEffect.cs:**
- Restores/drains Health or Energy
- Importance scales with context (critical health = High)
- Logs actual change (not requested amount)
- Metadata: `resourceType`, `actualChange`, `old/new` values

**CustomEffect.cs:**
- Unity Event hook for arbitrary logic
- Logs invocation with listener count
- Used for special mechanics

#### C. EffectInvocation.cs
- Wraps effects with targeting and hit-roll logic
- **Target Modes:** User, Target, Both, AllInStage
- **Hit Rolls:** Optional (requiresRollToHit flag)
- **Miss Logging:**
  - Individual misses logged per target
  - AoE miss summary if all targets missed
  - Importance: Medium (player involved), Low (NPC vs NPC)
- **Returns:** bool (true if any target hit)

**Recent Update:** Added comprehensive miss logging to EffectInvocation:
- Logs each miss with roll type, bonus, effect type
- Aggregates AoE misses into summary log
- Tracks miss count for skills

---

### 4. Skill System

**Purpose:** Define vehicle abilities with energy costs and effects.

**Base Class: Skill.cs**

**Key Features:**
- List of `EffectInvocation` (can have multiple effects per skill)
- Energy cost
- Description
- `Use(Vehicle user, Vehicle target)` method

**Skill Usage Flow:**
1.	Check for null target → Log failure (reason: "NoTarget")
2.	Check for no effects → Log failure (reason: "NoEffects")
3.	For each EffectInvocation: ├─ Apply effect (returns bool) ├─ Track success/miss └─ Aggregate results
4.	If any succeeded: ├─ Log success with effect descriptions └─ Mark partial miss if some missed
5.	If all failed: └─ Log failure (reason: "AllEffectsMissed" or "EffectsInvalid")


**Failure Logging (Recent Update):**
- **No Target:** "Red Racer attempted to use Fireball but there was no valid target"
- **No Effects:** "Red Racer attempted to use Broken Skill but it has no effects configured"
- **All Miss:** "Red Racer used Fireball on Bandit, but all 1 effect(s) missed"
- **Partial Miss:** Logs success + metadata `partialMiss: true, missCount: 2`

**Preset Skills:**
- `AttackSkill` - Auto-configures DamageEffect with hit roll
- `BuffSkill` - Auto-configures AttributeModifierEffect on self
- `RestorationSkill` - Auto-configures ResourceRestorationEffect on self
- `CustomSkill` - Fully manual configuration
- `SpecialSkill` - Auto-configures CustomEffect

---

### 5. Vehicle System

**Core Entity:** Represents player and AI vehicles.

**Key Properties:**
- `vehicleName` - Display name
- `health` / `maxHealth` - HP tracking
- `energy` / `maxEnergy` - Skill resource
- `speed` - Movement per turn
- `armorClass` - Defense rating
- `magicResistance` - Alternate defense
- `energyRegen` - Energy per turn
- `controlType` - Player or AI
- `currentStage` - Current race segment
- `progress` - Distance in current stage
- `skills` - List of available skills
- `Status` - Active or Destroyed

**Attribute System:**
- Base values + modifiers = final value
- `GetAttribute(Attribute)` calculates dynamically
- Supports Flat and Percent modifiers
- Modifiers have duration (turns or permanent)

**Event Logging in Vehicle:**
- **Damage Taken:** Logs with old/new health, health %, thresholds (Bloodied, Critical)
- **Modifier Added/Removed:** Logs with type, attribute, value, duration, source
- **Destruction:** Logs as Critical, detects if leading (tragic moment)
- **Finish Line:** Logs as Critical
- **Energy Regen:** Logs as Debug (low noise)

**Recent Updates:**
- Added comprehensive damage logging with context (Bloodied, Critical Health, Destroyed)
- Added finish line detection
- Added tragic moment detection (destroyed while leading)
- Improved importance calculation based on player involvement

---

### 6. Stage System

**Purpose:** Race track segments with hazards, event cards, and branching paths.

**Key Properties:**
- `stageName` - Display name
- `length` - Distance (in meters)
- `nextStages` - List of connected stages (branching paths)
- `isFinishLine` - Marks end of race
- `onEnterModifiers` - Auto-applied effects (stage hazards)
- `eventCards` - Random events triggered on entry
- `vehiclesInStage` - List of vehicles currently here

**Stage Transitions:**
Vehicle.SetCurrentStage(Stage newStage) ├─ TriggerLeave(oldStage) │  └─ Remove local modifiers from old stage ├─ Update currentStage reference ├─ Move vehicle GameObject to stage position ├─ TriggerEnter(newStage) │  ├─ Apply onEnterModifiers │  ├─ Draw random EventCard │  └─ Check for combat potential (multiple vehicles) └─ Log transition (Debug importance)


**Event Logging in Stage:**
- **Stage Entry:** Medium (player), Debug (NPC)
- **Stage Hazards:** Medium (player), Low (NPC)
- **Event Cards:** High (player), Medium (NPC)
- **Combat Potential:** High (player present), Medium (NPC only)
- **Stage Exit:** Debug

**Combat Potential Detection:**
- When 2+ active vehicles in same stage
- Logs: "[POWER] Red Racer, Bandit are all in Forest Path - combat possible!"
- Helps DM identify engagement opportunities

---

### 7. Turn Controller

**Purpose:** Manages turn order, initiative, and movement processing.

**Key Features:**

**Initiative System:**
Initialize(List<Vehicle> vehicles) ├─ Roll 1d100 for each vehicle ├─ Sort by initiative (descending) ├─ Log turn order └─ Set currentTurnIndex = 0

**Turn Advancement:**
AdvanceTurn() └─ currentTurnIndex = (currentTurnIndex + 1) % vehicles.Count

**Movement Processing:**
- `ProcessMovement(Vehicle)` - Adds speed to progress (no stage change)
- `ProcessAITurn(Vehicle)` - Movement + auto-stage-transitions for AI
- `MoveToStage(Vehicle, Stage)` - Manual stage transition (for player)

**Turn Skip Logic:**
- Skips destroyed vehicles
- Skips vehicles with no stage
- Logs skip reason

**Event Logging in TurnController:**
- Initiative rolls (Low importance)
- Turn order established (Medium)
- Movement (Low importance for player, Debug for NPC)
- Stage transitions (Medium for player, Low for NPC)
- Turn skips (Low)
- Vehicle removal (High)

---

### 8. Game Manager

**Purpose:** Orchestrates game initialization and turn flow.

**Initialization:**
InitializeGame() ├─ Clear RaceHistory ├─ Find all Stages and Vehicles in scene ├─ Place vehicles at entryStage ├─ Initialize TurnController (roll initiative) ├─ Initialize PlayerController ├─ Log race start (High importance) └─ Start NextTurn() loop

**Turn Flow:**
NextTurn() ├─ While vehicles exist: │  ├─ Get current vehicle │  ├─ Skip if invalid │  ├─ If player: │  │  ├─ Apply movement (speed to progress) │  │  ├─ Show PlayerController UI │  │  ├─ Wait for player action │  │  └─ RETURN (pause loop) │  └─ If AI: │     ├─ ProcessAITurn() │     ├─ AdvanceTurn() │     └─ Continue loop └─ Game over if no vehicles


**OnPlayerTurnComplete():**
├─ AdvanceTurn() ├─ RaceHistory.AdvanceTurn() ├─ Refresh all UI panels: │  ├─ RaceLeaderboard │  ├─ VehicleInspectorPanel │  ├─ OverviewPanel │  └─ FocusPanel └─ Resume NextTurn() loop


**UI Panel References:**
- `raceLeaderboard` - Shows sorted standings
- `vehicleInspectorPanel` - Detailed vehicle view
- `overviewPanel` - Race-wide analytics
- `focusPanel` - Player context

**Event Logging in GameManager:**
- Race initialization (High)
- Player turn start (Medium)
- AI turn start (Low)
- Game over (Critical)

---

## 🎨 Design Decisions Log

### 1. Event System Architecture

**Problem:** Simple string logging (`SimulationLogger`) lacked structure, filtering, and context.

**Solution:** Structured event system with:
- Type categorization (Combat, Movement, etc.)
- Importance levels (Critical → Debug)
- Rich metadata (damage, rolls, sources)
- Per-vehicle tracking
- Query methods

**Pattern Used:** Observer + Singleton + Repository

**Reasoning:** DM needs to filter noise (50 vehicles = 1000+ events per race). Importance levels allow showing only Critical/High by default, with option to deep-dive into Debug logs.

---

### 2. UI Panel Refresh Strategy

**Problem:** Real-time auto-refresh (10 FPS) was wasteful for turn-based game.

**Solution:** Refresh only on turn changes (event-driven).

**Implementation:**
- GameManager calls `OnTurnChanged()` on all panels
- Panels expose public refresh methods
- Inspector still has manual refresh button

**Reasoning:** Turn-based game state changes discretely, not continuously. Event-driven refresh is 99% more efficient.

---

### 3. Unicode Characters Removed

**Problem:** Unity TextMeshPro rendered emojis as `????` (font support issue).

**Solution:** Replaced all Unicode with ASCII-safe alternatives:
- `█` → `#` (filled bar)
- `░` → `-` (empty bar)
- `✓` → `[OK]` (status)
- `⚔️` → `[ATK]` (combat)
- `🏁` → `[FINISH]` (finish line)
- Etc.

**Reasoning:** Default Unity fonts don't support special characters. ASCII ensures universal compatibility.

---

### 4. Action Before Movement

**Problem:** Should player act before or after automatic movement?

**Decision:** Actions BEFORE movement (current implementation).

**Reasoning:**
- Predictable targeting (know who's in range)
- Matches D&D 5e convention
- Simpler mental model for players
- Current code already works this way

**Alternative Considered:** Move → Action (rejected, too confusing)
**Future Enhancement:** Hybrid system with Bonus Actions + Reactions

---

### 5. Miss Logging Implementation

**Problem:** Skills that miss don't log to RaceHistory (only SimulationLogger).

**Solution:** Added comprehensive miss logging:
- EffectInvocation logs each miss
- Skill.cs distinguishes failure types (no target, no effects, all miss)
- Importance scales with player involvement

**Reasoning:**
- Players need feedback when skills fail
- DM needs to see NPC attacks on player (even if missed)
- AoE miss summary prevents log spam

---

### 6. Effect Logging Level

**Problem:** Should effects log at High or Debug importance?

**Decision:**
- Effects log at **Debug**
- Higher-level systems (Vehicle, Skill) log at High/Medium

**Reasoning:**
- Avoids duplicate logging (Vehicle.TakeDamage already logs damage)
- Effects are implementation details
- DM cares about outcomes (damage dealt), not mechanics (dice rolled)
- Debug logs still available for troubleshooting

---

### 7. Overlay Mode for DM Interface

**Problem:** DM needs to see analytics but also press game buttons.

**Solution:** Overlay mode (DM Interface renders on top of Game View).

**Implementation:**
- Two canvases with different sort orders
- UIManager toggles active canvas
- Tab key hotkey for quick switching

**Reasoning:**
- DM can press "Next Turn" while viewing leaderboard
- Real-time feedback (see stats change when button pressed)
- More efficient workflow than switching screens

---

## 🔧 Key Code Patterns

### Fluent API for Events:
RaceHistory.Log(type, importance, description, stage, vehicles) .WithMetadata("key", value) .WithMetadata("key2", value2) .WithShortDescription("short");

### Importance Determination Pattern:
private EventImportance DetermineImportance(params) { if (player involved) return High; if (large amount) return Medium; return Low; }

### Effect Application Pattern:
public override void Apply(Entity user, Entity target, ...) { // Apply effect logic vehicle.TakeDamage(damage);
// Log to old system (backwards compat)
SimulationLogger.LogEvent("...");

// Log to new system (structured)
RaceHistory.Log(...).WithMetadata(...);
}


### Null Safety Pattern:
if (component == null) { Debug.LogWarning("Component missing!"); return; }
// Safe to use component

---

## 📊 Current State

### ✅ Completed Systems:
1. Event logging system (RaceHistory)
2. Multi-tab DM interface (5 panels)
3. Effect system with logging (Damage, Modifier, Restoration, Custom)
4. Skill system with failure logging
5. Vehicle event logging (damage, modifiers, destruction, finish)
6. Stage event logging (entry, hazards, event cards, combat potential)
7. Turn controller logging (initiative, movement, transitions)
8. UI refresh on turn changes (event-driven)
9. Miss logging (individual + AoE summary)
10. Overlay mode for DM interface

### 🔄 In Progress:
- None currently

### 📝 Planned Next:
- Revamp character/vehicle skill logic (multi-component system)
- 3D stage map visualization
- Draggable DM interface windows
- Highlight reel generation

---

## 🐛 Known Issues:

1. **EffectInvocation.Apply() may log duplicate misses** - Both EffectInvocation and Skill.cs log misses (by design, but could be redundant)
2. **Unicode character support** - Fully replaced with ASCII, but limits visual polish
3. **Vehicle.DetermineIfLeading()** - Uses placeholder logic (progress > 50m), should integrate with RaceLeaderboard
4. **Skill damage estimation** - Uses average dice value, not actual rolled value (DamageEffect doesn't return damage dealt)

---

## 📚 Important Files Reference

### Core Systems:
- `GameManager.cs` - Game orchestration, turn loop
- `TurnController.cs` - Turn order, initiative, movement
- `PlayerController.cs` - Player input, UI control

### Event System:
- `RaceHistory.cs` - Singleton event manager (148 lines)
- `RaceEvent.cs` - Event data structure with metadata (162 lines)
- `EventType.cs` - Event categorization enum (35 lines)
- `EventImportance.cs` - Priority levels enum (32 lines)

### UI System:
- `UIManager.cs` - Canvas switching, overlay mode (82 lines)
- `TabManager.cs` - Tab switching logic (62 lines)
- `FocusPanel.cs` - Player context panel (200+ lines)
- `OverviewPanel.cs` - Race-wide analytics (180+ lines)
- `VehicleInspectorPanel.cs` - Vehicle details (260+ lines)
- `EventFeed.cs` - Filtered event log (120+ lines)
- `RaceLeaderboard.cs` - Live standings (legacy, still used)

### Entities:
- `Vehicle.cs` - Player/NPC vehicle (350+ lines with logging)
- `Entity.cs` - Base class for combatants (30 lines)

### Effects:
- `IEffect.cs` - Effect interface (8 lines)
- `EffectBase.cs` - Abstract base (8 lines)
- `EffectInvocation.cs` - Effect wrapper with targeting (130+ lines with miss logging)
- `DamageEffect.cs` - Damage application (45 lines)
- `AttributeModifierEffect.cs` - Stat modifiers (65 lines)
- `ResourceRestorationEffect.cs` - HP/Energy restore (100+ lines)
- `CustomEffect.cs` - Unity Event hook (30 lines)

### Skills:
- `Skill.cs` - Base skill class (200+ lines with failure logging)
- `AttackSkill.cs` - Preset attack (28 lines)
- `BuffSkill.cs` - Preset buff (24 lines)
- `RestorationSkill.cs` - Preset healing (25 lines)
- `CustomSkill.cs` - Manual config (10 lines)
- `SpecialSkill.cs` - Custom effects (24 lines)

### Stages:
- `Stage.cs` - Race segment (170+ lines with logging)
- `EventCard.cs` - Random events (abstract base)
- `RollEventCard.cs` - Dice-based events

### Utilities:
- `RollUtility.cs` - Dice rolling, hit checks
- `SimulationLogger.cs` - Legacy string logging (still used)
- `SimulationMonitor.cs` - Old debug UI (still exists)

---

## 🎯 Key Metrics:

- **Total Scripts:** ~40 files
- **Total Lines (estimated):** ~5000 LOC
- **Event Types:** 13 (Combat, Movement, SkillUse, etc.)
- **Importance Levels:** 5 (Critical → Debug)
- **UI Panels:** 5 (Focus, Overview, Inspector, EventFeed, Leaderboard)
- **Effect Types:** 4 (Damage, Modifier, Restoration, Custom)
- **Skill Presets:** 5 (Attack, Buff, Restoration, Custom, Special)
- **Entities:** 2 (Vehicle, Entity base)

---

## 💡 Design Philosophy:

1. **DM-First:** Every system designed to give DM maximum visibility and control
2. **Event-Driven:** All state changes generate structured events
3. **Metadata-Rich:** Every event stores context for analysis/replay
4. **Backwards Compatible:** New systems coexist with old (SimulationLogger)
5. **Importance-Scaled:** Events prioritized by player involvement and impact
6. **Modular:** Systems loosely coupled (Effect ← Skill ← Vehicle ← Turn ← Game)
7. **Debuggable:** Debug-level logs available for troubleshooting without noise

---

## 🚀 Next Session Priorities:

1. **Revamp Vehicle Skill Logic** (current discussion topic)
2. Consider multi-component vehicle system (Driver, Gunner, Engineer)
3. Implement role-based action panels
4. Add 3D stage map visualization
5. Optimize UI refresh performance at scale (50 vehicles, 40 stages)

---

**END OF DOCUMENT**