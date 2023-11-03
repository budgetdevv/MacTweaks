#define DEBUG_BypassAskForPasswordModule

using System;
using AppKit;
using CoreGraphics;
using Foundation;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Credentials
{
	public class BypassAskForPasswordModule : ISudoModule
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

		private static readonly NSAppleScript GetAdminPasswordScript = new NSAppleScript(GetAdminPasswordScriptText),
											  AutoFillAdminPasswordScript;

		private static readonly string RootPassword;

		private static readonly bool Enabled;

		static BypassAskForPasswordModule()
		{
			bool enabled;
			
			const bool Bypass =
			#if DEBUG_BypassAskForPasswordModule
			true;
			#else
			false;
			#endif

			if (AccessibilityHelpers.IsSudoUser || Bypass)
			{
				var descriptor = GetAdminPasswordScript.ExecuteAndReturnError(out var error);

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

			//TODO: Some get it to focus on username instead of password field - Focusing on the latter causes the keystrokes to fail all together

			AutoFillAdminPasswordScriptText = $@"tell application ""System Events""
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
			AutoFillAdminPasswordScript = new NSAppleScript(AutoFillAdminPasswordScriptText);
		}
		
		private NSObject DidActivateApplicationNotification;
		
		public void Start()
		{
			if (Enabled)
			{
				// CGHelpers.CGEventTapManager.OnKeyDown.Event += OnCommandBacktick;
				DidActivateApplicationNotification = NSWorkspace.Notifications.ObserveDidActivateApplication(OnApplicationActivated);
			}
		}

		private static void OnApplicationActivated(object sender, NSWorkspaceApplicationEventArgs e)
		{
			if (e.Application.LocalizedName == ConstantHelpers.SECURITY_AGENT_NAME)
			{
				AutoFillAdminPasswordScript.ExecuteAndReturnError(out var _);
				
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
        
        private static IntPtr OnCommandBacktick(IntPtr proxy, CGEventType type, IntPtr handle, CGEvent @event)
        {
	        if (@event.Flags.GetKeyModifiersOnly() == CGEventFlags.Command)
	        {
		        var keyCode = (NSKey) AccessibilityHelpers.CGEventGetIntegerValueField(handle, AccessibilityHelpers.CGEventField.KeyboardEventKeycode);

		        if (keyCode == NSKey.Grave) // ASCII code 96 = ` ( Grave accent ) ( https://theasciicode.com.ar/ )
		        {
			        AutoFillAdminPasswordScript.ExecuteAndReturnError(out _);
	                    
			        return IntPtr.Zero;
		        }
	        }
            
            return handle;
        }

        public void Stop()
        {
	        if (Enabled)
	        {
		        // CGHelpers.CGEventTapManager.OnKeyDown.Event -= OnCommandBacktick;
	        }
        }
    }
}