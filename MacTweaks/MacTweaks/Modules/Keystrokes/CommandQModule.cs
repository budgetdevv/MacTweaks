using System;
using CoreGraphics;
using AppKit;
using CoreFoundation;
using MacTweaks.Helpers;

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
            
            CFRunLoop.Main.AddSource(onCommandQHandle.CreateRunLoopSource(), CFRunLoop.ModeDefault);
            
            CGEvent.TapEnable(onCommandQHandle);
        }
        
        private static IntPtr OnCommandQ(IntPtr proxy, CGEventType type, IntPtr handle, IntPtr userInfo)
        {
            var @event = new CGEvent(handle);

            if (@event.Flags.HasFlag(CGEventFlags.Command))
            {
                var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                if (keyCode == NSKey.Q)
                {
                    Console.WriteLine("Command Q is pressed!");
                    
                    return IntPtr.Zero;
                }
            }
            
            return handle;
        }

        public void Stop()
        {
            OnCommandQHandle.Dispose();
        }
    }
}