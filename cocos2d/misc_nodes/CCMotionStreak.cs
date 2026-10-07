#nullable disable
using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public class CCMotionStreak : CCNode, ICCTextureProtocol
{
    protected bool m_bFastMode;
    protected bool m_bStartingPositionInitialized;
    private float _fadeDelta;
    private float _minSeg;
    private float _stroke;
    private float[] _pointState;
    private CCPoint[] _pointVertexes;
    /** texture used for the motion streak */
    private CCTexture2D _texture;
    private CCV3F_C4B_T2F[] _vertices;
    private CCBlendFunc _blendFunc;
    private CCPoint _positionR;

    private int _maxPoints;
    private int _nuPoints;
    private int _previousNuPoints;

    /** Pointers */

    public CCMotionStreak()
    {
        _blendFunc = CCBlendFunc.NonPremultiplied;
    }

    public CCMotionStreak(float fadeTime, float minSegLength, float streakWidth, CCColor3B color, string pathToTexture)
    {
        InitWithFade(fadeTime, minSegLength, streakWidth, color, pathToTexture);
    }

    public CCMotionStreak(float fadeTime, float minSegLength, float streakWidth, CCColor3B color, CCTexture2D texture)
    {
        InitWithFade(fadeTime, minSegLength, streakWidth, color, texture);
    }

    public override CCPoint Position
    {
        set
        {
            m_bStartingPositionInitialized = true;
            _positionR = value;
        }
    }

    #region RGBA Protocol

    public override byte Opacity
    {
        get { return 0; }
        set { }
    }

    public override bool IsOpacityModifyRGB
    {
        get { return false; }
        set { }
    }

    #endregion

    #region ICCTextureProtocol Members

    public CCTexture2D Texture
    {
        get { return _texture; }
        set { _texture = value; }
    }

    public CCBlendFunc BlendFunc
    {
        set { _blendFunc = value; }
        get { return (_blendFunc); }
    }

    #endregion

    public bool FastMode
    {
        get { return m_bFastMode; }
        set { m_bFastMode = value; }
    }

    public bool StartingPositionInitialized
    {
        get { return m_bStartingPositionInitialized; }
        set { m_bStartingPositionInitialized = value; }
    }

    protected virtual bool InitWithFade(float fade, float minSeg, float stroke, CCColor3B color, string path)
    {
        Debug.Assert(!String.IsNullOrEmpty(path), "Invalid filename");

        CCTexture2D texture = CCTextureCache.SharedTextureCache.AddImage(path);
        return InitWithFade(fade, minSeg, stroke, color, texture);
    }

    protected virtual bool InitWithFade(float fade, float minSeg, float stroke, CCColor3B color, CCTexture2D texture)
    {
        Position = CCPoint.Zero;
        AnchorPoint = CCPoint.Zero;
        IgnoreAnchorPointForPosition = true;
        m_bStartingPositionInitialized = false;

        _positionR = CCPoint.Zero;
        m_bFastMode = true;
        _minSeg = (minSeg == -1.0f) ? stroke / 5.0f : minSeg;
        _minSeg *= _minSeg;

        _stroke = stroke;
        _fadeDelta = 1.0f / fade;

        _maxPoints = (int) (fade * 60.0f) + 2;
        _nuPoints = 0;
        _pointState = new float[_maxPoints];
        _pointVertexes = new CCPoint[_maxPoints];

        _vertices = new CCV3F_C4B_T2F[(_maxPoints + 1) * 2];

        // Set blend mode
        _blendFunc = CCBlendFunc.NonPremultiplied;

        Texture = texture;
        Color = color;
        ScheduleUpdate();

        return true;
    }

    public void TintWithColor(CCColor3B colors)
    {
        Color = colors;

        for (int i = 0; i < _nuPoints * 2; i++)
        {
            _vertices[i].Colors = new CCColor4B(colors.R, colors.G, colors.B, 255);
        }
    }

    public override void Update(float delta)
    {
        if (!m_bStartingPositionInitialized)
        {
            return;
        }

        delta *= _fadeDelta;

        int newIdx, newIdx2, i, i2;
        int mov = 0;

        // Update current points
        for (i = 0; i < _nuPoints; i++)
        {
            _pointState[i] -= delta;

            if (_pointState[i] <= 0)
            {
                mov++;
            }
            else
            {
                newIdx = i - mov;

                if (mov > 0)
                {
                    // Move data
                    _pointState[newIdx] = _pointState[i];

                    // Move point
                    _pointVertexes[newIdx] = _pointVertexes[i];

                    // Move vertices
                    i2 = i * 2;
                    newIdx2 = newIdx * 2;
                    _vertices[newIdx2].Vertices = _vertices[i2].Vertices;
                    _vertices[newIdx2 + 1].Vertices = _vertices[i2 + 1].Vertices;

                    // Move color
                    _vertices[newIdx2].Colors = _vertices[i2].Colors;
                    _vertices[newIdx2 + 1].Colors = _vertices[i2 + 1].Colors;
                }
                else
                {
                    newIdx2 = newIdx * 2;
                }

                _vertices[newIdx2].Colors.A = _vertices[newIdx2 + 1].Colors.A = (byte) (_pointState[newIdx] * 255.0f);
            }
        }
        _nuPoints -= mov;

        // Append new point
        bool appendNewPoint = true;
        if (_nuPoints >= _maxPoints)
        {
            appendNewPoint = false;
        }

        else if (_nuPoints > 0)
        {
            bool a1 = _pointVertexes[_nuPoints - 1].DistanceSquared(ref _positionR) < _minSeg;
            bool a2 = (_nuPoints != 1) && (_pointVertexes[_nuPoints - 2].DistanceSquared(ref _positionR) < (_minSeg * 2.0f));

            if (a1 || a2)
            {
                appendNewPoint = false;
            }
        }

        if (appendNewPoint)
        {
            _pointVertexes[_nuPoints] = _positionR;
            _pointState[_nuPoints] = 1.0f;

            // Color asignation
            int offset = _nuPoints * 2;
            _vertices[offset].Colors = _vertices[offset + 1].Colors = new CCColor4B(_displayedColor.R, _displayedColor.G, _displayedColor.B, 255);

            // Generate polygon
            if (_nuPoints > 0 && m_bFastMode)
            {
                if (_nuPoints > 1)
                {
                    VertexLineToPolygon(_pointVertexes, _stroke, _vertices, _nuPoints, 1);
                }
                else
                {
                    VertexLineToPolygon(_pointVertexes, _stroke, _vertices, 0, 2);
                }
            }

            _nuPoints++;
        }

        if (!m_bFastMode)
        {
            VertexLineToPolygon(_pointVertexes, _stroke, _vertices, 0, _nuPoints);
        }

        // Updated Tex Coords only if they are different than previous step
        if (_nuPoints > 0 && _previousNuPoints != _nuPoints)
        {
            float texDelta = 1.0f / _nuPoints;
            for (i = 0; i < _nuPoints; i++)
            {
                _vertices[i * 2].TexCoords = new CCTex2F(0, texDelta * i);
                _vertices[i * 2 + 1].TexCoords = new CCTex2F(1, texDelta * i);
            }

            _previousNuPoints = _nuPoints;
        }
    }


    private void VertexLineToPolygon(CCPoint[] points, float stroke, CCV3F_C4B_T2F[] vertices, int offset, int nuPoints)
    {
        nuPoints += offset;
        if (nuPoints <= 1) return;

        stroke *= 0.5f;

        int idx;
        int nuPointsMinus = nuPoints - 1;

        float rad70 = MathHelper.ToRadians(70);
        float rad170 = MathHelper.ToRadians(170);

        for (int i = offset; i < nuPoints; i++)
        {
            idx = i * 2;
            CCPoint p1 = points[i];
            CCPoint perpVector;

            if (i == 0)
            {
                perpVector = CCPoint.Perp(CCPoint.Normalize(p1 - points[i + 1]));
            }
            else if (i == nuPointsMinus)
            {
                perpVector = CCPoint.Perp(CCPoint.Normalize(points[i - 1] - p1));
            }
            else
            {
                CCPoint p2 = points[i + 1];
                CCPoint p0 = points[i - 1];

                CCPoint p2p1 = CCPoint.Normalize(p2 - p1);
                CCPoint p0p1 = CCPoint.Normalize(p0 - p1);

                // Calculate angle between vectors
                var angle = (float) Math.Acos(CCPoint.Dot(p2p1, p0p1));

                if (angle < rad70)
                {
                    perpVector = CCPoint.Perp(CCPoint.Normalize(CCPoint.Midpoint(p2p1, p0p1)));
                }
                else if (angle < rad170)
                {
                    perpVector = CCPoint.Normalize(CCPoint.Midpoint(p2p1, p0p1));
                }
                else
                {
                    perpVector = CCPoint.Perp(CCPoint.Normalize(p2 - p0));
                }
            }

            perpVector = perpVector * stroke;

            vertices[idx].Vertices = new CCVertex3F(p1.X + perpVector.X, p1.Y + perpVector.Y, 0);
            vertices[idx + 1].Vertices = new CCVertex3F(p1.X - perpVector.X, p1.Y - perpVector.Y, 0);
        }

        // Validate vertexes
        offset = (offset == 0) ? 0 : offset - 1;
        for (int i = offset; i < nuPointsMinus; i++)
        {
            idx = i * 2;
            int idx1 = idx + 2;

            CCVertex3F p1 = vertices[idx].Vertices;
            CCVertex3F p2 = vertices[idx + 1].Vertices;
            CCVertex3F p3 = vertices[idx1].Vertices;
            CCVertex3F p4 = vertices[idx1 + 1].Vertices;

            float s;
            bool fixVertex = !ccVertexLineIntersect(p1.X, p1.Y, p4.X, p4.Y, p2.X, p2.Y, p3.X, p3.Y, out s);
            if (!fixVertex)
            {
                if (s < 0.0f || s > 1.0f)
                {
                    fixVertex = true;
                }
            }

            if (fixVertex)
            {
                vertices[idx1].Vertices = p4;
                vertices[idx1 + 1].Vertices = p3;
            }
        }
    }

    private bool ccVertexLineIntersect(float Ax, float Ay, float Bx, float By, float Cx, float Cy, float Dx, float Dy, out float T)
    {
        float distAB, theCos, theSin, newX;

        T = 0;

        // FAIL: Line undefined
        if ((Ax == Bx && Ay == By) || (Cx == Dx && Cy == Dy)) return false;

        //  Translate system to make A the origin
        Bx -= Ax;
        By -= Ay;
        Cx -= Ax;
        Cy -= Ay;
        Dx -= Ax;
        Dy -= Ay;

        // Length of segment AB
        distAB = (float) Math.Sqrt(Bx * Bx + By * By);

        // Rotate the system so that point B is on the positive X axis.
        theCos = Bx / distAB;
        theSin = By / distAB;
        newX = Cx * theCos + Cy * theSin;
        Cy = Cy * theCos - Cx * theSin;
        Cx = newX;
        newX = Dx * theCos + Dy * theSin;
        Dy = Dy * theCos - Dx * theSin;
        Dx = newX;

        // FAIL: Lines are parallel.
        if (Cy == Dy) return false;

        // Discover the relative position of the intersection in the line AB
        T = (Dx + (Cx - Dx) * Dy / (Dy - Cy)) / distAB;

        // Success.
        return true;
    }

    private void Reset()
    {
        _nuPoints = 0;
    }

    public override void Draw()
    {
        CCDrawManager.BlendFunc(_blendFunc);
        CCDrawManager.BindTexture(_texture);
        CCDrawManager.VertexColorEnabled = true;
        CCDrawManager.DrawPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, _nuPoints * 2 - 2);
    }
}