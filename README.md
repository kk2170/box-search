# Box Search

[English](README.md) | [日本語](README.ja.md)

Box Search is a Core Keeper mod project focused on one goal: quickly finding where an item is stored across your boxes.

## Stack

- Language: C#
- Modding framework: BepInEx + Harmony

## Status

Project bootstrap is complete:

- Local git repository initialized
- MIT license added
- Agent development guide added

Initial implementation is now in place:

- BepInEx/Harmony C# project scaffold added under `src/BoxSearch`
- In-memory storage snapshot and item-name search services added
- Minimal in-game IMGUI search overlay added
- Debug sample-data hotkey added so the UI/search loop can be exercised before a real Core Keeper container hook is wired

## Planned Features

- Search item names across all discovered storage containers
- Show matching chest location and item count
- Fast in-game lookup flow with minimal UI friction
- Wire the observation layer to real Core Keeper storage/container events

## Development

This repository starts as a clean foundation. The implementation follows the rules in `AGENT.md`, including XML documentation requirements for all public classes and methods.

Build setup:

- Copy `Config.Build.user.props.template` to `Config.Build.user.props` when your Core Keeper install path is not the default Steam location.
- The build expects the game at `Core Keeper/` and deploys the compiled DLL to `BepInEx/plugins/`.

Current runtime controls:

- `Ctrl+F`: toggle the search overlay
- `Esc`: close the search overlay
- Optional debug path: enable `Debug/EnableDebugSampleHotkey` in the BepInEx config, then press `F8` to seed sample container data

## License

Released under the MIT License. See `LICENSE`.
