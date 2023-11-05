// #define DEBUG_BypassAskForPasswordModule

using System;
using AppKit;
using CoreGraphics;
using Foundation;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Credentials
{
	public class BypassAskForPasswordModule : ISudoModule
	{
		private const string GetRootPasswordScriptText = @"on generateRandomPassword()
                                                            	set possibleCharacters to ""abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ""
                                                            	set passwordLength to 12
                                                            	set generatedPassword to """"
                                                            	repeat passwordLength times
                                                            		set randomIndex to random number from 1 to (count of possibleCharacters)
                                                            		set generatedPassword to generatedPassword & character randomIndex of possibleCharacters
                                                            	end repeat
                                                            	return generatedPassword
                                                            end generateRandomPassword
                                                            
                                                            -- Generate a new password
                                                            set newPassword to generateRandomPassword()
                                                            
                                                            -- Set the password for the user. Note that the space after /Users/root next line is IMPORTANT.
                                                            do shell script ""sudo dscl . -passwd /Users/root "" & newPassword
                                                            
                                                            return newPassword";

		private const string GetAdminPasswordScriptText = @$"-- Function to generate a random password
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
                                                            end if
                                                            
                                                            -- Generate a new password
                                                            set newPassword to generateRandomPassword()
                                                            
                                                            -- Set the password for the user. Note that the space after {AdminUsername} next line is IMPORTANT.
                                                            do shell script ""sudo dscl . -passwd /Users/{AdminUsername} "" & newPassword
                                                            
                                                            return newPassword";
		
		private static readonly string AutoFillRootPasswordScriptText,
									   AutoFillAdminPasswordScriptText,
									   AutoFillSystemSettingsModalStyleAdminPasswordScriptText;

		private static readonly NSAppleScript GetRootPasswordScript = new NSAppleScript(GetRootPasswordScriptText),
											  GetAdminPasswordScript = new NSAppleScript(GetAdminPasswordScriptText),
											  AutoFillRootPasswordScript,
											  AutoFillAdminPasswordScript,
											  AutoFillSystemSettingsModalStyleAdminPasswordScript;

		private const string AdminUsername = "MacTweaks";
		
		private static readonly string RootPassword, AdminPassword;

		private static readonly bool Enabled;

		static BypassAskForPasswordModule()
		{
			bool enabled;
			
			const bool BYPASS =
			#if DEBUG_BypassAskForPasswordModule
			true;
			#else
			false;
			#endif

			if (AppHelpers.IsSudoUser || BYPASS)
			{
				var descriptor = GetRootPasswordScript.ExecuteAndReturnError(out _);

				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				enabled = descriptor != null;

				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				if (enabled)
				{
					RootPassword = descriptor.StringValue;
				}
			}

			else
			{
				enabled = false;
			}

			Enabled = enabled;

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (!enabled)
			{
				return;
			}
			
			AdminPassword = GetAdminPasswordScript.ExecuteAndReturnError(out _).StringValue;

			//TODO: Some get it to focus on username instead of password field - Focusing on the latter causes the keystrokes to fail all together

			AutoFillRootPasswordScriptText = $@"tell application ""System Events""
                                                 	set securityAgent to first process whose name is ""SecurityAgent""
                                                 	
                                                 	tell securityAgent
                                                 		try
                                                 			set usePasswordText to title of button 1 of window 1
                                                 			if usePasswordText is ""Use Password…"" then
                                                 				click button 1 of window 1
                                                 			end if
                                                 		end try
                                                 		
                                                 		set value of text field 1 of window 1 to ""root""
                                                 		set value of text field 2 of window 1 to ""{RootPassword}""
                                                 		
                                                 		try
                                                 			click button 2 of window 1
                                                 		end try
                                                 	end tell
                                                 	
                                                 	tell application ""Finder""
                                                 		activate -- This should fix null FrontmostApplication issue
                                                 	end tell
                                                 end tell";
			
			
			AutoFillRootPasswordScript = new NSAppleScript(AutoFillRootPasswordScriptText);
			
			// Weird quirks - keystroke return never work ( It just shakes the window, like when you get a wrong password ).
			// click button 2 works, but for the first invocation every time system settings is launched, it will
			// revert settings. Anyway, user will have to click button 2 manually.
			AutoFillSystemSettingsModalStyleAdminPasswordScriptText = $@"tell application ""System Events""
                                                                         	tell application process ""System Settings""
                                                                         		set frontmost to true
                                                                         		tell window 1
                                                                         			tell sheet 1
                                                                         				try
                                                                         					set usePasswordText to title of button 1
                                                                         					if usePasswordText is ""Use Password…"" then
                                                                         						click button 1
                                                                         					end if
                                                                         				end try
                                                                         				set value of text field 1 to ""root""
                                                                         				set value of text field 2 to ""{RootPassword}""
                                                                         				-- See above comment on why click button 2 is missing
                                                                         			end tell
                                                                         		end tell
                                                                         	end tell
                                                                         end tell";
			
			AutoFillSystemSettingsModalStyleAdminPasswordScript = new NSAppleScript(AutoFillSystemSettingsModalStyleAdminPasswordScriptText);
		}
		
		private NSObject DidActivateApplicationNotification;
		
		public void Start()
		{
			if (Enabled)
			{
				CGHelpers.CGEventTapManager.OnKeyDown.Event += OnCommandBacktick;
				DidActivateApplicationNotification = NSWorkspace.Notifications.ObserveDidActivateApplication(OnApplicationActivated);
			}
		}

		private static void OnApplicationActivated(object sender, NSWorkspaceApplicationEventArgs e)
		{
			Console.WriteLine(e.Application.LocalizedName);
			
			if (e.Application.LocalizedName == ConstantHelpers.SECURITY_AGENT_NAME)
			{
				AutoFillRootPasswordScript.ExecuteAndReturnError(out var _);
				
				// TODO: Make a window which asks end-user if they wanna autofill
				// the password.
				
				// var x = NSScreen.MainScreen.Frame;
				// x.Height = DockModule.DockHeight;
				// x.Location = new CGPoint(0, 0); 
				// var window = new NSWindow(x, NSWindowStyle.Closable, NSBackingStore.Buffered, false); 
				// window.MakeKeyAndOrderFront(default); 
				// Console.WriteLine(NSRunningApplication.CurrentApplication.Activate(NSApplicationActivationOptions.ActivateAllWindows));
			}
        }
        
        private static CGEvent OnCommandBacktick(IntPtr proxy, CGEventType type, CGEvent @event)
        {
	        if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
	        {
		        var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(@event.Handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

		        if (keyCode == NSKey.Grave) // ASCII code 96 = ` ( Grave accent ) ( https://theasciicode.com.ar/ )
		        {
			        AutoFillSystemSettingsModalStyleAdminPasswordScript.ExecuteAndReturnError(out _);
	                    
			        return null;
		        }
	        }
            
            return @event;
        }

        public void Stop()
        {
	        if (Enabled)
	        {
		        CGHelpers.CGEventTapManager.OnKeyDown.Event -= OnCommandBacktick;
	        }
        }
    }
}