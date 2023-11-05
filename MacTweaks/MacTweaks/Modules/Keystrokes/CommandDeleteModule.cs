using System;
using CoreGraphics;
using AppKit;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Keystrokes
{
    public class CommandDeleteModule: IModule
    {
        private static readonly NSWorkspace SharedWorkspace = NSWorkspace.SharedWorkspace;
        
        public void Start()
        {
            CGHelpers.CGEventTapManager.OnKeyDown.Event += OnCommandDelete;
        }
        
        private static CGEvent OnCommandDelete(IntPtr proxy, CGEventType type, CGEvent @event)
        {
            if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
            {
                var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(@event.Handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                if (keyCode == NSKey.Delete)
                {
                    if (AccessibilityHelpers.SelectedElementsMoveToTrashOrReturnEjectables(out var diskPaths))
                    {
                        foreach (var diskPath in diskPaths)
                        {
                            if (AccessibilityHelpers.TryUnmountVolume(diskPath))
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

                    return null;
                }
            }
            
            return @event;
        }

        public void Stop()
        {
            CGHelpers.CGEventTapManager.OnKeyDown.Event -= OnCommandDelete;
        }
    }
}