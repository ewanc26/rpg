# RPG Template (Godot 4, C#/Mono)

A minimal top-down RPG starter built for Godot **4.7** with the **.NET (C#)**
build. It's a from-scratch scaffold rather than a fork of an existing
template — a survey of existing options found nothing that was both
Godot-Mono and RPG-specific (the well-known "Open RPG" demo is GDScript;
existing C# templates are generic, not RPG-oriented) — so this repo builds
that combination directly, informed by common patterns from those projects.

## Requirements

- [Godot 4.7 .NET/Mono build](https://godotengine.org/download) (the
  editor build with C# support, not the standard build)
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)

## Getting started

1. Open `project.godot` in the Godot 4.7 .NET editor. Godot will generate
   the C# solution/user files on first open.
2. Build the project (Project > Tools > C# > Create/Build, or just press
   Play — Godot builds automatically).
3. Press F5 to run. It starts on the title screen and "Start Game" loads
   `scenes/World/TestLevel.tscn`.

## Controls

| Action    | Key          |
|-----------|--------------|
| Move      | Arrow keys   |
| Interact  | `E`          |

(Arrow keys are Godot's built-in `ui_*` actions, so they work with zero
input-map setup. Add WASD or a gamepad binding to `move_*`/`interact`
actions in Project Settings > Input Map if you want them.)

## Project structure

```
project.godot          Engine + autoload + input map config
RPGTemplate.csproj      C# project file (Godot.NET.Sdk, net8.0)
scripts/
  Autoload/             Singletons: GameManager (scene/pause), EventBus (global signals)
  Player/               PlayerController (movement/interaction), PlayerStats (health/leveling)
  Systems/              Inventory, InventoryItem, ItemPickup, SaveSystem, IInteractable
  NPC/                  NPC.cs (dialogue-triggering interactable)
  UI/                   HUD, DialogueBox, TitleScreen
scenes/
  TitleScreen.tscn
  Player.tscn           CharacterBody2D + Inventory + interaction Area2D
  NPC.tscn
  ItemPickup.tscn
  World/TestLevel.tscn  Playable room wiring it all together
  UI/HUD.tscn
  UI/DialogueBox.tscn
resources/items/
  health_potion.tres    Sample InventoryItem resource
```

## How the systems fit together

- **EventBus** (`scripts/Autoload/EventBus.cs`) is a global signal hub —
  `PlayerHealthChanged`, `PlayerLeveledUp`, `DialogueStarted`,
  `DialogueEnded`, `InventoryChanged`. UI listens to it instead of polling
  the player directly, so HUD/dialogue stay decoupled from gameplay code.
- **PlayerStats** is a `Resource` (health, attack, defense, leveling via
  `AddExperience`), so it can be saved, swapped, or authored as a preset.
- **Inventory** is a plain component `Node` — attach it to any actor
  (already on `Player.tscn`) and call `AddItem`/`RemoveItem`.
- **IInteractable** is implemented by `NPC.cs`; `PlayerController` tracks
  interactables in range via its `InteractionArea` and calls `Interact()`
  on the nearest one when you press `interact`. Add new interactable types
  (chests, signs, doors) by implementing the same interface.
- **SaveSystem** is a static utility (`SaveSystem.Save`/`Load`) that
  (de)serializes `PlayerStats` to `user://savegame.json`. It's not wired to
  a menu yet — call it from wherever you add save points.

## Extending

- New item: duplicate `resources/items/health_potion.tres`, change its
  `Id`/`DisplayName`/icon, point an `ItemPickup.tscn` instance at it.
- New NPC: instance `scenes/NPC.tscn`, set `NpcName`/`DialogueText` in the
  Inspector — no code needed for simple one-line dialogue.
- New enemy/combat: `PlayerStats.TakeDamage(int)` already applies defense
  mitigation and fires `PlayerHealthChanged`; hang an enemy AI + attack
  script off the same pattern.

## Sources consulted

- [chickensoft-games/GodotGame](https://github.com/chickensoft-games/GodotGame) — C# project template conventions (`.csproj`/SDK setup) for Godot 4
- [gdquest-demos/godot-open-rpg](https://github.com/gdquest-demos/godot-open-rpg) — reference for RPG project structure (GDScript, not C#)
- [Godot 4 C# documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html) — signals, autoloads, `GlobalClass` resources
