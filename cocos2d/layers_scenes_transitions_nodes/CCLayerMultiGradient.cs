using System;

namespace Cocos2D;

/// <summary>
/// A gradient layer that supports multiple color stops, rendered using CCDrawNode strips.
/// Unlike CCLayerGradient (which only supports 2 colors), this supports any number of
/// color stops for rich background gradients.
///
/// Usage:
///   var gradient = new CCLayerMultiGradient(
///       new CCColor4B[] { darkBlue, midBlue, purple },
///       new float[] { 0f, 0.5f, 1f },
///       width, height);
///   AddChild(gradient);
/// </summary>
public class CCLayerMultiGradient : CCDrawNode
{
    private CCColor4B[] _colors;
    private float[] _positions;
    private float _width;
    private float _height;
    private bool _isVertical;

    /// <summary>
    /// Creates a multi-stop gradient layer.
    /// </summary>
    /// <param name="colors">Array of colors at each stop.</param>
    /// <param name="positions">Array of stop positions (0.0 to 1.0), must match colors array length.</param>
    /// <param name="width">Width of the gradient layer.</param>
    /// <param name="height">Height of the gradient layer.</param>
    /// <param name="vertical">If true, gradient flows bottom-to-top. If false, left-to-right.</param>
    public CCLayerMultiGradient(CCColor4B[] colors, float[] positions, float width, float height, bool vertical = true)
    {
        if (colors == null || positions == null)
            throw new ArgumentNullException("colors and positions must not be null");
        if (colors.Length != positions.Length)
            throw new ArgumentException("colors and positions arrays must have the same length");
        if (colors.Length < 2)
            throw new ArgumentException("At least 2 color stops are required");

        _colors = colors;
        _positions = positions;
        _width = width;
        _height = height;
        _isVertical = vertical;

        RebuildGradient();
    }

    /// <summary>
    /// Updates the gradient colors and positions. Call this to change the gradient dynamically.
    /// </summary>
    public void SetGradient(CCColor4B[] colors, float[] positions)
    {
        if (colors.Length != positions.Length)
            throw new ArgumentException("colors and positions arrays must have the same length");

        _colors = colors;
        _positions = positions;
        RebuildGradient();
    }

    /// <summary>
    /// Updates the gradient size.
    /// </summary>
    public void SetSize(float width, float height)
    {
        _width = width;
        _height = height;
        RebuildGradient();
    }

    private void RebuildGradient()
    {
        Clear();

        // Render as strips between each pair of color stops
        int stripCount = 16; // subdivisions per segment for smooth blending

        for (int seg = 0; seg < _colors.Length - 1; seg++)
        {
            float startPos = _positions[seg];
            float endPos = _positions[seg + 1];
            CCColor4F startColor = (CCColor4F)_colors[seg];
            CCColor4F endColor = (CCColor4F)_colors[seg + 1];

            for (int i = 0; i < stripCount; i++)
            {
                float t0 = (float)i / stripCount;
                float t1 = (float)(i + 1) / stripCount;

                float pos0 = startPos + (endPos - startPos) * t0;
                float pos1 = startPos + (endPos - startPos) * t1;

                CCColor4F c0 = CCColor4F.Lerp(startColor, endColor, t0);
                CCColor4F c1 = CCColor4F.Lerp(startColor, endColor, t1);

                if (_isVertical)
                {
                    float y0 = pos0 * _height;
                    float y1 = pos1 * _height;

                    // Draw a horizontal strip from y0 to y1
                    var verts = new CCPoint[]
                    {
                        new CCPoint(0, y0),
                        new CCPoint(_width, y0),
                        new CCPoint(_width, y1),
                        new CCPoint(0, y1)
                    };

                    // Average color for uniform fill per strip (close enough at 16 strips per segment)
                    CCColor4F avgColor = CCColor4F.Lerp(c0, c1, 0.5f);
                    DrawPolygon(verts, 4, avgColor, 0, CCColor4F.Transparent);
                }
                else
                {
                    float x0 = pos0 * _width;
                    float x1 = pos1 * _width;

                    var verts = new CCPoint[]
                    {
                        new CCPoint(x0, 0),
                        new CCPoint(x1, 0),
                        new CCPoint(x1, _height),
                        new CCPoint(x0, _height)
                    };

                    CCColor4F avgColor = CCColor4F.Lerp(c0, c1, 0.5f);
                    DrawPolygon(verts, 4, avgColor, 0, CCColor4F.Transparent);
                }
            }
        }
    }
}
