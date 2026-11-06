// Checks a packed Cocos2D-Mono package on the OS it runs on, the way a game gets it: restored
// from the .nupkg that pack.yml built, not compiled from this repository (C2D-357).
//
// The two are different assemblies. The desktop label backend is chosen when the engine is
// compiled, and pack.yml compiles on Windows, so the package carries the GDI+ backend to every
// desktop OS. A build on Linux or macOS compiles the Skia backend instead, so it can't show what
// Linux and macOS users get. From 2.6.0 to 2.9.0 the package's CCLabel threw on both, while
// every CI cell was green.
//
// pack.yml runs this on Linux, macOS and Windows after packing, and won't publish unless all
// three pass. To run it by hand, put that workflow's nuget-packages artifact in artifacts/ at
// the repository root, then run it with an empty package cache, so no older copy of the same
// version answers:
//
//   NUGET_PACKAGES=$(mktemp -d) dotnet run --project Tests/Cocos2DMono.PackageCheck -c Release
//
// A package packed on Linux or macOS passes whatever the GDI+ backend does, so check the one
// pack.yml built.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Cocos2D;
using Microsoft.Xna.Framework;

Assembly engine = typeof(CCLabel).Assembly;
Console.WriteLine($"Cocos2D-Mono {engine.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
Console.WriteLine($"  from {engine.Location}");
Console.WriteLine($"  on {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture}), .NET {Environment.Version}");

int failures = 0;

// The type initializer runs the static field initializers of every backend compiled in, so a
// backend that touches a platform API there fails here, before any label exists.
Check("CCLabel's type initializer runs", () =>
{
    RuntimeHelpers.RunClassConstructor(typeof(CCLabel).TypeHandle);
    return null;
});

// CreateFont is where the backends' OS checks hand the platform to GDI+ or Skia, and where
// Skia first loads its native library.
Check("CCLabel makes a font from a family name", () => $"'Arial' gave '{CreateFont("Arial")}'");

// A .ttf file takes the backends' other path. On Windows it's the only code the C2D-357 fix
// changed: GDI+ loads the file into the PrivateFontCollection that is now made on first use.
if (SystemTtf() is not { } ttf)
{
    Console.WriteLine("SKIP  CCLabel loads a .ttf font: no known system .ttf on this machine");
}
else
{
    // The content root a game sets up. CCLabel looks for .ttf files under it, and an absolute
    // path stays absolute.
    CCContentManager.SharedContentManager = new CCContentManager(new GameServiceContainer(), "Content");
    Check($"CCLabel loads a .ttf font ({ttf.File})", () =>
    {
        string family = CreateFont(ttf.File);
        if (family != ttf.Family)
        {
            throw new InvalidOperationException(
                $"it gave '{family}', not '{ttf.Family}': the file didn't load, and the label fell back to its default font");
        }
        return $"'{family}'";
    });
}

// The size the backends draw text at, and whether they draw all of each glyph. On Linux and macOS,
// Skia drew glyphs about half as tall as GDI+ does on Windows, cut off accents, and lost full stops,
// which it drew outside the area CCLabel takes a glyph's pixels from (C2D-360). "Arial" is a
// substitute where it isn't installed, so this checks proportions rather than exact sizes.
Check("CCLabel draws text at its size in points", () =>
{
    // A 40-point em is 53 pixels, since Windows draws a point as 96/72 of a pixel. A capital is
    // 0.6 to 0.8 of an em in common fonts.
    const float em = 40f * 96 / 72;
    object label = NewLabel();
    CallLabel(label, "CreateFont", [typeof(string), typeof(float), typeof(CCRawList<char>)],
        "Arial", 40f, new CCRawList<char>("HEÉ.".ToCharArray()));

    (_, int capital) = DrawnSize(label, 'H');
    if (capital < 0.6f * em || capital > 0.8f * em)
    {
        throw new InvalidOperationException(
            $"an H is {capital} pixels tall, but at 40 points it should be {0.6f * em:0} to {0.8f * em:0}");
    }

    (_, int e) = DrawnSize(label, 'E');
    (_, int eAcute) = DrawnSize(label, 'É');
    if (eAcute < e + 0.1f * em)
    {
        throw new InvalidOperationException($"an É is {eAcute} pixels tall and an E {e}: the accent is cut off");
    }

    (int dotWidth, int dotHeight) = DrawnSize(label, '.');
    if (dotWidth == 0)
    {
        throw new InvalidOperationException("a full stop has no pixels");
    }

    return $"an H is {capital} pixels tall, an É {eAcute - e} taller than an E, and a full stop {dotWidth}x{dotHeight}";
});

Console.WriteLine(failures == 0 ? "The package passed." : $"The package failed {failures} check(s).");
return failures == 0 ? 0 : 1;

void Check(string name, Func<string?> body)
{
    try
    {
        string? detail = body();
        Console.WriteLine($"PASS  {name}{(detail is null ? "" : $": {detail}")}");
    }
    catch (Exception ex)
    {
        failures++;
        Exception inner = ex;
        while (inner.InnerException is not null) inner = inner.InnerException;
        string chain = inner == ex ? ex.GetType().Name : $"{ex.GetType().Name} -> {inner.GetType().Name}";
        Console.WriteLine($"FAIL  {name}: {chain}: {inner.Message}");
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
        {
            Console.WriteLine($"::error title=Package check::{name}: {chain}: {inner.Message}");
        }
    }
}

// The public constructors build the label's texture, which needs a graphics device, so the checks
// call the backends' methods directly, on a label whose constructor never ran. The backends keep
// their font and bitmap in static fields.
static object NewLabel()
{
    object label = RuntimeHelpers.GetUninitializedObject(typeof(CCLabel));
    // CCNode's finalizer runs Cleanup, which throws on a node whose constructor never ran.
    GC.SuppressFinalize(label);
    return label;
}

static object? CallLabel(object label, string name, Type[] parameters, params object?[] args)
{
    MethodInfo method = typeof(CCLabel).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, parameters)
        ?? throw new MissingMethodException(
            $"CCLabel.{name}({string.Join(", ", Array.ConvertAll(parameters, p => p.Name))}) is gone. These checks call the label backends' methods; point them at the new ones.");
    try
    {
        return method.Invoke(label, args);
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        throw;
    }
}

