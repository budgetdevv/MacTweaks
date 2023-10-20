using System;
using System.Linq;
using CoreGraphics;
using AppKit;
using CoreFoundation;
using Foundation;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Keystrokes
{
    public class CommandDeleteModule: IModule
    {
        private CGEvent.CGEventTapCallback OnCommandDeleteCallback;

        private CFMachPort OnCommandDeleteHandle;
        
        public void Start()
        {
            var onCommandDeleteCallback = OnCommandDeleteCallback = OnCommandDelete;
            
            var onCommandDeleteHandle = OnCommandDeleteHandle = CGEvent.CreateTap(
                CGEventTapLocation.Session, 
                CGEventTapPlacement.HeadInsert,
                CGEventTapOptions.Default, 
                CGEventMask.KeyDown, 
                onCommandDeleteCallback,
                IntPtr.Zero);
            
            CFRunLoop.Main.AddSource(onCommandDeleteHandle.CreateRunLoopSource(), CFRunLoop.ModeCommon);
            
            CGEvent.TapEnable(onCommandDeleteHandle);
        }

        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        private IntPtr OnCommandDelete(IntPtr proxy, CGEventType type, IntPtr handle, IntPtr userInfo)
        {
            if (!type.CGEventTapIsDisabled())
            {
                var @event = new CGEvent(handle);

                if (@event.Flags.HasFlag(CGEventFlags.Command))
                {
                    var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                    if (keyCode == NSKey.Delete)
                    {
                        AccessibilityHelpers.SelectedElementsEjectOrMoveToTrash();

                        return IntPtr.Zero;
                    }
                }
            }

            else
            {
                CGEvent.TapEnable(OnCommandDeleteHandle);
            }
            
            return handle;
        }

        public void Stop()
        {
            OnCommandDeleteHandle.Dispose();
        }
    }
}