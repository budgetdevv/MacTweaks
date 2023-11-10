using System;
using AppKit;
using CoreGraphics;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Mouse
{
    public class NavigationModule: IModule
    {
        public void Start()
        {
            CGHelpers.CGEventTapManager.OnOtherMouseDown.Event += OnMouseDown;
        }

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        private static CGEvent OnMouseDown(IntPtr proxy, CGEventType type, CGEvent @event)
        {
            var button = @event.MouseEventButtonNumber;
            
            const uint BACK = (uint) CGMouseButtonExtended.Back;

            // If button is < BACK, it will become an extremely
            // large uint value, causing this conditional to fail,
            // due to how uint wrap around
            var diff = unchecked((uint) button - BACK);
            
            if (diff <= BACK && SharedWorkspace.FrontmostApplication.LocalizedName == ConstantHelpers.FINDER_APP_NAME)
            {
                // In modern runtimes, it is converted into a cmov

                var isBack = diff == 0;
                
                var key = isBack ? NSKey.LeftBracket : NSKey.RightBracket;

                key = isBack && ((@event.Flags & CGEventFlags.Shift) == CGEventFlags.Shift) ? NSKey.UpArrow : key;
                
                @event = new CGEvent(null, (ushort) key, true);

                @event.Flags = CGEventFlags.Command;
                
                CGEvent.Post(@event, CGEventTapLocation.HID);

                @event.EventType = CGEventType.KeyUp;
                
                return @event;
            }

            return @event;
        }


        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnOtherMouseDown.Event -= OnMouseDown;
        }
    }
}