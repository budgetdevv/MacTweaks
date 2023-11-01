using System;
using AppKit;
using CoreGraphics;

namespace MacTweaks.Views
{
    //TODO: Make it work
    public class RectDrawView: NSView
    {
        private bool shouldDrawRectangle = false;

        public RectDrawView(CGRect frame): base(frame)
        {
        }

        public void DrawRectangle()
        {
            shouldDrawRectangle = true;
            
            SetNeedsDisplayInRect(Bounds);
        }

        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);

            if (shouldDrawRectangle)
            {
                // Get the current graphics context
                var context = NSGraphicsContext.CurrentContext;
                if (context != null)
                {
                    using (var graphics = context.GraphicsPort)
                    {
                        // Set the rectangle's properties (color, width, etc.)
                        NSColor.Red.Set();
                        graphics.SetLineWidth(2);

                        // Define the rectangle's position and size
                        var rect = new CGRect(50, 50, 100, 100);

                        // Draw the rectangle
                        graphics.StrokeRect(rect);
                    }
                }

                shouldDrawRectangle = false;
            }
        }
    }
}