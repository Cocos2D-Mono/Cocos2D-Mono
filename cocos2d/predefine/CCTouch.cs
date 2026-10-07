#nullable disable
using System;

namespace Cocos2D;

//
// TODO: Add CCGesture
//

public class CCTouch
{
    private int _id;

    /// <summary>
    /// Point of action
    /// </summary>
    private CCPoint _point;

    /// <summary>
    /// Previous point in the action
    /// </summary>
    private CCPoint _prevPoint;

    private CCPoint _startPoint;
    private bool _startPointCaptured;

    public CCTouch()
        : this(0, 0, 0)
    {
    }

    public CCTouch(int id, float x, float y)
    {
        _id = id;
        _point = new CCPoint(x, y);
        _prevPoint = new CCPoint(x, y);
    }

    /** returns the start touch location in OpenGL coordinates */
    public CCPoint StartLocation
    {
        get { return CCDirector.SharedDirector.ConvertToGl(_startPoint); }
    }

    public CCPoint LocationInView
    {
        get { return _point; }
    }

    /** returns the start touch location in screen coordinates */
    public CCPoint StartLocationInView
    {
        get { return _startPoint; }
    }

    public CCPoint PreviousLocationInView
    {
        get { return _prevPoint; }
    }

    /// <summary>
    /// Returns the location of the touch point in GL coordinates using ConvertToGl in CCDirector.
    /// </summary>
    public CCPoint Location
    {
        get { return CCDirector.SharedDirector.ConvertToGl(_point); }
    }

    /// <summary>
    /// Returns the previous location of the touch point in GL coordinates using ConvertToGl in CCDirector.
    /// </summary>
    public CCPoint PreviousLocation
    {
        get { return CCDirector.SharedDirector.ConvertToGl(_prevPoint); }
    }


    public int Id
    {
        get { return _id; }
    }

    /// <summary>
    /// Returns the difference, in GL coordinate space, of the last location and this current location.
    /// </summary>
    public CCPoint Delta
    {
        get { return Location - PreviousLocation; }
    }

    /// <summary>
    /// The touch delegate that consumed this touch. This is designed only for the one-at-a-time handler
    /// of touches.
    /// </summary>
    internal CCTargetedTouchHandler Consumer
    {
        get;
        set;
    }

    public void SetTouchInfo(int id, float x, float y)
    {
        _id = id;
        _prevPoint = _point;
        _point.X = x;
        _point.Y = y;
        if (!_startPointCaptured)
        {
            _startPoint = _point;
            _startPointCaptured = true;
        }
    }
}