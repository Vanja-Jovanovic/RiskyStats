# Risky Stats

A BepInEx plugin for **Risk of Rain 2** that adds a live, customizable combat stats HUD overlay - attack speed, armor, crit chance, healing, damage, streaks, damage taken, and movement speed - plus an in-game settings panel accessible from the pause menu.

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
- **Toggle key** (default `V`) to show/hide the panel instantly.
- **In-game settings panel**, added as a button in the pause menu, letting you:
  - Show/hide each stat individually
  - Adjust text size (14–48)
  - Adjust spacing between stats (5–100)
  - Switch panel layout between **Horizontal** and **Vertical**
  - When Vertical is selected, choose **Text Align** (Center or Left)
  - **Reset to Default** - restores all settings in one click
- All settings persist between sessions via BepInEx's config system.

---

## Controls

| Action | Default |
|---|---|
| Show/hide stats panel | `V` |
| Open settings | Pause menu → **Risky Stats** button |

The toggle key is configurable via BepInEx's config file (`General → Toggle Key`).

---

## How It Works

The mod is split into two main files:

### `RiskyStatsPlugin.cs` - The HUD Overlay

This is the core `BaseUnityPlugin`. On load it:

1. Initializes settings (`RSSettings.Init`) and adds the settings-menu component (`RSSettingsUI`).
2. Hooks into game events to track stats in real time:
   - `GlobalEventManager.onServerDamageDealt` → tracks damage dealt by the local player and their crit status.
   - `HealthComponent.TakeDamage` (IL2CPP hook via `On.RoR2...`) → tracks damage taken.
   - `HealthComponent.Heal` → tracks healing received.
3. Every frame (`Update`):
   - Checks for the toggle key press.
   - Waits for the `HUD` object to exist, then builds the stats panel once.
   - If the panel is visible, refreshes all stat text.

**Panel construction (`BuildUI`)** dynamically creates a `TextMeshProUGUI` object per stat inside a `HorizontalLayoutGroup` or `VerticalLayoutGroup`, depending on the current alignment setting. In Horizontal mode, Attack Speed / Armor / Crit are nested inside a small vertical sub-container so they stack neatly next to the other stats. A `ContentSizeFitter` keeps the panel sized to its contents automatically.

Because rebuilding the whole UI is expensive, `RefreshUI()` only calls a full `BuildUI()` when the **layout direction** actually changes (Horizontal ↔ Vertical). Simple changes like spacing, font size, visibility, or text alignment are applied in-place via `ApplySettings()`, which just updates existing text/layout components rather than recreating objects.

**Stat tracking specifics:**
- Damage, Damage Taken, and Healing streaks reset automatically if no matching event has fired in the last 3–5 seconds, so the numbers reflect "current burst" rather than a lifetime total.
- Numbers are formatted with `FormatNumber()` into human-readable suffixes (`1.2k`, `3.4m`, etc.) once they cross 1,000.
- `Armor` reads directly from `CharacterBody.armor`, which is RoR2's own final computed value - it includes not just a character's base armor, but also armor granted by items (Ceramic Plate, Bison Steak, etc.) and temporary buffs like spawn protection. This is expected and accurate; it's not limited to a survivor's base armor stat from the wiki.

### `RSSettings.cs` - Config & Settings Panel

This file contains two classes:

**`RSSettings`** (static) - the single source of truth for all mod settings:
- Backed by BepInEx's `ConfigFile`/`ConfigEntry<T>` system, so values persist across game sessions in the plugin's config file.
- Exposes a static `OnSettingsChanged` event. Any time a setting changes (visibility toggle, slider drag, alignment switch, reset), this event fires once, and `RiskyStatsPlugin` listens for it to refresh the HUD accordingly.
- `ResetToDefault()` restores every setting (all stats visible, font size 27, spacing 40, Horizontal alignment, Center text align) and fires the change event a single time.

**`RSSettingsUI`** (`MonoBehaviour`) - builds and manages the in-game settings menu:
- Hooks `PauseScreenController.Awake` to inject a **"Risky Stats"** button into the pause menu, cloned from an existing menu button so it matches the game's native style.
  - The clone strips its `LanguageTextMeshController` component after relabeling - otherwise RoR2's localization system would silently reset the label back to the original button's text (e.g. "Resume") whenever the pause menu re-enables itself, such as when backing out of a submenu.
  - The button also clears the EventSystem's selected object on click, so its selection outline doesn't stay stuck highlighted after you click into the settings panel.
- Clicking the button toggles a custom panel (`BuildPanel`), built entirely from generated `GameObject`s (no prefabs) using Unity's layout system:
  - A toggle row per stat (`CreateToggleRow`)
  - Sliders for font size and spacing (`CreateSliderRow`)
  - A button to flip Horizontal/Vertical alignment (`CreateAlignmentRow`) - clicking this fully rebuilds the panel, since switching to Vertical needs to reveal the extra **Text Align** row
  - A conditional **Text Align** row (`CreateTextAlignRow`), only shown when alignment is Vertical, letting you pick Center or Left-aligned stat text
  - A bottom row with **Reset to Default** and **Close** buttons (`CreateBottomButtonsRow`)
- Whenever a setting that changes the *shape* of the panel is clicked (alignment, reset), the panel destroys and rebuilds itself from scratch so all rows reflect the new state correctly. Simple value changes (toggles, sliders, text align) update in place without a rebuild.

---

## Requirements

- [BepInEx](https://github.com/BepInEx/BepInEx) for Risk of Rain 2
- Risk of Rain 2 (uses `RoR2`, `RoR2.UI`, and IL2CPP hook interop via `On.*`)
- TextMeshPro (bundled with the game)

## Installation

1. Build the project or drop the compiled `RiskyStats.dll` into your `BepInEx/plugins` folder.
2. Launch the game - the mod loads automatically and the stats panel appears once you're in a run.

## Configuration File

Settings are stored in BepInEx's standard config location (`BepInEx/config/com.shadowblade.riskystats.cfg`) under these sections:

- `General` - Toggle Key
- `Appearance` - Font Size, Spacing, Alignment, Text Alignment
- `Stats Visibility` - one entry per stat

You can edit this file directly, or use the in-game settings panel (recommended, since it also fires live UI updates).
