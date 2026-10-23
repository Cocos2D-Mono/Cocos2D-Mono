using Microsoft.Xna.Framework;
#if ANDROID
using System.Diagnostics;
using Android.App;
using Android.Util;
#endif

namespace Cocos2D;


public static class CCDevice
{
    private static float _dpi;

    public static float GetDPI()
    {
        if (_dpi == 0)
        {
#if ANDROID
            var display = Game.Activity.WindowManager?.DefaultDisplay;
            Debug.Assert(display != null, "An activity's window manager has a default display.");
            var metrics = new DisplayMetrics();
        
            display.GetMetrics(metrics);

            _dpi = metrics.Density * 160.0f;
#else
            //TODO: Implementing GetDPI for all platforms
            _dpi = 96;
#endif
        }
        return _dpi;
    }
}
