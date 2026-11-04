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

// The public constructors build the label's texture, which needs a graphics device, so this
// calls the backends' font entry point directly, on a label whose constructor never ran.
// CreateFont only uses the backend's static state.
static string CreateFont(string fontName)
{
    MethodInfo createFont = typeof(CCLabel).GetMethod(
            "CreateFont",
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(string), typeof(float), typeof(CCRawList<char>)])
        ?? throw new MissingMethodException(
            "CCLabel.CreateFont(string, float, CCRawList<char>) is gone. This check calls the label backends' font entry point; point it at the new one.");

    object label = RuntimeHelpers.GetUninitializedObject(typeof(CCLabel));
    var charset = new CCRawList<char>("CCLabel".ToCharArray());
    try
    {
        return (string)createFont.Invoke(label, [fontName, 40f, charset])!;
    }
    catch (TargetInvocationException ex) when (ex.InnerException is not null)
    {
        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        throw;
    }
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
