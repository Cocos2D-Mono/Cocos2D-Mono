#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cocos2D;

/// <summary>
/// Manages font registration and provides utilities for working with fonts
/// across the framework. Supports both content pipeline fonts (.xnb via SpriteFont)
/// and direct TTF file loading (bypasses the content pipeline).
///
/// For direct TTF loading (no content pipeline required), use CCLabel:
/// <code>
/// var label = new CCLabel("Hello", "fonts/myfont.ttf", 24);
/// </code>
///
/// For content pipeline fonts, use CCLabelTTF:
/// <code>
/// var label = new CCLabelTTF("Hello", "arial", 24);
/// </code>
///
/// If the content pipeline fails for certain fonts (e.g. MonoGame's
/// FontDescriptionProcessor rejects the font), switch to CCLabel with
/// a .ttf file path instead.
/// </summary>
public static class CCFontManager
{
    private static readonly HashSet<string> s_registeredTTFPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static readonly StringComparison s_pathComparison =
        Environment.OSVersion.Platform == PlatformID.Win32NT
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>
    /// Registers a TTF font file path for use with CCLabel.
    /// The path must be relative to the content root (e.g. "fonts/myfont.ttf").
    /// Rooted paths and directory traversal (e.g. "..") are rejected.
    /// The TTF file must exist in your content directory.
    /// </summary>
    /// <param name="relativePath">Path relative to content root, e.g. "fonts/myfont.ttf".</param>
    /// <returns>True if the font file exists and was registered.</returns>
    public static bool RegisterTTF(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return false;

        if (Path.IsPathRooted(relativePath) || relativePath.Contains(".."))
        {
            CCLog.Log("CCFontManager: Rejected path '{0}' — must be relative with no '..' traversal", relativePath);
            return false;
        }

        string fullPath = ResolveFontPath(relativePath);

        if (fullPath != null && File.Exists(fullPath))
        {
            s_registeredTTFPaths.Add(relativePath);
            return true;
        }

        CCLog.Log("CCFontManager: TTF file not found at '{0}'", relativePath);
        return false;
    }

    /// <summary>
    /// Checks whether a TTF font has been registered.
    /// </summary>
    public static bool IsTTFRegistered(string relativePath)
    {
        return s_registeredTTFPaths.Contains(relativePath);
    }

    /// <summary>
    /// Returns a copy of all registered TTF font paths.
    /// </summary>
    public static IEnumerable<string> RegisteredTTFFonts
    {
        get { return s_registeredTTFPaths.ToList(); }
    }

    /// <summary>
    /// Resolves a content-relative font path to a full filesystem path.
    /// Rejects rooted paths and directory traversal.
    /// Returns null if the content manager is not initialized or the path is invalid.
    /// </summary>
    public static string ResolveFontPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;

        if (Path.IsPathRooted(relativePath) || relativePath.Contains(".."))
            return null;

        try
        {
            string rootDir = null;

            if (CCContentManager.SharedContentManager != null)
            {
                rootDir = CCContentManager.SharedContentManager.RootDirectory;
            }
            else if (CCApplication.SharedApplication != null)
            {
                var content = CCApplication.SharedApplication.Content;
                if (content != null)
                {
                    rootDir = content.RootDirectory;
                }
            }

            if (rootDir != null)
            {
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                string contentRoot = Path.GetFullPath(Path.Combine(appPath, rootDir));
                if (!contentRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    contentRoot += Path.DirectorySeparatorChar;
                string fullPath = Path.GetFullPath(Path.Combine(contentRoot, relativePath));

                // Verify resolved path stays within the content root
                if (fullPath.StartsWith(contentRoot, s_pathComparison))
                {
                    return fullPath;
                }

                CCLog.Log("CCFontManager: Resolved path escapes content root");
            }
        }
        catch (Exception ex)
        {
            CCLog.Log("CCFontManager: Error resolving font path: {0}", ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Checks whether a font name refers to a TTF file (ends with .ttf extension).
    /// </summary>
    public static bool IsTTFFont(string fontName)
    {
        if (string.IsNullOrEmpty(fontName))
            return false;

        return fontName.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates the appropriate label type for a given font.
    /// If the font name ends with .ttf, creates a CCLabel (native TTF rendering,
    /// bypasses the content pipeline). Otherwise, creates a CCLabelTTF
    /// (content pipeline SpriteFont rendering).
    /// </summary>
    /// <param name="text">Text to display.</param>
    /// <param name="fontName">Font name or TTF path (e.g. "arial" or "fonts/myfont.ttf").</param>
    /// <param name="fontSize">Font size in points.</param>
    /// <returns>A CCNode that displays the text (either CCLabel or CCLabelTTF).</returns>
    public static CCNode CreateLabel(string text, string fontName, float fontSize)
    {
        if (IsTTFFont(fontName))
        {
            return new CCLabel(text, fontName, fontSize);
        }
        return new CCLabelTTF(text, fontName, fontSize);
    }

    /// <summary>
    /// Creates the appropriate label type with alignment support.
    /// If the font name ends with .ttf, creates a CCLabel. Otherwise, creates a CCLabelTTF.
    /// </summary>
    public static CCNode CreateLabel(string text, string fontName, float fontSize,
        CCSize dimensions, CCTextAlignment hAlignment, CCVerticalTextAlignment vAlignment)
    {
        if (IsTTFFont(fontName))
        {
            return new CCLabel(text, fontName, fontSize, dimensions, hAlignment, vAlignment);
        }
        return new CCLabelTTF(text, fontName, fontSize, dimensions, hAlignment, vAlignment);
    }

    /// <summary>
    /// Clears all registered fonts.
    /// </summary>
    public static void Clear()
    {
        s_registeredTTFPaths.Clear();
    }
}
