using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AppKit;
using CoreGraphics;
using Foundation;
using ObjCRuntime;

namespace MacTweaks.Helpers
{
    public static class AccessibilityHelpers
    {
        private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        
        private const string ApplicationServicesLibrary = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
        
        private static readonly NSDictionary AccessibilityChecker = new NSDictionary("AXTrustedCheckOptionPrompt", true);

        public static bool RequestForAccessibilityIfNotGranted()
        {
            return AXIsProcessTrustedWithOptions(AccessibilityChecker.Handle);
        }

        [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
        private static extern bool AXIsProcessTrustedWithOptions(IntPtr options);
        
        [DllImport(CoreFoundationLibrary)]
        public static extern IntPtr CFRetain(IntPtr handle);

        [DllImport(CoreFoundationLibrary)]
        public static extern void CFRelease(IntPtr handle);

        [DllImport(ApplicationServicesLibrary)]
        public static extern IntPtr AXUIElementCreateSystemWide();
        
        //TODO: Improve this
        private const string MacTweaksAXUIStubLibrary = "/Users/trumpmcdonaldz/Desktop/Code/MacTweaks/MacTweaksAXUIStub/MacTweaksAXUIStub.dylib";

        [StructLayout(LayoutKind.Sequential)]
        public struct AXUIElementMarshaller
        {
            public IntPtr AXTitle;
            public IntPtr AXSubrole;
            public CGRect Rect;
            public IntPtr AXIsApplicationRunning;
            public int PID;
        }
        
        [DllImport(MacTweaksAXUIStubLibrary)]
        private static extern bool AXGetElementAtPosition(IntPtr sysWide, float x, float y, out AXUIElementMarshaller output);
        
        private static readonly IntPtr SysWide = AXUIElementCreateSystemWide();
        
        public struct AXUIElement
        {
            public string AXTitle;
            public string AXSubrole;
            public CGRect Rect;
            public int AXIsApplicationRunning;
            public int PID;

            public bool ApplicationIsRunning => AXIsApplicationRunning != 0;
            
            public AXUIElement(AXUIElementMarshaller marshaller)
            {
                var mTitle = marshaller.AXTitle;

                if (mTitle != IntPtr.Zero)
                {
                    AXTitle = Runtime.GetNSObject<NSString>(mTitle);
                    CFRelease(mTitle);
                }

                else
                {
                    AXTitle = default;
                }

                var mSubrole = marshaller.AXSubrole;

                if (mSubrole != IntPtr.Zero)
                {
                    AXSubrole = Runtime.GetNSObject<NSString>(mSubrole);
                    CFRelease(mSubrole);
                }

                else
                {
                    AXSubrole = default;
                }
                
                Rect = marshaller.Rect;

                var mAXIsApplicationRunning = marshaller.AXIsApplicationRunning;
                
                if (mAXIsApplicationRunning != IntPtr.Zero)
                {
                    AXIsApplicationRunning = Runtime.GetNSObject<NSNumber>(mAXIsApplicationRunning).Int32Value;
                    CFRelease(mAXIsApplicationRunning);
                }
                
                else
                {
                    AXIsApplicationRunning = default;
                }

                PID = marshaller.PID;
            }
        }
        
        public static bool AXGetElementAtPosition(float x, float y, out AXUIElement output)
        {
            var success = AXGetElementAtPosition(SysWide, x, y, out AXUIElementMarshaller marshaller);

            if (success)
            {
                output = new AXUIElement(marshaller);
            }

            else
            {
                output = default;
            }

            return success;
        }

        [DllImport(MacTweaksAXUIStubLibrary)]
        public static extern bool MinimizeAllWindowsForApplication(int pid);
        
        [DllImport(MacTweaksAXUIStubLibrary)]
        public static extern bool ApplicationAllWindowsAreMinimized(int pid, out bool areMinimized);
        
        [DllImport("libc")]
        public static extern uint getuid();

        public static bool IsRoot()
        {
            return getuid() == 0;
        }
        
        [DllImport(MacTweaksAXUIStubLibrary)]
        public static extern bool ApplicationFocusedWindowIsFullScreen(int pid);

        public enum CGEventField
        {
            MouseEventNumber = 0,
            MouseEventClickState = 1,
            MouseEventPressure = 2,
            MouseEventButtonNumber = 3,
            MouseEventDeltaX = 4,
            MouseEventDeltaY = 5,
            MouseEventInstantMouser = 6,
            MouseEventSubtype = 7,
            KeyboardEventAutorepeat = 8,
            KeyboardEventKeycode = 9,
            KeyboardEventKeyboardType = 10, // 0x0000000A
            ScrollWheelEventDeltaAxis1 = 11, // 0x0000000B
            ScrollWheelEventDeltaAxis2 = 12, // 0x0000000C
            ScrollWheelEventDeltaAxis3 = 13, // 0x0000000D
            ScrollWheelEventInstantMouser = 14, // 0x0000000E
            TabletEventPointX = 15, // 0x0000000F
            TabletEventPointY = 16, // 0x00000010
            TabletEventPointZ = 17, // 0x00000011
            TabletEventPointButtons = 18, // 0x00000012
            TabletEventPointPressure = 19, // 0x00000013
            TabletEventTiltX = 20, // 0x00000014
            TabletEventTiltY = 21, // 0x00000015
            TabletEventRotation = 22, // 0x00000016
            TabletEventTangentialPressure = 23, // 0x00000017
            TabletEventDeviceID = 24, // 0x00000018
            TabletEventVendor1 = 25, // 0x00000019
            TabletEventVendor2 = 26, // 0x0000001A
            TabletEventVendor3 = 27, // 0x0000001B
            TabletProximityEventVendorID = 28, // 0x0000001C
            TabletProximityEventTabletID = 29, // 0x0000001D
            TabletProximityEventPointerID = 30, // 0x0000001E
            TabletProximityEventDeviceID = 31, // 0x0000001F
            TabletProximityEventSystemTabletID = 32, // 0x00000020
            TabletProximityEventVendorPointerType = 33, // 0x00000021
            TabletProximityEventVendorPointerSerialNumber = 34, // 0x00000022
            TabletProximityEventVendorUniqueID = 35, // 0x00000023
            TabletProximityEventCapabilityMask = 36, // 0x00000024
            TabletProximityEventPointerType = 37, // 0x00000025
            TabletProximityEventEnterProximity = 38, // 0x00000026
            EventTargetProcessSerialNumber = 39, // 0x00000027
            EventTargetUnixProcessID = 40, // 0x00000028
            EventSourceUnixProcessID = 41, // 0x00000029
            EventSourceUserData = 42, // 0x0000002A
            EventSourceUserID = 43, // 0x0000002B
            EventSourceGroupID = 44, // 0x0000002C
            EventSourceStateID = 45, // 0x0000002D
            ScrollWheelEventIsContinuous = 88, // 0x00000058
            EventWindowUnderMousePointer = 91, // 0x0000005B
            EventWindowUnderMousePointerThatCanHandleThisEvent = 92, // 0x0000005C
            ScrollWheelEventFixedPtDeltaAxis1 = 93, // 0x0000005D
            ScrollWheelEventFixedPtDeltaAxis2 = 94, // 0x0000005E
            ScrollWheelEventFixedPtDeltaAxis3 = 95, // 0x0000005F
            ScrollWheelEventPointDeltaAxis1 = 96, // 0x00000060
            ScrollWheelEventPointDeltaAxis2 = 97, // 0x00000061
            ScrollWheelEventPointDeltaAxis3 = 98, // 0x00000062
            ScrollWheelEventScrollPhase = 99, // 0x00000063
            ScrollWheelEventScrollCount = 100, // 0x00000064
            ScrollWheelEventMomentumPhase = 123, // 0x0000007B
            EventUnacceleratedPointerMovementX = 170, // 0x000000AA
            EventUnacceleratedPointerMovementY = 171, // 0x000000AB
        }

        [DllImport(MacTweaksAXUIStubLibrary, EntryPoint = "CGEventGetIntegerValueFieldWrapper")]
        public static extern long CGEventGetIntegerValueField(IntPtr cgEvent, CGEventField field);
        
        [DllImport(MacTweaksAXUIStubLibrary)]
        public static extern bool ApplicationCloseFocusedWindow(int pid);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct AXUIElementRawMarshaller
        {
            public AXUIElementMarshaller Data;

            public IntPtr Handle;
        }
        
        [DllImport(MacTweaksAXUIStubLibrary)]
        private static extern bool AXGetElementAtPositionRaw(IntPtr sysWide, float x, float y, out AXUIElementRawMarshaller output);
        
        public struct AXUIElementRaw: IDisposable
        {
            public string AXTitle;
            public string AXSubrole;
            public CGRect Rect;
            public int AXIsApplicationRunning;
            public int PID;
            public IntPtr Handle; // This should be at the bottom

            public bool ApplicationIsRunning => AXIsApplicationRunning != 0;
            
            public AXUIElementRaw(AXUIElementRawMarshaller marshaller)
            {
                var dataMarshaller = marshaller.Data;
                
                var mTitle = dataMarshaller.AXTitle;

                if (mTitle != IntPtr.Zero)
                {
                    AXTitle = Runtime.GetNSObject<NSString>(mTitle);
                    CFRelease(mTitle);
                }

                else
                {
                    AXTitle = default;
                }

                var mSubrole = dataMarshaller.AXSubrole;

                if (mSubrole != IntPtr.Zero)
                {
                    AXSubrole = Runtime.GetNSObject<NSString>(mSubrole);
                    CFRelease(mSubrole);
                }

                else
                {
                    AXSubrole = default;
                }
                
                Rect = dataMarshaller.Rect;

                var mAXIsApplicationRunning = dataMarshaller.AXIsApplicationRunning;
                
                if (mAXIsApplicationRunning != IntPtr.Zero)
                {
                    AXIsApplicationRunning = Runtime.GetNSObject<NSNumber>(mAXIsApplicationRunning).Int32Value;
                    CFRelease(mAXIsApplicationRunning);
                }
                
                else
                {
                    AXIsApplicationRunning = default;
                }

                Handle = marshaller.Handle;

                PID = dataMarshaller.PID;
            }
            
            public void Dispose()
            {
                CFRelease(Handle);
            }
        }
        
        public static bool AXGetElementAtPositionRaw(float x, float y, out AXUIElementRaw output)
        {
            var success = AXGetElementAtPositionRaw(SysWide, x, y, out var marshaller);

            if (success)
            {
                output = new AXUIElementRaw(marshaller);
            }

            else
            {
                output = default;
            }

            return success;
        }
        
        // [DllImport("/System/Library/Frameworks/ApplicationServices.framework/Versions/A/Frameworks/HIServices.framework/Versions/A/HIServices")]
        // public static extern bool AXMakeProcessTrusted(IntPtr pid);
        
        [DllImport(MacTweaksAXUIStubLibrary)]
        private static extern bool GetWindowListForApplication(int pid, out IntPtr windowsHandle);

        public static bool GetWindowListForApplication(int pid, out NSValue[] windows)
        {
            if (GetWindowListForApplication(pid, out IntPtr windowsHandle))
            {
                windows = NSArray.ArrayFromHandle<NSValue>(windowsHandle);
                CFRelease(windowsHandle);
                
                return true;
            }

            windows = null;
            
            return false;
        }
        
        private const string FinderGetSelectedItemsScriptText = "tell application \"Finder\"\n" +
                                                            "    set selectedItems to selection\n" +
                                                            "    set posixPaths to {}\n" +
                                                            "    repeat with selectedItem in selectedItems\n" +
                                                            "        set selectedItemAlias to selectedItem as alias\n" +
                                                            "        set end of posixPaths to POSIX path of selectedItemAlias\n" +
                                                            "    end repeat\n" +
                                                            "    return posixPaths\n" +
                                                            "end tell";

        private static readonly NSAppleScript FinderGetSelectedItemsScript = new NSAppleScript(FinderGetSelectedItemsScriptText);

        public static List<string> FinderGetSelectedFilePaths()
        {
            var descriptor = FinderGetSelectedItemsScript.ExecuteAndReturnError(out var errorInfo);
            
            if (descriptor != null)
            {
                // Get the array of string paths from the result descriptor
                var paths = new List<string>();
                for (int i = 1; i <= descriptor.NumberOfItems; i++)
                {
                    // Get the ith descriptor from the result descriptor
                    var itemDescriptor = descriptor.DescriptorAtIndex(i);
                    // Get the string value from the item descriptor
                    var itemPath = itemDescriptor.StringValue;
                    // Add the item path to the list of paths
                    paths.Add(itemPath);
                }

                return paths;
            }
            
            // Volume is in use. Display a warning dialog.
            var alert = new NSAlert
            {
                AlertStyle = NSAlertStyle.Warning,
                InformativeText = errorInfo.ToString(),
                MessageText = "Warning"
            };
            alert.RunSheetModal(null);

            return null;
        }
        
        public static bool IsVolumeInUse(string volumePath)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                FileName = "lsof",
                Arguments = "-n | grep \"" + volumePath + "\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return !string.IsNullOrWhiteSpace(output);
        }
        
