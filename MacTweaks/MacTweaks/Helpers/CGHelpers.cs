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
    }
}