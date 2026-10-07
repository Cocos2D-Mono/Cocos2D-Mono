#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public class CCDrawNode : CCNode
{
    const int DefaultBufferSize = 512;

    private CCRawList<VertexPositionColor> _vertices;
    private CCBlendFunc _blendFunc;
    private bool _dirty;

    public CCDrawNode()
    {
        Init();
    }

    public CCBlendFunc BlendFunc
    {
        get { return _blendFunc; }
        set { _blendFunc = value; }
    }

    public override bool Init()
    {
        base.Init();

        _blendFunc = CCBlendFunc.AlphaBlend;
        _vertices = new CCRawList<VertexPositionColor>(DefaultBufferSize);
        return true;
    }

    /// <summary>
    /// Draws a dot at a position with a given radius and color. For small radii (less than 4px),
    /// a fast quad is used. For larger radii, a geometry-based circle (triangle fan) is drawn
    /// to avoid visible square artifacts.
    /// </summary>
    public void DrawDot(CCPoint pos, float radius, CCColor4F color)
    {
        if (radius >= 4f)
        {
            DrawFilledCircle(pos, radius, color);
            return;
        }

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

        _dirty = true;
    }

    /// <summary>
    /// Draws a filled circle using a triangle fan. Unlike DrawDot, this always renders
    /// as a true circle regardless of radius.
    /// </summary>
    public void DrawFilledCircle(CCPoint center, float radius, CCColor4F color, int segments = 32)
    {
        if (segments < 3) segments = 3;

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

        _dirty = true;
    }

    /// <summary>
    /// Draws a filled triangle with the given vertices and color.
    /// </summary>
    public void DrawTriangle(CCPoint a, CCPoint b, CCPoint c, CCColor4F color)
    {
        var cl = new Color(color.R, color.G, color.B, color.A);

        _vertices.Add(new VertexPositionColor(new Vector3(a.X, a.Y, 0), cl));
        _vertices.Add(new VertexPositionColor(new Vector3(b.X, b.Y, 0), cl));
        _vertices.Add(new VertexPositionColor(new Vector3(c.X, c.Y, 0), cl));

        _dirty = true;
    }
    
    /// <summary>
    /// Creates 18 vertices that create a segment between the two points with the given radius of rounding
    /// on the segment end. The color is used to draw the segment.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="radius"></param>
    /// <param name="color"></param>
    /// <returns>The starting vertex index of the segment.</returns>
    public virtual int DrawSegment(CCPoint from, CCPoint to, float radius, CCColor4F color)
    {
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

        int returnIndex = _vertices.Count;
        _vertices.Add(new VertexPositionColor(v0, cl)); //__t(v2fneg(v2fadd(n, t)))
        _vertices.Add(new VertexPositionColor(v1, cl)); //__t(v2fsub(n, t))
        _vertices.Add(new VertexPositionColor(v2, cl)); //__t(v2fneg(n))}

        _vertices.Add(new VertexPositionColor(v3, cl)); //__t(n)
        _vertices.Add(new VertexPositionColor(v1, cl)); //__t(v2fsub(n, t))
        _vertices.Add(new VertexPositionColor(v2, cl)); //__t(v2fneg(n))

        _vertices.Add(new VertexPositionColor(v3, cl)); //__t(n)
        _vertices.Add(new VertexPositionColor(v4, cl)); //__t(v2fneg(n))
        _vertices.Add(new VertexPositionColor(v2, cl)); //__t(v2fneg(n))

        _vertices.Add(new VertexPositionColor(v3, cl)); //__t(n)
        _vertices.Add(new VertexPositionColor(v4, cl)); //__t(v2fneg(n))
        _vertices.Add(new VertexPositionColor(v5, cl)); //__t(n)

        _vertices.Add(new VertexPositionColor(v6, cl)); //__t(v2fsub(t, n))
        _vertices.Add(new VertexPositionColor(v4, cl)); //__t(v2fneg(n))
        _vertices.Add(new VertexPositionColor(v5, cl)); //__t(n)

        _vertices.Add(new VertexPositionColor(v6, cl)); //__t(v2fsub(t, n))
        _vertices.Add(new VertexPositionColor(v7, cl)); //__t(v2fadd(n, t))
        _vertices.Add(new VertexPositionColor(v5, cl)); //__t(n)

        _dirty = true;
        return (returnIndex);
    }

    public virtual void FadeBySegment(int vertexStart, float fadeFactor) 
    {
        FadeByVertices(vertexStart, 18, fadeFactor);
    }

    public virtual void FadeToSegment(int vertexStart, float fadeFactor)
    {
        FadeToVertices(vertexStart, 18, fadeFactor);
    }

    public virtual void RemoveSegment(int vertexStart) 
    {
        if (_vertices.Count == 18)
        {
            _vertices.Clear();
        }
        else
        {
            _vertices.RemoveRange(vertexStart, 18);
        }
        _dirty = true;
    }

    /// <summary>
    /// Multiplicatively applies the fadeFactor to the alpha channel of the vertices starting
    /// with start and for the number of vertices defined by count. the alpha channel is
    /// determined by the current alpha * fadeFactor.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="count"></param>
    /// <param name="fadeFactor"></param>
    public virtual void FadeByVertices(int start, int count, float fadeFactor)
    {
        for (int i = 0; i < count; i++)
        {
            VertexPositionColor vpc = _vertices[start + i];
            Color c = vpc.Color;
            vpc.Color = new Color(c.R, c.G, c.B, (byte)(c.A * fadeFactor));
            _vertices[start + i] = vpc;
        }
        _dirty = true;
    }

    /// <summary>
    /// For the start and count vertices drawn, this will set the alpha channel to the given fade factor.
    /// The alpha is determined by 255 * fadeFactor.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="count"></param>
    /// <param name="fadeFactor"></param>
    public virtual void FadeToVertices(int start, int count, float fadeFactor)
    {
        for (int i = 0; i < count; i++)
        {
            VertexPositionColor vpc = _vertices[start + i];
            Color c = vpc.Color;
            vpc.Color = new Color(c.R, c.G, c.B, (byte)(255f * fadeFactor));
            _vertices[start + i] = vpc;
        }
        _dirty = true;
    }

    /** draw a polygon with a fill color and line color */

    private struct ExtrudeVerts
    {
        public CCPoint offset;
        public CCPoint n;
    }

    public void DrawCircleOutline(CCPoint center, float radius, float lineWidth, CCColor4B color)
    {
        DrawCircleOutline(center, radius, lineWidth, CCMacros.CCDegreesToRadians(360f), 360, color);
    }

    public void DrawCircleOutline(CCPoint center, float radius, float lineWidth, float angle, int segments, CCColor4B color)
    {
        float increment = MathHelper.Pi * 2.0f / segments;
        double theta = 0.0;

        CCPoint v1;
        CCPoint v2 = CCPoint.Zero;
        CCColor4F cf = new CCColor4F(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

        for (int i = 0; i < segments; i++)
        {
            v1 = center + new CCPoint((float)Math.Cos(theta), (float)Math.Sin(theta)) * radius;
            v2 = center + new CCPoint((float)Math.Cos(theta + increment), (float)Math.Sin(theta + increment)) * radius;
            DrawSegment(v1, v2, lineWidth, cf);
            theta += increment;
        }
    }

    public void DrawCircle(CCPoint center, float radius, CCColor4B color)
    {
        DrawCircle(center, radius, 32, color);
    }

    public void DrawCircle(CCPoint center, float radius, int segments, CCColor4B color)
    {
        CCColor4F cf = new CCColor4F(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        DrawFilledCircle(center, radius, cf, segments);
    }

    public void DrawRect(CCRect rect, CCColor4B color)
    {
        float x1 = rect.MinX;
        float y1 = rect.MinY;
        float x2 = rect.MaxX;
        float y2 = rect.MaxY;
        CCPoint[] pt = new CCPoint[] { 
            new CCPoint(x1,y1), new CCPoint(x2,y1), new CCPoint(x2,y2), new CCPoint(x1,y2)
        };
        CCColor4F cf = new CCColor4F(color.R/255f, color.G/255f, color.B/255f, color.A/255f);
        DrawPolygon(pt, 4, cf, 0, new CCColor4F(0f, 0f, 0f, 0f));
    }

		public void DrawRect(CCRect rect, CCColor4F color, float borderWidth, CCColor4F borderColor)
		{
			float x1 = rect.MinX;
			float y1 = rect.MinY;
			float x2 = rect.MaxX;
			float y2 = rect.MaxY;
			CCPoint[] pt = new CCPoint[] {
				new CCPoint(x1,y1), new CCPoint(x2,y1), new CCPoint(x2,y2), new CCPoint(x1,y2)
			};
			DrawPolygon(pt, 4, color, borderWidth, borderColor);
		}

    /// <summary>
    /// Draws a rectangle outline (no fill) with the given line width and color.
    /// </summary>
    public void DrawRectOutline(CCRect rect, float lineWidth, CCColor4F color)
    {
        var bl = new CCPoint(rect.MinX, rect.MinY);
        var br = new CCPoint(rect.MaxX, rect.MinY);
        var tr = new CCPoint(rect.MaxX, rect.MaxY);
        var tl = new CCPoint(rect.MinX, rect.MaxY);

        DrawSegment(bl, br, lineWidth, color);
        DrawSegment(br, tr, lineWidth, color);
        DrawSegment(tr, tl, lineWidth, color);
        DrawSegment(tl, bl, lineWidth, color);
    }

    public void DrawPolygon(CCPoint[] verts, int count, CCColor4F fillColor, float borderWidth,
                            CCColor4F borderColor)
    {
        var extrude = new ExtrudeVerts[count];

        for (int i = 0; i < count; i++)
        {
            var v0 = verts[(i - 1 + count) % count];
            var v1 = verts[i];
            var v2 = verts[(i + 1) % count];

            var n1 = CCPoint.Normalize(CCPoint.Perp(v1 - v0));
            var n2 = CCPoint.Normalize(CCPoint.Perp(v2 - v1));

            var offset = (n1 + n2) * (1.0f / (CCPoint.Dot(n1, n2) + 1.0f));
            extrude[i] = new ExtrudeVerts() {offset = offset, n = n2};
        }

        bool outline = (fillColor.A > 0.0f && borderWidth > 0.0f);

        float inset = (!outline ? 0.5f : 0.0f);
        
        for (int i = 0; i < count - 2; i++)
        {
            var v0 = verts[0] - (extrude[0].offset * inset);
            var v1 = verts[i + 1] - (extrude[i + 1].offset * inset);
            var v2 = verts[i + 2] - (extrude[i + 2].offset * inset);

            _vertices.Add(new VertexPositionColor(v0, fillColor)); //__t(v2fzero)
            _vertices.Add(new VertexPositionColor(v1, fillColor)); //__t(v2fzero)
            _vertices.Add(new VertexPositionColor(v2, fillColor)); //__t(v2fzero)
        }

        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;
            var v0 = verts[i];
            var v1 = verts[j];

            var n0 = extrude[i].n;

            var offset0 = extrude[i].offset;
            var offset1 = extrude[j].offset;

            if (outline)
            {
                var inner0 = (v0 - (offset0 * borderWidth));
                var inner1 = (v1 - (offset1 * borderWidth));
                var outer0 = (v0 + (offset0 * borderWidth));
                var outer1 = (v1 + (offset1 * borderWidth));

                _vertices.Add(new VertexPositionColor(inner0, borderColor)); //__t(v2fneg(n0))
                _vertices.Add(new VertexPositionColor(inner1, borderColor)); //__t(v2fneg(n0))
                _vertices.Add(new VertexPositionColor(outer1, borderColor)); //__t(n0)

                _vertices.Add(new VertexPositionColor(inner0, borderColor)); //__t(v2fneg(n0))
                _vertices.Add(new VertexPositionColor(outer0, borderColor)); //__t(n0)
                _vertices.Add(new VertexPositionColor(outer1, borderColor)); //__t(n0)
            }
            else
            {
                var inner0 = (v0 - (offset0 * 0.5f));
                var inner1 = (v1 - (offset1 * 0.5f));
                var outer0 = (v0 + (offset0 * 0.5f));
                var outer1 = (v1 + (offset1 * 0.5f));

                _vertices.Add(new VertexPositionColor(inner0, fillColor)); //__t(v2fzero)
                _vertices.Add(new VertexPositionColor(inner1, fillColor)); //__t(v2fzero)
                _vertices.Add(new VertexPositionColor(outer1, fillColor)); //__t(n0)

                _vertices.Add(new VertexPositionColor(inner0, fillColor)); //__t(v2fzero)
                _vertices.Add(new VertexPositionColor(outer0, fillColor)); //__t(n0)
                _vertices.Add(new VertexPositionColor(outer1, fillColor)); //__t(n0)
            }
        }
        _dirty = true;
    }

    public void DrawLine(CCPoint from, CCPoint to, float lineWidth = 1, CCLineCap lineCap = CCLineCap.Butt)
    {
        DrawLine(from, to, lineWidth, new CCColor4B(Color.R, Color.G, Color.B, Opacity));
    }
    public void DrawLine(CCPoint from, CCPoint to, CCColor4B color, CCLineCap lineCap = CCLineCap.Butt)
    {
        DrawLine(from, to, 1, color);
    }

    public void DrawLine(CCPoint from, CCPoint to, float lineWidth, CCColor4B color, CCLineCap lineCap = CCLineCap.Butt)
    {
        System.Diagnostics.Debug.Assert(lineWidth >= 0, "Invalid value specified for lineWidth : value is negative");
        if (lineWidth <= 0)
            return;

        var cl = color;

        var a = from;
        var b = to;

        var normal = CCPoint.Normalize(a - b);
        if (lineCap == CCLineCap.Square)
        {
            var nr = normal * lineWidth;
            a += nr;
            b -= nr;
        }

        var n = CCPoint.PerpendicularCounterClockwise(normal);

        var nw = n * lineWidth;
        var v0 = b - nw;
        var v1 = b + nw;
        var v2 = a - nw;
        var v3 = a + nw;

        // Triangles from beginning to end
        _vertices.Add(new VertexPositionColor(v1, cl));
        _vertices.Add(new VertexPositionColor(v2, cl));
        _vertices.Add(new VertexPositionColor(v0, cl));

        _vertices.Add(new VertexPositionColor(v1, cl));
        _vertices.Add(new VertexPositionColor(v2, cl));
        _vertices.Add(new VertexPositionColor(v3, cl));

        if (lineCap == CCLineCap.Round)
        {
            var mb = (float)Math.Atan2(v1.Y - b.Y, v1.X - b.X);
            var ma = (float)Math.Atan2(v2.Y - a.Y, v2.X - a.X);

            // Draw rounded line caps
            DrawSolidArc(a, lineWidth, -ma, -MathHelper.Pi, color);
            DrawSolidArc(b, lineWidth, -mb, -MathHelper.Pi, color);
        }

        _dirty = true;
    }

    // Used for drawing line caps
    public void DrawSolidArc(CCPoint pos, float radius, float startAngle, float sweepAngle, CCColor4B color)
    {
        var cl = color;

        int segments = (int)(10 * (float)Math.Sqrt(radius));  //<- Let's try to guess at # segments for a reasonable smoothness

        float theta = -sweepAngle / (segments - 1);// MathHelper.Pi * 2.0f / segments;
        float tangetial_factor = (float)Math.Tan(theta);   //calculate the tangential factor 

        float radial_factor = (float)Math.Cos(theta);   //calculate the radial factor 

        float x = radius * (float)Math.Cos(-startAngle);   //we now start at the start angle
        float y = radius * (float)Math.Sin(-startAngle);

        var verticeCenter = new CCV3F_C4B(pos, cl);
        var vert1 = new CCV3F_C4B(CCVertex3F.Zero, cl);
        float tx = 0;
        float ty = 0;

        for (int i = 0; i < segments - 1; i++)
        {
            _vertices.Add(new VertexPositionColor(pos, cl));

            vert1.Vertices.X = x + pos.X;
            vert1.Vertices.Y = y + pos.Y;
            _vertices.Add(new VertexPositionColor(new Vector3(vert1.Vertices.X, vert1.Vertices.Y, vert1.Vertices.Z), cl));

            //calculate the tangential vector 
            //remember, the radial vector is (x, y) 
            //to get the tangential vector we flip those coordinates and negate one of them 
            tx = -y;
            ty = x;

            //add the tangential vector 
            x += tx * tangetial_factor;
            y += ty * tangetial_factor;

            //correct using the radial factor 
            x *= radial_factor;
            y *= radial_factor;

            vert1.Vertices.X = x + pos.X;
            vert1.Vertices.Y = y + pos.Y;

            _vertices.Add(new VertexPositionColor(new Vector3(vert1.Vertices.X, vert1.Vertices.Y, 0), cl));
        }

        _dirty = true;
    }

    /** Clear the geometry in the node's buffer. */

    public virtual void Clear()
    {
        _vertices = new CCRawList<VertexPositionColor>(DefaultBufferSize);
        _dirty = true;
        _toDraw = null;
        base.ContentSize = CCSize.Zero;
    }

    public bool FilterPrimitivesByAlpha
    {
        get;
        set;
    }

    private VertexPositionColor[] _toDraw;

    public override void Draw()
    {
        if (_dirty)
        {
            _dirty = false;
            if (FilterPrimitivesByAlpha)
            {
                _toDraw = _vertices.Elements.Where(x => x.Color.A > 0).ToArray();
            }
            else
            {
                _toDraw = _vertices.Elements;
            }
        }

        if (_toDraw != null)
        {
            CCDrawManager.TextureEnabled = false;
            CCDrawManager.BlendFunc(_blendFunc);
            CCDrawManager.DrawPrimitives(PrimitiveType.TriangleList, _toDraw, 0, _toDraw.Length / 3);
        }
    }
}
