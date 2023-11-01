using System;
using System.Linq;
using CoreGraphics;
using AppKit;
using CoreFoundation;
using Foundation;
using MacTweaks.Helpers;
using ObjCRuntime;

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
                var @event = Runtime.GetINativeObject<CGEvent>(handle, false);

                if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
                {
                    var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                    if (keyCode == NSKey.Delete)
                    {
                        if (AccessibilityHelpers.SelectedElementsEjectOrMoveToTrash(out var diskPaths))
                        {
                            foreach (var diskPath in diskPaths)
                            {
                                if (AccessibilityHelpers.UnmountVolume(diskPath))
                                {
                                    continue;
                                }
                                
                                // Volume is in use. Display a warning dialog.
                                var alert = new NSAlert
                                {
                                    AlertStyle = NSAlertStyle.Warning,
                                    InformativeText = $"The volume {diskPath} is in use and cannot be ejected.",
                                    MessageText = "Warning"
                                };
                                alert.RunSheetModal(null);
                            }
                        }

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