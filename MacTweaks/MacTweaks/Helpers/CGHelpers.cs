using System;
using AppKit;
using CoreGraphics;

namespace MacTweaks.Helpers
{
    public static class CGHelpers
    {
        private static readonly nfloat ScreenHeight = NSScreen.MainScreen.Frame.Height;
        
        public static CGPoint ToMacOSCoordinates(this CGPoint point)
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
    }
}