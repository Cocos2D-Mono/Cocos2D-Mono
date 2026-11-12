using System.Text.RegularExpressions;
using Xunit;

namespace Cocos2DMono.UnitTests;

// Android's asset paths are case-sensitive and Windows' aren't, so a path in the test app whose case
// differs from its file's loads on Windows but not on Android (C2D-375). ParticleTest asked for
// "Images/fps_Images", and on Android the app ended when the scene opened. The test app builds only
// for its platforms, so this reads its source instead. Any string literal without escapes that
// names one of its content files, ignoring case, has to match the file's case. A path that the code
// builds from parts isn't checked.
public class TestAppContentPathTests
{
    private static readonly Regex StringLiteral = new Regex("\"([^\"\\\\\\r\\n]+)\"");

    [Fact]
    public void ContentPathsInTheTestApp_MatchTheirFilesCase()
    {
        string tests = Path.Combine(RepositoryRoot(), "Tests", "cocos2d-mono.Tests");
        string content = Path.Combine(tests, "Content");

        // Each content file's path as code names it, with its extension and without.
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(content, "*", SearchOption.AllDirectories))
        {
            string path = Relative(content, file);
            // The build output, and the content project itself, which the code never loads.
            if (path.StartsWith("bin/", StringComparison.Ordinal) || path.StartsWith("obj/", StringComparison.Ordinal) ||
                path == "Content.mgcb")
            {
                continue;
            }
            paths.Add(path);
            paths.Add(WithoutExtension(path));
        }
        var byLowercase = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in paths)
        {
            byLowercase.TryAdd(path.ToLowerInvariant(), path);
        }

        var mismatches = new List<string>();
        foreach (string source in Directory.EnumerateFiles(tests, "*.cs", SearchOption.AllDirectories))
        {
            string name = Relative(tests, source);
            if (name.Contains("/bin/", StringComparison.Ordinal) || name.Contains("/obj/", StringComparison.Ordinal) ||
                name.StartsWith("bin/", StringComparison.Ordinal) || name.StartsWith("obj/", StringComparison.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(source);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (Match match in StringLiteral.Matches(lines[i]))
                {
                    string path = match.Groups[1].Value;
                    foreach (string form in new[] { path, WithoutExtension(path) })
                    {
                        if (paths.Contains(form))
                        {
                            break;
                        }
                        if (byLowercase.TryGetValue(form.ToLowerInvariant(), out string? onDisk))
                        {
                            mismatches.Add($"{name}:{i + 1}: \"{path}\", but the file is \"{onDisk}\"");
                            break;
                        }
                    }
                }
            }
        }

        Assert.True(mismatches.Count == 0, "These paths' case differs from their files', so they don't load on Android:\n" +
            string.Join("\n", mismatches));
    }

    private static string Relative(string folder, string path) => Path.GetRelativePath(folder, path).Replace('\\', '/');

    private static string WithoutExtension(string path)
    {
        int slash = path.LastIndexOf('/');
        int dot = path.LastIndexOf('.');
        return dot > slash ? path.Substring(0, dot) : path;
    }

    // The repository, found by walking up from where the tests run.
    private static string RepositoryRoot()
    {
        for (DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            if (Directory.Exists(Path.Combine(folder.FullName, "Tests", "cocos2d-mono.Tests", "Content")))
            {
                return folder.FullName;
            }
        }
        throw new DirectoryNotFoundException("The test app's sources aren't in any folder above " + AppContext.BaseDirectory);
    }
}
