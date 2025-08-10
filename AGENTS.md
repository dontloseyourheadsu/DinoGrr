# Repository Guidelines

## Project Structure & Module Organization
- `DinoGrr/DinoGrr.sln`: Solution root tying all targets.
- `DinoGrr.Core/`: Core game code (UI, Physics, Rendering, Entities, Database) and content pipeline under `Content/` with `DinoGrr.mgcb` and assets in `Content/Assets`.
- Runners: `DinoGrr.DesktopGL/` (cross‑platform desktop), `DinoGrr.WindowsDX/` (Windows), `DinoGrr.Android/`, `DinoGrr.iOS/`.
- Common namespaces: `DinoGrr.Core.*` (e.g., `UI`, `Physics`, `Rendering`, `Database`).

## Build, Test, and Development Commands
- `dotnet restore DinoGrr/DinoGrr.sln`: Restore NuGet packages and local tools.
- `dotnet build DinoGrr/DinoGrr.sln -c Debug`: Build all projects; MonoGame content compiles via `DinoGrr.mgcb`.
- `dotnet run --project DinoGrr/DinoGrr.DesktopGL`: Run the desktop game locally.
- `dotnet run --project DinoGrr/DinoGrr.WindowsDX` (Windows only): Run the DirectX target.
- `dotnet tool restore` then `mgcb-editor DinoGrr/DinoGrr.Core/Content/DinoGrr.mgcb`: Open the content pipeline editor.

## Coding Style & Naming Conventions
- C# (net8.0), 4‑space indentation, braces on new lines.
- Private fields: `_camelCase`; public types/members: `PascalCase`; locals/params: `camelCase`.
- Prefer file‑scoped namespaces; one type per file; keep code within feature folders (e.g., `Physics/`, `UI/`).
- Use `var` for obvious types; explicit types when clarity helps. Keep methods small and focused.

## Testing Guidelines
- No test project yet. Create `DinoGrr.Tests` (xUnit) at repo root.
- Naming: `ClassNameTests.cs`; methods `MethodName_Should_DoThing()`.
- Run tests with `dotnet test`. Target pure logic (physics, AI, repositories) for unit tests.

## Commit & Pull Request Guidelines
- Commits: short, imperative (“Add main menu”, “Fix camera zoom”); reference issues (`#39`) when relevant.
- Branches: include issue + slug (e.g., `39-submenu-level-selector`).
- PRs: clear description, linked issues (`Fixes #39`), platforms tested, and screenshots/GIFs for visual/UI changes. Note any content additions in `DinoGrr.mgcb`.

## Security & Configuration Tips
- Do not commit secrets; only commit assets placed under `DinoGrr.Core/Content/Assets`.
- Large binaries should use Git LFS if added. Android/iOS builds require their platform SDKs; DesktopGL is recommended for local development.

