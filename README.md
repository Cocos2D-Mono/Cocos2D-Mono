<div align="center">

![Cocos2D-Mono](https://raw.githubusercontent.com/Cocos2D-Mono/cocos2d-mono/master/Logos/logo-full-200.png)

### MonoGame powered built the cocos2d way!

[Check out the docs!](https://cocos2d-mono.dev)

[![DesktopGL (Windows/Linux/macOS)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-desktopgl.yml/badge.svg)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-desktopgl.yml)
[![Windows](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-windows.yml/badge.svg)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-windows.yml)
[![macOS](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-macos.yml/badge.svg)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-macos.yml)
[![Linux](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-linux.yml/badge.svg)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-linux.yml)
[![Android](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-android.yml/badge.svg)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-android.yml)
[![iOS](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-ios.yml/badge.svg)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-ios.yml)

</div>

# Packages

Starting with the release after 2.5.10, Cocos2D-Mono ships as three consolidated,
multi-targeted NuGet packages (DesktopGL for Windows/Linux/macOS, WindowsDX, Android,
and iOS in one package each):

| Package | Use it when |
|---|---|
| `Cocos2D-Mono` | The engine, including the MonoGame content-pipeline (MGCB) build task dependency. |
| `Cocos2D-Mono.Core` | Same engine, without the MGCB dependency — for projects that don't use the content pipeline. |
| `Cocos2D-Mono.Box2D` | The Box2D physics port (also flows transitively through the packages above). |

**Migrating from the per-platform packages** (`Cocos2D-Mono.DesktopGL`,
`Cocos2D-Mono.Windows`, `Cocos2D-Mono.Linux`, `Cocos2D-Mono.macOS`,
`Cocos2D-Mono.Android`, `Cocos2D-Mono.iOS` and their `.Core.*` variants):
**2.5.10 is the final release under those IDs** — replace the reference with
`Cocos2D-Mono` (or `Cocos2D-Mono.Core`) and the right target framework is selected
automatically. The DesktopGL/Windows/Android/iOS flavors are compiled exactly as their
legacy counterparts were. If you are coming from the dedicated `Linux`/`macOS`
packages, note that the unified DesktopGL build uses the same compile-time flavor the
flagship `Cocos2D-Mono.DesktopGL` package always shipped on those platforms (this can
relocate `CCUserDefault` storage written by the old dedicated packages).

# Getting Started

Check out the [guides](https://cocos2d-mono.dev/docs/category/getting-started)!

# Building & running the test app

The library and its interactive test app build from a single solution, `Cocos2DMono.sln`
(the per-platform solutions were retired when the build was consolidated).

```bash
# Build the library (all target frameworks)
dotnet build Cocos2DMono.sln

# Run the interactive test app on the desktop (DesktopGL / net10.0)
dotnet run --project Tests/Cocos2DMono.IntegrationTests/Cocos2DMono.IntegrationTests.csproj -f net10.0 -p:TargetFrameworks=net10.0

# Run the headless unit tests
dotnet test Tests/Cocos2DMono.UnitTests/Cocos2DMono.UnitTests.csproj
```

The test app's scenes live in `Tests/cocos2d-mono.Tests/`; the multi-targeted
`Cocos2DMono.IntegrationTests` project compiles them into a runnable host for each platform
(desktop, Windows, Android, iOS). See [`Tests/README.md`](Tests/README.md) for details.

# Contributing

Thanks so much for your interest in cocos2d-mono and wanting to contribute to the project! Here's a [guide](https://cocos2d-mono.dev/docs/category/contributing) to help you get started.

[`CONTRIBUTING.md`](CONTRIBUTING.md) covers the working conventions that apply across all Cocos2D-Mono repositories — branching and PR flow, how changes are verified, API stability, and how releases are cut.
