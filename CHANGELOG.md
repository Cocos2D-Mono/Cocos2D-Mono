# Changelog

All notable changes to Cocos2D-Mono are recorded here. This file was introduced with
2.5.10; earlier releases are described in their GitHub release notes / git history.
The project follows [Semantic Versioning](https://semver.org/) where practical.

## Unreleased

### Changed

- **Nullable annotations for the menus** (`CCMenu` and the menu items). The visible changes:
  - `CCMenu.SelectedItem` and `FocusedItem` are nullable: no item is selected or has the focus
    until one is touched or focused.
  - `CCMenuItem`'s `OnActivated`, `OnSelected` and `OnUnselected` events are nullable, and so
    is the selector its constructor, `InitWithTarget` and `SetTarget` take.
  - `CCMenuItemLabel.Label` and `CCMenuItemSprite`'s `NormalImage`, `SelectedImage` and
    `DisabledImage` are nullable. Each can be set to null, and an item made with the
    parameterless constructor has none.
  - `CCMenuItemImage`'s constructors, and `CCMenuItemSprite`'s constructors that take image
    nodes, accept null for the selected and disabled images. The four-argument ones accept it
    for the normal image too.

  Overrides of `CCMenuItemFont.InitWithString` must set the font name, for example by calling
  the base method; the compiler now checks.
- **Nullable annotations for the misc nodes** (the clipping, draw, lightning, motion streak,
  parallax scroll, progress timer and render texture nodes). The visible changes:
  - `CCClippingNode.Stencil` is nullable, and so is the stencil `Init` takes: a clipping node
    made with the parameterless constructor has none.
  - `CCMotionStreak.Texture` is nullable: a streak made with the parameterless constructor has
    none, and neither has one whose texture file couldn't be loaded.
  - `CCParallaxScrollNode.BatchNode` is nullable: a node made with the parameterless
    constructor doesn't batch its sprites.

  Overrides of `CCDrawNode.Init` and `CCRenderTexture.InitWithWidthAndHeight` must call the
  base method, and overrides of `CCProgressTimer.InitWithSprite` must set the sprite, for
  example by calling the base method; the compiler now checks. Overrides of
  `CCMotionStreak.InitWithFade` that take a texture should declare it nullable.
- **Nullable annotations for the tile maps** (TMX maps with their layers, object groups and
  tilesets, tile map atlases and parallax nodes). The visible changes:
  - `CCTMXTiledMap.LayerNamed` and `ObjectGroupNamed`, `CCTMXObjectGroup.ObjectNamed` and
    `CCTMXLayer.TileAt` return null for a name or position with nothing there, as they already
    did.
  - `CCTMXLayer.Tiles` and `CCTileMapAtlas.TGAInfo` are nullable: `ReleaseMap` drops them.
  - `CCTMXMapInfo.TMXFileName` is nullable: a map read from a stream has no file name.
  - `CCTMXTilesetInfo.m_sName` and `m_sSourceImage`, `CCTMXObjectGroup.GroupName` and
    `CCPointObject.Child` are nullable until they're set.
  - `CCTMXLayerInfo.Tiles` is an empty array, not null, for a layer info made with `new`.

  Overrides of `CCTMXLayer.InitWithTilesetInfo` and `CCTMXTiledMap.InitWithTmxFile` must call
  the base method; the compiler now checks.
- **Nullable annotations for the support helpers** (`CCUserDefault`, `CCObjectPool`, the
  `CCPoint`, `CCSize` and `CCRect` type converters, `CCUtils` and `CCImageTGA`). The visible
  changes:
  - `CCUserDefault.GetStringForKey` accepts a null default, which it returns for a missing key.
    With a non-null default, the result is non-null.
  - `CCObjectPool`'s optional `parent` is nullable.
  - `CCPointConverter.CCPointFromString`, `CCSizeConverter.CCSizeFromString`,
    `CCRectConverter.CCRectFromString` and `CCUtils.SplitWithForm` accept null, which gives
    zero (or false), as it already did.
  - `CCImageTGA.imageData` is an empty array, not null, for an image made with the
    parameterless constructor.

  Overrides of the converters' `ConvertFrom` and `ConvertTo` should declare `context`,
  `culture` and `ConvertTo`'s `value` nullable, as `TypeConverter` does.
- **Gzip data is decompressed with .NET's `System.IO.Compression`.** That covers TMX layers with
  `compression="gzip"`, gzip-compressed particle textures and `ZipUtils.InflateMemory`. The engine
  used to bundle an internal port of an old zlib library, from before Windows Phone had
  `System.IO.Compression`; it's removed. No public API changes.

### Fixed

- Giving the focus to a `CCMenu` with no items no longer throws `NullReferenceException`.
- Pressing a gamepad button while a `CCMenu` has the focus but none of its items does no
  longer throws `NullReferenceException`; nothing is selected.
- `CCMenu.AddChild` throws `ArgumentException` for a child that isn't a menu item, in release
  builds as well as debug ones. Release builds used to add it, leaving a null in the menu's
  item list that threw `NullReferenceException` when the menu entered a scene.
- A `CCMenuItemToggle` made with the parameterless constructor starts with an empty item list,
  so `AddSubItem` and `Enabled` no longer throw `NullReferenceException`.
- Enabling or disabling a `CCMenuItemLabel` that has no label no longer throws
  `NullReferenceException`.
- `CCMenuItemLabel.Text` and `SetString` throw `InvalidOperationException` for an item with no
  label, or whose label has no text, instead of `NullReferenceException`. So do
  `CCMenuItemFont`'s `ItemFontName`, `ItemFontSize` and `Color` setters, which rebuild the
  label from its text.
- A `CCLightningStreak` made with the parameterless constructor no longer throws
  `NullReferenceException` when it updates. It has no track to follow, so it stays where it is.
- `CCParallaxScrollNode`'s sprite sheet constructor throws `ArgumentException` for a sheet whose
  first frame has no texture, instead of `NullReferenceException`.
- `CCProgressTimer`'s constructor and `Sprite` setter throw `ArgumentNullException` for a null
  sprite. The constructor used to make a timer that drew nothing, and replacing a sprite with
  null threw `NullReferenceException` after clearing it.
- A TMX map with a layer whose tiles are all empty no longer throws `NullReferenceException`
  when it's built. Such a layer has no tileset to draw with, so it isn't created, as in
  cocos2d-x 3, and `LayerNamed` returns null for it.
- A TMX map read from a stream (`CCTMXMapInfo` and `CCTMXTiledMap` from a `StreamReader`) no
  longer throws `ArgumentNullException` at its first tileset image. With no map file for the
  image path to be relative to, the path is used as written, without its extension.
- `CCTMXTiledMap.LayerNamed` returns null on a map with no layers, instead of throwing
  `NullReferenceException`.
- `CCTMXObjectGroup.ObjectNamed` skips objects without a name. An unnamed object before the one
  asked for made it throw `KeyNotFoundException`.
- Gzip-compressed layer data that can't be decompressed leaves the layer empty, as the logged
  error intends, instead of throwing `NullReferenceException`.
- A malformed TMX file with a `<tile>`, `<image>`, `<data>`, `<object>`, `<property>` or
  `<polygon>` element outside the element it belongs to throws an `InvalidDataException` that
  names the element, instead of `NullReferenceException`.
- `CCTMXLayer` throws `ArgumentException` for a tileset with no image, or one whose image can't
  be loaded. It used to fail inside the texture cache or throw `NullReferenceException`.
- The `CCPoint`, `CCSize` and `CCRect` type converters convert a null value to an empty string,
  as `TypeConverter` does, instead of throwing `NullReferenceException`. For example,
  `TypeDescriptor.GetConverter(typeof(CCPoint)).ConvertToString(null)`.
- Gzip data whose checksum doesn't match is rejected, like other data that can't be
  decompressed: a TMX layer stays empty, `ZipUtils.InflateMemory` returns 0, and a particle
  system gets the default texture. The checksum used to go unchecked, so corrupt data became
  wrong tiles or pixels.
- A particle system whose embedded texture can't be decompressed or decoded gets the default
  texture, as when decoding throws. It used to be left without a texture, and debug builds
  failed an assertion.

## 2.7.0 - 2026-10-16

The first nullable reference type annotations. The base nodes, input, layers and scenes,
actions, particles, sprites, textures and labels now say which of their members can be null,
so a project with nullable enabled is warned where it uses one without a check. Converting
them also turned up the long-standing bugs fixed below. The rest, including menus, tile
maps, the director and scheduler, the platform code, the extensions and Box2D, follows in
later 2.x releases.

### ⚠️ Upgrading

Bump the package reference to 2.7.0. Nothing stops compiling: apart from nullability, no
public API changed or went away since 2.6.2. Three things can still affect a game:

- **New warnings in projects that enable nullable.** Using a member that can be null without
  a check, such as `CCNode.Parent` or the result of `CCNode.GetChildByTag`, now raises a
  nullable warning (CS86xx). A build with `TreatWarningsAsErrors` fails on them until the
  code checks for null; `<WarningsNotAsErrors>nullable</WarningsNotAsErrors>` keeps them as
  warnings in the meantime. Projects without nullable don't get these warnings.
- **Some failures throw a more specific exception.** For example, several methods given a
  texture or font file that can't be loaded throw `ArgumentException`, and reversing an
  action that has no reverse throws `NotSupportedException`; both used to end in
  `NullReferenceException`. `CCEaseElastic.Reverse()` and `CCGridAction.Grid` throw
  `NotSupportedException` instead of returning null. Code that catches
  `NullReferenceException` from the engine, or checks for those nulls, should handle the new
  exceptions; Changed and Fixed list each one.
- **Some fixes change what existing code does.** Check code that worked around the old
  behavior:
  - `CCShuffleTiles` and `CCTurnOffTiles` follow their seed.
  - `CCMoveFrom` and `CCColorBlendAnimation` change their node, and `CCAccelDeccelAmplitude`
    animates the effect it wraps; none of them did before.
  - `Copy(zone)` on `CCScaleTo`, `CCScaleBy`, `CCReverseTime` and `CCOrbitCamera` fills the
    copy instead of overwriting the original, and a copied `CCCardinalSplineBy`,
    `CCCatmullRomTo` or `CCCatmullRomBy` keeps its type.
  - `CCLayerMultiplex` numbers a layer added after a tagged one consecutively.
  - `CCSpriteSheetCache.AddSpriteSheet` uses the stream or dictionary it's given.

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
- **Nullable annotations for the foundation types** (math, geometry, shared types, script
  support and audio). The visible changes:
  - `ICCCopyable.Copy` takes a nullable `zone`; the implementations already handle null.
  - `CCSimpleAudioEngine.PlayEffectHandled` returns `CCSoundHandle?`, as its documentation
    already said it could.
  - `CCScriptEngineManager.ScriptEngine` is nullable.
  - The geometry types' `Equals(object)` overrides accept null.
- **Nullable annotations for input handling** (the touch, keyboard and keypad dispatchers
  and the text input node). The visible changes:
  - `CCTouchDispatcher.FindHandler` returns `CCTouchHandler?`. It already returned null for
    a delegate that isn't registered.
  - `CCKeypadHandler.Delegate` is nullable: a handler created with `new` has no delegate
    until `InitWithDelegate`.
- **Nullable annotations for layers, scenes and transitions.** The visible changes:
  - `CCLayerMultiplex`'s `SwitchTo`, `SwitchToFirstLayer`, `SwitchToNextLayer`,
    `SwitchToPreviousLayer`, `SwitchToAndReleaseMe` and `ActiveLayer` return `CCLayer?`.
    They already returned null when there was no layer to show.
  - `CCLayerMultiplex.InAction`, `OutAction` and the constructors' action arguments are
    nullable.
  - Overrides of `CCTransitionScene.InitWithDuration(float, CCScene)` must set the in and
    out scenes, for example by calling the base method; the compiler now checks.
- **Nullable annotations for particles and grid effects.** The visible changes:
  - `CCParticleSystem.Texture` and `BatchNode` are nullable: a system has no texture until
    one is set, and a batch node only while it renders through one.
  - `CCParticleEmitterLight.OnUpdateParticle` is nullable; it's optional.
- **Nullable annotations for the core actions** (`CCAction`, `CCActionManager` and the
  instant actions). The visible changes:
  - `CCAction.Target` and `OriginalTarget` are nullable: an action has no target until it
    starts, and `Stop` clears `Target`.
  - `CCFiniteTimeAction.Reverse()` returns `CCFiniteTimeAction?`, since the base class has
    no reverse. Every engine action except `CCBRotateTo`, `CCBRotateXTo` and `CCBRotateYTo`
    overrides it with a non-null result.
  - `CCActionManager.GetAction` and `CCNode.GetAction` return `CCAction?`; they already
    returned null for an unknown tag.
  - `CCCallFuncO`'s callback takes `object?`, since its object can be null.

  Subclasses that override `Copy(ICCCopyable zone)` should declare `zone` nullable; the
  compiler now checks it.
- **Nullable annotations for the interval actions.** The visible changes:
  - `CCActionTween`'s five-argument constructor accepts a null callback, which then works
    like the four-argument one.
  - `CCParallel.Actions` is an empty array, not null, for a `CCParallel` made with the
    parameterless constructor.
- **Nullable annotations for the ease, camera, spline and progress actions.** The visible
  change: `CCEaseElastic.Reverse()` throws `NotSupportedException` instead of returning
  null. The base elastic ease has no curve of its own to reverse; `CCEaseElasticIn`,
  `CCEaseElasticOut` and `CCEaseElasticInOut` reverse as before.
- **Nullable annotations for the grid actions.** The visible changes:
  - `CCGridAction.Grid` throws `NotSupportedException` instead of returning null for a grid
    action that doesn't create a grid, so running one fails with that instead of
    `NullReferenceException`. `CCGrid3DAction` and `CCTiledGrid3DAction` create a grid, as
    before.
  - `CCShuffleTiles` and `CCTurnOffTiles` keep empty tile arrays, not null, until they
    start.
- **Nullable annotations for sprite frames, animations and their caches.** The visible
  changes:
  - `CCSpriteFrame.Texture` and `TextureFilename` are nullable, and so is the texture its
    constructors take: a frame made with the parameterless constructor has no texture.
  - `CCSpriteFrameCache.SpriteFrameByName`, `CCSpriteSheet.SpriteFrameByName` and its
    indexer, `CCSpriteSheetCache.SpriteSheetForKey`, and `CCAnimationCache.AnimationByName`
    and its indexer return null for an unknown name, as they already did.
  - `CCAnimationFrame.UserInfo` is nullable.
  - `CCSpriteFrameCache.pSharedSpriteFrameCache` is nullable. `SharedSpriteFrameCache`
    creates the cache on first use.
  - A `CCSpriteFrameCache` or `CCAnimationCache` made with `new` works before `Init`, which
    now only clears it. Both used to throw `NullReferenceException` until then.
- **Nullable annotations for sprites and sprite batch nodes.** The visible changes:
  - `CCSprite.Texture` and `BatchNode` are nullable, and so is the texture
    `InitWithTexture(texture, rect)` takes: a sprite can be made without a texture, and only
    a batched sprite has a batch node.
  - `ICCTextureProtocol.Texture` can read as null, as a sprite's or particle system's
    texture already could, but can't be set to null through the interface.
  - `CCMaskedSprite.CollisionMask` is nullable: a masked sprite made with the parameterless
    constructor has no mask.
- **Nullable annotations for textures.** The visible changes:
  - `CCTextureCache.AddImage`, `AddRawImage`, `TextureForKey` and its indexer return
    `CCTexture2D?`. They already returned null for a texture that can't be loaded or isn't
    cached. `AddImageAsync`'s callback is optional and can receive null for the same reason.
  - `CCTextureAtlas.Create(file, capacity)` returns null when the file can't be loaded.
  - `CCTexture2D.XNATexture`, `Name` and `OnReInit` are nullable: a texture that hasn't been
    created, or couldn't be recreated, has no XNA texture.
- **Nullable annotations for the core labels** (`CCLabelBMFont`, `CCBMFontConfiguration`,
  `CCLabelAtlas`, `CCLabelTTF` and `CCPixelLabel`). The visible changes:
  - `CCBMFontConfiguration.Create` and `CCLabelBMFont.FNTConfigLoadFile` return
    `CCBMFontConfiguration?`. They already returned null for a font file with no data.
  - `CCBMFontConfiguration.AtlasName` is nullable: it's null until a font file names its
    texture.
  - `CCLabelBMFont.FntFile` is nullable: a label made with the parameterless constructor has
    no font.
  - `CCPixelLabel.Text`, its constructors' `text` and `MeasureString` accept null, as they
    already did.

  Subclasses of `CCLabelBMFont` that override `InitWithString` should declare its text, font
  file and texture nullable; the compiler now checks it.
- **Nullable annotations for `CCLabel`.** `CCLabel.m_pTexture` and `m_pData`, the atlas the
  TTF labels share, are nullable: they're null until a label is given text or a font, and
  `CCLabelBMFont.FNTConfigRemoveCache` clears them.

### Fixed

- `CCNode.Compare` accepts null arguments, ordering them first as `IComparer<T>` allows,
  instead of throwing.
- `CCNode.SortAllChildren` and `UpdateZOrderRange` no longer throw on a node with no
  children.
- `CCTapNode.RefreshContentSize` sizes a node with no children to zero instead of throwing.
- Deserializing malformed node data throws an `InvalidDataException` that names the problem,
  instead of a null reference error.
- `CCPoint`, `CCSize` and `CCRect` `Equals(object)` return false for null or a value of
  another type. They used to throw `NullReferenceException` or `InvalidCastException`.
- `CCLightningTrack.GetPoint` no longer throws when a subclass overrides `CreateBolt`
  without calling the base method; it falls back to its older algorithm instead.
- `CCLightningTrack.GetPoint`'s fallback interpolates on the segment that contains the
  requested position. It used to skip that segment, returning a point on the next one or
  the end of the bolt.
- `CCTouchDispatcher.SetPriority` works during a touch dispatch. For a delegate added in
  the same dispatch, such as a popup menu opened from a menu callback, it no longer
  throws, and the priority applies when the delegate joins. Called from `TouchBegan` for
  a registered delegate, it no longer throws `InvalidOperationException`; the new order
  applies from the next touch. `UpdateGraphPriority` also reaches a delegate added in the
  same dispatch.
- `CCTouchDispatcher.SetPriority` and `UpdateGraphPriority` update both handlers of a
  delegate registered as both targeted and standard, instead of only the first.
- `CCTouchDispatcher.SetPriority` throws `ArgumentException` for a delegate that isn't
  registered, instead of `NullReferenceException`.
- A `CCTouchDispatcher` used before `Init()` no longer throws when delegates are added or
  removed.
- `CCLayerMultiplex.SwitchTo` returns null for a layer released by `SwitchToAndReleaseMe`
  instead of throwing, and switching away from a released active layer no longer throws.
- `CCParticleSystemQuad.SetDisplayFrame` takes the frame's texture on a system that has
  none, instead of throwing. Its texture check was inverted.
- Setting `CCParticleSystemQuad.Texture` to null clears the texture instead of throwing,
  as `CCParticleSystem` already did.
- `CCLayerMultiplex.SwitchToFirstLayer`, `SwitchToNextLayer` and `SwitchToPreviousLayer`
  work for a multiplex built from a layer list; they used to return null. They also skip
  layers released by `SwitchToAndReleaseMe` instead of showing nothing, and so does the
  layer a multiplex shows when it enters a scene.
- `CCLayerMultiplex.AddLayer` numbers layers consecutively. A layer added after a tagged
  one used to get an index one higher than expected.
- `CCLayerMultiplex.SwitchToAndReleaseMe` releases a tagged layer's tag as well as its
  index. Releasing the active layer itself now removes it and shows nothing, instead of
  leaving it on screen and still marked active.
- `CCMoveFrom`, `CCColorBlendAnimation`, `CCRotateAnimation` and `CCTimerAction` now set
  their target when they start. Before, the first two never changed their node,
  `CCRotateAnimation` threw `NullReferenceException` as soon as it ran, and the action
  manager couldn't remove any of them. `CCRotateAnimation` starts from `Rotation` when the
  node doesn't implement `ICCRotationAnimationGetter`.
- A `CCRepeat` around a `CCBlink` no longer throws `NullReferenceException` when it
  finishes. `CCRepeat` stops its inner action twice at the end, and `CCBlink.Stop`
  dereferenced the target the first stop had cleared.
- Reversing a `CCSequence`, `CCSpawn`, `CCRepeat`, `CCParallel` or `CCTargetedAction` whose
  part has no reverse throws `NotSupportedException` naming that action, instead of
  failing later with `NullReferenceException`.
- Reversing an ease action whose inner action has no reverse throws `NotSupportedException`
  naming that action, instead of `NullReferenceException`.
- Copying a `CCCardinalSplineBy`, `CCCatmullRomTo` or `CCCatmullRomBy` returns an action of
  the same type. It used to return a `CCCardinalSplineTo`, so a copied spline-by action moved
  the node to the spline's absolute points instead of relative to where it started.
- `Copy(zone)` on an interval action throws `InvalidCastException` for a zone of another
  type, as most already did. Some returned null and others threw `NullReferenceException`.
- `CCShuffleTiles` and `CCTurnOffTiles` use their seed, so the same seed gives the same tile
  order. `CCShuffleTiles` replaced the seed with a random number, and `CCTurnOffTiles`
  ignored it and threw for a negative seed other than -1. A seeded run no longer touches
  `CCRandom`, and a `CCTurnOffTiles` made without a seed still picks a new order each run.
- `CCScaleTo.Copy(zone)`, which `CCScaleBy` also uses, `CCReverseTime.Copy(zone)` and
  `CCOrbitCamera.Copy(zone)` fill the zone from the original. They used to copy the zone's
  values into the original.
- Copying a `CCReverseTime` copies its inner action, so the original and the copy can run
  at the same time on different nodes. They used to share it.
- `CCAccelDeccelAmplitude` updates its inner action, so the grid effect it wraps animates. It
  only set the effect's amplitude and never updated it, an omission cocos2d-x shares.
- `CCAccelAmplitude`, `CCAccelDeccelAmplitude` and `CCDeccelAmplitude` throw
  `InvalidCastException` when given an action that isn't an interval action, instead of
  `NullReferenceException` when they start.
- `CCSpriteSheetCache.AddSpriteSheet` builds the sheet from the stream or dictionary it's
  given. Those overloads used to ignore it and load a plist named after the cache key.
- `CCSpriteFrameCache` and `CCSpriteSheet` load a plist with no frames, or a SpriteKit plist
  with no images, as empty. They used to throw `NullReferenceException`.
- `CCSpriteFrameCache.RemoveSpriteFrameByName` removes a frame that has no alias instead of
  throwing `KeyNotFoundException`. Removing a frame also removes its aliases, so they don't
  find a frame added later under the same name.
- `CCSpriteFrameCache.RemoveSpriteFramesFromTexture` skips frames that have no texture, and
  `CCParticleSystemQuad.SetDisplayFrame` with such a frame clears the system's texture.
  Both used to throw `NullReferenceException`.
- `CCSprite.SortAllChildren` on a sprite with no children yet, `RemoveAllChildren` on a
  batched sprite with no children, and `IsSpriteFrameDisplayed` with no textures no longer
  throw `NullReferenceException`.
- `CCSpriteBatchNode.Compare` accepts null arguments, ordering them first as
  `CCNode.Compare` does, instead of throwing.
- `CCAnimate.Stop` no longer throws when the animation's `RestoreOriginalFrame` was turned on
  after the action started; there's no original frame to restore.
- `CCAnimation.AddSpriteFrameWithFileName`, `CCAtlasNode.InitWithTileFile`,
  `CCParticleBatchNode.InitWithFile` and `CCSpriteBatchNode`'s file constructors throw
  `ArgumentException` naming a texture file that can't be loaded, instead of
  `NullReferenceException`.
- `CCBMFontConfiguration` reads the last line of a font file that doesn't end with a
  newline. It used to throw `NullReferenceException`.
- `CCBMFontConfiguration` keeps the last character of a value that ends its line. It used
  to drop it when nothing followed, not even a carriage return: a line of just `char id=65`
  read as character 6.
- `CCBMFontConfiguration.CharacterSet` is empty, not null, for a font file with no data.
- `CCLabelBMFont.FNTConfigLoadFile` doesn't cache a font that fails to load, so loading it
  again under that name can succeed. It used to return null for that name from then on.
- A `CCLabelBMFont` with no font no longer throws `NullReferenceException` laying out its
  text once its `Dimensions` have a height.
- `CCLabelBMFont`'s font file constructors and its `FntFile` setter throw
  `ArgumentException` naming a font file that doesn't name a texture, or a texture that
  can't be loaded. They used to throw `NullReferenceException` or `ArgumentNullException`.
  `FntFile` also throws it for a font file with no data, and now checks the font before
  changing the label.
- `CCLabelAtlas.InitWithString(label, fntFile)`, which its two-argument constructor calls,
  throws `ArgumentException` for a file that isn't a property list dictionary, instead of
  `NullReferenceException`.
- A `CCLabel` drawn before any label has been given text or a font no longer throws
  `NullReferenceException`; there's no shared atlas to upload yet.

### Documentation

- `CONTRIBUTING.md` describes how the engine moves to nullable reference types: a file that
  starts with `#nullable disable` hasn't been converted yet, and converting one marks what
  can really be null instead of silencing warnings.

## 2.6.2 - 2026-10-13

A small release with no API changes and nothing to migrate: bump the package reference to
2.6.2. The packaged platforms behave as in 2.6.1, and Mac Catalyst and tvOS can now be
built from source.

### Added

- **Mac Catalyst and tvOS targets** (`net10.0-maccatalyst26.0` and `net10.0-tvos26.0`), for
  building the engine from source. MonoGame publishes no package for either platform, so
  both build against the project's MonoGame fork and aren't in the NuGet packages. The
  README's Platform support section has the build steps. On tvOS the engine runs the iOS
  code without a touch screen or accelerometer; the Siri Remote's selection arrives as
  gamepad input.

### Documentation

- The README describes platform support in three tiers: the platforms in the NuGet
  packages, the source-only Mac Catalyst and tvOS targets, and consoles for registered
  developers.
- The roadmap records the platform plan after the modernization: follow MonoGame's graphics
  backends, and move a platform into the packages once its MonoGame dependency can be
  restored from nuget.org and CI builds it. A new versioning section says baseline moves
  ship in minor releases and 3.0 will be an API-cleanup release.
- `CONTRIBUTING.md` describes how a release is tagged and published, and the rule that a
  package only carries a platform whose MonoGame dependency is on nuget.org.

### Changed (internal)

- Packages are published to nuget.org from a `v`-prefixed release tag, starting with
  `v2.6.2`, through nuget.org Trusted Publishing once a maintainer approves. Earlier
  releases keep their unprefixed tags.
- `dotnet pack` leaves out the source-only Mac Catalyst and tvOS targets, even on a Mac
  that builds them.
- Removed internal label code that nothing called: `CCLabelUtilities`' Skia and GDI paths.

## 2.6.1 - 2026-10-06

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
