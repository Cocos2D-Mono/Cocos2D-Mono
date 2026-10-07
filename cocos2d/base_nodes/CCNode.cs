using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Input;

namespace Cocos2D;

internal enum CCNodeTag
{
    Invalid = -1,
};

/** @brief CCNode is the main element. Anything thats gets drawn or contains things that get drawn is a CCNode.
	The most popular CCNodes are: CCScene, CCLayer, CCSprite, CCMenu.

	The main features of a CCNode are:
	- They can contain other CCNode nodes (addChild, getChildByTag, removeChild, etc)
	- They can schedule periodic callback (schedule, unschedule, etc)
	- They can execute actions (runAction, stopAction, etc)

	Some CCNode nodes provide extra functionality for them or their children.

	Subclassing a CCNode usually means (one/all) of:
	- overriding init to initialize resources and schedule callbacks
	- create callbacks to handle the advancement of time
	- overriding draw to render the node

	Features of CCNode:
	- position
	- scale (x, y)
	- rotation (in degrees, clockwise)
	- CCCamera (an interface to gluLookAt )
	- CCGridBase (to do mesh transformations)
	- anchor point
	- size
	- visible
	- z-order
	- openGL z position

	Default values:
	- rotation: 0
	- position: (x=0,y=0)
	- scale: (x=1,y=1)
	- contentSize: (x=0,y=0)
	- anchorPoint: (x=0,y=0)

	Limitations:
	- A CCNode is a "void" object. It doesn't have a texture

	Order in transformations with grid disabled
	-# The node will be translated (position)
	-# The node will be rotated (rotation)
	-# The node will be scaled (scale)
	-# The node will be moved according to the camera values (camera)

	Order in transformations with grid enabled
	-# The node will be translated (position)
	-# The node will be rotated (rotation)
	-# The node will be scaled (scale)
	-# The grid will capture the screen
	-# The node will be moved according to the camera values (camera)
	-# The grid will render the captured screen

	Camera:
	- Each node has a camera. By default it points to the center of the CCNode.
	*/

public class CCNode : ICCSelectorProtocol, ICCFocusable, ICCTargetedTouchDelegate, ICCStandardTouchDelegate, ICCKeypadDelegate, ICCKeyboardDelegate, ICCRGBAProtocol, IComparer<CCNode>
{
    /// <summary>
    /// Use this to determine if a tag has been set on the node.
    /// </summary>
    public const int kCCNodeTagInvalid = -1;

    private static uint s_globalOrderOfArrival = 1;
    protected bool m_bIgnoreAnchorPointForPosition;

    // transform
    public CCAffineTransform m_sTransform;
    protected bool m_bInverseDirty;
    protected bool m_bRunning;
    protected bool m_bTransformDirty;
    protected bool m_bVisible;
    protected bool m_bReorderChildDirty;
    protected float m_fRotationX;
    protected float m_fRotationY;
    protected float m_fScaleX;
    protected float m_fScaleY;
    protected bool m_bWorldTransformIsDirty = true;
    protected CCAffineTransform m_NodeToWorldTransform = CCAffineTransform.Identity;

    //protected int m_nScriptHandler;

    protected float m_fSkewX;
    protected float m_fSkewY;
    protected float m_fVertexZ;
    internal protected uint m_uOrderOfArrival;
    private int _tag;
    private uint _collisionCategory = 0xFFFFFFFF;
    private uint _collisionCategoryMask = 0xFFFFFFFF;
    internal int m_nZOrder;
    protected CCActionManager m_pActionManager;
    protected CCCamera? m_pCamera;
    protected CCRawList<CCNode>? m_pChildren;
    protected Dictionary<int, List<CCNode>>? m_pChildrenByTag;
    protected CCGridBase? m_pGrid;
    protected CCNode? m_pParent;
    protected CCScheduler m_pScheduler;
    protected CCTouchDispatcher? pDispatcher;

    protected object? m_pUserData;
    protected CCPoint m_obAnchorPoint;
    protected CCPoint m_obAnchorPointInPoints;
    protected CCSize m_obContentSize;
    private CCAffineTransform _inverse;
    protected CCPoint m_obPosition;
    private bool _hasFocus = false;

    private bool _additionalTransformDirty;
    private CCAffineTransform _additionalTransform;

    private string? _name;

    // input variables
    private bool _keypadEnabled;
    private bool _keyboardEnabled;
    private bool _gamePadEnabled;
    private bool _touchEnabled;
    private CCTouchMode _touchMode = CCTouchMode.OneByOne;
    private CCKeyboardMode _keyboardMode = CCKeyboardMode.All;
    private int _touchPriority;
    private bool _gamePadDelegatesInited;

    private bool _initialized = false;

    #region Color & Opacity
    protected byte _displayedOpacity;
    protected byte _realOpacity;
    protected CCColor3B _displayedColor;
    protected CCColor3B _realColor;
    protected bool _cascadeColorEnabled;
    protected bool _cascadeOpacityEnabled;

    public virtual CCColor3B Color
    {
        get { return _realColor; }
        set
        {
            _displayedColor = _realColor = value;

            if (_cascadeColorEnabled)
            {
                var parentColor = CCTypes.CCWhite;
                var parent = m_pParent as ICCRGBAProtocol;
                if (parent != null && parent.CascadeColorEnabled)
                {
                    parentColor = parent.DisplayedColor;
                }

                UpdateDisplayedColor(parentColor);
            }
        }
    }

    public virtual CCColor3B DisplayedColor
    {
        get { return _displayedColor; }
    }

    public virtual byte Opacity
    {
        get { return _realOpacity; }
        set
        {
            _displayedOpacity = _realOpacity = value;

            if (_cascadeOpacityEnabled)
            {
                byte parentOpacity = 255;
                var pParent = m_pParent as ICCRGBAProtocol;
                if (pParent != null && pParent.CascadeOpacityEnabled)
                {
                    parentOpacity = pParent.DisplayedOpacity;
                }
                UpdateDisplayedOpacity(parentOpacity);
            }
        }
    }

    public virtual byte DisplayedOpacity
    {
        get { return _displayedOpacity; }
    }

    public virtual bool IsOpacityModifyRGB
    {
        get { return false; }
        set { }
    }

    public virtual bool CascadeColorEnabled
    {
        get { return _cascadeColorEnabled; }
        set { _cascadeColorEnabled = value; }
    }

    public virtual bool CascadeOpacityEnabled
    {
        get { return _cascadeOpacityEnabled; }
        set { _cascadeOpacityEnabled = value; }
    }
    #endregion

    public enum CCTouchMode
    {
        AllAtOnce,
        OneByOne
    }

    public CCNode(CCSize contentSize) : this()
    {
        ContentSize = contentSize;
    }

    public CCNode()
    {
        m_fScaleX = 1.0f;
        m_fScaleY = 1.0f;
        m_bVisible = true;
        _tag = kCCNodeTagInvalid;

        m_sTransform = CCAffineTransform.Identity;
        m_bInverseDirty = true;

        // set default scheduler and actionManager
        CCDirector director = CCDirector.SharedDirector;
        m_pActionManager = director.ActionManager;
        m_pScheduler = director.Scheduler;

        _displayedOpacity = 255;
        _realOpacity = 255;
        _displayedColor = CCTypes.CCWhite;
        _realColor = CCTypes.CCWhite;
        _cascadeColorEnabled = false;
        _cascadeOpacityEnabled = false;

    }

    public virtual bool Init()
    {
        _displayedOpacity = _realOpacity = 255;
        _displayedColor = _realColor = CCTypes.CCWhite;
        _cascadeOpacityEnabled = _cascadeColorEnabled = false;

        return true;
    }

    #region Game State Management
    /// <summary>
    /// Gets whether or not this scene is serializable. If this is true,
    /// the screen will be recorded into the director's state and
    /// its Serialize and Deserialize methods will be called as appropriate.
    /// If this is false, the screen will be ignored during serialization.
    /// By default, all screens are assumed to be serializable.
    /// </summary>
    public virtual bool IsSerializable
    {
        get { return _isSerializable; }
        protected set { _isSerializable = value; }
    }

    private bool _isSerializable = true;

