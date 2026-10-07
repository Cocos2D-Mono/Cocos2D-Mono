#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

/// <summary>
/// Identifies a shape drawn in a CCDrawNodeRetained, allowing
/// individual shapes to be moved, recolored, or removed without
/// rebuilding the entire vertex buffer.
/// </summary>
public class CCShapeHandle
{
    internal int StartIndex;
    internal int VertexCount;
    internal CCDrawNodeRetained Owner;
    internal bool Active = true;

    /// <summary>
    /// Whether this shape is still active (not removed).
    /// </summary>
    public bool IsActive { get { return Active; } }
}

/// <summary>
/// A retained-mode draw node where individual shapes can be added, moved,
/// recolored, or removed independently. Shape modifications (move, recolor,
/// opacity) update vertices in-place. The draw buffer array is rebuilt from
/// the vertex list on the next Draw() call when any change occurs.
///
/// Use this instead of CCDrawNode when you have many shapes and only a few
/// change per frame. The immediate-mode Clear() + redraw pattern still works
/// if needed.
///
/// <code>
/// var node = new CCDrawNodeRetained();
/// var dot = node.AddDot(center, 10f, CCColor4F.Red);
/// // Later, move or recolor:
/// node.SetShapePosition(dot, newCenter);
/// node.RecolorShape(dot, CCColor4F.Blue);
/// node.RemoveShape(dot);
/// </code>
/// </summary>
public class CCDrawNodeRetained : CCNode
{
    const int DefaultBufferSize = 512;

    private List<VertexPositionColor> _vertices;
    private List<CCShapeHandle> _shapes;
    private CCBlendFunc _blendFunc;
    private VertexPositionColor[] _drawBuffer;
    private int _drawBufferCount;
    private bool _dirty;

    public CCDrawNodeRetained()
    {
        _blendFunc = CCBlendFunc.AlphaBlend;
        _vertices = new List<VertexPositionColor>(DefaultBufferSize);
        _shapes = new List<CCShapeHandle>();
        _dirty = true;
    }

    public CCBlendFunc BlendFunc
    {
        get { return _blendFunc; }
        set { _blendFunc = value; }
    }

    /// <summary>
    /// Number of active shapes in this node.
    /// </summary>
    public int ShapeCount
    {
        get { return _shapes.Count; }
    }

    /// <summary>
    /// Total vertex count across all shapes.
    /// </summary>
    public int VertexCount
    {
        get { return _vertices.Count; }
    }

    #region Add Shapes

    /// <summary>
    /// Adds a filled dot and returns a handle for later manipulation.
    /// </summary>
    public CCShapeHandle AddDot(CCPoint pos, float radius, CCColor4F color)
    {
        if (radius >= 4f)
        {
            return AddFilledCircle(pos, radius, color);
        }

        int start = _vertices.Count;
        var cl = new Color(color.R, color.G, color.B, color.A);

        var a = new VertexPositionColor(new Vector3(pos.X - radius, pos.Y - radius, 0), cl);
        var b = new VertexPositionColor(new Vector3(pos.X - radius, pos.Y + radius, 0), cl);
        var c = new VertexPositionColor(new Vector3(pos.X + radius, pos.Y + radius, 0), cl);
        var d = new VertexPositionColor(new Vector3(pos.X + radius, pos.Y - radius, 0), cl);

        _vertices.Add(a);
        _vertices.Add(b);
        _vertices.Add(c);
        _vertices.Add(a);
        _vertices.Add(c);
        _vertices.Add(d);

        return CreateHandle(start, 6);
    }

    /// <summary>
    /// Adds a filled circle and returns a handle.
    /// </summary>
    public CCShapeHandle AddFilledCircle(CCPoint center, float radius, CCColor4F color, int segments = 32)
    {
        if (segments < 3) segments = 3;

        int start = _vertices.Count;
        var cl = new Color(color.R, color.G, color.B, color.A);
        var centerVertex = new VertexPositionColor(new Vector3(center.X, center.Y, 0), cl);
        float increment = MathHelper.TwoPi / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * increment;
            float angle2 = (i + 1) * increment;

            var v1 = new VertexPositionColor(
                new Vector3(center.X + (float)Math.Cos(angle1) * radius,
                            center.Y + (float)Math.Sin(angle1) * radius, 0), cl);
            var v2 = new VertexPositionColor(
                new Vector3(center.X + (float)Math.Cos(angle2) * radius,
                            center.Y + (float)Math.Sin(angle2) * radius, 0), cl);

            _vertices.Add(centerVertex);
            _vertices.Add(v1);
            _vertices.Add(v2);
        }

