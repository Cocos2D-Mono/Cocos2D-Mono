using System.Collections.Generic;
using System.Diagnostics;

namespace Cocos2D;

/// <summary>
/// CCMultipleLayer is a CCLayer with the ability to multiplex it's children.
/// Features:
/// - It supports one or more children
/// - Only one children will be active a time
/// </summary>
public class CCLayerMultiplex : CCLayerRGBA
{
    /// <summary>
    /// Indicates no layer to be displayed.
    /// </summary>
    public const int NoLayer = -1;
    /// <summary>
    /// Offset used to preserve uniqueness for the layer tags. This is necessary
    /// to allow for SwitchTo(index) and SwitchTo(Layer.Tag) to work properly.
    /// </summary>
    private const int kTagOffsetForUniqueness = 5000;
    /// <summary>
    /// Current index of the active layer.
    /// </summary>
    protected int m_nEnabledLayer=NoLayer;
    // SwitchToAndReleaseMe leaves a null entry for the layer it releases.
    protected Dictionary<int, CCLayer?> m_pLayers = new Dictionary<int,CCLayer?>();
    private CCAction? _inAction, _outAction;
    public bool ShowFirstLayerOnEnter { get; set; }

    #region Constructors
    public CCLayerMultiplex()
    {
        ShowFirstLayerOnEnter = true;
    }
    /// <summary>
    ///  creates a CCLayerMultiplex with one or more layers using a variable argument list. 
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="?"></param>
    /// <returns></returns>
    public CCLayerMultiplex (params CCLayer[] layer)
    {
        InitWithLayers(layer);
        ShowFirstLayerOnEnter = true;
    }

    public CCLayerMultiplex(CCAction? inAction, CCAction? outAction, params CCLayer[] layer)
    {
        InitWithLayers(layer);
        _inAction = inAction;
        _outAction = outAction;
        ShowFirstLayerOnEnter = true;
    }

    public CCLayerMultiplex(CCAction? inAction, CCAction? outAction)
    {
        _inAction = inAction;
        _outAction = outAction;
    }

    public CCLayerMultiplex(CCAction? inAction, CCAction? outAction, CCLayer layer)
    {
        InitWithLayer(layer);
        _inAction = inAction;
        _outAction = outAction;
    }
    #endregion

    #region Legacy Init Methods
    private bool InitWithLayer(CCLayer layer)
    {
        m_pLayers = new Dictionary<int,CCLayer?>();
        int ix = m_pLayers.Count;
        m_pLayers[ix] = layer;
        _LayersInOrder.Add(ix);
        if (layer.Tag != CCNode.kCCNodeTagInvalid)
        {
            m_pLayers[layer.Tag + kTagOffsetForUniqueness] = layer;
        }
        return true;
    }

    /** initializes a MultiplexLayer with one or more layers using a variable argument list. */

    private bool InitWithLayers(params CCLayer[] layer)
    {
        m_pLayers = new Dictionary<int, CCLayer?>();
        for (int i = 0; i < layer.Length; i++)
        {
            m_pLayers[i] = layer[i];
            _LayersInOrder.Add(i);
            if (layer[i].Tag != CCNode.kCCNodeTagInvalid)
            {
                m_pLayers[layer[i].Tag + kTagOffsetForUniqueness] = layer[i];
            }
        }
        return true;
    }
    #endregion

    /// <summary>
    /// The action to play on the layer that becomes the active layer
    /// </summary>
    public CCAction? InAction
    {
        get { return (_inAction); }
        set { _inAction = value; }
    }

    /// <summary>
    /// The action to play on the active layer when it becomes inactive.
    /// </summary>
    public CCAction? OutAction
    {
        get { return (_outAction); }
        set { _outAction = value; }
    }

    /// <summary>
    /// The list of layers in their insertion order.
    /// </summary>
    private List<int> _LayersInOrder = new List<int>();

    /// <summary>
    /// Switches to the first layer.
    /// </summary>
    /// <returns></returns>
    public CCLayer? SwitchToFirstLayer()
    {
        if (_LayersInOrder.Count == 0)
        {
            return (null);
        }
        return (SwitchTo(_LayersInOrder[SkipReleased(0, 1)]));
    }

