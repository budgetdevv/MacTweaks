using AppKit;
using CoreGraphics;

namespace MacTweaks.Helpers
{
    public static class CGHelpers
    {
        private static readonly nfloat ScreenHeight = NSScreen.MainScreen.Frame.Height;
        
        public static CGPoint InvertY(this CGPoint point)
        {
            //https://sl.bing.net/cQoOrHsLws0
            
            var screenHeight = ScreenHeight;
            
            // Adjust for MacOS's coordinate system
            var adjustedY = screenHeight - point.Y;

            return new CGPoint(point.X, adjustedY);
        }

        public static nfloat GetCenterX(this CGRect rect)
        {
            return rect.X + rect.Width / 2;
        }
        
        public static nfloat GetCenterY(this CGRect rect)
        {
            return rect.Y + rect.Height / 2;
        }
        
        public static CGPoint GetCentrePoint(this CGRect rect)
        {
            return new CGPoint(GetCenterX(rect), GetCenterY(rect));
        }
        
        public static bool CGEventTapIsDisabled(this CGEventType type)
        {
            return type == CGEventType.TapDisabledByTimeout || type == CGEventType.TapDisabledByUserInput;
        }

        public static CGEventFlags GetKeyModifiersOnly(this CGEventFlags flags)
        {
            // Mask off anything 255 and below ( Apparently some bits are set 255 and below )
            
            const CGEventFlags IGNORE_255_AND_BELOW_MASK = (CGEventFlags) ~((ulong) 255);
            
            const CGEventFlags IGNORE_NON_COALESCED_MASK = ~CGEventFlags.NonCoalesced;

            const CGEventFlags IGN0RE_MASK = IGNORE_255_AND_BELOW_MASK & IGNORE_NON_COALESCED_MASK;

            return flags & IGN0RE_MASK;
        }
    }
}