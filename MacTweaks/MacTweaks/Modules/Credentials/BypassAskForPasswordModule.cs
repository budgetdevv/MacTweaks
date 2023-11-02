using System;
using AppKit;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using MacTweaks.Helpers;
using ObjCRuntime;

namespace MacTweaks.Modules.Credentials
{
	public class BypassAskForPasswordModule: ISudoModule
    {
        private static readonly string GetAdminPasswordScriptText = $@"-- Function to generate a random password
                                                                       on generateRandomPassword()
                                                                       	set possibleCharacters to ""abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ""
                                                                       	set passwordLength to 12
                                                                       	set generatedPassword to """"
                                                                       	repeat passwordLength times
                                                                       		set randomIndex to (random number from 1 to (count of possibleCharacters))
                                                                       		set generatedPassword to generatedPassword & character randomIndex of possibleCharacters
                                                                       	end repeat
                                                                       	return generatedPassword
                                                                       end generateRandomPassword
                                                                       
                                                                       -- Generate a new password
                                                                       set newPassword to generateRandomPassword()
                                                                       
                                                                       -- Set the password for the user. Note that the space after /Users/root next line is IMPORTANT.
                                                                       do shell script ""sudo dscl . -passwd /Users/root "" & newPassword
                                                                       
                                                                       return newPassword";

        private static readonly string AutoFillAdminPasswordScriptText;

        private static readonly NSAppleScript GetAdminPasswordScript = new NSAppleScript(GetAdminPasswordScriptText), AutoFillAdminPasswordScript;
        
        private static readonly string RootPassword;

        private static readonly bool Enabled;

        static BypassAskForPasswordModule()
        {
	        var descriptor = GetAdminPasswordScript.ExecuteAndReturnError(out var error);
		        
	        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
	        var enabled = Enabled = descriptor != null;
	        
	        if (enabled)
	        {
		        RootPassword = descriptor.StringValue;
	        }
		        
	        //TODO: Some get it to focus on username instead of password field - Focusing on the latter causes the keystrokes to fail all together

	        AutoFillAdminPasswordScriptText =  $@"tell application ""System Events""
                                                  	tell process ""SecurityAgent""
                                                  		set value of text field 1 of window 1 to ""root""
                                                  		set value of text field 2 of window 1 to ""{RootPassword}""
														try
                                                  		click button ""OK"" of window 1
                                                  		on error
                                                  			try
                                                  				click button ""Allow"" of window 1
                                                  			end try
                                                  		end try
                                                  	end tell
                                                  end tell
												  tell application ""Finder""
													activate -- This should fix null FrontmostApplication issue
												  end tell";
		        
	        AutoFillAdminPasswordScript = new NSAppleScript(AutoFillAdminPasswordScriptText);
        }
        
        public void Start()
        {
	        if (Enabled)
	        {
		        CGHelpers.CGEventTapManager.OnKeyDown.Event += OnCommandBacktick;
	        }
        }
        
        private static IntPtr OnCommandBacktick(IntPtr proxy, CGEventType type, IntPtr handle, CGEvent @event)
        {
	        if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
	        {
		        var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

		        if (keyCode == NSKey.Grave) // ASCII code 96 = ` ( Grave accent ) ( https://theasciicode.com.ar/ )
		        {
			        AutoFillAdminPasswordScript.ExecuteAndReturnError(out var zzz);
	                    
			        return IntPtr.Zero;
		        }
	        }
            
            return handle;
        }

        public void Stop()
        {
	        if (Enabled)
	        {
		        CGHelpers.CGEventTapManager.OnKeyDown.Event += OnCommandBacktick;
	        }
        }
    }
}