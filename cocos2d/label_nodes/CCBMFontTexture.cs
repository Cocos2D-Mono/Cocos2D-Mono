using System;
using System.Collections.Generic;
using System.Text;
using Box2D.Common;
using Cocos2D;
using static Cocos2D.CCLabel;

namespace cocos2d.label_nodes;

public struct CCBMFontTexture
{
    public CCRawList<ivec3> m_pNodes;
    public int m_nUsed;
    public int m_nWidth;
    public int m_nHeight;
    public int m_nDepth;
    public int[] m_pData;
    public CCTexture2D m_pTexture;
}
