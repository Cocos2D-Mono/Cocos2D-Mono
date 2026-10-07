#nullable disable
using System.Collections.Generic;

namespace Cocos2D;

public class CCTMXLayerInfo 
{
    public Dictionary<string, string> Properties = new Dictionary<string, string>();
    private bool _ownTiles = true;
    private bool _visible;
    private byte _opacity;
    private uint[] _tiles;
    private string _name = "";
    private CCSize _layerSize;
    private CCPoint _offset;
    private uint _maxGID;
    private uint _minGID = 100000;

    public bool OwnTiles 
    {
        get { 
            return _ownTiles; 
        }
        set { 
            _ownTiles = value; 
        }
    }

    public bool Visible
    {
        get { 
            return _visible; 
        }
        set { 
            _visible = value; 
        }
    }

    public byte Opacity
    {
        get { 
            return _opacity; 
        }
        set { 
            _opacity = value; 
        }
    }

    public uint[] Tiles {
        get {
            return _tiles;
        }
        set {
            _tiles = value;
        }
    }

    public string Name {
        get {
            return _name;
        }
        set {
            _name = value;
        }
    }

    public CCSize LayerSize {
        get {
            return _layerSize;
        }
        set {
            _layerSize = value;
        }
    }

    public CCPoint Offset {
        get {
            return _offset;
        }
        set {
            _offset = value;
        }
    }

    public uint MaxGID {
        get {
            return _maxGID;
        }
        set {
            _maxGID = value;
        }
    }

    public uint MinGID {
        get {
            return _minGID;
        }
        set {
            _minGID = value;
        }
    }
}