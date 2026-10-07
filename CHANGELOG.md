# Changelog

All notable changes to Cocos2D-Mono are recorded here. This file was introduced with
2.5.10; earlier releases are described in their GitHub release notes / git history.
The project follows [Semantic Versioning](https://semver.org/) where practical.

## Unreleased

### Changed

- **Nullable annotations for `CCNode`, `CCAtlasNode` and `CCTapNode`.** These are the first
  types converted to nullable reference types; more follow directory by directory through
  2.x. Projects with nullable enabled now see which members can be null:
  - `CCNode.Parent`, `Grid`, `Name`, `UserData`, `UserObject`
  - `CCNode.Children`, which is null until the first child is added
  - `CCNode.GetChildByTag`, which returns null for an unknown tag
  - `CCNode.RunActions`, which returns null when given no actions
  - `CCTapNode<T>.Data` and its handlers' `data` argument, which are `T?`

  This only adds warnings, and only in projects that enable nullable; nothing stops
  compiling.

### Fixed

- `CCNode.Compare` accepts null arguments, ordering them first as `IComparer<T>` allows,
  instead of throwing.
- `CCNode.SortAllChildren` and `UpdateZOrderRange` no longer throw on a node with no
  children.
- `CCTapNode.RefreshContentSize` sizes a node with no children to zero instead of throwing.
- Deserializing malformed node data throws an `InvalidDataException` that names the problem,
  instead of a null reference error.

## 2.6.1 - 2026-10-09

A small patch release: two changes to how `CCLabel` draws text on Linux and macOS. No API
changes and nothing to migrate — bump the package reference to 2.6.1.

### Fixed

- **`CCLabel` on Linux and macOS** now disposes the native Skia canvas and font it creates
  each time a label redraws its text, instead of leaving them for the garbage collector.

### Changed

- The Linux/macOS label backend moved off SkiaSharp's deprecated `SKPaint` text APIs onto
  `SKFont`. Rendering is unchanged, and the engine now builds with no obsolete-API
  warnings, ahead of the old APIs' eventual removal upstream.

Other platforms are unaffected: Windows draws labels with GDI, and Android and iOS use
their platform text renderers.

### Documentation

- Added `CONTRIBUTING.md` with the working conventions shared across the Cocos2D-Mono
  repositories: branching and pull requests, how changes are verified, API stability, and
  how releases are cut.

## 2.6.0 - 2026-07-28

The platform-line release: **.NET 10 + MonoGame 3.8.5**. No engine API changes — scenes,
nodes, actions, and every public type behave exactly as they did on 2.5.12.

### ⚠️ Migration (one step)

Retarget your game project to .NET 10 and bump the package reference to 2.6.0:

| Your target | New TFM |
|---|---|
| Desktop (DesktopGL) | `net10.0` |
| WindowsDX | `net10.0-windows7.0` |
| Android | `net10.0-android36.0` |
| iOS | `net10.0-ios26.0` (note Apple's version-numbering jump — the .NET 10 iOS workload ships 26.x bindings) |

The .NET 10 SDK is required to build. The MonoGame 3.8.5 move rides along transparently —
no code changes.

### Changed

- **MonoGame 3.8.4.1 → 3.8.5** — MonoGame's major restructuring release (new native core,
  ARM64 support, Vulkan/Direct3D 12 backends in preview). The engine continues to ship on
  the DesktopGL and WindowsDX backends; the new preview backends are not consumed yet.
- **.NET 9 → .NET 10** across every target — .NET 9 is an STS release past its support
  window; 2.6.0 puts the engine on the current LTS.
- Migrated off MonoGame-obsoleted APIs (`GraphicsDevice.DrawIndexedPrimitives` legacy
  overload, `Keyboard.GetState(PlayerIndex)`) — no behavior change, and the engine is
  clean ahead of their eventual removal upstream.

### Validation

Full pass on the new stack: all four TFMs + Box2D compile clean, 132 unit tests, the MGCB
content build, and an interactive test-app pass (sprites, particles, every label backend,
tilemaps, draw nodes, transitions, input, audio, Box2D testbed) on both desktop backends.

## 2.5.12 - 2026-07-23

A bug-fix release. Consumer-visible behavior fixes to actions, menu items, and text
fields, plus a buffer-leak fix and the completion of the internal private-field naming
modernization (no public API change).

### Fixed

- **Instant action copy constructors** — `CCFlipX`, `CCFlipY`, and `CCPlace` now preserve
  their state when copied (e.g. via `Copy`) instead of copying default values.
- **`CCCallFuncN.InitWithTarget`** now returns `true` on success, matching
  `CCCallFuncO.InitWithTarget` (previously returned `false`).
- **`CCMenuItemSprite`** — assigning `null` to `NormalImage`, `SelectedImage`, or
  `DisabledImage` now clears the image instead of throwing `NullReferenceException`.
- **`CCTextFieldTTF`** — auto-edit touch handling re-registers correctly after toggling
  `ReadOnly` / `AutoEdit`; a field returned to editable is touch-interactive again.
- **`CCRawList<T>`** — `RemoveRange` no longer corrupts the list when the removed range is
  followed by `1..rangeCount` surviving elements (the shift was skipped and the survivors
  zeroed — visible as vertex corruption when removing middle segments from draw nodes).
  Also reworked pooled-buffer rent/return handling and fixed a tail-clear buffer leak.

### Changed (internal)

- Completed the private-field naming modernization: every private Hungarian `m_*` field
  across the library is now `_camelCase`. No public, protected, or internal API change.

## 2.5.11 - 2026-07-10

The debut of the **consolidated NuGet package line**. The build was collapsed from ~30
per-platform projects into three multi-targeted packages, so the twelve per-platform
packages are replaced by three that each cover every platform. Runtime behavior matches
2.5.10 plus the fixes below; the .NET 10 / MonoGame 3.8.5 upgrade continues on the `2.6.0`
line.

### 📦 Packages — new IDs (migration required)

The per-platform packages are retired (2.5.10 was their final release). Replace your
reference with one of:

| New package | Replaces | Use when |
|---|---|---|
| `Cocos2D-Mono` | `Cocos2D-Mono.{DesktopGL,Windows,Linux,macOS,Android,iOS}` | The engine, with the MonoGame content-pipeline (MGCB) build task. |
| `Cocos2D-Mono.Core` | `Cocos2D-Mono.Core.{…}` | Same engine without the MGCB dependency. |
| `Cocos2D-Mono.Box2D` | (was bundled) | Box2D physics port (also flows transitively through the above). |

Each package multi-targets DesktopGL (Windows/Linux/macOS), WindowsDX, Android, and iOS;
the correct target framework is selected automatically. UWP / Xbox-UWP remains in the
separate [Cocos2D-Mono.UWP](https://github.com/Cocos2D-Mono/Cocos2D-Mono.UWP) repo.

### ⚠️ Breaking / migration

- **Assembly renamed** `Cocos2D.dll` → `Cocos2DMono.dll` (matching the product name). The
  **namespace is unchanged (`Cocos2D`)**, so source and NuGet-resolved references need no
  change; only by-name `<Reference>` entries or `Assembly.Load("Cocos2D")` / reflection by
  assembly name are affected.
- **OpenTK is no longer a dependency.** It was referenced but unused (its only consumer was
  a long-dead GL-extensions probe). Consumers who relied on the transitive OpenTK reference
  (uncommon) should add their own.

### Added

- `ICCUserDefaultStorage` + the settable `CCUserDefault.Storage` — a pluggable backend for
  where `CCUserDefault` persists its settings file (file on desktop, isolated storage
  elsewhere by default). Enables custom stores on platforms without a writable file system.

### Fixed / changed (internal)

- `CCAccelerometer`'s platform guard now states its intent (`ANDROID || IOS`) instead of a
  "not desktop" exclusion; removed dead `WINDOWS_PHONE8` code.
- `CCRawList<T>` clears returned pooled buffers unconditionally on .NET Framework targets
  (which lack `RuntimeHelpers.IsReferenceOrContainsReferences`).
- Modernized C#: file-scoped namespaces repo-wide; private fields adopt `_camelCase` in the
  denshion and misc_nodes subsystems (ongoing, internal-only).

## 2.5.10 - 2026-06-15

A modernization and hardening pass on the current runtime (.NET 9 / MonoGame 3.8.4.1).
It is almost entirely internal cleanup, correctness fixes, and removal of long-dead
platform code — no new public API. The .NET 10 / MonoGame 3.8.5 upgrade continues
separately on the `2.6.0` line and will ship once MonoGame 3.8.5 is stable.

### ⚠️ Breaking
- Removed the custom array pool `Cocos2D.ArrayPool<T>`. `CCRawList<T>` now pools through
  `System.Buffers.ArrayPool<T>.Shared`, and its public `UseArrayPool` option is unchanged.
  Code that referenced `Cocos2D.ArrayPool<T>` directly (uncommon) should switch to
  `System.Buffers.ArrayPool<T>`.

### Fixed
- `CCDirector.SharedDirector` lazy initialization is now thread-safe. Previously a race
  during concurrent first access could expose a partially-initialized director (a node
  built on another thread could cache a null action manager and throw).
- `CCInputState` now receives the correct per-frame delta time. It was passed
  `1 / ElapsedGameTime.Milliseconds` (the 0–999 millisecond component, inverted), which
  produced wrong values and `Infinity` when the component was 0.
- `CCTouchDelegate.DoesScriptHandlerExist` no longer throws `KeyNotFoundException` for an
  unregistered event type; it returns `false`.
- `CCRawList<T>.PackToCount` keeps the pooled buffer correctly owned and never leaves a
  zero-length backing array (which `Add`'s doubling growth could not expand).

### Performance
- `CCRawList<T>` array pooling is now backed by `System.Buffers.ArrayPool<T>` — about 21%
  faster with near-zero allocation on the pooled path.

### Removed (dead code)
- All legacy dead-platform conditional code: `WINDOWS_PHONE`, `XBOX`, `XBOX360`, `PSM`,
  `WP8`, `SILVERLIGHT`, `PCL`, and `NETFX_CORE` / `WINRT` / `WINDOWS_UWP` (UWP support is
  maintained in its own package).
- Dead files `Tuple.cs` and `b2FlagExtensions.cs`.
- Non-generic `System.Collections` usage (`ArrayList` / `Hashtable`) replaced with generics.

### Internal
- Added a unit-test suite (geometry, color, node, actions, scheduler, `CCRawList`) and a
  BenchmarkDotNet project for the perf-sensitive paths.
- Added an `.editorconfig` formatting and C# style baseline.
