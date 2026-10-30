using Microsoft.Xna.Framework;
#if ANDROID
using System.Diagnostics;
using Android.App;
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
            var metrics = Game.Activity.Resources?.DisplayMetrics;
            Debug.Assert(metrics != null, "An activity's resources have display metrics.");

            _dpi = metrics.Density * 160.0f;
#else
            //TODO: Implementing GetDPI for all platforms
            _dpi = 96;
#endif
        }
        return _dpi;
    }
}
