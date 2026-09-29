# Cocos2D-Mono Roadmap

Cocos2D-Mono is an actively maintained 2D game framework for .NET, built on MonoGame. This document outlines the direction of the project at a high level. Priorities and timing may shift, and feedback is welcome through the [issue tracker](https://github.com/Cocos2D-Mono/cocos2d-mono/issues).

## Guiding principles

- **Incremental and stable.** Improvements land in small, reviewable steps. We avoid large, disruptive rewrites.
- **Compatibility first.** Where practical, public API changes go through `[Obsolete]` deprecation cycles. Breaking changes are signaled by a major version bump and documented with migration notes.
- **Current platform.** Track current .NET and MonoGame releases.

## Themes

### Build & packaging
Simplify the project and packaging layout — fewer, clearer NuGet packages with centralized, consistent dependency versions — so the framework is easier to consume and maintain. This has landed: the build is consolidated into multi-targeted projects, and the next release ships as three packages (`Cocos2D-Mono`, `Cocos2D-Mono.Core`, `Cocos2D-Mono.Box2D`) replacing the per-platform package line (final per-platform release: 2.5.10). Migration notes live in the README.

### Quality & testing
Grow automated test coverage (math primitives, actions, scheduling, serialization) and adopt code analyzers, so changes stay safe and regressions are caught early. A unit-test suite now gates changes in CI and keeps growing alongside the work; a benchmark project supports performance decisions on demand.

### Modern C#
Adopt current language features — nullable reference types for null-safety, up-to-date syntax, and value-type discipline for the math types — improving correctness and readability.

### API & architecture *(longer-term, exploratory)*
Evolve toward a more composable, testable design — composition over deep inheritance, optional dependency injection, and async content loading — while preserving familiar entry points. This work is exploratory and will be staged carefully.

### Platforms
Platform support comes in three tiers, depending on how each platform's MonoGame dependency is distributed:

- **Packaged** — desktop (Windows, macOS and Linux via DesktopGL), WindowsDX, Android and iOS ship in the NuGet packages and track the current release.
- **Source only** — Mac Catalyst and tvOS build against our [MonoGame fork](https://github.com/Cocos2D-Mono/MonoGame), because MonoGame publishes no package for either. Both are buildable from source today and will live on the fork for the foreseeable future. A NuGet package only carries a platform when its MonoGame dependency can be restored from nuget.org, so these stay out of the packages.
- **Registered console developers** — **PlayStation 5** support is available in a private repository, following the model MonoGame uses for its console frameworks: access is limited to registered console developers under NDA. Console support tracks the **2.5.x line** rather than the current release. Reach out to broberts@cocos2d-mono.dev if you are a registered PlayStation developer.

UWP / Xbox-UWP support is maintained separately in the [Cocos2D-Mono.UWP](https://github.com/Cocos2D-Mono/Cocos2D-Mono.UWP) repository.

## Status at a glance

| Theme | Status |
|---|---|
| Quality & testing | In place and growing |
| Build & packaging | Shipped in 2.5.11 |
| Modern C# | Next up |
| API & architecture | Exploratory |
| Platforms | Maintained, in three tiers — packaged, source only, console |

---

*This roadmap is a living document, not a commitment to specific features or dates.*
