# Contributing to Cocos2D-Mono

Thanks for your interest in the project. This document covers the working conventions
that apply across **all** Cocos2D-Mono repositories — how work is branched, verified,
and released. For user-facing guides (installation, tutorials, API usage), see
[cocos2d-mono.dev](https://cocos2d-mono.dev/docs/category/contributing).

The [contributing guides on the site](https://cocos2d-mono.dev/docs/category/contributing)
cover the same conventions in more detail, including code style. Where the two ever
disagree, **this file is authoritative** — it ships with the code and is versioned
alongside it.

## Repository map

The project spans several repositories. Changes to the engine often have downstream
counterparts, and knowing which repo owns what saves a lot of time.

| Repository | Contents | Notes |
| --- | --- | --- |
| `cocos2d-mono` | The engine, Box2D port, and the integration/unit test projects | Publishes the NuGet packages |
| `Cocos2D-Mono.Docs` | The documentation site (Docusaurus) | Publishes from `main` — merged work is not live until released |
| `Cocos2D-Mono.Samples` | Sample games and the tutorial sample projects | Tutorial checkpoints must stay in sync with the docs |
| `Cocos2D-Mono.ProjectTemplates` | `dotnet new` templates and the Visual Studio extension | Ships to NuGet *and* the VS Marketplace |
| `Cocos2D-Mono.Tests` | The `Cocos2DMono.IntegrationTests` host plus shared scenes, built against the **published** packages | A separate repo — not this one's `Tests/` directory. Validates what consumers actually install; store-packaging vehicle |

## Branching and pull requests

**Feature branches are cut from `dev`, and pull requests target `dev`.** The default
branch (`main`, or `master` in the engine repo) is **release-only** — it receives merges
from `release/**` branches and release-coupled content, never direct feature work.
This holds in every repository listed above.

```bash
git checkout dev
git pull --ff-only
git checkout -b feature/my-change   # or fix/, docs/, chore/
```

Keep pull requests **small and reviewable**. A large change split into a sequence of
focused PRs lands faster and is far easier to revert if something goes wrong. Prefer
mechanical changes (renames, moves) in their own PR, separate from behavior changes.

After a PR merges, sync `dev` locally and delete the feature branch.

## Commits

- Write commits under your own identity. Don't add co-author trailers for tools or
  assistants that helped produce the change.
- Use imperative subject lines with a type prefix (`fix:`, `feat:`, `docs:`, `refactor:`,
  `chore:`). Explain *why* in the body when the reason isn't obvious from the diff.
- One logical change per commit. When a PR closes multiple issues, one commit per issue
  keeps history readable.

## Verify before you push

- The solution builds clean, with **no new warnings**. Note that incremental builds can
  hide warnings from unchanged files — confirm warning claims with `--no-incremental`.
- Unit tests pass: `dotnet test Tests/Cocos2DMono.UnitTests/Cocos2DMono.UnitTests.csproj`
- Behavior changes come with a regression test where one can reasonably be written.

**A clean build and a no-crash launch do not prove gameplay works.** For anything that
touches rendering, input, physics, or a sample game, actually run it and exercise the
affected behavior. A smoke run only proves the process stayed alive; it cannot catch a
mis-sized hitbox, a collision filter that fails in one direction, or an input path that
silently stopped firing. If you couldn't verify visually, say so explicitly in the PR
rather than describing the change as verified.

Two physics traps worth knowing, both of which have shipped as real bugs here:

- **Collision filtering is two-sided.** A collision only occurs when each fixture's mask
  accepts the other's category. Updating one side alone silently does nothing.
- **Never mutate the world from a contact callback.** `BeginContact`/`EndContact` run
  inside `world.Step` with the world locked, where `DestroyBody` is *silently ignored*.
  Record what happened in the callback and resolve it after the step. Same-step contact
  order is unspecified, so ordering-dependent logic belongs in the resolver too.

## Public API stability

Consumers depend on this library, so public API is changed deliberately:

- Avoid breaking public API. Where a change is warranted, ship an `[Obsolete]` shim first
  and remove it in a later major version.
- Breaking changes require a major version bump and migration notes.

See [ROADMAP.md](ROADMAP.md) for the longer-term direction of the API.

## When you find an unrelated bug

Finding a real bug in the middle of another change is common. **Don't fold it into the
current PR** — especially not into a mechanical one. Open a separate issue describing the
symptom and root cause, and fix it in its own PR with a regression test.

This keeps mechanical changes trivially reviewable and gives each behavior fix a history
someone can find later. The exception is a bug your change directly causes or exposes in
the same lines — fix that in place and say so in the PR description.

## Samples and documentation stay in lockstep

Tutorial pages show code that readers copy into their own projects, so:

- Code blocks in the docs must match the corresponding samples-repo checkpoint
  **verbatim**. If you change sample code that a tutorial teaches, update the page in the
  same round of work.
- Tutorial samples keep a `Checkpoints/Part N/` snapshot per part, plus `Final/`, which
  reflects the latest complete state. Update both.
- Don't teach code a reader's project wouldn't have at that point in the series. A page
  should only build on what earlier parts established.

## Releases (maintainers)

Releases flow through a dedicated branch, not directly to the default branch:

1. Cut `release/<version>` from `dev`.
2. PR that branch to the default branch, merge, and tag.
3. Back-merge the default branch into `dev` so the branches stay aligned.

When a change spans repositories, release them in dependency order: the **engine package
first**, then **samples and project templates** (which consume the published packages),
with **docs** able to go in parallel, and the **Tests** showcase app trailing since it
consumes published packages. Publishing steps that live outside git — NuGet pushes and
the Visual Studio Marketplace upload — are part of the release, not an afterthought.

## Console platforms

Console support (PlayStation, and any future console target) is developed in a **private
repository under platform-holder NDA**. Do not open issues, pull requests, or discussions
in the public repositories that reference console SDKs, toolchains, or platform
documentation. Keep console-specific code and identifiers out of public branches.

## Issue tracking

Day-to-day planning lives in Linear; the GitHub issue tracker remains open for bug
reports and feature requests from the community. When working an issue, keep its status
current and link the pull request, so the board reflects what is actually in flight.