    /// <summary>
    /// Tells the screen to serialize its state into the given stream.
    /// </summary>
    public virtual void Serialize(Stream stream) 
    {
        StreamWriter sw = new StreamWriter(stream);
        CCSerialization.SerializeData(m_bVisible, sw);
        CCSerialization.SerializeData(m_fRotationX, sw);
        CCSerialization.SerializeData(m_fRotationY, sw);
        CCSerialization.SerializeData(m_fScaleX, sw);
        CCSerialization.SerializeData(m_fScaleY, sw);
        CCSerialization.SerializeData(m_fSkewX, sw);
        CCSerialization.SerializeData(m_fSkewY, sw);
        CCSerialization.SerializeData(m_fVertexZ, sw);
        CCSerialization.SerializeData(m_bIgnoreAnchorPointForPosition, sw);
        CCSerialization.SerializeData(m_bInverseDirty, sw);
        CCSerialization.SerializeData(m_bRunning, sw);
        CCSerialization.SerializeData(m_bTransformDirty, sw);
        CCSerialization.SerializeData(m_bReorderChildDirty, sw);
        CCSerialization.SerializeData(m_uOrderOfArrival, sw);
        CCSerialization.SerializeData(_tag, sw);
        CCSerialization.SerializeData(m_nZOrder, sw);
        CCSerialization.SerializeData(m_obAnchorPoint, sw);
        CCSerialization.SerializeData(m_obContentSize, sw);
        CCSerialization.SerializeData(Position, sw);
        if (m_pChildren != null)
        {
            CCSerialization.SerializeData(m_pChildren.Count, sw);
            foreach (CCNode child in m_pChildren)
            {
                sw.WriteLine(child.GetType().AssemblyQualifiedName);
            }
            foreach (CCNode child in m_pChildren)
            {
                child.Serialize(stream);
            }
        }
        else
        {
            CCSerialization.SerializeData(0, sw); // No children
        }
        // Appended after the children block so older saves (which end with
        // children) still deserialize cleanly via the EndOfStream guard.
        CCSerialization.SerializeData(unchecked((int)_collisionCategory), sw);
        CCSerialization.SerializeData(unchecked((int)_collisionCategoryMask), sw);
    }

    /// <summary>
    /// Tells the screen to deserialize its state from the given stream.
    /// </summary>
    public virtual void Deserialize(Stream stream) 
    {
        StreamReader sr = new StreamReader(stream);
        m_bVisible = CCSerialization.DeSerializeBool(sr); 
        m_fRotationX = CCSerialization.DeSerializeFloat(sr);
        m_fRotationY = CCSerialization.DeSerializeFloat(sr);
        m_fScaleX = CCSerialization.DeSerializeFloat(sr);
        m_fScaleY = CCSerialization.DeSerializeFloat(sr);
        m_fSkewX = CCSerialization.DeSerializeFloat(sr);
        m_fSkewY = CCSerialization.DeSerializeFloat(sr);
        m_fVertexZ = CCSerialization.DeSerializeFloat(sr);
        m_bIgnoreAnchorPointForPosition = CCSerialization.DeSerializeBool(sr);
        m_bInverseDirty = CCSerialization.DeSerializeBool(sr);
        m_bRunning = CCSerialization.DeSerializeBool(sr);
        m_bTransformDirty = CCSerialization.DeSerializeBool(sr);
        m_bReorderChildDirty = CCSerialization.DeSerializeBool(sr);
        m_uOrderOfArrival = (uint)CCSerialization.DeSerializeInt(sr);
        _tag = CCSerialization.DeSerializeInt(sr);
        m_nZOrder = CCSerialization.DeSerializeInt(sr);
        AnchorPoint = CCSerialization.DeSerializePoint(sr);
        ContentSize = CCSerialization.DeSerializeSize(sr);
        Position = CCSerialization.DeSerializePoint(sr);
        // m_UserData is handled by the specialized class.
        // TODO: Serializze the action manager
        // TODO :Serialize the grid
        // TODO: Serialize the camera
        int count = CCSerialization.DeSerializeInt(sr);
        for (int i = 0; i < count; i++)
        {
            string s = sr.ReadLine()
                ?? throw new InvalidDataException("Serialized node data ended before all children were read.");
            Type screenType = Type.GetType(s)
                ?? throw new InvalidDataException($"Serialized child type '{s}' could not be found.");
            CCNode scene = Activator.CreateInstance(screenType) as CCNode
                ?? throw new InvalidDataException($"Serialized child type '{s}' is not a CCNode.");
            AddChild(scene);
            scene.Deserialize(stream);
        }
        // Collision fields appended after the children block; older saves
        // end here and fall back to the all-categories default.
        _collisionCategory = sr.EndOfStream
            ? uint.MaxValue
            : unchecked((uint)CCSerialization.DeSerializeInt(sr));
        _collisionCategoryMask = sr.EndOfStream
            ? uint.MaxValue
            : unchecked((uint)CCSerialization.DeSerializeInt(sr));
    }


    #endregion

    #region CCIFocusable
    public virtual bool HasFocus
    {
        get { return (_hasFocus); }
        set { _hasFocus = value; }
    }
    public virtual bool CanReceiveFocus
    {
        get
        {
            return (Visible);
        }
    }
    #endregion

    public int Tag
    {
        get { return _tag; }
        set
        {
            if (_tag != value)
            {
                if (Parent != null)
                {
                    Parent.ChangedChildTag(this, _tag, value);
                }
                _tag = value;
            }
        }
    }

    /// <summary>
    /// Bitmask identifying which collision category this node belongs to.
    /// Defaults to 0xFFFFFFFF (all categories).
    /// </summary>
    public uint CollisionCategory
    {
        get { return _collisionCategory; }
        set { _collisionCategory = value; }
    }

    /// <summary>
    /// Bitmask identifying which collision categories this node can collide with.
    /// Defaults to 0xFFFFFFFF (all categories).
    /// </summary>
    public uint CollisionCategoryMask
    {
        get { return _collisionCategoryMask; }
        set { _collisionCategoryMask = value; }
    }

    public object? UserData
    {
        get { return m_pUserData; }
        set { m_pUserData = value; }
    }

    public object? UserObject { get; set; }

    public virtual float SkewX
    {
        get { return m_fSkewX; }
        set
        {
            m_fSkewX = value;
            SetTransformIsDirty();
        }
    }

    public virtual float SkewY
    {
        get { return m_fSkewY; }
        set
        {
            m_fSkewY = value;
            SetTransformIsDirty();
        }
    }

    /// <summary>
    /// The range of the zorder space local to this node.
    /// </summary>
    private int _localMaxZOrder = int.MinValue;
    private int _localMinZOrder = int.MaxValue;

    public int LocalMaxZForChildren { get { return (_localMaxZOrder); } }
    public int LocalMInZForChildren { get { return (_localMinZOrder); } }

    public int ZOrder
    {
        get { return m_nZOrder; }
        set
        {
            m_nZOrder = value;
            if (m_pParent != null)
            {
                m_pParent.ReorderChild(this, value);
            }
        }
    }

    public virtual float VertexZ
    {
        get { return m_fVertexZ; }
        set { m_fVertexZ = value; }
    }

    /// <summary>
    /// 2D rotation of the node relative to the 0,1 vector in a clock-wise orientation.
    /// </summary>
    public virtual float Rotation
    {
        get
        {
            Debug.Assert(m_fRotationX == m_fRotationY, "CCNode#rotation. RotationX != RotationY. Don't know which one to return");
            return m_fRotationX;
        }
        set
        {
            m_fRotationX = m_fRotationY = value;
            SetTransformIsDirty();
        }
    }

    public virtual float RotationX
    {
        get { return m_fRotationX; }
        set
        {
            m_fRotationX = value;
            SetTransformIsDirty();
        }
    }

    public virtual float RotationY
    {
        get { return m_fRotationY; }
        set
        {
            m_fRotationY = value;
            SetTransformIsDirty();
        }
    }

    /// <summary>
    /// The general scale that applies to both X and Y directions.
    /// </summary>
    public virtual float Scale
    {
        get
        {
            Debug.Assert(m_fScaleX == m_fScaleY, "CCNode#scale. ScaleX != ScaleY. Don't know which one to return");
            return m_fScaleX;
        }
        set
        {
            m_fScaleX = m_fScaleY = value;
            SetTransformIsDirty();
        }
    }

    /// <summary>
    /// Scale of the node in the X direction (left to right)
    /// </summary>
    public virtual float ScaleX
    {
        get { return m_fScaleX; }
        set
        {
            m_fScaleX = value;
            SetTransformIsDirty();
        }
    }