        return CreateHandle(start, segments * 3);
    }

    /// <summary>
    /// Adds a filled triangle and returns a handle.
    /// </summary>
    public CCShapeHandle AddTriangle(CCPoint a, CCPoint b, CCPoint c, CCColor4F color)
    {
        int start = _vertices.Count;
        var cl = new Color(color.R, color.G, color.B, color.A);

        _vertices.Add(new VertexPositionColor(new Vector3(a.X, a.Y, 0), cl));
        _vertices.Add(new VertexPositionColor(new Vector3(b.X, b.Y, 0), cl));
        _vertices.Add(new VertexPositionColor(new Vector3(c.X, c.Y, 0), cl));

        return CreateHandle(start, 3);
    }

    /// <summary>
    /// Adds a filled rectangle and returns a handle.
    /// </summary>
    public CCShapeHandle AddRect(CCRect rect, CCColor4F color)
    {
        int start = _vertices.Count;
        var cl = new Color(color.R, color.G, color.B, color.A);

        float x1 = rect.MinX, y1 = rect.MinY;
        float x2 = rect.MaxX, y2 = rect.MaxY;

        var bl = new VertexPositionColor(new Vector3(x1, y1, 0), cl);
        var br = new VertexPositionColor(new Vector3(x2, y1, 0), cl);
        var tr = new VertexPositionColor(new Vector3(x2, y2, 0), cl);
        var tl = new VertexPositionColor(new Vector3(x1, y2, 0), cl);

        _vertices.Add(bl);
        _vertices.Add(br);
        _vertices.Add(tr);
        _vertices.Add(bl);
        _vertices.Add(tr);
        _vertices.Add(tl);

        return CreateHandle(start, 6);
    }

    /// <summary>
    /// Adds a line segment and returns a handle.
    /// </summary>
    public CCShapeHandle AddSegment(CCPoint from, CCPoint to, float radius, CCColor4F color)
    {
        // Guard against zero-length segments which would produce NaN from Normalize
        var delta = from - to;
        if (delta.X * delta.X + delta.Y * delta.Y <= 1e-6f)
        {
            return AddDot(from, radius, color);
        }

        int start = _vertices.Count;
        var cl = new Color(color.R, color.G, color.B, color.A);

        var a = from;
        var b = to;
        var n = CCPoint.Normalize(CCPoint.Perp(a - b));
        var t = CCPoint.Perp(n);
        var nw = n * radius;
        var tw = t * radius;

        var v0 = b - (nw + tw);
        var v1 = b + (nw - tw);
        var v2 = b - nw;
        var v3 = b + nw;
        var v4 = a - nw;
        var v5 = a + nw;
        var v6 = a - (nw - tw);
        var v7 = a + (nw + tw);

        _vertices.Add(new VertexPositionColor(v0, cl));
        _vertices.Add(new VertexPositionColor(v1, cl));
        _vertices.Add(new VertexPositionColor(v2, cl));
        _vertices.Add(new VertexPositionColor(v3, cl));
        _vertices.Add(new VertexPositionColor(v1, cl));
        _vertices.Add(new VertexPositionColor(v2, cl));
        _vertices.Add(new VertexPositionColor(v3, cl));
        _vertices.Add(new VertexPositionColor(v4, cl));
        _vertices.Add(new VertexPositionColor(v2, cl));
        _vertices.Add(new VertexPositionColor(v3, cl));
        _vertices.Add(new VertexPositionColor(v4, cl));
        _vertices.Add(new VertexPositionColor(v5, cl));
        _vertices.Add(new VertexPositionColor(v6, cl));
        _vertices.Add(new VertexPositionColor(v4, cl));
        _vertices.Add(new VertexPositionColor(v5, cl));
        _vertices.Add(new VertexPositionColor(v6, cl));
        _vertices.Add(new VertexPositionColor(v7, cl));
        _vertices.Add(new VertexPositionColor(v5, cl));

        return CreateHandle(start, 18);
    }

    #endregion

    #region Manipulate Shapes

    /// <summary>
    /// Moves all vertices of a shape by the given offset.
    /// </summary>
    public void MoveShape(CCShapeHandle handle, CCPoint offset)
    {
        ValidateHandle(handle);
        var off = new Vector3(offset.X, offset.Y, 0);

        for (int i = handle.StartIndex; i < handle.StartIndex + handle.VertexCount; i++)
        {
            var v = _vertices[i];
            v.Position += off;
            _vertices[i] = v;
        }

        _dirty = true;
    }

    /// <summary>
    /// Sets the position of all vertices by translating the shape so its
    /// centroid is at the given position.
    /// </summary>
    public void SetShapePosition(CCShapeHandle handle, CCPoint position)
    {
        ValidateHandle(handle);
        if (handle.VertexCount == 0) return;

        // Compute current centroid
        float cx = 0, cy = 0;
        for (int i = handle.StartIndex; i < handle.StartIndex + handle.VertexCount; i++)
        {
            cx += _vertices[i].Position.X;
            cy += _vertices[i].Position.Y;
        }
        cx /= handle.VertexCount;
        cy /= handle.VertexCount;

        var off = new Vector3(position.X - cx, position.Y - cy, 0);
        for (int i = handle.StartIndex; i < handle.StartIndex + handle.VertexCount; i++)
        {
            var v = _vertices[i];
            v.Position += off;
            _vertices[i] = v;
        }

        _dirty = true;
    }

    /// <summary>
    /// Changes the color of all vertices in a shape.
    /// </summary>
    public void RecolorShape(CCShapeHandle handle, CCColor4F color)
    {
        ValidateHandle(handle);
        var cl = new Color(color.R, color.G, color.B, color.A);

        for (int i = handle.StartIndex; i < handle.StartIndex + handle.VertexCount; i++)
        {
            var v = _vertices[i];
            v.Color = cl;
            _vertices[i] = v;
        }

        _dirty = true;
    }

    /// <summary>
    /// Sets the opacity of all vertices in a shape.
    /// </summary>
    public void SetShapeOpacity(CCShapeHandle handle, byte opacity)
    {
        ValidateHandle(handle);

        for (int i = handle.StartIndex; i < handle.StartIndex + handle.VertexCount; i++)
        {
            var v = _vertices[i];
            v.Color = new Color(v.Color.R, v.Color.G, v.Color.B, opacity);
            _vertices[i] = v;
        }

        _dirty = true;
    }

    /// <summary>
    /// Removes a shape from the draw node. This compacts the vertex buffer
    /// and adjusts indices on all remaining handles so they stay valid.
    /// For best performance, remove shapes in reverse order of creation
    /// or use Clear() when removing many shapes at once.
    /// </summary>
    public void RemoveShape(CCShapeHandle handle)
    {
        ValidateHandle(handle);

        int start = handle.StartIndex;
        int count = handle.VertexCount;

        _vertices.RemoveRange(start, count);

        handle.Active = false;
        _shapes.Remove(handle);

        // Update start indices for all shapes after the removed one
        for (int i = 0; i < _shapes.Count; i++)
        {
            if (_shapes[i].StartIndex > start)
            {
                _shapes[i].StartIndex -= count;
            }
        }

        _dirty = true;
    }

    #endregion

    /// <summary>
    /// Clears all shapes and vertices.
    /// </summary>
    public void Clear()
    {
        _vertices.Clear();
        for (int i = 0; i < _shapes.Count; i++)
        {
            _shapes[i].Active = false;
        }
        _shapes.Clear();
        _drawBuffer = null;
        _dirty = true;
    }

    public override void Draw()
    {
        int vertCount = _vertices.Count;
        if (vertCount == 0) return;

        if (_dirty)
        {
            _dirty = false;
            _drawBufferCount = vertCount;

            // Reuse existing array if large enough, otherwise grow
            if (_drawBuffer == null || _drawBuffer.Length < vertCount)
            {
                _drawBuffer = new VertexPositionColor[vertCount];
            }
            _vertices.CopyTo(_drawBuffer);
        }

        if (_drawBuffer != null && _drawBufferCount >= 3)
        {
            CCDrawManager.TextureEnabled = false;
            CCDrawManager.BlendFunc(_blendFunc);
            CCDrawManager.DrawPrimitives(PrimitiveType.TriangleList, _drawBuffer, 0, _drawBufferCount / 3);
        }
    }

    private CCShapeHandle CreateHandle(int startIndex, int vertexCount)
    {
        var handle = new CCShapeHandle
        {
            StartIndex = startIndex,
            VertexCount = vertexCount,
            Owner = this,
            Active = true
        };
        _shapes.Add(handle);
        _dirty = true;
        return handle;
    }

    private void ValidateHandle(CCShapeHandle handle)
    {
        if (handle == null)
            throw new ArgumentNullException("handle");
        if (!handle.Active)
            throw new InvalidOperationException("Shape handle has been removed.");
        if (handle.Owner != this)
            throw new InvalidOperationException("Shape handle belongs to a different CCDrawNodeRetained.");
    }
}
