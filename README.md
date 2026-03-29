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
- Live Core Keeper container observation hooks added through the in-game chest UI
- Debug sample-data hotkey kept as an optional testing path

## Planned Features

- Search item names across all discovered storage containers
- Show matching chest location and item count
- Fast in-game lookup flow with minimal UI friction
- Validate container identifiers and location hints against real gameplay

## Development

This repository starts as a clean foundation. The implementation follows the rules in `AGENT.md`, including XML documentation requirements for all public classes and methods.

Build setup:

- Install the .NET 8 SDK.
- Copy `Config.Build.user.props.template` to `Config.Build.user.props` when your Core Keeper install path is not the default Steam location.
- The build expects the game at `Core Keeper/` and deploys the compiled DLL to `BepInEx/plugins/`.
- A build also needs a valid `BepInEx/core/` directory. That can come from your local game install or from a downloaded BepInEx release.
- NuGet feeds for BepInEx-related packages are declared in `NuGet.config`.

Local build commands:

- Default install path: `dotnet build src/BoxSearch/BoxSearch.csproj`
- Custom Core Keeper path: `dotnet build src/BoxSearch/BoxSearch.csproj -p:CoreKeeperGameRootDir="/path/to/Core Keeper/"`
- Build against a downloaded BepInEx package: `dotnet build src/BoxSearch/BoxSearch.csproj -p:CoreKeeperBepInExCoreDir="/path/to/BepInEx/core/"`

CI:

- GitHub Actions builds the project on pushes, pull requests, and manual runs.
- The workflow downloads `BepInEx_unix_5.4.21.0.zip` and points the build at its extracted `BepInEx/core/` directory.

Current runtime controls:

- `Ctrl+F`: toggle the search overlay
- `Esc`: close the search overlay
- Real container indexing: open a storage container to refresh its snapshot in Box Search
- Optional debug path: enable `Debug/EnableDebugSampleHotkey` in the BepInEx config, then press `F8` to seed sample container data

## License

Released under the MIT License. See `LICENSE`.
