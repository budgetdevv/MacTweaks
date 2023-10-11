using System;
using System.Runtime.InteropServices;
using Foundation;

namespace MacTweaks.Helpers
{
    public static class AccessibilityHelpers
    {
        private static readonly NSDictionary AccessibilityChecker = new NSDictionary("AXTrustedCheckOptionPrompt", true);
        
        public static bool RequestForAccessibilityIfNotGranted()
        {
            return AXIsProcessTrustedWithOptions(AccessibilityChecker.Handle);
        }

        [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
        private static extern bool AXIsProcessTrustedWithOptions(IntPtr options);

        // public static void RequestForAccessibilityAccess()
        // {
        //     NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl("x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"));
        // }
    }
}