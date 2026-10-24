namespace Cocos2D;

/// <summary>
/// Resolution policy for the CCGameView.
/// These policies determine how the design resolution is mapped to the actual view size.
/// </summary>
public enum CCViewResolutionPolicy
{
    /// <summary>
    /// Use ViewportRectRatio for custom viewport configuration.
    /// </summary>
    Custom,

    /// <summary>
    /// Fit to entire view. Distortion may occur if aspect ratios don't match.
    /// </summary>
    ExactFit,

    /// <summary>
    /// Maintain design resolution aspect ratio, but scene may appear cropped.
    /// The entire view is filled with the scene.
    /// </summary>
    NoBorder,

    /// <summary>
    /// Maintain design resolution aspect ratio, ensuring entire scene is visible.
    /// Black bars may appear on the sides.
    /// </summary>
    ShowAll,

    /// <summary>
    /// Use width of design resolution and scale height to aspect ratio of view.
    /// Useful for horizontal scrolling games.
    /// </summary>
    FixedHeight,

    /// <summary>
    /// Use height of design resolution and scale width to aspect ratio of view.
    /// Useful for vertical scrolling games.
    /// </summary>
    FixedWidth
}
