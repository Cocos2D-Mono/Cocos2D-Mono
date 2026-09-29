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

<sub>Built from source only — not in the NuGet packages (see <a href="#platform-support">Platform support</a>):</sub><br>
[![Mac Catalyst](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-maccatalyst.yml/badge.svg)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-maccatalyst.yml)
[![tvOS](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-tvos.yml/badge.svg)](https://github.com/Cocos2D-Mono/cocos2d-mono/actions/workflows/status-tvos.yml)

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

# Platform support

| Tier | Platforms | How to get it |
|---|---|---|
| **Packaged** | Desktop via DesktopGL (Windows, macOS, Linux), WindowsDX, Android, iOS | The NuGet packages above. |
| **Source only** | Mac Catalyst, tvOS | Build the engine from source against our [MonoGame fork](https://github.com/Cocos2D-Mono/MonoGame), checked out beside this repository at the commit pinned by `MONOGAME_FORK_REF` in [`build.yml`](.github/workflows/build.yml). MonoGame publishes no package for either platform, so neither is in the published NuGet packages. |
| **Registered console developers** | PlayStation 5 | On the 2.5.x line — see the [roadmap](ROADMAP.md#platforms). |

A published NuGet package only carries a platform when that platform's MonoGame dependency can also be restored from nuget.org — which is what separates the first two tiers. The build enforces it: `dotnet pack` leaves the source-only targets out even on a Mac with the fork present.

## Building Mac Catalyst or tvOS from source

On macOS, with the .NET 10 SDK:

```bash
# 1. The workload for the platform you want
dotnet workload install maccatalyst        # or: tvos

# 2. Our MonoGame fork, checked out beside this repository at the commit CI pins
#    (MONOGAME_FORK_REF in .github/workflows/build.yml)
git clone https://github.com/Cocos2D-Mono/MonoGame.git ../MonoGame
git -C ../MonoGame checkout <MONOGAME_FORK_REF>
git -C ../MonoGame submodule update --init ThirdParty/StbImageSharp ThirdParty/StbImageWriteSharp

# 3. The fork's MonoGame.Framework for that platform
dotnet build ../MonoGame/MonoGame.Framework/MonoGame.Framework.MacCatalyst.csproj -c Release
#    tvOS: MonoGame.Framework.tvOS.csproj

# 4. The engine for that platform (Box2D first, as CI does)
TFM=net10.0-maccatalyst26.0                  # tvOS: net10.0-tvos26.0
dotnet build src/Box2D/Box2D.csproj -c Release -f $TFM -p:TargetFrameworks=$TFM
dotnet build src/Cocos2DMono/Cocos2DMono.csproj -c Release -f $TFM -p:TargetFrameworks=$TFM
```

A game can instead reference `src/Cocos2DMono/Cocos2DMono.csproj` from its own `net10.0-maccatalyst` or `net10.0-tvos` project: once step 3 has run, the engine finds the fork's assembly and adds the target on its own. `Directory.Build.props` documents the overrides for a fork checked out somewhere else.

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
