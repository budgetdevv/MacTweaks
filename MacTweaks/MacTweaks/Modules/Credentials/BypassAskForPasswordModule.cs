using System;
using AppKit;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Credentials
{
	public class BypassAskForPasswordModule : IModule
    {
        private const string AdminUsername = "MacTweaks";

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
                                                                       
                                                                       -- Function to generate a random UniqueID
                                                                       on generateUniqueID()
                                                                       	set isUnique to false
                                                                       	repeat until isUnique
                                                                       		set potentialID to (random number from 501 to 1000)
                                                                       		try
                                                                       			do shell script ""dscl . -read /Users/uid_"" & potentialID
                                                                       		on error
                                                                       			-- If an error occurs (i.e., the user does not exist), the ID is unique
                                                                       			set isUnique to true
                                                                       		end try
                                                                       	end repeat
                                                                       	return potentialID
                                                                       end generateUniqueID
                                                                       
                                                                       -- Check if the MacTweaks account already exists
                                                                       set userExists to false
                                                                       try
                                                                       	do shell script ""id -u {AdminUsername}""
                                                                       	set userExists to true
                                                                       on error
                                                                       	-- Do nothing if an error occurs, which likely means the user does not exist
                                                                       end try
                                                                       
                                                                       -- Create the hidden user account if it doesn't exist
                                                                       if userExists is false then
                                                                       	do shell script ""sudo dscl . -create /Users/{AdminUsername} IsHidden 1""
                                                                       	do shell script ""sudo dscl . -create /Users/{AdminUsername} UniqueID "" & generateUniqueID()
                                                                       	do shell script ""sudo dscl . -create /Users/{AdminUsername} PrimaryGroupID 80""
                                                                       	do shell script ""sudo dscl . -append /Groups/admin GroupMembership {AdminUsername}""
                                                                       	-- We don't really need the latter two - We are just using this account to acquire sudo / root. UniqueID is needed.
                                                                       	-- do shell script ""sudo dscl . -create /Users/{AdminUsername} UserShell /bin/bash""
                                                                       	-- do shell script ""sudo dscl . -create /Users/{AdminUsername} NFSHomeDirectory""
                                                                       end if
                                                                       
                                                                       -- Generate a new password
                                                                       set newPassword to generateRandomPassword()
                                                                       
                                                                       -- Set the password for the user. Note that the space after {AdminUsername} next line is IMPORTANT.
                                                                       do shell script ""sudo dscl . -passwd /Users/{AdminUsername} "" & newPassword
                                                                       
                                                                       return newPassword";

        private static readonly string AutoFillAdminPasswordScriptText;

        private static readonly NSAppleScript GetAdminPasswordScript = new NSAppleScript(GetAdminPasswordScriptText), AutoFillAdminPasswordScript;
        private static readonly string AdminPassword;

        private static readonly bool Enabled;

        static BypassAskForPasswordModule()
        {
	        var descriptor = GetAdminPasswordScript.ExecuteAndReturnError(out var error);
		        
	        var enabled = Enabled = descriptor != null;

	        if (enabled)
	        {
		        AdminPassword = descriptor.StringValue;
	        }
		        
	        //TODO: Some get it to focus on username instead of password field - Focusing on the latter causes the keystrokes to fail all together

	        AutoFillAdminPasswordScriptText =  $@"tell application ""System Events""								
                                                                 			tell process ""SecurityAgent"" -- this is the process for security dialogs
                                                                 			set value of text field 1 of window 1 to ""{AdminUsername}""
                                                                 			set value of text field 2 of window 1 to ""{AdminPassword}""
                                                                 			click button ""OK"" of window 1
                                                                 		end tell
																	end tell";
		        
	        AutoFillAdminPasswordScript = new NSAppleScript(AutoFillAdminPasswordScriptText);
        }
        
        private CGEvent.CGEventTapCallback OnCommandBacktickCallback;

        private CFMachPort OnCommandBacktickHandle;
        
        public void Start()
        {
	        if (Enabled)
	        {
		        var onCommandBacktickCallback = OnCommandBacktickCallback = OnCommandBacktick;
            
		        var onCommandBacktickHandle = OnCommandBacktickHandle = CGEvent.CreateTap(
			        CGEventTapLocation.HID, 
			        CGEventTapPlacement.HeadInsert,
			        CGEventTapOptions.Default, 
			        CGEventMask.KeyDown, 
			        onCommandBacktickCallback,
			        IntPtr.Zero);
            
		        CFRunLoop.Main.AddSource(onCommandBacktickHandle.CreateRunLoopSource(), CFRunLoop.ModeCommon);
            
		        CGEvent.TapEnable(onCommandBacktickHandle);
	        }
        }
        
        private IntPtr OnCommandBacktick(IntPtr proxy, CGEventType type, IntPtr handle, IntPtr userInfo)
        {
            if (!type.CGEventTapIsDisabled())
            {
                var @event = new CGEvent(handle);

                if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
                {
                    var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

                    if (keyCode == NSKey.Grave) // ASCII code 96 = ` ( Grave accent ) ( https://theasciicode.com.ar/ )
                    {
	                    AutoFillAdminPasswordScript.ExecuteAndReturnError(out var zzz);

	                    Console.WriteLine(zzz);
	                    
                        return IntPtr.Zero;
                    }
                }
            }

            else
            {
                CGEvent.TapEnable(OnCommandBacktickHandle);
            }
            
            return handle;
        }

        public void Stop()
        {
            OnCommandBacktickHandle.Dispose();
        }
    }
}