using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cocos2D;

namespace tests;

public class SplitRowsDemo : CCSplitRows
{
    public static CCActionInterval actionWithDuration(float t)
    {
        return new CCSplitRows(t, 9);
    }
}
