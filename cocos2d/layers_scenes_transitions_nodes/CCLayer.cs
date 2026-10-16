/****************************************************************************
Copyright (c) 2010-2012 cocos2d-x.org
Copyright (c) 2008-2010 Ricardo Quesada
Copyright (c) 2011      Zynga Inc.
Copyright (c) 2011-2012 openxlive.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cocos2D;

public class CCLayer : CCNode, ICCAccelerometerDelegate
{
    private bool _isAccelerometerEnabled;

    // Only exists while the clipping mode is BoundsWithRenderTarget.
    private CCRenderTexture? _renderTexture;
    private bool _restoreScissor;
    private CCRect _saveScissorRect;
    private bool _noDrawChildren;

    /// <summary>
    /// Set to true if the child drawing should be isolated in their own render target
    /// </summary>
    protected CCClipMode m_childClippingMode = CCClipMode.None;

    public CCLayer(CCClipMode clipMode)
    {
        m_childClippingMode = clipMode;
        AnchorPoint = new CCPoint(0.5f, 0.5f);
        m_bIgnoreAnchorPointForPosition = true;
        CCDirector director = CCDirector.SharedDirector;
        if (director != null)
        {
            ContentSize = director.WinSize;
        }
        Init();
    }

    /// <summary>
    /// Default layer constructor that does not use clipping.
    /// </summary>
    public CCLayer() : this(CCClipMode.None)
    {
    }


    public CCClipMode ChildClippingMode
    {
        get { return m_childClippingMode; }
        set
        {
            if (m_childClippingMode != value)
            {
                m_childClippingMode = value;
                InitClipping();
            }
        }
    }

    public override CCSize ContentSize
    {
        get { return base.ContentSize; }
        set
        {
            base.ContentSize = value;
            InitClipping();
        }
    }

    private bool _didInit = false;

    public override void Visit()
    {
        // quick return if not visible
        if (!Visible)
        {
            return;
        }
        if (m_childClippingMode == CCClipMode.None)
        {
            base.Visit();
            return;
        }

        UpdateGraphIndex();
        CCDrawManager.PushMatrix();

        if (m_pGrid != null && m_pGrid.Active)
        {
            m_pGrid.BeforeDraw();
            TransformAncestors();
        }

        Transform();

        BeforeDraw();

        if (!_noDrawChildren && m_pChildren != null)
        {
            SortAllChildren();

            CCNode[] arrayData = m_pChildren.Elements;
            int count = m_pChildren.count;
            int i = 0;

            // draw children zOrder < 0
            for (; i < count; i++)
            {
                CCNode child = arrayData[i];
                if (child.m_nZOrder < 0)
                {
                    child.Visit();
                }
                else
                {
                    break;
                }
            }

            // this draw
            Draw();

            // draw children zOrder >= 0
            for (; i < count; i++)
            {
                arrayData[i].Visit();
            }
        }
        else
        {
            Draw();
        }

        AfterDraw();

        if (m_pGrid != null && m_pGrid.Active)
        {
            m_pGrid.AfterDraw(this);
        }

        CCDrawManager.PopMatrix();
    }

    private void InitClipping()
    {
        if (m_childClippingMode == CCClipMode.BoundsWithRenderTarget)
        {
            if (_renderTexture == null || _renderTexture.ContentSize.Width < ContentSize.Width || _renderTexture.ContentSize.Height < ContentSize.Height)
            {
                _renderTexture = new CCRenderTexture((int)ContentSize.Width, (int)ContentSize.Height);
                _renderTexture.Sprite.AnchorPoint = new CCPoint(0, 0);
            }
            _renderTexture.Sprite.TextureRect = new CCRect(0, 0, ContentSize.Width, ContentSize.Height);
        }
        else
        {
            _renderTexture = null;
        }
    }

    private void BeforeDraw()
    {
        _noDrawChildren = false;

        if (m_childClippingMode == CCClipMode.Bounds)
        {
            // We always clip to the bounding box
            var rect = new CCRect(0, 0, m_obContentSize.Width, m_obContentSize.Height);
            var bounds = CCAffineTransform.Transform(rect, NodeToWorldTransform());

            var winSize = CCDirector.SharedDirector.WinSize;

            CCRect prevScissorRect;
            if (CCDrawManager.ScissorRectEnabled)
            {
                prevScissorRect = CCDrawManager.ScissorRect;
            }
            else
            {
                prevScissorRect = new CCRect(0, 0, winSize.Width, winSize.Height);
            }

            if (!bounds.IntersectsRect(prevScissorRect))
            {
                _noDrawChildren = true;
                return;
            }

            float minX = Math.Max(bounds.MinX, prevScissorRect.MinX);
            float minY = Math.Max(bounds.MinY, prevScissorRect.MinY);
            float maxX = Math.Min(bounds.MaxX, prevScissorRect.MaxX);
            float maxY = Math.Min(bounds.MaxY, prevScissorRect.MaxY);
          
            if (CCDrawManager.ScissorRectEnabled)
            {
                _restoreScissor = true;
            }
            else
            {
                CCDrawManager.ScissorRectEnabled = true;
            }

            _saveScissorRect = prevScissorRect;

            CCDrawManager.SetScissorInPoints(minX, minY, maxX - minX, maxY - minY);
        }
        else if (m_childClippingMode == CCClipMode.BoundsWithRenderTarget)
        {
            _saveScissorRect = CCDrawManager.ScissorRect;
            _restoreScissor = CCDrawManager.ScissorRectEnabled;

            CCDrawManager.ScissorRectEnabled = false;

            CCDrawManager.PushMatrix();
            CCDrawManager.SetIdentityMatrix();

            Debug.Assert(_renderTexture != null, "InitClipping creates the render texture for BoundsWithRenderTarget");
            _renderTexture.BeginWithClear(0, 0, 0, 0);
        }
    }

    /**
 * retract what's done in beforeDraw so that there's no side effect to
 * other nodes.
 */
    private void AfterDraw()
    {
        if (m_childClippingMode != CCClipMode.None)
        {
            if (m_childClippingMode == CCClipMode.BoundsWithRenderTarget)
            {
                Debug.Assert(_renderTexture != null, "InitClipping creates the render texture for BoundsWithRenderTarget");
                _renderTexture.End();

                CCDrawManager.PopMatrix();
            }

            if (_restoreScissor)
            {
                CCDrawManager.SetScissorInPoints(
                    _saveScissorRect.Origin.X, _saveScissorRect.Origin.Y,
                    _saveScissorRect.Size.Width, _saveScissorRect.Size.Height);

                CCDrawManager.ScissorRectEnabled = true;

                _restoreScissor = false;
            }
            else
            {
                CCDrawManager.ScissorRectEnabled = false;
            }

            if (m_childClippingMode == CCClipMode.BoundsWithRenderTarget)
            {
                Debug.Assert(_renderTexture != null, "InitClipping creates the render texture for BoundsWithRenderTarget");
                _renderTexture.Sprite.Visit();
            }
        }
    }

    public override bool Init()
    {
        if (_didInit)
        {
            return (true);
        }

        TouchMode = CCTouchMode.AllAtOnce;

        bool bRet = false;
        CCDirector director = CCDirector.SharedDirector;
        if (director != null)
        {
            //                ContentSize = director.WinSize;
            _isAccelerometerEnabled = false;
            bRet = true;
            _didInit = true;
        }
        return bRet;
    }

    protected override void AddedToScene()
    {
        base.AddedToScene();
    }

    public override void OnEnter()
    {
        if(!_didInit) {
            Init();
        }

        // then iterate over all the children
        base.OnEnter();

        CCDirector director = CCDirector.SharedDirector;
        CCApplication application = CCApplication.SharedApplication;

        // add this layer to concern the Accelerometer Sensor
        if (_isAccelerometerEnabled)
        {
            director.Accelerometer.SetDelegate(this);
			}
    }

    public override void OnExit()
    {

        // remove this layer from the delegates who concern Accelerometer Sensor
        if (_isAccelerometerEnabled)
        {
            //CCDirector director = CCDirector.SharedDirector;
            //director.Accelerometer.setDelegate(null);
        }

        base.OnExit();
    }

    public override void OnEnterTransitionDidFinish()
    {
        //if (_isAccelerometerEnabled)
        //{
        //    CCDirector.SharedDirector.Accelerometer.SetDelegate(this);
        //}

        base.OnEnterTransitionDidFinish();
    }

    public bool AccelerometerEnabled
    {
        get { 
				return _isAccelerometerEnabled;
			}
        set {
            if (value != _isAccelerometerEnabled)
            {
                _isAccelerometerEnabled = value;

                if (m_bRunning)
                {
                    CCDirector pDirector = CCDirector.SharedDirector;
                    pDirector.Accelerometer.SetDelegate(value ? this : null);
                }
            }
			}
    }

    public virtual void DidAccelerate(CCAcceleration pAccelerationValue)
    {
    }
}