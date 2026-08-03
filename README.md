# Risky Stats

A BepInEx plugin for **Risk of Rain 2** that adds a live, customizable combat stats HUD overlay - attack speed, armor, crit chance, healing, damage, streaks, damage taken, and movement speed - plus an optional "Risky Stats+" panel with extra stats, an in-game settings system accessible from the pause menu, and a hold-to-view run progress panel showing your totals for the run so far.

---

## Features

- **Live stat panel** overlaid on the HUD showing:
  - Attack Speed
  - Armor
  - Crit Chance
  - Healing (rolling streak)
  - Damage (last hit, colored red on crit)
  - Damage Streak (rolling total)
  - Damage Taken (last hit)
  - Damage Taken Streak (rolling total)
  - Movement Speed
- **Risky Stats+ panel** - a second, opt-in HUD panel (everything off by default) anchored to the bottom right of the screen, just above your ability icons. As you enable more of its stats, the panel grows upward:
  - Jumps (max jumps / jumps used, e.g. `4/3`)
  - Mountain Shrines (how many Shrines of the Mountain you've activated this run)
  - Drones (how many drones you currently have alive)
  - Luck (57 Leaf Clover count)
  - Kills (total monsters killed this run)
- **Toggle key** (default `V`) to show/hide the main stats panel instantly. The Risky Stats+ panel is always visible once you've enabled at least one of its stats.
- **Run progress panel** (default hold `B`) showing your cumulative totals for the whole run so far, not per stage:
  - Total Damage
  - Total Damage Taken
  - Total Healing
  - Maximum Speed Achieved
  - Biggest Single Hit (colored red if it was a crit)
- **In-game settings panel**, added as a button in the pause menu, letting you:
  - Show/hide each stat individually
  - Adjust text size (14-48)
  - Adjust spacing between stats (5-100)
  - Switch panel layout between **Horizontal** and **Vertical**
  - When Vertical is selected, choose **Text Align** (Center or Left)
  - **Reset to Default** - restores all settings in one click, for both the main panel and Risky Stats+
  - A yellow **Next** button in the bottom right of the screen switches over to the **Risky Stats+ Settings** panel, which has its own toggles, and its own font size / spacing sliders that only affect the Risky Stats+ panel. A **Back** button returns you to the main settings.
- A dimmed, click-blocking backdrop sits behind both settings panels so the pause menu's own buttons (like Quit to Desktop) don't show or interfere while you're adjusting settings.
- All settings persist between sessions via BepInEx's config system.

---

## Controls

| Action | Default |
|---|---|
| Show/hide stats panel | `V` |
| Hold to view run progress | `B` |
| Open settings | Pause menu -> **Risky Stats** button |
| Switch to Risky Stats+ settings | **Next** button, bottom right of screen, while settings are open |

Both the stats toggle key and the progress hold key are configurable via BepInEx's config file (`General -> Toggle Key` and `General -> Progress Toggle Key`).

---

## How It Works

The mod is split into three main files:

### `RiskyStatsPlugin.cs` - The HUD Overlay

This is the core `BaseUnityPlugin`. On load it:

1. Initializes settings (`RSSettings.Init`) and adds the settings-menu component (`RSSettingsUI`).
2. Initializes Risky Stats+ settings (`RSPlusSettings.Init`).
3. Initializes progress settings (`ProgressSettings.Init`) and adds the run progress component (`RunProgressUI`).
4. Hooks into game events to track stats in real time:
   - `GlobalEventManager.onServerDamageDealt` -> tracks damage dealt by the local player and their crit status.
   - `HealthComponent.TakeDamage` (IL2CPP hook via `On.RoR2...`) -> tracks damage taken.
   - `HealthComponent.Heal` -> tracks healing received.
   - `PurchaseInteraction.OnInteractionBegin` -> detects when the local player activates a Shrine of the Mountain, for the Mountain Shrines stat.
   - `GlobalEventManager.onCharacterDeathGlobal` -> detects monster kills by the local player, for the Kills stat.
   - `Run.onRunStartGlobal` -> resets the Mountain Shrines and Kills counters at the start of each run.
5. Every frame (`Update`):
   - Checks for the toggle key press.
   - Waits for the `HUD` object to exist, then builds the main stats panel and the Risky Stats+ panel once.
   - Refreshes the main panel's text while it's visible, and refreshes the Risky Stats+ panel's text every frame.

**Main panel construction (`BuildUI`)** dynamically creates a `TextMeshProUGUI` object per stat inside a `HorizontalLayoutGroup` or `VerticalLayoutGroup`, depending on the current alignment setting. In Horizontal mode, Attack Speed / Armor / Crit are nested inside a small vertical sub-container so they stack neatly next to the other stats. A `ContentSizeFitter` keeps the panel sized to its contents automatically.

Because rebuilding the whole UI is expensive, `RefreshUI()` only calls a full `BuildUI()` when the **layout direction** actually changes (Horizontal <-> Vertical). Simple changes like spacing, font size, visibility, or text alignment are applied in-place via `ApplySettings()`, which just updates existing text/layout components rather than recreating objects.

**Risky Stats+ panel construction (`BuildPlusUI`)** works the same way, but is always a `VerticalLayoutGroup`, anchored and pivoted at the bottom right of the HUD. Because the panel is pivoted at its bottom edge, the `ContentSizeFitter` grows the panel upward as more (enabled) stats are added to the visible list, which is what makes newly-enabled stats stack upward above your ability icons instead of downward off-screen.

**Stat tracking specifics:**
- Damage, Damage Taken, and Healing streaks reset automatically if no matching event has fired in the last 3-5 seconds, so the numbers reflect "current burst" rather than a lifetime total.
- Numbers are formatted with `FormatNumber()` into human-readable suffixes (`1.2k`, `3.4m`, etc.) once they cross 1,000.
- `Armor` reads directly from `CharacterBody.armor`, which is RoR2's own final computed value - it includes not just a character's base armor, but also armor granted by items (Ceramic Plate, Bison Steak, etc.) and temporary buffs like spawn protection. This is expected and accurate; it's not limited to a survivor's base armor stat from the wiki.
- `Jumps` reads used jumps from `CharacterBody.characterMotor.jumpCount` and the max from `CharacterBody.maxJumpCount`.
- `Luck` counts 57 Leaf Clover (`RoR2Content.Items.Clover`) in the player's inventory, since RoR2 doesn't expose a single plain "luck" stat on `CharacterBody`.
- `Drones` scans all live `CharacterBody` instances each frame and counts ones owned by the local player's master with "Drone" in their master's name.

### `RSSettings.cs` - Config & Settings Panels

This file contains three classes:

**`RSSettings`** (static) - the single source of truth for all main-panel settings:
- Backed by BepInEx's `ConfigFile`/`ConfigEntry<T>` system, so values persist across game sessions in the plugin's config file.
- Exposes a static `OnSettingsChanged` event. Any time a setting changes (visibility toggle, slider drag, alignment switch, reset), this event fires once, and `RiskyStatsPlugin` listens for it to refresh the HUD accordingly.
- `ResetToDefault()` restores every main-panel setting (all stats visible, font size 27, spacing 40, Horizontal alignment, Center text align), also calls `RSPlusSettings.ResetToDefault()` so Risky Stats+ gets fully reset and disabled at the same time, and fires the change event a single time.

**`RSPlusSettings`** (static) - the same pattern as `RSSettings`, but for the Risky Stats+ panel:
- Every stat defaults to hidden.
- Has its own independent font size and spacing config entries, stored under separate config sections, so adjusting Risky Stats+ never touches the main panel's appearance.
- `ResetToDefault()` hides every Risky Stats+ stat and restores its font size/spacing to default.

**`RSSettingsUI`** (`MonoBehaviour`) - builds and manages both in-game settings panels:
- Hooks `PauseScreenController.Awake` to inject a **"Risky Stats"** button into the pause menu, cloned from an existing menu button so it matches the game's native style.
  - The clone strips its `LanguageTextMeshController` component after relabeling - otherwise RoR2's localization system would silently reset the label back to the original button's text (e.g. "Resume") whenever the pause menu re-enables itself, such as when backing out of a submenu.
  - The button also clears the EventSystem's selected object on click, so its selection outline doesn't stay stuck highlighted after you click into the settings panel.
- Clicking the button builds a full-screen dimmed backdrop (`BuildBackdrop`), the main settings panel (`BuildPanel`), and a screen-anchored **Next** button (`BuildNavButton`), all built entirely from generated `GameObject`s (no prefabs) using Unity's layout system. The backdrop is created first so it always renders behind both panels, fully covering (and blocking clicks to) whatever pause menu buttons sit underneath.
- The main panel (`BuildPanel`) includes:
  - A toggle row per stat (`CreateToggleRow`)
  - Sliders for font size and spacing (`CreateSliderRow`)
  - A button to flip Horizontal/Vertical alignment (`CreateAlignmentRow`) - clicking this fully rebuilds the panel, since switching to Vertical needs to reveal the extra **Text Align** row
  - A conditional **Text Align** row (`CreateTextAlignRow`), only shown when alignment is Vertical, letting you pick Center or Left-aligned stat text
  - A bottom row with **Reset to Default** and **Close** buttons (`CreateBottomButtonsRow`)
- The **Next** button is anchored to the bottom right corner of the whole screen (not the panel), so it stays in a fixed spot regardless of which panel is currently open. Clicking it lazily builds the Risky Stats+ panel (`BuildPlusPanel`) the first time, then swaps which panel is active and flips its own label between **Next** and **Back**.
- The Risky Stats+ panel (`BuildPlusPanel`) mirrors the main panel's structure and reuses the same `CreateToggleRow`/`CreateSliderRow` helpers (now parameterized with a start value and change callback so they work with either settings class), but is bound to `RSPlusSettings`, is titled "RISKY STATS+ SETTINGS", has no Alignment or Text Align rows since it's always a vertical stack, and is built with extra spacing above its Reset/Close row so the buttons sit pinned to the bottom of the taller panel.
- Whenever a setting that changes the *shape* of a panel is clicked (alignment, reset), that panel destroys and rebuilds itself from scratch so all rows reflect the new state correctly. Simple value changes (toggles, sliders, text align) update in place without a rebuild.

### `Progress.cs` - Run Progress Panel

This file contains three pieces:

**`ProgressSettings`** (static) - binds the `Progress Toggle Key` config entry (default `B`), same pattern as the main toggle key.

**`RunProgressStats`** (static) - tracks cumulative totals for the entire run, independent of the HUD overlay's rolling streaks:
- Hooks its own copies of `GlobalEventManager.onServerDamageDealt`, `HealthComponent.TakeDamage`, and `HealthComponent.Heal` to add up Total Damage, Total Damage Taken, and Total Healing without resetting.
- Tracks the single biggest hit dealt (`BiggestHit`) and whether that specific hit was a crit (`BiggestHitCrit`).
- Polls `CharacterMotor.velocity` each frame to keep a running `MaxSpeed` for the run.
- Resets all totals via `Run.onRunStartGlobal`, so stats are scoped to the current run, not any single stage.
- Shares the same `FormatNumber()` k/m/b/t formatting as the main HUD panel for consistency.

**`RunProgressUI`** (`MonoBehaviour`) - builds and manages the popup panel, styled with the same yellow/blue theme as the settings panels:
- The panel is created once under the `HUD` and hidden by default.
- Every frame, checks whether the progress key is being held (`GetKey`, not `GetKeyDown`) - the panel shows while held and hides the instant it's released.
- While visible, refreshes all five stat rows each frame so numbers stay current mid-run.
- Damage is white, Damage Taken is dark red, Healing is green, Max Speed is cyan, and Biggest Hit switches between white and red depending on whether that hit crit - matching the color choices used in the main HUD panel.

---

## Requirements

- [BepInEx](https://github.com/BepInEx/BepInEx) for Risk of Rain 2
- [HookGenPatcher](https://github.com/risk-of-thunder/Bepinex.Monomod.HookGenPatcher) for Risk of Rain 2
- Risk of Rain 2 (uses `RoR2`, `RoR2.UI`, and IL2CPP hook interop via `On.*`)
- TextMeshPro (bundled with the game)

## Installation

1. Build the project or drop the compiled `RiskyStats.dll` into your `BepInEx/plugins` folder.
2. Launch the game - the mod loads automatically and the stats panel appears once you're in a run.

## Configuration File

Settings are stored in BepInEx's standard config location (`BepInEx/config/com.shadowblade.riskystats.cfg`) under these sections:

- `General` - Toggle Key, Progress Toggle Key
- `Appearance` - Font Size, Spacing, Alignment, Text Alignment (main panel)
- `Stats Visibility` - one entry per main-panel stat
- `Plus Appearance` - Font Size, Spacing (Risky Stats+ panel)
- `Plus Stats Visibility` - one entry per Risky Stats+ stat

You can edit this file directly, or use the in-game settings panels (recommended, since they also fire live UI updates).
