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
        
        private const string MacTweaksAXUIStubLibrary = "/Users/trumpmcdonaldz/Desktop/Code/MacTweaks/MacTweaks/MacTweaksAXUIStub.dylib";

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
            public NSString AXTitle;
            public NSString AXSubrole;
            public CGRect Rect;
            public NSNumber AXIsApplicationRunning;

            public AXUIElement(AXUIElementMarshaller marshaller)
            {
                var mTitle = marshaller.AXTitle;
                AXTitle = Runtime.GetNSObject<NSString>(mTitle);
                CFRelease(mTitle);

                var mSubrole = marshaller.AXSubrole;
                AXSubrole = Runtime.GetNSObject<NSString>(mSubrole);
                CFRelease(mSubrole);
                
                Rect = marshaller.Rect;

                var mAXIsApplicationRunning = marshaller.AXIsApplicationRunning;
                AXIsApplicationRunning = Runtime.GetNSObject<NSNumber>(mAXIsApplicationRunning);
                CFRelease(mAXIsApplicationRunning);
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
    }
}