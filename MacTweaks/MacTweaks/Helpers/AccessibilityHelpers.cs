using System;
using System.Runtime.InteropServices;
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
        public static extern bool ApplicationAllWindowsAreMinimized(int pid);
        
        [DllImport("libc")]
        public static extern uint getuid();

        public static bool IsRoot()
        {
            return getuid() == 0;
            
            // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            // {
            //     bool isAdmin;
            //     using (var identity = WindowsIdentity.GetCurrent())
            //     {
            //         var principal = new WindowsPrincipal(identity);
            //         isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            //     }
            //
            //     return isAdmin;
            // }
            // else
            // {
            //     return getuid() == 0;
            // }
        }
    }
}