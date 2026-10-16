#nullable disable
using System;
using Microsoft.Xna.Framework.Content;

namespace Cocos2D;

public class CCContent
{
    [ContentSerializer]
    public string Content { get; set; }

    /// <summary>
    /// Helper static method to load the contents of a CCContent object.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static string LoadContentFile(string file)
    {
        // Get the content manager from CCApplication or CCContentManager (for CCGameView)
        ContentManager contentManager = CCApplication.SharedApplication != null
            ? CCApplication.SharedApplication.Content
            : CCContentManager.SharedContentManager;

        if (contentManager == null)
        {
            throw new InvalidOperationException("No content manager available. Initialize CCApplication or CCGameView first.");
        }

        string content = null;
        try
        {
            content = contentManager.Load<string>(file);
        }
        catch (Exception)
        {
            // Ignore - continue with loading as CCContent
        }
        if (content == null)
        {
            try
            {
                var data = contentManager.Load<CCContent>(file);
                if (data != null && data.Content != null)
                {
                    content = data.Content;
                }
            }
            catch (Exception)
            {
            }
        }
        if (content == null)
        {
            try
            {
                var dx = contentManager.Load<CCContent>(file);
                if (dx == null || dx.Content == null)
                {
                    throw (new ContentLoadException("Could not load the contents of " + file + " as raw text."));
                }
                content = dx.Content;
            }
            catch (Exception ex)
            {
                throw (new ContentLoadException("Could not load the contents of " + file + " as raw text.", ex));
            }
        }
        return (content);
    }
}