    /// <summary>
    /// Scale of the node in the Y direction (top to bottom)
    /// </summary>
    public virtual float ScaleY
    {
        get { return m_fScaleY; }
        set
        {
            m_fScaleY = value;
            SetTransformIsDirty();
        }
    }

    /// <summary>
    /// Sets and gets the position of the node. For Menus, this is the center of the menu. For layers,
    /// this is the lower left corner of the layer.
    /// </summary>
    public virtual CCPoint Position
    {
        get { return m_obPosition; }
        set
        {
            m_obPosition = value;
            SetTransformIsDirty();
        }
    }
    /// <summary>
    /// Safely set the X position of the node. This will not create unnecessary temporary 
    /// instances of the Position's CCPoint instance.
    /// </summary>
    public float PositionX
    {
        get { return m_obPosition.X; }
        set { SetPosition(value, m_obPosition.Y); }
    }

    /// <summary>
    /// Safely set the Y position of the node. This will not create unnecessary temporary 
    /// instances of the Position's CCPoint instance.
    /// </summary>
    public float PositionY
    {
        get { return m_obPosition.Y; }
        set { SetPosition(m_obPosition.X, value); }
    }

    /// <summary>
    /// The node's children, or null when no child has ever been added.
    /// </summary>
    public CCRawList<CCNode>? Children
    {
        get { return m_pChildren; }
    }

    public int ChildrenCount
    {
        get { return m_pChildren == null ? 0 : m_pChildren.count; }
    }

    /// <summary>
    /// Returns the camera for this node.
    /// </summary>
    public CCCamera Camera
    {
        get { return m_pCamera ?? (m_pCamera = new CCCamera()); }
    }

    /// <summary>
    /// This is only valid when the node is used in a CCGrid action. Expect this to be
    /// null when no grid action is active.
    /// </summary>
    public CCGridBase? Grid
    {
        get { return m_pGrid; }
        set { m_pGrid = value; }
    }

    /// <summary>
    /// Returns whether or not the node is visible IN the parent node.
    /// This simply returns the value of m_bVisible.
    /// </summary>
    public virtual bool VisibleInParent
    {
        get
        {
            return (m_bVisible);
        }
    }

    /// <summary>
    /// This returns true if the node is visible and the parent is visible. Otherwise,
    /// it returns false.
    /// </summary>
    public virtual bool Visible
    {
        get { 
            if (!m_bVisible) {
                return(false);
            }
            // I am not visible if my parent is not visible
            if (Parent != null) {
                return(Parent.Visible);
            }
            // Problem: If I am a root node and I am visible, then maybe I am really not visible
            // We should do more to validate when a node is part of the scene graph or not.
            // It's visible at this point
            return true; 
        }
        set { m_bVisible = value; }
    }

    /// <summary>
    /// Returns the anchor point in pixels, AnchorPoint * ContentSize. This does not use
    /// the scale factor of the node.
    /// </summary>
    public virtual CCPoint AnchorPointInPoints
    {
        get { return m_obAnchorPointInPoints; }
    }

    /// <summary>
    /// returns the Anchor Point of the node as a value [0,1], where 1 is 100% of the dimension and 0 is 0%.
    /// </summary>
    public virtual CCPoint AnchorPoint
    {
        get { return m_obAnchorPoint; }
        set
        {
            if (!value.Equals(m_obAnchorPoint))
            {
                m_obAnchorPoint = value;
                m_obAnchorPointInPoints = new CCPoint(m_obContentSize.Width * m_obAnchorPoint.X,
                                                              m_obContentSize.Height * m_obAnchorPoint.Y);
                SetTransformIsDirty();
            }
        }
    }

    /// <summary>
    /// Returns the content size with the scale applied.
    /// </summary>
    public virtual CCSize ContentSizeInPixels
    {
        get { 
            CCSize size = new CCSize(ContentSize.Width * ScaleX, ContentSize.Height * ScaleY);
            return (size);
        }
    }

    public virtual CCSize ScaledContentSize
    {
        get
        {
            var sizeToScale = ContentSize;
            return new CCSize(sizeToScale.Width * ScaleX, sizeToScale.Height * ScaleY);
        }
    }

    public virtual CCSize ContentSize
    {
        get { return m_obContentSize; }
        set
        {
            if (!CCSize.Equal(ref value, ref m_obContentSize))
            {
                m_obContentSize = value;
                m_obAnchorPointInPoints = new CCPoint(m_obContentSize.Width * m_obAnchorPoint.X,
                                                              m_obContentSize.Height * m_obAnchorPoint.Y);
                SetTransformIsDirty();
            }
        }
    }

    public bool IsRunning
    {
        // read only
        get { return m_bRunning; }
    }

    public CCNode? Parent
    {
        get { return m_pParent; }
        set {
            if (m_pParent != value)
            {
                if (CCConfiguration.SharedConfiguration.UseGraphPriority)
                {
                    CCDirector.SharedDirector.TouchDispatcher.UpdateGraphPriority(this);
                }
            }
            m_pParent = value; 
        }
    }

    public virtual bool IgnoreAnchorPointForPosition
    {
        get { return m_bIgnoreAnchorPointForPosition; }
        set
        {
            if (value != m_bIgnoreAnchorPointForPosition)
            {
                m_bIgnoreAnchorPointForPosition = value;
                SetTransformIsDirty();
            }
        }
    }

    /// <summary>
    /// Returns the given point, which is assumed to be in this node's coordinate
    /// system, as a point in the given target's coordinate system.
    /// </summary>
    /// <param name="ptInNode"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public CCPoint ConvertPointTo(ref CCPoint ptInNode, CCNode target)
    {
        CCPoint pt = NodeToWorldTransform().Transform(ptInNode);
        pt = target.WorldToNodeTransform().Transform(pt);
        return (pt);
    }

    /// <summary>
    /// Returns this node's bounding box in the coordinate system of the given target.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public CCRect GetBoundingBox(CCNode target)
    {
        CCRect rect = WorldBoundingBox;
        rect = target.WorldToNodeTransform().Transform(rect);
        return (rect);
    }

    /// <summary>
    /// Returns the bounding box of this node in world coordinates
    /// </summary>
    public CCRect WorldBoundingBox
    {
        get
        {
            var rect = new CCRect(0, 0, m_obContentSize.Width, m_obContentSize.Height);
            return CCAffineTransform.Transform(rect, NodeToWorldTransform());
        }
    }

    // Bounding box after scale/rotation/skew in world space
    public CCRect BoundingBoxTransformedToWorld
    {
        get
        {
            CCAffineTransform localTransform = AffineWorldTransform;
            CCRect worldtransformedBounds = localTransform.Transform(new CCRect(0.0f, 0.0f, ContentSize.Width, ContentSize.Height));
            return worldtransformedBounds;
        }
    }

    public virtual CCAffineTransform AffineLocalTransform
    {
        get { if (m_bTransformDirty) return NodeToParentTransform(); return m_sTransform; }
    }

    public CCAffineTransform AffineWorldTransform
    {
        get
        {
            CCAffineTransform worldTransform = AffineLocalTransform;
            CCNode? parent = this.Parent;
            if (parent != null)
            {
                var parentTransform = parent.AffineWorldTransform;
                CCAffineTransform.Concat(ref worldTransform, ref parentTransform, out worldTransform);
            }

            return worldTransform;
        }
    }

    /// <summary>
    /// Returns the bounding box of this node in the coordinate system of its parent.
    /// </summary>
    public CCRect BoundingBox
    {
        get
        {
            var rect = new CCRect(0, 0, m_obContentSize.Width, m_obContentSize.Height);
            return CCAffineTransform.Transform(rect, NodeToParentTransform());
        }
    }

    /// <summary>
    /// Returns the bounding box of this node, in the coordinate system of its parent,
    /// with the scale, content scale, and other display transforms applied to return
    /// the per-pixel bounding box.
    /// </summary>
    public CCRect BoundingBoxInPixels
    {
        get
        {
            var rect = new CCRect(0, 0, ContentSizeInPixels.Width, ContentSizeInPixels.Height);
            return CCAffineTransform.Transform(rect, NodeToParentTransform());
        }
    }