        public static bool UnmountVolume(string volumePath, bool force = false)
        {
            if (!force && IsVolumeInUse(volumePath))
            {
                return false;
            }
            
            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                FileName = "umount",
                // Account for volumes with whitespace
                Arguments = $"\"{volumePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            
            process.Start();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        
        private const string FinderToggleTrashWindowScriptText = @"tell application ""Finder""
                                                                    set trashTarget to trash
                                                                    set trashWindows to windows whose name is ""Trash""

                                                                    repeat with trashWindow in trashWindows
                                                                        if (target of trashWindow) is trashTarget then
                                                                            set isMinimized to collapsed of trashWindow
                                                                            
                                                                            if isMinimized is false then
				                                                                set collapsed of trashWindow to true
			                                                                else
				                                                                set collapsed of trashWindow to false
			                                                                end if

                                                                            return true
                                                                        end if
                                                                    end repeat

                                                                    return false
                                                                end tell";
        
        private static readonly NSAppleScript FinderToggleTrashWindowScript = new NSAppleScript(FinderToggleTrashWindowScriptText);
        
        public static bool TryToggleTrashWindow()
        {
            var descriptor = FinderToggleTrashWindowScript.ExecuteAndReturnError(out var errorInfo);

            return descriptor.BooleanValue;
        }
        
        [DllImport(MacTweaksAXUIStubLibrary)]
        public static extern bool WindowToggleMinimize(IntPtr window);

        private const string FinderSelectedElementsEjectOrMoveToTrashScriptText = @"on run
                                                                                    	run script o
                                                                                    end run
                                                                                    
                                                                                    script o
                                                                                    	tell application ""Finder""
                                                                                    		set selectedItems to selection
                                                                                    		repeat with anItem in selectedItems
                                                                                    			try
                                                                                    				if class of anItem is disk then
                                                                                    					eject anItem
                                                                                    				else if class of anItem is document file then
                                                                                    					delete anItem
                                                                                    				else if class of anItem is folder then
                                                                                    					delete anItem
                                                                                    				end if
                                                                                    			on error errMsg number errNum
                                                                                    				-- Ignore the error and continue
                                                                                    			end try
                                                                                    		end repeat
                                                                                    	end tell
                                                                                    end script";

        private static readonly NSAppleScript FinderSelectedElementsEjectOrMoveToTrashScript = new NSAppleScript(FinderSelectedElementsEjectOrMoveToTrashScriptText);
        
        public static bool SelectedElementsEjectOrMoveToTrash()
        {
            return FinderSelectedElementsEjectOrMoveToTrashScript.ExecuteAndReturnError(out _) != null;
        }
    }
}