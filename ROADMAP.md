# Cocos2D-Mono Roadmap

Cocos2D-Mono is an actively maintained 2D game framework for .NET, built on MonoGame. This document outlines the direction of the project at a high level. Priorities and timing may shift, and feedback is welcome through the [issue tracker](https://github.com/Cocos2D-Mono/cocos2d-mono/issues).

## Guiding principles

- **Incremental and stable.** Improvements land in small, reviewable steps. We avoid large, disruptive rewrites.
- **Compatibility first.** Where practical, public API changes go through `[Obsolete]` deprecation cycles. Breaking changes are signaled by a major version bump and documented with migration notes.
- **Current platform.** Build on upstream MonoGame and the current long-term support (LTS) release of .NET.

## Themes

### Build & packaging
Simplify the project and packaging layout — fewer, clearer NuGet packages with centralized, consistent dependency versions — so the framework is easier to consume and maintain. This has landed: the build is consolidated into multi-targeted projects, and since 2.5.11 releases ship as three packages (`Cocos2D-Mono`, `Cocos2D-Mono.Core`, `Cocos2D-Mono.Box2D`) replacing the per-platform package line (final per-platform release: 2.5.10). Migration notes live in the README.

### Quality & testing
Grow automated test coverage (math primitives, actions, scheduling, serialization) and adopt code analyzers, so changes stay safe and regressions are caught early. A unit-test suite now gates changes in CI and keeps growing alongside the work; a benchmark project supports performance decisions on demand.

### Modern C#
Adopt current language features — nullable reference types for null-safety, up-to-date syntax, and value-type discipline for the math types — improving correctness and readability. Changes that would break existing code are held for [3.0](#30).

### API & architecture *(longer-term, exploratory)*
Evolve toward a more composable, testable design — composition over deep inheritance, optional dependency injection, and async content loading — while preserving familiar entry points. This work is exploratory, will be staged carefully, and targets a major release after 3.0.

### Platforms
Platform support comes in three tiers, depending on how each platform's MonoGame dependency is distributed:

- **Packaged** — desktop (Windows, macOS and Linux via DesktopGL), WindowsDX, Android and iOS ship in the NuGet packages and track the current release.
- **Source only** — Mac Catalyst and tvOS build against our [MonoGame fork](https://github.com/Cocos2D-Mono/MonoGame), because MonoGame publishes no package for either. Both are buildable from source today and will live on the fork for the foreseeable future. A NuGet package only carries a platform when its MonoGame dependency can be restored from nuget.org, so these stay out of the packages.
- **Registered console developers** — **PlayStation 5** support is available in a private repository, following the model MonoGame uses for its console frameworks: access is limited to registered console developers under NDA. Console support tracks the **2.5.x line** rather than the current release. Reach out to broberts@cocos2d-mono.dev if you are a registered PlayStation developer.

A platform moves into the packages once its MonoGame dependency can be restored from nuget.org and CI builds it.

UWP / Xbox-UWP support is maintained separately in the [Cocos2D-Mono.UWP](https://github.com/Cocos2D-Mono/Cocos2D-Mono.UWP) repository.

**Graphics backends.** Cocos2D-Mono renders through MonoGame and follows MonoGame's backends rather than maintaining a graphics layer of its own. Today the packages use OpenGL through DesktopGL, DirectX through WindowsDX, and MonoGame's mobile backends on Android and iOS. MonoGame 3.8.5 introduced Vulkan and Direct3D 12 in preview; once MonoGame declares them stable, we'll evaluate offering them in the packages.

**Hosting.** The engine runs as a MonoGame game, and `CCGameView` embeds it in native Android and iOS views. Embedding in app frameworks such as .NET MAUI or Avalonia is being explored, starting with a sample before anything is promised.

**Native AOT.** iOS apps are already compiled ahead of time. The goal for 3.0 is a library that is trim-safe and compatible with Native AOT publishing.

## Versioning and compatibility

Cocos2D-Mono follows [Semantic Versioning](https://semver.org/) where practical.

- **Minor releases** add features and fixes. A minor release may also move to a new .NET or MonoGame baseline, with migration notes, as 2.6.0 did for .NET 10 and MonoGame 3.8.5.
- **.NET versions.** The packages target the current LTS release of .NET, today .NET 10, and move to the next LTS release (.NET 12) once MonoGame supports it. Short-term support releases are skipped.
- **Major releases** are where breaking API changes land. Anything removed in a major release is marked `[Obsolete]` in an earlier release first, naming its replacement.

### 3.0

The next major release is an API cleanup, with the same platforms as 2.x:

- Public mutable fields become properties.
- The math types adopt value-type discipline.
- Nullable reference type annotations cover the public API.
- Misnamed public members are corrected.
- The library becomes trim-safe and compatible with Native AOT publishing.

The larger architecture work (composition, dependency injection, async content loading) follows in a later major release. There is no date for 3.0 yet. After 3.0 ships, the 2.x line is not maintained in parallel by default; if you depend on 2.x and need a fix, open an issue.

## Status at a glance

| Theme | Status |
|---|---|
| Quality & testing | In place and growing |
| Build & packaging | Shipped in 2.5.11 |
| Modern C# | In progress — nullable reference types next |
| API & architecture | Exploratory — after 3.0 |
| Platforms | Maintained, in three tiers — packaged, source only, console |
| 3.0 | Planned — API cleanup, no date yet |

---

*This roadmap is a living document, not a commitment to specific features or dates.*