    /// <summary>
    /// Switches to the next logical layer that was added to the multiplexer. Switches to the
    /// first layer when no layer is active.
    /// </summary>
    public CCLayer? SwitchToNextLayer()
    {
        if (_LayersInOrder.Count == 0)
        {
            return (null);
        }
        int idx = -1;
        if (m_nEnabledLayer != -1)
        {
            for(int z = 0; z < _LayersInOrder.Count; z++)
            {
                int ix = _LayersInOrder[z];
                CCLayer? layer = m_pLayers[ix];
                if (layer != null)
                {
                    if ((m_nEnabledLayer > kTagOffsetForUniqueness) && layer.Tag == (m_nEnabledLayer - kTagOffsetForUniqueness))
                    {
                        idx = z;
                        break;
                    }
                    else if(ix == m_nEnabledLayer) {
                        idx = z;
                        break;
                    }
                }
            }
            idx = (idx + 1) % _LayersInOrder.Count;
        }
        else
        {
            idx = 0;
        }
        if (idx == -1)
        {
            idx = 0;
        }
        return(SwitchTo(_LayersInOrder[SkipReleased(idx, 1)]));
    }

    /// <summary>
    /// Switches to the previous logical layer that was added to the multiplexer. Switches to the
    /// first layer when no layer is active.
    /// </summary>
    public CCLayer? SwitchToPreviousLayer()
    {
        if (_LayersInOrder.Count == 0)
        {
            return (null);
        }
        int idx = -1;
        if (m_nEnabledLayer != -1)
        {
            for (int z = 0; z < _LayersInOrder.Count; z++)
            {
                int ix = _LayersInOrder[z];
                CCLayer? layer = m_pLayers[ix];
                if (layer != null)
                {
                    if ((m_nEnabledLayer > kTagOffsetForUniqueness) && layer.Tag == (m_nEnabledLayer - kTagOffsetForUniqueness))
                    {
                        idx = z;
                        break;
                    }
                    else if (ix == m_nEnabledLayer)
                    {
                        idx = z;
                        break;
                    }
                }
            }
            idx = idx - 1;
            if (idx < 0)
            {
                idx = _LayersInOrder.Count - 1;
            }
            idx = SkipReleased(idx, -1);
        }
        else
        {
            idx = SkipReleased(0, 1);
        }
        return(SwitchTo(_LayersInOrder[idx]));
    }

    /// <summary>
    /// Steps through the layer order from position 'from', wrapping around in direction 'step',
    /// to the first layer that SwitchToAndReleaseMe hasn't released. Returns 'from' if every
    /// layer has been released.
    /// </summary>
    private int SkipReleased(int from, int step)
    {
        int count = _LayersInOrder.Count;
        for (int i = 0, pos = from; i < count; i++, pos = (pos + step + count) % count)
        {
            if (m_pLayers[_LayersInOrder[pos]] != null)
            {
                return pos;
            }
        }
        return from;
    }

    /// <summary>
    /// Adds the given layer to the list of layers to multiplex. The CCNode.Tag is used
    /// as thelayer tag, but is offset by the kTagOffsetForUniqueness constant.
    /// </summary>
    /// <param name="layer"></param>
    public void AddLayer(CCLayer layer)
    {
        if (m_pLayers == null)
        {
            InitWithLayer(layer);
        }
        else
        {
            // The next index is the number of layers added so far. m_pLayers.Count would also
            // count the tag aliases.
            int ix = _LayersInOrder.Count;
            m_pLayers[ix] = layer;
            _LayersInOrder.Add(ix);
            if (layer.Tag != CCNode.kCCNodeTagInvalid)
            {
                m_pLayers[layer.Tag + kTagOffsetForUniqueness] = layer;
            }
        }
    }

    /// <summary>
    /// Returns the active layer that was last selected. This method will return null
    /// if no layer has been selected.
    /// </summary>
    public virtual CCLayer? ActiveLayer
    {
        get
        {
            if (m_pLayers == null || m_nEnabledLayer == -1)
            {
                return (null);
            }
            return (m_pLayers[m_nEnabledLayer]);
        }
    }