    /// <summary>
    /// Returns the bounding box size of this node in the coordinate system of its parent.
    /// </summary>
    public CCSize Size
    {
        get
        {
            var boundingBox = BoundingBox;
            return boundingBox.Size;
        }
    }

    public uint OrderOfArrival
    {
        get { return m_uOrderOfArrival; }
    }

    /// <summary>
    /// Sets all of the transform indictators to dirty so that the visual transforms
    /// are recomputed.
    /// </summary>
    public virtual void ForceTransformRefresh()
    {
        m_bTransformDirty = true;
        m_bWorldTransformIsDirty = true;
        _additionalTransformDirty = true;
        m_bInverseDirty = true;
    }

    public CCAffineTransform AdditionalTransform
    {
        get { return _additionalTransform; }
        set
        {
            _additionalTransform = value;
            m_bTransformDirty = true;
            _additionalTransformDirty = true;
        }
    }

    public string? Name
    {
        get { return _name; }
        set { _name = value; }
    }

    #region SelectorProtocol Members

    public virtual void Update(float dt)
    {
        /*
        if (m_nUpdateScriptHandler)
        {
            CCScriptEngineManager::sharedManager()->getScriptEngine()->executeSchedule(m_nUpdateScriptHandler, fDelta, this);
        }

        if (m_pComponentContainer && !m_pComponentContainer->isEmpty())
        {
            m_pComponentContainer->visit(fDelta);
        }
        */
    }

    #endregion

    private bool _cleaned = false;

    ~CCNode()
    {
        //unregisterScriptHandler();
        Cleanup();
        if(m_pChildren != null)
            m_pChildren.Clear();
        if(m_pChildrenByTag != null)
            m_pChildrenByTag.Clear();
    }

    public void GetPosition(out float x, out float y)
    {
        x = m_obPosition.X;
        y = m_obPosition.Y;
    }

    public virtual void SetPosition(float x, float y)
    {
        m_obPosition.X = x;
        m_obPosition.Y = y;
        SetTransformIsDirty();
    }