// CreateFont is the backends' font entry point.
static string CreateFont(string fontName)
{
    return (string)CallLabel(NewLabel(), "CreateFont", [typeof(string), typeof(float), typeof(CCRawList<char>)],
        fontName, 40f, new CCRawList<char>("CCLabel".ToCharArray()))!;
}

// The size of a glyph's pixels, found as CCLabel finds them: the backend measures the glyph and draws
// it, and CCLabel searches the measured size, plus two pixels each way, for pixels that aren't zero.
static unsafe (int Width, int Height) DrawnSize(object label, char glyph)
{
    string text = glyph.ToString();
    var size = (CCSize)CallLabel(label, "GetMeasureString", [typeof(string)], text)!;
    int width = (int)Math.Ceiling(size.Width + 2);
    int height = (int)Math.Ceiling(size.Height + 2);

    object?[] args = [text, null];
    var pixels = (byte*)Pointer.Unbox(CallLabel(label, "GetBitmapData", [typeof(string), typeof(int).MakeByRefType()], args)!);
    int stride = (int)args[1]!;

    int left = width, top = height, right = -1, bottom = -1;
    for (int y = 0; y < height; y++)
    {
        var row = (int*)(pixels + y * stride);
        for (int x = 0; x < width; x++)
        {
            if (row[x] != 0)
            {
                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
            }
        }
    }
    return right < 0 ? (0, 0) : (right - left + 1, bottom - top + 1);
}

// A font file the OS ships, with the family it holds, so the .ttf check can tell a font that
// loaded from the default the label falls back to.
static (string File, string Family)? SystemTtf()
{
    (string File, string Family)[] candidates =
    [
        (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"), "Arial"),
        ("/System/Library/Fonts/Supplemental/Arial.ttf", "Arial"),
        ("/Library/Fonts/Arial.ttf", "Arial"),
        ("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "DejaVu Sans"),
        ("/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf", "Liberation Sans"),
    ];
    foreach (var candidate in candidates)
    {
        if (System.IO.File.Exists(candidate.File)) return candidate;
    }
    return null;
}
