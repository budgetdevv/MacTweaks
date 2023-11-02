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
        
        private static IntPtr OnCommandQ(IntPtr proxy, CGEventType type, IntPtr handle, CGEvent @event)
        {
            var activeApp = SharedWorkspace.FrontmostApplication;

            if (activeApp.LocalizedName != ConstantHelpers.FINDER_APP_NAME)
            {
                return handle;
            }

            if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
            {
                var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                if (keyCode == NSKey.Q)
                {
                    AccessibilityHelpers.ApplicationCloseFocusedWindow(activeApp.ProcessIdentifier);
                    
                    return IntPtr.Zero;
                }
            }
            
            return handle;
        }

        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnKeyDown.Event -= OnCommandQ;
        }
    }
}