    protected virtual void ResetCleanState()
    {
        _cleaned = false;
        if (m_pChildren != null && m_pChildren.count > 0)
        {
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                elements[i].ResetCleanState();
            }
        }
    }

    public virtual void Cleanup()
    {
        if (_cleaned == true)
        {
            return;
        }
        // actions
        StopAllActions();

        // timers
        UnscheduleAllSelectors();

        if (m_pChildren != null && m_pChildren.count > 0)
        {
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                elements[i].Cleanup();
            }
        }
        _cleaned = true;
    }

    public CCNode? GetChildByTag(int tag)
    {
        Debug.Assert(tag != (int) CCNodeTag.Invalid, "Invalid tag");

        if (m_pChildrenByTag != null && m_pChildrenByTag.Count > 0)
        {
            //Debug.Assert(m_pChildren != null && m_pChildren.count > 0);

            List<CCNode>? list;
            if (m_pChildrenByTag.TryGetValue(tag, out list))
            {
                if (list.Count > 0)
                {
                    return list[0];
                }
            }
        }
        return null;
    }

    #region AddChild

    public void AddChild(CCNode child)
    {
        Debug.Assert(child != null, "Argument must be no-null");
        AddChild(child, child.ZOrder, child.Tag);
    }

    public void AddChild(CCNode child, int zOrder)
    {
        Debug.Assert(child != null, "Argument must be no-null");
        AddChild(child, zOrder, child.Tag);
    }

    public virtual void AddChild(CCNode child, int zOrder, int tag)
    {
        Debug.Assert(child != null, "Argument must be non-null");
        Debug.Assert(child.m_pParent == null, "child already added. It can't be added again");
        Debug.Assert(child != this, "Can not add myself to myself.");

        if (m_pChildren == null)
        {
            m_pChildren = new CCRawList<CCNode>();
        }

        InsertChild(child, zOrder, tag);

        child.Parent = this;
        child._tag = tag;
        child.m_uOrderOfArrival = s_globalOrderOfArrival++;
        if (child._cleaned)
        {
            child.ResetCleanState();
        }

        if (m_bRunning)
        {
            child.OnEnter();
            child.OnEnterTransitionDidFinish();
        }
        if (zOrder < _localMinZOrder)
        {
            _localMinZOrder = zOrder;
        }
        if (zOrder > _localMaxZOrder)
        {
            _localMaxZOrder = zOrder;
        }
        if (CCConfiguration.SharedConfiguration.UseGraphPriority)
        {
            // My graph is changing, so rearrange the handlers
            if (CCDirector.SharedDirector.TouchDispatcher != null)
            {
                CCDirector.SharedDirector.TouchDispatcher.RearrangeAllHandlersUponTouch();
            }
        }
    }
    private void InsertChild(CCNode child, int zOrder, int tag)
    {
        // Only called from AddChild, which allocates m_pChildren first.
        Debug.Assert(m_pChildren != null);
        m_bReorderChildDirty = true;
        m_pChildren.Add(child);

        ChangedChildTag(child, kCCNodeTagInvalid, tag);

        child.m_nZOrder = zOrder;
        if (zOrder < _localMinZOrder)
        {
            _localMinZOrder = zOrder;
        }
        if (zOrder > _localMaxZOrder)
        {
            _localMaxZOrder = zOrder;
        }
    }
    #endregion

    #region RemoveChild

    public void RemoveFromParent(bool cleanup = true)
    {
        if (m_pParent != null)
        {
            m_pParent.RemoveChild(this, cleanup);
        }
    }

    public virtual void RemoveChild(CCNode child, bool cleanup = true)
    {
        // explicit nil handling
        if (m_pChildren == null || child == null)
        {
            return;
        }

        ChangedChildTag(child, child.Tag, kCCNodeTagInvalid);

        if (m_pChildren.Contains(child))
        {
            DetachChild(child, cleanup);
        }
    }

    public void RemoveChildByTag(int tag)
    {
        RemoveChildByTag(tag, true);
    }

    public void RemoveChildByTag(int tag, bool cleanup)
    {
        Debug.Assert(tag != (int) CCNodeTag.Invalid, "Invalid tag");

        CCNode? child = GetChildByTag(tag);

        if (child == null)
        {
            CCLog.Log("cocos2d: removeChildByTag: child not found!");
        }
        else
        {
            RemoveChild(child, cleanup);
        }
    }

    public virtual void RemoveAllChildrenByTag(int tag)
    {
        RemoveAllChildrenByTag(tag, true);
    }

    public virtual void RemoveAllChildrenByTag(int tag, bool cleanup)
    {
        Debug.Assert(tag != (int)CCNodeTag.Invalid, "Invalid tag");
        while (true)
        {
            CCNode? child = GetChildByTag(tag);
            if (child == null)
            {
                break;
            }
            RemoveChild(child, cleanup);
        }
    }

    public virtual void RemoveAllChildren(bool cleanup = true)
    {
        // not using detachChild improves speed here
        if (m_pChildren != null && m_pChildren.Count > 0)
        {
            if (m_pChildrenByTag != null)
            {
                m_pChildrenByTag.Clear();
            }
            
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                CCNode node = elements[i];

                // IMPORTANT:
                //  -1st do onExit
                //  -2nd cleanup
                if (m_bRunning)
                {
                    node.OnExitTransitionDidStart();
                    node.OnExit();
                }

                if (cleanup)
                {
                    node.Cleanup();
                }

                // set parent nil at the end
                node.Parent = null;
            }

            m_pChildren.Clear();
        }
    }

    private void DetachChild(CCNode child, bool doCleanup)
    {
        // IMPORTANT:
        //  -1st do onExit
        //  -2nd cleanup
        if (m_bRunning)
        {
            child.OnExitTransitionDidStart();
            child.OnExit();
        }

        // If you don't do cleanup, the child's actions will not get removed and the
        // its scheduledSelectors_ dict will not get released!
        if (doCleanup)
        {
            child.Cleanup();
        }

        // set parent nil at the end
        child.Parent = null;

        // Only reached from RemoveChild, for a child found in m_pChildren.
        Debug.Assert(m_pChildren != null);
        m_pChildren.Remove(child);

        // Adjust the zorder range if this removed child sat on the bounds of the range.
        if (child.ZOrder == _localMaxZOrder || child.ZOrder == _localMinZOrder)
        {
            UpdateZOrderRange();
        }
        if (CCConfiguration.SharedConfiguration.UseGraphPriority)
        {
            // My graph is changing, so rearrange the handlers
            if (CCDirector.SharedDirector.TouchDispatcher != null)
            {
                CCDirector.SharedDirector.TouchDispatcher.RearrangeAllHandlersUponTouch();
            }
        }
    }
    #endregion

    protected virtual void UpdateZOrderRange()
    {
        _localMinZOrder = int.MaxValue;
        _localMaxZOrder = int.MinValue;
        if (m_pChildren == null)
        {
            // No children: the range stays empty.
            return;
        }
        for (int i = 0; i < m_pChildren.Count; i++)
        {
            int z = m_pChildren[i].ZOrder;
            if (z < _localMinZOrder)
            {
                _localMinZOrder = z;
            }
            if (z > _localMaxZOrder)
            {
                _localMaxZOrder = z;
            }
        }
    }

    private void ChangedChildTag(CCNode child, int oldTag, int newTag)
    {
        List<CCNode>? list;

        if (m_pChildrenByTag != null && oldTag != kCCNodeTagInvalid)
        {
            if (m_pChildrenByTag.TryGetValue(oldTag, out list))
            {
                list.Remove(child);
            }
        }

        if (newTag != kCCNodeTagInvalid)
        {
            if (m_pChildrenByTag == null)
            {
                m_pChildrenByTag = new Dictionary<int, List<CCNode>>();
            }

            if (!m_pChildrenByTag.TryGetValue(newTag, out list))
            {
                list = new List<CCNode>();
                m_pChildrenByTag.Add(newTag, list);
            }

            list.Add(child);
        }
    }

    public virtual void ReorderChild(CCNode child, int zOrder)
    {
        Debug.Assert(child != null, "Child must be non-null");

        m_bReorderChildDirty = true;
        child.m_uOrderOfArrival = s_globalOrderOfArrival++;
        child.m_nZOrder = zOrder;
    }
    
    #region Child Sorting

    public virtual int Compare(CCNode? n1, CCNode? n2)
    {
        // IComparer<T> allows nulls; order them first, as Comparer<T>.Default does.
        if (ReferenceEquals(n1, n2))
        {
            return 0;
        }
        if (n1 is null)
        {
            return -1;
        }
        if (n2 is null)
        {
            return 1;
        }

        if (n1.m_nZOrder < n2.m_nZOrder || (n1.m_nZOrder == n2.m_nZOrder && n1.m_uOrderOfArrival < n2.m_uOrderOfArrival))
        {
            return -1;
        }

        if (n1 == n2)
        {
            return 0;
        }

        return 1;
    }

    public virtual void SortAllChildren()
    {
        if (m_bReorderChildDirty)
        {
            // The flag can be set with no children (it is deserialized), so check.
            if (m_pChildren != null)
            {
                Array.Sort(m_pChildren.Elements, 0, m_pChildren.count, this);
            }
            m_bReorderChildDirty = false;
        }
    }

    #endregion

    /// <summary>
    /// This is called from the Visit() method. This is where you DRAW your node. Only
    /// draw stuff from this method call.
    /// </summary>
    public virtual void Draw()
    {
        // Does nothing in the root node class.
    }

    protected void UpdateGraphIndex()
    {
        _myGraphIndex = CCDirector.SharedDirector.GraphIndex++;
        if (TouchEnabled && CCConfiguration.SharedConfiguration.UseGraphPriority)
        {
            CCDirector.SharedDirector.TouchDispatcher.UpdateGraphPriority(this);
        }
    }

    /// <summary>
    /// This is called with every call to the MainLoop on the CCDirector class. In XNA, this is the same as the Draw() call.
    /// </summary>
    public virtual void Visit()
    {
        // quick return if not visible. children won't be drawn.
        if (!m_bVisible)
        {
            return;
        }
        UpdateGraphIndex();    
        CCDrawManager.PushMatrix();

        if (m_pGrid != null && m_pGrid.Active)
        {
            m_pGrid.BeforeDraw();
            //TransformAncestors();
        }
        else
        {
            Transform();
        }

        int i = 0;

        if ((m_pChildren != null) && (m_pChildren.count > 0) && m_pChildren.Elements != null)
        {
            SortAllChildren();

            CCNode[] elements = m_pChildren.Elements;
            int count = m_pChildren.count;

            // draw children zOrder < 0
            for (; i < count; ++i)
            {
                if (elements[i].m_bVisible && elements[i].m_nZOrder < 0)
                {
                    elements[i].Visit();
                }
                else
                {
                    break;
                }
            }

            // self draw
            Draw();
            // draw the children
            for (; i < count; ++i)
            {
                // Draw the z >= 0 order children next.
                if (elements[i].m_bVisible)
                {
                    elements[i].Visit();
                }
            }
        }
        else
        {
            // self draw
            Draw();
        }

        //m_uOrderOfArrival = 0;

        if (m_pGrid != null && m_pGrid.Active)
        {
            m_pGrid.AfterDraw(this);
            Transform();
            m_pGrid.Blit();
        }

        //kmGLPopMatrix();
        CCDrawManager.PopMatrix();
    }

    public virtual void Visit(ref CCAffineTransform parentWorldTransform)
    {
        if (!Visible)
            return;

        if (m_bTransformDirty)
            NodeToParentTransform();


        var worldTransform = CCAffineTransform.Identity;
        CCAffineTransform.Concat(ref m_sTransform, ref parentWorldTransform, out worldTransform);

        SortAllChildren();

        VisitRenderer(ref worldTransform);

        if (Children != null)
        {
            var elements = Children.Elements;
            for (int i = 0, N = Children.Count; i < N; ++i)
            {
                var child = elements[i];
                if (child != null && child.Visible)
                    child.Visit(ref worldTransform);
            }
        }
    }

    protected virtual void VisitRenderer(ref CCAffineTransform worldTransform)
    {
    }

    public void TransformAncestors()
    {
        if (m_pParent != null)
        {
            m_pParent.TransformAncestors();
            m_pParent.Transform();
        }
    }

    public void SetTransform(CCAffineTransform tx)
    {
        m_sTransform = tx;
        m_bTransformDirty = false;
    }

    public void Transform()
    {
        CCDrawManager.MultMatrix(NodeToParentTransform(), m_fVertexZ);

        // XXX: Expensive calls. Camera should be integrated into the cached affine matrix
        if (m_pCamera != null && !(m_pGrid != null && m_pGrid.Active))
        {
            bool translate = (m_obAnchorPointInPoints.X != 0.0f || m_obAnchorPointInPoints.Y != 0.0f);

            if (translate)
            {
                CCDrawManager.Translate(m_obAnchorPointInPoints.X, m_obAnchorPointInPoints.Y, 0);
            }

            m_pCamera.Locate();

            if (translate)
            {
                CCDrawManager.Translate(-m_obAnchorPointInPoints.X, -m_obAnchorPointInPoints.Y, 0);
            }
        }
    }

    #region Color & Opacity
    public virtual void UpdateDisplayedColor(CCColor3B parentColor)
    {
        _displayedColor.R = (byte)(_realColor.R * parentColor.R / 255.0f);
        _displayedColor.G = (byte)(_realColor.G * parentColor.G / 255.0f);
        _displayedColor.B = (byte)(_realColor.B * parentColor.B / 255.0f);

        if (_cascadeColorEnabled)
        {
            if (_cascadeOpacityEnabled && m_pChildren != null)
            {
                for (int i = 0, count = m_pChildren.count; i < count; i++)
                {
                    var item = m_pChildren.Elements[i] as ICCRGBAProtocol;
                    if (item != null)
                    {
                        item.UpdateDisplayedColor(_displayedColor);
                    }
                }
            }
        }
    }

    public virtual void UpdateDisplayedOpacity(byte parentOpacity)
    {
        _displayedOpacity = (byte)(_realOpacity * parentOpacity / 255.0f);

        if (_cascadeOpacityEnabled && m_pChildren != null)
        {
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                var item = m_pChildren.Elements[i] as ICCRGBAProtocol;
                if (item != null)
                {
                    item.UpdateDisplayedOpacity(_displayedOpacity);
                }
            }
        }
    }
    #endregion

    public virtual void OnEnter()
    {

        // register 'parent' nodes first
        // since events are propagated in reverse order
        if (_touchEnabled)
        {
            RegisterWithTouchDispatcher();
        }

        if (m_pChildren != null && m_pChildren.count > 0)
        {
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                elements[i].OnEnter();
            }
        }

        ResumeSchedulerAndActions();

        m_bRunning = true;

        CCDirector director = CCDirector.SharedDirector;

        // add this node to concern the keypad msg
        if (_keypadEnabled)
        {
            director.KeypadDispatcher.AddDelegate(this);
        }

			// tell the director that this node is interested in Keyboard message
			if (_keyboardEnabled)
			{
				director.KeyboardDispatcher.AddDelegate(this);
			}


        if (GamePadEnabled && director.GamePadEnabled)
        {
            if (!_gamePadDelegatesInited)
            {
                _onGamePadButtonUpdateDelegate = new CCGamePadButtonDelegate(OnGamePadButtonUpdate);
                _onGamePadConnectionUpdateDelegate = new CCGamePadConnectionDelegate(OnGamePadConnectionUpdate);
                _onGamePadDPadUpdateDelegate = new CCGamePadDPadDelegate(OnGamePadDPadUpdate);
                _onGamePadStickUpdateDelegate = new CCGamePadStickUpdateDelegate(OnGamePadStickUpdate);
                _onGamePadTriggerUpdateDelegate = new CCGamePadTriggerDelegate(OnGamePadTriggerUpdate);
                _gamePadDelegatesInited = true;
            }

            CCApplication application = CCApplication.SharedApplication;

            application.GamePadButtonUpdate += _onGamePadButtonUpdateDelegate;
            application.GamePadConnectionUpdate += _onGamePadConnectionUpdateDelegate;
            application.GamePadDPadUpdate += _onGamePadDPadUpdateDelegate;
            application.GamePadStickUpdate += _onGamePadStickUpdateDelegate;
            application.GamePadTriggerUpdate += _onGamePadTriggerUpdateDelegate;
        }

        if (!_initialized)
        {
            AddedToScene();
        }

        _initialized = true;
        /*
        if (m_nScriptHandler)
        {
            CCScriptEngineManager::sharedManager()->getScriptEngine()->executeFunctionWithIntegerData(m_nScriptHandler, kCCNodeOnEnter);
        }
        */
    }

    public virtual void OnEnterTransitionDidFinish()
    {
        if (m_pChildren != null && m_pChildren.count > 0)
        {
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                elements[i].OnEnterTransitionDidFinish();
            }
        }
    }

    protected virtual void AddedToScene()
    {
    }

    public virtual void OnExitTransitionDidStart()
    {
        if (m_pChildren != null && m_pChildren.count > 0)
        {
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                elements[i].OnExitTransitionDidStart();
            }
        }
    }

    public virtual void OnExit()
    {

        CCDirector director = CCDirector.SharedDirector;

        if (_touchEnabled)
        {
            director.TouchDispatcher.RemoveDelegate(this);
            //unregisterScriptTouchHandler();
        }

        if (_keypadEnabled)
        {
            director.KeypadDispatcher.RemoveDelegate(this);
        }

			if (_keyboardEnabled)
			{
				director.KeyboardDispatcher.RemoveDelegate(this);
			}

        if (GamePadEnabled && director.GamePadEnabled)
        {
            CCApplication application = CCApplication.SharedApplication;
            application.GamePadButtonUpdate -= _onGamePadButtonUpdateDelegate;
            application.GamePadConnectionUpdate -= _onGamePadConnectionUpdateDelegate;
            application.GamePadDPadUpdate -= _onGamePadDPadUpdateDelegate;
            application.GamePadStickUpdate -= _onGamePadStickUpdateDelegate;
            application.GamePadTriggerUpdate -= _onGamePadTriggerUpdateDelegate;
        }

        PauseSchedulerAndActions();

        m_bRunning = false;

        /*
        if (m_nScriptHandler)
        {
            CCScriptEngineManager::sharedManager()->getScriptEngine()->executeFunctionWithIntegerData(m_nScriptHandler, kCCNodeOnExit);
        }
        */

        if (m_pChildren != null && m_pChildren.count > 0)
        {
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                elements[i].OnExit();
            }
        }
    }

    #region Actions

    public CCActionManager ActionManager
    {
        get { return m_pActionManager; }
        set
        {
            if (value != m_pActionManager)
            {
                StopAllActions();
                m_pActionManager = value;
            }
        }
    }

    public CCAction RunAction(CCAction action)
    {
        Debug.Assert(action != null, "Argument must be non-nil");
        m_pActionManager.AddAction(action, this, !m_bRunning);
        return action;
    }

    public CCAction Repeat(CCFiniteTimeAction[] actions, uint times)
    {
        return RunAction (new CCRepeat (new CCSequence(actions), times));
    }

    public CCAction Repeat (CCFiniteTimeAction action, uint times)
    {
        return RunAction (new CCRepeat (action, times));
    }

    public CCAction RepeatForever(params CCActionInterval[] actions)
    {
        Debug.Assert(actions != null, "Argument must be non-null");
        Debug.Assert(actions.Length > 0, "At least one action must be provided");
        var repeatForever = new CCRepeatForever(actions);
        if (actions.Length > 0)
        {
            repeatForever.Tag = actions[0].Tag;
        }
        return RunAction(repeatForever);
    }

    public CCAction RepeatForever(CCActionInterval action)
    {
        Debug.Assert(action != null, "Argument must be non-nil");
        return RunAction(new CCRepeatForever (action) { Tag = action.Tag });
    }

    /// <summary>
    /// Runs the actions in sequence. Returns null, and runs nothing, when no actions are given.
    /// </summary>
    public CCAction? RunActions(params CCFiniteTimeAction[] actions)
    {
        Debug.Assert(actions != null, "Argument must be non-nil");
        if (actions.Length == 0)
        {
            return null;
        }
        var sequence = new CCSequence(actions);
        m_pActionManager.AddAction(sequence, this, !m_bRunning);
        return sequence;
    }

    public void StopAllActions()
    {
        m_pActionManager.RemoveAllActionsFromTarget(this);
    }

    public void StopAction(CCAction action)
    {
        m_pActionManager.RemoveAction(action);
    }

    public void StopAction(int tag)
    {
        Debug.Assert(tag != (int) CCNodeTag.Invalid, "Invalid tag");
        m_pActionManager.RemoveActionByTag(tag, this);
    }

    public CCAction GetAction(int tag)
    {
        Debug.Assert(tag != (int) CCNodeTag.Invalid, "Invalid tag");
        return m_pActionManager.GetAction(tag, this);
    }

    public int NumberOfRunningActions()
    {
        return m_pActionManager.NumberOfRunningActionsInTarget(this);
    }

    #endregion

    #region CCNode - Callbacks

    public CCScheduler Scheduler
    {
        get { return m_pScheduler; }
        set
        {
            if (value != m_pScheduler)
            {
                UnscheduleAllSelectors();
                m_pScheduler = value;
            }
        }
    }

    public void ScheduleUpdate()
    {
        ScheduleUpdateWithPriority(0);
    }

    public void ScheduleUpdateWithPriority(int priority)
    {
        m_pScheduler.ScheduleUpdateForTarget(this, priority, !m_bRunning);
    }

    public void UnscheduleUpdate()
    {
        m_pScheduler.UnscheduleUpdateForTarget(this);
    }

    public void Schedule(Action<float> selector)
    {
        Schedule(selector, 0f, CCScheduler.kCCRepeatForever, 0f);
    }

    public void Schedule(Action<float> selector, float interval)
    {
        Schedule(selector, interval, CCScheduler.kCCRepeatForever, 0f);
    }

    public void Schedule(Action<float> selector, float interval, uint repeat, float delay)
    {
        Debug.Assert(selector != null, "Argument must be non-nil");
        Debug.Assert(interval >= 0, "Argument must be positive");

        m_pScheduler.ScheduleSelector(selector, this, interval, repeat, delay, !m_bRunning);
    }

    public void ScheduleOnce(Action<float> selector, float delay)
    {
        Schedule(selector, 0.0f, 0, delay);
    }

    public void Unschedule(Action<float> selector)
    {
        // explicit nil handling
        if (selector == null)
            return;

        m_pScheduler.UnscheduleSelector(selector, this);
    }

    public void UnscheduleAllSelectors()
    {
        m_pScheduler.UnscheduleAllForTarget(this);
    }

    public void UnscheduleAll()
    {
        UnscheduleAllSelectors();
    }

    /// <summary>
    /// This will resume scheduler, actions and register dispatcher
    /// </summary>
    public void Resume()
    {
        ResumeSchedulerAndActions();
        if (_touchEnabled)
        {
            RegisterWithTouchDispatcher();
        }
    }

    public void ResumeSchedulerAndActions()
    {
        m_pScheduler.ResumeTarget(this);
        m_pActionManager.ResumeTarget(this);
    }

    /// <summary>
    /// This will pause scheduler, actions and unregister dispatcher
    /// </summary>
    public void Pause()
    {
        PauseSchedulerAndActions();
        if (pDispatcher != null)
        {
            pDispatcher.RemoveDelegate(this);
        }
    }

    public void PauseSchedulerAndActions()
    {
        m_pScheduler.PauseTarget(this);
        m_pActionManager.PauseTarget(this);
    }


    #endregion

    #region Transformations

    public virtual CCAffineTransform NodeToParentTransform()
    {
        if (m_bTransformDirty)
        {
            // Translate values
            float x = m_obPosition.X;
            float y = m_obPosition.Y;

            if (m_bIgnoreAnchorPointForPosition)
            {
                x += m_obAnchorPointInPoints.X;
                y += m_obAnchorPointInPoints.Y;
            }

            // Rotation values
            // Change rotation code to handle X and Y
            // If we skew with the exact same value for both x and y then we're simply just rotating
            float cx = 1, sx = 0, cy = 1, sy = 0;
            if (m_fRotationX != 0 || m_fRotationY != 0)
            {
                float radiansX = -CCMacros.CCDegreesToRadians(m_fRotationX);
                float radiansY = -CCMacros.CCDegreesToRadians(m_fRotationY);
                cx = (float)Math.Cos(radiansX);
                sx = (float)Math.Sin(radiansX);
                cy = (float)Math.Cos(radiansY);
                sy = (float)Math.Sin(radiansY);
            }

            bool needsSkewMatrix = (m_fSkewX != 0f || m_fSkewY != 0f);

            // optimization:
            // inline anchor point calculation if skew is not needed
            if (!needsSkewMatrix && !m_obAnchorPointInPoints.Equals(CCPoint.Zero))
            {
                x += cy * -m_obAnchorPointInPoints.X * m_fScaleX + -sx * -m_obAnchorPointInPoints.Y * m_fScaleY;
                y += sy * -m_obAnchorPointInPoints.X * m_fScaleX + cx * -m_obAnchorPointInPoints.Y * m_fScaleY;
            }

            // Build Transform Matrix
            // Adjusted transform calculation for rotational skew
            m_sTransform.a = cy * m_fScaleX;
            m_sTransform.b = sy * m_fScaleX;
            m_sTransform.c = -sx * m_fScaleY;
            m_sTransform.d = cx * m_fScaleY;
            m_sTransform.tx = x;
            m_sTransform.ty = y;

            // XXX: Try to inline skew
            // If skew is needed, apply skew and then anchor point
            if (needsSkewMatrix)
            {
                var skewMatrix = new CCAffineTransform(
                    1.0f, (float) Math.Tan(CCMacros.CCDegreesToRadians(m_fSkewY)),
                    (float) Math.Tan(CCMacros.CCDegreesToRadians(m_fSkewX)), 1.0f,
                    0.0f, 0.0f);

                m_sTransform = CCAffineTransform.Concat(skewMatrix, m_sTransform);

                // adjust anchor point
                if (!m_obAnchorPointInPoints.Equals(CCPoint.Zero))
                {
                    m_sTransform = CCAffineTransform.Translate(m_sTransform,
                                                                                -m_obAnchorPointInPoints.X,
                                                                                -m_obAnchorPointInPoints.Y);
                }
            }

            if (_additionalTransformDirty)
            {
                m_sTransform.Concat(ref _additionalTransform);
                _additionalTransformDirty = false;
            }

            m_bTransformDirty = false;
        }

        return m_sTransform;
    }

    /// <summary>
    /// Set my transform to be dirty and recursively set my children's transform to be dirty.
    /// </summary>
    protected virtual void SetTransformIsDirty()
    {
        m_bTransformDirty = true;
        m_bInverseDirty = true;
        // Me and all of my children have dirty world transforms now.
        SetWorldTransformIsDirty();
    }

    protected virtual void SetWorldTransformIsDirty()
    {
        m_bWorldTransformIsDirty = true;
        if (m_pChildren != null && m_pChildren.count > 0)
        {
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                elements[i].SetWorldTransformIsDirty();
            }
        }
    }

    public virtual void UpdateTransform()
    {
        // Recursively iterate over children
        if (m_pChildren != null && m_pChildren.count > 0)
        {
            CCNode[] elements = m_pChildren.Elements;
            for (int i = 0, count = m_pChildren.count; i < count; i++)
            {
                elements[i].UpdateTransform();
            }
        }
    }

    public CCAffineTransform ParentToNodeTransform()
    {
        if (m_bInverseDirty)
        {
            _inverse = CCAffineTransform.Invert(NodeToParentTransform());
            m_bInverseDirty = false;
        }
        return _inverse;
    }

    public CCAffineTransform NodeToWorldTransform()
    {
        CCAffineTransform t = NodeToParentTransform();

        // CCLog.Log("{0}.NodeToWorld: woldIsDirty={1}, transformIsDirty={2}", GetType().Name, m_bWorldTransformIsDirty, m_bTransformDirty);

        if (!m_bWorldTransformIsDirty)
        {
            return (m_NodeToWorldTransform);
        }
        if (m_pParent != null)
        {
            CCAffineTransform n2p = m_pParent.NodeToWorldTransform();
            t.Concat(ref n2p);
        }
        m_bWorldTransformIsDirty = false;
        m_NodeToWorldTransform = t;
        return t;
    }

    public CCAffineTransform WorldToNodeTransform()
    {
        return CCAffineTransform.Invert(NodeToWorldTransform());
    }

    #endregion

    #region ConvertToSpace

    public CCPoint ConvertToNodeSpace(CCPoint worldPoint)
    {
        return CCAffineTransform.Transform(worldPoint, WorldToNodeTransform());
    }

    public CCPoint ConvertToWorldSpace(CCPoint nodePoint)
    {
        return CCAffineTransform.Transform(nodePoint, NodeToWorldTransform());
    }

    public CCPoint ConvertToNodeSpaceAr(CCPoint worldPoint)
    {
        CCPoint nodePoint = ConvertToNodeSpace(worldPoint);
        return nodePoint - m_obAnchorPointInPoints;
    }

    public CCPoint ConvertToWorldSpaceAr(CCPoint nodePoint)
    {
        CCPoint pt = nodePoint + m_obAnchorPointInPoints;
        return ConvertToWorldSpace(pt);
    }

    public CCPoint ConvertToWindowSpace(CCPoint nodePoint)
    {
        CCPoint worldPoint = ConvertToWorldSpace(nodePoint);
        return CCDirector.SharedDirector.ConvertToUi(worldPoint);
    }

    public CCPoint ConvertTouchToNodeSpace(CCTouch touch)
    {
        var point = touch.Location;
        return ConvertToNodeSpace(point);
    }

    public CCPoint ConvertTouchToNodeSpaceAr(CCTouch touch)
    {
        var point = touch.Location;
        return ConvertToNodeSpaceAr(point);
    }

    #endregion

    /*
    void registerScriptHandler(int nHandler)
    {
        unregisterScriptHandler();
        m_nScriptHandler = nHandler;
        LUALOG("[LUA] Add CCNode event handler: %d", m_nScriptHandler);
    }

    void unregisterScriptHandler(void)
    {
        if (m_nScriptHandler)
        {
            CCScriptEngineManager::sharedManager().getScriptEngine().removeLuaHandler(m_nScriptHandler);
            LUALOG("[LUA] Remove CCNode event handler: %d", m_nScriptHandler);
            m_nScriptHandler = 0;
        }
    }
    */

    public virtual void RegisterWithTouchDispatcher()
    {
        CCTouchDispatcher pDispatcher = CCDirector.SharedDirector.TouchDispatcher;

        /*
        if (m_pScriptHandlerEntry)
        {
            if (m_pScriptHandlerEntry->isMultiTouches())
            {
                pDispatcher->addStandardDelegate(this, 0);
                LUALOG("[LUA] Add multi-touches event handler: %d", m_pScriptHandlerEntry->getHandler());
            }
            else
            {
                pDispatcher->addTargetedDelegate(this,
                                     m_pScriptHandlerEntry->getPriority(),
                                     m_pScriptHandlerEntry->getSwallowsTouches());
                LUALOG("[LUA] Add touch event handler: %d", m_pScriptHandlerEntry->getHandler());
            }
            return;
        }
        */
        if (_touchMode == CCTouchMode.AllAtOnce)
        {
            pDispatcher.AddStandardDelegate(this, TouchPriority);
        }
        else
        {
            pDispatcher.AddTargetedDelegate(this, TouchPriority, true);
        }
    }

    public CCTouchMode TouchMode
    {
        get { return _touchMode; }
        set
        {
            if (_touchMode != value)
            {
                _touchMode = value;

                if (_touchEnabled)
                {
                    TouchEnabled = false;
                    TouchEnabled = true;
                }
            }
        }
    }

    public virtual bool TouchEnabled
    {
        get { return _touchEnabled; }
        set
        {
            if (_touchEnabled != value)
            {
                _touchEnabled = value;

                if (m_bRunning)
                {
                    if (value)
                    {
                        RegisterWithTouchDispatcher();
                    }
                    else
                    {
                        CCDirector.SharedDirector.TouchDispatcher.RemoveDelegate(this);
                    }
                }
            }
        }
    }

    private bool _visibleToTouches = true;

    public virtual bool VisibleForTouches
    {
        get
        {
            return (Visible && _visibleToTouches);
        }
        set
        {
            _visibleToTouches = value;
        }
    }

    private int _myGraphIndex = 0;

    /// <summary>
    /// Get or set the priority at which touches are sent to this node. When CCConfiguration.UseGraphPriority is true,
    /// set values that are on the range [0,9]. This method returns a priority value that uses graph priority where
    /// each graph level is a factor of 10.
    /// </summary>
    public virtual int TouchPriority
    {
        get {
            int p = _touchPriority;
            if (CCConfiguration.SharedConfiguration.UseGraphPriority)
            {
                return (_myGraphIndex);
            }
            return p;
        }
        set
        {
            if (_touchPriority != value)
            {
                _touchPriority = value;

                if (m_bRunning)
                {
                    TouchEnabled = false;
                    TouchEnabled = true;
                }
            }
        }
    }

    public virtual bool KeypadEnabled
    {
        get { return _keypadEnabled; }
        set
        {
            if (value != _keypadEnabled)
            {
                _keypadEnabled = value;

                if (m_bRunning)
                {
                    if (value)
                    {
                        CCDirector.SharedDirector.KeypadDispatcher.AddDelegate(this);
                    }
                    else
                    {
                        CCDirector.SharedDirector.KeypadDispatcher.RemoveDelegate(this);
                    }
                }
            }
        }
    }

		public virtual bool KeyboardEnabled
		{
			get { return _keyboardEnabled; }
			set
			{
				if (value != _keyboardEnabled)
				{
					_keyboardEnabled = value;

					if (m_bRunning)
					{
						if (value)
						{
							CCDirector.SharedDirector.KeyboardDispatcher.AddDelegate(this);
						}
						else
						{
							CCDirector.SharedDirector.KeyboardDispatcher.RemoveDelegate(this);
						}
					}
				}
			}
		}

		public virtual CCKeyboardMode KeyboardMode
		{
			get { return _keyboardMode; }
			set
			{
				if (_keyboardMode != value)
				{
					_keyboardMode = value;
				}
			}
		}

    public virtual bool GamePadEnabled
    {
        get { return (_gamePadEnabled); }
        set
        {
            if (value != _gamePadEnabled)
            {
                _gamePadEnabled = value;
            }
            if (value && !CCDirector.SharedDirector.GamePadEnabled)
            {
                CCDirector.SharedDirector.GamePadEnabled = true;
            }
        }
    }

    #region touches

    #region ICCStandardTouchDelegate Members

    public virtual void TouchesBegan(List<CCTouch> touches)
    {
    }

    public virtual void TouchesMoved(List<CCTouch> touches)
    {
    }

    public virtual void TouchesEnded(List<CCTouch> touches)
    {
    }

    public virtual void TouchesCancelled(List<CCTouch> touches)
    {
    }

    #endregion

    #region ICCTargetedTouchDelegate Members

    /// <summary>
    /// Returns true signaling that this node will consume the touch.
    /// </summary>
    /// <param name="touch"></param>
    /// <returns></returns>
    public virtual bool TouchBegan(CCTouch touch)
    {
        return true;
    }

    public virtual void TouchMoved(CCTouch touch)
    {
    }

    public virtual void TouchEnded(CCTouch touch)
    {
    }

    public virtual void TouchCancelled(CCTouch touch)
    {
    }

    #endregion

    #endregion


    public virtual void KeyBackClicked()
    {
    }

    public virtual void KeyMenuClicked()
    {
    }

		#region Keyboard Support

		public virtual void KeyPressed (Keys key) 
		{ }

		public virtual void KeyReleased (Keys key)
		{ }

		public virtual void KeyboardCurrentState (KeyboardState currentState)
		{ }

		#endregion

    #region GamePad Support
    // Created the first time gamepad input is enabled.
    private CCGamePadButtonDelegate? _onGamePadButtonUpdateDelegate;
    private CCGamePadConnectionDelegate? _onGamePadConnectionUpdateDelegate;
    private CCGamePadDPadDelegate? _onGamePadDPadUpdateDelegate;
    private CCGamePadStickUpdateDelegate? _onGamePadStickUpdateDelegate;
    private CCGamePadTriggerDelegate? _onGamePadTriggerUpdateDelegate;

    protected virtual void OnGamePadTriggerUpdate(float leftTriggerStrength, float rightTriggerStrength, Microsoft.Xna.Framework.PlayerIndex player)
    {
    }

    protected virtual void OnGamePadStickUpdate(CCGameStickStatus leftStick, CCGameStickStatus rightStick, Microsoft.Xna.Framework.PlayerIndex player)
    {
    }

    protected virtual void OnGamePadDPadUpdate(CCGamePadButtonStatus leftButton, CCGamePadButtonStatus upButton, CCGamePadButtonStatus rightButton, CCGamePadButtonStatus downButton, Microsoft.Xna.Framework.PlayerIndex player)
    {
        if (!HasFocus)
        {
            return;
        }
    }

    protected virtual void OnGamePadConnectionUpdate(Microsoft.Xna.Framework.PlayerIndex player, bool IsConnected)
    {
    }

    protected virtual void OnGamePadButtonUpdate(CCGamePadButtonStatus backButton, CCGamePadButtonStatus startButton, CCGamePadButtonStatus systemButton, CCGamePadButtonStatus aButton, CCGamePadButtonStatus bButton, CCGamePadButtonStatus xButton, CCGamePadButtonStatus yButton, CCGamePadButtonStatus leftShoulder, CCGamePadButtonStatus rightShoulder, Microsoft.Xna.Framework.PlayerIndex player)
    {
    }
    #endregion

    public void Dispose()
    {
        this.Dispose(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose of managed resources
        }

        // Want to stop all actions and timers regardless of whether or not this object was explicitly disposed
        this.Cleanup();

        // remove all children from this node recursively
        if (m_pChildrenByTag != null)
        {
            m_pChildrenByTag.Clear();
        }

        CCDirector.SharedDirector.TouchDispatcher.RemoveDelegate(this);

        // Clean up the UserData and UserObject as these may hold references to other CCNodes.
        UserData = null;
        UserObject = null;

        if (Children != null && Children.Count > 0)
        {
            CCNode[] elements = Children.Elements;
            for (var index = 0; index < Children.Elements.Length; index++)
            {
                var child = Children.Elements[index];
                if (child != null)
                {
                    if (!child._cleaned)
                    {
                        child.OnExit();
                    }
                }
            }
        }
        CleanUpParentsProperly();

    }

    public void CleanUpParentsProperly()
    {
        if (Children != null && Children.Count > 0)
        {
            CCNode[] elements = Children.Elements;
            for (var index = 0; index < Children.Elements.Length; index++)
            {
                var child = Children.Elements[index];
                child?.CleanUpParentsProperly();
            }

            Children.Clear(true);
        }
        Parent = null;
    }
}