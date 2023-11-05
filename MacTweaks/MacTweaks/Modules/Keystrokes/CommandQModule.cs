using System;
using CoreGraphics;
using AppKit;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Keystrokes
{
    public class CommandQModule: IModule
    {
        public void Start()
        {
            CGHelpers.CGEventTapManager.OnKeyDown.Event += OnCommandQ;
        }

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        private static CGEvent OnCommandQ(IntPtr proxy, CGEventType type, CGEvent @event)
        {
            var activeApp = SharedWorkspace.FrontmostApplication;

            if (activeApp.LocalizedName != ConstantHelpers.FINDER_APP_NAME)
            {
                return @event;
            }

            if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
            {
                var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(@event.Handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                if (keyCode == NSKey.Q)
                {
                    AccessibilityHelpers.ApplicationCloseFocusedWindow(activeApp.ProcessIdentifier);
                    
                    return null;
                }
            }
            
            return @event;
        }

        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnKeyDown.Event -= OnCommandQ;
        }
    }
}