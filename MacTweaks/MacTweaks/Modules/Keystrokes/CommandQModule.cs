using System;
using CoreGraphics;
using AppKit;
using CoreFoundation;
using MacTweaks.Helpers;
using ObjCRuntime;

namespace MacTweaks.Modules.Keystrokes
{
    public class CommandQModule: IModule
    {
        private CGEvent.CGEventTapCallback OnCommandQCallback;

        private CFMachPort OnCommandQHandle;
        
        public void Start()
        {
            var onCommandQCallback = OnCommandQCallback = OnCommandQ;
            
            var onCommandQHandle = OnCommandQHandle = CGEvent.CreateTap(
                CGEventTapLocation.Session, 
                CGEventTapPlacement.HeadInsert,
                CGEventTapOptions.Default, 
                CGEventMask.KeyDown, 
                onCommandQCallback,
                IntPtr.Zero);
            
            CFRunLoop.Main.AddSource(onCommandQHandle.CreateRunLoopSource(), CFRunLoop.ModeCommon);
            
            CGEvent.TapEnable(onCommandQHandle);
        }

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        private IntPtr OnCommandQ(IntPtr proxy, CGEventType type, IntPtr handle, IntPtr userInfo)
        {
            if (!type.CGEventTapIsDisabled())
            {
                var activeApp = SharedWorkspace.FrontmostApplication;

                if (activeApp.LocalizedName != ConstantHelpers.FINDER_APP_NAME)
                {
                    return handle;
                }
            
                var @event = Runtime.GetINativeObject<CGEvent>(handle, false);

                if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
                {
                    var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                    if (keyCode == NSKey.Q)
                    {
                        AccessibilityHelpers.ApplicationCloseFocusedWindow(activeApp.ProcessIdentifier);
                    
                        return IntPtr.Zero;
                    }
                }
            }

            else
            {
                CGEvent.TapEnable(OnCommandQHandle);
            }
            
            return handle;
        }

        public void Stop()
        {
            OnCommandQHandle.Dispose();
        }
    }
}