    /// <summary>
    /// This will switch to the first layer if the ShowFirstLayerOnEnter flag is true 
    /// and there is a layer in the list of multiplexed layers.
    /// </summary>
    public override void OnEnter()
    {
        if (m_nEnabledLayer == -1 && m_pLayers.Count > 0 && ShowFirstLayerOnEnter)
        {
            SwitchTo(0);
        }
        base.OnEnter();
    }

    /// <summary>
    /// Hides the current display layer and sets the current enabled layer to none.
    /// The out action is played for the current display layer. Calling this method
    /// will also set ShowFirstLayerOnEnter to false.
    /// </summary>
    public void SwitchToNone() 
    {
        SwitchTo(NoLayer);
    }

    /// <summary>
    /// Swtich to the given index layer and use the given action after the layer is
    /// added to the parent. The parameter can be the index or it can be the tag of the layer.
    /// </summary>
    /// <param name="n">Send in NoLayer to hide all multiplexed layers. Otherwise, send in a tag or the logical index of the 
    /// layer to show.</param>
    /// <returns>The layer that is going to be shown. This can return null if the SwitchTo layer is NoLayer</returns>
    public CCLayer? SwitchTo(int n)
    {
        if (n != -1)
        {
            if (m_nEnabledLayer == n || m_nEnabledLayer == (n + kTagOffsetForUniqueness))
            {
                // no-op
                if (m_nEnabledLayer == -1)
                {
                    return (null);
                }
                return (m_pLayers[m_nEnabledLayer]);
            }
        }
        if (m_nEnabledLayer != -1)
        {
            // Released layers have null entries. SwitchToAndReleaseMe clears the marker when it
            // releases the active layer, so this is a safety net.
            if (m_pLayers.TryGetValue(m_nEnabledLayer, out CCLayer? outLayer) && outLayer != null)
            {
                if (_outAction != null)
                {
                    outLayer.RunAction(
                        new CCSequence(
                            (CCFiniteTimeAction)_outAction.Copy(),
                            new CCCallFunc(() => RemoveChild(outLayer, true))
                            )
                        );
                }
                else
                {
                    RemoveChild(outLayer, true);
                }
            }
            // We have no enabled layer at this point
            m_nEnabledLayer = NoLayer;
        }
        // When NoLayer, the multiplexer shows nothing.
        if (n == NoLayer)
        {
            ShowFirstLayerOnEnter = false;
            return (null);
        }
        if (!m_pLayers.ContainsKey(n))
        {
            int f = n + kTagOffsetForUniqueness;
            if (m_pLayers.ContainsKey(f))
            {
                n = f;
            }
            else
            {
                // Invalid index - layer not found
                return(null);
            }
        }
        CCLayer? inLayer = m_pLayers[n];
        if (inLayer == null)
        {
            // Released by SwitchToAndReleaseMe, so treat it like a missing layer.
            return(null);
        }
        // Set the active layer
        AddChild(inLayer);
        m_nEnabledLayer = n;
        // Run the in-action on the new layer
        if (_inAction != null)
        {
            inLayer.RunAction(_inAction.Copy());
        }
        return (inLayer);
    }


    /// <summary>
    /// Switches to the new layer and removes the old layer from management. If n is the
    /// active layer, that layer is released too and nothing is shown.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public CCLayer? SwitchToAndReleaseMe(int n)
    {
        m_pLayers.TryGetValue(m_nEnabledLayer, out CCLayer? released);
        CCLayer? l = SwitchTo(n);
        if (released == null)
        {
            return (l);
        }

        if (l == released)
        {
            // SwitchTo kept the layer being released on screen.
            RemoveChild(released, true);
            m_nEnabledLayer = NoLayer;
            l = null;
        }

        // Release every key that maps to the layer: its index and its tag alias.
        foreach (int key in new List<int>(m_pLayers.Keys))
        {
            if (m_pLayers[key] == released)
            {
                m_pLayers[key] = null;
            }
        }
        return (l);
    }
}